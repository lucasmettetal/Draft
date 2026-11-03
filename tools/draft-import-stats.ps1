# Importe tes parties classees solo/duo via l'API officielle de Riot,
# puis ecrit draft-stats.js a cote de draft-mid-rpcopi.html.
# La cle n'est ni affichee ni enregistree. Le fichier ne contient aucun nom de joueur.
param(
    [string[]]$Accounts = @('rpcopi#FISH', 'rpcopix#EUW'),
    [datetime]$Since = '2026-01-01',
    [string]$OutDir = $PSScriptRoot,
    [string]$TestMatchFile,
    [string]$TestPuuid
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$QueueSoloDuo = 420
$PageSize = 100
$DelayMs = 1300          # limite d'une cle personnelle : 100 requetes par 2 minutes
$MinDurationSec = 300    # en dessous, on considere la partie comme un remake
$Region = 'https://europe.api.riotgames.com'
$CachePath = Join-Path $OutDir 'draft-stats-cache.json'
$OutPath = Join-Path $OutDir 'draft-stats.js'

function Write-Utf8([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding($false)))
}

function Invoke-Riot([string]$Path, [hashtable]$Headers) {
    while ($true) {
        Start-Sleep -Milliseconds $DelayMs
        try {
            return Invoke-RestMethod -Uri ($Region + $Path) -Headers $Headers -Method Get
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

function Convert-Match($Match, [string]$Puuid, [string]$Account) {
    $info = $Match.info
    if ([int]$info.gameDuration -lt $MinDurationSec) { return $null }
    $me = $info.participants | Where-Object { $_.puuid -eq $Puuid } | Select-Object -First 1
    if (-not $me) { return $null }
    $enemies = @($info.participants | Where-Object { $_.teamId -ne $me.teamId })
    $enemyMid = $enemies | Where-Object { $_.teamPosition -eq 'MIDDLE' } | Select-Object -First 1
    $midName = ''
    if ($enemyMid) { $midName = [string]$enemyMid.championName }
    return [pscustomobject]@{
        id      = [string]$Match.metadata.matchId
        acct    = $Account
        time    = [int64]$info.gameCreation
        patch   = (([string]$info.gameVersion).Split('.')[0..1] -join '.')
        sec     = [int]$info.gameDuration
        champ   = [string]$me.championName
        pos     = [string]$me.teamPosition
        win     = [bool]$me.win
        mid     = $midName
        enemies = @($enemies | ForEach-Object { [string]$_.championName })
    }
}

function Write-Stats($Rows, [string[]]$AccountNames) {
    $sorted = @($Rows | Sort-Object time -Descending)
    $payload = [pscustomobject]@{
        generated = (Get-Date).ToUniversalTime().ToString('o')
        accounts  = @($AccountNames)
        rows      = $sorted
    }
    $json = ConvertTo-Json -InputObject $payload -Depth 5 -Compress
    Write-Utf8 $OutPath ("window.DRAFT_STATS = $json;")
    Write-Utf8 $CachePath (ConvertTo-Json -InputObject $sorted -Depth 5 -Compress)
}

if ($TestMatchFile) {
    $match = Get-Content $TestMatchFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $row = Convert-Match $match $TestPuuid 'test#TEST'
    Write-Stats @($row) @('test#TEST')
    Write-Host 'Test ecrit.'
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

$rows = New-Object System.Collections.ArrayList
$known = @{}
if (Test-Path $CachePath) {
    # En PowerShell 5.1, ConvertFrom-Json renvoie la liste d'un bloc : il faut la stocker avant de la parcourir.
    $cached = Get-Content $CachePath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($row in $cached) {
        [void]$rows.Add($row)
        $known[$row.acct + '|' + $row.id] = $true
    }
    Write-Host "$($rows.Count) parties deja en cache."
}

$startTime = [DateTimeOffset]::new($Since.ToUniversalTime()).ToUnixTimeSeconds()

foreach ($account in $Accounts) {
    $parts = $account.Split('#')
    if ($parts.Count -ne 2) { throw "Compte invalide : $account (format attendu Nom#TAG)." }
    Write-Host "Compte $account..."
    $name = [uri]::EscapeDataString($parts[0])
    $tag = [uri]::EscapeDataString($parts[1])
    $riotAccount = Invoke-Riot "/riot/account/v1/accounts/by-riot-id/$name/$tag" $headers
    if (-not $riotAccount) { Write-Host "Compte introuvable : $account"; continue }

    $ids = New-Object System.Collections.ArrayList
    $start = 0
    while ($true) {
        $page = @(Invoke-Riot "/lol/match/v5/matches/by-puuid/$($riotAccount.puuid)/ids?queue=$QueueSoloDuo&startTime=$startTime&start=$start&count=$PageSize" $headers | Where-Object { $_ })
        if ($page.Count -eq 0) { break }
        [void]$ids.AddRange($page)
        if ($page.Count -lt $PageSize) { break }
        $start += $PageSize
    }

    $missing = @($ids | Where-Object { -not $known.ContainsKey($account + '|' + $_) })
    Write-Host "$($ids.Count) parties trouvees, $($missing.Count) a telecharger."
    $done = 0
    foreach ($id in $missing) {
        $match = Invoke-Riot "/lol/match/v5/matches/$id" $headers
        $done++
        if ($match) {
            $row = Convert-Match $match $riotAccount.puuid $account
            if ($row) { [void]$rows.Add($row) }
        }
        if ($done % 25 -eq 0) {
            Write-Host "  $done / $($missing.Count)"
            Write-Stats $rows $Accounts
        }
    }
}

Write-Stats $rows $Accounts
Write-Host "Termine : $($rows.Count) parties dans $OutPath. Recharge la page."
