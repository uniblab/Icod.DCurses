namespace Icod.DCurses;

/// <summary>Window-local geometric line and border drawing operations.</summary>
public sealed partial class CursesWindow {
	/// <summary>Draws one horizontal line using a one-column logical cell.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="length">The positive number of columns to draw.</param>
	/// <param name="cell">The one-column non-continuation cell used for the line.</param>
	/// <remarks>The cursor is preserved.</remarks>
	public void DrawHorizontalLine(
		int row,
		int column,
		int length,
		CursesCell cell
	) {
		ValidateLineLength( length );
		ValidateRegion(
			row,
			column,
			1,
			length
		);
		ValidateFillCell( cell );

		FillRegion(
			row,
			column,
			1,
			length,
			cell
		);
	}

	/// <summary>Draws one vertical line using a one-column logical cell.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <param name="length">The positive number of rows to draw.</param>
	/// <param name="cell">The one-column non-continuation cell used for the line.</param>
	/// <remarks>The cursor is preserved.</remarks>
	public void DrawVerticalLine(
		int row,
		int column,
		int length,
		CursesCell cell
	) {
		ValidateLineLength( length );
		ValidateRegion(
			row,
			column,
			length,
			1
		);
		ValidateFillCell( cell );

		FillRegion(
			row,
			column,
			length,
			1,
			cell
		);
	}

	/// <summary>Draws a box-shaped border around the complete window.</summary>
	/// <param name="horizontal">The one-column cell used for top and bottom edges.</param>
	/// <param name="vertical">The one-column cell used for left and right edges.</param>
	/// <param name="topLeft">The upper-left corner cell.</param>
	/// <param name="topRight">The upper-right corner cell.</param>
	/// <param name="bottomLeft">The lower-left corner cell.</param>
	/// <param name="bottomRight">The lower-right corner cell.</param>
	/// <remarks>
	/// The window must be at least two rows by two columns. The cursor is preserved. Glyph selection is
	/// deliberately caller-owned in 0.4; capability-aware semantic line-drawing policy remains deferred to
	/// the 0.6 presentation tranche.
	/// </remarks>
	public void DrawBorder(
		CursesCell horizontal,
		CursesCell vertical,
		CursesCell topLeft,
		CursesCell topRight,
		CursesCell bottomLeft,
		CursesCell bottomRight
	) {
		if ( Rows < 2 || Columns < 2 ) {
			throw new InvalidOperationException(
				"A window must be at least two rows by two columns to draw a border."
			);
		}

		ValidateFillCell( horizontal );
		ValidateFillCell( vertical );
		ValidateFillCell( topLeft );
		ValidateFillCell( topRight );
		ValidateFillCell( bottomLeft );
		ValidateFillCell( bottomRight );

		if ( 2 < Columns ) {
			DrawHorizontalLine(
				0,
				1,
				Columns - 2,
				horizontal
			);
			DrawHorizontalLine(
				Rows - 1,
				1,
				Columns - 2,
				horizontal
			);
		}
		if ( 2 < Rows ) {
			DrawVerticalLine(
				1,
				0,
				Rows - 2,
				vertical
			);
			DrawVerticalLine(
				1,
				Columns - 1,
				Rows - 2,
				vertical
			);
		}

		SetCellIfVisible(
			0,
			0,
			topLeft
		);
		SetCellIfVisible(
			0,
			Columns - 1,
			topRight
		);
		SetCellIfVisible(
			Rows - 1,
			0,
			bottomLeft
		);
		SetCellIfVisible(
			Rows - 1,
			Columns - 1,
			bottomRight
		);
	}

	private static void ValidateLineLength( int length ) {
		if ( 0 >= length ) {
			throw new ArgumentOutOfRangeException(
				nameof( length ),
				length,
				"The line length must be positive."
			);
		}
	}
}
