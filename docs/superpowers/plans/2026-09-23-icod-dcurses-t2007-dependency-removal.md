# Icod.DCurses T2007 Dependency Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans in the current session. Steps use checkbox (`- [ ]`) syntax; do not delegate to subagents.

**Goal:** Make `Icod.Terminal 1.18.0` the sole direct terminal package dependency of DCurses and permanently detect production, metadata, and package regressions.

**Architecture:** Keep test-only TermInfo builders for fixture construction. Remove the production package reference, prove compiled DCurses has no `Icod.TermInfo` assembly or TypeRef, and compare each packed target-framework dependency group with an independently frozen one-dependency policy. Preserve the 2.0 public API and Terminal-owned output transaction.

**Tech Stack:** C# 13, xUnit, `System.Reflection.Metadata`, .NET 8/9/10, PowerShell 5.1-compatible release workflow. No Python.

**Spec:** `Icod.DCurses-2.0.0-Development-Roadmap.md` T2007, `docs/2.0-TermInfo-Coupling-Inventory.md`, `docs/T2006-Lifecycle-and-Recovery-Gate.md`.

## Global Constraints

- Start only after T2006 accepts an exact head. Keep version/package identity `2.0.0-alpha.1`, assembly `2.0.0.0`, and Terminal minimum `1.18.0`.
- Production code may reference Terminal semantics, not TermInfo types, capability IDs, parameter expansion, raw output or direct flush. TermInfo may restore transitively and stay a direct test-only fixture dependency.
- Keep the root README attribution/license and `docs/Public-API-Fingerprint-2.0.json` unchanged. No merge, tag, release, or publication.

## Review Focus

1. A newly added production TermInfo `using` or fully qualified type fails the source check before package validation.
2. A direct TermInfo package reference fails even when a matching extra nuspec dependency would otherwise make project-versus-package comparison pass.
3. A compiled private TermInfo TypeRef or assembly reference fails even when the exported API contains no such type.
4. TermInfo still appears in the transitive restore graph through Terminal without failing a direct-dependency guard.
5. Every `net8.0`, `net9.0`, and `net10.0` nuspec group contains exactly `Icod.Terminal` at `1.18.0`.

---

### Task 1: Freeze independent source/project/assembly checks

**Files:** Create `tests/Icod.DCurses.Tests/src/T2007DependencyBoundaryTests.cs`; retain `T2005TransactionalBoundaryContractTests.cs` for its existing raw-output guard.

**Interfaces:** Consume repository root, `Icod.DCurses.csproj`, and `typeof(CursesSession).Assembly.Location`; produce independently testable assertions for the production source, XML PackageReference list, assembly references, and metadata TypeRefs.

- [ ] Write source negative control: feed a test string containing `Icod.TermInfo.TerminalDescription` to the same source-token predicate used by the production scan, and assert rejection. Preserve the T2005 production-wide check.
- [ ] Write project negative control: inject a TermInfo PackageReference into an in-memory project XML sample and assert rejection; assert the real production project declares exactly `Icod.Terminal` at `1.18.0`. Do not derive the expected set from the project.
- [ ] Write assembly negative controls on a metadata reader helper: synthetic assembly-reference and type-reference names containing `Icod.TermInfo` must be rejected. Examine the actual compiled DCurses PE metadata for both tables. Keep test-only fixture assembly references outside this check.
- [ ] Run focused tests before removal; the real project dependency assertion must fail while the existing TermInfo reference remains. Remove only the production `Icod.TermInfo` PackageReference and rerun. If compiled metadata reveals a leak, migrate the owning production call through Terminal instead of suppressing the guard.

### Task 2: Enforce packed and release dependency policy

**Files:** Modify `tools/package-verifier/Program.cs`, `.github/workflows/release.yaml`, `Icod.DCurses.csproj`; adapt `tools/package-smoke/Program.cs` only to remove unused direct TermInfo symbols. Keep its project reference set to the packed DCurses package alone.

**Interfaces:** Consume `.nupkg` nuspec groups and direct project references; produce an independent expected set `{ Icod.Terminal = 1.18.0 }` in each framework group.

- [ ] Create verifier negative control for a project with an extra TermInfo reference and a nuspec with an extra TermInfo dependency: both must fail independently even when they match each other.
- [ ] Replace `ReadProjectDependencyIds`'s variable self-derived expectation with an exact Terminal-only versioned policy; require it for each nuspec TFM group. Do not reject TermInfo in the transitive restore graph.
- [ ] Change release metadata validation to require one Terminal PackageReference at the qualified version and reject a direct TermInfo reference; remove the TermInfo output and direct-dependency release-note wording. This workflow must remain tag-only and must not be triggered during T2007.
- [ ] Remove the package-only smoke's direct TermInfo `using`/type references; retain public Terminal and DCurses compile witnesses. Run packaging validate in CI.

### Task 3: Exact-head negative controls and gate

**Files:** Create `docs/T2007-TermInfo-Dependency-Removal-Gate.md`; update `Icod.DCurses-2.0.0-Development-Roadmap.md` and PR body after acceptance.

**Interfaces:** Consume focused negative-control results, packed artifact metadata, package-only smoke, and six runtime jobs; produce T2007 acceptance evidence.

- [ ] Verify the three negative controls fail for their deliberate mutations and pass after removal; record commands and precise failure diagnostics.
- [ ] Run the exact-head package job and all six runtime jobs on each of .NET 8/9/10. Inspect the packed nuspec and compiled assembly references for all TFMs; retain T2006's API fingerprint.
- [ ] Record SHA, workflow URL, counts/failures/skips, direct versus transitive TermInfo behavior, protected file hashes and remaining T2008 consumer/documentation work. Advance the roadmap only after the complete gate passes.

## Self-review

The project, compiled assembly, package, and release metadata have independent expected policies. A tautological project-versus-package equality cannot admit a direct TermInfo dependency. Fixture-only TermInfo remains separately scoped, and the next consumer migration belongs to T2008.
