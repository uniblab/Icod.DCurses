# Icod.DCurses 2.4 Widgets and dedit Foundations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the focused DCurses mechanisms that let Widgets build a responsive editor and dedit consume it, without moving widget or application policy into DCurses.

**Architecture:** Use the existing retained text/interaction substrate and one Terminal-owned session. Add narrow semantic clipboard and cursor-style facades, prove visible-slice presentation and captured interaction through public APIs, and add a new lower-layer API only after a failing consumer witness and explicit design review.

**Tech Stack:** C# 13; net8.0/net9.0/net10.0; Icod.Terminal 1.29.0; xUnit; PowerShell 5.1/cmd/sh; existing package-verifier and CI workflows.

**Spec:** [2.4 design](../specs/2026-10-10-icod-dcurses-2.4-widgets-editor-foundations-design.md). See also [versioned development roadmap](../../Icod.DCurses-2.4.0-Development-Roadmap.md).

## Global Constraints

- Production direct dependency: only `Icod.Terminal 1.29.0`, after clean published-package restore; no direct Icod.TermInfo production reference.
- `Version` and `PackageVersion` start at `2.4.0-alpha.1`; retain `AssemblyVersion 2.0.0.0`. Stable `2.4.0` only at T2409.
- C# `13.0`; `net8.0;net9.0;net10.0`; Windows/Linux/macOS qualification.
- DLL `LGPL-3.0-or-later`, samples/tests `GPL-3.0-or-later`, prescribed 2026 Timothy J. Bruce notices; no Python/C tooling.
- Clipboard read and write default independently to 65,536 decoded/encoded bytes; supported local maximum is 16,777,216 bytes. This is not remote capacity negotiation.
- Application owns event loop/time; Widgets owns document/cache/selection policy; Terminal owns protocols, query correlation, cursor baseline/restoration and output.
- Preserve 2.3.1 public behavior, no direct new Terminal type in the DCurses public API, no implicit clipboard I/O, and no file-sized pad or document cache.
- Owner alone merges, tags and publishes; green CI is not live-terminal acceptance.

## Review Focus

These five likely user-facing failure conditions have concrete tests in the owning task:

1. Externally supplied TerminalSession with a larger budget: omitted DCurses options inherit it; supplied conflicting budgets fail before ownership transfer (T2402).
2. A clipboard read that times out after a terminal ignores OSC 52: keep TimeoutException, do not claim Unsupported or corrupt subsequent input (T2402).
3. Cursor style lease acquired while the cursor is hidden: lease must not make it visible or guess a reset; nested disposal restores exact baseline (T2404).
4. Selection reaching the right edge through a wide grapheme/tab during horizontal scrolling: no split cell, stale highlight or corrupted metadata (T2405).
5. Captured drag released outside the region during modal/focus transition: deliver or explicitly cancel exactly once and do not apply the gesture to a background control (T2407).

---

## File map

| Path | Responsibility |
|---|---|
| `Icod.DCurses.csproj` | 2.4 preview/stable package identity and one direct Terminal dependency |
| `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj` | Test transport/reference alignment |
| `src/CursesClipboardSelection.cs`, `src/CursesClipboardOptions.cs` | DCurses-owned public selection and independent local budgets |
| `src/CursesSessionOptions.cs` | Session-open clipboard policy |
| `src/Integration/CursesSession.Clipboard.Terminal.cs` | Scoped forwarding to canonical Terminal session |
| `src/CursesCursorStyle.cs`, `src/CursesCursorStyleLease.cs` | Semantic cursor shape and exact-release wrapper |
| `src/Integration/CursesSession.CursorStyle.Terminal.cs` | Scoped Terminal lease acquisition |
| `src/CursesWindow.TextLayout.cs`, `src/CursesTextLayout*.cs` | Existing visible projection/geometry; modify only on witnessed gap |
| `src/CursesInteractionRouter*.cs` | Existing capture/routing; modify only on witnessed gap |
| `samples/Icod.DCurses.Editor.Sample/` | Public-only visible-slice, clipboard/caret/capture witness; no document engine |
| `tests/Icod.DCurses.Tests/src/CursesClipboard*Tests.cs`, `CursesCursorStyle*Tests.cs`, `CursesEditorViewport24Tests.cs`, `CursesEditorInteraction24Tests.cs` | Deterministic red/green witnesses |
| `tests/Icod.DCurses.Tests/src/PublicTwoFourDevelopmentIdentityTests.cs`, existing dependency/API guards | Identity and public dependency boundary |
| `tools/package-smoke/`, `tools/package-verifier/`, `docs/Public-API-*.2.4.*` | Fresh package consumption and contract freeze |
| `README.md`, `CHANGELOG.md`, `samples/README.md`, `docs/T24xx-*.md` | Durable usage, actual feature history and exact-head evidence |

Do not create every possible file at setup time. Each task introduces only its own files. Historical 2.3 fingerprints/closure records remain immutable.

### Task 1 — T2400: Published dependency and preview identity

**Files:** Modify `Icod.DCurses.csproj`, `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`, any exact-version package smoke references found by `rg -n '1\.28\.0|2\.3\.1' --glob '*.csproj' --glob '*.cs' --glob '*.ps1' --glob '*.sh' --glob '*.cmd'`; create `tests/Icod.DCurses.Tests/src/PublicTwoFourDevelopmentIdentityTests.cs`; record `docs/T2400-Dependency-and-Identity.md`.

**Interfaces:** Produces a buildable 2.4.0-alpha.1 project backed by stable Terminal 1.29.0; later tasks consume that package API.

- [ ] Write `PreviewIdentityPinsStableTerminal`: assert Version/PackageVersion `2.4.0-alpha.1`, AssemblyVersion `2.0.0.0`, exactly one production PackageReference `Icod.Terminal` at `1.29.0`, no direct TermInfo; test csproj explicitly references 1.29.0. Confirm the test fails on 2.3.1/1.28.0.
- [ ] Confirm `dotnet restore Icod.DCurses.sln --force` obtains published 1.29.0 from a clean package location; if not available, pause this task with the exact restore error and do not substitute a prerelease.
- [ ] Change version/reference identities and any exact-version package guard in the same commit; do not change AssemblyVersion or historical stable fingerprints.
- [ ] Run `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~PublicTwoFourDevelopmentIdentityTests` and `dotnet build Icod.DCurses.sln -c Staging`; expect pass and zero warnings/errors.
- [ ] Commit `build: start 2.4 preview on Terminal 1.29.0` and record exact restore/commit evidence in T2400.

### Task 2 — T2401: Clipboard API and option contract

**Files:** Create `src/CursesClipboardSelection.cs`, `src/CursesClipboardOptions.cs`, `tests/Icod.DCurses.Tests/src/CursesClipboardContractTests.cs`; modify `src/CursesSessionOptions.cs`, `src/Integration/CursesSession.Terminal.cs`.

**Interfaces:** Produces `CursesClipboardSelection { Clipboard, Primary, Secondary, Select }`; `CursesClipboardOptions` with init-only `MaximumReadBytes`/`MaximumWriteBytes` default 65_536 and limits 1..16_777_216; `CursesSessionOptions.ClipboardOptions` defaults to new options. Session-owned `OpenAsync` passes those budgets to `TerminalSessionOptions.ClipboardOptions`; external `OpenAsync` uses the Task 3 consistency rule.

- [ ] Write `ClipboardOptionsDefaultAndBoundaries` asserting both 65_536, 1 and 16_777_216 valid, 0 and 16_777_217 invalid; write `OpeningOwnedSessionPassesIndependentBudgets` with a deterministic transport/fixture.
- [ ] Run the focused tests; expect failure because DCurses options do not exist or are not forwarded.
- [ ] Add the enum/options and validate before output/session creation; snapshot each option so caller mutation cannot change an open session. Wire owned-session `OpenAsync` through Terminal's options, without eager maximum-sized allocation.
- [ ] Run `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesClipboardContractTests`; expect pass.
- [ ] Commit `feat: configure DCurses clipboard budgets` and record T2401 API decision.

### Task 3 — T2402: Clipboard forwarding and failure semantics

**Files:** Create `src/Integration/CursesSession.Clipboard.Terminal.cs`, `tests/Icod.DCurses.Tests/src/CursesClipboardIntegrationTests.cs`; extend Task 2 tests/options; update `docs/T2402-Clipboard-Integration.md`.

**Interfaces:** Produces `CursesSession.WriteClipboardAsync(CursesClipboardSelection, string, CancellationToken)`, `WriteClipboardAsync(CursesClipboardSelection, ReadOnlyMemory<byte>, CancellationToken)`, `ReadClipboardAsync(CursesClipboardSelection, TimeSpan, CancellationToken): ValueTask<byte[]>`. No new public Icod.Terminal type.

- [ ] Write failing tests for exact selection mapping and transmitted UTF-8, raw byte read, independent 64 KiB defaults and larger configured budgets; external TerminalSession options omitted/inherited and explicit conflict rejected before ownership transfer.
- [ ] Write failing failure-path tests: invalid selection, oversize before output, malformed UTF-8 text write, correlated malformed/oversize reply, ignored-query timeout followed by ordinary input, cancellation, disposal, output failure. Assert no automatic clipboard I/O on open/refresh/close.
- [ ] Run the focused `CursesClipboardIntegrationTests`; expect failures at missing methods/behavior. Implement only the mapping/forwarding and activity/lifetime semantics using existing `AcquireTerminalActivityAsync` where appropriate; never parse OSC or convert timeout to support evidence.
- [ ] Run `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesClipboard` plus the full net10 suite; expect pass. Test external-session rejection leaves ownership with its caller.
- [ ] Commit `feat: expose bounded terminal clipboard through curses session`; record exact outcomes in T2402.

### Task 4 — T2403: Cursor-style contract

**Files:** Create `src/CursesCursorStyle.cs`, `src/CursesCursorStyleLease.cs`, `tests/Icod.DCurses.Tests/src/CursesCursorStyleContractTests.cs`; create `src/Integration/CursesSession.CursorStyle.Terminal.cs`.

**Interfaces:** Produces `CursesCursorStyle` values BlinkingBlock, SteadyBlock, BlinkingUnderline, SteadyUnderline, BlinkingBar, SteadyBar; `CursesCursorStyleLease : IAsyncDisposable` with `Style`; `CursesSession.AcquireCursorStyleAsync(CursesCursorStyle style, TimeSpan timeout, CancellationToken cancellationToken = default): ValueTask<CursesCursorStyleLease>`.

- [ ] Write `StyleMappingAndLeaseSignature` assertions for six values and exact public shape; `InvalidStyleFailsBeforeOutput`; `LeaseDoesNotChangeHideCursorPolicy`.
- [ ] Run focused tests; expect missing contracts.
- [ ] Implement the enum, narrow Terminal lease wrapper and session method following existing pointer-shape lease patterns, but preserve Terminal's cursor-style query/lease behavior rather than copying cursor bytes.
- [ ] Run `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesCursorStyleContractTests`; expect pass.
- [ ] Commit `feat: define scoped curses cursor style` and record T2403 contract.

### Task 5 — T2404: Cursor-style lifecycle and fallback

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesCursorStyleIntegrationTests.cs`; modify Task 4 integration/wrapper only as the tests require; record `docs/T2404-Cursor-Style-Lifecycle.md`.

**Interfaces:** Consumes Task 4 `AcquireCursorStyleAsync`; produces stable LIFO/restoration behavior without new public methods.

- [ ] Write failing deterministic cases: real observed baseline restored after nested LIFO disposal; unsupported observation does not mutate; timeout/cancellation do not guess a reset; hidden cursor stays hidden; session close and lease disposal race does not leak style.
- [ ] Run focused tests; expect the specific lifecycle failures.
- [ ] Adjust terminal-activity/disposal coordination only where needed; propagate Terminal exceptions/status; sample caller catches unavailable style and retains a usable default caret.
- [ ] Run focused tests and full net10 suite; expect pass with no style/visibility regression.
- [ ] Commit `test: harden scoped cursor restoration`; record exact outcome in T2404.

### Task 6 — T2405: Visible-viewport evidence before adding APIs

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesEditorViewport24Tests.cs`; extend `samples/Icod.DCurses.Editor.Sample/EditorSampleState.cs` and `Program.cs` only with public-API slice projection; record `docs/T2405-Viewport-Measurement.md`.

**Interfaces:** Consume `CursesTextLayout.Create(string, CursesTextLayoutOptions)`, `GetVisualPosition`, `HitTest`, `GetSelectionRectangles`, `CursesWindow.PresentTextLayout(...)`; Widgets owns document-to-local coordinates and revision caching. Produce a documented public-only viewport mapping helper confined to the sample, not a DCurses document model.

- [ ] Write failing/characterization tests: 100,000 short logical lines must build/project only viewport slices; long single line horizontally scrolls without full-width mutation; combining/wide/tab/emoji and control characters produce legal caret/hit/selection geometry; changing selection clears old highlight and preserves metadata.
- [ ] Run focused tests to classify each failure as sample composition or irreducible DCurses gap; record baseline latency/allocation for many-line, long-line, Unicode and large-paste cases. Do not bypass a failing witness by weakening its assertions.
- [ ] Compose the public-only sample with bounded visible row slices and existing layout/projection APIs. If a required invariant cannot be met without a new public mechanism, stop this task, record exact red case, and take it to Task 7's amendment gate before production code.
- [ ] Run `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesEditorViewport24Tests`; compare measured edit-to-frame work to viewport size rather than total document length.
- [ ] Commit `test: qualify viewport-local editor projection`; record numerical budgets and whether Task 7 is needed.

### Task 7 — T2406: Conditional API gap gate

**Files:** Only the exact `src/CursesWindow.TextLayout.cs`, `src/CursesTextLayout*.cs`, or `src/CursesInteractionRouter*.cs` implicated by a Task 6/8 red witness; targeted new tests; amendment to the design and `docs/T2406-API-Gap-Decision.md`.

**Interfaces:** No public signature is preapproved here. The default deliverable is an evidence-backed `No additive viewport/interaction API required` decision. A positive gate defines exact signature, coordinate types, invalidation, ownership and tests in a reviewed spec amendment before production edits.

- [ ] If no irreducible failure exists, record public-only witness and measurements, then mark T2406 closed with no code change.
- [ ] If an irreducible failure exists, write the exact failing package-consumer test and spec amendment; request maintainer approval of the new public signature before coding it.
- [ ] After approval only, run red test, implement the smallest lower-layer mechanism, run focused and full net10 tests, and commit the reviewed API plus T2406 evidence.
- [ ] If approval is not granted, stop feature work at this gate rather than introduce a speculative API.

### Task 8 — T2407: Captured interaction and application witness

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesEditorInteraction24Tests.cs`; update `samples/Icod.DCurses.Editor.Sample/EditorSampleInteraction.cs`, `Program.cs`, `samples/README.md`; record `docs/T2407-Editor-Interaction-Acceptance.md`.

**Interfaces:** Consume existing `CursesInputEvent`, `CursesInteractionRouter`, scopes/capture, Tasks 3–6 clipboard/cursor/viewport APIs. Sample owns the event loop and elapsed-time policy.

- [ ] Write failing tests for captured outside drag/release, modal/focus cancellation, key release not triggering a second action, bracketed paste delivered as text, editor Tab versus form focus navigation, selection/scrollbar gesture separation.
- [ ] Run focused tests; expect exact failures where sample integration is missing. Reuse current router; if a DCurses routing defect is irreducible, return to Task 7's amendment gate.
- [ ] Implement public-only sample interactions with an internal clipboard fallback when host read is unavailable; explicit visible-caret option and scoped style; cancel capture on focus/modal/region lifecycle.
- [ ] Run focused and full net10 tests; run sample in a real terminal where available and record actual observed behavior, not assumed clipboard support.
- [ ] Commit `sample: exercise editor foundation through public APIs`; record T2407 environment/result.

### Task 9 — T2408: Contract, package, documentation and adversarial gate

**Files:** Update `tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs`, `PublicApiFingerprintTests.cs`, `PublicTwoFourDevelopmentIdentityTests.cs`, `tools/package-smoke/`, `tools/package-verifier/`; create `docs/Public-API-Baseline-2.4.md`, `docs/Public-API-Fingerprint-2.4.json`, `docs/T2408-API-Package-Documentation-Gate.md`; update `README.md`, `CHANGELOG.md`, `samples/README.md` and package notes.

**Interfaces:** Freeze additive public API over 2.3.1; fresh consumer declares only DCurses and restores Terminal 1.29 transitively.

- [ ] Add failing package/identity tests for one direct production Terminal 1.29 reference, absence of new public Terminal types/TermInfo, correct DLL/framework/README/license contents, and runnable clipboard/cursor public calls from a NuGet-only consumer.
- [ ] Run tests against the pre-gate package to verify red, then update verifier/consumer and current docs. Preserve historical fingerprints; generate/review 2.4 baseline and compare exact changes. README uses full GitHub URLs for docs not packaged and an actual runnable sample command.
- [ ] Run `dotnet build Icod.DCurses.sln -c Staging`, `dotnet test Icod.DCurses.sln -c Staging --no-build`, `dotnet pack Icod.DCurses.csproj -c Release`, then existing `build.cmd`/`build.sh` validation on supported hosts; expect all three TFMs and package-only consumer pass.
- [ ] Record allocation/latency budgets/results, Windows/Linux/macOS CI status, live limitations and exact package hash in T2408. Do not claim a platform live-tested by a headless runner.
- [ ] Commit `build: freeze 2.4 API and package contract` only after a reviewable regret gate.

### Task 10 — T2409: RC, stable candidate and handoff

**Files:** `Icod.DCurses.csproj`, release identity guards, `README.md`, `CHANGELOG.md`, `docs/T2409-2.4.0-Stable-Source-Gate.md`; workflows only if an actual gate change is required.

**Interfaces:** Produce stable source/package Version and PackageVersion `2.4.0` with AssemblyVersion `2.0.0.0`; preserve Terminal 1.29.0 and frozen public contract.

- [ ] First qualify `2.4.0-rc.1` from the T2408 feature-frozen source. Assert only identity/release-document changes versus frozen behavior and run package/public-consumer CI on exact RC head.
- [ ] Add failing stable-identity assertions, then promote to `2.4.0` in project, package metadata, install instructions and version guards; remove prerelease language from packaged README.
- [ ] Run clean restore/build/test/pack/validate and confirm exact candidate head, all CI jobs, package contents and per-framework consumer results. Record available live-terminal evidence; request an explicit scoped waiver for unverified environments if necessary.
- [ ] Commit the stable-source candidate and present PR, version, digest and remaining caveats to owner. Do not merge, tag or publish; owner performs post-merge validation and publication.

## Plan self-review and execution rule

Coverage map: dependency/version T2400; clipboard contract/lifecycle T2401–T2402; cursor T2403–T2404; text/overlays T2405–T2406; interaction T2407; adversarial/package/docs T2408; RC/stable T2409. No text-buffer, widget or file-policy task is hidden in DCurses. All public signatures defined here match the design; conditional signatures require an approved amendment before coding.

At each task: write failing test → run red → smallest implementation → focused/full green → exact evidence → commit. The implementation method is native/sequential per maintainer preference; do not dispatch subagents. The roadmap remains a plan until the maintainer approves execution.
