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

namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>
/// Owns the logical terminal frame and the standard screen window projected over that frame.
/// </summary>
public sealed class CursesScreen {
	private readonly CursesPanelOrder<CursesPanel> panelOrder = new();
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
		virtualScreen = CreateOwnedVirtualScreen(
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

	/// <summary>Gets the complete logical-screen rectangle.</summary>
	public CursesRectangle Bounds => new(
		0,
		0,
		Rows,
		Columns
	);

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

	/// <summary>Creates an independent retained panel positioned over this logical screen.</summary>
	/// <param name="row">Zero-based screen row of the panel origin.</param>
	/// <param name="column">Zero-based screen column of the panel origin.</param>
	/// <param name="rows">The positive panel height.</param>
	/// <param name="columns">The positive panel width.</param>
	/// <returns>The new visible panel, initially at the top of the panel order.</returns>
	/// <remarks>
	/// Editing the panel's content window does not directly modify this screen's base logical cells.
	/// Panel projection is performed by the 1.2 logical composition layer.
	/// </remarks>
	public CursesPanel CreatePanel(
		int row,
		int column,
		int rows,
		int columns
	) {
		ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			Rows,
			Columns
		);

		CursesPanel panel = new(
			this,
			row,
			column,
			rows,
			columns
		);
		panelOrder.Add( panel );
		return panel;
	}

	/// <summary>Gets whether this screen owns at least one panel without allocating an order snapshot.</summary>
	internal bool HasPanels => 0 < panelOrder.Count;

	/// <summary>Creates a stable bottom-to-top snapshot of this screen's complete panel order.</summary>
	/// <returns>A new array containing visible and hidden panels in remembered z-order.</returns>
	internal CursesPanel[] SnapshotPanelsBottomToTop() {
		return panelOrder.SnapshotBottomToTop();
	}

	/// <summary>Permanently removes one owned panel from this screen's composition order.</summary>
	/// <param name="panel">The owned panel to remove.</param>
	internal void RemovePanel( CursesPanel panel ) {
		ArgumentNullException.ThrowIfNull( panel );
		if ( !ReferenceEquals(
			panel.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The panel belongs to another screen.",
				nameof( panel )
			);
		}
		if ( !panelOrder.Remove( panel ) ) {
			throw new ArgumentException(
				"The panel is not attached to this screen.",
				nameof( panel )
			);
		}
	}

	/// <summary>Moves one owned panel to the top of the remembered panel order.</summary>
	/// <param name="panel">The owned panel to move.</param>
	internal void MovePanelToTop( CursesPanel panel ) {
		ArgumentNullException.ThrowIfNull( panel );
		ValidateOwnedPanel(
			panel,
			nameof( panel )
		);
		panelOrder.MoveToTop( panel );
	}

	/// <summary>Moves one owned panel to the bottom of the remembered panel order.</summary>
	/// <param name="panel">The owned panel to move.</param>
	internal void MovePanelToBottom( CursesPanel panel ) {
		ArgumentNullException.ThrowIfNull( panel );
		ValidateOwnedPanel(
			panel,
			nameof( panel )
		);
		panelOrder.MoveToBottom( panel );
	}

	/// <summary>Moves one owned panel immediately above another owned panel.</summary>
	/// <param name="panel">The owned panel to move.</param>
	/// <param name="sibling">The owned sibling which should immediately precede it.</param>
	internal void MovePanelAbove(
		CursesPanel panel,
		CursesPanel sibling
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );
		ValidateOwnedPanel(
			panel,
			nameof( panel )
		);
		ValidateOwnedPanel(
			sibling,
			nameof( sibling )
		);
		panelOrder.MoveAbove(
			panel,
			sibling
		);
	}

	/// <summary>Moves one owned panel immediately below another owned panel.</summary>
	/// <param name="panel">The owned panel to move.</param>
	/// <param name="sibling">The owned sibling which should immediately follow it.</param>
	internal void MovePanelBelow(
		CursesPanel panel,
		CursesPanel sibling
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );
		ValidateOwnedPanel(
			panel,
			nameof( panel )
		);
		ValidateOwnedPanel(
			sibling,
			nameof( sibling )
		);
		panelOrder.MoveBelow(
			panel,
			sibling
		);
	}

	/// <summary>
	/// Resizes the logical screen and optionally preserves cells in the overlapping upper-left region.
	/// </summary>
	/// <param name="columns">The new positive column count.</param>
	/// <param name="rows">The new positive row count.</param>
	/// <param name="preserveContents">Whether overlapping logical cells and semantic metadata should be retained.</param>
	public void Resize(
		int columns,
		int rows,
		bool preserveContents = true ) {
		if ( columns == Columns
			&& rows == Rows
			&& preserveContents ) {
			return;
		}

		CursesVirtualScreen replacement = CreateOwnedVirtualScreen(
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
					CursesCellMetadata? metadata = virtualScreen.GetMetadata(
						row,
						column
					);
					if ( metadata is not null ) {
						replacement.SetMetadata(
							row,
							column,
							metadata
						);
					}
				}
			}

			CursesCellFootprint.Repair( replacement );
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

	private void ValidateOwnedPanel(
		CursesPanel panel,
		string parameterName
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentException.ThrowIfNullOrEmpty( parameterName );

		if ( !ReferenceEquals(
			panel.Owner,
			this
		) || panel.IsDisposed ) {
			throw new ArgumentException(
				"The panel does not belong to this screen's active panel set.",
				parameterName
			);
		}
	}

	private static CursesVirtualScreen CreateOwnedVirtualScreen(
		int columns,
		int rows ) {
		CursesVirtualScreen result = new(
			columns,
			rows
		) {
			RepairWideFootprintsOnReplacement = true
		};
		return result;
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
