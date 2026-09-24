# T2112 RC and Stable-Source Closure Plan

**Status:** in progress; T2111 exact-head gate accepted.\
**Authority:** `Icod.DCurses-2.1.0-Development-Roadmap.md`, T2112.\
**Branch:** `2.1.0-roadmap` in open PR #33. Do not merge, tag, create a release or publish to NuGet.

## Goal

Qualify the accepted 2.1 source as an RC and then as an unpublished `2.1.0` stable-source candidate. Keep the 2.1 public fingerprint hash, 2.0 assembly identity, framework matrix, Terminal-only direct dependency, and application behavior fixed after T2111.

## Sequence

1. **RC identity.** Update `Version`, `PackageVersion`, package release notes, the 2.1 fingerprint release/status metadata and the exact-identity test to `2.1.0-rc.1`. Keep `AssemblyVersion` `2.0.0.0`. Run the exact PR-head seven-job Staging runtime/package matrix. Check package-only .NET 8/9/10 consumers and source commit provenance in the package. Release validation belongs to a later push to `main`.
2. **RC review.** Record the RC executable head, workflow, artifact names and SHA-256 hashes, package dependency floor and any limitations. Correct defects through a new exact-head run before promotion.
3. **Stable-source identity.** Update version/package notes/fingerprint release metadata/identity test to `2.1.0` without changing the accepted public surface or production behavior. Synchronize README, changelog and sample links to describe the stable-source candidate accurately while avoiding a publication claim. Run another exact-head seven-job PR Staging matrix and fresh package-only consumer validation.
4. **Provenance and handoff.** Record the stable executable head, CI run, Staging package and symbol package hashes, dependency/license/README/XML checks, no-merge state, and manual sample limits. If an evidence-only commit follows, require its own exact-head PR CI before a readiness claim; put the final head and hashes in the PR description without self-referential source commits. The `main` push workflow already runs Release only; its validation follows a separately authorized merge and is not part of this open PR gate.

## Acceptance

RC and stable-source identities each pass the seven-job PR Staging matrix at their own source head. The stable candidate package carries version `2.1.0`, only direct `Icod.Terminal 1.18.0`, .NET 8/9/10 assets, license, README, XML docs and portable symbols, and its provenance points to the checked-out PR head. PR #33 stays open and unmerged for maintainer review. Release builds and tests run on a future push to `main`.
