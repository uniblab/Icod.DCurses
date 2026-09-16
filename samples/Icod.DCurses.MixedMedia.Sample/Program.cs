/*
	Icod.DCurses.MixedMedia.Sample
	Retained mixed-media presentation acceptance sample for Icod.DCurses 1.6.
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
using Icod.Terminal;

const int MinimumColumns = 48;
const int MinimumRows = 16;
const int PadColumns = 56;
const int PadRows = 14;
const int PlaceholderColumns = 8;
const int PlaceholderRows = 4;

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
standard.WrapMode = CursesWrapMode.Clip;

if ( screen.Columns < MinimumColumns || screen.Rows < MinimumRows ) {
	standard.Clear();
	WriteLabel(
		standard,
		0,
		0,
		$"Mixed-media sample needs at least {MinimumColumns}x{MinimumRows}."
	);
	await session.RefreshAsync();
	return;
}

standard.Clear();
WriteLabel(
	standard,
	0,
	0,
	"Icod.DCurses 1.6 retained mixed-media presentation"
);
WriteLabel(
	standard,
	1,
	0,
	"Pad: logical text + raster placeholder cells | Panel: independent overlay"
);

int viewportRows = Math.Min( 9, screen.Rows - 5 );
int viewportColumns = Math.Min( 40, screen.Columns - 4 );
CursesWindow viewportWindow = screen.CreateWindow(
	3,
	2,
	viewportRows,
	viewportColumns
);
viewportWindow.WrapMode = CursesWrapMode.Clip;

CursesPad pad = new( PadColumns, PadRows );
CursesWindow padWindow = pad.ContentWindow;
padWindow.WrapMode = CursesWrapMode.Clip;
for ( int row = 0; row < PadRows; row++ ) {
	WriteLabel(
		padWindow,
		row,
		0,
		$"row {row:D2}  retained logical content --------------------------------"
	);
}

byte[] pixels = CreateRasterPixels(
	32,
	16
);
TerminalRasterImage image = TerminalRasterImage.CreateRgb24(
	32,
	16,
	pixels
);
TerminalControlResult<CursesRasterResource> resourceResult =
	await session.CreateRasterResourceAsync( image );

CursesRasterResource? resource = null;
CursesRasterPlaceholder? placeholder = null;
try {
	if ( resourceResult.IsAvailable ) {
		resource = resourceResult.GetRequiredValue();
		TerminalControlResult<CursesRasterPlaceholder> placeholderResult =
			await resource.CreatePlaceholderAsync(
				PlaceholderColumns,
				PlaceholderRows
			);
		if ( placeholderResult.IsAvailable ) {
			placeholder = placeholderResult.GetRequiredValue();
			for ( int row = 0; row < placeholder.Rows; row++ ) {
				padWindow.Move(
					4 + row,
					7
				);
				for ( int column = 0; column < placeholder.Columns; column++ ) {
					padWindow.WriteRasterCell(
						placeholder.GetCell(
							row,
							column
						)
					);
				}
			}
		}
	}

	CursesPadViewport viewport = pad.CreateViewport(
		viewportWindow,
		0,
		0,
		viewportRows,
		viewportColumns,
		0,
		0
	);
	viewport.Present();

	int panelColumns = Math.Min( 25, screen.Columns - 8 );
	using CursesPanel panel = screen.CreatePanel(
		5,
		screen.Columns - panelColumns - 2,
		5,
		panelColumns
	);
	panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
	panel.ContentWindow.WrapMode = CursesWrapMode.Clip;
	WriteLabel(
		panel.ContentWindow,
		0,
		1,
		"overlay panel"
	);
	WriteLabel(
		panel.ContentWindow,
		2,
		1,
		"blank cells stay transparent"
	);

	using CursesInteractionRouter router = new( screen );
	using CursesInteractionRegion viewportRegion = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle(
				3,
				2,
				viewportRows,
				viewportColumns
			)
		) {
			IsFocusable = true,
			TraversalOrder = 0,
			PointerShape = CursesPointerShape.Text
		}
	);
	using CursesInteractionRegion panelRegion = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle(
				0,
				0,
				panel.Rows,
				panel.Columns
			)
		) {
			Panel = panel,
			IsFocusable = true,
			TraversalOrder = 1,
			PointerShape = CursesPointerShape.Pointer
		}
	);

	standard.Move(
		Math.Min( screen.Rows - 1, 14 ),
		0
	);
	await session.RefreshAsync();

	if ( viewport.PadColumn + 1 <= pad.Columns - viewport.Columns ) {
		viewport.PanBy(
			0,
			1
		);
		viewport.Present();
		WriteLabel(
			standard,
			2,
			0,
			"Second frame pans the same retained raster through the logical viewport."
		);
		await session.RefreshAsync();
	}
} finally {
	if ( placeholder is not null ) {
		await placeholder.DisposeAsync();
	}
	if ( resource is not null ) {
		await resource.DisposeAsync();
	}
}

static byte[] CreateRasterPixels(
	int width,
	int height
) {
	byte[] result = new byte[ checked( width * height * 3 ) ];
	int offset = 0;
	for ( int row = 0; row < height; row++ ) {
		for ( int column = 0; column < width; column++ ) {
			result[ offset++ ] = (byte)( column * 255 / Math.Max( 1, width - 1 ) );
			result[ offset++ ] = (byte)( row * 255 / Math.Max( 1, height - 1 ) );
			result[ offset++ ] = (byte)( 255 - result[ offset - 3 ] );
		}
	}
	return result;
}

static void WriteLabel(
	CursesWindow window,
	int row,
	int column,
	string text
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( text );
	if ( row < 0 || row >= window.Rows || column < 0 || column >= window.Columns ) {
		return;
	}

	int available = window.Columns - column;
	int count = Math.Min(
		available,
		text.Length
	);
	for ( int index = 0; index < count; index++ ) {
		window.SetCell(
			row,
			column + index,
			new CursesCell( text[ index ].ToString() )
		);
	}
}
