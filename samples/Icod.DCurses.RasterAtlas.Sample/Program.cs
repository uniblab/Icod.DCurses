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
bool forceRaster = args.Any(
	argument => string.Equals( argument, "--raster", StringComparison.OrdinalIgnoreCase )
);
( CursesSession openedSession, bool persistentUsable, bool ordinaryUsable, string startupStatus ) =
	await OpenSessionAsync( forceText, forceRaster );
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
bool rasterActive = persistentUsable || ordinaryUsable;
bool completeFrame = !persistentUsable && ordinaryUsable;
bool helpVisible = false;
bool running = true;
bool layoutDirty = true;
bool displayDirty = true;
int previousRows = screen.Rows;
int previousColumns = screen.Columns;
TimeSpan resizePollInterval = TimeSpan.FromMilliseconds( 250 );
string message = startupStatus;

while ( running ) {
	// Refresh also synchronizes live dimensions. Compare with the dimensions of
	// the last layout so that a resize observed there is not lost by the poll.
	layoutDirty |= screen.Rows != previousRows || screen.Columns != previousColumns;
	if ( layoutDirty ) {
		layoutDirty = false;
		displayDirty = true;
		previousRows = screen.Rows;
		previousColumns = screen.Columns;
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
					geometry = default;
					geometry = await session.QueryRasterAtlasGeometryAsync(
						mapRows,
						mapColumns,
						TimeSpan.FromSeconds( 2 )
					);
					if ( !completeFrame ) {
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
							completeFrame = ordinaryUsable;
							rasterActive = ordinaryUsable;
							message = ordinaryUsable ? "Atlas unavailable; using complete raster frames." : "Raster creation unavailable; using text.";
						}
					}
					if ( completeFrame ) {
						message = "Complete raster frames active (Terminal chooses Kitty/Sixel).";
					}
				} catch ( Exception exception ) when (
					RasterAtlasSampleFallback.IsRecoverableSetupException( exception )
				) {
					if ( atlas is not null ) {
						await atlas.DisposeAsync();
						atlas = null;
					}
					completeFrame = ordinaryUsable && geometry.Rows == mapRows && geometry.Columns == mapColumns;
					rasterActive = completeFrame;
					map.Clear();
					message = completeFrame ? "Atlas setup unavailable; using complete raster frames."
						: $"Raster setup unavailable ({exception.GetType().Name}); using text.";
				}
			}
			if ( !rasterActive || completeFrame ) {
				DrawTextMap( map, state );
			}
		}
	}

	if ( displayDirty && screen.Rows >= 6 && screen.Columns >= 20 ) {
		displayDirty = false;
		DrawStatus( status, state, rasterActive, completeFrame, message );
		if ( helpVisible ) {
			DrawHelp( help.ContentWindow );
			help.Show();
		} else {
			help.Hide();
		}
		if ( rasterActive && completeFrame && !helpVisible ) {
			try {
				await session.RefreshRasterAsync( state.CreateInitialImage( geometry ), map.Bounds.Row, map.Bounds.Column, geometry );
			} catch ( Exception exception ) when (
				RasterAtlasSampleFallback.IsRecoverableSetupException( exception ) || exception is NotSupportedException
			) {
				if ( exception is InvalidOperationException
					&& ( screen.Rows != previousRows || screen.Columns != previousColumns ) ) {
					layoutDirty = true;
					continue;
				}
				rasterActive = false;
				message = $"Complete raster unavailable ({exception.GetType().Name}); using text.";
				DrawStatus( status, state, false, false, message );
				DrawTextMap( map, state );
				await session.RefreshAsync();
			}
		} else {
			await session.RefreshAsync();
		}
	}

	CursesEvent current = await session.ReadEventAsync( resizePollInterval );
	if ( CursesEventKind.Timeout == current.Kind ) {
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
			displayDirty = true;
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
			displayDirty = true;
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
	displayDirty = true;
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

static async Task<( CursesSession Session, bool PersistentUsable, bool OrdinaryUsable, string Status )> OpenSessionAsync(
	bool forceText,
	bool forceRaster
) {
	CursesSessionOptions options = new() { UseSynchronizedOutput = true };
	if ( forceText ) {
		return (
			await CursesSession.OpenAsync( options ),
			false,
			false,
			"Text mode forced by --text."
		);
	}

	TerminalSession? terminal = null;
	try {
		terminal = await TerminalSession.OpenAsync(
			new TerminalSessionOptions { RequireInteractiveOutput = true }
		);
		TerminalCapabilityStatus ordinary = await terminal.VerifyCapabilityAsync(
			TerminalCapability.RasterGraphics
		);
		bool persistent = false;
		if ( ordinary.IsUsable && !forceRaster ) {
			try {
				persistent = ( await terminal.VerifyCapabilityAsync( TerminalCapability.PersistentRasterGraphics ) ).IsUsable;
			} catch ( Exception exception ) when ( RasterAtlasSampleFallback.IsRecoverableSetupException( exception ) ) {
				// Ordinary raster evidence remains independent of persistent image identities.
			}
		}
		CursesSession session = await CursesSession.OpenAsync( terminal, options );
		terminal = null;
		return ( session, persistent, ordinary.IsUsable, persistent
			? "Persistent raster capability verified."
			: ordinary.IsUsable ? "Ordinary raster capability verified." : "Raster unavailable; using text." );
	} catch {
		if ( terminal is not null ) {
			await terminal.DisposeAsync();
		}
		return (
			await CursesSession.OpenAsync( options ),
			false,
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
	bool completeFrame,
	string message
) {
	status.Clear();
	WriteLine(
		status,
		0,
		$"{( rasterActive ? completeFrame ? "FRAME" : "ATLAS" : "TEXT" )}  Pos {state.PlayerRow},{state.PlayerColumn}  Arrows/WASD move  ? help  Q exit"
	);
	WriteLine( status, 1, message );
}

static void DrawHelp( CursesWindow window ) {
	window.Clear();
	WriteLine( window, 0, " RASTER ATLAS SAMPLE" );
	WriteLine( window, 2, " Arrows/WASD  Move across the original generated map" );
	WriteLine( window, 3, " Blue water    Blocks movement" );
	WriteLine( window, 4, " ? or H        Close this retained panel" );
	WriteLine( window, 5, " Resize        Requery pixels and recreate view" );
	WriteLine( window, 6, " Frame mode    Text view while help is open" );
}

static void WriteLine( CursesWindow window, int row, string text ) {
	if ( row < 0 || row >= window.Rows ) {
		return;
	}
	window.Move( row, 0 );
	window.Write( text[ .. Math.Min( text.Length, window.Columns ) ] );
}
