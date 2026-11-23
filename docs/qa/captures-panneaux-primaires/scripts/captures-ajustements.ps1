# À lancer avec Windows PowerShell 5.1. Ajustements V2 (panneau gauche fermé, compact réductible) : copie minimale de captures-panneaux.ps1.
# Mêmes principes : VRAIES fenêtres WebView2 de DRAFT sur un bureau caché (bureau-cache.ps1), dossier de données neuf, pools et drafts
# FICTIFS, état du programme injecté par CDP (fixture), zone utile et échelle imposées par CDP, seule notre instance arrêtée à la fin.
# Captures :
#   droit, panneau gauche fermé (1920 x 1080 à 125 %, zone 252 x 711) : accès « Composition adverse 3/5 » replié, puis déplié (avec le
#   lien « Rouvrir le panneau gauche », compté dans l'essentiel visible sans défilement) ;
#   compact, composition complète 5/5 (1366 x 768 à 125 %, zone 248 x 565 ; 1920 x 1080 à 125 %, zone 248 x 711) : replié, puis déplié.
# « Panneau gauche fermé » : visibilité injectée dans la page (hostState.panelVisible.left = false), comme la lecture du client. Le vrai
# envoi par le programme (Alt+F4 sur le gauche, puis placement suivant) est vérifié par l'e2e (src/DraftMid.cs, EndToEnd.Run).
param([Parameter(Mandatory = $true)][string]$Exe, [Parameter(Mandatory = $true)][string]$Out,
  [ValidateSet('fr', 'en')][string]$Lang = 'fr', [int]$Port = 9441, [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
. 'C:\Users\lucas\Downloads\DRAFT-outil-captures\tools\shots\ShotsLib.ps1'
$launcher = 'C:\Users\lucas\AppData\Local\Temp\claude\55cd5a90b067dcd7\79512fa2-80a0-4195-80b7-6fd87f3209db\scratchpad\bureau-cache.ps1'
if (Get-Process -Name 'LeagueClientUx', 'LeagueClient' -ErrorAction SilentlyContinue) { throw 'Client LoL ouvert : captures refusées.' }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$root = Join-Path $env:TEMP ("draft-panneaux-ajust-$Lang-" + $PID)
if (Test-Path $root) { throw "Dossier déjà présent : $root" }
$data = Join-Path $root 'donnees'; New-Item -ItemType Directory -Force $data | Out-Null
$runKey = "Software\DRAFT-Test-PanA-Cap-$Lang\Run"
$desk = "DraftPanACap$Lang"
function Get-OurApp { Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.ExecutablePath -eq $Exe -and $_.CommandLine -and $_.CommandLine.Contains($data) } }
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-debugging-address=127.0.0.1 --host-resolver-rules=`"MAP * ~NOTFOUND, EXCLUDE ddragon.leagueoflegends.com`" --disable-features=CalculateNativeWinOcclusion"
$a = "--data `"$data`" --lang $Lang --run-key $runKey --no-hotkey"
$job = [powershell]::Create(); [void]$job.AddScript({ param($f, $e, $x, $d) & $f -Exe $e -Arguments $x -TimeoutSec 900 -Desktop $d }).AddArgument($launcher).AddArgument($Exe).AddArgument($a).AddArgument($desk); $handle = $job.BeginInvoke()
for ($i = 0; $i -lt 60 -and -not (Get-OurApp); $i++) { Start-Sleep -Milliseconds 250 }
Start-Sleep 3
Remove-Item Env:\WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS

function Invoke-CdpSession($Target, [object[]]$Calls) {
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
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
function Ev([string]$x) { @('Runtime.evaluate', @{ expression = $x; returnByValue = $true; awaitPromise = $true }) }
function EvalT($Target, [string]$x) { $r = Invoke-CdpSession $Target @(, (Ev $x)); $o = $r[0]; if ($o['result'] -and $o['result']['exceptionDetails']) { throw ('Exception dans la page : ' + $script:Json.Serialize($o['result']['exceptionDetails'])) }; if ($o['result'] -and $o['result']['result']) { return $o['result']['result']['value'] } }
function WaitT($Target, [string]$cond, [int]$tries = 100) { for ($i = 0; $i -lt $tries; $i++) { try { if ((EvalT $Target $cond) -eq $true) { return $true } } catch {}; Start-Sleep -Milliseconds 200 }; return $false }

# ---------- Scénarios (fictifs) : nom ; rôle ; pool ; adversaires ; lecture (on, off) ; visibilité des panneaux (injectée) ----------
$MID = "['TF','Vex','Fizz','Xerath']"
$S = [ordered]@{
  'a1-gauche-ferme'    = @('mid', $MID, "['Zed','Malphite','Vi']", 'off', '{left:false,right:true,compact:false}')
  'a2-compact-complet' = @('mid', $MID, "['Zed','Malphite','Vi','Caitlyn','Leona']", 'on', '{left:false,right:false,compact:true}')
}
$Z = @{
  'droit-1920-125'   = @('1920 × 1080', 1.25, '252x711')
  'compact-1366-125' = @('1366 × 768', 1.25, '248x565')
  'compact-1920-125' = @('1920 × 1080', 1.25, '248x711')
}
function HostFixture($s) {
  $on = $s[3] -eq 'on'
  "(()=>{Object.assign(hostState,{available:true,lcuConfirmed:true,lcuEnabled:$(if($on){'true'}else{'false'}),lcuStatus:'$(if($on){'connected'}else{'off'})',assignedRole:'mid',panelVisible:$($s[4])});state.assignedRole='mid';state.sessionActive=true;rerender();return 1})()"
}
function Driver($s) {
  "(async()=>{const w=m=>new Promise(r=>setTimeout(r,m));state.viewed=null;resetDraft();saveMyProfile('$($s[0])',$($s[1]));switchRole('$($s[0])','manual');resetRoleSession();state.sessionActive=true;publish(null);await w(300);handleAssignedRole('mid');await w(300);$($s[2]).forEach(n=>draftApi.add(n));await w(500);publish(null);await w(400);return JSON.stringify({role:myRole(),shown:V.shown,enemies:state.enemies.length})})()"
}
# Mesure dans la vraie page : essentiel visible sans défilement (dont la ligne « Composition »), état de la composition, mots coupés,
# débordement horizontal.
$Measure = @'
(()=>{const d=document,H=innerHeight,W=innerWidth,bar=d.querySelector('.p-actions'),barTop=bar?bar.getBoundingClientRect().top:H;
const ess=[['roles',d.querySelector('[data-test=role-switch-support]')],['etatRole',d.querySelector('[data-test=panel-role-meta]')],['compo',d.querySelector('[data-test=compo-toggle]')],['conseil',d.querySelector('.p-reco')||d.querySelector('.tie-group')],['pourquoi',d.querySelector('.panel-right .p-why')],['menaceOuBan',d.querySelector('.p-threat')||d.querySelector('.p-keyban')||d.querySelector('.p-risk')],['pick',d.querySelector('.p-actions [data-test=choose-pick]')],['details',d.querySelector('[data-test=open-analysis]')],['adversaires',d.querySelector('.compact-roles')],['rouvrir',d.querySelector('[data-test=compo-reopen]')]].filter(x=>x[1]);
const hid=ess.filter(([k,e])=>{const r=e.getBoundingClientRect(),lim=e.closest('.p-actions')?H:barTop;return !(r.width>0&&r.top>=-0.5&&r.bottom<=lim+0.5&&r.left>=-0.5&&r.right<=W+0.5)}).map(x=>x[0]);
const lines=r=>{const t=[...r.getClientRects()].filter(x=>x.width>0.5).map(x=>x.top);return t.length?Math.max(...t)-Math.min(...t):0};
const cut=[];const tw=d.createTreeWalker(d.body,NodeFilter.SHOW_TEXT);let tn;
while(tn=tw.nextNode()){const el=tn.parentElement;if(!el||['SCRIPT','STYLE','OPTION'].includes(el.tagName)||(el.closest('details:not([open])')&&!el.closest('summary')))continue;const cs=getComputedStyle(el);if(cs.display==='none'||cs.visibility==='hidden')continue;
 const re=/[^\s\-\u2013\u2014\/]{2,}/g;let m;while(m=re.exec(tn.nodeValue)){const r=d.createRange();r.setStart(tn,m.index);r.setEnd(tn,m.index+m[0].length);if(lines(r)>4){cut.push(m[0].slice(0,30));break}}}
const over=Math.max(d.documentElement.scrollWidth-W,...[...d.querySelectorAll('.p-scroll,.p-body')].map(e=>e.scrollWidth-e.clientWidth),0);
const tg=d.querySelector('[data-test=compo-toggle]');
return JSON.stringify({vue:W+'x'+H,dpr:devicePixelRatio,essentiel:ess.length,caches:hid,coupes:cut,debord:over,acces:!!d.querySelector('[data-test=compo-access]'),rangee:!!d.querySelector('.compact-roles'),ligne:tg?tg.textContent.trim():'',choix:compoChoice,gauche:hostState.panelVisible?hostState.panelVisible.left:null})})()
'@

$rows = New-Object Collections.Generic.List[object]
function Add-Band([string]$file, [string[]]$lines) {
  $img = [Drawing.Image]::FromFile($file)
  try {
    $lh = 16; $all = @($lines); $h = $lh * $all.Count + 4
    $bmp = New-Object Drawing.Bitmap $img.Width, ($img.Height + $h)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear([Drawing.Color]::FromArgb(54, 47, 64))
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $font = New-Object Drawing.Font('Segoe UI', [single]12, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $light = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(242, 236, 228))
    $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'; $fmt.Trimming = 'EllipsisCharacter'; $fmt.FormatFlags = 'NoWrap'
    for ($k = 0; $k -lt $all.Count; $k++) { $g.DrawString($all[$k], $font, $light, (New-Object Drawing.RectangleF(2, (2 + $k * $lh), ($img.Width - 4), $lh)), $fmt) }
    $g.DrawImage($img, 0, $h, $img.Width, $img.Height)
    $g.Dispose(); $font.Dispose(); $light.Dispose()
  } finally { $img.Dispose() }
  $bmp.Save($file, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
$Reset = "(()=>{document.querySelectorAll('.p-sec[open]').forEach(e=>e.open=false);document.querySelectorAll('.p-scroll,.p-body').forEach(e=>e.scrollTop=0);if(document.activeElement)document.activeElement.blur();return 1})()"
# $open : choix du joueur ($null = comportement par défaut, 'open' = déplié à la main).
function Shot($target, [string]$scName, [string]$key, $open, [string]$state) {
  $z = $Z[$key]; $panel, $label = $key.Split('-', 2)
  $wh = $z[2].Split('x') | ForEach-Object { [int]$_ }; $dpr = $z[1]
  $s = $S[$scName]
  $choice = if ($open) { "'$open'" } else { 'null' }
  $calls = @(@('Emulation.setDeviceMetricsOverride', @{ width = $wh[0]; height = $wh[1]; deviceScaleFactor = $dpr; mobile = $false }), 900,
    (Ev $Reset), (Ev (HostFixture $s)), (Ev "(()=>{compoChoice=$choice;rerender();return 1})()"), 600)
  $calls += , (Ev $Measure)
  $calls += , @('Page.captureScreenshot', @{ format = 'png' })
  $calls += , @('Emulation.clearDeviceMetricsOverride', @{})
  $res = Invoke-CdpSession $target $calls
  $shot = $res | Where-Object { $_['result'] -and $_['result']['data'] } | Select-Object -Last 1
  $meas = ($res | Where-Object { $_['result'] -and $_['result']['result'] -and ($_['result']['result']['value'] -is [string]) -and $_['result']['result']['value'].StartsWith('{"vue"') } | Select-Object -Last 1)['result']['result']['value']
  $file = Join-Path $Out "ajust-$Lang-$scName-$panel-$label-$state.png"
  [IO.File]::WriteAllBytes($file, [Convert]::FromBase64String($shot['result']['data']))
  $band = @('Données fictives / client simulé', "$($z[0]) à $([int]($dpr * 100)) % · zone $($wh[0]) × $($wh[1]) (V2)")
  if ($panel -eq 'droit') { $band += 'Panneau gauche fermé : état du programme injecté' }
  Add-Band $file $band
  $m = $script:Json.DeserializeObject($meas)
  $rows.Add([pscustomobject]@{ fichier = Split-Path $file -Leaf; langue = $Lang; scenario = $scName; panneau = $panel; etat = $state; resolution = $z[0]; zone_utile_css = $z[2]; echelle = $dpr; vue_page = $m['vue']; essentiel = $m['essentiel']; caches = ($m['caches'] -join ','); mots_coupes = ($m['coupes'] -join ' | '); debord_horizontal = $m['debord']; acces_composition = $m['acces']; rangee_visible = $m['rangee']; ligne = $m['ligne']; choix_joueur = $m['choix']; gauche_visible = $m['gauche'] })
  "  $(Split-Path $file -Leaf) : $($m['vue']) dpr $($m['dpr']), essentiel $($m['essentiel'] - @($m['caches']).Count)/$($m['essentiel'])$(if (@($m['caches']).Count) { ' caché : ' + ($m['caches'] -join ',') })$(if (@($m['coupes']).Count) { ' ; COUPÉ : ' + ($m['coupes'] -join ' | ') })$(if ($m['debord'] -gt 0) { " ; débord $($m['debord']) px" }), accès $($m['acces']), rangée $($m['rangee']), ligne « $($m['ligne']) »"
}
function Want($id) { -not $Only.Count -or ($Only | Where-Object { $id -like $_ }) }

try {
  if (-not (Get-OurApp)) { throw 'instance d''essai absente (lancement sur le bureau caché)' }
  $full = Get-CdpTarget -Port $Port -Panel 'full' -TimeoutSec 120
  if (-not $full) { throw 'fenêtre principale absente' }
  [void](WaitT $full "typeof PROFILES!=='undefined'&&typeof hostState!=='undefined'&&hostState.available" 150)
  "Ajustements V2, langue $(EvalT $full 'I18N.lang'), APP_VERSION $(EvalT $full 'APP_VERSION'), exe $Exe, port $Port, bureau $desk"
  [void](EvalT $full "saveMyProfile('mid',$MID);switchRole('mid','manual');(typeof storeOnboarding==='function')&&storeOnboarding({started:true,skipped:['pool','lcu','panels']});goView('home');1")
  Start-Sleep 1
  # « hideAll » de la page bascule l'affichage : il affiche les panneaux (aucun client : place manuelle).
  $shown = $false
  $sc = 'a1-gauche-ferme'
  if ((Want "$sc-droit-replie") -or (Want "$sc-droit-deplie")) {
    [void](EvalT $full "hostSend({cmd:'hideAll'});1"); $shown = $true
    $right = Get-CdpTarget -Port $Port -Panel 'right' -TimeoutSec 60
    if (-not $right) { throw 'panneau droit absent' }
    [void](WaitT $right "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced")
    Start-Sleep 2
    "Scénario $sc : $(EvalT $right (Driver $S[$sc]))"
    Start-Sleep -Milliseconds 800
    if (Want "$sc-droit-replie") { Shot $right $sc 'droit-1920-125' $null 'replie' }
    if (Want "$sc-droit-deplie") { Shot $right $sc 'droit-1920-125' 'open' 'deplie' }
    [void](EvalT $right "compoChoice=null;rerender();1")
  }
  $sc = 'a2-compact-complet'
  if (-not $Only.Count -or ($Only | Where-Object { $_ -like "$sc-compact-*" })) {
    [void](EvalT $full "hostSend({cmd:'toggleCompact'});$(if (-not $shown) { "hostSend({cmd:'hideAll'});" })1")
    $compact = Get-CdpTarget -Port $Port -Panel 'compact' -TimeoutSec 60
    if (-not $compact) { throw 'panneau compact absent' }
    [void](WaitT $compact "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced")
    Start-Sleep 2
    "Scénario $sc (compact) : $(EvalT $compact (Driver $S[$sc]))"
    Start-Sleep -Milliseconds 800
    foreach ($k in 'compact-1366-125', 'compact-1920-125') {
      $res = $k.Split('-')[1]
      if (Want "$sc-compact-$res-replie") { Shot $compact $sc $k $null 'replie' }
      if (Want "$sc-compact-$res-deplie") { Shot $compact $sc $k 'open' 'deplie' }
    }
  }
} finally {
  Get-OurApp | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; "instance d'essai $($_.ProcessId) arrêtée (exe et dossier de données vérifiés)" }
  $job.EndInvoke($handle) | Select-Object -Last 1; $job.Dispose()
  Remove-Item "HKCU:\Software\DRAFT-Test-PanA-Cap-$Lang" -Recurse -ErrorAction SilentlyContinue
  Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
  $csv = Join-Path $Out "manifeste-ajustements-$Lang.csv"
  $rows | Export-Csv $csv -NoTypeInformation -Encoding UTF8 -Delimiter ';'
  "$($rows.Count) captures ; manifeste : $csv"
}
