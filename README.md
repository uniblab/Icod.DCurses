# Icod.DCurses

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, terminal cells and styles, rendition policy, and
refresh/damage synchronization.

## Status

The project is under active development toward version `0.1.0`.

`0.1.0-Alpha-15` established the automated Icod.Terminal T19 rich-input
acceptance boundary and builds and tests successfully against
`Icod.Terminal 0.2.0-alpha.6`. `0.1.0-Alpha-16` provides the live rich-input
acceptance showcase. `0.1.0-Alpha-17` begins the T12 ProcPs application-shaped
acceptance pass with a dedicated `watch` harness that exercises periodic and
immediate refresh, resize repaint, title/no-title layout, wrap/clip policy,
difference highlighting, semantic child colors, failure alerts, and preserved
presentation state without introducing ProcPs policy into the library.

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, or second input loop.

The first release continues to be driven by the requirements of `top`,
`slabtop`, and `watch`.

See `Icod.DCurses-Development-Roadmap.md` for the broader development contract
through `1.0.0`, `docs/Icod-Terminal-T10-Integration.md` for the substrate reset,
and `docs/Icod-Terminal-T19-Rich-Input-Acceptance.md` for the active rich-input
acceptance checkpoint.

## Architecture

```text
top / slabtop / watch / other TUIs
                 |
            Icod.DCurses
     windows / cells / refresh
       rendition / curses events
                 |
            Icod.Terminal
 session / input / lifecycle / dimensions
 presentation / input-protocol leases
                 |
            Icod.TermInfo
      terminal capability model
                 |
            terminal / tty
```

`Icod.DCurses` does not hard-code one terminal family. Terminal-specific output
continues to be selected through `Icod.TermInfo`, while live session ownership
and reversible terminal state are centralized in `Icod.Terminal`.

## Target

The initial implementation targets:

- .NET 10
- C# 13
- Windows
- Linux
- macOS

The active runtime dependencies are:

- `Icod.TermInfo` 1.0.0
- `Icod.Terminal` 0.2.0-alpha.6

## Build

From the repository root:

```text
build.cmd
```

or:

```text
./build.sh
```

Both scripts perform clean, restore, build, test, and pack operations by default
and also accept one of those phase names individually.

## License

`Icod.DCurses` is licensed under the GNU Lesser General Public License,
version 3 or later.
