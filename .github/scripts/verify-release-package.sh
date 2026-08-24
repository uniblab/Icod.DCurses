#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: verify-release-package.sh <artifact-directory> <Staging|Release>" >&2
}

if (( $# != 2 )); then
  usage
  exit 2
fi

artifact_dir="$1"
configuration="$2"

case "${configuration}" in
  Staging|Release)
    ;;
  *)
    usage
    exit 2
    ;;
esac

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${repository_root}"

mkdir -p "${artifact_dir}"
artifact_dir="$(cd "${artifact_dir}" && pwd)"

# Repository-only maintenance tools are executed on one explicit framework.
dotnet run \
  --project tools/terminfo-metadata/Icod.TermInfo.MetadataGenerator.csproj \
  -c "${configuration}" \
  -f net10.0 \
  -- --check

# The reviewed 1.0 API baseline must remain exact.
dotnet run   --project tools/public-api-snapshot/Icod.TermInfo.PublicApiSnapshot.csproj   -c "${configuration}"   --no-build   -- --check

# The two shipped target frameworks must expose the exact same API.
dotnet run   --project tools/public-api-snapshot/Icod.TermInfo.PublicApiSnapshot.csproj   -c "${configuration}"   --no-build   -- --compare   bin/${configuration}/net8.0/Icod.TermInfo.dll   bin/${configuration}/net10.0/Icod.TermInfo.dll

# Structural package, Source Link, dependency, and architecture verification.
dotnet run \
  --project tools/package-verifier/Icod.TermInfo.PackageVerifier.csproj \
  -c "${configuration}" \
  -f net10.0 \
  -- "${artifact_dir}"

package_version="$(
  dotnet msbuild Icod.TermInfo.csproj \
    -nologo \
    -getProperty:PackageVersion
)"

if [[ -z "${package_version}" ]]; then
  echo "Unable to determine PackageVersion." >&2
  exit 1
fi

# Copy the package-reference-only consumer to a temporary directory so the smoke
# test cannot accidentally use a project reference or stale repository outputs.
smoke_root="$(mktemp -d)"
trap 'rm -rf "${smoke_root}"' EXIT

cp \
  tools/package-smoke/Icod.TermInfo.PackageSmoke.csproj \
  "${smoke_root}/Icod.TermInfo.PackageSmoke.csproj"
cp \
  tools/package-smoke/Program.cs \
  "${smoke_root}/Program.cs"

export NUGET_PACKAGES="${smoke_root}/packages"

dotnet restore \
  "${smoke_root}/Icod.TermInfo.PackageSmoke.csproj" \
  --source "${artifact_dir}" \
  -p:IcodTermInfoPackageVersion="${package_version}"

# The isolated consumer must execute against every supported target framework.
dotnet run \
  --project "${smoke_root}/Icod.TermInfo.PackageSmoke.csproj" \
  -c "${configuration}" \
  -f net8.0 \
  --no-restore \
  -p:IcodTermInfoPackageVersion="${package_version}"

dotnet run \
  --project "${smoke_root}/Icod.TermInfo.PackageSmoke.csproj" \
  -c "${configuration}" \
  -f net10.0 \
  --no-restore \
  -p:IcodTermInfoPackageVersion="${package_version}"

# The repository sample must retain a non-interactive path suitable for CI.
dotnet run \
  --project samples/Icod.TermInfo.Sample/Icod.TermInfo.Sample.csproj \
  -c "${configuration}" \
  -f net10.0 \
  -- --describe-only --profile ms-terminal-direct
