# Icod.DCurses 2.0 Development Public API Baseline

**Development package identity:** `2.0.0-alpha.1`.\
**AssemblyVersion:** `2.0.0.0`.\
**Production dependencies:** `Icod.Terminal 1.18.0`; temporary direct `Icod.TermInfo 1.15.0`.\
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`.\
**Status:** T2002 development baseline; not a stable release contract.

## Compiled fingerprint

```text
75 exported types
559 canonical declared contract lines
sha256 1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65
```

`docs/Public-API-Fingerprint-2.0.json` is the machine-readable authority. Its
exported type names are identical to the frozen 1.6 list. Historical 0.9-1.6
fingerprints remain unchanged.

## Approved 1.6-to-2.0 delta

| Action | 1.6 contract | 2.0 development contract |
|---|---|---|
| Replace property | `CursesSession.Terminal : Icod.TermInfo.TerminalDescription` | `CursesSession.Profile : Icod.Terminal.TerminalProfile` |
| Change return type | `CursesSession.GetDimensions() : TerminalControlResult<Icod.TermInfo.TerminalSize>` | `CursesSession.GetDimensions() : TerminalControlResult<Icod.Terminal.TerminalDimensions>` |
| Change return type | `CursesSession.SynchronizeDimensions() : TerminalControlResult<Icod.TermInfo.TerminalSize>` | `CursesSession.SynchronizeDimensions() : TerminalControlResult<Icod.Terminal.TerminalDimensions>` |
| Change property type | `CursesLifecycleEvent.Dimensions : Icod.TermInfo.TerminalSize?` | `CursesLifecycleEvent.Dimensions : Icod.Terminal.TerminalDimensions?` |
| Assembly identity | `1.0.0.0` | `2.0.0.0` |

No exported type name, unrelated signature, enum value, nullability annotation,
parameter default, or generic constraint changes in T2002. The recursive public
dependency guard permits only approved `Icod.Terminal` types and finds no public
`Icod.TermInfo` type.

## Behavioral contract

`CursesSession.Profile` returns the exact Terminal-owned profile for the underlying
session. Live dimensions delegate to `TerminalSession.GetDimensions()`, preserving
available values plus unavailable, unsupported and failed status/message/native-error
metadata. Synchronization resizes and invalidates the logical screen only for an
available result. Lifecycle resize/resume dimensions use Terminal-owned positive
`TerminalDimensions`; events without observed dimensions remain nullable.

## Remaining migration debt

The 2.0 public boundary is Terminal-only, but the 1.6 renderer remains in place at
this checkpoint. Production still directly references TermInfo 1.15.0 and retains an
internal `TerminalDescription` seam for capability interpretation and raw output.
T2003-T2006 migrate presentation, optimization, transactions and recovery; T2007
removes the direct package/source/shim coupling and proves the final package boundary.
