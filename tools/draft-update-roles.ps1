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
    [string]$PlatformUrl = "https://$Platform.api.riotgames.com",
    [string]$RegionUrl = "https://$Region.api.riotgames.com",
    [int]$MaxFailures = 5,
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

$script:lastError = ''
$script:failures = 0

# Message de Riot, sans la cle ni le puuid, qui n'y figurent pas et ne sont jamais affiches ici.
function Get-RiotMessage($ErrorRecord) {
    $body = ''
    if ($ErrorRecord.ErrorDetails) { $body = [string]$ErrorRecord.ErrorDetails.Message }
    if ($body -match '"message"\s*:\s*"([^"]{1,150})"') { return $Matches[1] }
    return 'sans detail'
}

# Renvoie $null si la requete est refusee : une seule reponse refusee n'arrete pas tout le calcul.
function Invoke-Riot([string]$Url, [hashtable]$Headers, [string]$Label) {
    while ($true) {
        Start-Sleep -Milliseconds $DelayMs
        try {
            $answer = Invoke-RestMethod -Uri $Url -Headers $Headers -Method Get
            $script:failures = 0
            return $answer
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
            $script:lastError = "$Label : code $code, $(Get-RiotMessage $_)"
            Write-Host "  Refuse par Riot - $($script:lastError)"
            $script:failures++
            if ($script:failures -ge $MaxFailures) {
                if ($counted -gt 0) { Save-Rates }
                throw "Arret apres $MaxFailures refus de suite. Dernier refus - $($script:lastError)"
            }
            return $null
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

$startTime = [DateTimeOffset]::UtcNow.AddDays(-$Days).ToUnixTimeSeconds()

$players = New-Object System.Collections.ArrayList
foreach ($tier in $Tiers) {
    Write-Host "Joueurs $tier..."
    $entries = @(Invoke-Riot "$PlatformUrl/lol/league-exp/v4/entries/RANKED_SOLO_5x5/$tier/II?page=1" $headers "liste $tier")
    $added = 0
    foreach ($entry in $entries) { if ($entry -and $entry.puuid) { [void]$players.Add([string]$entry.puuid); $added++ } }
    Write-Host "  $added joueurs"
}
if ($players.Count -eq 0) { throw 'Aucun joueur lu : le format de reponse de Riot a peut-etre change.' }
$players = @($players | Sort-Object { Get-Random })

$seen = @{}
foreach ($puuid in $players) {
    if ($counted -ge $Games) { break }
    $ids = @(Invoke-Riot "$RegionUrl/lol/match/v5/matches/by-puuid/$puuid/ids?queue=$QueueSoloDuo&startTime=$startTime&start=0&count=$MatchesPerPlayer" $headers 'historique d''un joueur' | Where-Object { $_ })
    foreach ($id in $ids) {
        if ($counted -ge $Games) { break }
        if ($seen.ContainsKey($id)) { continue }
        $seen[$id] = $true
        $match = Invoke-Riot "$RegionUrl/lol/match/v5/matches/$id" $headers 'detail d''une partie'
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
