# T1109 — Release Candidate and Stable 1.1.0 Closure

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1109  
**Current checkpoint:** `1.1.0`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Accepted public contract:** 45 exported types / 337 canonical contract lines  
**Accepted SHA-256:** `21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039`  
**Status:** stable-source exact-head qualification active

## Purpose

T1109 closes the 1.1 release without reopening the semantic API or implementation design completed by T1102–T1108.

The closure rule is strict:

- the public API fingerprint does not change;
- the implementation does not gain new features;
- `AssemblyVersion` remains `1.0.0.0`;
- `Icod.Terminal` remains `1.6.0`;
- `Icod.TermInfo` remains `1.10.0`;
- only demonstrated release blockers may alter code.

## T1108 qualification prerequisite

The documentation-complete `1.1.0-alpha.8` head:

```text
290508c69ed7e76179f168cc748edf38a4091b76
```

passed workflow #543 (`34422961869`) across the complete seven-job matrix:

- package/fresh-consumer candidate;
- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64.

That closed T1108 and authorized RC promotion.

## Release-candidate qualification

The final documentation-synchronized `1.1.0-rc.1` head was:

```text
b90e54c668ccb8a02142c434470a020775fd375f
```

Workflow #552 (`34426070109`) passed all seven jobs. No release blocker was found.

The RC preserved the accepted 1.1 API exactly:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

## Stable-source identity

Stable-source promotion changes release identity and current release documentation only:

```text
Version         1.1.0
PackageVersion  1.1.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

No implementation/API change is introduced by stable promotion.

T1108's two regret corrections remain frozen:

1. `CursesCellMetadata.Hyperlink` is nullable for future additive metadata kinds, while the current constructor still requires a real hyperlink.
2. `WriteWithMetadata(string, CursesCellMetadata)` is the two-argument semantic convenience, avoiding ambiguity with stable 1.0 `Write(string, CursesStyle)` calls such as `Write("text", default)`.

## Stable qualification gate

The atomic stable-source commit contains only release/package/status-document changes. Its exact SHA and workflow are recorded in PR #25 after qualification without moving the branch.

The stable source must pass exactly the same seven-job matrix as alpha.8 and RC before T1109 is considered complete.

After that exact-head result is green:

- the branch remains unmoved;
- PR #25 records the tested stable-source SHA and workflow;
- no merge occurs automatically;
- no `v1.1.0` tag is created automatically;
- no GitHub Release is created automatically;
- no NuGet package is published automatically.

Those remain explicit follow-up actions.

## Release blockers

A final stable-source failure is a blocker only when it indicates a real defect in build/warnings-as-errors, tests, package identity/contents, fresh NuGet-only consumption, supported runtime behavior, accepted public API fingerprint, or required release documentation.

Any blocker fix must be narrowly scoped and must trigger a new exact-head stable qualification.

## Non-goals

T1109 does not add new semantic metadata kinds, rendering optimizations, Terminal protocol wrappers, interaction/layout/panel APIs, dependency upgrades, or API naming revisions absent a demonstrated release blocker.

## Exit criteria

T1109 is complete when:

1. `1.1.0-rc.1` has passed one exact seven-job PR matrix — complete at `b90e54c668ccb8a02142c434470a020775fd375f`, workflow #552 (`34426070109`);
2. stable `1.1.0` source is produced without API/behavioral feature changes — complete in the atomic stable-source promotion commit;
3. that exact stable-source SHA passes all seven jobs — active;
4. package metadata, README, roadmaps, API baseline, and PR metadata agree on the stable identity and accepted contract;
5. the branch is left unmoved after final stable-source qualification;
6. merge/tag/publication remain explicit follow-up actions.
