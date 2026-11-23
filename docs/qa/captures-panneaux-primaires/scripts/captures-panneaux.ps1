# À lancer avec Windows PowerShell 5.1. Panneaux primaires, V2 DÉFINITIVE : captures des VRAIES fenêtres WebView2 de DRAFT (panneaux gauche,
# droit, compact), lancées sur un bureau caché (bureau-cache.ps1, -Desktop DraftPanneaux<langue>). Rien à l'écran de l'utilisateur.
# Version « après » seulement : les captures « avant » (main 4fb621f, V1) sont dans ../avant/ et sont réutilisées telles quelles.
# Version précédente de ce script (tailles V1 222x511, 252x567, variantes V2 et V3 de test, échelles 100 et 150 %) : commit 95d4640.
# Zones utiles V2 réelles (tools/layout-table.ps1 -Variantes sur l'exe de la branche ; panneau moins 4 ou 8 en largeur, moins 9 en hauteur) :
#   1366 x 768 à 125 % : panneau étroit 256 x 574, zone 248 x 565 (un seul panneau, même si deux sont demandés) ;
#   1920 x 1080 à 125 % : deux panneaux 256 x 720, gauche et droit 252 x 711 ; panneau unique demandé 256 x 720, zone 248 x 711.
# Données : dossier neuf, pools et drafts FICTIFS. État du programme (lecture du client, poste attribué) : FIXTURE injectée dans chaque
# page par CDP (aucun vrai client). « Dans League » : donnée de test (leagueSlot), la lecture réelle vient de la PR #40.
# Taille : zone utile du panneau imposée par CDP (Emulation.setDeviceMetricsOverride), échelle comprise ; la fenêtre reste celle du programme.
# FR et EN peuvent tourner en parallèle : port CDP, dossier de données, bureau caché et clé Run propres à chaque langue ; seule l'instance
# lancée par CE script (repérée par son dossier de données) est arrêtée à la fin.
param([Parameter(Mandatory = $true)][string]$Exe, [Parameter(Mandatory = $true)][string]$Out,
  [ValidateSet('fr', 'en')][string]$Lang = 'fr', [int]$Port = 9431, [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
$Version = 'apres'
. 'C:\Users\lucas\Downloads\DRAFT-outil-captures\tools\shots\ShotsLib.ps1'
$launcher = 'C:\Users\lucas\AppData\Local\Temp\claude\55cd5a90b067dcd7\79512fa2-80a0-4195-80b7-6fd87f3209db\scratchpad\bureau-cache.ps1'
if (Get-Process -Name 'LeagueClientUx', 'LeagueClient' -ErrorAction SilentlyContinue) { throw 'Client LoL ouvert : captures refusées.' }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$root = Join-Path $env:TEMP ("draft-panneaux-v2-$Lang-" + $PID)
if (Test-Path $root) { throw "Dossier déjà présent : $root" }
$data = Join-Path $root 'donnees'; New-Item -ItemType Directory -Force $data | Out-Null
$runKey = "Software\DRAFT-Test-Panneaux-V2-$Lang\Run"
$desk = "DraftPanneaux$Lang"
# Notre instance seulement : même exe ET notre dossier de données dans la ligne de commande.
function Get-OurApp { Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.ExecutablePath -eq $Exe -and $_.CommandLine -and $_.CommandLine.Contains($data) } }
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-debugging-address=127.0.0.1 --host-resolver-rules=`"MAP * ~NOTFOUND, EXCLUDE ddragon.leagueoflegends.com`" --disable-features=CalculateNativeWinOcclusion"
$a = "--data `"$data`" --lang $Lang --run-key $runKey --no-hotkey"
$job = [powershell]::Create(); [void]$job.AddScript({ param($f, $e, $x, $d) & $f -Exe $e -Arguments $x -TimeoutSec 1200 -Desktop $d }).AddArgument($launcher).AddArgument($Exe).AddArgument($a).AddArgument($desk); $handle = $job.BeginInvoke()
for ($i = 0; $i -lt 60 -and -not (Get-OurApp); $i++) { Start-Sleep -Milliseconds 250 }
Start-Sleep 3
Remove-Item Env:\WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS

# Une seule session CDP pour la taille simulée, la fixture, la mesure et la capture.
function Invoke-CdpSession($Target, [object[]]$Calls) {
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
  # Délai de 60 s par session : une page figée ne bloque plus tout le relevé.
  $cts = New-Object Threading.CancellationTokenSource 60000
  $ct = $cts.Token
  $ws.ConnectAsync([Uri]$Target['webSocketDebuggerUrl'], $ct).Wait()
  $results = @()
  try {
    foreach ($c in $Calls) {
      if ($c -is [int]) { Start-Sleep -Milliseconds $c; continue }
      $script:CdpId++; $id = $script:CdpId
      $bytes = [Text.Encoding]::UTF8.GetBytes($script:Json.Serialize(@{ id = $id; method = $c[0]; params = $c[1] }))
      $ws.SendAsync((New-Object 'ArraySegment[byte]' (, $bytes)), 'Text', $true, $ct).Wait()
      $buf = New-Object byte[] 1048576
      while ($true) {
        $ms = New-Object IO.MemoryStream
        do { $r = $ws.ReceiveAsync((New-Object 'ArraySegment[byte]' (, $buf)), $ct).Result; $ms.Write($buf, 0, $r.Count) } while (-not $r.EndOfMessage)
        $o = $script:Json.DeserializeObject([Text.Encoding]::UTF8.GetString($ms.ToArray()))
        if ($o['id'] -eq $id) { $results += , $o; break }
      }
    }
  } finally { try { $ws.Abort() } catch {} ; $ws.Dispose(); $cts.Dispose() }
  return , $results
}
# Évaluation avec délai (même session que Invoke-CdpSession), à la place de Invoke-CdpEval de ShotsLib, sans délai.
function EvalT($Target, [string]$x) { $r = Invoke-CdpSession $Target @(, (Ev $x)); $o = $r[0]; if ($o['result'] -and $o['result']['exceptionDetails']) { throw ('Exception dans la page : ' + $script:Json.Serialize($o['result']['exceptionDetails'])) }; if ($o['result'] -and $o['result']['result']) { return $o['result']['result']['value'] } }
function Ev([string]$x) { @('Runtime.evaluate', @{ expression = $x; returnByValue = $true; awaitPromise = $true }) }
# Attente d'une condition dans la page, avec délai.
function WaitT($Target, [string]$cond, [int]$tries = 100) { for ($i = 0; $i -lt $tries; $i++) { try { if ((EvalT $Target $cond) -eq $true) { return $true } } catch {}; Start-Sleep -Milliseconds 200 }; return $false }

# ---------- Scénarios (fictifs) ----------
$MID = "['TF','Vex','Fizz','Xerath']"; $JUNGLE = "[`"Cho'Gath`",'Viego','Master Yi','Lee Sin']"
$COMP = "['Zed','Malphite','Vi','Caitlyn','Leona']"
# nom ; rôle ; pool ; adversaires ; consulter ; lecture (on, off) ; poste attribué ; Dans League (json ou '')
$S = [ordered]@{
  '01-debut'       = @('mid', $MID, '[]', $false, 'on', 'mid', '')
  '03-complete'    = @('mid', $MID, $COMP, $false, 'on', 'mid', '')
  '04-consulte'    = @('mid', $MID, $COMP, $true, 'on', 'mid', '')
  '05-egalite'     = @('jungle', $JUNGLE, "['Ornn','Sion','Maokai','Jinx','Thresh']", $false, 'on', 'jungle', '')
  '07-role-main'   = @('mid', $MID, "['Zed','Malphite','Vi']", $false, 'on', 'jungle', '')
  '08-sans-lecture'= @('mid', $MID, $COMP, $false, 'off', '', '')
  '09-compact-league' = @('mid', $MID, $COMP, $true, 'on', 'mid', '{champion:"Fizz",state:"hover"}')
  '11-league-verrouille' = @('mid', $MID, $COMP, $false, 'on', 'mid', '{champion:"TF",state:"locked"}')
}
# Zones utiles V2 (pixels CSS) et échelle : résolution, échelle, largeur x hauteur.
$Z = @{
  'droit-1920-125'   = @('1920 × 1080', 1.25, '252x711')
  'gauche-1920-125'  = @('1920 × 1080', 1.25, '252x711')
  'compact-1366-125' = @('1366 × 768', 1.25, '248x565')
  'compact-1920-125' = @('1920 × 1080', 1.25, '248x711')
}
function HostFixture($s) {
  $on = $s[4] -eq 'on'; $assigned = if ($s[5]) { "'$($s[5])'" } else { 'null' }
  "(()=>{Object.assign(hostState,{available:true,lcuConfirmed:true,lcuEnabled:$(if($on){'true'}else{'false'}),lcuStatus:'$(if($on){'connected'}else{'off'})',assignedRole:$assigned});state.assignedRole=$assigned;state.sessionActive=true;typeof FULL!=='undefined'&&(FULL.Nunu='Nunu & Willump');try{leagueSlot=$(if($s[6]){$s[6]}else{'null'})}catch(e){};rerender();return 1})()"
}
function Driver($name, $s) {
  $consult = if ($s[3]) { "consult(V.rival?V.rival.pick:V.order.find(p=>p.pick!==V.shown).pick);" } else { '' }
  $manual = if ($name -eq '07-role-main') { "saveMyProfile('jungle',['Viego','Lee Sin']);" } else { '' }
  $after = if ($name -eq '07-role-main') { "handleAssignedRole('jungle');await w(300);switchRole('mid','manual');" } elseif ($s[5]) { "handleAssignedRole('$($s[5])');" } else { '' }
  "(async()=>{const w=m=>new Promise(r=>setTimeout(r,m));state.viewed=null;resetDraft();$manual saveMyProfile('$($s[0])',$($s[1]));switchRole('$($s[0])','manual');resetRoleSession();state.sessionActive=true;publish(null);await w(300);$after await w(300);$($s[2]).forEach(n=>draftApi.add(n));await w(500);$consult publish(null);await w(400);return JSON.stringify({role:myRole(),shown:V.shown,viewed:state.viewed,enemies:state.enemies.length})})()"
}
# Mesure dans la vraie page : essentiel visible sans défilement (mêmes éléments que draft-panneaux-tests.html) ; mots coupés et bouton
# principal sur deux lignes (mêmes règles que MOT-COUPE et BOUTON-DEUX-LIGNES de draft-layout-tests.html) ; débordement horizontal.
$Measure = @'
(()=>{const d=document,H=innerHeight,W=innerWidth,bar=d.querySelector('.p-actions'),barTop=bar?bar.getBoundingClientRect().top:H;
const ess=[['roles',d.querySelector('[data-test=role-switch-support]')],['etatRole',d.querySelector('[data-test=panel-role-meta]')||d.querySelector('[data-test=role-state]')],['conseil',d.querySelector('.p-reco')||d.querySelector('.tie-group')],['pourquoi',d.querySelector('.panel-right .p-why')],['menaceOuBan',d.querySelector('.p-threat')||d.querySelector('.p-keyban')],['consulte',d.querySelector('[data-test=viewed] .viewed-head')],['pick',d.querySelector('.p-actions [data-test=choose-pick]')||d.querySelector('[data-test=league-line]')],['details',d.querySelector('[data-test=open-analysis]')],['retourConseil',d.querySelector('[data-test=back-to-advice]')],['adversaires',d.querySelector('.compact-roles')||d.querySelector('.panel-left .roles')],['profil',d.querySelector('.panel-left .effects')]].filter(x=>x[1]);
const hid=ess.filter(([k,e])=>{const r=e.getBoundingClientRect(),lim=e.closest('.p-actions')?H:barTop;return !(r.width>0&&r.top>=-0.5&&r.bottom<=lim+0.5&&r.left>=-0.5&&r.right<=W+0.5)}).map(x=>x[0]);
const lines=r=>{const t=[...r.getClientRects()].filter(x=>x.width>0.5).map(x=>x.top);return t.length?Math.max(...t)-Math.min(...t):0};
const skip=e=>(e.closest('details:not([open])')&&!e.closest('summary'))||e.closest('.sr,.rank-glow,.rank-ring,.role-flash,.sugg-hl,.tab-line,.portrait .init,.meter-delta')||[e,e.closest('.to-mid')].some(x=>x&&getComputedStyle(x).opacity==='0');
const cut=[];const tw=d.createTreeWalker(d.body,NodeFilter.SHOW_TEXT);let tn;
while(tn=tw.nextNode()){const el=tn.parentElement;if(!el||skip(el)||['SCRIPT','STYLE','OPTION'].includes(el.tagName))continue;const cs=getComputedStyle(el);if(cs.display==='none'||cs.visibility==='hidden')continue;
 const re=/[^\s\-\u2013\u2014\/]{2,}/g;let m;while(m=re.exec(tn.nodeValue)){const r=d.createRange();r.setStart(tn,m.index);r.setEnd(tn,m.index+m[0].length);if(lines(r)>4){cut.push(m[0].slice(0,30));break}}}
const cta=d.querySelector('.p-actions [data-test=choose-pick]');let two=false;if(cta){const r=d.createRange();r.selectNodeContents(cta);two=lines(r)>4}
const over=Math.max(d.documentElement.scrollWidth-W,...[...d.querySelectorAll('.p-scroll,.p-body,.col-left')].map(e=>e.scrollWidth-e.clientWidth),0);
return JSON.stringify({vue:W+'x'+H,dpr:devicePixelRatio,essentiel:ess.length,caches:hid,coupes:cut,bouton2:two,debord:over,hostLcu:hostState.lcuStatus,role:myRole()})})()
'@

$rows = New-Object Collections.Generic.List[object]
# Bandeau au-dessus de la capture (hors du panneau : la mise en page n'est pas touchée) : « Données fictives / client simulé », résolution,
# échelle et zone utile ; une ligne bleue de plus pour la donnée de test « Dans League ».
function Add-Band([string]$file, [string[]]$lines, [bool]$league) {
  $img = [Drawing.Image]::FromFile($file)
  try {
    $lh = 16; $all = @($lines); if ($league) { $all += 'TEST : « Dans League » simulé (PR #40)' }
    $h = $lh * $all.Count + 4
    $bmp = New-Object Drawing.Bitmap $img.Width, ($img.Height + $h)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear([Drawing.Color]::FromArgb(54, 47, 64))
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $font = New-Object Drawing.Font('Segoe UI', [single]12, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $light = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(242, 236, 228)); $dark = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(11, 26, 32))
    $blue = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(121, 198, 218))
    $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'; $fmt.Trimming = 'EllipsisCharacter'; $fmt.FormatFlags = 'NoWrap'
    for ($k = 0; $k -lt $all.Count; $k++) {
      $y = 2 + $k * $lh; $isTest = $league -and $k -eq $all.Count - 1
      if ($isTest) { $g.FillRectangle($blue, 0, $y, $img.Width, $lh) }
      $g.DrawString($all[$k], $font, $(if ($isTest) { $dark } else { $light }), (New-Object Drawing.RectangleF(2, $y, ($img.Width - 4), $lh)), $fmt)
    }
    $g.DrawImage($img, 0, $h, $img.Width, $img.Height)
    $g.Dispose(); $font.Dispose(); $light.Dispose(); $dark.Dispose(); $blue.Dispose()
  } finally { $img.Dispose() }
  $bmp.Save($file, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
$Reset = "(()=>{document.querySelectorAll('.p-sec[open]').forEach(e=>e.open=false);document.querySelectorAll('.p-scroll,.p-body,.col-left').forEach(e=>e.scrollTop=0);if(document.activeElement)document.activeElement.blur();return 1})()"
function Shot($target, [string]$scName, [string]$key) {
  $z = $Z[$key]; $panel, $label = $key.Split('-', 2)
  $wh = $z[2].Split('x') | ForEach-Object { [int]$_ }; $dpr = $z[1]
  $s = $S[$scName]
  $calls = @(@('Emulation.setDeviceMetricsOverride', @{ width = $wh[0]; height = $wh[1]; deviceScaleFactor = $dpr; mobile = $false }), 900,
    (Ev $Reset), (Ev (HostFixture $s)), 500)
  $calls += , (Ev $Measure)
  $calls += , @('Page.captureScreenshot', @{ format = 'png' })
  $calls += , @('Emulation.clearDeviceMetricsOverride', @{})
  $res = Invoke-CdpSession $target $calls
  $shot = $res | Where-Object { $_['result'] -and $_['result']['data'] } | Select-Object -Last 1
  $meas = ($res | Where-Object { $_['result'] -and $_['result']['result'] -and ($_['result']['result']['value'] -is [string]) -and $_['result']['result']['value'].StartsWith('{"vue"') } | Select-Object -Last 1)['result']['result']['value']
  $file = Join-Path $Out "$Version-$Lang-$scName-$panel-$label.png"
  [IO.File]::WriteAllBytes($file, [Convert]::FromBase64String($shot['result']['data']))
  Add-Band $file @('Données fictives / client simulé', "$($z[0]) à $([int]($dpr * 100)) % · zone $($wh[0]) × $($wh[1]) (V2)") ([bool]$s[6])
  $m = $script:Json.DeserializeObject($meas)
  $rows.Add([pscustomobject]@{ fichier = Split-Path $file -Leaf; version = $Version; langue = $Lang; scenario = $scName; panneau = $panel; resolution = $z[0]; zone_utile_css = $z[2]; echelle = $dpr; vue_page = $m['vue']; essentiel = $m['essentiel']; caches = ($m['caches'] -join ','); mots_coupes = ($m['coupes'] -join ' | '); bouton_deux_lignes = $m['bouton2']; debord_horizontal = $m['debord']; lecture_client = $m['hostLcu']; role = $m['role']; dans_league_test = [bool]$s[6] })
  "  $(Split-Path $file -Leaf) : $($m['vue']) dpr $($m['dpr']), essentiel $($m['essentiel'] - @($m['caches']).Count)/$($m['essentiel'])$(if (@($m['caches']).Count) { ' caché : ' + ($m['caches'] -join ',') })$(if (@($m['coupes']).Count) { ' ; COUPÉ : ' + ($m['coupes'] -join ' | ') })$(if ($m['bouton2']) { ' ; BOUTON SUR DEUX LIGNES' })$(if ($m['debord'] -gt 0) { " ; débord $($m['debord']) px" }), lecture $($m['hostLcu'])"
}
function Want($sc) { -not $Only.Count -or ($Only | Where-Object { $sc -like $_ }) }

try {
  if (-not (Get-OurApp)) { throw 'instance d''essai absente (lancement sur le bureau caché)' }
  $full = Get-CdpTarget -Port $Port -Panel 'full' -TimeoutSec 120
  if (-not $full) { throw 'fenêtre principale absente' }
  [void](WaitT $full "typeof PROFILES!=='undefined'&&typeof hostState!=='undefined'&&hostState.available" 150)
  "Version $Version (V2), langue $(EvalT $full 'I18N.lang'), APP_VERSION $(EvalT $full 'APP_VERSION'), exe $Exe, port $Port, bureau $desk"
  [void](EvalT $full "saveMyProfile('mid',$MID);switchRole('mid','manual');(typeof storeOnboarding==='function')&&storeOnboarding({started:true,skipped:['pool','lcu','panels']});goView('home');1")
  Start-Sleep 1
  # Deux panneaux (gauche, droit) : la commande « masquer / afficher » de la page les affiche (aucun client : place manuelle).
  [void](EvalT $full "hostSend({cmd:'hideAll'});1")
  $right = Get-CdpTarget -Port $Port -Panel 'right' -TimeoutSec 60; $left = Get-CdpTarget -Port $Port -Panel 'left' -TimeoutSec 60
  if (-not $right -or -not $left) { throw 'panneaux gauche et droit absents' }
  [void](WaitT $right "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced")
  Start-Sleep 2
  foreach ($sc in $S.Keys) {
    if (-not (Want $sc) -or $sc -eq '09-compact-league') { continue }
    "Scénario $sc : $(EvalT $right (Driver $sc $S[$sc]))"
    Start-Sleep -Milliseconds 800
    Shot $right $sc 'droit-1920-125'
    if ($sc -in '01-debut', '03-complete', '05-egalite', '08-sans-lecture') { Shot $left $sc 'gauche-1920-125' }
  }
  # Panneau unique (compact) : étroit à 1366 x 768 à 125 % (cas de l'utilisateur) ; unique demandé à 1920 x 1080 à 125 %.
  [void](EvalT $full "hostSend({cmd:'toggleCompact'});1")
  $compact = Get-CdpTarget -Port $Port -Panel 'compact' -TimeoutSec 60
  if (-not $compact) { throw 'panneau compact absent' }
  [void](WaitT $compact "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced")
  Start-Sleep 2
  foreach ($sc in $S.Keys) {
    if (-not (Want $sc)) { continue }
    "Scénario $sc (compact) : $(EvalT $compact (Driver $sc $S[$sc]))"
    Start-Sleep -Milliseconds 800
    Shot $compact $sc 'compact-1366-125'
    Shot $compact $sc 'compact-1920-125'
  }
} finally {
  Get-OurApp | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; "instance d'essai $($_.ProcessId) arrêtée (exe et dossier de données vérifiés)" }
  $job.EndInvoke($handle) | Select-Object -Last 1; $job.Dispose()
  Remove-Item "HKCU:\Software\DRAFT-Test-Panneaux-V2-$Lang" -Recurse -ErrorAction SilentlyContinue
  Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
  $csv = Join-Path $Out "manifeste-$Version-$Lang.csv"
  $rows | Export-Csv $csv -NoTypeInformation -Encoding UTF8 -Delimiter ';'
  "$($rows.Count) captures ; manifeste : $csv"
}
