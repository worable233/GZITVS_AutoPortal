param(
    [Parameter(Mandatory=$true)]
    [string]$PublishDir,
    [Parameter(Mandatory=$true)]
    [string]$OutputDir,
    [Parameter(Mandatory=$true)]
    [string]$ManifestPath
)

Write-Host "PublishDir: $PublishDir"
Write-Host "OutputDir: $OutputDir"
Write-Host "ManifestPath: $ManifestPath"

$mappingFile = "$OutputDir\mapping.txt"
$outputPath = "$OutputDir\AutoPortal_v1.1.0_x64.msix"
$makeAppxPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"

# Copy manifest to publish dir and replace placeholders
$destManifest = "$PublishDir\AppxManifest.xml"
Write-Host "Copying manifest to: $destManifest"
$content = Get-Content $ManifestPath -Raw
$content = $content -replace '\$targetnametoken\$', 'AutoPortal'
$content = $content -replace '\$targetentrypoint\$', 'Windows.FullTrustApplication'
$content | Out-File -FilePath $destManifest -Encoding UTF8 -NoNewline

# Create mapping file
Write-Host "Creating mapping file: $mappingFile"
"[Files]" | Out-File -FilePath $mappingFile -Encoding UTF8

Get-ChildItem -Path $PublishDir -Recurse -File | Where-Object { $_.Name -ne "AppxManifest.xml" } | ForEach-Object {
    $sourcePath = $_.FullName
    $destPath = $_.FullName.Substring($PublishDir.Length).TrimStart('\')
    "`"$sourcePath`" `"$destPath`"" | Out-File -FilePath $mappingFile -Append -Encoding UTF8
}

Write-Host "Creating MSIX package..."
Write-Host "makeappx pack /f $mappingFile /m $destManifest /p $outputPath /nv"
& $makeAppxPath pack /f "$mappingFile" /m "$destManifest" /p "$outputPath" /nv

if ($LASTEXITCODE -ne 0) {
    Write-Host "MSIX packaging failed with exit code: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "MSIX package created successfully: $outputPath"
