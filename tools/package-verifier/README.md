# Package Verifier

`Icod.DCurses.PackageVerifier` performs structural validation of an already
packed DCurses `.nupkg` and `.snupkg`.

It verifies:

- `<Version>` and `<PackageVersion>` are present and identical;
- `<AssemblyVersion>` is present and valid;
- `<TargetFrameworks>` is exactly `net8.0;net9.0;net10.0`;
- the `net8.0`, `net9.0`, and `net10.0` library and XML-documentation payloads
  are present;
- every packaged assembly name/version matches the project and remains unsigned;
- package metadata identifies the expected id, title, author, project, readme,
  icon, LGPL license expression, and repository;
- the package contains the non-empty `icod_tui_toolchain.jpg` README banner;
- each target-framework dependency group contains exactly
  `Icod.Terminal 0.3.0` and `Icod.TermInfo 1.4.1`;
- dependency assemblies are not accidentally bundled into the primary package;
- native/runtime and repository-only payloads are absent;
- the symbol package contains exactly one non-empty portable PDB for each target
  framework.

Run after packing:

```text
dotnet run --project tools/package-verifier/Icod.DCurses.PackageVerifier.csproj -- artifacts
```

Normal T13 validation invokes this tool through
`.github/scripts/verify-release-package.cmd` or
`.github/scripts/verify-release-package.sh`.
