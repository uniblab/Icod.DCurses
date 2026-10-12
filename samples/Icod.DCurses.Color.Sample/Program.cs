/*
	Icod.DCurses.Color.Sample
	Interactive color-surface acceptance sample for Icod.DCurses.
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

using Icod.DCurses;

const int MinimumColumns = 48;
const int MinimumRows = 14;

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
standard.WrapMode = CursesWrapMode.Clip;
using CursesPanel panel = screen.CreatePanel( 0, 0, 1, 1 );
panel.Transparency = CursesPanelTransparency.Opaque;
panel.ContentWindow.WrapMode = CursesWrapMode.Clip;

bool running = true;
while ( running ) {
	Paint( screen, standard, panel, session.PresentationCapabilities );
	await session.RefreshAsync();

	CursesEvent current = await session.ReadEventAsync();
	if ( current.Kind == CursesEventKind.Lifecycle
		&& current.Lifecycle is not null
		&& current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
		break;
	}
	if ( current.RequiresRepaint ) {
		_ = session.SynchronizeDimensions();
		session.Invalidate();
		continue;
	}
	if ( current.Kind != CursesEventKind.Input || current.Input is null ) {
		continue;
	}

	CursesInputEvent input = current.Input;
	if ( input.Kind == CursesInputEventKind.EndOfInput
		|| ( input.Kind == CursesInputEventKind.Key && input.Key == CursesKey.Escape )
		|| ( input.Kind == CursesInputEventKind.Text
			&& input.Character.HasValue
			&& input.Character.Value.Value is 'q' or 'Q' ) ) {
		running = false;
	}
}

panel.Hide();
standard.Clear();
await session.RefreshAsync();
return 0;

static void Paint(
	CursesScreen screen,
	CursesWindow standard,
	CursesPanel panel,
	CursesPresentationCapabilities capabilities
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( standard );
	ArgumentNullException.ThrowIfNull( panel );
	SamplePalette palette = SelectPalette( capabilities );

	standard.BackgroundCell = CursesCell.Blank();
	standard.Clear();
	if ( screen.Columns < MinimumColumns || screen.Rows < MinimumRows ) {
		panel.Hide();
		WriteLine(
			standard,
			0,
			$"Resize to at least {MinimumColumns} columns x {MinimumRows} rows; Q/Escape exits."
		);
		return;
	}

	panel.SetBounds( new CursesRectangle(
		1,
		2,
		Math.Min( 12, screen.Rows - 2 ),
		Math.Min( 68, screen.Columns - 4 )
	) );
	panel.Show();
	CursesWindow content = panel.ContentWindow;
	content.BackgroundCell = CursesCell.Blank( palette.Surface );
	content.Clear();
	content.DrawBorder( palette.Title );

	WriteLine( content, 1, $"Icod.DCurses color surfaces — {palette.Name}", palette.Title );
	WriteLine(
		content,
		2,
		$"indexed={capabilities.IndexedColorCount} rgb={capabilities.SupportsDirectRgb} "
			+ $"fg={capabilities.SupportsForegroundColor} bg={capabilities.SupportsBackgroundColor} "
			+ $"restore={capabilities.SupportsDefaultColorRestoration}",
		palette.Surface
	);
	WriteLine( content, 4, " terminal-default foreground/background ", CursesStyle.Default );
	WriteLine( content, 5, " explicit foreground-only request ", palette.ForegroundOnly );
	WriteLine( content, 6, " explicit background-only request ", palette.BackgroundOnly );
	WriteLine( content, 7, " simultaneous foreground/background pair ", palette.Pair );
	WriteLine( content, 8, " reverse attribute (colors are not swapped in storage) ", palette.Reverse );
	WriteLine( content, 9, " width-two: 界 on a colored background ", palette.Wide );
	WriteLine( content, content.Rows - 2, "Opaque panel padding owns its styled blanks. Q/Escape exits.", palette.Surface );
}

static SamplePalette SelectPalette( CursesPresentationCapabilities capabilities ) {
	bool bothAxes = capabilities.SupportsDefaultColorRestoration
		&& capabilities.SupportsForegroundColor
		&& capabilities.SupportsBackgroundColor;
	if ( bothAxes && capabilities.SupportsDirectRgb ) {
		return CreatePalette(
			"direct RGB requested",
			CursesColor.Rgb( 235, 242, 255 ),
			CursesColor.Rgb( 28, 42, 72 ),
			CursesColor.Rgb( 255, 214, 102 ),
			CursesColor.Rgb( 76, 44, 18 )
		);
	}
	if ( bothAxes && capabilities.IndexedColorCount >= 16 ) {
		return CreatePalette(
			"16-color indexed requested",
			CursesColor.Indexed( 15 ),
			CursesColor.Indexed( 4 ),
			CursesColor.Indexed( 11 ),
			CursesColor.Indexed( 1 )
		);
	}
	if ( bothAxes && capabilities.IndexedColorCount >= 8 ) {
		return CreatePalette(
			"8-color indexed requested",
			CursesColor.Indexed( 7 ),
			CursesColor.Indexed( 4 ),
			CursesColor.Indexed( 3 ),
			CursesColor.Indexed( 1 )
		);
	}

	CursesStyle defaultStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		capabilities.SupportsBold ? CursesTextAttributes.Bold : CursesTextAttributes.None
	);
	return new SamplePalette(
		"terminal-default fallback",
		defaultStyle,
		defaultStyle,
		defaultStyle,
		defaultStyle,
		defaultStyle,
		defaultStyle,
		defaultStyle
	);
}

static SamplePalette CreatePalette(
	string name,
	CursesColor light,
	CursesColor dark,
	CursesColor accent,
	CursesColor accentBackground
) {
	CursesStyle surface = new( light, dark );
	return new SamplePalette(
		name,
		surface,
		new CursesStyle( accent, dark, CursesTextAttributes.Bold ),
		new CursesStyle( accent, CursesColor.Default ),
		new CursesStyle( CursesColor.Default, accentBackground ),
		new CursesStyle( light, accentBackground, CursesTextAttributes.Bold ),
		new CursesStyle( light, accentBackground, CursesTextAttributes.Reverse ),
		new CursesStyle( accent, dark, CursesTextAttributes.Underline )
	);
}

static void WriteLine(
	CursesWindow window,
	int row,
	string text,
	CursesStyle style = default
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( text );
	if ( row < 0 || row >= window.Rows || window.Columns <= 2 ) {
		return;
	}
	window.Move( row, 1 );
	window.Write( CursesText.TruncateToColumns( text, window.Columns - 2 ), style );
}

internal readonly record struct SamplePalette(
	string Name,
	CursesStyle Surface,
	CursesStyle Title,
	CursesStyle ForegroundOnly,
	CursesStyle BackgroundOnly,
	CursesStyle Pair,
	CursesStyle Reverse,
	CursesStyle Wide
);
