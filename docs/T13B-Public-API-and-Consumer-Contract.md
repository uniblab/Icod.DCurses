# T13B — Public API and Consumer Contract Regret Review

**Project:** `Icod.DCurses`
**Development line:** `0.1.0`
**Development version:** `0.1.0-Alpha-21`
**Tranche:** T13B — public API and consumer-contract regret review
**Reference branch:** `0.1.0`
**Reference commit before tranche:** `0a2bcc39b4e5459da9910ac9cf42a6949db1406f`
**Status:** Implementation prepared; validation gate pending

---

## 1. Purpose

T13A established the package-only and tag-controlled release machinery.

T13B reviews the public 0.1 consumer contract before the stable version is
assigned. It is intentionally conservative: this tranche does not add a new
feature family.

## 2. Public API review result

No public type or member requires removal or renaming before the 0.1 release.

The review accepts the existing division:

- `CursesSession` owns the live curses presentation;
- `CursesEvent` and `CursesInputEvent` carry semantic application input;
- rich-input protocols are requested through curses-shaped reversible leases;
- `CursesScreen`, `CursesVirtualScreen`, and `CursesWindow` own logical display
  state;
- `CursesCell`, `CursesStyle`, and `CursesColor` own semantic cell/rendition
  state;
- `Icod.Terminal` remains the live-terminal substrate;
- `Icod.TermInfo` remains the immutable capability authority.

The release-line baseline is recorded in
[`Public-API-Baseline-0.1.md`](Public-API-Baseline-0.1.md).

## 3. Intentional lower-layer public types

The audit specifically reviewed public signatures which expose
`Icod.Terminal`/`Icod.TermInfo` types.

They are retained intentionally in 0.1 because they preserve useful lower-layer
semantics without forcing DCurses to invent duplicate wrappers:

- advanced session injection accepts `TerminalSession`;
- endpoint identity uses `TerminalEndpoint`;
- live-size/control results use `TerminalControlResult<TerminalSize>`;
- the selected immutable terminal description is exposed as
  `TerminalDescription`.

The ordinary application path does not require direct use of these lower-level
types.

## 4. Contract-preserving correction

One implementation defect was found in the direct public
`CursesCell(string, CursesStyle)` constructor.

The prior constructor dereferenced `content.Length` in its constructor
initializer before the internal null validation could run. Passing null
therefore produced an accidental `NullReferenceException`.

Alpha-21 performs deterministic entry validation and throws
`ArgumentNullException`, consistent with the repository contract.

The constructor's documentation is also clarified: direct cell construction is
a low-level one-column operation. Normal text should be written through
`CursesWindow`, where the configured width provider owns multi-column and
combining behavior.

No public signature changes.

## 5. Required sample audit

The roadmap requires `Icod.DCurses.Sample` itself to demonstrate:

- session creation;
- styled output;
- a movable or updating region;
- input;
- resize;
- clean exit.

Before T13B the sample covered session/input/resize/exit but its output was
static and unstyled.

Alpha-21 adds a small styled marker updated by timeout-shaped
`ReadEventAsync(...)` waits. The sample remains intentionally small and exits on
ordinary input.

## 6. Documentation cleanup

The root roadmap still contained provisional text describing
`Icod.CommandFramework.Terminal` as the present terminal substrate even though
the T10 migration has long been complete.

T13B rewrites the active architecture/dependency sections around
`Icod.Terminal`. Historical tranche descriptions remain historical where they
explain how the migration occurred.

## 7. Stable dependency release-order gate

The current DCurses package depends on the published
`Icod.Terminal 0.2.0-alpha.6` prerelease.

The public API review does not justify publishing a stable `Icod.DCurses 0.1.0`
package while its required live-terminal substrate remains prerelease.

Therefore T13B stays at `0.1.0-Alpha-21`.

T13C begins after stable `Icod.Terminal 0.2.0` is published. T13C will:

1. update the DCurses Terminal dependency to `0.2.0`;
2. update package-verifier, package-smoke, README, and release-workflow
   dependency metadata to the same version;
3. set both `Version` and `PackageVersion` to `0.1.0`;
4. rerun the complete three-host Release/package-only consumer gate;
5. merge the release commit to `main`;
6. create `v0.1.0` only after the matching main commit is green.

## 8. Validation gate

T13B is accepted when:

1. Debug/Staging/Release builds remain clean;
2. the ordinary test suite passes;
3. the new deterministic CursesCell null-validation regression test passes;
4. the quick-start sample builds with the new timed/styled update path;
5. Staging package verification and the isolated consumer pass on Windows,
   Ubuntu, and macOS;
6. the public API baseline and README accurately describe the 0.1 contract;
7. `git diff --check` reports no whitespace errors.

After this gate passes, DCurses may wait at Alpha-21 while Terminal T20 closes.
