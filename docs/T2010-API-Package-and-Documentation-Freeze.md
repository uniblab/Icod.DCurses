# T2010 API, Package and Documentation Freeze

**Status:** accepted on exact executable head `eab9f4ab4c2da293cde12ed2e71f937bc76be134` for the `2.0.0-alpha.1` development identity. T2011 may change only release identity/status/evidence unless final qualification exposes a defect.

## Public and production boundary

`PublicApiFingerprintTests` checks the compiled assembly on each net8.0/net9.0/net10.0 runtime job against `docs/Public-API-Fingerprint-2.0.json`: 75 exported types, 559 canonical contract lines and SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`. `PublicTwoZeroApiContractTests` and the [approved break manifest](2.0-API-Break-Manifest.md) permit exactly the four TermInfo-bearing signature replacements and assembly version `2.0.0.0`; the 1.6 fingerprint stays untouched. The complete suite found no accidental feature family or extra public break.

The production project directly requires only `Icod.Terminal 1.18.0`. The project/source/public-signature/AssemblyRef/TypeRef and packed nuspec guards reject a new direct TermInfo coupling; deliberate project, source and package negative controls pass. The sole deliberate direct `Icod.TermInfo 1.15.0` reference remains in the **test project** with `PrivateAssets="all"` to construct synthetic fixtures. Transitive TermInfo restore through Terminal is expected.

## Packed artifact review

[PR workflow 35823884700](https://github.com/uniblab/Icod.DCurses/actions/runs/35823884700) passed the package verifier, fresh NuGet-only consumers on each TFM, a live Linux pseudo-terminal package consumer and all six runtime jobs with 1,055 passing tests per framework, zero failures and zero skips. The package build had zero warnings/errors. The package artifact ZIP SHA-256 is `39f0d705d6291123479c2fefbfd6d409c069af66a98124d6f47e0c7aa639852a`; its embedded `Icod.DCurses.2.0.0-alpha.1.nupkg` SHA-256 is `0323d3290b2c5580f04ece2f63bf89f66935cbe2170a9f03f0bbe794365f2edf`.

Direct inspection of the emitted nuspec found exactly one dependency in each `net8.0`, `net9.0` and `net10.0` group: `Icod.Terminal` at `1.18.0`. The package contains a DLL and XML documentation for each TFM, an icon, readme, LGPL-3.0-or-later license payload and portable symbols in its `.snupkg`. The packed README SHA-256 `411ee515fdae1b20674df66be62abf4190041c2b89cb886cd433d80aff198cd7` and LICENSE SHA-256 `af3fecb6b14ab1b0aae2023e69bb86eac13a2631c81eae254f14baa57deb603a` match the source files. The package verifier additionally inspects each DLL's assembly identity and PE metadata, XML documentation, dependency groups and portable symbols.

## Documentation and release identity

The root README keeps its author attribution and describes the published 1.6 package separately from the unpublished 2.0 development branch. `CHANGELOG.md`, `samples/README.md` and [the 2.0 migration guide](2.0-Migration-Guide.md) describe the four API replacements, rebuild requirement, direct versus transitive dependency path, cancellation/capacity/recovery and advanced Terminal ownership. Package release notes currently say `2.0.0-alpha.1` and explicitly call it unpublished. T2011 must synchronize those notes, the readme/changelog/status text, the 2.0 fingerprint's release label and the companion test when advancing RC and stable-source versions; the frozen API hash and dependency floor must remain the same.

No new feature family is accepted after this gate. Merge, tag, GitHub Release and NuGet publication remain separate maintainer actions.
