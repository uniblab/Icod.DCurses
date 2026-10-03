# T2301 `CursesRasterAtlas` API gate

**Date:** 2026-10-03
**Status:** Contract frozen; expected RED captured in workflow run `37100960059`
**Baseline:** `Icod.DCurses 2.2.0`, `Icod.Terminal 1.24.0`

## Decision

The approved 2.3 public contract is frozen without renaming. The permanent contract tests cover the two value records, presentation enum/result, sealed atlas owner, session creation and geometry queries, retained window projection, optional cancellation, update ceiling and absence of public Terminal resource, placeholder, animation or frame identities.

Validation is also frozen before implementation:

- atlas rows and columns are each 1 through 256;
- tile pixel axes are positive and total pixel axes must fit `Int32`;
- geometry-query timeout is strictly positive;
- initial images are non-null and exactly divisible by the requested cell grid;
- presentation lists are non-null, contain at most 4096 unique in-range coordinates, use RGB24 or RGBA32, and contain exact-size tile images.

## RED expectation

Before production types are added, the focused `CursesRasterAtlas` test selection must fail only because the new public types and members do not yet exist (`CS0246` and `CS1061`). The build environment for this branch has no local .NET SDK, so the exact PR-head GitHub Actions run is the executable RED record.

## Regret deadline

Any later public-name, enum-value, default, result-shape or validation change requires an explicit amendment to the approved design and this gate before T2309. Private implementation helpers are not part of the frozen contract.
