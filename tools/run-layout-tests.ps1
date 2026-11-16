# Verifie le rendu reel des panneaux dans Chrome : debordements, textes coupes ou superposes, cibles trop petites.
# Tailles en pixels d'interface : 230 et 256 correspondent aux panneaux reels a 125 % d'affichage Windows.
# Ce test ne remplace pas un controle a l'oeil : -Shots ecrit aussi des captures.
# -Lang qps : pseudo-langue (tools/i18n/pseudo-localize.ps1), dans la copie de test seulement. Chaque texte du catalogue devient
#   [Texte accentue et allonge] : les debordements sont listes (sans faire echouer le banc) et tout texte visible hors crochets est
#   un texte reste en dur. La pseudo-langue n'est jamais livree ni proposee au joueur (draft-i18n.js ne l'accepte qu'en mode test).
# -Lang en : catalogue anglais reel (I18N-2, mode test). Les debordements font echouer le banc comme en francais ; les cles absentes
#   et le francais reste visible (lettres ou mots propres au francais, hors noms de champions) sont listes et font echouer le banc.
param([Alias('Work')][string]$Out = (Join-Path $env:TEMP 'draftmid-layout'), [switch]$Shots, [ValidateSet('', 'qps', 'en')][string]$Lang = '')

$Source = Join-Path (Split-Path $PSScriptRoot -Parent) 'web'
# Copie de test : le banc interroge la page avec eval, autorisé ici seulement, jamais dans l'application.
$Web = Join-Path $env:TEMP ('draftmid-layout-web-' + $PID)
Remove-Item -Recurse -Force $Web -ErrorAction SilentlyContinue
Copy-Item $Source $Web -Recurse
$index = Join-Path $Web 'index.html'
[IO.File]::WriteAllText($index, [IO.File]::ReadAllText($index).Replace("script-src 'self' 'unsafe-inline'", "script-src 'self' 'unsafe-inline' 'unsafe-eval'"), [Text.UTF8Encoding]::new($false))
if ($Lang -eq 'qps') {
    & (Join-Path $PSScriptRoot 'i18n\pseudo-localize.ps1') -Source (Join-Path $Source 'i18n\fr.js') -Out (Join-Path $Web 'i18n\qps.js') | Out-Null
    $idx = [IO.File]::ReadAllText($index)
    [IO.File]::WriteAllText($index, $idx.Replace('<script src="i18n/fr.js"></script>', '<script src="i18n/fr.js"></script><script src="i18n/qps.js"></script>'), [Text.UTF8Encoding]::new($false))
    $lt = Join-Path $Web 'draft-layout-tests.html'
    $page = [IO.File]::ReadAllText($lt)
    # Textes visibles hors crochets (textes en dur) et cles absentes, releves dans chaque cadre.
    $hard = 'const HARD=w=>{let names=["DRAFT","TF"];try{names=names.concat(w.eval("NAMES"))}catch(e){}let s=w.document.body.innerText+"\n"+[...w.document.querySelectorAll("[title],[aria-label],[placeholder]")].map(e=>[e.getAttribute("title"),e.getAttribute("aria-label"),e.getAttribute("placeholder")].filter(Boolean).join("\n")).join("\n");let prev;do{prev=s;s=s.replace(/\[[^\[\]]*\]/g," ")}while(s!==prev);names.sort((a,b)=>b.length-a.length).forEach(n=>{s=s.split(n).join(" ")});const lines=[...new Set(s.split("\n").map(x=>x.replace(/\s+/g," ").trim()).filter(x=>/\p{L}{2}/u.test(x)))];return{words:lines.slice(0,40),missing:w.I18N?w.I18N.missing():["I18N absent"]}};'
    $page = $page.Replace("'index.html?panel='", "'index.html?lang=qps&i18ncheck=1&panel='").Replace('(async()=>{const o=', $hard + '(async()=>{const o=').Replace('o.frames[f.dataset.k]=measure(f.contentWindow)', 'o.frames[f.dataset.k]=Object.assign(measure(f.contentWindow),{pseudo:HARD(f.contentWindow)})')
    [IO.File]::WriteAllText($lt, $page, [Text.UTF8Encoding]::new($false))
}
if ($Lang -eq 'en') {
    $lt = Join-Path $Web 'draft-layout-tests.html'
    $page = [IO.File]::ReadAllText($lt)
    # Francais reste visible (texte, title, aria-label, placeholder), noms de champions et nom de la langue francaise (selecteur, I18N-3) retires ; cles absentes.
    $hard = 'const HARD=w=>{let names=["DRAFT","Fran\u00e7ais"];try{names=names.concat(w.eval("NAMES"))}catch(e){}let s=w.document.body.innerText+"\n"+[...w.document.querySelectorAll("[title],[aria-label],[placeholder]")].map(e=>[e.getAttribute("title"),e.getAttribute("aria-label"),e.getAttribute("placeholder")].filter(Boolean).join("\n")).join("\n");names.sort((a,b)=>b.length-a.length).forEach(n=>{s=s.split(n).join(" ")});const fr=/[\u00e0\u00e2\u00e7\u00e9\u00e8\u00ea\u00eb\u00ee\u00ef\u00f4\u00fb\u00f9\u0153\u00ab\u00bb]|\b(le|la|les|des|du|de|et|ou|pas|pour|avec|sur|dans|ton|ta|tes|une|est|sont|contre|partie|parties)\b/i;const lines=[...new Set(s.split("\n").map(x=>x.replace(/\s+/g," ").trim()).filter(x=>fr.test(x)))];return{words:lines.slice(0,40),missing:w.I18N?w.I18N.missing():["I18N absent"]}};'
    $page = $page.Replace("'index.html?panel='", "'index.html?lang=en&i18ncheck=1&panel='").Replace('(async()=>{const o=', $hard + '(async()=>{const o=').Replace('o.frames[f.dataset.k]=measure(f.contentWindow)', 'o.frames[f.dataset.k]=Object.assign(measure(f.contentWindow),{pseudo:HARD(f.contentWindow)})')
    [IO.File]::WriteAllText($lt, $page, [Text.UTF8Encoding]::new($false))
}
$Chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Chrome) { throw 'Chrome introuvable.' }
New-Item -ItemType Directory -Force $Out | Out-Null
$Base = 'file:///' + ($Web -replace '\\', '/') + '/draft-layout-tests.html'
$Common = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=$Out\profil-$PID", '--window-size=1960,1120')
$Hard = '^(PAGE-X|TEXTE-|BOITE-|COUPE-X|DEFILE-X|CIBLE|TEXTES-|BOUTON-|PETIT-TEXTE|ACTION-|CROIX-)'
$OfflineArg = '--host-resolver-rules=MAP * ~NOTFOUND'

$Runs = @()
foreach ($s in 'empty', 'one', 'full', 'midunknown', 'close', 'excluded', 'none', 'picked', 'pickedother', 'history', 'extreme') { $Runs += , @($s, "state=$s&frozen=1", $false) }
$Runs += , @('egalite jungle, choix hors egalite', 'state=tieother&profile=jungle&frozen=1', $false)
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
# Mes statistiques (tableau de bord) : historiques fictifs de 93 et 400 parties, données incomplètes, onglets Journal et Historique Riot.
foreach ($st in 'stats93', 'statsrich', 'statsincomplete') { $Runs += , @("statistiques $st", "state=$st&set=full&page=stats&host=on", $false) }
$Runs += , @('statistiques journal', 'state=statsrich&set=full&page=stats&section=journal&host=on', $false)
$Runs += , @('statistiques historique riot', 'state=statsrich&set=full&page=stats&section=history', $false)
foreach ($v in 'home', 'pool', 'stats') { $Runs += , @("section $v", "state=history&set=full&page=$v&host=on", $false); $Runs += , @("section $v hors application", "state=empty&set=full&page=$v", $false) }
foreach ($s in 'general', 'league', 'display', 'data', 'updates', 'diag') { $Runs += , @("reglages $s", "state=full&set=full&page=settings&section=$s&host=on", $false) }
$Runs += , @('mon pool sans profil', 'state=full&set=full&page=pool&profile=none', $false)
# UX-8 : Accueil, chaque etat : premier lancement (profil vierge), configure avec LoL ferme, connecte, erreur de connexion, draft en cours.
$Runs += , @('accueil vierge', 'state=empty&set=full&page=home&profile=none&host=off', $false)
$Runs += , @('accueil vierge hors application', 'state=empty&set=full&page=home&profile=none', $false)
$Runs += , @('accueil configure lol ferme', 'state=empty&set=full&page=home&host=waiting', $false)
$Runs += , @('accueil configure connecte', 'state=empty&set=full&page=home&host=on', $false)
$Runs += , @('accueil configure lecture desactivee', 'state=empty&set=full&page=home&host=off', $false)
$Runs += , @('accueil erreur de connexion', 'state=empty&set=full&page=home&host=failed', $false)
$Runs += , @('accueil draft en cours', 'state=live&set=full&page=home&host=on', $false)
$Runs += , @('sans reseau', 'state=full', $true)

$failures = 0
$qpsHard = @{}; $qpsMissing = @{}; $qpsOverflow = 0
# Anglais : donnees fictives de l'etat 'extreme' (noms et raisons tres longs, ecrits en francais pour le test), pas de l'interface.
# Depuis I18N-3, l'etat simule du programme (phase, placement) passe par des cles : plus aucune exception.
$enFixtures = @()
foreach ($run in $Runs) {
    $args_ = $Common + @('--virtual-time-budget=120000', '--dump-dom')
    if ($run[2]) { $args_ += $OfflineArg }
    $dom = & $Chrome @args_ "$Base`?$($run[1])" 2>$null | Out-String
    $json = [Net.WebUtility]::HtmlDecode([regex]::Match($dom, '<pre id="out"[^>]*>([\s\S]*?)</pre>').Groups[1].Value)
    if (-not $json) { Write-Host "ECHEC : $($run[0]) : aucune mesure"; $failures++; continue }
    $found = @{}
    if ($Lang -eq 'qps' -or $Lang -eq 'en') {
        foreach ($frame in ($json | ConvertFrom-Json).frames.PSObject.Properties) { foreach ($w in $frame.Value.pseudo.words) { if ($Lang -eq 'en' -and ($run[0] -eq 'extreme' -or @($enFixtures | Where-Object { $w -like $_ }).Count)) { continue }; $script:qpsHard[$w] = 1 + [int]$script:qpsHard[$w] }; foreach ($m in $frame.Value.pseudo.missing) { $script:qpsMissing[$m] = 1 } }
    }
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
    if ($found.Count -and $Lang -eq 'qps') { $qpsOverflow += $found.Count; Write-Host "PSEUDO : $($run[0]) : $($found.Count) debordement(s)"; foreach ($k in ($found.Keys | Sort-Object)) { Write-Host "  $($k.Substring(0, [Math]::Min(110, $k.Length))) <= $(($found[$k] | Select-Object -First 4) -join ', ')" } }
    elseif ($found.Count) {
        $failures += $found.Count
        Write-Host "ECHEC : $($run[0]) : $($found.Count) defaut(s)"
        foreach ($k in ($found.Keys | Sort-Object)) { Write-Host "  $($k.Substring(0, [Math]::Min(110, $k.Length))) <= $(($found[$k] | Select-Object -First 4) -join ', ')" }
    } else { Write-Host "OK : $($run[0]) : $(@($frames.PSObject.Properties).Count) tailles" }
    if ($Shots) {
        $name = $run[0] -replace '[^a-z0-9]', '-'
        & $Chrome @($Common + @('--hide-scrollbars', '--virtual-time-budget=120000', "--screenshot=$Out\$name.png")) "$Base`?$($run[1])&shot=1&h=576" 2>$null | Out-Null
    }
}
if ($Lang -eq 'qps') {
    Write-Host "Pseudo-langue : $qpsOverflow debordement(s) liste(s) ci-dessus ; textes hors crochets : $($qpsHard.Count) ; cles ou variables absentes : $($qpsMissing.Count)"
    if ($qpsHard.Count) { $qpsHard.Keys | Sort-Object | ForEach-Object { Write-Host "  Hors crochets : $_" } }
    if ($qpsMissing.Count) { Write-Host ("  Absentes : " + (($qpsMissing.Keys | Sort-Object) -join ' | ')) }
    if ($qpsMissing.Count) { exit 1 }; exit 0
}
if ($Lang -eq 'en') {
    Write-Host "Anglais : francais visible : $($qpsHard.Count) ; cles ou variables absentes : $($qpsMissing.Count)"
    if ($qpsHard.Count) { $qpsHard.Keys | Sort-Object | ForEach-Object { Write-Host "  Francais visible : $_" } }
    if ($qpsMissing.Count) { Write-Host ("  Absentes : " + (($qpsMissing.Keys | Sort-Object) -join ' | ')) }
    if ($qpsHard.Count -or $qpsMissing.Count) { $failures++ }
}
if ($failures) { Write-Host "$failures DEFAUT(S)"; exit 1 }
Write-Host 'TOUT EST BON'
