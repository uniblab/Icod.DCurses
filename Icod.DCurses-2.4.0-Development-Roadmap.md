# Icod.DCurses 2.4.0 Development Roadmap

**Decision:** Focused Widgets and dedit foundations; approved in conversation on 2026-10-10 (America/New_York)  
**State:** Planned, not implemented or published  
**Base:** Stable Icod.DCurses 2.3.1, AssemblyVersion 2.0.0.0  
**Target dependency:** Direct Icod.Terminal 1.29.0 after clean published-package restore; no direct Icod.TermInfo reference  
**Targets:** net8.0, net9.0, net10.0; C# 13; Windows/Linux/macOS  
**Design authority:** [2.4 design](docs/superpowers/specs/2026-10-10-icod-dcurses-2.4-widgets-editor-foundations-design.md)  
**Execution detail:** [2.4 implementation plan](docs/superpowers/plans/2026-10-11-icod-dcurses-2.4-widgets-editor-foundations.md)

## 1. Goal and acceptance witnesses

Deliver the lower-layer mechanisms that Icod.Widgets needs to build a responsive text editor and dedit needs to compose a modeless, tabbed application. The baseline is already substantial: DCurses 2.3 supplies Unicode layout and source/visual geometry, retained projection and damage, normalized rich input, deterministic scopes and pointer capture. Version 2.4 must integrate Terminal 1.29 clipboard/cursor facilities through one DCurses session and demonstrate bounded visible-region rendering and captured interaction without absorbing control or application policy.

The release succeeds when a public-only sample can show a visible editor slice, caret and selection; write/read bounded optional clipboard content with an internal fallback in its caller; drag outside a region and cancel capture safely; resize and close without leaking terminal state. A package-only consumer must compile/run against the released candidate for each target framework. CI evidence is distinct from live terminal acceptance.

No claim of a Widgets multiline editor or dedit executable is made by this roadmap.

## 2. Boundary and dependency contract

| Layer | Owns in this release |
|---|---|
| Icod.Terminal 1.29.0 | OSC 52 framing/query correlation, independent local read/write byte budgets, cursor-style observation/lease/restoration, host I/O and lifecycle |
| Icod.DCurses 2.4.0 | Semantic session facades, retained visible-cell projection/damage, existing Unicode geometry, deterministic routing/capture mechanisms |
| Icod.Widgets | Document model, view state/revision caches, focus/control policy, clipboard fallback, tabs, dialogs, selection and autoscroll timing |
| dedit | Event loop, commands, file I/O, encoding/EOL, safe save/close and multiple-document lifetime |

Direct production graph stays `DCurses -> Terminal -> TermInfo`; Widgets has only a direct DCurses reference. No direct TermInfo package/source/metadata reference may re-enter DCurses. Preserve the published 2.3.1 API and AssemblyVersion 2.0.0.0. A 2.4 preview uses Version and PackageVersion `2.4.0-alpha.1` together; promotion to RC/stable is an explicit later gate, not a roadmap edit.

Before the Terminal upgrade, check that `Icod.Terminal 1.29.0` restores from NuGet on a clean cache. Change production and tests deliberately and verify package metadata; do not silently accept an unpublished version or hand-edit the transitive dependency graph. Terminal budgets are local limits, not a remote buffer negotiation.

## 3. Chosen feature set

### Session clipboard

Expose `CursesClipboardSelection`, `CursesClipboardOptions`, and explicit read/write methods on `CursesSession`. Independent read/write defaults are 65,536 bytes, each configurable to at most 16,777,216 bytes before opening an owned Terminal session. Strict UTF-8 text write and raw byte read retain Terminal's behavior and its cancellation, timeout, format/error distinctions. A supplied TerminalSession's existing budgets are authoritative; omitted DCurses options inherit them and conflicting supplied DCurses budgets fail before ownership transfer. Writes are not host-acceptance acknowledgements; a timed-out read is not proof of lack of support. DCurses neither polls nor performs implicit clipboard I/O.

### Scoped cursor style

Expose a semantic `CursesCursorStyle` and `CursesCursorStyleLease`, forwarding to Terminal's exact-baseline reversible style lease. Keep style separate from visibility: the existing default `HideCursor = true` does not change implicitly. Tests cover unsupported observation, nested LIFO release, cancellation/timeout, disposal, and usable fallback. No guessed reset escape sequence or style is permitted.

### Visible text projection

Use existing `CursesTextLayout`, geometry and `CursesWindow.PresentTextLayout` first, with Widgets supplying bounded visible line slices and revision-aware cache. Prove caret/hit/selection coordinates, wide-cell coherence, safe display of controls, old-highlight clearing, resize, and scrolling. The DCurses working set must depend on the visible window and bounded context, not document size; no file-sized pad or hidden document cache. If a specific regression witness demonstrates an irreducible lower-layer gap, review a narrowly scoped additive public API with exact coordinates, clipping and invalidation semantics before implementation. Without such evidence, the 2.4 viewport deliverable is a qualified use of existing public APIs, not a speculative new type.

### Interaction integration

Exercise current function-key/phase/paste/focus event data, scopes and pointer capture. Widgets owns Tab-vs-traversal, menu commands, click count, autoscroll and elapsed-time policy. An observed captured-release or cancellation gap may justify a small deterministic DCurses mechanism only after an exact failing event trace and API review. No private timer, event loop, callback dispatcher or implicit focus policy enters DCurses.

## 4. Testable tranches

| Tranche | Deliverable | Exit evidence |
|---|---|---|
| T2400 | Baseline, 1.29 restore and development identity | Clean NuGet restore, one direct Terminal 1.29 reference in production, test/project/package guards, 2.4.0-alpha.1 identity, 2.0.0.0 assembly |
| T2401 | Clipboard contract and red witnesses | Frozen semantic enum/options/session signatures; tests initially fail for default/bounds/external-session mismatch and no implicit I/O |
| T2402 | Clipboard integration and hardening | Owned/external sessions, byte/text operations, 64 KiB and opt-in larger budgets, cancellation/timeout/malformed/oversize/disposal proven against deterministic transport |
| T2403 | Cursor style contract and red witnesses | Semantic style/lease signatures frozen; visibility independence, unsupported baseline and nested restoration tests initially fail |
| T2404 | Cursor style integration and hardening | Terminal lease forwarding, LIFO cleanup, session-close races, unsupported/fallback behavior pass |
| T2405 | Viewport baseline and measured public-only witness | Existing APIs handle visible slices, coordinate mapping and overlays; many-line/long-line/Unicode/paste workload and allocation evidence recorded |
| T2406 | Conditional lower-layer gap closure | Only if T2405 or interaction acceptance reveals an irreducible gap: amend design/API gate, add exact red test, smallest additive mechanism and proof; otherwise record no new API required |
| T2407 | Captured interaction and sample acceptance | Outside drag/release, focus/modal cancellation, paste as data, key phases, visible cursor/clipboard fallback/resize in a public-only sample |
| T2408 | Adversarial, package and API regret gate | Full framework matrix, package-only consumers, baseline/fingerprint, documentation, dependency/licensing audit and performance budgets |
| T2409 | RC/stable-source and maintainer handoff | Exact-head CI plus scoped live-terminal evidence, version 2.4.0 and package metadata aligned; owner handles merge, main validation, tag and publication |

T2406 is a gated branch, not a promise to invent a new viewport or gesture API. Any additive API enters before the T2408 freeze and must remain compatible with 2.3.1. No task may mark a later gate accepted solely because an earlier build was green. Each tranche records commit, tests, limitations, package identity and follow-up in `docs/T24xx-*.md`.

## 5. Verification matrix

- Deterministic unit/integration tests: clipboard limits, strict Unicode, selection mapping, external-session option consistency, cancellation, timeout, malformed/oversized replies, failures after output, disposal; cursor baseline/nesting/unsupported; viewport Unicode/wrap/selection/resize; event capture/focus/modal/paste traces.
- Staging/Release warnings-as-errors on net8.0/net9.0/net10.0; Windows, Linux and macOS runners; existing solution and package scripts. If an architecture lane is not available, state that limitation rather than infer it passed.
- Build a candidate package and install it into a clean consumer with only `Icod.DCurses` declared; inspect actual nuspec dependency group, DLL contents, XML docs, license and README. Confirm no direct TermInfo and no new public Terminal types beyond the existing approved boundary.
- Benchmark representative many-line, long-line, Unicode-heavy and large-paste workloads. Record baseline, target budget and measured result before release; no unsupported `unlimited file` claim.
- Live terminal acceptance records actual OS, terminal/version, architecture, .NET runtime, source commit, clipboard consent and observed scenario. CI cannot prove host clipboard policy or visible cursor fidelity. Missing macOS/Linux live checks are unverified, with any release waiver explicit and scoped.

## 6. Documentation, samples and release gates

The root README remains a durable package README: installation of the published version, runnable sample, explicit ownership/fallback explanations, and full GitHub URLs for linked documentation not shipped in the package. The CHANGELOG reports only implemented features. Update sample README with command, prerequisites, focus/clipboard/resize behavior and known terminal limitations. Keep readiness/evidence in tranche docs, not a lengthy Status section.

T2408 compares the public contract against the frozen 2.3 baseline, reviews output/state restoration and confirms version, dependency and package identity. T2409 runs exact-head PR qualification, promotes an RC only after the earlier gates, then prepares stable 2.4.0 source. The maintainer merges, waits for main workflow, tags and publishes. No planning commit authorizes those actions.

## 7. Explicit non-goals and later work

Do not add multiline document storage/undo, a tab control, interactive scrollbars, dialog/file-picker policies, editor-specific command mappings, file codecs, safe save/close, auto-recovery, native clipboard access, new OSC protocol handling, physical sprite coordination, terminal emulator or PTY hosting. Those belong in Widgets, dedit, Terminal or a separately approved track. Do not reopen 2.3.1 maintenance or rewrite historical release evidence.

## 8. Immediate next action

Review this roadmap and the linked implementation plan. After approval, implement T2400 first and verify the published Terminal 1.29 package through clean restore. Then proceed tranche by tranche under the plan's red/green/review gates.
