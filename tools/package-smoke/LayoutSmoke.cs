/*
	Icod.DCurses.PackageSmoke
	Package-only consumer validation for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Runtime.CompilerServices;
using Icod.DCurses;

internal static class LayoutSmoke {
	[ModuleInitializer]
	internal static void VerifyLayoutSurface() {
		CursesRectangle outer = new(
			1,
			2,
			10,
			20
		);
		CursesInsets insets = new(
			1,
			2,
			1,
			2
		);
		CursesRectangle inner = outer.Inset( insets );
		if ( inner != new CursesRectangle( 2, 4, 8, 16 )
			|| 4 != insets.Horizontal
			|| 2 != insets.Vertical ) {
			throw new InvalidOperationException(
				"DCurses package-only immutable geometry surface failed validation."
			);
		}

		CursesLayout.SplitTop(
			inner,
			2,
			out CursesRectangle header,
			out CursesRectangle belowHeader
		);
		CursesLayout.Dock(
			belowHeader,
			CursesDockEdge.Left,
			4,
			out CursesRectangle sidebar,
			out CursesRectangle body
		);
		CursesLayout.SplitColumnsProportional(
			body,
			1,
			3,
			out CursesRectangle firstBody,
			out CursesRectangle secondBody
		);
		CursesRectangle clipped = CursesLayout.Clip(
			new CursesRectangle( 0, 0, 4, 8 ),
			outer
		);
		if ( header != new CursesRectangle( 2, 4, 2, 16 )
			|| sidebar != new CursesRectangle( 4, 4, 6, 4 )
			|| firstBody.Columns + secondBody.Columns != body.Columns
			|| clipped != new CursesRectangle( 1, 2, 3, 6 ) ) {
			throw new InvalidOperationException(
				"DCurses package-only layout helper surface failed validation."
			);
		}

		CursesScreen logical = new(
			30,
			12
		);
		if ( logical.Bounds != new CursesRectangle( 0, 0, 12, 30 ) ) {
			throw new InvalidOperationException(
				"DCurses package-only screen bounds surface failed validation."
			);
		}

		CursesWindow window = logical.CreateWindow(
			0,
			0,
			1,
			1
		);
		CursesRectangle windowBounds = new(
			1,
			2,
			4,
			10
		);
		window.SetBounds( windowBounds );
		if ( windowBounds != window.Bounds ) {
			throw new InvalidOperationException(
				"DCurses package-only window bounds application failed validation."
			);
		}

		using CursesPanel panel = logical.CreatePanel(
			2,
			3,
			2,
			6
		);
		panel.ContentWindow.Write( "A\u754CB" );
		panel.Resize(
			3,
			8
		);
		if ( panel.Bounds != new CursesRectangle( 2, 3, 3, 8 )
			|| "A" != panel.ContentWindow.GetCell( 0, 0 ).Content
			|| "\u754C" != panel.ContentWindow.GetCell( 0, 1 ).Content
			|| !panel.ContentWindow.GetCell( 0, 2 ).IsContinuation
			|| "B" != panel.ContentWindow.GetCell( 0, 3 ).Content ) {
			throw new InvalidOperationException(
				"DCurses package-only retained panel resize failed validation."
			);
		}

		CursesRectangle panelBounds = new(
			4,
			5,
			4,
			10
		);
		panel.SetBounds( panelBounds );
		if ( panelBounds != panel.Bounds
			|| "A" != panel.ContentWindow.GetCell( 0, 0 ).Content ) {
			throw new InvalidOperationException(
				"DCurses package-only panel bounds application failed validation."
			);
		}
	}
}
