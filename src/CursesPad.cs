namespace Icod.DCurses;

/// <summary>
/// Owns a large off-screen logical cell surface whose contents can be edited through a normal
/// <see cref="CursesWindow"/>.
/// </summary>
/// <remarks>
/// A pad does not own a terminal session or perform physical terminal output. Its content window reuses
/// the standard DCurses Unicode, cell, editing, composition, drawing, and damage semantics. Selected pad
/// rectangles can be projected into ordinary destination windows by later viewport APIs.
/// </remarks>
public sealed class CursesPad {
	private readonly CursesScreen backingScreen;

	/// <summary>Initializes a blank off-screen pad.</summary>
	/// <param name="columns">The positive number of pad columns.</param>
	/// <param name="rows">The positive number of pad rows.</param>
	/// <param name="textWidthProvider">Optional terminal display-width policy used by the pad content.</param>
	public CursesPad(
		int columns,
		int rows,
		ICursesTextWidthProvider? textWidthProvider = null
	) {
		backingScreen = new CursesScreen(
			columns,
			rows,
			textWidthProvider
		);
		ContentWindow = backingScreen.StandardWindow;
	}

	/// <summary>Gets the number of columns in the off-screen pad.</summary>
	public int Columns => backingScreen.Columns;

	/// <summary>Gets the number of rows in the off-screen pad.</summary>
	public int Rows => backingScreen.Rows;

	/// <summary>Gets the text-width policy used by the pad content window.</summary>
	public ICursesTextWidthProvider TextWidthProvider => backingScreen.TextWidthProvider;

	/// <summary>
	/// Gets the standard content window covering the complete off-screen pad.
	/// </summary>
	/// <remarks>
	/// Cursor state is local to this window. Callers can use the established <see cref="CursesWindow"/>
	/// APIs directly, including <see cref="CursesWindow.CreateSubwindow(int,int,int,int)"/> when a shared
	/// derived view into the pad is needed.
	/// </remarks>
	public CursesWindow ContentWindow {
		get;
	}
}
