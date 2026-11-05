# Compile DraftMid-Portable.exe et DraftMid-Setup.exe avec le compilateur C# fourni par Windows.
# Ni Node, ni Rust, ni Visual Studio nécessaires.
# -SansMeraki : n'embarque pas la référence Meraki, dont la licence de redistribution n'est pas confirmée.
# -Prototypes : ajoute les prototypes isolés (OP.GG). La compilation normale ne les contient jamais.
param([string]$Out = "$PSScriptRoot\dist", [switch]$SansMeraki, [switch]$Prototypes)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "Compilateur C# introuvable : $csc" }
New-Item -ItemType Directory -Force $Out | Out-Null

# Icône générée, pour ne pas dépendre d'un fichier binaire.
Add-Type -AssemblyName System.Drawing
$icoPath = Join-Path $Out 'draftmid.ico'
$bmp = New-Object Drawing.Bitmap 64, 64
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([Drawing.Color]::FromArgb(10, 15, 22))
$pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(63, 216, 198)), 7
$pen.LineJoin = 'Round'
$g.DrawPolygon($pen, [Drawing.Point[]]@((New-Object Drawing.Point 11, 53), (New-Object Drawing.Point 11, 24), (New-Object Drawing.Point 27, 11), (New-Object Drawing.Point 53, 11), (New-Object Drawing.Point 53, 24), (New-Object Drawing.Point 37, 53)))
$g.DrawLine($pen, 24, 40, 40, 24)
$g.Dispose()
$icon = [Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = [IO.File]::Create($icoPath); $icon.Save($fs); $fs.Close()

# Interface, moteur et composants WebView2 sont intégrés à l'exe.
$res = New-Object Collections.Generic.List[string]
$webRoot = (Resolve-Path "$root\web").Path
Get-ChildItem $webRoot -Recurse -File | Where-Object { $_.Name -notlike '*-tests.html' -and -not ($SansMeraki -and $_.Name -eq 'draft-role-rates.js') } | ForEach-Object {
  $rel = $_.FullName.Substring($webRoot.Length + 1).Replace('\', '/')
  $res.Add("/resource:`"$($_.FullName)`",web/$rel")
}
foreach ($n in 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll') { $res.Add("/resource:`"$root\lib\$n`",lib/$n") }

$exe = Join-Path $Out 'DraftMid-Portable.exe'
$rsp = Join-Path $Out 'build.rsp'
$lines = @(
  '/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/utf8output', '/codepage:65001',
  "/out:`"$exe`"", "/win32icon:`"$icoPath`"",
  '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
  '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll', '/reference:Microsoft.CSharp.dll',
  "/reference:`"$root\lib\Microsoft.Web.WebView2.Core.dll`"", "/reference:`"$root\lib\Microsoft.Web.WebView2.WinForms.dll`""
) + $res + (Get-ChildItem "$root\src\*.cs" | Where-Object { $Prototypes -or $_.Name -notlike 'Opgg*' } | ForEach-Object { "`"$($_.FullName)`"" }) + $(if ($Prototypes) { @('/define:DRAFTMID_PROTOTYPES') } else { @() })
[IO.File]::WriteAllLines($rsp, $lines)
& $csc "@$rsp"
if ($LASTEXITCODE -ne 0) { throw "La compilation a échoué." }

# L'installateur est le même programme : il s'installe seul quand son nom contient « Setup ».
Copy-Item $exe (Join-Path $Out 'DraftMid-Setup.exe') -Force
Remove-Item $rsp, $icoPath -ErrorAction SilentlyContinue
Get-ChildItem $Out -Filter '*.exe' | Select-Object Name, @{n='Mo';e={[math]::Round($_.Length / 1MB, 1)}}
