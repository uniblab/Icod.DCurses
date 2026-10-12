# Icod.DCurses 2.5 Public API Baseline

**Release-line identity:** `2.5.0` (stable-source candidate; not yet published)
**AssemblyVersion:** `2.0.0.0`
**Direct production dependency:** `Icod.Terminal 1.29.0`

The compiled contract remains identical to published 2.4.0:

```text
109 exported types
857 canonical declared contract lines
sha256 87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e
```

`Public-API-Fingerprint-2.5.json` records the unchanged contract, while the
2.4 fingerprint remains immutable. The color-surface work qualifies existing
`CursesColor`, `CursesStyle`, `CursesCell`, `CursesPresentationCapabilities`,
and `CursesPanelTransparency` rather than adding a widget theme or new protocol
API. The assembly has no direct production TermInfo reference. The application
owns palette selection; Widgets owns theme/state resolution; Terminal owns
physical normalization and encoding.

This document records the candidate source contract, not a claim that 2.5 has
been tagged or published. See [the 2.4 baseline](https://github.com/uniblab/Icod.DCurses/blob/main/docs/Public-API-Baseline-2.4.md)
for the inherited signatures and compatibility notes.
