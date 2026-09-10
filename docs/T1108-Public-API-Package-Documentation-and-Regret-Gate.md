# T1108 — Public API, Package, Documentation, and Regret Gate

**Project:** `Icod.DCurses`  
**Release:** `1.1.0`  
**Development package after closure:** `1.1.0-alpha.8`  
**Stable compatibility floor:** `1.0.0`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** pre-RC contract accepted; documentation-synchronized alpha.8 exact-head qualification pending

---

## 1. Purpose

T1108 is the final public-contract regret gate before `1.1.0-rc.1`.

T1101–T1107 established representation, logical semantics, physical rendering, structural propagation, failure recovery, and application/performance acceptance. T1108 asks a different question: if the current alpha API shipped as stable 1.1 today, would any avoidable naming, nullability, source-compatibility, packaging, documentation, or dependency mistake become a permanent 1.x burden?

The answer identified two deliberate pre-RC API refinements and several documentation/package-acceptance corrections. No stable 1.0 contract is changed.

---

## 2. Compiler-derived accepted 1.1 contract

The provisional T1103 fingerprint was intentionally left mutable until this gate.

After the T1108 refinements, the compiled surface is:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

The fingerprint was independently reproduced by `net8.0`, `net9.0`, and `net10.0` during workflow #534 before the baseline file was updated. In each target framework the only failing test was the deliberately stale fingerprint assertion; all other 485 tests passed.

Exactly two exported types remain new relative to stable 1.0:

- `CursesHyperlink`;
- `CursesCellMetadata`.

No additional Terminal or TermInfo type enters the public signature surface.

---

## 3. Regret correction: semantic convenience name

The provisional T1103 API contained:

```text
CursesWindow.Write(string, CursesCellMetadata)
```

Stable 1.0 already contains:

```text
CursesWindow.Write(string, CursesStyle)
```

That apparently additive overload introduces a source-compatibility regression. Previously valid 1.0 source such as:

```csharp
window.Write( "text", default );
```

becomes ambiguous when recompiled because `default` is convertible to both `CursesStyle` and `CursesCellMetadata`.

T1108 therefore renames only the two-argument semantic convenience to:

```text
CursesWindow.WriteWithMetadata(string, CursesCellMetadata)
```

The explicit three-argument semantic operation remains:

```text
CursesWindow.Write(string, CursesStyle, CursesCellMetadata)
```

and `WriteCell(CursesCell, CursesCellMetadata)` remains unchanged.

`PublicSemanticMetadataRegretTests` compiles the stable `Write("plain", default)` source form alongside `WriteWithMetadata(...)` so the ambiguity cannot silently return.

This is a pre-RC correction to a provisional alpha API, not a breaking change to any published stable DCurses contract.

---

## 4. Regret correction: metadata-field nullability

The provisional T1103 `CursesCellMetadata.Hyperlink` property was non-nullable.

That accurately described every 1.1-constructible metadata value, but it would make the container unnecessarily hostile to later additive semantic kinds. If a future metadata value represents another semantic concept without a hyperlink, the property would have to change from non-nullable to nullable after publication.

T1108 instead freezes:

```text
CursesCellMetadata(CursesHyperlink hyperlink)  // hyperlink required in 1.1
CursesHyperlink? Hyperlink                     // property forward-compatible
```

The 1.1 constructor still throws `ArgumentNullException` for null and every value constructible through that constructor still contains a hyperlink. The nullable property reserves room for future metadata kinds without later weakening an established return contract.

The record remains immutable and retains value equality semantics.

---

## 5. Public naming, equality, validation, and ownership review

The remaining semantic API is accepted without further pre-RC changes.

### `CursesHyperlink`

Accepted:

- DCurses-owned immutable record;
- canonical absolute encoded-ASCII URI string;
- optional identifier;
- percent-escape normalization;
- Terminal-compatible validation bounds/grammar;
- no URI activation or navigation behavior.

### `CursesCellMetadata`

Accepted:

- DCurses-owned immutable record;
- separate from visual `CursesStyle`;
- no untyped metadata dictionary;
- no Terminal protocol object or lease exposure;
- reference-value sharing remains suitable for the sparse sidecar.

### Surface operations

Accepted:

```text
CursesVirtualScreen.GetMetadata(...)
CursesVirtualScreen.SetMetadata(...)
CursesWindow.GetMetadata(...)
CursesWindow.SetMetadata(...)
CursesWindow.WriteWithMetadata(...)
CursesWindow.Write(string, CursesStyle, CursesCellMetadata)
CursesWindow.WriteCell(CursesCell, CursesCellMetadata)
```

Coordinates, validation, clipping, wide-cell behavior, ordinary-overwrite clearing, and shared-window projection remain aligned with existing curses semantics.

---

## 6. Dependency-boundary review

The stable allow-list remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

`PublicDependencyBoundaryTests` rejects any additional lower-layer public type definition.

The new semantic values contain no `TerminalHyperlink`, `TerminalHyperlinkLease`, protocol frame, router, output, or capability implementation type.

Terminal remains authoritative for live OSC 8 framing, output serialization, synchronized-output state, lifecycle ordering, and final protocol cleanup.

---

## 7. Package-only semantic consumer

The fresh NuGet-only smoke consumer previously covered the stable screen/window/input/Unicode/pad surface but did not instantiate the 1.1 semantic types.

T1108 extends it to compile and execute:

- `CursesHyperlink` construction and URI canonicalization;
- `CursesCellMetadata` construction;
- `WriteWithMetadata(...)`;
- metadata inspection through both window and virtual-screen coordinates;
- metadata removal;
- metadata reassignment.

The consumer continues to restore from the generated local `Icod.DCurses` package rather than a repository project reference and resolves Terminal/TermInfo through the package dependency graph.

---

## 8. Sample/documentation review

The minimal `Icod.DCurses.Sample` now includes one retained hyperlink line using:

```text
CursesHyperlink
CursesCellMetadata
WriteWithMetadata(...)
```

This demonstrates the 1.1 feature without teaching raw OSC 8 or turning the quick-start sample into a protocol-specific showcase.

`samples/README.md` documents that Terminal remains the hyperlink framing owner.

The documentation audit also found two T1103 files describing the same tranche. The earlier `docs/T1103-Hyperlink-and-Public-Semantic-Metadata-Contract.md` was an implementation-staged draft and is removed. `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md` remains the single authoritative T1103 record and is synchronized to the final pre-RC convenience name/nullability decision.

---

## 9. XML/package validation

The package project continues to generate XML documentation for all three target frameworks.

The established package verifier continues to require:

- synchronized package/assembly identity;
- `net8.0`, `net9.0`, and `net10.0` assemblies;
- XML documentation payloads;
- portable PDBs and symbol package;
- exact `Icod.Terminal 1.6.0` / `Icod.TermInfo 1.10.0` dependency groups;
- README, license, icon, repository, and license-acceptance metadata;
- absence of accidentally bundled dependency/runtime/repository-only payloads;
- fresh package-only consumer compilation and execution.

No T1108 public diagnostic or packaging-only runtime API is introduced.

---

## 10. Stable 1.0 compatibility remains independent

The published stable floor remains:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Historical 0.9/1.0 baseline files and freeze tests remain unchanged.

The compatible additive 1.x assembly policy likewise remains:

```text
AssemblyVersion 1.0.0.0
```

---

## 11. Accepted pre-RC public baseline

The machine-readable authority is:

- `docs/Public-API-Fingerprint-1.1.json`

The human-readable companion is:

- `docs/Public-API-Baseline-1.1.md`

The accepted public contract after T1108 is expected to be promoted unchanged into T1109 unless a release-blocking defect is discovered before RC.

---

## 12. Exit gate

T1108 is complete when one documentation-synchronized `1.1.0-alpha.8` source head passes all seven PR jobs:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

That exact head must prove the accepted `21dff2e5...` fingerprint, the stable dependency boundary, all semantic/failure/application acceptance tests, package-only semantic consumption, XML/package verification, and synchronized documentation/package identity.

After that, T1109 may promote the accepted contract to `1.1.0-rc.1` without adding new feature work.
