@echo off
setlocal EnableExtensions

if "%~1"=="" goto usage
if "%~2"=="" goto usage
if not "%~3"=="" goto usage

set "ARTIFACT_DIR=%~1"
set "CONFIGURATION=%~2"

if /I "%CONFIGURATION%"=="Staging" (
    set "CONFIGURATION=Staging"
) else if /I "%CONFIGURATION%"=="Release" (
    set "CONFIGURATION=Release"
) else (
    goto usage
)

pushd "%~dp0\..\.." >nul || exit /b 1

set "RESULT=0"
if not exist "%ARTIFACT_DIR%" mkdir "%ARTIFACT_DIR%" || goto fail
for %%I in ("%ARTIFACT_DIR%") do set "ARTIFACT_DIR=%%~fI"

echo.
echo === Verify generated capability metadata (%CONFIGURATION%) ===
dotnet run --project tools\terminfo-metadata\%REPO_NAME%.MetadataGenerator.csproj -c %CONFIGURATION% -f net10.0 -- --check
if errorlevel 1 goto fail

echo.
echo === Verify approved public API baseline (%CONFIGURATION%) ===
dotnet run --project tools\public-api-snapshot\%REPO_NAME%.PublicApiSnapshot.csproj -c %CONFIGURATION% --no-build -- --check
if errorlevel 1 goto fail

echo.
echo === Verify net8.0/net10.0 API equivalence (%CONFIGURATION%) ===
dotnet run --project tools\public-api-snapshot\%REPO_NAME%.PublicApiSnapshot.csproj -c %CONFIGURATION% --no-build -- --compare bin\%CONFIGURATION%\net8.0\%REPO_NAME%.dll bin\%CONFIGURATION%\net10.0\%REPO_NAME%.dll
if errorlevel 1 goto fail

echo.
echo === Verify package structure and symbols (%CONFIGURATION%) ===
dotnet run --project tools\package-verifier\%REPO_NAME%.PackageVerifier.csproj -c %CONFIGURATION% -f net10.0 -- "%ARTIFACT_DIR%"
if errorlevel 1 goto fail

set "PACKAGE_VERSION="
for /f "delims=" %%V in ('dotnet msbuild %REPO_NAME%.csproj -nologo -getProperty:PackageVersion') do set "PACKAGE_VERSION=%%V"
if not defined PACKAGE_VERSION (
    echo Unable to determine PackageVersion. 1>&2
    goto fail
)

set "SMOKE_ROOT=%TEMP%\%REPO_NAME%-package-smoke-%RANDOM%-%RANDOM%"
if exist "%SMOKE_ROOT%" rmdir /s /q "%SMOKE_ROOT%"
mkdir "%SMOKE_ROOT%" || goto fail

copy /y tools\package-smoke\%REPO_NAME%.PackageSmoke.csproj "%SMOKE_ROOT%\%REPO_NAME%.PackageSmoke.csproj" >nul || goto fail
copy /y tools\package-smoke\Program.cs "%SMOKE_ROOT%\Program.cs" >nul || goto fail

set "OLD_NUGET_PACKAGES=%NUGET_PACKAGES%"
set "NUGET_PACKAGES=%SMOKE_ROOT%\packages"

echo.
echo === Fresh package consumer: net8.0 ===
dotnet restore "%SMOKE_ROOT%\%REPO_NAME%.PackageSmoke.csproj" --source "%ARTIFACT_DIR%" -p:IcodTermInfoPackageVersion=%PACKAGE_VERSION%
if errorlevel 1 goto fail

dotnet run --project "%SMOKE_ROOT%\%REPO_NAME%.PackageSmoke.csproj" -c %CONFIGURATION% -f net8.0 --no-restore -p:IcodTermInfoPackageVersion=%PACKAGE_VERSION%
if errorlevel 1 goto fail

echo.
echo === Fresh package consumer: net10.0 ===
dotnet run --project "%SMOKE_ROOT%\%REPO_NAME%.PackageSmoke.csproj" -c %CONFIGURATION% -f net10.0 --no-restore -p:IcodTermInfoPackageVersion=%PACKAGE_VERSION%
if errorlevel 1 goto fail

echo.
echo === Non-interactive repository sample ===
dotnet run --project samples\%REPO_NAME%.Sample\%REPO_NAME%.Sample.csproj -c %CONFIGURATION% -f net10.0 -- --describe-only --profile ms-terminal-direct
if errorlevel 1 goto fail

goto cleanup

:usage
echo Usage: verify-release-package.cmd ^<artifact-directory^> ^<Staging^|Release^> 1>&2
exit /b 2

:fail
set "RESULT=%ERRORLEVEL%"
if "%RESULT%"=="0" set "RESULT=1"

:cleanup
if defined SMOKE_ROOT if exist "%SMOKE_ROOT%" rmdir /s /q "%SMOKE_ROOT%"
if defined OLD_NUGET_PACKAGES (
    set "NUGET_PACKAGES=%OLD_NUGET_PACKAGES%"
) else (
    set "NUGET_PACKAGES="
)
popd >nul
exit /b %RESULT%
