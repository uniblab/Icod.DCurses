# T905 — Nullable, Documentation, and Dependency-Boundary Regret Review

**Release line:** `0.9.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Status:** accepted for the 1.x contract

## Nullable contract

Nullable reference types remain enabled for the library and test projects.

The 0.9 canonical public API fingerprint includes compiled nullability state for public fields, properties, events, method returns, and parameters (including nested generic/array element nullability where reflection exposes it). A nullable-signature change therefore changes the fingerprint and requires explicit review.

The ordinary compiler/analyzer gate remains warnings-as-errors under Staging and Release. No blanket nullable-warning suppression is introduced for 0.9.

## XML documentation contract

`GenerateDocumentationFile` remains enabled for the package project. Staging/Release compile the complete public surface with the established warning policy, and package validation requires the XML documentation payload for `net8.0`, `net9.0`, and `net10.0` alongside each library assembly.

The 0.9 review accepts the current public type/member naming and documentation shape as the starting 1.x contract. T906 expands conceptual and migration documentation rather than duplicating every XML summary in the README.

## Public mutability and implementation leakage

The T901 inventory and T902 fingerprint expose every public constructor, public field/constant, property accessor, event, and declared method. No exported hardening helper, synchronization primitive, transport abstraction, physical-screen state object, refresh resolver, cost model, test seam, or diagnostics record is present.

No new public mutable collection or public lock/token/scheduler is justified by the 0.9 review.

## Upstream public dependency types

`PublicDependencyBoundaryTests` currently permits exactly five upstream type definitions. T905 reviews each one individually.

### `Icod.Terminal.TerminalSession` — keep

The advanced `CursesSession.OpenAsync(TerminalSession, ...)` overload is an intentional integration seam for a caller which already owns/configures the live Terminal session. It prevents DCurses from inventing a parallel low-level session abstraction and is central to the Terminal/DCurses ownership architecture.

The ownership-transfer behavior is frozen in T904.

### `Icod.Terminal.TerminalEndpoint` — keep

`CursesSession.InputEndpoint` and `OutputEndpoint` expose the identity of the actual Terminal endpoints selected by the lower-layer owner. Replacing these with duplicate DCurses endpoint enums/records would create a redundant vocabulary with no curses-specific semantics.

### `Icod.Terminal.TerminalControlResult<T>` — keep

`CursesSession.GetDimensions()` returns the lower-layer availability/result contract rather than throwing or erasing the distinction between an available live size and a controlled unavailable result. DCurses has no useful independent generic result abstraction to add here.

### `Icod.TermInfo.TerminalSize` — keep

`TerminalSize` is the payload of the live-size result. It is the canonical immutable terminal dimensions value already used by Terminal; duplicating it would introduce conversion-only surface and ambiguity over rows/columns ownership.

### `Icod.TermInfo.TerminalDescription` — keep as the advanced escape hatch

`CursesSession.Terminal` exposes the immutable selected terminal description. Ordinary applications should use curses-level presentation/capability APIs, but advanced applications and diagnostics may need the canonical TermInfo description. This property has been an explicitly reviewed low-level escape hatch since the early stable line.

DCurses does not mutate the description or create a second capability database.

## Rejected additional upstream exposure

No additional Terminal or TermInfo public type is accepted by T905. In particular, Terminal input-decoder types, presentation/protocol lease implementations, lifecycle participant types, output abstractions, mode snapshots, capability writer/resolver types, and TermInfo raw capability internals remain below the public DCurses boundary unless a future major-version design deliberately changes that architecture.

## Regret decision

The five-type allow-list remains unchanged for 0.9/1.0 because each exposure either preserves the authoritative lower-layer ownership vocabulary or provides an intentional advanced integration escape hatch. Removing them now would force duplicate DCurses concepts or block legitimate advanced composition without improving ordinary API safety.

The machine gates remain complementary:

- `PublicApiFingerprintTests` freezes the complete declared public contract, including nullability and exact member signatures;
- `PublicDependencyBoundaryTests` fails if any new Terminal/TermInfo type definition leaks into a public DCurses signature;
- Staging/Release warnings-as-errors protect compiler/analyzer quality;
- the package verifier requires XML documentation and exact dependency/package metadata for every target framework.

No breaking cleanup is deferred from this review to `1.0.0`.
