namespace Icod.DCurses;

/// <summary>
/// Stores one independently pannable projection from a <see cref="CursesPad"/> into a destination
/// <see cref="CursesWindow"/>.
/// </summary>
/// <remarks>
/// A viewport owns logical projection state only. It does not own terminal output or physical refresh.
/// Multiple viewports can reference the same pad, pan independently, and observe pad changes without
/// acknowledging those changes on behalf of one another.
/// </remarks>
public sealed class CursesPadViewport {
	private readonly CursesPad pad;
	private readonly CursesWindow destination;
	private readonly int rows;
	private readonly int columns;
	private readonly int destinationRow;
	private readonly int destinationColumn;
	private int padRow;
	private int padColumn;
	private bool hasPresented;
	private int presentedPadRow;
	private int presentedPadColumn;
	private ulong lastCheckedPadRevision;
	private ulong[]? presentedCellRevisions;

	internal CursesPadViewport(
		CursesPad pad,
		CursesWindow destination,
		int padRow,
		int padColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn
	) {
		ArgumentNullException.ThrowIfNull( pad );
		ArgumentNullException.ThrowIfNull( destination );

		CursesScreen.ValidateWindowRectangle(
			padRow,
			padColumn,
			rows,
			columns,
			pad.Rows,
			pad.Columns
		);
		CursesScreen.ValidateWindowRectangle(
			destinationRow,
			destinationColumn,
			rows,
			columns,
			destination.Rows,
			destination.Columns
		);

		this.pad = pad;
		this.destination = destination;
		this.padRow = padRow;
		this.padColumn = padColumn;
		this.rows = rows;
		this.columns = columns;
		this.destinationRow = destinationRow;
		this.destinationColumn = destinationColumn;
	}

	/// <summary>Gets the zero-based first pad row currently projected by this viewport.</summary>
	public int PadRow => padRow;

	/// <summary>Gets the zero-based first pad column currently projected by this viewport.</summary>
	public int PadColumn => padColumn;

	/// <summary>Gets the viewport height.</summary>
	public int Rows => rows;

	/// <summary>Gets the viewport width.</summary>
	public int Columns => columns;

	/// <summary>Gets the zero-based first destination row.</summary>
	public int DestinationRow => destinationRow;

	/// <summary>Gets the zero-based first destination column.</summary>
	public int DestinationColumn => destinationColumn;

	/// <summary>
	/// Gets whether the current pad source rectangle changed or moved since this viewport last presented it.
	/// </summary>
	/// <remarks>
	/// This property tracks pad-local content and explicit touch/invalidation independently for this viewport.
	/// It does not inspect unrelated mutations in the destination window. <see cref="Present"/> remains an
	/// authoritative projection and will restore destination cells even when this property is
	/// <see langword="false"/>.
	/// </remarks>
	public bool HasVisiblePadChanges {
		get {
			if ( !hasPresented
				|| presentedPadRow != padRow
				|| presentedPadColumn != padColumn ) {
				return true;
			}

			ulong currentRevision = pad.ChangeRevision;
			if ( currentRevision == lastCheckedPadRevision ) {
				return false;
			}

			if ( VisibleCellRevisionsChanged() ) {
				return true;
			}

			lastCheckedPadRevision = currentRevision;
			return false;
		}
	}

	/// <summary>Changes the absolute pad source origin without presenting it.</summary>
	/// <param name="row">The new zero-based first pad row.</param>
	/// <param name="column">The new zero-based first pad column.</param>
	public void SetSource(
		int row,
		int column
	) {
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			pad.Rows,
			pad.Columns
		);

		padRow = row;
		padColumn = column;
	}

	/// <summary>Pans the pad source origin by a relative number of rows and columns.</summary>
	/// <param name="rowDelta">Signed row delta.</param>
	/// <param name="columnDelta">Signed column delta.</param>
	public void PanBy(
		int rowDelta,
		int columnDelta
	) {
		long nextRow = (long)padRow + rowDelta;
		long nextColumn = (long)padColumn + columnDelta;
		long maximumRow = pad.Rows - rows;
		long maximumColumn = pad.Columns - columns;

		if ( 0 > nextRow || nextRow > maximumRow ) {
			throw new ArgumentOutOfRangeException(
				nameof( rowDelta ),
				rowDelta,
				"The requested pan would move the viewport outside the pad rows."
			);
		}
		if ( 0 > nextColumn || nextColumn > maximumColumn ) {
			throw new ArgumentOutOfRangeException(
				nameof( columnDelta ),
				columnDelta,
				"The requested pan would move the viewport outside the pad columns."
			);
		}

		padRow = (int)nextRow;
		padColumn = (int)nextColumn;
	}

	/// <summary>Projects the viewport's current pad source rectangle into its destination window.</summary>
	/// <remarks>
	/// The destination rectangle is revalidated by <see cref="CursesPad.PresentTo"/> at each presentation,
	/// so a destination window resized after viewport construction cannot silently receive a clipped copy.
	/// Panning marks the complete destination viewport dirty even when newly exposed cells compare equal to
	/// the prior contents. Pad-local touch/invalidation is propagated in changed row spans. Presentation
	/// itself remains authoritative so destination drift is repaired even when the pad did not change.
	/// </remarks>
	public void Present() {
		bool sourcePositionChanged = !hasPresented
			|| presentedPadRow != padRow
			|| presentedPadColumn != padColumn;
		bool[]? changedCells = null;

		if ( !sourcePositionChanged
			&& pad.ChangeRevision != lastCheckedPadRevision ) {
			changedCells = CaptureChangedCells();
		}

		pad.PresentTo(
			destination,
			padRow,
			padColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn
		);

		if ( sourcePositionChanged ) {
			destination.TouchRegion(
				destinationRow,
				destinationColumn,
				rows,
				columns
			);
		} else if ( null != changedCells ) {
			TouchChangedDestinationSpans( changedCells );
		}

		CapturePresentedRevisions();
	}

	private bool[] CaptureChangedCells() {
		bool[] result = new bool[ checked( rows * columns ) ];
		if ( null == presentedCellRevisions ) {
			Array.Fill(
				result,
				true
			);
			return result;
		}

		int offset = 0;
		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				result[ offset ] = presentedCellRevisions[ offset ]
					!= pad.GetCellChangeRevision(
						padRow + rowOffset,
						padColumn + columnOffset
					);
				offset++;
			}
		}
		return result;
	}

	private void CapturePresentedRevisions() {
		presentedCellRevisions ??= new ulong[ checked( rows * columns ) ];

		int offset = 0;
		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				presentedCellRevisions[ offset ] = pad.GetCellChangeRevision(
					padRow + rowOffset,
					padColumn + columnOffset
				);
				offset++;
			}
		}

		hasPresented = true;
		presentedPadRow = padRow;
		presentedPadColumn = padColumn;
		lastCheckedPadRevision = pad.ChangeRevision;
	}

	private bool VisibleCellRevisionsChanged() {
		if ( null == presentedCellRevisions ) {
			return true;
		}

		int offset = 0;
		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				if ( presentedCellRevisions[ offset ]
					!= pad.GetCellChangeRevision(
						padRow + rowOffset,
						padColumn + columnOffset
					) ) {
					return true;
				}
				offset++;
			}
		}
		return false;
	}

	private void TouchChangedDestinationSpans( bool[] changedCells ) {
		ArgumentNullException.ThrowIfNull( changedCells );
		if ( changedCells.Length != rows * columns ) {
			throw new ArgumentException(
				"The changed-cell map must match the viewport geometry.",
				nameof( changedCells )
			);
		}

		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			int rowStart = rowOffset * columns;
			int column = 0;
			while ( column < columns ) {
				while ( column < columns
					&& !changedCells[ rowStart + column ] ) {
					column++;
				}
				if ( column >= columns ) {
					break;
				}

				int startColumn = column;
				while ( column < columns
					&& changedCells[ rowStart + column ] ) {
					column++;
				}

				destination.TouchRegion(
					destinationRow + rowOffset,
					destinationColumn + startColumn,
					1,
					column - startColumn
				);
			}
		}
	}
}
