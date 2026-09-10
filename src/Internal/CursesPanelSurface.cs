namespace Icod.DCurses.Internal;

/// <summary>
/// Owns one panel-private logical surface while reusing the ordinary screen/window semantics.
/// </summary>
internal sealed class CursesPanelSurface {
	private readonly CursesScreen backingScreen;

	/// <summary>Initializes an independent retained surface for one panel.</summary>
	/// <param name="rows">The positive panel height.</param>
	/// <param name="columns">The positive panel width.</param>
	/// <param name="textWidthProvider">The destination screen's display-width policy.</param>
	internal CursesPanelSurface(
		int rows,
		int columns,
		ICursesTextWidthProvider textWidthProvider
	) {
		if ( 0 >= rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		ArgumentNullException.ThrowIfNull( textWidthProvider );

		backingScreen = new CursesScreen(
			columns,
			rows,
			textWidthProvider
		);
	}

	/// <summary>Gets the retained content window.</summary>
	internal CursesWindow ContentWindow => backingScreen.StandardWindow;

	/// <summary>Gets the backing virtual screen.</summary>
	internal CursesVirtualScreen VirtualScreen => backingScreen.VirtualScreen;

	/// <summary>Gets the panel height.</summary>
	internal int Rows => backingScreen.Rows;

	/// <summary>Gets the panel width.</summary>
	internal int Columns => backingScreen.Columns;
}
