# T2008 Consumer Migration Gate

**Status:** accepted on exact executable head `9ecf1293b6401c7e6b47d893f401da37a40e3084`; T2009 may begin.

## Consumer and documentation boundary

- The minimal sample reads `CursesSession.Profile` and `GetDimensions()` through Terminal-owned types, handles unavailable dimensions, and refreshes retained content. The mixed-media and interaction samples retain their public DCurses demonstrations; no sample imports or directly references TermInfo.
- `docs/2.0-Migration-Guide.md` maps all four approved public breaks, the assembly-version/rebuild requirement, live-size status, nullable lifecycle dimensions, Terminal session ownership, transaction capacity/cancellation, failure recovery, and lost raster identity. The README, changelog, active roadmap, sample index and historical 2.0 API-baseline annotation distinguish the published 1.6 package from the unpublished 2.0 development branch.
- The isolated package consumer has a single DCurses PackageReference, no project reference or direct TermInfo source/package usage. Its public-profile/dimensions assertions and existing retained text, line drawing, hyperlink, raster vocabulary, pad and panel witnesses compile and run for each of net8.0, net9.0 and net10.0.
- Linux package validation additionally enters an 80x24 pseudo-terminal from that restored package. The consumer opens a live session, observes Terminal-owned profile/dimensions, prepares styled text, line drawing, a hyperlink, a pad viewport and a panel, requests a raster resource/placeholder when available, refreshes, invalidates and refreshes again. A terminal with no raster backend may return an unavailable result; the consumer does not invent a raster token. A nonzero exit or 60-second timeout fails validation. This is an actual packaged live refresh, separate from the in-memory package surface checks.

## Exact-head evidence

[PR workflow 35823291712](https://github.com/uniblab/Icod.DCurses/actions/runs/35823291712) passed on `9ecf1293b6401c7e6b47d893f401da37a40e3084` without a retry. The package job built with zero warnings/errors, verified the packed dependency/assembly boundary, executed the fresh consumer across all three frameworks and reported `Package-only live pseudo-terminal refresh completed successfully.` All six runtime jobs (Windows/Linux/macOS, x64/ARM64) passed **1,055 tests per target framework per job**, with zero failures and zero skips.

The smoke's live raster branch is conditional on terminal support; synthetic raster ownership/refresh and failure behavior remain exercised by the full runtime suite. No merge, tag, release or publication is authorized by this gate.
