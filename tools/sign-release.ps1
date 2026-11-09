# Signe l'exécutable portable d'une release publiée, puis joint la signature à la release. À lancer après le workflow du tag.
# Tant que la signature n'est pas jointe, DRAFT refuse d'installer cette version.
# -File et -Out signent un fichier local sans rien envoyer (essais).
param(
  [string]$Tag,
  [string]$File,
  [string]$Out,
  [string]$Repo = 'lucasmettetal/Draft',
  [string]$KeyFile = (Join-Path $env:APPDATA 'DRAFT-signature\draft-update-private.xml')
)
$ErrorActionPreference = 'Stop'
if (-not $Tag) { throw 'Indique -Tag, par exemple -Tag v2.16.1.' }
if (-not (Test-Path $KeyFile)) { throw "Clé privée introuvable : $KeyFile" }
$work = $null
if (-not $File) {
  $work = Join-Path $env:TEMP ("draft-signature-" + [guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory $work | Out-Null
  & gh release download $Tag -R $Repo -p '*-Portable.exe' -D $work
  if ($LASTEXITCODE) { throw 'Téléchargement de la release impossible.' }
  $File = (Get-ChildItem $work -Filter '*-Portable.exe' | Select-Object -First 1).FullName
}
try {
  $hash = (Get-FileHash $File -Algorithm SHA256).Hash.ToLowerInvariant()
  $text = [Text.Encoding]::UTF8.GetBytes("DRAFT-update`n$Tag`n$hash")
  $rsa = [Security.Cryptography.RSA]::Create()
  $rsa.FromXmlString([IO.File]::ReadAllText($KeyFile))
  $signature = [Convert]::ToBase64String($rsa.SignData($text, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1))
  $target = if ($Out) { $Out } else { "$File.sig" }
  [IO.File]::WriteAllText($target, $signature, [Text.UTF8Encoding]::new($false))
  if ($work) {
    # L'exécutable contrôle lui-même la signature avec la clé publique qu'il embarque.
    $p = Start-Process $File -ArgumentList '--test-signature', "`"$File`"", $Tag, "`"$target`"" -PassThru -Wait
    if ($p.ExitCode -ne 0) { throw 'La signature ne correspond pas à la clé publique de cette version : rien n''est envoyé.' }
    & gh release upload $Tag $target -R $Repo --clobber
    if ($LASTEXITCODE) { throw 'Envoi de la signature impossible.' }
    Write-Host "Release $Tag signée."
  } else { Write-Host "Signature écrite : $target" }
}
finally { if ($work) { Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue } }
