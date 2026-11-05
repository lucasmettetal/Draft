# Génère docs/COACHING-EXEMPLES.md : des exemples de conseils à relire, avec leur contexte exact.
param([string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'), [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\review'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-engine.js', 'draft-data.js', 'draft-champions.js', 'draft-role-sheet.js', 'draft-champion-official.js', 'draft-coaching.js') { Copy-Item (Join-Path $web $f) $Work }
Copy-Item (Join-Path $PSScriptRoot 'experiments\coaching-review.html') $Work; Copy-Item (Join-Path $DataDir 'draft-role-rates-live.js') $Work
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
$prof = Join-Path $Work 'pf'; $out = Join-Path $Work 'r.dom'; $url = 'file:///' + ((Join-Path $Work 'coaching-review.html') -replace '\\', '/')
$a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=60000', '--dump-dom', "`"$url`"")
$p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"; [void]$p.WaitForExit(120000)
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
$m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>'); if (-not $m.Groups[1].Value) { throw 'Aucun résultat : erreur dans la page.' }
$rows = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json -Depth 8
$level = @{ beginner = 'Débutant'; intermediate = 'Intermédiaire'; advanced = 'Avancé' }
$b = New-Object Text.StringBuilder
[void]$b.AppendLine('# Exemples de conseils à relire'); [void]$b.AppendLine('')
[void]$b.AppendLine('Généré par `tools/run-coaching-review.ps1`, avec les données de rôles locales. Situations choisies pour couvrir les cas, pas pour être représentatives.'); [void]$b.AppendLine('')
[void]$b.AppendLine('- **Règle :** le contenu se déduit d''une donnée ou d''un calcul, et un test peut le vérifier.'); [void]$b.AppendLine('- **À relire :** c''est un avis sur le jeu. Il faut un joueur pour dire s''il est juste.'); [void]$b.AppendLine('')
$rule = 0; $human = 0
foreach ($l in 'beginner', 'intermediate', 'advanced') {
  [void]$b.AppendLine("## Niveau $($level[$l])"); [void]$b.AppendLine('')
  $n = 0
  foreach ($r in @($rows | Where-Object { $_.level -eq $l })) {
    $n++
    [void]$b.AppendLine("### $($level[$l]) $n : $($r.nom)"); [void]$b.AppendLine('')
    [void]$b.AppendLine("- **Rôle et pool :** $($r.role), $(@($r.pool) -join ', ')")
    [void]$b.AppendLine("- **Adversaires visibles :** $(@($r.enemies) -join ', ')")
    [void]$b.AppendLine("- **Conseil du moteur :** $($r.pick)$(if ($r.rival) { ", sinon $($r.rival)" }) ; scores : $(@($r.scores) -join ', ')")
    [void]$b.AppendLine("- **Adversaire direct :** $(if ($r.opp) { $r.opp } else { 'non identifié' }), confiance $($r.conf)")
    [void]$b.AppendLine("- **Raisons du moteur :** $(if (@($r.raisons).Count) { @($r.raisons) -join ' ; ' } else { 'aucune' })")
    [void]$b.AppendLine("- **Ligne courte :** $($r.headline)"); [void]$b.AppendLine('')
    foreach ($i in $r.items) {
      if ($i.v -eq 'rule') { $rule++ } else { $human++ }
      $tag = if ($i.v -eq 'rule') { 'Règle' } else { 'À relire' }
      $court = if (@($r.court) -contains $i.id) { ', vue courte' } else { '' }
      $ev = if (@($i.ev).Count) { ' Déclencheur : `' + (ConvertTo-Json -InputObject @($i.ev) -Compress -Depth 4) + '`.' } else { '' }
      [void]$b.AppendLine("1. **$($i.title)** ($tag, $($i.basis)$court). $($i.text)$(if ($i.limits) { " *Limite : $($i.limits)*" })$ev")
    }
    [void]$b.AppendLine('')
  }
}
[IO.File]::WriteAllText((Join-Path $repo 'docs\COACHING-EXEMPLES.md'), $b.ToString(), [Text.UTF8Encoding]::new($false))
"Exemples : $(@($rows).Count) situations ; conseils : $($rule + $human) dont $rule par règle et $human à relire"
