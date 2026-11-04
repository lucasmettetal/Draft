# Met a jour web/draft-role-rates.js depuis le fichier public de Meraki Analytics, puis il faut recompiler.
# Licence non confirmee : a verifier avant toute distribution de ces donnees.
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$Source = 'https://cdn.merakianalytics.com/riot/lol/resources/latest/en-US/championrates.json'
$Root = Split-Path $PSScriptRoot -Parent
$IdsText = Get-Content (Join-Path $Root 'web\draft-champion-ids.js') -Raw -Encoding UTF8
$IdsJson = [regex]::Match($IdsText, 'CHAMPION_IDS\s*=\s*(\{.*?\})').Groups[1].Value
if (-not $IdsJson) { throw 'Identifiants de champions introuvables.' }
$Ids = $IdsJson | ConvertFrom-Json

$Response = Invoke-WebRequest -Uri $Source -UseBasicParsing -TimeoutSec 30
$Data = $Response.Content | ConvertFrom-Json
$Roles = 'TOP', 'JUNGLE', 'MIDDLE', 'BOTTOM', 'UTILITY'
$Culture = [Globalization.CultureInfo]::InvariantCulture
$Rows = New-Object System.Collections.ArrayList
foreach ($Champion in $Data.data.PSObject.Properties) {
    $Key = $Ids.PSObject.Properties[$Champion.Name]
    if (-not $Key) { continue }
    $Rates = foreach ($Role in $Roles) { ([math]::Round([double]$Champion.Value.$Role.playRate, 3)).ToString($Culture) }
    [void]$Rows.Add('"' + ($Key.Value.ToLowerInvariant() -replace '[^a-z0-9]', '') + '":[' + ($Rates -join ',') + ']')
}
if ($Rows.Count -lt 100) { throw "Trop peu de champions lus : $($Rows.Count)." }

$Out = "// Genere par tools/update-role-rates.ps1. Taux de jeu par role : top, jungle, mid, adc, support.`n" +
    "// Source : Meraki Analytics. Licence non confirmee : a verifier avant toute distribution.`n" +
    'const ROLE_RATES = {"patch":"' + $Data.patch + '","champions":{' + ($Rows -join ',') + "}};`n"
[IO.File]::WriteAllText((Join-Path $Root 'web\draft-role-rates.js'), $Out, (New-Object Text.UTF8Encoding($false)))
Write-Host "Patch $($Data.patch), $($Rows.Count) champions. Recompile l'application."
