# T2400 — Terminal 1.29 dependency and 2.4 preview identity

**Status:** Accepted on implementation commit `2bd668b`  
**Release state:** Unpublished `2.4.0-alpha.1` development source  
**AssemblyVersion:** `2.0.0.0`  
**Direct production dependency:** `Icod.Terminal 1.29.0` only; TermInfo remains transitive  

## Red/green evidence

The new `PublicTwoFourDevelopmentIdentityTests.PreviewIdentityPinsStableTerminal`
test was run before the project change. It failed as intended because the project
reported `2.3.1` where `2.4.0-alpha.1` was required. This established the identity
guard before mutation.

A clean isolated project then restored published `Icod.Terminal 1.29.0` from NuGet
into an empty package directory. No prerelease or local feed substituted for the
requested stable dependency.

After updating the production and test references and synchronizing `Version` and
`PackageVersion`, the focused test passed on net10.0. The complete Staging solution
build passed for net8.0, net9.0 and net10.0 with zero warnings/errors, and the full
net10.0 suite passed 1,371/1,371 tests.

## Contract and limitations

- Historical 2.3 API fingerprints and stable documentation remain historical
  evidence; the old identity test now verifies the frozen 2.3 fingerprint rather
  than requiring the active project to remain 2.3.1 forever.
- The root/package README still gives durable installation instructions for the
  published stable 2.3.1 package. Preview/stable package-readme changes belong to
  T2408/T2409.
- This gate changes dependency and development identity only. Clipboard,
  cursor-style, viewport and captured-interaction behavior is not implemented or
  claimed by T2400.
- Cross-platform and package-candidate matrices remain later gates. The evidence
  here is the local Linux Staging build/test plus clean NuGet restore.

## Next gate

T2401 freezes the DCurses-owned clipboard options and selection contract with
failing tests. T2402 then forwards explicit operations through the canonical
Terminal session and hardens failure behavior.
