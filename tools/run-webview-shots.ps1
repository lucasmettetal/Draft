# Captures de la VRAIE application DRAFT (WinForms + WebView2), fenêtre par fenêtre, pour comparer deux versions.
# À lancer avec Windows PowerShell 5.1 :
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-webview-shots.ps1 -Version apres -Out docs\ux\captures-webview2\apres
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-webview-shots.ps1 -Version avant -Exe <exe de 4dd76f9> -Out docs\ux\captures-webview2\avant
# Sans -Exe, la copie de travail est compilée (build.ps1) dans un dossier temporaire neuf.
#
# Sécurité :
# - données : copie neuve des données d'essai dans un dossier temporaire (jamais %LOCALAPPDATA%\DraftMid), compte du journal anonymisé ;
# - options : --data <copie> --no-hotkey --run-key Software\DRAFT-Shots\Run ; la clé HKCU\Software\DRAFT-Shots est retirée à la fin ;
# - réseau : WebView2 reçoit --host-resolver-rules="MAP * ~NOTFOUND" (portraits Data Dragon : seulement ceux déjà en cache) ;
# - CDP : port local 127.0.0.1, passé par variable d'environnement au SEUL processus lancé ;
# - capture : PrintWindow (PW_RENDERFULLCONTENT = 2) sur les fenêtres du pid lancé, pid vérifié à chaque image ; jamais l'écran entier ;
# - arrêt : seulement le pid lancé, après vérification du chemin de l'exe. Aucun autre DRAFT n'est touché.
#
# Tailles (-Sizes) :
#   1366x768, 1920x1080 : zone cliente en pixels physiques (la page en voit autant si son devicePixelRatio vaut 1, cas mesuré à 125 %) ;
#                         Windows borne la zone cliente à l'écran : 1920 x 1080 devient 1920 x 1055 sur un écran 1080p ;
#   css1366x768         : zone cliente multipliée par le devicePixelRatio de la page, pour qu'elle voie 1366 x 768 CSS.
param(
  [Parameter(Mandatory = $true)][ValidateSet('avant', 'apres')][string]$Version,
  [Parameter(Mandatory = $true)][string]$Out,
  [string]$Exe,
  [string]$TestData = 'C:\Users\lucas\AppData\Local\Temp\draft-essai-2162\data',
  [string[]]$Sizes = @('1366x768', '1920x1080'),
  [int]$Port = 9371,
  [int]$SettleMs = 1200,
  [string]$Work = (Join-Path $env:TEMP ('draft-webview-shots-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $PID))
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'shots\ShotsLib.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$RunKey = 'Software\DRAFT-Shots\Run'

if (Get-Process -Name 'LeagueClientUx', 'LeagueClient' -ErrorAction SilentlyContinue) { throw 'Client LoL ouvert : DRAFT y lirait un vrai compte. Ferme-le avant les captures.' }
if (Test-Path $Work) { throw "Dossier de travail déjà présent : $Work" }
New-Item -ItemType Directory $Work | Out-Null
if (-not $Exe) {
  & (Join-Path $repo 'build.ps1') -Out (Join-Path $Work 'build') | Out-Null
  $Exe = Join-Path $Work 'build\DRAFT-Portable.exe'
}
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$data = Join-Path $Work 'data'
New-ShotData $TestData $data

# ---------- Écrans à capturer ----------
# JS exécuté dans la fenêtre principale ; « c » clique un bouton par son texte.
$prelude = "const c=(t,s)=>{const b=[...document.querySelectorAll(s||'button')].find(x=>x.textContent.trim()===t);if(!b)throw new Error('introuvable : '+t);b.click();};"
if ($Version -eq 'apres') {
  $steps = @(
    @('accueil', "goView('home')"), @('mon-pool', "goView('pool')"),
    @('simulateur', "goView('sim');openViewInPage('plan','')"),
    @('simulateur-criteres', "openViewInPage('criteria','')"), @('simulateur-comparer', "openViewInPage('compare','')"),
    @('simulateur-regles', "openViewInPage('rules','')"), @('simulateur-conseils', "openViewInPage('coach','')"),
    @('simulateur-duel', "openViewInPage('matchups','')"),
    @('mes-statistiques', "goView('stats')"),
    @('reglages-general', "goView('settings','general')"), @('reglages-connexion', "goView('settings','league')"),
    @('reglages-affichage', "goView('settings','display')"), @('reglages-donnees', "goView('settings','data')"),
    @('reglages-mises-a-jour', "goView('settings','updates')"), @('reglages-diagnostic', "goView('settings','diag')")
  )
} else {
  # Avant la refonte : une fenêtre à onglets (Plan, Analyse, Conseils, Duel, Parties) et une modale de réglages.
  $esc = "document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))"
  $steps = @(
    @('fenetre', "if(document.querySelector('.settings'))$esc;c('Plan')"),
    @('onglet-analyse', "c('Analyse')"), @('onglet-analyse-comparer', "c('Comparer')"), @('onglet-analyse-regles', "c('Règles')"),
    @('onglet-conseils', "c('Conseils')"), @('onglet-duel', "c('Duel')"), @('onglet-parties', "c('Parties')"),
    @('reglages-modale', "c('Plan');c('Réglages')")
  )
}

# ---------- Lancement ----------
$p = Start-ShotApp -Exe $Exe -Data $data -Port $Port -RunKey $RunKey
$appPid = $p.Id
$rows = New-Object Collections.Generic.List[object]
function Add-Shot([string]$File, [long]$Handle, [string]$Size, $Target, [string]$Note) {
  $info = ([DraftShots]::Shot([IntPtr]$Handle, [uint32]$appPid, $File)).Split('|')
  $view = if ($Target) { Invoke-CdpEval $Target "JSON.stringify({w:innerWidth,h:innerHeight,dpr:devicePixelRatio,ox:Math.max(0,document.documentElement.scrollWidth-innerWidth),below:Math.max(0,...[...document.querySelectorAll('*')].filter(e=>e.clientHeight>200&&e.scrollHeight>e.clientHeight+4&&getComputedStyle(e).overflowY!=='visible').map(e=>e.scrollHeight-e.clientHeight),document.documentElement.scrollHeight-innerHeight)})" | ForEach-Object { $script:Json.DeserializeObject($_) } } else { $null }
  $ok = ([int]$info[1] -gt 6) -and ([int]$info[2] -gt 30)
  $rows.Add([pscustomobject]@{
      fichier = Split-Path $File -Leaf; pid = $appPid; taille_demandee = $Size; fenetre = $info[0]; zone_cliente = [DraftShots]::Client([IntPtr]$Handle)
      page_css = $(if ($view) { "$($view['w'])x$($view['h'])" } else { '' }); dpr = $(if ($view) { $view['dpr'] } else { '' })
      debord_horizontal_px = $(if ($view) { $view['ox'] } else { '' }); contenu_sous_le_pli_px = $(if ($view) { $view['below'] } else { '' })
      luminosite = $info[1]; couleurs = $info[2]; valide = $ok; note = $Note
    })
  if (-not $ok) { Write-Warning "Image douteuse (noire ou uniforme) : $File" }
  "  $(Split-Path $File -Leaf) : $($info[0]), page $(if ($view) { "$($view['w'])x$($view['h'])" })"
}

try {
  $full = Get-CdpTarget $Port 'full' 60
  if (-not $full) { throw 'La fenêtre principale ne s''est pas ouverte (CDP).' }
  $win = $null
  for ($i = 0; $i -lt 100 -and -not $win; $i++) { $win = Get-ProcWindow $appPid 'DRAFT'; if (-not $win) { Start-Sleep -Milliseconds 200 } }
  if (-not $win) { throw 'Fenêtre principale introuvable pour le pid lancé.' }
  for ($i = 0; $i -lt 100; $i++) { if ((Invoke-CdpEval $full "document.readyState==='complete'&&!!document.querySelector('#root, #app, body > div')") -eq $true) { break }; Start-Sleep -Milliseconds 200 }
  Start-Sleep -Seconds 3
  $browser = $script:Json.DeserializeObject((New-Object Net.WebClient).DownloadString("http://127.0.0.1:$Port/json/version"))['Browser']
  $winScale = [DraftShots]::WindowDpi([IntPtr]$win.Handle) / 96.0
  # Échelle de la PAGE (devicePixelRatio) : DRAFT la laisse à 1 même quand Windows est à 125 % (mesuré).
  $scale = [double](Invoke-CdpEval $full 'devicePixelRatio')
  "DRAFT lancé : pid $appPid, $Exe, WebView2 $browser, échelle Windows $([int]($winScale * 100)) %, échelle de la page $([int]($scale * 100)) %"

  # Panneaux de l'overlay, avec une draft simulée par les opérations de test de la page (draftApi).
  [void](Invoke-CdpEval $full "hostSend({cmd:'hideAll'});1")
  $right = Get-CdpTarget $Port 'right' 30
  $left = Get-CdpTarget $Port 'left' 30
  if ($right -and $left) {
    for ($i = 0; $i -lt 50; $i++) { if ((Invoke-CdpEval $right "typeof draftApi!=='undefined'&&!!document.querySelector('[data-test=panel], .panel, main, #root > *')") -eq $true) { break }; Start-Sleep -Milliseconds 200 }
    [void](Invoke-CdpEval $right "draftApi.setActive(true);['Syndra','Ornn','Lee Sin','Jinx'].forEach(n=>draftApi.add(n));1")
    Start-Sleep -Milliseconds 2500
    foreach ($n in 'left', 'right') {
      $w = Get-ProcWindow $appPid "DRAFT : $n"
      $t = if ($n -eq 'left') { $left } else { $right }
      if ($w) { Add-Shot (Join-Path $Out "$Version-panneau-$($n -replace 'left','gauche' -replace 'right','droit').png") $w.Handle 'placement de DRAFT' $t 'draft simulée : 4 adversaires' }
    }
    [void](Invoke-CdpEval $full "hostSend({cmd:'toggleCompact'});1")
    $compact = Get-CdpTarget $Port 'compact' 30
    Start-Sleep -Milliseconds 2500
    $w = Get-ProcWindow $appPid 'DRAFT : compact'
    if ($w) { Add-Shot (Join-Path $Out "$Version-panneau-unique.png") $w.Handle 'placement de DRAFT' $compact 'draft simulée : 4 adversaires' }
    [void](Invoke-CdpEval $full "hostSend({cmd:'toggleCompact'});1")
    Start-Sleep -Milliseconds 800
    [void](Invoke-CdpEval $full "hostSend({cmd:'hideAll'});1")
    Start-Sleep -Milliseconds 800
  } else { Write-Warning 'Panneaux non ouverts.' }

  foreach ($size in $Sizes) {
    $css = $size.StartsWith('css')
    $wh = $size.Replace('css', '').Split('x') | ForEach-Object { [int]$_ }
    $cw = if ($css) { [int][Math]::Round($wh[0] * $scale) } else { $wh[0] }
    $ch = if ($css) { [int][Math]::Round($wh[1] * $scale) } else { $wh[1] }
    $got = [DraftShots]::SetClient([IntPtr]$win.Handle, 0, 0, $cw, $ch)
    "Taille $size : zone cliente demandée ${cw}x${ch}, obtenue $got"
    Start-Sleep -Milliseconds 1500
    foreach ($s in $steps) {
      [void](Invoke-CdpEval $full "(()=>{$prelude $($s[1]);return 1})()")
      Start-Sleep -Milliseconds $SettleMs
      [void](Invoke-CdpEval $full "(()=>{if(document.activeElement)document.activeElement.blur();document.querySelectorAll('*').forEach(e=>{if(e.scrollTop)e.scrollTop=0});scrollTo(0,0);return 1})()")
      Start-Sleep -Milliseconds 300
      Add-Shot (Join-Path $Out "$Version-$($s[0])-$size.png") $win.Handle $size $full ''
      # La modale de réglages d'avant défile : une image par écran jusqu'en bas.
      if ($s[0] -eq 'reglages-modale') {
        for ($k = 2; $k -le 8; $k++) {
          $moved = Invoke-CdpEval $full "(()=>{const e=[...document.querySelectorAll('.settings, .settings *')].find(x=>x.scrollHeight>x.clientHeight+4&&getComputedStyle(x).overflowY!=='visible')||document.querySelector('.settings');const b=e.scrollTop;e.scrollTop=b+e.clientHeight*0.9;return e.scrollTop>b})()"
          if ($moved -ne $true) { break }
          Start-Sleep -Milliseconds 500
          Add-Shot (Join-Path $Out "$Version-reglages-modale-$k-$size.png") $win.Handle $size $full "défilement $k"
        }
      }
    }
  }
} finally {
  if (Stop-ShotApp $appPid $Exe) { "Instance $appPid arrêtée." } else { Write-Warning "Instance $appPid déjà fermée ou étrangère : rien arrêté." }
  if (Test-Path 'HKCU:\Software\DRAFT-Shots') { Remove-Item 'HKCU:\Software\DRAFT-Shots' -Recurse; 'Clé HKCU\Software\DRAFT-Shots retirée.' }
  $rows | Export-Csv (Join-Path $Out "manifeste-$Version.csv") -NoTypeInformation -Encoding UTF8 -Delimiter ';'
}
$bad = @($rows | Where-Object { -not $_.valide })
"$($rows.Count) images, $($bad.Count) douteuse(s). Manifeste : $(Join-Path $Out "manifeste-$Version.csv")"
