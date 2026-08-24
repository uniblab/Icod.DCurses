namespace Icod.DCurses;

/// <summary>
/// Owns the logical terminal frame and the standard screen window projected over that frame.
/// </summary>
public sealed class CursesScreen {
	private CursesVirtualScreen virtualScreen;

	/// <summary>Initializes a logical screen with a standard window covering the complete frame.</summary>
	/// <param name="columns">The positive number of terminal columns.</param>
	/// <param name="rows">The positive number of terminal rows.</param>
	/// <param name="textWidthProvider">Optional terminal display-width policy.</param>
	public CursesScreen(
		int columns,
		int rows,
		ICursesTextWidthProvider? textWidthProvider = null ) {
		TextWidthProvider = textWidthProvider
			?? UnicodeCursesTextWidthProvider.Instance;
		virtualScreen = new CursesVirtualScreen(
			columns,
			rows
		);
		StandardWindow = CursesWindow.CreateStandard( this );
	}

	/// <summary>Occurs after the logical screen dimensions change.</summary>
	public event EventHandler<CursesScreenResizedEventArgs>? Resized;

	/// <summary>Gets the display-width policy used by windows owned by this screen.</summary>
	public ICursesTextWidthProvider TextWidthProvider {
		get;
	}

	/// <summary>Gets the current number of columns.</summary>
	public int Columns => virtualScreen.Columns;

	/// <summary>Gets the current number of rows.</summary>
	public int Rows => virtualScreen.Rows;

	/// <summary>Gets the application-requested virtual-screen image.</summary>
	public CursesVirtualScreen VirtualScreen => virtualScreen;

	/// <summary>Gets the standard window covering the entire logical screen.</summary>
	public CursesWindow StandardWindow {
		get;
	}

	/// <summary>Creates a rectangular window projected directly onto the logical screen.</summary>
	/// <param name="row">Zero-based screen row of the window origin.</param>
	/// <param name="column">Zero-based screen column of the window origin.</param>
	/// <param name="rows">The positive window height.</param>
	/// <param name="columns">The positive window width.</param>
	/// <returns>The shared logical-screen view.</returns>
	public CursesWindow CreateWindow(
		int row,
		int column,
		int rows,
		int columns ) {
		ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			Rows,
			Columns
		);

		return CursesWindow.CreateRootView(
			this,
			row,
			column,
			rows,
			columns
		);
	}

	/// <summary>
	/// Resizes the logical screen and optionally preserves cells in the overlapping upper-left region.
	/// </summary>
	/// <param name="columns">The new positive column count.</param>
	/// <param name="rows">The new positive row count.</param>
	/// <param name="preserveContents">Whether overlapping logical cells should be retained.</param>
	public void Resize(
		int columns,
		int rows,
		bool preserveContents = true ) {
		CursesVirtualScreen replacement = new(
			columns,
			rows
		);

		if ( preserveContents ) {
			int copyRows = Math.Min(
				Rows,
				rows
			);
			int copyColumns = Math.Min(
				Columns,
				columns
			);

			for ( int row = 0; row < copyRows; row++ ) {
				for ( int column = 0; column < copyColumns; column++ ) {
					replacement[ row, column ] = virtualScreen[ row, column ];
				}
			}
		}

		if ( columns == Columns
			&& rows == Rows
			&& preserveContents ) {
			return;
		}

		int oldColumns = Columns;
		int oldRows = Rows;
		virtualScreen = replacement;
		StandardWindow.HandleScreenResize();

		if ( oldColumns != columns || oldRows != rows ) {
			Resized?.Invoke(
				this,
				new CursesScreenResizedEventArgs(
					oldColumns,
					oldRows,
					columns,
					rows
				)
			);
		}
	}

	/// <summary>Validates that a window rectangle fits inside its containing surface.</summary>
	/// <param name="row">The zero-based origin row.</param>
	/// <param name="column">The zero-based origin column.</param>
	/// <param name="rows">The positive window height.</param>
	/// <param name="columns">The positive window width.</param>
	/// <param name="containingRows">The containing surface height.</param>
	/// <param name="containingColumns">The containing surface width.</param>
	internal static void ValidateWindowRectangle(
		int row,
		int column,
		int rows,
		int columns,
		int containingRows,
		int containingColumns ) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 >= rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( row > containingRows - rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The window extends below its containing surface."
			);
		}
		if ( column > containingColumns - columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( columns ),
				columns,
				"The window extends beyond its containing surface."
			);
		}
	}
}

/// <summary>Reports one logical-screen resize.</summary>
public sealed class CursesScreenResizedEventArgs
	: EventArgs {
	/// <summary>Initializes logical-screen resize event data.</summary>
	/// <param name="oldColumns">The previous column count.</param>
	/// <param name="oldRows">The previous row count.</param>
	/// <param name="columns">The new column count.</param>
	/// <param name="rows">The new row count.</param>
	internal CursesScreenResizedEventArgs(
		int oldColumns,
		int oldRows,
		int columns,
		int rows ) {
		OldColumns = oldColumns;
		OldRows = oldRows;
		Columns = columns;
		Rows = rows;
	}

	/// <summary>Gets the previous number of columns.</summary>
	public int OldColumns {
		get;
	}

	/// <summary>Gets the previous number of rows.</summary>
	public int OldRows {
		get;
	}

	/// <summary>Gets the new number of columns.</summary>
	public int Columns {
		get;
	}

	/// <summary>Gets the new number of rows.</summary>
	public int Rows {
		get;
	}
}
