# Calcule les roles joues par chaque champion sur des parties recentes, via l'API officielle de Riot.
# Ne garde que des compteurs champion/role : aucun nom ni identifiant de joueur n'est ecrit.
# La cle n'est ni affichee ni enregistree. Relancer a chaque patch.
param(
    [int]$Games = 1000,
    [int]$Days = 14,
    [string]$Platform = 'euw1',
    [string]$Region = 'europe',
    [string[]]$Tiers = @('SILVER', 'GOLD', 'PLATINUM', 'EMERALD'),
    [string]$OutDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'),
    [string]$TestMatchFile
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$QueueSoloDuo = 420
$MatchesPerPlayer = 5
$DelayMs = 1300          # limite d'une cle personnelle : 100 requetes par 2 minutes
$SaveEvery = 50
$MinDurationSec = 300    # en dessous, on considere la partie comme un remake
$Roles = 'TOP', 'JUNGLE', 'MIDDLE', 'BOTTOM', 'UTILITY'
$OutPath = Join-Path $OutDir 'draft-role-rates-live.js'
$counts = @{}
$counted = 0

function Invoke-Riot([string]$Url, [hashtable]$Headers) {
    while ($true) {
        Start-Sleep -Milliseconds $DelayMs
        try {
            return Invoke-RestMethod -Uri $Url -Headers $Headers -Method Get
        } catch {
            $response = $_.Exception.Response
            if (-not $response) { throw }
            $code = [int]$response.StatusCode
            if ($code -eq 429) {
                $wait = 120
                $retry = $response.Headers['Retry-After']
                if ($retry) { $wait = [int]$retry }
                Write-Host "Limite atteinte, attente de $wait secondes..."
                Start-Sleep -Seconds $wait
                continue
            }
            if ($code -eq 401 -or $code -eq 403) { throw "Cle Riot refusee ou expiree (code $code)." }
            if ($code -eq 404) { return $null }
            throw "Erreur Riot (code $code) sur une requete."
        }
    }
}

function Add-Match($Match) {
    if ([int]$Match.info.gameDuration -lt $MinDurationSec) { return }
    foreach ($player in $Match.info.participants) {
        $role = [array]::IndexOf($Roles, [string]$player.teamPosition)
        if ($role -lt 0) { continue }
        $key = ([string]$player.championName).ToLowerInvariant() -replace '[^a-z0-9]', ''
        if (-not $key) { continue }
        if (-not $counts.ContainsKey($key)) { $counts[$key] = @(0, 0, 0, 0, 0) }
        $counts[$key][$role]++
    }
    $script:counted++
}

function Save-Rates {
    $rows = foreach ($key in ($counts.Keys | Sort-Object)) { '"' + $key + '":[' + ($counts[$key] -join ',') + ']' }
    $date = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
    $text = "// Genere par draft-update-roles.ps1 : parties par role, top, jungle, mid, adc, support.`n" +
        'window.ROLE_RATES_LIVE = {"updated":"' + $date + '","games":' + $counted + ',"champions":{' + ($rows -join ',') + "}};`n"
    New-Item -ItemType Directory -Force $OutDir | Out-Null
    [IO.File]::WriteAllText($OutPath, $text, (New-Object Text.UTF8Encoding($false)))
}

if ($TestMatchFile) {
    $matches_ = Get-Content $TestMatchFile -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($m in $matches_) { Add-Match $m }
    Save-Rates
    Write-Host "Test ecrit : $counted parties."
    return
}

$key = $env:RIOT_API_KEY
if (-not $key) {
    $secure = Read-Host 'Colle ta cle Riot (elle reste masquee et ne sera pas enregistree)' -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $key = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}
if (-not $key) { throw 'Aucune cle fournie.' }
$headers = @{ 'X-Riot-Token' = $key.Trim() }

$platformUrl = "https://$Platform.api.riotgames.com"
$regionUrl = "https://$Region.api.riotgames.com"
$startTime = [DateTimeOffset]::UtcNow.AddDays(-$Days).ToUnixTimeSeconds()

$players = New-Object System.Collections.ArrayList
foreach ($tier in $Tiers) {
    Write-Host "Joueurs $tier..."
    $entries = @(Invoke-Riot "$platformUrl/lol/league-exp/v4/entries/RANKED_SOLO_5x5/$tier/II?page=1" $headers)
    foreach ($entry in $entries) { if ($entry.puuid) { [void]$players.Add([string]$entry.puuid) } }
}
if ($players.Count -eq 0) { throw 'Aucun joueur lu : le format de reponse de Riot a peut-etre change.' }
$players = @($players | Sort-Object { Get-Random })

$seen = @{}
foreach ($puuid in $players) {
    if ($counted -ge $Games) { break }
    $ids = @(Invoke-Riot "$regionUrl/lol/match/v5/matches/by-puuid/$puuid/ids?queue=$QueueSoloDuo&startTime=$startTime&start=0&count=$MatchesPerPlayer" $headers | Where-Object { $_ })
    foreach ($id in $ids) {
        if ($counted -ge $Games) { break }
        if ($seen.ContainsKey($id)) { continue }
        $seen[$id] = $true
        $match = Invoke-Riot "$regionUrl/lol/match/v5/matches/$id" $headers
        if ($match) { Add-Match $match }
        if ($counted -gt 0 -and $counted % $SaveEvery -eq 0) {
            Save-Rates
            Write-Host "  $counted / $Games parties"
        }
    }
}

if ($counted -eq 0) { throw 'Aucune partie lue : rien n''a ete modifie.' }
Save-Rates
Write-Host "Termine : $counted parties, $($counts.Count) champions. Relance DraftMid pour utiliser ces chiffres."
