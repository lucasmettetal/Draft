# À lancer avec Windows PowerShell 5.1. Panneaux primaires : captures des VRAIES fenêtres WebView2 de DRAFT (panneaux gauche, droit,
# compact), lancées sur un bureau caché (bureau-cache.ps1, -Desktop DraftPanneaux). Rien à l'écran de l'utilisateur.
# Données : dossier neuf, pools et drafts FICTIFS. État du programme (lecture du client, poste attribué) : FIXTURE injectée dans chaque
# page par CDP (aucun vrai client). « Dans League » : donnée de test (leagueSlot), la lecture réelle vient de la PR #40.
# Taille : zone utile du panneau imposée par CDP (Emulation.setDeviceMetricsOverride), échelle comprise ; la fenêtre reste celle du programme.
param([Parameter(Mandatory = $true)][string]$Exe, [Parameter(Mandatory = $true)][string]$Out, [ValidateSet('avant', 'apres')][string]$Version = 'apres',
  [ValidateSet('fr', 'en')][string]$Lang = 'fr', [int]$Port = 9417, [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
. 'C:\Users\lucas\Downloads\DRAFT-outil-captures\tools\shots\ShotsLib.ps1'
$launcher = 'C:\Users\lucas\AppData\Local\Temp\claude\55cd5a90b067dcd7\79512fa2-80a0-4195-80b7-6fd87f3209db\scratchpad\bureau-cache.ps1'
if (Get-Process -Name 'LeagueClientUx', 'LeagueClient' -ErrorAction SilentlyContinue) { throw 'Client LoL ouvert : captures refusées.' }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$root = Join-Path $env:TEMP ("draft-panneaux-captures-$Version-$Lang-" + $PID)
if (Test-Path $root) { throw "Dossier déjà présent : $root" }
$data = Join-Path $root 'donnees'; New-Item -ItemType Directory -Force $data | Out-Null
$runKey = 'Software\DRAFT-Test-Panneaux\Run'
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-debugging-address=127.0.0.1 --host-resolver-rules=`"MAP * ~NOTFOUND, EXCLUDE ddragon.leagueoflegends.com`" --disable-features=CalculateNativeWinOcclusion"
$a = "--data `"$data`" --lang $Lang --run-key $runKey --no-hotkey"
$job = [powershell]::Create(); [void]$job.AddScript({ param($f, $e, $x) & $f -Exe $e -Arguments $x -TimeoutSec 1500 -Desktop DraftPanneaux }).AddArgument($launcher).AddArgument($Exe).AddArgument($a); $handle = $job.BeginInvoke()
for ($i = 0; $i -lt 60 -and -not (Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.ExecutablePath -eq $Exe }); $i++) { Start-Sleep -Milliseconds 250 }
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

# ---------- Scénarios (fictifs) ----------
$MID = "['TF','Vex','Fizz','Xerath']"; $BIG = "['TF','Vex','Fizz','Xerath','Ahri','Syndra','Orianna','Viktor','Annie']"
$LONGS = "['Renata Glasc','Aurelion Sol','TF','Miss Fortune','Nunu']"; $JUNGLE = "[`"Cho'Gath`",'Viego','Master Yi','Lee Sin']"
$COMP = "['Zed','Malphite','Vi','Caitlyn','Leona']"
# nom ; rôle ; pool ; adversaires ; consulter ; lecture (on, off) ; poste attribué ; Dans League (json ou '')
$S = [ordered]@{
  '01-debut'       = @('mid', $MID, '[]', $false, 'on', 'mid', '')
  '02-partielle'   = @('mid', $MID, "['Zed','Malphite','Vi']", $false, 'on', 'mid', '')
  '03-complete'    = @('mid', $MID, $COMP, $false, 'on', 'mid', '')
  '04-consulte'    = @('mid', $MID, $COMP, $true, 'on', 'mid', '')
  '05-egalite'     = @('jungle', $JUNGLE, "['Ornn','Sion','Maokai','Jinx','Thresh']", $false, 'on', 'jungle', '')
  '06-grandpool'   = @('mid', $BIG, $COMP, $false, 'on', 'mid', '')
  '07-role-main'   = @('mid', $MID, "['Zed','Malphite','Vi']", $false, 'on', 'jungle', '')
  '08-sans-lecture'= @('mid', $MID, $COMP, $false, 'off', '', '')
  '09-compact-league' = @('mid', $MID, $COMP, $true, 'on', 'mid', '{champion:"Fizz",state:"hover"}')
  '10-textes-longs'= @('mid', $LONGS, "['Kassadin','Tahm Kench','Master Yi',`"Kai'Sa`",'Miss Fortune']", $true, 'on', 'mid', '')
  '11-league-verrouille' = @('mid', $MID, $COMP, $false, 'on', 'mid', '{champion:"TF",state:"locked"}')
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
# Mesure dans la vraie page : essentiel visible sans défilement (mêmes éléments que draft-panneaux-tests.html).
$Measure = "(()=>{const d=document,H=innerHeight,W=innerWidth,bar=d.querySelector('.p-actions'),barTop=bar?bar.getBoundingClientRect().top:H;const ess=[['roles',d.querySelector('[data-test=role-switch-support]')],['etatRole',d.querySelector('[data-test=panel-role-meta]')||d.querySelector('[data-test=role-state]')],['conseil',d.querySelector('.p-reco')||d.querySelector('.tie-group')],['pourquoi',d.querySelector('.panel-right .p-why')],['menaceOuBan',d.querySelector('.p-threat')||d.querySelector('.p-keyban')],['consulte',d.querySelector('[data-test=viewed] .viewed-head')],['pick',d.querySelector('.p-actions [data-test=choose-pick]')||d.querySelector('[data-test=league-line]')],['details',d.querySelector('[data-test=open-analysis]')],['retourConseil',d.querySelector('[data-test=back-to-advice]')],['adversaires',d.querySelector('.compact-roles')||d.querySelector('.panel-left .roles')],['profil',d.querySelector('.panel-left .effects')]].filter(x=>x[1]);const hid=ess.filter(([k,e])=>{const r=e.getBoundingClientRect(),lim=e.closest('.p-actions')?H:barTop;return !(r.width>0&&r.top>=-0.5&&r.bottom<=lim+0.5&&r.left>=-0.5&&r.right<=W+0.5)}).map(x=>x[0]);return JSON.stringify({vue:W+'x'+H,dpr:devicePixelRatio,essentiel:ess.length,caches:hid,hostLcu:hostState.lcuStatus,role:myRole()})})()"

$rows = New-Object Collections.Generic.List[object]
# Bandeau au-dessus de la capture (hors du panneau : la mise en page n'est pas touchée). Bleu « connexion » pour la donnée de test.
function Add-Band([string]$file, [string]$text, [bool]$strong) {
  $img = [Drawing.Image]::FromFile($file)
  try {
    $h = [int][Math]::Max(18, [Math]::Round($img.Width / 16))
    $bmp = New-Object Drawing.Bitmap $img.Width, ($img.Height + $h)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear($(if ($strong) { [Drawing.Color]::FromArgb(121, 198, 218) } else { [Drawing.Color]::FromArgb(54, 47, 64) }))
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $font = New-Object Drawing.Font('Segoe UI', [single]([Math]::Max(8, $h * 0.42)), [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object Drawing.SolidBrush($(if ($strong) { [Drawing.Color]::FromArgb(11, 26, 32) } else { [Drawing.Color]::FromArgb(242, 236, 228) }))
    $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'; $fmt.Trimming = 'EllipsisCharacter'
    $g.DrawString($text, $font, $brush, (New-Object Drawing.RectangleF(2, 0, ($img.Width - 4), $h)), $fmt)
    $g.DrawImage($img, 0, $h, $img.Width, $img.Height)
    $g.Dispose(); $font.Dispose(); $brush.Dispose()
  } finally { $img.Dispose() }
  $bmp.Save($file, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
$Reset = "(()=>{document.querySelectorAll('#cap-v3').forEach(e=>e.remove());document.querySelectorAll('.p-sec[open]').forEach(e=>e.open=false);document.querySelectorAll('.p-scroll,.p-body,.col-left').forEach(e=>e.scrollTop=0);if(document.activeElement)document.activeElement.blur();return 1})()"
function Shot($target, [string]$scName, [string]$panel, [string]$size, [double]$dpr, [string]$label, [string]$extra = '', [string]$note = '') {
  $wh = $size.Split('x') | ForEach-Object { [int]$_ }
  $s = $S[$scName]
  $calls = @(@('Emulation.setDeviceMetricsOverride', @{ width = $wh[0]; height = $wh[1]; deviceScaleFactor = $dpr; mobile = $false }), 900,
    (Ev $Reset), (Ev (HostFixture $s)), 500)
  if ($extra) { $calls += , (Ev $extra); $calls += 600 }
  $calls += , (Ev $Measure)
  $calls += , @('Page.captureScreenshot', @{ format = 'png' })
  $calls += , @('Emulation.clearDeviceMetricsOverride', @{})
  $res = Invoke-CdpSession $target $calls
  $shot = $res | Where-Object { $_['result'] -and $_['result']['data'] } | Select-Object -Last 1
  $meas = ($res | Where-Object { $_['result'] -and $_['result']['result'] -and ($_['result']['result']['value'] -is [string]) -and $_['result']['result']['value'].StartsWith('{"vue"') } | Select-Object -Last 1)['result']['result']['value']
  $file = Join-Path $Out "$Version-$Lang-$scName-$panel-$label.png"
  [IO.File]::WriteAllBytes($file, [Convert]::FromBase64String($shot['result']['data']))
  $lg = $s[6] -and $Version -eq 'apres'
  $band = if ($lg) { 'DONNÉE DE TEST : « Dans League » simulé (lecture réelle : PR #40)' } elseif ($note) { "Données fictives · $note" } else { 'Données fictives · état du client simulé' }
  Add-Band $file $band $lg
  $m = $script:Json.DeserializeObject($meas)
  $rows.Add([pscustomobject]@{ fichier = Split-Path $file -Leaf; version = $Version; langue = $Lang; scenario = $scName; panneau = $panel; zone_utile_css = $size; echelle = $dpr; vue_page = $m['vue']; essentiel = $m['essentiel']; caches = ($m['caches'] -join ','); lecture_client = $m['hostLcu']; role = $m['role']; note = $note })
  "  $(Split-Path $file -Leaf) : $($m['vue']) dpr $($m['dpr']), essentiel $($m['essentiel'] - @($m['caches']).Count)/$($m['essentiel'])$(if (@($m['caches']).Count) { ' caché : ' + ($m['caches'] -join ',') }), lecture $($m['hostLcu'])"
}

try {
  $full = Get-CdpTarget -Port $Port -Panel 'full' -TimeoutSec 120
  if (-not $full) { throw 'fenêtre principale absente' }
  for ($i = 0; $i -lt 150; $i++) { if ((Invoke-CdpEval $full "typeof PROFILES!=='undefined'&&typeof hostState!=='undefined'&&hostState.available") -eq $true) { break }; Start-Sleep -Milliseconds 200 }
  "Version $Version, langue $(Invoke-CdpEval $full 'I18N.lang'), APP_VERSION $(Invoke-CdpEval $full 'APP_VERSION'), exe $Exe"
  [void](Invoke-CdpEval $full "saveMyProfile('mid',$MID);switchRole('mid','manual');(typeof storeOnboarding==='function')&&storeOnboarding({started:true,skipped:['pool','lcu','panels']});goView('home');1")
  Start-Sleep 1
  # Deux panneaux (gauche, droit) : la commande « masquer / afficher » de la page les affiche (aucun client : place manuelle).
  [void](Invoke-CdpEval $full "hostSend({cmd:'hideAll'});1")
  $right = Get-CdpTarget -Port $Port -Panel 'right' -TimeoutSec 60; $left = Get-CdpTarget -Port $Port -Panel 'left' -TimeoutSec 60
  if (-not $right -or -not $left) { throw 'panneaux gauche et droit absents' }
  for ($i = 0; $i -lt 100; $i++) { if ((Invoke-CdpEval $right "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced") -eq $true) { break }; Start-Sleep -Milliseconds 200 }
  Start-Sleep 2
  foreach ($sc in $S.Keys) {
    if ($Only.Count -and -not ($Only | Where-Object { $sc -like $_ })) { continue }
    if ($sc -eq '09-compact-league') { continue }
    "Scénario $sc : $(EvalT $right (Driver $sc $S[$sc]))"
    Start-Sleep -Milliseconds 800
    Shot $right $sc 'droit' '252x567' 1.25 '1920-125'
    if ($sc -in '01-debut', '02-partielle', '03-complete', '08-sans-lecture', '10-textes-longs') { Shot $left $sc 'gauche' '252x567' 1.25 '1920-125' }
    if ($sc -eq '04-consulte') {
      Shot $right $sc 'droit' '316x711' 1.0 '1920-100'
      if ($Version -eq 'apres') {
        Shot $right $sc 'droit' '252x711' 1.25 '1920-125-V2' '' 'V2 : hauteur 1,25 x client (taille de test)'
        Shot $right $sc 'droit' '356x567' 1.25 '1920-125-V3' "(()=>{const s=document.createElement('style');s.id='cap-v3';s.textContent='.p-now{display:none!important}';document.head.appendChild(s);document.querySelectorAll('.p-sec').forEach(e=>e.open=true);return 1})()" 'V3 : vue étendue Détails, simulée (option de test)'
        Shot $right $sc 'droit' '252x567' 1.25 '1920-125-details' "(()=>{document.querySelector('[data-test=open-analysis]').click();return 1})()" 'après un clic sur Détails'
      }
    }
    if ($Version -eq 'apres' -and $sc -eq '11-league-verrouille') { }
  }
  # Panneau unique (compact) : 1366 x 768 à 125 % (cas de l'utilisateur), et les échelles 100 et 150 %.
  [void](Invoke-CdpEval $full "hostSend({cmd:'toggleCompact'});1")
  $compact = Get-CdpTarget -Port $Port -Panel 'compact' -TimeoutSec 60
  if (-not $compact) { throw 'panneau compact absent' }
  for ($i = 0; $i -lt 100; $i++) { if ((Invoke-CdpEval $compact "typeof draftApi!=='undefined'&&typeof V!=='undefined'&&synced") -eq $true) { break }; Start-Sleep -Milliseconds 200 }
  Start-Sleep 2
  foreach ($sc in $S.Keys) {
    if ($Only.Count -and -not ($Only | Where-Object { $sc -like $_ })) { continue }
    "Scénario $sc (compact) : $(EvalT $compact (Driver $sc $S[$sc]))"
    Start-Sleep -Milliseconds 800
    Shot $compact $sc 'compact' '222x511' 1.25 '1366-125'
    if ($sc -eq '04-consulte') {
      Shot $compact $sc 'compact' '222x567' 1.0 '1366-100'
      Shot $compact $sc 'compact' '222x463' 1.5 '1366-150'
      Shot $compact $sc 'compact' '222x511' 1.5 '1920-150'
      if ($Version -eq 'apres') {
        Shot $compact $sc 'compact' '248x565' 1.25 '1366-125-V2' '' 'V2 : étroit 256, hauteur utile (taille de test)'
        Shot $compact $sc 'compact' '352x511' 1.25 '1366-125-V3' "(()=>{const s=document.createElement('style');s.id='cap-v3';s.textContent='.p-now,.p-compact-enemies{display:none!important}';document.head.appendChild(s);document.querySelectorAll('.p-sec').forEach(e=>e.open=true);return 1})()" 'V3 : vue étendue Détails, simulée (option de test)'
        Shot $compact $sc 'compact' '222x511' 1.25 '1366-125-details' "(()=>{document.querySelector('[data-test=open-analysis]').click();return 1})()" 'après un clic sur Détails'
      }
    }
  }
} finally {
  Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.ExecutablePath -eq $Exe } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; "instance d'essai $($_.ProcessId) arrêtée (chemin vérifié)" }
  $job.EndInvoke($handle) | Select-Object -Last 1; $job.Dispose()
  Remove-Item 'HKCU:\Software\DRAFT-Test-Panneaux' -Recurse -ErrorAction SilentlyContinue
  Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
  $csv = Join-Path $Out "manifeste-$Version-$Lang.csv"
  $rows | Export-Csv $csv -NoTypeInformation -Encoding UTF8 -Delimiter ';'
  "$($rows.Count) captures ; manifeste : $csv"
}
