$BinaryPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'bin', 'PSOTLP.dll')

Import-Module -Name $BinaryPath.FullName

$TypesPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'PSOTLP.Types.ps1xml')
$FormatPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'PSOTLP.Format.ps1xml')

if ([System.IO.File]::Exists($TypesPath.FullName)) {
    Update-TypeData -PrependPath $TypesPath.FullName -ErrorAction SilentlyContinue
}

if ([System.IO.File]::Exists($FormatPath.FullName)) {
    Update-FormatData -PrependPath $FormatPath.FullName -ErrorAction SilentlyContinue
}