namespace Icod.DCurses.Internal;

using System.Runtime.ExceptionServices;
using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;

/// <summary>
/// Synchronizes one desired logical screen with the last physical-screen image known to DCurses.
/// </summary>
internal sealed class CursesRefreshEngine {
	private readonly TerminalDescription terminal;
	private readonly ITerminalOutput output;
	private readonly CursesPresentationResolver presentationResolver;
	private readonly CursesLinePresentationResolver linePresentationResolver;
	private readonly CursesCursorMotionResolver cursorMotionResolver;
	private readonly CursesCharacterShiftResolver characterShiftResolver;
	private readonly CursesLineShiftResolver lineShiftResolver;
	private readonly CursesEraseResolver eraseResolver;
	private readonly SemaphoreSlim refreshGate = new( 1, 1 );

	private CursesPhysicalScreenState? physicalScreen;
	private CursesStyle? currentStyle;
	private int? cursorRow;
	private int? cursorColumn;
	private int invalidationRequested = 1;

	/// <summary>Initializes a physical refresh engine for one terminal and output service.</summary>
	/// <param name="terminal">The active terminal capability description.</param>
	/// <param name="output">The terminal output service.</param>
	/// <param name="applicationEncoding">The application-text encoding used for deterministic byte cost.</param>
	internal CursesRefreshEngine(
		TerminalDescription terminal,
		ITerminalOutput output,
		Encoding? applicationEncoding = null
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( output );

		this.terminal = terminal;
		this.output = output;
		presentationResolver = new CursesPresentationResolver( terminal );
		linePresentationResolver = new CursesLinePresentationResolver( terminal );
		cursorMotionResolver = new CursesCursorMotionResolver( terminal );
		CursesOutputCostModel costModel = new(
			applicationEncoding ?? new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false
			)
		);
		characterShiftResolver = new CursesCharacterShiftResolver(
			terminal,
			costModel
		);
		lineShiftResolver = new CursesLineShiftResolver(
			terminal,
			costModel
		);
		eraseResolver = new CursesEraseResolver(
			terminal,
			costModel
		);
	}

	/// <summary>Requests complete physical-screen invalidation at the next refresh boundary.</summary>
	internal void Invalidate() {
		Interlocked.Exchange( ref invalidationRequested, 1 );
	}

	/// <summary>Serializes one direct terminal-control capability with refresh output.</summary>
	/// <param name="capability">The expanded terminal capability.</param>
	/// <param name="invalidatePhysicalScreen">Whether the capability invalidates retained screen knowledge.</param>
	/// <param name="cancellationToken">Cancellation for the control operation.</param>
	/// <returns>A value task representing the serialized control write.</returns>
	internal async ValueTask WriteControlAsync(
		string capability,
		bool invalidatePhysicalScreen,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( capability );
		cancellationToken.ThrowIfCancellationRequested();

		await refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			await TerminalCapabilityWriter.WriteAsync(
				output,
				capability,
				cancellationToken
			).ConfigureAwait( false );
			await output.FlushAsync(
				cancellationToken
			).ConfigureAwait( false );

			if ( invalidatePhysicalScreen ) {
				InvalidateKnownState();
			}
		} catch {
			InvalidateKnownState();
			throw;
		} finally {
			refreshGate.Release();
		}
	}

	/// <summary>Positions the physical cursor through the refresh serialization gate.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cancellationToken">Cancellation for the cursor operation.</param>
	/// <returns>A value task representing the cursor-positioning operation.</returns>
	internal async ValueTask SetCursorPositionAsync(
		int row,
		int column,
		CancellationToken cancellationToken = default
	) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		await refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			await MoveCursorAsync(
				row,
				column,
				cancellationToken
			).ConfigureAwait( false );
			await output.FlushAsync( cancellationToken ).ConfigureAwait( false );
		} catch {
			InvalidateKnownState();
			throw;
		} finally {
			refreshGate.Release();
		}
	}

	/// <summary>Restores terminal rendition to the available default capabilities.</summary>
	/// <param name="cancellationToken">Cancellation for the reset operation.</param>
	/// <returns>A value task representing rendition restoration.</returns>
	internal async ValueTask ResetRenditionAsync(
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		await refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			if ( currentStyle.HasValue ) {
				await ResetAttributesAsync(
					currentStyle.Value.Attributes,
					cancellationToken
				).ConfigureAwait( false );
			} else {
				await WriteCapabilityIfPresentAsync(
					StringCapability.ExitAttributeMode,
					cancellationToken
				).ConfigureAwait( false );
			}
			await WriteCapabilityIfPresentAsync(
				StringCapability.OriginalColorPair,
				cancellationToken
			).ConfigureAwait( false );
			await output.FlushAsync( cancellationToken ).ConfigureAwait( false );
			currentStyle = null;
		} catch {
			InvalidateKnownState();
			throw;
		} finally {
			refreshGate.Release();
		}
	}

	/// <summary>Synchronizes one desired logical screen with the physical terminal.</summary>
	/// <param name="screen">The desired logical screen.</param>
	/// <param name="requestedCursorRow">The requested final cursor row.</param>
	/// <param name="requestedCursorColumn">The requested final cursor column.</param>
	/// <param name="cancellationToken">Cancellation for the refresh operation.</param>
	/// <returns>A value task representing the refresh boundary.</returns>
	internal async ValueTask RefreshAsync(
		CursesScreen screen,
		int requestedCursorRow,
		int requestedCursorColumn,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ValidateCursor(
			screen,
			requestedCursorRow,
			requestedCursorColumn
		);
		cancellationToken.ThrowIfCancellationRequested();

		await refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			await RefreshCoreAsync(
				screen,
				requestedCursorRow,
				requestedCursorColumn,
				cancellationToken
			).ConfigureAwait( false );
		} catch {
			InvalidateKnownState();
			throw;
		} finally {
			refreshGate.Release();
		}
	}

	private async ValueTask RefreshCoreAsync(
		CursesScreen screen,
		int requestedCursorRow,
		int requestedCursorColumn,
		CancellationToken cancellationToken
	) {
		CursesVirtualScreen desired = screen.VirtualScreen;
		EnsurePhysicalScreen( desired );

		if ( 0 != Interlocked.Exchange( ref invalidationRequested, 0 ) ) {
			physicalScreen!.Invalidate();
			currentStyle = null;
			cursorRow = null;
			cursorColumn = null;
		}

		CursesLineShiftPlan? lineShift = lineShiftResolver.Resolve(
			desired,
			physicalScreen!,
			currentStyle,
			cursorRow,
			cursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
		if ( lineShift.HasValue ) {
			await ApplyLineShiftAsync(
				desired,
				lineShift.Value,
				cancellationToken
			).ConfigureAwait( false );
		} else {
			bool eraseCompletedRefresh = false;
			for ( int row = 0; row < desired.Rows && !eraseCompletedRefresh; row++ ) {
				CursesCharacterShiftPlan? characterShift = characterShiftResolver.Resolve(
					desired,
					physicalScreen!,
					row,
					currentStyle
				);
				if ( characterShift.HasValue ) {
					await ApplyCharacterShiftAsync(
						desired,
						characterShift.Value,
						cancellationToken
					).ConfigureAwait( false );
					continue;
				}

				int column = 0;
				while ( column < desired.Columns ) {
					if ( !NeedsUpdate( desired, row, column ) ) {
						column++;
						continue;
					}

					int start = FindSpanStart(
						desired,
						row,
						column
					);
					int end = FindSpanEnd(
						desired,
						row,
						column
					);

					CursesErasePlan? erasePlan = eraseResolver.Resolve(
						desired,
						physicalScreen!,
						row,
						start
					);
					if ( erasePlan.HasValue ) {
						await EraseAsync(
							desired,
							row,
							start,
							erasePlan.Value,
							cancellationToken
						).ConfigureAwait( false );
						if ( CursesEraseKind.ClearToEndOfLine == erasePlan.Value.Kind ) {
							column = desired.Columns;
							continue;
						}
						eraseCompletedRefresh = true;
						break;
					}

					await RenderSpanAsync(
						desired,
						row,
						start,
						end,
						screen.TextWidthProvider,
						cancellationToken
					).ConfigureAwait( false );
					column = end + 1;
				}
			}
		}

		await MoveCursorAsync(
			requestedCursorRow,
			requestedCursorColumn,
			cancellationToken
		).ConfigureAwait( false );
		await output.FlushAsync( cancellationToken ).ConfigureAwait( false );
		desired.MarkClean();
	}

	private void EnsurePhysicalScreen( CursesVirtualScreen desired ) {
		ArgumentNullException.ThrowIfNull( desired );

		if ( null != physicalScreen
			&& physicalScreen.Columns == desired.Columns
			&& physicalScreen.Rows == desired.Rows ) {
			return;
		}

		physicalScreen = new CursesPhysicalScreenState(
			desired.Columns,
			desired.Rows
		);
		currentStyle = null;
		cursorRow = null;
		cursorColumn = null;
	}

	private bool NeedsUpdate(
		CursesVirtualScreen desired,
		int row,
		int column
	) {
		if ( desired.IsDirty( row, column ) ) {
			return true;
		}

		if ( !physicalScreen!.TryGetCell(
			row,
			column,
			out CursesCell physicalCell
		) ) {
			return true;
		}

		return physicalCell != desired[ row, column ];
	}

	private int FindSpanStart(
		CursesVirtualScreen desired,
		int row,
		int column
	) {
		int start = column;
		while ( desired[ row, start ].IsContinuation ) {
			if ( 0 == start ) {
				throw new InvalidOperationException(
					"A logical row cannot begin with a continuation cell."
				);
			}
			start--;
		}

		return start;
	}

	private int FindSpanEnd(
		CursesVirtualScreen desired,
		int row,
		int column
	) {
		int end = column;
		while ( end + 1 < desired.Columns
			&& NeedsUpdate( desired, row, end + 1 ) ) {
			end++;
		}

		while ( end + 1 < desired.Columns
			&& desired[ row, end + 1 ].IsContinuation ) {
			end++;
		}

		return end;
	}

	private async ValueTask ApplyLineShiftAsync(
		CursesVirtualScreen desired,
		CursesLineShiftPlan plan,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( desired );
		switch ( plan.Operation ) {
			case CursesLineShiftOperation.InsertLines:
			case CursesLineShiftOperation.DeleteLines:
			case CursesLineShiftOperation.ScrollReverse:
			case CursesLineShiftOperation.ScrollForward:
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( plan ),
					plan.Operation,
					"Unknown curses line-shift operation."
				);
		}

		if ( !plan.UsesTemporaryScrollRegion ) {
			await MoveCursorAsync(
				plan.OperationRow,
				plan.OperationColumn,
				cancellationToken
			).ConfigureAwait( false );
			await TerminalCapabilityWriter.WriteAsync(
				output,
				plan.OperationSequence,
				plan.OperationAffectedLines,
				cancellationToken
			).ConfigureAwait( false );

			for ( int row = plan.TopRow; row <= plan.BottomRow; row++ ) {
				MarkPhysicalRange(
					desired,
					row,
					0,
					desired.Columns
				);
			}
			cursorRow = plan.CursorAfterRow;
			cursorColumn = plan.CursorAfterColumn;
			return;
		}

		if ( null == plan.SetRegionSequence
			|| null == plan.RestoreRegionSequence ) {
			throw new InvalidOperationException(
				"A temporary scrolling-region line shift must provide setup and restoration sequences."
			);
		}

		Exception? operationFailure = null;
		cursorRow = null;
		cursorColumn = null;
		try {
			await TerminalCapabilityWriter.WriteAsync(
				output,
				plan.SetRegionSequence,
				plan.SetRegionAffectedLines,
				cancellationToken
			).ConfigureAwait( false );
			await MoveCursorAsync(
				plan.OperationRow,
				plan.OperationColumn,
				cancellationToken
			).ConfigureAwait( false );
			await TerminalCapabilityWriter.WriteAsync(
				output,
				plan.OperationSequence,
				plan.OperationAffectedLines,
				cancellationToken
			).ConfigureAwait( false );
		} catch ( Exception exception ) {
			operationFailure = exception;
		}

		Exception? restorationFailure = null;
		cursorRow = null;
		cursorColumn = null;
		try {
			await TerminalCapabilityWriter.WriteAsync(
				output,
				plan.RestoreRegionSequence,
				plan.RestoreRegionAffectedLines,
				CancellationToken.None
			).ConfigureAwait( false );
		} catch ( Exception exception ) {
			restorationFailure = exception;
		}
		cursorRow = null;
		cursorColumn = null;

		if ( null != operationFailure && null != restorationFailure ) {
			throw new AggregateException(
				"Curses line-shift output failed and scrolling-region restoration also reported an error.",
				operationFailure,
				restorationFailure
			);
		}
		if ( null != operationFailure ) {
			ExceptionDispatchInfo.Capture( operationFailure ).Throw();
		}
		if ( null != restorationFailure ) {
			ExceptionDispatchInfo.Capture( restorationFailure ).Throw();
		}

		for ( int row = plan.TopRow; row <= plan.BottomRow; row++ ) {
			MarkPhysicalRange(
				desired,
				row,
				0,
				desired.Columns
			);
		}
	}

	private async ValueTask ApplyCharacterShiftAsync(
		CursesVirtualScreen desired,
		CursesCharacterShiftPlan plan,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( desired );
		switch ( plan.Kind ) {
			case CursesCharacterShiftKind.Insert:
			case CursesCharacterShiftKind.Delete:
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( plan ),
					plan.Kind,
					"Unknown curses character-shift operation."
				);
		}

		await MoveCursorAsync(
			plan.Row,
			plan.Column,
			cancellationToken
		).ConfigureAwait( false );
		await TerminalCapabilityWriter.WriteAsync(
			output,
			plan.Sequence,
			cancellationToken
		).ConfigureAwait( false );

		MarkPhysicalRange(
			desired,
			plan.Row,
			0,
			desired.Columns
		);
		cursorRow = plan.Row;
		cursorColumn = plan.Column;
	}

	private async ValueTask EraseAsync(
		CursesVirtualScreen desired,
		int row,
		int startColumn,
		CursesErasePlan plan,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( desired );
		if ( CursesEraseKind.ClearScreen != plan.Kind ) {
			await MoveCursorAsync(
				row,
				startColumn,
				cancellationToken
			).ConfigureAwait( false );
		}
		await ApplyStyleAsync(
			CursesStyle.Default,
			cancellationToken
		).ConfigureAwait( false );

		await TerminalCapabilityWriter.WriteAsync(
			output,
			plan.Sequence,
			plan.AffectedLines,
			cancellationToken
		).ConfigureAwait( false );

		switch ( plan.Kind ) {
			case CursesEraseKind.ClearToEndOfLine:
				MarkPhysicalRange(
					desired,
					row,
					startColumn,
					desired.Columns
				);
				cursorRow = row;
				cursorColumn = startColumn;
				break;

			case CursesEraseKind.ClearToEndOfScreen:
				MarkPhysicalRange(
					desired,
					row,
					startColumn,
					desired.Columns
				);
				for ( int candidateRow = row + 1; candidateRow < desired.Rows; candidateRow++ ) {
					MarkPhysicalRange(
						desired,
						candidateRow,
						0,
						desired.Columns
					);
				}
				cursorRow = row;
				cursorColumn = startColumn;
				break;

			case CursesEraseKind.ClearScreen:
				for ( int candidateRow = 0; candidateRow < desired.Rows; candidateRow++ ) {
					MarkPhysicalRange(
						desired,
						candidateRow,
						0,
						desired.Columns
					);
				}
				cursorRow = null;
				cursorColumn = null;
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( plan ),
					plan.Kind,
					"Unknown curses erase operation."
				);
		}
	}

	private void MarkPhysicalRange(
		CursesVirtualScreen desired,
		int row,
		int startColumn,
		int endColumnExclusive
	) {
		ArgumentNullException.ThrowIfNull( desired );
		for ( int column = startColumn; column < endColumnExclusive; column++ ) {
			physicalScreen!.SetCell(
				row,
				column,
				desired[ row, column ]
			);
		}
	}

	private async ValueTask RenderSpanAsync(
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		ICursesTextWidthProvider textWidthProvider,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( textWidthProvider );
		await MoveCursorAsync(
			row,
			start,
			cancellationToken
		).ConfigureAwait( false );

		int segmentStart = start;
		while ( segmentStart <= end ) {
			CursesStyle style = desired[ row, segmentStart ].Style;
			int segmentEnd = segmentStart;
			while ( segmentEnd + 1 <= end
				&& desired[ row, segmentEnd + 1 ].Style == style ) {
				segmentEnd++;
			}

			await RenderStyleSegmentAsync(
				desired,
				row,
				segmentStart,
				segmentEnd,
				style,
				textWidthProvider,
				cancellationToken
			).ConfigureAwait( false );
			segmentStart = segmentEnd + 1;
		}
	}

	private async ValueTask RenderStyleSegmentAsync(
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		CursesStyle style,
		ICursesTextWidthProvider textWidthProvider,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( textWidthProvider );
		await ApplyStyleAsync(
			style,
			cancellationToken
		).ConfigureAwait( false );

		await RenderCellContentAsync(
			desired,
			row,
			start,
			end,
			textWidthProvider,
			cancellationToken
		).ConfigureAwait( false );

		for ( int column = start; column <= end; column++ ) {
			physicalScreen!.SetCell(
				row,
				column,
				desired[ row, column ]
			);
		}

		if ( end + 1 < desired.Columns ) {
			cursorRow = row;
			cursorColumn = end + 1;
		} else {
			cursorRow = null;
			cursorColumn = null;
		}
	}

	private async ValueTask RenderCellContentAsync(
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		ICursesTextWidthProvider textWidthProvider,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( desired );
		ArgumentNullException.ThrowIfNull( textWidthProvider );

		StringBuilder payload = new();
		bool alternateCharacterSetActive = false;
		for ( int column = start; column <= end; column++ ) {
			CursesCell cell = desired[ row, column ];
			if ( cell.IsContinuation ) {
				continue;
			}

			bool useAlternateCharacterSet = false;
			string content;
			if ( cell.IsBlank ) {
				content = " ";
			} else if ( cell.LineGlyph.HasValue ) {
				CursesPhysicalLineGlyph resolved = linePresentationResolver.Resolve(
					cell.LineGlyph.Value,
					textWidthProvider
				);
				content = resolved.Content;
				useAlternateCharacterSet = resolved.UsesAlternateCharacterSet;
			} else {
				content = cell.Content;
			}

			if ( useAlternateCharacterSet != alternateCharacterSetActive ) {
				await FlushTextPayloadAsync(
					payload,
					cancellationToken
				).ConfigureAwait( false );
				await WriteCapabilityIfPresentAsync(
					useAlternateCharacterSet
						? StringCapability.EnterAlternateCharacterSetMode
						: StringCapability.ExitAlternateCharacterSetMode,
					cancellationToken
				).ConfigureAwait( false );
				alternateCharacterSetActive = useAlternateCharacterSet;
			}

			payload.Append( content );
		}

		await FlushTextPayloadAsync(
			payload,
			cancellationToken
		).ConfigureAwait( false );
		if ( alternateCharacterSetActive ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitAlternateCharacterSetMode,
				cancellationToken
			).ConfigureAwait( false );
		}
	}

	private async ValueTask FlushTextPayloadAsync(
		StringBuilder payload,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( payload );
		if ( 0 == payload.Length ) {
			return;
		}

		await WriteTextAsync(
			payload.ToString(),
			cancellationToken
		).ConfigureAwait( false );
		payload.Clear();
	}

	private async ValueTask MoveCursorAsync(
		int row,
		int column,
		CancellationToken cancellationToken
	) {
		if ( cursorRow == row && cursorColumn == column ) {
			return;
		}

		CursesCursorMotion motion = cursorMotionResolver.Resolve(
			cursorRow,
			cursorColumn,
			row,
			column
		);
		await TerminalCapabilityWriter.WriteAsync(
			output,
			motion.Sequence,
			cancellationToken
		).ConfigureAwait( false );

		cursorRow = row;
		cursorColumn = column;
	}

	private async ValueTask ApplyStyleAsync(
		CursesStyle requested,
		CancellationToken cancellationToken
	) {
		CursesStyle style = presentationResolver.Resolve( requested );
		if ( currentStyle.HasValue && currentStyle.Value == style ) {
			return;
		}

		if ( !currentStyle.HasValue ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitAttributeMode,
				cancellationToken
			).ConfigureAwait( false );
			await WriteCapabilityIfPresentAsync(
				StringCapability.OriginalColorPair,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyAttributesAsync(
				style.Attributes,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyColorAsync(
				style.Foreground,
				foreground: true,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyColorAsync(
				style.Background,
				foreground: false,
				cancellationToken
			).ConfigureAwait( false );
			currentStyle = style;
			return;
		}

		CursesStyle current = currentStyle.Value;
		CursesTextAttributes removedAttributes = current.Attributes
			& ~style.Attributes;
		bool returnsForegroundToDefault = !current.Foreground.IsDefault
			&& style.Foreground.IsDefault;
		bool returnsBackgroundToDefault = !current.Background.IsDefault
			&& style.Background.IsDefault;
		bool requiresReset = CursesTextAttributes.None != removedAttributes
			|| returnsForegroundToDefault
			|| returnsBackgroundToDefault;

		if ( requiresReset ) {
			if ( CursesTextAttributes.None != removedAttributes ) {
				await ResetAttributesAsync(
					current.Attributes,
					cancellationToken
				).ConfigureAwait( false );
			}
			await WriteCapabilityIfPresentAsync(
				StringCapability.OriginalColorPair,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyAttributesAsync(
				style.Attributes,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyColorAsync(
				style.Foreground,
				foreground: true,
				cancellationToken
			).ConfigureAwait( false );
			await ApplyColorAsync(
				style.Background,
				foreground: false,
				cancellationToken
			).ConfigureAwait( false );
		} else {
			CursesTextAttributes addedAttributes = style.Attributes
				& ~current.Attributes;
			await ApplyAttributesAsync(
				addedAttributes,
				cancellationToken
			).ConfigureAwait( false );
			if ( current.Foreground != style.Foreground ) {
				await ApplyColorAsync(
					style.Foreground,
					foreground: true,
					cancellationToken
				).ConfigureAwait( false );
			}
			if ( current.Background != style.Background ) {
				await ApplyColorAsync(
					style.Background,
					foreground: false,
					cancellationToken
				).ConfigureAwait( false );
			}
		}

		currentStyle = style;
	}

	private async ValueTask ApplyAttributesAsync(
		CursesTextAttributes attributes,
		CancellationToken cancellationToken
	) {
		if ( 0 != ( attributes & CursesTextAttributes.Bold ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterBoldMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Dim ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterDimMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Underline ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterUnderlineMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Reverse ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterReverseMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Standout ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterStandoutMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Italic ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterItalicMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Blink ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterBlinkMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Conceal ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.EnterInvisibleMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Strikeout ) ) {
			await WriteExtendedCapabilityIfPresentAsync(
				"smxx",
				cancellationToken
			).ConfigureAwait( false );
		}
	}

	private async ValueTask ApplyColorAsync(
		CursesColor color,
		bool foreground,
		CancellationToken cancellationToken
	) {
		switch ( color.Kind ) {
			case CursesColorKind.Default:
				return;

			case CursesColorKind.Indexed:
				int index = color.Index
					?? throw new InvalidOperationException(
						"An indexed curses color does not contain an index."
					);
				string indexedCapability = foreground
					? TerminalColors.ExpandForeground( terminal, index )
					: TerminalColors.ExpandBackground( terminal, index );
				await TerminalCapabilityWriter.WriteAsync(
					output,
					indexedCapability,
					cancellationToken
				).ConfigureAwait( false );
				return;

			case CursesColorKind.Rgb:
				if ( !color.Red.HasValue
					|| !color.Green.HasValue
					|| !color.Blue.HasValue ) {
					throw new InvalidOperationException(
						"An RGB curses color does not contain all three components."
					);
				}
				TerminalRgbColor rgb = new(
					color.Red.Value,
					color.Green.Value,
					color.Blue.Value
				);
				string rgbCapability = foreground
					? TerminalColors.ExpandForeground( terminal, rgb )
					: TerminalColors.ExpandBackground( terminal, rgb );
				await TerminalCapabilityWriter.WriteAsync(
					output,
					rgbCapability,
					cancellationToken
				).ConfigureAwait( false );
				return;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( color ),
					color.Kind,
					"Unknown curses color kind."
				);
		}
	}

	private async ValueTask ResetAttributesAsync(
		CursesTextAttributes attributes,
		CancellationToken cancellationToken
	) {
		if ( null != terminal.GetString( StringCapability.ExitAttributeMode ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitAttributeMode,
				cancellationToken
			).ConfigureAwait( false );
			return;
		}

		if ( 0 != ( attributes & CursesTextAttributes.Underline ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitUnderlineMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Standout ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitStandoutMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Italic ) ) {
			await WriteCapabilityIfPresentAsync(
				StringCapability.ExitItalicMode,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( 0 != ( attributes & CursesTextAttributes.Strikeout ) ) {
			await WriteExtendedCapabilityIfPresentAsync(
				"rmxx",
				cancellationToken
			).ConfigureAwait( false );
		}
	}

	private async ValueTask WriteCapabilityIfPresentAsync(
		StringCapability capability,
		CancellationToken cancellationToken
	) {
		string? value = terminal.GetString( capability );
		if ( null == value ) {
			return;
		}

		await TerminalCapabilityWriter.WriteAsync(
			output,
			value,
			cancellationToken
		).ConfigureAwait( false );
	}

	private async ValueTask WriteExtendedCapabilityIfPresentAsync(
		string capabilityName,
		CancellationToken cancellationToken
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( capabilityName );
		if ( !terminal.TryGetExtendedString(
			capabilityName,
			out string? value
		) || null == value ) {
			return;
		}

		await TerminalCapabilityWriter.WriteAsync(
			output,
			value,
			cancellationToken
		).ConfigureAwait( false );
	}

	private async ValueTask WriteTextAsync(
		string text,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( text );
		await output.WriteTextAsync(
			text,
			cancellationToken
		).ConfigureAwait( false );
	}

	private void InvalidateKnownState() {
		physicalScreen?.Invalidate();
		currentStyle = null;
		cursorRow = null;
		cursorColumn = null;
		Interlocked.Exchange( ref invalidationRequested, 1 );
	}

	private static void ValidateCursor(
		CursesScreen screen,
		int row,
		int column
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( row < 0 || row >= screen.Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 || column >= screen.Columns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
	}
}
