# T2408 API, Package, and Documentation Gate

**Decision:** Accept the additive 2.4 public feature contract and package shape  
**Implementation head:** `8e1ed20a24561074e43a16922aeb336f8a0b6a61`  
**Qualified package identity:** `2.4.0-alpha.1`  
**Environment:** Linux 6.18.44 x86_64, .NET SDK/runtime 10.0.401/10.0.12

## Public contract review

The complete compiled contract is frozen by
`Public-API-Fingerprint-2.4.json`:

```text
109 exported types
857 canonical declared contract lines
sha256 87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e
```

The only new exported types over 2.3.1 are `CursesClipboardOptions`,
`CursesClipboardSelection`, `CursesCursorStyle`, and
`CursesCursorStyleLease`. The reviewed member delta is one session option and
four session methods (two clipboard-write overloads, clipboard read, and cursor
style acquisition). No 2.3.1 type/member was removed, no new public Terminal
type entered the surface, and no TermInfo type or direct dependency was added.
`AssemblyVersion` remains `2.0.0.0`.

## Build and test evidence

- complete Staging solution build for net8.0/net9.0/net10.0: zero warnings,
  zero errors;
- complete Release solution build for net8.0/net9.0/net10.0: zero warnings,
  zero errors;
- complete Staging tests: 1,415/1,415 passed for each target framework (4,245
  executions total);
- focused public API/package/dependency gates: 9/9 passed;
- viewport witness: caller-prepared 40-character slice projected in 4.078 ms
  with 30,384 allocated bytes; the whole 100,000-column layout witness was
  rejected as invalid higher-layer composition;
- repository audit: every library C# source begins with the prescribed LGPLv3+
  notice, and every sample/test C# and project file begins with the prescribed
  GPLv3+ notice.

Only the .NET 10 runtime is installed locally. The net8.0 and net9.0 test and
package-consumer binaries were compiled for their exact TFMs and executed with
`DOTNET_ROLL_FORWARD=Major` on .NET 10. Exact .NET 8 and .NET 9 runtime execution
therefore remains a PR CI obligation rather than a local claim.

## Package evidence

The exact-head Release package contains only the expected README, license, icon,
image, three managed assemblies, three XML documentation files, and the matching
portable-symbol package. Its nuspec contains one dependency in every target
group: exact `Icod.Terminal 1.29.0`. There is no direct TermInfo, native runtime
payload, sample, test, tool, workflow, or repository-doc payload.

```text
Icod.DCurses.2.4.0-alpha.1.nupkg
sha256 f7f397fa36f94008db597b27deeac1c3ed2543f9e414640f6b988b2f9a551268

Icod.DCurses.2.4.0-alpha.1.snupkg
sha256 57cfef3b0957c71a4b005303404233ca2fff752b4ce0a22b4f6b7d0eeb27039e
```

An isolated NuGet cache restored only the packed DCurses candidate plus its
declared dependency graph. Package-only net8.0, net9.0, and net10.0 consumers
compiled and ran the existing surfaces plus the new clipboard options/methods
and cursor-style lease. A Linux `script` pseudo-terminal also completed the
non-interactive retained refresh witness.

## Documentation and remaining limits

The packaged README is now timeless: it uses an unversioned installation
command, contains a runnable editor-sample command, explains the 2.4 clipboard
and cursor contracts, and uses full GitHub URLs for files not shipped in the
package. CHANGELOG records implemented behavior rather than qualification
status. The package release notes now describe the completed 2.4 feature set.

No real interactive terminal was available for OSC 52 consent/readback, visible
cursor shape, mouse reporting, or focus-event acceptance. Windows and macOS were
not tested locally. No stable identity, RC, merge, tag, publication, or post-main
workflow result is claimed by this gate; those belong to T2409 and the
maintainer-owned release sequence.
