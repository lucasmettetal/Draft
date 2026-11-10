# Reproduit le constat n°1 de l'audit : ouvrir la fenêtre principale depuis un panneau peut vider la draft des panneaux.
# Prérequis : lancer-audit.ps1, panneaux affichés, profil Mid rempli. Lecture seule sur le code de DRAFT.
# Critère de la future correction : 0 perte sur 50 essais.
param([int]$Runs = 10, [int]$Port = 9339)
. (Join-Path $PSScriptRoot 'cdp.ps1')
$add = @'
(async()=>{const w=m=>new Promise(r=>setTimeout(r,m));resetDraft();await w(300);
const set=(el,v)=>{Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(el,v);el.dispatchEvent(new Event('input',{bubbles:true}));};
for(const q of ['ahri','sion','vi']){const inp=document.getElementById('add');inp.focus();set(inp,q);await w(250);inp.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));await w(350);}
return state.enemies.length})()
'@
$lost = 0
for ($i = 1; $i -le $Runs; $i++) {
  $ts = Get-CdpTargets -Port $Port
  # Panneau unique s'il existe (les panneaux gauche et droit restent alors chargés mais cachés).
  $compact = $ts | Where-Object { $_.url -match 'panel=compact$' } | Select-Object -First 1
  $panel = if ($compact) { $compact } else { $ts | Where-Object { $_.url -match 'panel=left$' } | Select-Object -First 1 }
  $verdict = if ($compact) { $compact } else { $ts | Where-Object { $_.url -match 'panel=right$' } | Select-Object -First 1 }
  [void](Invoke-CdpEval $panel $add)
  Start-Sleep 1
  [void](Invoke-CdpEval $verdict "(()=>{const b=document.querySelector('.p-actions .ghost-btn');b.click();return 1})()")
  Start-Sleep 4
  $n = Invoke-CdpEval ((Get-CdpTargets -Port $Port) | Where-Object { $_.url -eq $panel.url }) 'state.enemies.length'
  if ($n -ne 3) { $lost++ }
  "essai $i : $n adversaire(s) dans le panneau après ouverture de l'analyse"
}
"PERTES : $lost sur $Runs"
