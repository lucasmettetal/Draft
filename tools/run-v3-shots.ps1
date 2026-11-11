# Captures de la fenêtre principale, section par section, aux tailles 1366 x 768 et 1920 x 1080 (Chrome sans écran, même page).
# Copie de travail sous %TEMP% : aucune donnée personnelle (profil de test Mid), hors ligne (portraits remplacés par leurs initiales).
# Exemples :
#   tools\run-v3-shots.ps1 -Web <dossier web> -Out docs\ux\captures-v3 -Prefix apres
#   tools\run-v3-shots.ps1 -Web <copie de web de main> -Out docs\ux\captures-v3 -Prefix avant -Before
param(
  [Parameter(Mandatory = $true)][string]$Web,
  [Parameter(Mandatory = $true)][string]$Out,
  [Parameter(Mandatory = $true)][string]$Prefix,
  [switch]$Before,
  [string]$Work = (Join-Path $env:TEMP ('draft-v3-shots-' + $PID))
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Out | Out-Null
$work = Join-Path $Work 'web'
Copy-Item $Web $work -Recurse
Remove-Item (Join-Path $work 'draft-role-rates.js') -ErrorAction SilentlyContinue
# État de démonstration injecté dans la COPIE seulement : application connectée, adversaires donnés par l'adresse.
$h = [IO.File]::ReadAllText((Join-Path $work 'index.html'), [Text.Encoding]::UTF8)
$h = $h.Replace('</body>', '<style>*,*::before,*::after{animation:none!important;transition:none!important}[style*=opacity]{opacity:1!important}.role-flash,.rank-glow,.meter-delta{display:none!important}</style><script>window.addEventListener("load",()=>setTimeout(()=>{const q=new URLSearchParams(location.search);Object.assign(hostState,{available:true,lcuConfirmed:true,lcuEnabled:true,lcuStatus:"connected",hotkey:true,autoDraft:true,follow:true,phase:"Client ouvert, hors draft",placement:"Panneaux placés de chaque côté du client.",updateStatus:""});(q.get("enemies")||"").split(",").filter(Boolean).forEach(n=>addEnemy(n));rerender();if(document.activeElement)document.activeElement.blur();},500));</script></body>')
[IO.File]::WriteAllText((Join-Path $work 'index.html'), $h, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $work 'seed.html'), "<script>localStorage.setItem('draftProfile',JSON.stringify({role:'mid',pool:['TF','Fizz',`"Kog'Maw`",'Xerath']}));localStorage.setItem('draftPatch','16.22');location.replace(new URLSearchParams(location.search).get('to'))</script>", [Text.UTF8Encoding]::new($false))
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$enemies = 'Syndra,Ornn,Lee Sin,Jinx'
$shots = if ($Before) {
  @(@('fenetre', "panel=full&enemies=$enemies"), @('reglages', 'panel=full&tab=settings'), @('parties', "panel=full&tab=parties&enemies=$enemies"))
} else {
  @(@('accueil', 'panel=full&view=home'), @('mon-pool', 'panel=full&view=pool'), @('simulateur', "panel=full&view=sim&enemies=$enemies"), @('mes-statistiques', 'panel=full&view=stats'),
    @('reglages-general', 'panel=full&view=settings&section=general'), @('reglages-connexion', 'panel=full&view=settings&section=league'), @('reglages-affichage', 'panel=full&view=settings&section=display'),
    @('reglages-donnees', 'panel=full&view=settings&section=data'), @('reglages-mises-a-jour', 'panel=full&view=settings&section=updates'), @('reglages-diagnostic', 'panel=full&view=settings&section=diag'))
}
foreach ($size in @(@(1366, 768), @(1920, 1080))) {
  foreach ($s in $shots) {
    $file = "$Prefix-$($s[0])-$($size[0])x$($size[1]).png"
    $prof = Join-Path $Work "pf-$($s[0])-$($size[0])"
    $to = 'index.html?' + $s[1].Replace(' ', '%20')
    $url = "file:///$($work -replace '\\','/')/seed.html?to=" + [uri]::EscapeDataString($to)
    $a = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', '--force-device-scale-factor=1', "--user-data-dir=`"$prof`"", "--window-size=$($size[0]),$($size[1])", '--hide-scrollbars', '--virtual-time-budget=8000', '"--host-resolver-rules=MAP * ~NOTFOUND"', "--screenshot=`"$(Join-Path (Resolve-Path $Out) $file)`"", "`"$url`"")
    $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden
    if (-not $p.WaitForExit(60000)) { "Délai : $file" }
    Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    $file
  }
}
