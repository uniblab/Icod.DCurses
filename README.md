# Icod.DCurses

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation leases. `Icod.DCurses` owns curses-shaped events,
virtual screens and windows, terminal cells and styles, rendition policy, and
refresh/damage synchronization.

## Status

The project is under active development toward version `0.1.0`.

`0.1.0-Alpha-13` follows the Icod.Terminal T10 integration checkpoint. The active
DCurses build consumes `Icod.Terminal 0.1.0-alpha.11` for live-terminal ownership
and delegates relative event-timeout and Escape-sequence timing to the
`Icod.Timing 1.0.0` substrate owned by Terminal. DCurses does not add a direct
`Icod.Timing` dependency because the active curses layer owns no independent timer
or scheduler. The former DCurses backend, native mode, lifecycle-source, and
input-decoder files remain excluded from compilation pending physical cleanup.

The first release continues to be driven by the requirements of `top`,
`slabtop`, and `watch`.

See `Icod.DCurses-Development-Roadmap.md` for the broader development contract
through `1.0.0`, and `docs/Icod-Terminal-T10-Integration.md` for the substrate
reset implemented by Alpha-12.

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
       presentation-state leases
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
- `Icod.Terminal` 0.1.0-alpha.11

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
