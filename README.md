# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for
.NET. It provides retained screens, windows, pads and panels; Unicode-aware text
layout; viewports and geometry; semantic metadata and mixed media; damage-aware
refresh; and deterministic interaction routing without taking ownership of the
application's event loop.

## Install

```text
dotnet add package Icod.DCurses
```

The package targets .NET 8, .NET 9 and .NET 10 and uses C# 13. NuGet restores
the compatible `Icod.Terminal` dependency; applications do not need a direct
`Icod.TermInfo` reference for DCurses.

To run the editor-foundation sample from a repository checkout:

```text
dotnet run --project samples/Icod.DCurses.Editor.Sample/Icod.DCurses.Editor.Sample.csproj -c Release -f net10.0
```

To inspect foreground/background behavior on your own terminal, run the
[public-only color sample](https://github.com/uniblab/Icod.DCurses/blob/main/samples/Icod.DCurses.Color.Sample/README.md)
from a repository checkout:

```text
dotnet run --project samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj -c Release -f net10.0
```

## Quick start

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

screen.Clear();
screen.Move( 0, 0 );
screen.Write(
	"Hello from Icod.DCurses",
	new CursesStyle(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Bold
	)
);

await session.RefreshAsync();
CursesEvent inputEvent = await session.ReadEventAsync();
```

The application decides how to interpret `inputEvent`, when to refresh again,
and when to exit. DCurses does not start a callback dispatcher or a second input
reader.

## Input modes and application shortcuts

`CursesSessionOptions.InputMode` defaults to `CursesInputMode.CBreak`, which
keeps host signal processing enabled. Select `CursesInputMode.Raw` when the
application must receive `Ctrl+C`, `Ctrl+Z`, or similar control chords itself:

```csharp
CursesSessionOptions options = new() {
	InputMode = CursesInputMode.Raw
};
await using CursesSession session = await CursesSession.OpenAsync( options );
```

In raw mode, `Ctrl+C` and `Ctrl+Z` are delivered as `CursesKey.Character`
events with `CursesKeyModifiers.Control`; the application decides whether they
mean copy, undo, exit, or another command. Use `CBreak` when the host should
retain its normal interrupt and suspend handling.

## Color surfaces and fallback

Each `CursesCell` carries a complete `CursesStyle`: independently requested
foreground and background `CursesColor` values plus text attributes. A color
may be terminal-default, indexed, or direct RGB. Logical windows, pads,
viewports, and panels retain the requested style; refresh resolves it against
the current terminal's capabilities. Inspect `session.PresentationCapabilities`
and choose an application palette that remains legible when a color cannot be
represented:

```csharp
CursesPresentationCapabilities capabilities = session.PresentationCapabilities;
CursesColor ink = capabilities.SupportsDefaultColorRestoration
	&& capabilities.SupportsForegroundColor
	&& capabilities.IndexedColorCount >= 8
	? CursesColor.Indexed( 7 )
	: CursesColor.Default;
CursesColor paper = capabilities.SupportsDefaultColorRestoration
	&& capabilities.SupportsBackgroundColor
	&& capabilities.IndexedColorCount >= 8
	? CursesColor.Indexed( 4 )
	: CursesColor.Default;
screen.Write( "Colored text", new CursesStyle( ink, paper ) );
```

This snippet continues the quick-start session and window above. An unsupported
axis degrades to terminal-default without changing a supported axis; there is
no automatic nearest-color quantization. The terminal's actual appearance can
still depend on its palette and user settings. For a colored surface, set
`CursesPanelTransparency.Opaque`: blank cells with a styled background then
cover lower panels. `BlankCellsTransparent` deliberately treats even styled
blanks as transparent. `CursesTextAttributes.Reverse` is a semantic attribute,
not an interchange of the stored foreground and background colors; display
support is terminal-dependent.

## Clipboard and cursor style

Icod.DCurses 2.4 adds explicit, bounded clipboard operations and reversible
semantic cursor-style leases over the canonical terminal session:

```csharp
CursesSessionOptions options = new() {
	HideCursor = false,
	ClipboardOptions = new CursesClipboardOptions {
		MaximumReadBytes = 65_536,
		MaximumWriteBytes = 65_536
	}
};

await using CursesSession session = await CursesSession.OpenAsync( options );
await using CursesCursorStyleLease caret =
	await session.AcquireCursorStyleAsync(
		CursesCursorStyle.SteadyBar,
		TimeSpan.FromMilliseconds( 250 )
	);

await session.WriteClipboardAsync(
	CursesClipboardSelection.Clipboard,
	"selected text"
);

byte[] payload = await session.ReadClipboardAsync(
	CursesClipboardSelection.Clipboard,
	TimeSpan.FromMilliseconds( 250 )
);
```

The default read and write budgets are independently 65,536 bytes and may be
configured up to 16 MiB before opening an owned session. They are local safety
limits, not a negotiation of remote clipboard capacity. Writes confirm that the
complete terminal command was emitted, not that the host accepted it. A read
timeout is inconclusive; applications should own fallback content and policy.

Cursor style and visibility are independent. The lease restores the exact
observed preceding style in LIFO order. If observation is unsupported, times out
or is cancelled, no guessed reset or style mutation is emitted.

## Editor foundations

The editor sample is deliberately a public-API composition witness, not a
document engine. It demonstrates:

- viewport-local Unicode layout, caret geometry, selection and retained repaint;
- a synthetic ten-million-record source with work bounded to visible records;
- bracketed paste as routed text data and key-release suppression;
- primary-button caret placement and selection with captured outside drag;
- cancellation on release, focus loss, modal activation, resize and disposal;
- a separately hit-tested final-column scrollbar without implicit Tab traversal;
- explicit `Ctrl+C`/`Ctrl+V` clipboard commands with an application fallback;
- a visible caret with a best-effort scoped steady-bar style.

Document storage, undo/redo, revision caches, selection semantics, autoscroll
timing, tabs, dialogs and file I/O belong in Icod.Widgets or the application.
The complete controls and limitations are documented in the
[samples guide](https://github.com/uniblab/Icod.DCurses/blob/main/samples/README.md#icoddcurseseditorsample).

## Text layout and retained projection

Source positions are UTF-16 offsets at legal text-element boundaries; visual
positions are rows and terminal columns. The application supplies the bounded
text slice to lay out and decides which lines to project:

```csharp
CursesTextLayout layout = CursesTextLayout.Create(
	"Hello 👩‍💻 world",
	new CursesTextLayoutOptions( 20 ) {
		WrapMode = CursesTextWrapMode.TextElement
	}
);
CursesTextPosition afterEmoji =
	layout.GetNextPosition( new CursesTextPosition( 6 ) );
CursesTextVisualPosition caret = layout.GetVisualPosition( afterEmoji );

screen.PresentTextLayout( layout, 0, layout.Lines.Count, 0, 0 );
if ( caret.Line < screen.Rows && caret.Column < screen.Columns ) {
	screen.Move( caret.Line, caret.Column );
}
await session.RefreshAsync();
```

`HitTest` and `GetSelectionRectangles` use the same Unicode/cell geometry.
Applications should not build file-sized pads or whole-file layouts; keep
document indexing and revision-aware visible-slice caches above DCurses.

## Interaction routing

Applications can register semantic key gestures and sequences, inspect the
currently effective bindings for help text, and route one normalized input event
at a time. Pending sequences have no hidden timer:

```csharp
CursesKeyGesture g = CursesKeyGesture.ForCharacter( new Rune( 'g' ) );
CursesCommand goTop = new( "document.go-top" );
router.BindGlobalGestureSequence( [ g, g ], goTop );

IReadOnlyList<CursesCommandSequenceBinding> helpBindings =
	router.GetEffectiveGestureSequenceBindings();

CursesCommandSequenceResult result = router.ProcessCommandSequence( inputEvent );
if ( result.Kind is CursesCommandSequenceResultKind.Completed ) {
	ExecuteApplicationCommand( result.Command! );
} else if ( result.Fallback is not null ) {
	HandleOrdinaryRoutingResult( result.Fallback );
}
```

Focused-region, active-scope and router-global precedence match ordinary routing.
The application owns command execution, labels, localization, prefix timeouts,
Tab policy and event-loop timing.

## Feature inventory

- Retained logical screens, windows, pads, viewports and panels.
- Unicode text-element measurement, wrapping, hit testing and selection geometry.
- Styles, line drawing, semantic hyperlinks and width-two cell coherence.
- Sparse/full damage refresh, synchronized output and physical-state invalidation.
- Geometry, clipping, tracks, docking and prepared bulk cell writes.
- Normalized keyboard, mouse, focus, paste and lifecycle events.
- Interaction regions, modal scopes, focus, pointer capture and command sequences.
- Bounded clipboard access and scoped semantic cursor styles.
- Retained raster resources/placeholders and bounded cell-aligned raster atlases.
- net8.0, net9.0 and net10.0 package assets with portable symbols and XML docs.

## Architecture and boundaries

```text
application / Icod.Widgets
    document, controls, commands, event loop, files and policy
        |
Icod.DCurses
    retained cells, layout projection, damage and interaction mechanisms
        |
Icod.Terminal
    live session, semantic protocols, host I/O and reversible terminal state
        |
Icod.TermInfo
    immutable capability data and planning
        |
terminal / tty
```

Icod.DCurses does not provide a widget framework, text buffer, callback
dispatcher, terminal emulator, PTY host, native clipboard, private protocol
parser, automatic raster replay or application policy. Terminal remains the sole
owner of protocol framing, query correlation, live identities and physical state.

## Compatibility and versioning

The 2.5 color-surface qualification retains the 2.4 public surface, which is
additive over 2.3.1, and keeps `AssemblyVersion`
`2.0.0.0`. It has 109 exported types, 857 canonical declared contract lines and
SHA-256 fingerprint
`87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e`.

Version 2.0 changed the public profile/dimensions signatures and advanced
`AssemblyVersion` to `2.0.0.0`; applications upgrading from 1.6 must rebuild.
See the [2.0 migration guide](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/2.0-Migration-Guide.md).

## Documentation and samples

- [2.5 public API baseline](https://github.com/uniblab/Icod.DCurses/blob/main/docs/Public-API-Baseline-2.5.md)
- [2.5 development roadmap](https://github.com/uniblab/Icod.DCurses/blob/main/docs/Icod.DCurses-2.5.0-Development-Roadmap.md)
- [Sample guide and runnable commands](https://github.com/uniblab/Icod.DCurses/blob/main/samples/README.md)
- [Clipboard integration evidence](https://github.com/uniblab/Icod.DCurses/blob/main/docs/T2402-Clipboard-Integration.md)
- [Cursor-style lifecycle evidence](https://github.com/uniblab/Icod.DCurses/blob/main/docs/T2404-Cursor-Style-Lifecycle.md)
- [Viewport measurement](https://github.com/uniblab/Icod.DCurses/blob/main/docs/T2405-Viewport-Measurement.md)
- [Editor interaction acceptance](https://github.com/uniblab/Icod.DCurses/blob/main/docs/T2407-Editor-Interaction-Acceptance.md)
- [Changelog](https://github.com/uniblab/Icod.DCurses/blob/main/CHANGELOG.md)
- [NuGet package and version history](https://www.nuget.org/packages/Icod.DCurses/)

## Support the project

Icod.DCurses and its ecosystem packages are maintained by a solo developer.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Authors

Inspired by Bill Joy (`termcap`); Ken Arnold (`termcap` and `curses`); Mary Ann
(born Mark) Horton (`terminfo`); Pavel Curtis (`pcurses`); and Zeyd Ben-Halim,
Eric S. Raymond and Thomas Dickey (`libtinfo` and `ncurses`).

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## License

Icod.DCurses is licensed under the GNU Lesser General Public License, version 3
or later. Samples and tests are licensed under the GNU General Public License,
version 3 or later. See the
[repository license](https://github.com/uniblab/Icod.DCurses/blob/main/LICENSE)
and per-project/source declarations.
