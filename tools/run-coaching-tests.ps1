# Tests du moteur de conseils et du moteur stratégique, dans Chrome, sur une copie de travail propre à ce dossier.
param([string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\tests'), [switch]$Samples)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-engine.js', 'draft-data.js', 'draft-champions.js', 'draft-role-sheet.js', 'draft-champion-official.js', 'draft-coaching.js', 'draft-coaching-tests.html', 'draft-tests.html', 'draft-ties-tests.html', 'draft-mastery.js', 'draft-mastery-tests.html', 'draft-providers.js') { Copy-Item (Join-Path $web $f) $Work }; Copy-Item (Join-Path $web 'draft-i18n.js') $Work; Copy-Item (Join-Path $web 'i18n') (Join-Path $Work 'i18n') -Recurse -Force
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
function Page($name) {
  $prof = Join-Path $Work "pf-$name"; $out = Join-Path $Work "$name.dom"
  $url = 'file:///' + ((Join-Path $Work $name) -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=120000', '--dump-dom', "`"$url`"")
  $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  [void]$p.WaitForExit(200000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  [IO.File]::ReadAllText($out)
}
$dom = Page 'draft-coaching-tests.html'
$m = [regex]::Match($dom, '<pre id="out">([\s\S]*?)</pre>')
if (-not $m.Groups[1].Value) { throw 'Conseils : aucun résultat, erreur dans la page.' }
$r = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json -Depth 12
"Conseils : $($r.ok) réussis, $(@($r.ko).Count) échecs"
@($r.ko) | ForEach-Object { "  ÉCHEC : $_" }
"Moyenne par draft : débutant $($r.stats.moyenneParDraft.debutant), intermédiaire $($r.stats.moyenneParDraft.intermediaire), avancé $($r.stats.moyenneParDraft.avance) | avec un conseil propre à la draft : $($r.stats.avecConseilPropre) % | texte le plus long : $($r.stats.texteLePlusLong) signes"
"Fréquence d'apparition : " + (($r.stats.frequence.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)%" }) -join ', ')
if ($Samples) { $r.samples | ForEach-Object { "`n=== $($_.draft) | conseil $($_.pick), sinon $($_.rival) | adversaire $($_.opp) | $($_.conf) | $($_.niveau) | court: $($_.court -join ', ')"; $_.items | ForEach-Object { "  [$($_[0]) / $($_[1])] $($_[2]) : $($_[3])" + $(if ($_[4]) { " ($($_[4]))" } else { '' }) } } }
$ties = Page 'draft-ties-tests.html'; $tm = [regex]::Match($ties, '<pre id="out">([\s\S]*?)</pre>'); if (-not $tm.Groups[1].Value) { throw 'Égalités : aucun résultat.' }; $tr = [Net.WebUtility]::HtmlDecode($tm.Groups[1].Value) | ConvertFrom-Json; "Égalités : $($tr.ok) réussis, $(@($tr.ko).Count) échecs ($($tr.essais) essais de pool mélangé, $($tr.avecEgalite) drafts avec égalité)"; @($tr.ko) | ForEach-Object { "  ÉCHEC : $_" }
$ms = Page 'draft-mastery-tests.html'; $mm = [regex]::Match($ms, '<pre id="out">([\s\S]*?)</pre>'); if (-not $mm.Groups[1].Value) { throw 'Maîtrise : aucun résultat.' }; $mr = [Net.WebUtility]::HtmlDecode($mm.Groups[1].Value) | ConvertFrom-Json; "Maîtrise : $($mr.ok) réussis, $(@($mr.ko).Count) échecs ($($mr.conseils) conseils, $($mr.regles) règles déclenchées)"; @($mr.ko) | ForEach-Object { "  ÉCHEC : $_" }
# Même format JSON que les autres pages : une page qui plante ne passe plus pour « OK=0 ÉCHEC=0 ».
$e = Page 'draft-tests.html'; $em = [regex]::Match($e, '<pre id="out">([\s\S]*?)</pre>'); if (-not $em.Groups[1].Value) { throw 'Moteur stratégique : aucun résultat, erreur dans la page.' }; $er = [Net.WebUtility]::HtmlDecode($em.Groups[1].Value) | ConvertFrom-Json
"Moteur stratégique : $($er.ok) réussis, $(@($er.ko).Count) échecs"
@($er.ko) | ForEach-Object { "  ÉCHEC : $_" }
