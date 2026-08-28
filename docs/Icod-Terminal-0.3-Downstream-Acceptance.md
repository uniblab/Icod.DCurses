# Icod.Terminal 0.3 Downstream Acceptance

**DCurses version:** `0.1.0-Alpha-22`
**Terminal package:** `Icod.Terminal 0.3.0-alpha.8`
**TermInfo package:** `Icod.TermInfo 1.3.0`
**Reference commit:** `1e1cb2e5043f5ccfaf68d2380f3d870fb6e0f7c8`
**Status:** execution pending

Alpha-22 is a downstream compatibility checkpoint for the Terminal 0.3 release
candidate. It does not add a DCurses feature family and does not decide the
stable DCurses 0.1 dependency line.

Acceptance requires the ordinary Windows/Ubuntu/macOS PR matrix, package
verification, and fresh package-only consumer restore to pass with the published
Terminal and TermInfo packages.

The architecture must remain unchanged:

- `CursesSession` remains the application-facing terminal owner;
- `TerminalSession` remains the lower-level live-terminal owner;
- DCurses `src/` does not implement `ITerminalInput`;
- DCurses `src/` does not add a second raw input loop;
- DCurses `src/` does not add CSI/DCS response parsing or response matchers;
- rich input, presentation, lifecycle, and restoration remain on public
  Icod.Terminal APIs.

The fresh package-only restore consumes locally packed DCurses while resolving
`Icod.Terminal 0.3.0-alpha.8` and `Icod.TermInfo 1.3.0` from NuGet.org.

If Alpha-22 passes without production-source changes, Terminal 0.3 has satisfied
its required real-downstream DCurses acceptance.
