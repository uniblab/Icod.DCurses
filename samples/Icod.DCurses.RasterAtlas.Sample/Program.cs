/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

using Icod.DCurses;
using Icod.DCurses.RasterAtlas.Sample;
using Icod.Terminal;

bool forceText = args.Any(
	argument => string.Equals( argument, "--text", StringComparison.OrdinalIgnoreCase )
);
( CursesSession openedSession, bool capabilityUsable, string startupStatus ) =
	await OpenSessionAsync( forceText );
await using CursesSession session = openedSession;
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
CursesWindow map = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
using CursesPanel help = screen.CreatePanel( 0, 0, 1, 1 );
foreach ( CursesWindow window in new[] { standard, map, status, help.ContentWindow } ) {
	window.WrapMode = CursesWrapMode.Clip;
}

RasterAtlasSampleState state = new( 1, 1 );
CursesRasterAtlas? atlas = null;
CursesRasterAtlasGeometry geometry = default;
bool rasterActive = capabilityUsable;
bool helpVisible = false;
bool running = true;
bool layoutDirty = true;
TimeSpan resizePollInterval = TimeSpan.FromMilliseconds( 250 );
string message = startupStatus;

while ( running ) {
	if ( layoutDirty ) {
		layoutDirty = false;
		if ( !RasterAtlasSampleLayout.TryArrange(
			screen,
			standard,
			map,
			status,
			help,
			out int mapRows,
			out int mapColumns
		) ) {
			if ( atlas is not null ) {
				await atlas.DisposeAsync();
				atlas = null;
			}
			WriteLine( standard, 0, "Resize to at least 20 columns x 6 rows; Q exits." );
			await session.RefreshAsync();
		} else {
			_ = state.ResizeViewport( mapRows, mapColumns );
			map.Clear();

			if ( rasterActive ) {
				if ( atlas is not null ) {
					await atlas.DisposeAsync();
					atlas = null;
				}
				try {
					geometry = await session.QueryRasterAtlasGeometryAsync(
						mapRows,
						mapColumns,
						TimeSpan.FromSeconds( 2 )
					);
					TerminalControlResult<CursesRasterAtlas> creation =
						await session.CreateRasterAtlasAsync(
							state.CreateInitialImage( geometry ),
							mapRows,
							mapColumns
						);
					if ( creation.IsAvailable ) {
						atlas = creation.GetRequiredValue();
						map.WriteRasterAtlas(
							0,
							0,
							atlas,
							new CursesRectangle( 0, 0, mapRows, mapColumns )
						);
						message = "Raster atlas active; resize recreates it explicitly.";
					} else {
						rasterActive = false;
						message = creation.Message ?? "Raster creation unavailable; using text.";
					}
				} catch ( Exception exception ) when (
					RasterAtlasSampleFallback.IsRecoverableSetupException( exception )
				) {
					rasterActive = false;
					message = $"Raster setup unavailable ({exception.GetType().Name}); using text.";
				}
			}
			if ( !rasterActive ) {
				DrawTextMap( map, state );
			}
		}
	}

	if ( screen.Rows >= 6 && screen.Columns >= 20 ) {
		DrawStatus( status, state, rasterActive, message );
		if ( helpVisible ) {
			DrawHelp( help.ContentWindow );
			help.Show();
		} else {
			help.Hide();
		}
		await session.RefreshAsync();
	}

	CursesEvent current = await session.ReadEventAsync( resizePollInterval );
	if ( CursesEventKind.Timeout == current.Kind ) {
		int previousRows = screen.Rows;
		int previousColumns = screen.Columns;
		_ = session.SynchronizeDimensions();
		if ( screen.Rows != previousRows || screen.Columns != previousColumns ) {
			session.Invalidate();
			layoutDirty = true;
		}
		continue;
	}
	if ( CursesEventKind.Lifecycle == current.Kind && current.Lifecycle is not null ) {
		if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
			break;
		}
	}
	if ( current.RequiresRepaint ) {
		_ = session.SynchronizeDimensions();
		session.Invalidate();
		layoutDirty = true;
		continue;
	}
	if ( CursesEventKind.Input != current.Kind || current.Input is null ) {
		continue;
	}
	CursesInputEvent input = current.Input;
	if ( CursesInputEventKind.EndOfInput == input.Kind ) {
		break;
	}

	if ( input.Key is CursesKey.Escape ) {
		if ( helpVisible ) {
			helpVisible = false;
			continue;
		}
		break;
	}
	if ( input.Kind is CursesInputEventKind.Text && input.Character.HasValue ) {
		int character = input.Character.Value.Value;
		if ( character is 'q' or 'Q' ) {
			break;
		}
		if ( character is '?' or 'h' or 'H' ) {
			helpVisible = !helpVisible;
			continue;
		}
	}
	if ( helpVisible || screen.Rows < 6 || screen.Columns < 20 ) {
		continue;
	}

	int rowDelta = 0;
	int columnDelta = 0;
	switch ( input.Key ) {
		case CursesKey.Up: rowDelta = -1; break;
		case CursesKey.Down: rowDelta = 1; break;
		case CursesKey.Left: columnDelta = -1; break;
		case CursesKey.Right: columnDelta = 1; break;
	}
	if ( input.Kind is CursesInputEventKind.Text && input.Character.HasValue ) {
		switch ( input.Character.Value.Value ) {
			case 'w': case 'W': rowDelta = -1; break;
			case 's': case 'S': rowDelta = 1; break;
			case 'a': case 'A': columnDelta = -1; break;
			case 'd': case 'D': columnDelta = 1; break;
		}
	}
	if ( 0 == rowDelta && 0 == columnDelta ) {
		continue;
	}

	RasterAtlasMoveResult move = state.Move( rowDelta, columnDelta );
	if ( !move.Moved ) {
		message = "Water blocks that move.";
		continue;
	}
	message = $"Moved to {state.PlayerRow},{state.PlayerColumn}.";
	if ( rasterActive && atlas is not null ) {
		CursesRasterAtlas currentAtlas = atlas;
		try {
			CursesRasterAtlasPresentationResult result = await currentAtlas.PresentAsync(
				state.CreateUpdates( move, geometry )
			);
			if ( result.Status is not CursesRasterAtlasPresentationStatus.Presented
				and not CursesRasterAtlasPresentationStatus.NoChanges ) {
				message = result.Message ?? "Raster presentation unavailable; using text.";
				await currentAtlas.DisposeAsync();
				atlas = null;
				rasterActive = false;
				map.Clear();
				DrawTextMap( map, state );
			}
		} catch ( Exception exception ) when (
			exception is IOException
				or InvalidOperationException
				or OperationCanceledException
		) {
			message = $"Raster state uncertain ({exception.GetType().Name}); using text.";
			try {
				await currentAtlas.DisposeAsync();
			} catch {
				// The session still owns final terminal restoration.
			}
			atlas = null;
			rasterActive = false;
			map.Clear();
			DrawTextMap( map, state );
		}
	} else {
		DrawTextMap( map, state );
	}
}

if ( atlas is not null ) {
	await atlas.DisposeAsync();
}
help.Hide();
standard.Clear();
await session.RefreshAsync();
return 0;

static async Task<( CursesSession Session, bool RasterUsable, string Status )> OpenSessionAsync(
	bool forceText
) {
	if ( forceText ) {
		return (
			await CursesSession.OpenAsync(),
			false,
			"Text mode forced by --text."
		);
	}

	TerminalSession? terminal = null;
	try {
		terminal = await TerminalSession.OpenAsync(
			new TerminalSessionOptions { RequireInteractiveOutput = true }
		);
		TerminalCapabilityStatus capability = await terminal.VerifyCapabilityAsync(
			TerminalCapability.PersistentRasterGraphics
		);
		CursesSession session = await CursesSession.OpenAsync( terminal );
		terminal = null;
		return capability.IsUsable
			? ( session, true, "Persistent raster capability verified." )
			: ( session, false, "Persistent raster unavailable; using text." );
	} catch {
		if ( terminal is not null ) {
			await terminal.DisposeAsync();
		}
		return (
			await CursesSession.OpenAsync(),
			false,
			"Raster verification failed; using text."
		);
	}
}

static void DrawTextMap( CursesWindow map, RasterAtlasSampleState state ) {
	map.WriteCells(
		0,
		0,
		state.ViewportRows,
		state.ViewportColumns,
		state.CreateTextFrame(),
		state.ViewportColumns
	);
}

static void DrawStatus(
	CursesWindow status,
	RasterAtlasSampleState state,
	bool rasterActive,
	string message
) {
	status.Clear();
	WriteLine(
		status,
		0,
		$"{( rasterActive ? "RASTER" : "TEXT" )}  Pos {state.PlayerRow},{state.PlayerColumn}  Arrows/WASD move  ? help  Q exit"
	);
	WriteLine( status, 1, message );
}

static void DrawHelp( CursesWindow window ) {
	window.Clear();
	WriteLine( window, 0, " RASTER ATLAS SAMPLE" );
	WriteLine( window, 2, " Arrows/WASD  Move across the original generated map" );
	WriteLine( window, 3, " Blue water    Blocks movement" );
	WriteLine( window, 4, " ? or H        Close this retained panel" );
	WriteLine( window, 5, " Resize        Explicitly recreate atlas and cells" );
}

static void WriteLine( CursesWindow window, int row, string text ) {
	if ( row < 0 || row >= window.Rows ) {
		return;
	}
	window.Move( row, 0 );
	window.Write( text[ .. Math.Min( text.Length, window.Columns ) ] );
}
