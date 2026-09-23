/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Internal;

using System.Text;
using Icod.Terminal;

/// <summary>
/// Synchronizes one desired logical screen through Terminal-owned semantic transactions.
/// </summary>
internal sealed class CursesRefreshEngine {
	private readonly TerminalSession terminalSession;
	private readonly TerminalScreenPlanner planner;
	private readonly bool useSynchronizedOutput;
	private readonly CursesPresentationResolver presentationResolver;
	private readonly CursesLinePresentationResolver linePresentationResolver;
	private readonly CursesCursorMotionResolver cursorMotionResolver;
	private readonly CursesEraseResolver eraseResolver;
	private readonly CursesCharacterShiftResolver characterShiftResolver;
	private readonly CursesLineShiftResolver lineShiftResolver;
	private readonly SemaphoreSlim refreshGate = new( 1, 1 );

	private CursesRefreshPhysicalState? physicalState;
	private int invalidationRequested = 1;
	private bool scrollRegionResetRequired;

	/// <summary>Initializes a physical refresh engine for one Terminal session.</summary>
	internal CursesRefreshEngine(
		TerminalSession terminalSession,
		bool useSynchronizedOutput
	) {
		ArgumentNullException.ThrowIfNull( terminalSession );

		this.terminalSession = terminalSession;
		this.planner = terminalSession.Screen;
		this.useSynchronizedOutput = useSynchronizedOutput;
		this.presentationResolver = new CursesPresentationResolver( this.planner );
		this.linePresentationResolver = new CursesLinePresentationResolver( this.planner );
		this.cursorMotionResolver = new CursesCursorMotionResolver( this.planner );
		CursesOutputCostModel costModel = new(
			new UTF8Encoding( encoderShouldEmitUTF8Identifier: false )
		);
		this.eraseResolver = new CursesEraseResolver( this.planner, costModel );
		this.characterShiftResolver = new CursesCharacterShiftResolver(
			this.planner,
			costModel
		);
		this.lineShiftResolver = new CursesLineShiftResolver(
			this.planner,
			costModel
		);
	}

	/// <summary>Requests complete physical-screen invalidation at the next refresh boundary.</summary>
	internal void Invalidate() {
		Interlocked.Exchange( ref this.invalidationRequested, 1 );
	}

	/// <summary>Returns the safe reset plan for the retained rendition certainty.</summary>
	internal TerminalScreenOperationPlan? PlanRenditionReset() {
		CursesStyle? current = this.physicalState?.CurrentStyle;
		return current.HasValue
			? this.presentationResolver.PlanReset( current.Value )
			: this.presentationResolver.PlanBaseline();
	}

	/// <summary>Commits one semantic plan through the refresh serialization gate.</summary>
	internal async ValueTask CommitPlanAsync(
		TerminalScreenOperationPlan plan,
		CancellationToken cancellationToken
	) {
		cancellationToken.ThrowIfCancellationRequested();
		await this.refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			CursesPreparedRefresh prepared = new(
				this.terminalSession,
				this.useSynchronizedOutput
			);
			prepared.AddPlan( plan );
			await prepared.CommitAsync( cancellationToken ).ConfigureAwait( false );
		} catch {
			this.InvalidateKnownState();
			throw;
		} finally {
			this.refreshGate.Release();
		}
	}

	/// <summary>Commits one cursor plan and publishes its target only after success.</summary>
	internal async ValueTask SetCursorPositionAsync(
		TerminalScreenOperationPlan plan,
		int row,
		int column,
		CancellationToken cancellationToken
	) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		await this.refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			CursesPreparedRefresh prepared = new(
				this.terminalSession,
				this.useSynchronizedOutput
			);
			prepared.AddPlan( plan );
			await prepared.CommitAsync( cancellationToken ).ConfigureAwait( false );
			if ( this.physicalState is not null ) {
				this.physicalState.CursorRow = row;
				this.physicalState.CursorColumn = column;
			}
		} catch {
			this.InvalidateKnownState();
			throw;
		} finally {
			this.refreshGate.Release();
		}
	}

	/// <summary>Commits one rendition-reset plan and publishes default rendition after success.</summary>
	internal async ValueTask ResetRenditionAsync(
		TerminalScreenOperationPlan plan,
		CancellationToken cancellationToken
	) {
		cancellationToken.ThrowIfCancellationRequested();
		await this.refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			CursesPreparedRefresh prepared = new(
				this.terminalSession,
				this.useSynchronizedOutput
			);
			prepared.AddPlan( plan );
			await prepared.CommitAsync( cancellationToken ).ConfigureAwait( false );
			if ( this.physicalState is not null ) {
				this.physicalState.CurrentStyle = CursesStyle.Default;
			}
		} catch {
			this.InvalidateKnownState();
			throw;
		} finally {
			this.refreshGate.Release();
		}
	}

	/// <summary>Synchronizes one desired logical screen with the physical terminal.</summary>
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

		await this.refreshGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			await this.RefreshCoreAsync(
				screen,
				requestedCursorRow,
				requestedCursorColumn,
				cancellationToken
			).ConfigureAwait( false );
		} catch {
			this.InvalidateKnownState();
			throw;
		} finally {
			this.refreshGate.Release();
		}
	}

	private async ValueTask RefreshCoreAsync(
		CursesScreen screen,
		int requestedCursorRow,
		int requestedCursorColumn,
		CancellationToken cancellationToken
	) {
		CursesVirtualScreen desired = screen.VirtualScreen;
		desired.EnableChangeTracking();
		this.EnsurePhysicalState( desired );
		if ( 0 != Interlocked.Exchange( ref this.invalidationRequested, 0 ) ) {
			this.physicalState!.Invalidate();
		}

		ulong capturedRevision = desired.ChangeRevision;
		if ( !this.RequiresOutput(
			desired,
			this.physicalState!,
			requestedCursorRow,
			requestedCursorColumn
		) ) {
			return;
		}

		CursesRefreshPhysicalState speculative = this.physicalState!.Clone();
		Preparation preparation = new(
			new CursesPreparedRefresh(
				this.terminalSession,
				this.useSynchronizedOutput
			)
		);

		if ( this.scrollRegionResetRequired ) {
			TerminalScreenOperationPlan? recovery = this.planner.PlanScrollRegion(
				0,
				desired.Rows - 1,
				desired.Rows
			);
			if ( !recovery.HasValue || 0 == recovery.Value.ByteCount ) {
				throw new NotSupportedException(
					$"Terminal '{this.planner.Profile.Name}' cannot restore the full-screen scroll region."
				);
			}
			preparation.Prepared.AddPlan( recovery.Value );
			preparation.HasOutput = true;
			preparation.RestoresFullScrollRegion = true;
			speculative.CursorRow = null;
			speculative.CursorColumn = null;
		}

		CursesLineShiftPlan? lineShift = this.lineShiftResolver.Resolve(
			desired,
			speculative.Screen,
			speculative.CurrentStyle,
			speculative.CursorRow,
			speculative.CursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
		if ( lineShift.HasValue ) {
			CursesLineShiftPlan plan = lineShift.Value;
			AddPlanSequence( preparation, plan.Sequence );
			CopyDesiredRange(
				desired,
				speculative.Screen,
				plan.TopRow,
				plan.BottomRow + 1,
				0,
				desired.Columns
			);
			speculative.CurrentStyle = plan.StyleAfter;
			speculative.CursorRow = plan.CursorAfterRow;
			speculative.CursorColumn = plan.CursorAfterColumn;
			preparation.UsesTemporaryScrollRegion =
				plan.Sequence.UsesTemporaryScrollRegion;
			if ( plan.Sequence.UsesTemporaryScrollRegion ) {
				preparation.RestoresFullScrollRegion = true;
			}
		} else {
			this.PrepareRows(
				preparation,
				speculative,
				desired,
				screen.TextWidthProvider
			);
		}

		this.PrepareCursor(
			preparation,
			speculative,
			requestedCursorRow,
			requestedCursorColumn
		);
		if ( !preparation.HasOutput ) {
			return;
		}

		if ( preparation.UsesTemporaryScrollRegion ) {
			this.scrollRegionResetRequired = true;
		}
		await preparation.Prepared.CommitAsync( cancellationToken ).ConfigureAwait( false );
		if ( preparation.RestoresFullScrollRegion ) {
			this.scrollRegionResetRequired = false;
		}
		this.physicalState = speculative;
		desired.MarkCleanThrough( capturedRevision );
	}

	private void PrepareRows(
		Preparation preparation,
		CursesRefreshPhysicalState speculative,
		CursesVirtualScreen desired,
		ICursesTextWidthProvider textWidthProvider
	) {
		bool screenComplete = false;
		for ( int row = 0; row < desired.Rows; row++ ) {
			CursesCharacterShiftPlan? characterShift =
				this.characterShiftResolver.Resolve(
					desired,
					speculative.Screen,
					row,
					speculative.CurrentStyle,
					speculative.CursorRow,
					speculative.CursorColumn
				);
			if ( characterShift.HasValue ) {
				CursesCharacterShiftPlan plan = characterShift.Value;
				AddPlanSequence( preparation, plan.Sequence );
				CopyDesiredRange(
					desired,
					speculative.Screen,
					row,
					row + 1,
					0,
					desired.Columns
				);
				speculative.CurrentStyle = plan.StyleAfter;
				speculative.CursorRow = plan.CursorAfterRow;
				speculative.CursorColumn = plan.CursorAfterColumn;
				continue;
			}

			int column = 0;
			while ( column < desired.Columns ) {
				if ( !NeedsUpdate(
					desired,
					speculative.Screen,
					row,
					column
				) ) {
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
					speculative.Screen,
					row,
					column
				);
				CursesErasePlan? erase = this.eraseResolver.Resolve(
					desired,
					speculative.Screen,
					row,
					start,
					speculative.CurrentStyle,
					speculative.CursorRow,
					speculative.CursorColumn
				);
				if ( erase.HasValue ) {
					CursesErasePlan plan = erase.Value;
					AddPlanSequence( preparation, plan.Sequence );
					switch ( plan.Kind ) {
						case CursesEraseKind.ClearToEndOfLine:
							CopyDesiredRange(
								desired,
								speculative.Screen,
								row,
								row + 1,
								start,
								desired.Columns
							);
							break;
						case CursesEraseKind.ClearToEndOfScreen:
							CopyDesiredRange(
								desired,
								speculative.Screen,
								row,
								row + 1,
								start,
								desired.Columns
							);
							if ( row + 1 < desired.Rows ) {
								CopyDesiredRange(
									desired,
									speculative.Screen,
									row + 1,
									desired.Rows,
									0,
									desired.Columns
								);
							}
							screenComplete = true;
							break;
						case CursesEraseKind.ClearScreen:
							CopyDesiredRange(
								desired,
								speculative.Screen,
								0,
								desired.Rows,
								0,
								desired.Columns
							);
							screenComplete = true;
							break;
						default:
							throw new InvalidOperationException(
								"The erase resolver returned an unknown operation kind."
							);
					}
					speculative.CurrentStyle = plan.StyleAfter;
					speculative.CursorRow = plan.CursorAfterRow;
					speculative.CursorColumn = plan.CursorAfterColumn;
					break;
				}
				this.PrepareSpan(
					preparation,
					speculative,
					desired,
					row,
					start,
					end,
					textWidthProvider
				);
				column = end + 1;
			}
			if ( screenComplete ) {
				break;
			}
		}
	}

	private bool RequiresOutput(
		CursesVirtualScreen desired,
		CursesRefreshPhysicalState state,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		if ( this.scrollRegionResetRequired ) {
			return true;
		}
		if ( state.CursorRow != requestedCursorRow
			|| state.CursorColumn != requestedCursorColumn ) {
			return true;
		}

		for ( int row = 0; row < desired.Rows; row++ ) {
			for ( int column = 0; column < desired.Columns; column++ ) {
				if ( NeedsUpdate(
					desired,
					state.Screen,
					row,
					column
				) ) {
					return true;
				}
			}
		}
		return false;
	}

	private void EnsurePhysicalState( CursesVirtualScreen desired ) {
		ArgumentNullException.ThrowIfNull( desired );
		if ( this.physicalState is not null
			&& this.physicalState.Screen.Columns == desired.Columns
			&& this.physicalState.Screen.Rows == desired.Rows ) {
			return;
		}

		this.physicalState = new CursesRefreshPhysicalState(
			new CursesPhysicalScreenState(
				desired.Columns,
				desired.Rows
			)
		);
	}

	private static bool NeedsUpdate(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int row,
		int column
	) {
		CursesRasterCell? desiredRasterCell = desired.GetRasterCell(
			row,
			column
		);
		if ( desiredRasterCell.HasValue ) {
			ValidateRasterOwnershipForEmission( desiredRasterCell.Value );
		}
		if ( desired.IsDirty( row, column ) ) {
			return true;
		}
		if ( !physical.TryGetCell(
			row,
			column,
			out CursesCell physicalCell
		) ) {
			return true;
		}
		if ( physicalCell != desired[ row, column ] ) {
			return true;
		}
		if ( !Equals(
			physical.GetMetadata( row, column ),
			desired.GetMetadata( row, column )
		) ) {
			return true;
		}
		return !Equals(
			physical.GetRasterCell( row, column ),
			desiredRasterCell
		);
	}

	private static int FindSpanStart(
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

	private static int FindSpanEnd(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int row,
		int column
	) {
		int end = column;
		while ( end + 1 < desired.Columns
			&& NeedsUpdate(
				desired,
				physical,
				row,
				end + 1
			) ) {
			end++;
		}
		while ( end + 1 < desired.Columns
			&& desired[ row, end + 1 ].IsContinuation ) {
			end++;
		}
		return end;
	}

	private static void AddPlanSequence(
		Preparation preparation,
		CursesTerminalPlanSequence sequence
	) {
		ArgumentNullException.ThrowIfNull( preparation );
		ArgumentNullException.ThrowIfNull( sequence );
		foreach ( TerminalScreenOperationPlan plan in sequence.Plans ) {
			preparation.Prepared.AddPlan( plan );
			preparation.HasOutput = true;
		}
	}

	private static void CopyDesiredRange(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int topRow,
		int bottomRowExclusive,
		int startColumn,
		int endColumnExclusive
	) {
		ArgumentNullException.ThrowIfNull( desired );
		ArgumentNullException.ThrowIfNull( physical );
		for ( int row = topRow; row < bottomRowExclusive; row++ ) {
			for ( int column = startColumn; column < endColumnExclusive; column++ ) {
				physical.SetCell(
					row,
					column,
					desired[ row, column ],
					desired.GetMetadata( row, column ),
					desired.GetRasterCell( row, column )
				);
			}
		}
	}

	private void PrepareSpan(
		Preparation preparation,
		CursesRefreshPhysicalState state,
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		ICursesTextWidthProvider textWidthProvider
	) {
		ArgumentNullException.ThrowIfNull( textWidthProvider );
		this.PrepareCursor(
			preparation,
			state,
			row,
			start
		);

		int segmentStart = start;
		while ( segmentStart <= end ) {
			CursesRasterCell? rasterCell = desired.GetRasterCell(
				row,
				segmentStart
			);
			if ( rasterCell.HasValue ) {
				this.PrepareRasterCell(
					preparation,
					state,
					desired,
					row,
					segmentStart,
					rasterCell.Value
				);
				segmentStart++;
				continue;
			}

			CursesStyle style = desired[ row, segmentStart ].Style;
			CursesCellMetadata? metadata = desired.GetMetadata(
				row,
				segmentStart
			);
			int segmentEnd = segmentStart;
			while ( segmentEnd + 1 <= end
				&& !desired.GetRasterCell(
					row,
					segmentEnd + 1
				).HasValue
				&& desired[ row, segmentEnd + 1 ].Style == style
				&& Equals(
					desired.GetMetadata(
						row,
						segmentEnd + 1
					),
					metadata
				) ) {
				segmentEnd++;
			}

			this.PrepareStyleSegment(
				preparation,
				state,
				desired,
				row,
				segmentStart,
				segmentEnd,
				style,
				metadata,
				textWidthProvider
			);
			segmentStart = segmentEnd + 1;
		}
	}

	private void PrepareRasterCell(
		Preparation preparation,
		CursesRefreshPhysicalState state,
		CursesVirtualScreen desired,
		int row,
		int column,
		CursesRasterCell rasterCell
	) {
		ValidateRasterOwnershipForEmission( rasterCell );
		this.PrepareCursor(
			preparation,
			state,
			row,
			column
		);
		this.PrepareStyle(
			preparation,
			state,
			desired[ row, column ].Style
		);
		preparation.Prepared.WriteRasterPlaceholderCell( rasterCell );
		preparation.HasOutput = true;
		state.Screen.SetCell(
			row,
			column,
			desired[ row, column ],
			desired.GetMetadata( row, column ),
			rasterCell
		);

		state.CurrentStyle = null;
		if ( column + 1 < desired.Columns ) {
			state.CursorRow = row;
			state.CursorColumn = column + 1;
		} else {
			state.CursorRow = null;
			state.CursorColumn = null;
		}
	}

	private void PrepareStyleSegment(
		Preparation preparation,
		CursesRefreshPhysicalState state,
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		CursesStyle style,
		CursesCellMetadata? metadata,
		ICursesTextWidthProvider textWidthProvider
	) {
		this.PrepareStyle(
			preparation,
			state,
			style
		);
		this.PrepareCellContent(
			preparation,
			desired,
			row,
			start,
			end,
			metadata?.Hyperlink,
			textWidthProvider
		);
		for ( int column = start; column <= end; column++ ) {
			state.Screen.SetCell(
				row,
				column,
				desired[ row, column ],
				desired.GetMetadata( row, column ),
				rasterCell: null
			);
		}

		if ( end + 1 < desired.Columns ) {
			state.CursorRow = row;
			state.CursorColumn = end + 1;
		} else {
			state.CursorRow = null;
			state.CursorColumn = null;
		}
	}

	private void PrepareCellContent(
		Preparation preparation,
		CursesVirtualScreen desired,
		int row,
		int start,
		int end,
		CursesHyperlink? hyperlink,
		ICursesTextWidthProvider textWidthProvider
	) {
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
				CursesPhysicalLineGlyph resolved =
					this.linePresentationResolver.Resolve(
						cell.LineGlyph.Value,
						textWidthProvider
					);
				content = resolved.Content;
				useAlternateCharacterSet = resolved.UsesAlternateCharacterSet;
			} else {
				content = cell.Content;
			}

			if ( useAlternateCharacterSet != alternateCharacterSetActive ) {
				FlushTextPayload(
					preparation,
					payload,
					hyperlink
				);
				TerminalScreenOperationPlan plan =
					this.linePresentationResolver.PlanAlternateCharacterSet(
						useAlternateCharacterSet
					) ?? throw new NotSupportedException(
						$"Terminal '{this.planner.Profile.Name}' does not provide a safe alternate-character-set transition."
					);
				preparation.Prepared.AddPlan( plan );
				preparation.HasOutput = true;
				alternateCharacterSetActive = useAlternateCharacterSet;
			}
			payload.Append( content );
		}

		FlushTextPayload(
			preparation,
			payload,
			hyperlink
		);
		if ( alternateCharacterSetActive ) {
			TerminalScreenOperationPlan exit =
				this.linePresentationResolver.PlanAlternateCharacterSet(
					enabled: false
				) ?? throw new NotSupportedException(
					$"Terminal '{this.planner.Profile.Name}' does not provide a safe alternate-character-set exit."
				);
			preparation.Prepared.AddPlan( exit );
			preparation.HasOutput = true;
		}
	}

	private static void FlushTextPayload(
		Preparation preparation,
		StringBuilder payload,
		CursesHyperlink? hyperlink
	) {
		ArgumentNullException.ThrowIfNull( preparation );
		ArgumentNullException.ThrowIfNull( payload );
		if ( 0 == payload.Length ) {
			return;
		}

		string text = payload.ToString();
		if ( hyperlink is null ) {
			preparation.Prepared.WriteText( text );
		} else {
			preparation.Prepared.WriteHyperlink(
				text,
				hyperlink
			);
		}
		preparation.HasOutput = true;
		payload.Clear();
	}

	private void PrepareCursor(
		Preparation preparation,
		CursesRefreshPhysicalState state,
		int row,
		int column
	) {
		if ( state.CursorRow == row
			&& state.CursorColumn == column ) {
			return;
		}

		TerminalScreenOperationPlan motion = this.cursorMotionResolver.Resolve(
			state.CursorRow,
			state.CursorColumn,
			row,
			column
		);
		preparation.Prepared.AddPlan( motion );
		preparation.HasOutput = true;
		state.CursorRow = row;
		state.CursorColumn = column;
	}

	private void PrepareStyle(
		Preparation preparation,
		CursesRefreshPhysicalState state,
		CursesStyle requested
	) {
		CursesStyle normalized = this.presentationResolver.Normalize( requested );
		if ( state.CurrentStyle.HasValue
			&& state.CurrentStyle.Value == normalized ) {
			return;
		}

		if ( !state.CurrentStyle.HasValue ) {
			TerminalScreenOperationPlan baseline =
				this.presentationResolver.PlanBaseline()
				?? throw new NotSupportedException(
					$"Terminal '{this.planner.Profile.Name}' does not provide a safe rendition baseline for the requested refresh."
				);
			preparation.Prepared.AddPlan( baseline );
			preparation.HasOutput = true;
			state.CurrentStyle = CursesStyle.Default;
		}

		if ( state.CurrentStyle.Value != normalized ) {
			TerminalScreenOperationPlan transition =
				this.presentationResolver.PlanTransition(
					state.CurrentStyle.Value,
					normalized
				) ?? throw new NotSupportedException(
					$"Terminal '{this.planner.Profile.Name}' does not provide a safe rendition transition for the requested refresh."
				);
			preparation.Prepared.AddPlan( transition );
			preparation.HasOutput = true;
		}
		state.CurrentStyle = normalized;
	}

	private static void ValidateRasterOwnershipForEmission(
		CursesRasterCell rasterCell
	) {
		CursesRasterOwnershipState ownership = rasterCell.Placeholder.OwnershipState;
		if ( CursesRasterOwnershipStatus.Current == ownership.Status ) {
			return;
		}
		throw new InvalidOperationException(
			$"Retained raster ownership is {ownership.Status} ({ownership.LossReason}) and cannot be emitted."
		);
	}

	private void InvalidateKnownState() {
		this.physicalState?.Invalidate();
		Interlocked.Exchange( ref this.invalidationRequested, 1 );
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

	private sealed class Preparation {
		internal Preparation( CursesPreparedRefresh prepared ) {
			ArgumentNullException.ThrowIfNull( prepared );
			this.Prepared = prepared;
		}

		internal CursesPreparedRefresh Prepared {
			get;
		}

		internal bool HasOutput {
			get;
			set;
		}

		internal bool UsesTemporaryScrollRegion {
			get;
			set;
		}

		internal bool RestoresFullScrollRegion {
			get;
			set;
		}
	}
}
