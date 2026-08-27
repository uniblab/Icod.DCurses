# T13A — Package and Release Foundation

**Project:** `Icod.DCurses`
**Development line:** `0.1.0`
**Development version:** `0.1.0-Alpha-20`
**Tranche:** T13A — package and release foundation
**Reference branch:** `0.1.0`
**Reference commit before tranche:** `17a7bad9b7e8dc84b9bf5c5b2a2f1f45448447a0`
**Status:** Implementation prepared; validation gate pending

---

## 1. Purpose

T01 intentionally installed only a minimal package check. The roadmap reserves
the deeper package-only consumer and release checks for the final 0.1 release
gate.

T13A installs that infrastructure before assigning the stable `0.1.0` package
version.

No public curses behavior changes in this tranche.

## 2. Package structural verifier

`tools/package-verifier` inspects the packed `.nupkg` and `.snupkg` directly.

The verifier requires:

- synchronized `Version` and `PackageVersion`;
- a valid project `AssemblyVersion`;
- `README.md` and `icon.png`;
- exactly the expected `lib/net10.0/Icod.DCurses.dll`;
- matching XML documentation;
- package id/title/author/project/license/repository metadata;
- exactly the declared Terminal and TermInfo dependency versions;
- no bundled dependency DLLs;
- no `runtimes/`, native, test, sample, tool, documentation, or workflow payload;
- exactly one portable DCurses PDB in the symbol package.

This converts package inspection from a file-existence check into an explicit
release contract.

## 3. Fresh package-only consumer

`tools/package-smoke` is deliberately excluded from `Icod.DCurses.sln`.

The validation wrappers copy it to a temporary directory and configure:

- a fresh temporary project directory;
- a private `NUGET_PACKAGES` cache;
- a temporary `NuGet.Config`;
- the newly packed artifact directory as the first package source;
- NuGet.org only for the declared runtime dependencies.

The consumer therefore cannot fall back to the repository's project reference
or previously restored DCurses package.

Automated execution exercises public virtual-screen, window, style, and resize
APIs without touching the CI runner terminal. The same source compiles an
optional real `CursesSession` path for manual package-only interactive
validation.

## 4. Three-host package validation

Pull requests, `main`, and release tags all:

1. clean;
2. restore;
3. build;
4. test;
5. pack;
6. run the structural verifier;
7. restore and execute the isolated package-only consumer.

The package gate runs independently on Windows, Ubuntu, and macOS.

The Windows package is retained as the canonical artifact for publication after
all three hosts succeed.

## 5. Publication ownership

A push to `main` is validation-only after T13A.

Publication is moved to `.github/workflows/release.yaml`, triggered only by a
matching `v*` tag. The release workflow requires:

- the tagged commit to be contained in `main`;
- the tag version to match both `Version` and `PackageVersion`;
- three-host Release validation;
- publication of the validated Windows artifact to NuGet.org;
- publication of the same artifact to GitHub Packages;
- a GitHub Release containing the `.nupkg`, `.snupkg`, and SHA-256 checksums.

Prerelease tags are marked as prereleases. Stable `v0.1.0` uses the same path.

## 6. Deliberately deferred to T13B

Alpha-20 does not yet declare the stable release.

T13B remains responsible for:

- the final public-API regret/documentation review;
- any remaining README or package-metadata correction discovered by that audit;
- final dependency-version confirmation;
- setting `Version` and `PackageVersion` to `0.1.0`;
- final Release validation and package-only consumer validation;
- creating the stable `v0.1.0` release tag only after all gates are green.

## 7. Validation gate

T13A is accepted when:

1. `git diff --check` is clean;
2. Debug/Staging/Release repository builds remain clean;
3. the normal unit-test suite remains green;
4. PR Staging package verification succeeds on Windows/Linux/macOS;
5. the package verifier accepts the generated `.nupkg` and `.snupkg`;
6. the isolated consumer restores DCurses from the generated package rather than
   a project reference;
7. the isolated consumer builds and executes on all three hosts;
8. a `main` push validates but does not publish;
9. a matching prerelease tag can use the new release workflow when publication
   is desired.

After this checkpoint is validated, continue with T13B and the stable `0.1.0`
release gate.
