# Captures comparables de l'interface, sur une copie de travail : chaque état, aux tailles réelles.
# Simulation dans Chrome : ne remplace pas un contrôle dans la vraie application.
# Exemple : .\tools\run-ux-shots.ps1 -Name avant
param(
  [Parameter(Mandatory = $true)][string]$Name,
  [string]$OutRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-ux-captures'),
  [string[]]$Only
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$out = Join-Path $OutRoot $Name; $work = Join-Path $env:TEMP "draftmid-ux-$Name"
Remove-Item -Recurse -Force $work, $out -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item (Join-Path $repo 'web') $work -Recurse
# Comme la version installée : sans Meraki.
Remove-Item (Join-Path $work 'draft-role-rates.js') -ErrorAction SilentlyContinue
Remove-Item (Join-Path $work 'draft-stats.js') -ErrorAction SilentlyContinue
$live = Join-Path $env:LOCALAPPDATA 'DraftMid\draft-role-rates-live.js'; if (Test-Path $live) { Copy-Item $live $work }

$h = [IO.File]::ReadAllText((Join-Path $work 'index.html'), [Text.Encoding]::UTF8)
$m = [regex]::Match($h, 'computeVerdict\(\);\s*ReactDOM\.createRoot'); if (-not $m.Success) { throw "Point d'insertion absent." }
$h = $h.Insert($m.Index, '{const q=new URLSearchParams(location.search).get("enemies");if(q)q.split(",").forEach(n=>{state.enemies.push(n);computeVerdict();});}' + "`n")
$h = $h.Replace('</body>', '<style>*,*::before,*::after{animation:none!important;transition:none!important}</style><script>window.addEventListener("load",()=>setTimeout(()=>{const q=new URLSearchParams(location.search);if(q.get("host")){Object.assign(hostState,{available:true,lcuConfirmed:true,lcuEnabled:true,lcuStatus:"connected",hotkey:true,autoDraft:true,follow:true,phase:"Client ouvert, hors draft"});rerender();}if(q.get("pick")&&V&&V.shown)choosePick(V.shown);if(document.activeElement)document.activeElement.blur();},400));</script></body>')
[IO.File]::WriteAllText((Join-Path $work 'index.html'), $h, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $work 'seed.html'), "<script>const q=new URLSearchParams(location.search);localStorage.setItem('draftProfile',q.get('profile'));localStorage.setItem('draftPatch','16.22');if(q.get('prefs'))localStorage.setItem('draftPrefs',q.get('prefs'));location.replace(q.get('to'))</script>")
# Planche : les panneaux à leurs tailles réelles, côte à côte. 256 correspond à 320 pixels physiques à 125 %.
$sheet = @'
<!DOCTYPE html><meta charset="utf-8"><style>html,body{margin:0;background:#2a2f36;overflow:hidden}iframe{position:absolute;top:8px;border:0;background:#0a0f16}</style><script>
const q=new URLSearchParams(location.search);const add=(panel,left,w,h,extra)=>{const f=document.createElement("iframe");f.src="index.html?panel="+panel+"&enemies="+encodeURIComponent(q.get("enemies")||"")+(q.get("pick")?"&pick=1":"")+(extra||"");f.style.cssText=`left:${left}px;width:${w}px;height:${h}px`;document.documentElement.appendChild(f)};
add("left",8,256,704);add("right",272,256,704);add("right",536,340,704);add("compact",884,256,704);add("compact",1148,300,600);
</script>
'@
[IO.File]::WriteAllText((Join-Path $work 'sheet.html'), $sheet, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $work 'frame.html'), '<!DOCTYPE html><meta charset="utf-8"><style>html,body{margin:0;background:#0a0f16;overflow:hidden}iframe{position:absolute;left:0;top:0;border:0}</style><script>const q=new URLSearchParams(location.search);const f=document.createElement("iframe");f.style.width=q.get("w")+"px";f.style.height=q.get("h")+"px";f.src="index.html"+location.search;document.documentElement.appendChild(f)</script>', [Text.UTF8Encoding]::new($false))

$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
function Shot($file, $w, $h, $to, $profile, $prefs, [switch]$Offline) {
  $prof = Join-Path $work "pf\$file"
  $url = "file:///$($work -replace '\\','/')/seed.html?profile=" + [uri]::EscapeDataString($profile) + '&to=' + [uri]::EscapeDataString($to) + $(if ($prefs) { '&prefs=' + [uri]::EscapeDataString($prefs) } else { '' })
  $a = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", "--window-size=$w,$h", '--hide-scrollbars', '--virtual-time-budget=12000', "--screenshot=`"$(Join-Path $out "$file.png")`"")
  if ($Offline) { $a += '"--host-resolver-rules=MAP * ~NOTFOUND"' }
  $a += "`"$url`""
  $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(60000)) { "Délai : $file" }
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
$mine = '{"role":"mid","pool":["TF","Fizz","Kog''Maw","Xerath"]}'
$full = 'Malzahar,Ornn,Rengar,Samira,Pyke'
$states = @(
  @('vide', '', $mine, $null), @('un', 'Syndra', $mine, $null), @('trois', 'Syndra,Ornn,Lee Sin', $mine, $null), @('complete', $full, $mine, $null),
  @('inconnu', 'Sett,Vi', $mine, $null), @('serre', 'Yasuo,Yone,Lee Sin,Jinx,Thresh', $mine, $null), @('exclu', 'Syndra,Ornn,Lee Sin', $mine, '{"excluded":["Fizz"],"favorite":null,"assumeFront":true}'),
  @('support', 'Ornn,Lee Sin,Ahri,Jinx,Thresh', '{"role":"support","pool":["Lux","Seraphine","Renata Glasc","Tahm Kench","Soraka"]}', $null),
  @('sombre', 'Ornn,Lee Sin,Ahri,Jinx', '{"role":"adc","pool":["Kog''Maw","Vayne"]}', $null),
  @('sans-profil', 'Ornn,Lee Sin', 'null', $null),
  @('egalite-jungle', 'Ornn,Sion,Maokai,Jinx,Thresh', '{"role":"jungle","pool":["Cho''Gath","Viego","Master Yi","Lee Sin"]}', $null),
  @('top', 'Sett,Vi,Ahri,Jinx,Thresh', '{"role":"top","pool":["Darius","Garen","Malphite","Ornn","Camille"]}', $null),
  @('adc', 'Ornn,Lee Sin,Ahri,Caitlyn,Thresh', '{"role":"adc","pool":["Jinx","Caitlyn","Kog''Maw","Ezreal","Vayne"]}', $null)
)
foreach ($s in $states) {
  if ($Only -and $Only -notcontains $s[0]) { continue }
  $q = '&enemies=' + [uri]::EscapeDataString($s[1])
  Shot "$($s[0])-grand" 1280 720 "frame.html?w=1280&h=720$q" $s[2] $s[3]
  Shot "$($s[0])-panneaux" 1456 720 "sheet.html?x=1$q" $s[2] $s[3]
}
if (-not $Only) {
  Shot 'reduit-1024' 1024 640 ('frame.html?w=1024&h=640&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'choisi-panneaux' 1456 720 ('sheet.html?pick=1&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'reglages' 1280 720 'frame.html?w=1280&h=720&tab=settings&host=1' $mine $null
}
if (-not $Only -or $Only -contains 'hors-ligne') {
  Shot 'hors-ligne-grand' 1280 720 ('frame.html?w=1280&h=720&enemies=' + [uri]::EscapeDataString($full)) $mine $null -Offline
  Shot 'hors-ligne-panneaux' 1456 720 ('sheet.html?x=1&enemies=' + [uri]::EscapeDataString($full)) $mine $null -Offline
}
if (-not $Only -or $Only -contains 'v215') {
  # Historique fictif, pour voir le plus joué et les conseils sans aucune donnée personnelle.
  $rows = 0..139 | ForEach-Object { '{"id":"H' + $_ + '","acct":"Test#TEST","time":' + ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() - ($_ + 1) * 86400000) + ',"patch":"16.22","sec":1800,"champ":"' + $(if ($_ % 7) { 'Fizz' } else { 'Xerath' }) + '","pos":"MIDDLE","win":' + $(if ($_ % 2) { 'true' } else { 'false' }) + ',"k":5,"d":4,"a":6,"mid":"Ahri","enemies":["Ahri"]}' }
  [IO.File]::WriteAllText((Join-Path $work 'draft-history.js'), 'window.DRAFT_HISTORY = {"generated":"2026-11-05T00:00:00Z","accounts":["Test#TEST"],"skipped":[],"rows":[' + ($rows -join ',') + ']};', [Text.UTF8Encoding]::new($false))
  Shot 'v215-historique-panneaux' 1456 720 ('sheet.html?x=1&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-conseils-grand' 1280 720 ('frame.html?w=1280&h=720&tab=coach&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-conseils-long' 1280 1500 ('frame.html?w=1280&h=1500&tab=coach&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-analyse' 1280 720 ('frame.html?w=1280&h=720&tab=analyse&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-comparer' 1280 720 ('frame.html?w=1280&h=720&tab=compare&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-regles' 1280 720 ('frame.html?w=1280&h=720&tab=rules&enemies=' + [uri]::EscapeDataString($full)) $mine $null
  Shot 'v215-reglages' 1280 720 'frame.html?w=1280&h=720&tab=settings&host=1' $mine $null
  Remove-Item (Join-Path $work 'draft-history.js')
}
if (-not $Only -or $Only -contains 'stats') {
  # Statistiques fictives, pour voir ce rendu sans aucune donnée personnelle.
  $rows = 0..59 | ForEach-Object { '{"id":"T' + $_ + '","acct":"Test#TEST","time":' + (1760000000000 + $_ * 86400000) + ',"patch":"16.22","sec":1800,"champ":"' + @('Twisted Fate', 'Fizz', "Kog'Maw", 'Xerath')[$_ % 4] + '","pos":"MIDDLE","win":' + $(if ($_ % 3) { 'true' } else { 'false' }) + ',"mid":"' + @('Ahri', 'Syndra', 'Malzahar')[$_ % 3] + '","enemies":["Ornn","Lee Sin","Ahri","Jinx","Thresh"]}' }
  [IO.File]::WriteAllText((Join-Path $work 'draft-stats.js'), 'window.DRAFT_STATS = {"generated":"2026-11-05T00:00:00Z","accounts":["Test#TEST"],"rows":[' + ($rows -join ',') + ']};', [Text.UTF8Encoding]::new($false))
  Shot 'stats-grand' 1280 720 ('frame.html?w=1280&h=720&tab=parties&enemies=' + [uri]::EscapeDataString($full)) $mine $null
}
Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
"Captures : $out ($(@(Get-ChildItem $out -Filter *.png).Count))"
