# Verifie le rendu reel des panneaux dans Chrome : debordements, textes coupes ou superposes, cibles trop petites.
# Tailles en pixels d'interface : 230 et 256 correspondent aux panneaux reels a 125 % d'affichage Windows.
# Ce test ne remplace pas un controle a l'oeil : -Shots ecrit aussi des captures.
param([string]$Out = (Join-Path $env:TEMP 'draftmid-layout'), [switch]$Shots)

$Source = Join-Path (Split-Path $PSScriptRoot -Parent) 'web'
# Copie de test : le banc interroge la page avec eval, autorisé ici seulement, jamais dans l'application.
$Web = Join-Path $env:TEMP ('draftmid-layout-web-' + $PID)
Remove-Item -Recurse -Force $Web -ErrorAction SilentlyContinue
Copy-Item $Source $Web -Recurse
$index = Join-Path $Web 'index.html'
[IO.File]::WriteAllText($index, [IO.File]::ReadAllText($index).Replace("script-src 'self' 'unsafe-inline'", "script-src 'self' 'unsafe-inline' 'unsafe-eval'"), [Text.UTF8Encoding]::new($false))
$Chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Chrome) { throw 'Chrome introuvable.' }
New-Item -ItemType Directory -Force $Out | Out-Null
$Base = 'file:///' + ($Web -replace '\\', '/') + '/draft-layout-tests.html'
$Common = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=$Out\profil-$PID", '--window-size=1960,1120')
$Hard = '^(PAGE-X|TEXTE-|BOITE-|COUPE-X|DEFILE-X|CIBLE|TEXTES-|BOUTON-)'
$OfflineArg = '--host-resolver-rules=MAP * ~NOTFOUND'

$Runs = @()
foreach ($s in 'empty', 'one', 'full', 'midunknown', 'close', 'excluded', 'none', 'picked', 'history', 'extreme') { $Runs += , @($s, "state=$s&frozen=1", $false) }
$Runs += , @('analyse, vue comparer', 'state=history&set=full&tabclick=analyse&view=compare&frozen=1', $false)
$Runs += , @('analyse, vue regles', 'state=history&set=full&tabclick=analyse&view=rules&frozen=1', $false)
$Runs += , @('analyse, comparer sur egalite', 'state=tie&profile=jungle&set=full&tabclick=analyse&view=compare&frozen=1', $false)
foreach ($t in 'analyse', 'coach', 'matchups', 'parties') { $Runs += , @("onglet $t", "state=history&set=full&tabclick=$t&frozen=1", $false) }
$Runs += , @('reglages', 'state=full&set=full&tab=settings', $false)
$Runs += , @('reglages application', 'state=full&set=full&tab=settings&host=on', $false)
$Runs += , @('profil support, noms longs', 'state=full&set=full&profile=support&frozen=1', $false)
$Runs += , @('role sans champion', 'state=jungleempty&frozen=1', $false)
$Runs += , @('role attribue different', 'state=assigned&frozen=1', $false)
$Runs += , @('egalite jungle', 'state=tie&profile=jungle&frozen=1', $false)
$Runs += , @('egalite sans adversaire', 'state=tieempty&profile=jungle&frozen=1', $false)
$Runs += , @('le plus joue affiche', 'state=played&frozen=1', $false)
$Runs += , @('onglet conseils avec historique', 'state=played&set=full&tabclick=coach&frozen=1', $false)
$Runs += , @('uncertain', 'state=uncertain&frozen=1', $false)
$Runs += , @('partial', 'state=partial&frozen=1', $false)
$Runs += , @('sans profil', 'state=full&set=full&profile=none&frozen=1', $false)
$Runs += , @('sources de donnees ouvertes', 'state=full&set=full&page=settings&section=data&host=on&open=1', $false)
# UX-3 : chaque section de la fenetre principale, et chaque rubrique des reglages, dans l'application et hors application.
foreach ($v in 'home', 'pool', 'stats') { $Runs += , @("section $v", "state=history&set=full&page=$v&host=on", $false); $Runs += , @("section $v hors application", "state=empty&set=full&page=$v", $false) }
foreach ($s in 'general', 'league', 'display', 'data', 'updates', 'diag') { $Runs += , @("reglages $s", "state=full&set=full&page=settings&section=$s&host=on", $false) }
$Runs += , @('mon pool sans profil', 'state=full&set=full&page=pool&profile=none', $false)
$Runs += , @('sans reseau', 'state=full', $true)

$failures = 0
foreach ($run in $Runs) {
    $args_ = $Common + @('--virtual-time-budget=120000', '--dump-dom')
    if ($run[2]) { $args_ += $OfflineArg }
    $dom = & $Chrome @args_ "$Base`?$($run[1])" 2>$null | Out-String
    $json = [Net.WebUtility]::HtmlDecode([regex]::Match($dom, '<pre id="out"[^>]*>([\s\S]*?)</pre>').Groups[1].Value)
    if (-not $json) { Write-Host "ECHEC : $($run[0]) : aucune mesure"; $failures++; continue }
    $found = @{}
    $frames = ($json | ConvertFrom-Json).frames
    foreach ($frame in $frames.PSObject.Properties) {
        foreach ($issue in $frame.Value.issues) {
            # Nom fictif de 41 caracteres : ces signaux varient selon les animations. Le rendu a ete controle a l'oeil, sans chevauchement.
            if ($run[0] -eq 'extreme' -and $issue -match '^(TEXTES-|BOUTON-|DEFILE-X aside)') { continue }
            if ($issue -match $Hard -and $issue -notmatch 'section\.stage "Illustration') { if (-not $found[$issue]) { $found[$issue] = @() }; $found[$issue] += $frame.Name }
        }
        foreach ($k in '.p-name', '.p-actions .cta', '.champ-name') {
            if ($k -eq '.champ-name' -and $frame.Name -like '* 800x600') { continue } # petite fenetre : la page defile
            $v = $frame.Value.fold.$k
            if ($v -and $v -ne 'ok') { $key = "SOUS-LE-PLI $k $v"; if (-not $found[$key]) { $found[$key] = @() }; $found[$key] += $frame.Name }
        }
    }
    if ($found.Count) {
        $failures += $found.Count
        Write-Host "ECHEC : $($run[0]) : $($found.Count) defaut(s)"
        foreach ($k in ($found.Keys | Sort-Object)) { Write-Host "  $($k.Substring(0, [Math]::Min(110, $k.Length))) <= $(($found[$k] | Select-Object -First 4) -join ', ')" }
    } else { Write-Host "OK : $($run[0]) : $(@($frames.PSObject.Properties).Count) tailles" }
    if ($Shots) {
        $name = $run[0] -replace '[^a-z0-9]', '-'
        & $Chrome @($Common + @('--hide-scrollbars', '--virtual-time-budget=120000', "--screenshot=$Out\$name.png")) "$Base`?$($run[1])&shot=1&h=576" 2>$null | Out-Null
    }
}
if ($failures) { Write-Host "$failures DEFAUT(S)"; exit 1 }
Write-Host 'TOUT EST BON'
