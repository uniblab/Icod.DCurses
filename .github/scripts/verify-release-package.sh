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

if [[ ! -d "${artifact_dir}" ]]; then
  echo "Artifact directory does not exist: ${artifact_dir}" >&2
  exit 1
fi

artifact_dir="$(cd "${artifact_dir}" && pwd)"

package_version="$(
  dotnet msbuild Icod.DCurses.csproj \
    -nologo \
    -getProperty:PackageVersion
)"
package_version="${package_version//$'\r'/}"

if [[ -z "${package_version}" ]]; then
  echo "Unable to determine PackageVersion." >&2
  exit 1
fi

package_path="${artifact_dir}/Icod.DCurses.${package_version}.nupkg"
symbols_path="${artifact_dir}/Icod.DCurses.${package_version}.snupkg"

if [[ ! -f "${package_path}" ]]; then
  echo "Missing package: ${package_path}" >&2
  exit 1
fi

if [[ ! -f "${symbols_path}" ]]; then
  echo "Missing symbols package: ${symbols_path}" >&2
  exit 1
fi

echo
echo "=== Verify package structure, metadata, dependencies, and symbols (${configuration}) ==="
dotnet run \
  --project tools/package-verifier/Icod.DCurses.PackageVerifier.csproj \
  -c "${configuration}" \
  -- "${artifact_dir}"

smoke_root="$(mktemp -d "${TMPDIR:-/tmp}/Icod.DCurses-package-smoke.XXXXXX")"
old_nuget_packages="${NUGET_PACKAGES-}"

cleanup() {
  rm -rf "${smoke_root}"
  if [[ -n "${old_nuget_packages}" ]]; then
    export NUGET_PACKAGES="${old_nuget_packages}"
  else
    unset NUGET_PACKAGES || true
  fi
}
trap cleanup EXIT

cp tools/package-smoke/Icod.DCurses.PackageSmoke.csproj \
  "${smoke_root}/Icod.DCurses.PackageSmoke.csproj"
cp tools/package-smoke/Program.cs \
  "${smoke_root}/Program.cs"

export NUGET_PACKAGES="${smoke_root}/packages"
nuget_config="${smoke_root}/NuGet.Config"

cat > "${nuget_config}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="T13 artifacts" value="${artifact_dir}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo
echo "=== Fresh package consumer restore ==="
dotnet restore \
  "${smoke_root}/Icod.DCurses.PackageSmoke.csproj" \
  --no-cache \
  --configfile "${nuget_config}" \
  -p:IcodDCursesPackageVersion="${package_version}"

echo
echo "=== Fresh package consumer: net10.0 ==="
dotnet run \
  --project "${smoke_root}/Icod.DCurses.PackageSmoke.csproj" \
  -c "${configuration}" \
  --no-restore \
  -p:IcodDCursesPackageVersion="${package_version}"
