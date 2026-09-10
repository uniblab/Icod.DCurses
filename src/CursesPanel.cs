namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>
/// Represents one independent retained surface positioned over a destination logical screen.
/// </summary>
/// <remarks>
/// Panel content is independent of the destination screen and of other panels. Editing the
/// <see cref="ContentWindow"/> therefore does not directly modify cells on the destination
/// <see cref="CursesScreen"/>. Logical composition projects visible panels onto that destination
/// in deterministic z-order.
/// </remarks>
public sealed class CursesPanel {
	private readonly CursesScreen owner;
	private readonly CursesPanelSurface surface;
	private CursesPanelTransparency transparency;

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

	/// <summary>Gets or sets how blank cells in this panel participate in logical composition.</summary>
	public CursesPanelTransparency Transparency {
		get => transparency;
		set {
			if ( value is not CursesPanelTransparency.Opaque
				and not CursesPanelTransparency.BlankCellsTransparent ) {
				throw new ArgumentOutOfRangeException( nameof( value ) );
			}

			transparency = value;
		}
	}

	/// <summary>Makes this panel visible without changing its remembered z-order position.</summary>
	public void Show() {
		if ( IsVisible ) {
			return;
		}

		IsVisible = true;
	}

	/// <summary>Hides this panel while retaining its content, position, and z-order membership.</summary>
	public void Hide() {
		if ( !IsVisible ) {
			return;
		}

		IsVisible = false;
	}

	/// <summary>Moves this panel to a new destination-screen origin without changing its content or z-order.</summary>
	/// <param name="row">The new zero-based destination row.</param>
	/// <param name="column">The new zero-based destination column.</param>
	public void MoveTo(
		int row,
		int column
	) {
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			Rows,
			Columns,
			owner.Rows,
			owner.Columns
		);

		if ( row == Row
			&& column == Column ) {
			return;
		}

		Row = row;
		Column = column;
	}

	/// <summary>Moves this panel to the top of its owning screen's panel order.</summary>
	public void MoveToTop() {
		owner.MovePanelToTop( this );
	}

	/// <summary>Moves this panel to the bottom of its owning screen's panel order.</summary>
	public void MoveToBottom() {
		owner.MovePanelToBottom( this );
	}

	/// <summary>Moves this panel immediately above another panel on the same screen.</summary>
	/// <param name="sibling">The panel which should immediately precede this panel.</param>
	public void MoveAbove( CursesPanel sibling ) {
		ArgumentNullException.ThrowIfNull( sibling );
		owner.MovePanelAbove(
			this,
			sibling
		);
	}

	/// <summary>Moves this panel immediately below another panel on the same screen.</summary>
	/// <param name="sibling">The panel which should immediately follow this panel.</param>
	public void MoveBelow( CursesPanel sibling ) {
		ArgumentNullException.ThrowIfNull( sibling );
		owner.MovePanelBelow(
			this,
			sibling
		);
	}

	/// <summary>Gets the destination screen which owns this panel.</summary>
	internal CursesScreen Owner => owner;

	/// <summary>Gets the panel-private virtual screen for logical composition.</summary>
	internal CursesVirtualScreen VirtualScreen => surface.VirtualScreen;
}
