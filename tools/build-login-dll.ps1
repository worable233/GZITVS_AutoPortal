param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("x64", "Win32")]
    [string]$Platform = "x64",
    [switch]$CopyToAppOutput
)

$ErrorActionPreference = "Stop"

function Resolve-MsBuildPath {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        throw "vswhere not found. Please install Visual Studio Build Tools or Visual Studio Community."
    }

    $installationPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ([string]::IsNullOrWhiteSpace($installationPath)) {
        throw "No Visual Studio installation with MSBuild was found."
    }

    $msbuild = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
    if (-not (Test-Path $msbuild)) {
        throw "MSBuild.exe not found at: $msbuild"
    }

    return $msbuild
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$loginProject = Join-Path $repoRoot "Login\Login.vcxproj"

if (-not (Test-Path $loginProject)) {
    throw "Login.vcxproj not found: $loginProject"
}

$msbuildExe = Resolve-MsBuildPath
Write-Host "Using MSBuild: $msbuildExe"

& $msbuildExe $loginProject /m /nologo /p:Configuration=$Configuration /p:Platform=$Platform
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$outRoot = Join-Path $repoRoot "Login\$Platform\$Configuration"
$dll = Join-Path $outRoot "Login.dll"

if (-not (Test-Path $dll)) {
    throw "Build finished but Login.dll was not found: $dll"
}

Write-Host "Built: $dll"

if ($CopyToAppOutput) {
    $targets = @(
        (Join-Path $repoRoot "bin\Debug\net8.0-windows10.0.19041.0\win-x64")
        (Join-Path $repoRoot "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish")
    )

    foreach ($target in $targets) {
        if (-not (Test-Path $target)) {
            continue
        }

        Copy-Item -LiteralPath $dll -Destination (Join-Path $target "Login.dll") -Force

        foreach ($nativeName in @("libcurl.dll", "zlib1.dll")) {
            $nativeSrc = Join-Path $outRoot $nativeName
            if (Test-Path $nativeSrc) {
                Copy-Item -LiteralPath $nativeSrc -Destination (Join-Path $target $nativeName) -Force
            }
        }

        Write-Host "Synced DLLs to: $target"
    }
}
