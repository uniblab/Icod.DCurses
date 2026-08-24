# Icod.DCurses

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library is intended to sit above `Icod.TermInfo` and the neutral terminal
control substrate used by the Icod libraries. It will own live terminal-session
lifecycle, input events, virtual screens and windows, terminal-cell styling,
refresh/damage synchronization, resize handling, and safe restoration.

## Status

The project is under active development toward version `0.1.0`.

The first release is being driven by the requirements of `top`, `slabtop`, and
`watch`. T01 establishes the repository, solution, package, test, sample, and CI
foundation. The public curses/session API begins in subsequent 0.1.0 tranches.

See `Icod.DCurses-Development-Roadmap.md` for the development contract through
`1.0.0`.

## Architecture

```text
top / slabtop / watch / other TUIs
                 |
            Icod.DCurses
           /            \
  Icod.TermInfo    terminal control
           \            /
            terminal / tty
```

`Icod.TermInfo` remains the terminal-capability authority. `Icod.DCurses` will
use those capabilities to operate live interactive terminal sessions rather than
hard-coding one terminal family.

## Target

The initial implementation targets:

- .NET 10
- C# 13
- Windows
- Linux
- macOS

The initial runtime dependencies are:

- `Icod.TermInfo`
- `Icod.CommandFramework`

The dependency on `Icod.CommandFramework` is limited architecturally to the
neutral terminal-control substrate and may be revisited before `1.0.0`.

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
