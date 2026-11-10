param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[a-zA-Z0-9.-]+)?$')]
    [string] $Version = '0.0.0-dev',
    [string] $Compiler = 'ISCC.exe'
)
$ErrorActionPreference = 'Stop'
$compilerPath = (Get-Command $Compiler -ErrorAction Stop).Source
$project = Join-Path $PSScriptRoot 'IraqPlatformer.csproj'
$dist = Join-Path $PSScriptRoot 'dist'
$payload = Join-Path $dist 'publish'
& dotnet publish $project -c Release -r win-x64 --self-contained true -o $payload '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' "-p:Version=$Version"
if ($LASTEXITCODE) { throw 'Windows publish failed.' }

$verify = Join-Path $dist ('verification-' + [guid]::NewGuid().ToString('N'))
$process = Start-Process -FilePath (Join-Path $payload 'IraqPlatformer.exe') -ArgumentList @('--verify', ('"' + $verify + '"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $verify 'verified.txt'))) {
    throw 'The packaged game failed verification.'
}

$routes = @(Get-Content -LiteralPath (Join-Path $verify 'integrated-routes.txt'))
if ($routes.Count -ne 152 -or @($routes | Where-Object { $_ -notmatch '^\d+-\d+: PASS$' }).Count -ne 0) {
    throw "Campaign traversal failed. See $verify\integrated-routes.txt."
}

& $compilerPath "/DVersion=$Version" (Join-Path $PSScriptRoot 'Installer.iss')
if ($LASTEXITCODE) { throw 'Windows installer compilation failed.' }
Get-FileHash -LiteralPath (Join-Path $dist 'IraqPlatformer-Setup.exe') -Algorithm SHA256
