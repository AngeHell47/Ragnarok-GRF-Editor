$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild / Visual Studio Build Tools est requis, avec les outils .NET Framework 4.8 et WPF.' }
$solutionDirectory = $PSScriptRoot + '\'
& $msbuild (Join-Path $PSScriptRoot 'GRFEditor\GRFEditor.csproj') /restore /t:Build /p:Configuration=Release /p:Platform=AnyCPU '/p:OutputPath=bin\CharacterPreview\' "/p:SolutionDir=$solutionDirectory" /p:SkipResourceCompression=true /m /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Compilation echouee ($LASTEXITCODE)." }
# The existing application loader resolves gzip-compressed embedded assemblies.
# Bundle the current dependencies so installation only replaces the EXE/config.
$buildDirectory = Join-Path $PSScriptRoot 'GRFEditor\bin\CharacterPreview'
$embeddedDirectory = Join-Path $PSScriptRoot 'GRFEditor\Files\Compressed'
foreach ($library in Get-ChildItem -LiteralPath $buildDirectory -Filter '*.dll' -File) {
    $target = [IO.File]::Create((Join-Path $embeddedDirectory $library.Name))
    try {
        $gzip = [IO.Compression.GZipStream]::new($target, [IO.Compression.CompressionMode]::Compress, $true)
        try {
            $bytes = [IO.File]::ReadAllBytes($library.FullName)
            $gzip.Write($bytes, 0, $bytes.Length)
        } finally { $gzip.Dispose() }
    } finally { $target.Dispose() }
}
& $msbuild (Join-Path $PSScriptRoot 'GRFEditor\GRFEditor.csproj') /t:Build /p:BuildProjectReferences=false /p:Configuration=Release /p:Platform=AnyCPU '/p:OutputPath=bin\CharacterPreview\' "/p:SolutionDir=$solutionDirectory" /p:SkipResourceCompression=true /m /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Incorporation des dependances echouee ($LASTEXITCODE)." }
Write-Host (Join-Path $PSScriptRoot 'GRFEditor\bin\CharacterPreview\GRF Editor.exe')
