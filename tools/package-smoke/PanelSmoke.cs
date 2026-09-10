namespace Icod.DCurses.PackageSmoke;

using System.Runtime.CompilerServices;
using Icod.DCurses;

internal static class PanelSmoke {
	[ModuleInitializer]
	internal static void VerifyPanelSurface() {
		CursesScreen screen = new(
			40,
			12
		);
		using CursesPanel panel = screen.CreatePanel(
			2,
			3,
			4,
			12
		);
		if ( 2 != panel.Row
			|| 3 != panel.Column
			|| 4 != panel.Rows
			|| 12 != panel.Columns
			|| !panel.IsVisible ) {
			throw new InvalidOperationException(
				"DCurses package-only panel creation surface failed validation."
			);
		}

		panel.ContentWindow.Write( "retained" );
		panel.Hide();
		if ( panel.IsVisible ) {
			throw new InvalidOperationException(
				"DCurses package-only panel hide surface failed validation."
			);
		}
		panel.Show();
		panel.MoveTo(
			4,
			8
		);
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.MoveToTop();
		panel.MoveToBottom();
		if ( 4 != panel.Row
			|| 8 != panel.Column
			|| CursesPanelTransparency.BlankCellsTransparent != panel.Transparency
			|| "r" != panel.ContentWindow.GetCell( 0, 0 ).Content ) {
			throw new InvalidOperationException(
				"DCurses package-only panel manipulation surface failed validation."
			);
		}

		IDisposable disposable = panel;
		disposable.Dispose();
		panel.Dispose();
		if ( panel.IsVisible ) {
			throw new InvalidOperationException(
				"DCurses package-only panel disposal surface failed validation."
			);
		}
		try {
			panel.Show();
		}
		catch ( ObjectDisposedException ) {
			return;
		}

		throw new InvalidOperationException(
			"DCurses package-only panel disposal did not reject later manipulation."
		);
	}
}
