Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host "> dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if (0 -ne $LASTEXITCODE) {
        throw "dotnet exited with status $LASTEXITCODE."
    }
}

function Get-RepositorySolution {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $solutions = @(
        Get-ChildItem -LiteralPath $RepositoryRoot -File |
            Where-Object { $_.Extension -in @('.sln', '.slnx') }
    )

    if (1 -ne $solutions.Count) {
        throw "Expected exactly one root .sln or .slnx file; found $($solutions.Count)."
    }

    return $solutions[0].FullName
}

function Get-MSBuildProperty {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string]$Configuration = 'Release'
    )

    $value = @(
        & dotnet msbuild $ProjectPath -nologo "-property:Configuration=$Configuration" "-getProperty:$Name"
    ) -join "`n"
    if (0 -ne $LASTEXITCODE) {
        throw "Unable to read MSBuild property '$Name' from '$ProjectPath'."
    }

    return $value.Trim()
}

Export-ModuleMember -Function @(
    'Invoke-DotNet',
    'Get-RepositorySolution',
    'Get-MSBuildProperty'
)
