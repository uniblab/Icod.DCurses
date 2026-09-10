namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>
/// Represents one independent retained surface positioned over a destination logical screen.
/// </summary>
/// <remarks>
/// Panel content is independent of the destination screen and of other panels. Editing the
/// <see cref="ContentWindow"/> therefore does not directly modify cells on the destination
/// <see cref="CursesScreen"/>. Later 1.2 composition stages project visible panels onto that
/// destination in deterministic z-order.
/// </remarks>
public sealed class CursesPanel {
	private readonly CursesScreen owner;
	private readonly CursesPanelSurface surface;

	/// <summary>Initializes one screen-owned independent retained panel.</summary>
	/// <param name="owner">The destination logical screen.</param>
	/// <param name="row">The zero-based destination row of the panel origin.</param>
	/// <param name="column">The zero-based destination column of the panel origin.</param>
	/// <param name="rows">The positive panel height.</param>
	/// <param name="columns">The positive panel width.</param>
	internal CursesPanel(
		CursesScreen owner,
		int row,
		int column,
		int rows,
		int columns
	) {
		ArgumentNullException.ThrowIfNull( owner );
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			owner.Rows,
			owner.Columns
		);

		this.owner = owner;
		Row = row;
		Column = column;
		surface = new CursesPanelSurface(
			rows,
			columns,
			owner.TextWidthProvider
		);
		IsVisible = true;
	}

	/// <summary>Gets the retained content window owned only by this panel.</summary>
	public CursesWindow ContentWindow => surface.ContentWindow;

	/// <summary>Gets the zero-based destination row of the panel origin.</summary>
	public int Row {
		get;
		private set;
	}

	/// <summary>Gets the zero-based destination column of the panel origin.</summary>
	public int Column {
		get;
		private set;
	}

	/// <summary>Gets the panel height in terminal cells.</summary>
	public int Rows => surface.Rows;

	/// <summary>Gets the panel width in terminal cells.</summary>
	public int Columns => surface.Columns;

	/// <summary>Gets whether this panel currently participates in logical composition.</summary>
	public bool IsVisible {
		get;
		private set;
	}

	/// <summary>Gets the destination screen which owns this panel.</summary>
	internal CursesScreen Owner => owner;

	/// <summary>Gets the panel-private virtual screen for logical composition.</summary>
	internal CursesVirtualScreen VirtualScreen => surface.VirtualScreen;
}
