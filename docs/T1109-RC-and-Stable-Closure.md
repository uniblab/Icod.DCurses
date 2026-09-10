# T1109 — Release Candidate and Stable 1.1.0 Closure

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1109  
**Current checkpoint:** `1.1.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Accepted public contract:** 45 exported types / 337 canonical contract lines  
**Accepted SHA-256:** `21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039`  
**Status:** release-candidate exact-head qualification active

## Purpose

T1109 closes the 1.1 release without reopening the semantic API or implementation design completed by T1102–T1108.

The release-candidate rule is strict:

- the public API fingerprint does not change;
- the implementation does not gain new features;
- `AssemblyVersion` remains `1.0.0.0`;
- `Icod.Terminal` remains `1.6.0`;
- `Icod.TermInfo` remains `1.10.0`;
- only release blockers discovered by qualification may change code before stable promotion.

## T1108 qualification prerequisite

The documentation-complete `1.1.0-alpha.8` head is:

```text
290508c69ed7e76179f168cc748edf38a4091b76
```

Workflow #543 (`34422961869`) passed the complete seven-job pull-request matrix:

- package/fresh-consumer candidate;
- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64.

That closes T1108 and authorizes RC promotion.

## Release-candidate identity

T1109 promotes the accepted alpha.8 surface unchanged to:

```text
Version         1.1.0-rc.1
PackageVersion  1.1.0-rc.1
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

The accepted 1.1 API remains:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

The two intentional new exported types remain:

- `CursesHyperlink`;
- `CursesCellMetadata`.

T1108's two pre-RC regret corrections remain frozen:

1. `CursesCellMetadata.Hyperlink` is nullable for future additive metadata kinds, while the current constructor still requires a real hyperlink.
2. `WriteWithMetadata(string, CursesCellMetadata)` is the two-argument semantic convenience, avoiding ambiguity with stable 1.0 `Write(string, CursesStyle)` calls such as `Write("text", default)`.

The explicit style-bearing semantic write and `WriteCell` semantic overload remain unchanged.

## RC qualification gate

The RC must pass exactly the same seven-job Staging matrix used for alpha qualification.

Several intermediate RC documentation commits may generate workflows while package identity and governing documents are synchronized. Those runs are evidence only and do not define the RC freeze.

The **final documentation-synchronized RC head** is the branch head produced by this record. Its exact SHA and workflow are recorded in PR #25 after qualification without moving the branch. Only that final exact-head seven-job result authorizes stable promotion.

No stable version promotion occurs until that exact-head workflow is green.

## Stable promotion rule

After one exact RC head passes all seven jobs:

1. only `Version`, `PackageVersion`, release notes, and current release-status documentation are changed to stable `1.1.0`;
2. the public fingerprint remains byte-for-byte the accepted T1108 contract unless a release blocker requires reopening the gate;
3. no feature work enters the stable-promotion commit;
4. one exact stable-source SHA must pass the complete seven-job PR matrix;
5. that tested stable-source SHA is recorded in PR metadata without subsequently moving the branch;
6. merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.

## Release blockers

An RC failure is treated as a blocker only when it indicates a real defect in:

- build or warnings-as-errors;
- tests;
- package identity or contents;
- fresh NuGet-only consumption;
- supported runtime behavior;
- accepted public API fingerprint;
- release documentation required by the package.

A blocker fix must be narrowly scoped and must trigger a new exact-head RC qualification.

## Non-goals

T1109 does not add:

- new semantic metadata kinds;
- new rendering optimizations;
- new Terminal protocol wrappers;
- new interaction/layout/panel APIs;
- dependency upgrades;
- API naming revisions after the T1108 regret gate, absent a demonstrated release blocker.

## Exit criteria

T1109 is complete only when:

1. `1.1.0-rc.1` has passed one exact seven-job PR matrix;
2. stable `1.1.0` source is produced without API/behavioral feature changes;
3. that exact stable-source SHA passes all seven jobs;
4. package metadata, README, roadmaps, API baseline, and PR metadata agree on the stable identity and accepted contract;
5. the branch is left unmoved after the final stable-source qualification;
6. merge/tag/publication remain explicit follow-up actions.
