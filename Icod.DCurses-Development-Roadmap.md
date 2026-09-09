# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Merged stable-source baseline:** `1.0.0`  
**Current main baseline:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Latest published release:** `0.9.0`  
**Current package metadata:** `1.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.5.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and hyperlinks  
**Status:** post-1.0 planning and roadmap foundation; this planning PR does not bump the package version

---

## Purpose

This file is the current roadmap index for `Icod.DCurses`.

The original long-form roadmap served the project from its initial 0.1 work through the 1.0 stable-contract program. It is preserved unchanged under:

- `docs/history/Icod.DCurses-Development-Roadmap-through-1.0.md`

Current development is now organized by stable 1.x release documents rather than by continuously rewriting the original pre-1.0 plan.

---

## Current release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Merged; publication remains separate |
| `1.1.0` | Semantic cell metadata and hyperlinks | Active planning / next implementation release |
| `1.2.0` | Panels, layers, visibility, and z-order composition | Approved future release |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus, interaction regions, key gestures, hit testing, and pointer semantics | Approved future release |
| later | Raster graphics over Terminal semantic raster routing | Deferred until the required Terminal public contract exists |

The approved 1.1–1.4 release train is documented in:

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`

The detailed active 1.1 plan is:

- `Icod.DCurses-1.1.0-Development-Roadmap.md`

---

## Layer ownership remains unchanged

```text
Applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
     semantic cells / windows / pads / composition
       retained refresh / interaction / layout
                         |
                    Icod.Terminal
   live session / input / lifecycle / semantic protocols
     capability evidence / protocol routing / output
                         |
                    Icod.TermInfo
             immutable capability authority
                         |
                  terminal / tty
```

The stable boundary remains:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal conversation, raw protocol framing/routing, active evidence, session-owned state, input decoding, and output serialization;
- `Icod.DCurses` owns higher-level terminal UI semantics, logical composition, cells, windows, pads, retained rendering, and application-facing interaction mechanics.

`Icod.DCurses` must not grow raw OSC/CSI/DCS/APC protocol writers merely because Terminal supports those protocol families. DCurses should consume Terminal semantic operations where the higher-level curses model adds meaning.

---

## Post-1.0 development principles

1. Stable 1.x changes are additive unless an explicit compatibility decision requires otherwise.
2. Every public API addition receives a machine-readable compiled API fingerprint and human-readable regret review before stable release.
3. Semantic metadata is distinct from visual rendition. `CursesStyle` remains presentation-only.
4. Terminal protocol ownership remains in `Icod.Terminal`; DCurses does not construct private OSC/CSI/DCS/APC sequences.
5. Higher-level features must preserve Unicode/wide-cell, editing, copy/overlay, pad, lifecycle, and failure-recovery invariants.
6. Large-surface memory and allocation behavior remain release concerns; convenient APIs must not casually impose large permanent per-cell costs.
7. `Icod.DCurses` does not become a one-for-one wrapper over every `TerminalSession` semantic method.
8. The core library remains a terminal UI substrate rather than a widget toolkit.
9. A future native-curses compatibility facade should be a separate package layered over explicit `CursesSession` ownership.
10. A future widget library should be a separate package so controls can evolve independently of the stable core.
11. Raster graphics should wait for Terminal's reviewed common raster/public routing contract rather than introducing Sixel- or Kitty-specific DCurses core APIs.
12. Historical release and tranche documents remain historical and are not rewritten merely to reflect newer dependencies or release state.

---

## Stable compatibility floor

The 1.0 public contract remains the compatibility floor for the post-1.0 release train:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The current `1.0.0` source retains:

```text
Version         1.0.0
PackageVersion  1.0.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.5.0
Icod.TermInfo   1.10.0
```

The 1.1 implementation tranche must make an explicit assembly-versioning decision before its first alpha rather than changing assembly identity incidentally. The recommended policy is to keep `AssemblyVersion 1.0.0.0` through additive 1.x releases while package versions advance normally.

---

## Historical and current roadmap documents

Current authorities:

- `Icod.DCurses-Development-Roadmap.md` — this current index;
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.1.0-Development-Roadmap.md` — active detailed 1.1 implementation plan.

Stable 1.0 closure authority:

- `Icod.DCurses-1.0.0-Development-Roadmap.md`;
- `docs/Public-API-Fingerprint-1.0.json`;
- `docs/Public-API-Baseline-1.0.md`;
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`.

Historical pre-1.0 authorities remain in their existing release-specific roadmap and tranche files. The superseded original root long-form roadmap is additionally preserved verbatim under `docs/history/` for discoverability.
