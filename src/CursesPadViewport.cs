namespace Icod.DCurses;

/// <summary>
/// Stores one independently pannable projection from a <see cref="CursesPad"/> into a destination
/// <see cref="CursesWindow"/>.
/// </summary>
/// <remarks>
/// A viewport owns logical projection state only. It does not own terminal output or physical refresh.
/// Multiple viewports can reference the same pad and pan independently.
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
	/// </remarks>
	public void Present() {
		pad.PresentTo(
			destination,
			padRow,
			padColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn
		);
	}
}
