# Écrit web/draft-champion-official.js depuis Data Dragon, la source publique de Riot. À relancer après un patch.
# Fichier local ensuite : aucune connexion à l'exécution, aucune clé.
param([string]$Out = (Join-Path (Split-Path $PSScriptRoot -Parent) 'web\draft-champion-official.js'))
$ErrorActionPreference = 'Stop'
$version = (Invoke-RestMethod 'https://ddragon.leagueoflegends.com/api/versions.json')[0]
$data = (Invoke-RestMethod "https://ddragon.leagueoflegends.com/cdn/$version/data/en_US/champion.json").data
# Noms de Data Dragon différents de ceux de DraftMid.
$rename = @{ 'Nunu & Willump' = 'Nunu' }
$rows = foreach ($p in $data.PSObject.Properties) {
  $c = $p.Value; $name = if ($rename.ContainsKey($c.name)) { $rename[$c.name] } else { $c.name }
  $tags = ($c.tags | ForEach-Object { '"' + $_ + '"' }) -join ','
  '"' + $name + '":{"tags":[' + $tags + '],"range":' + [int]$c.stats.attackrange + ',"speed":' + [int]$c.stats.movespeed + ',"attack":' + [int]$c.info.attack + ',"defense":' + [int]$c.info.defense + ',"magic":' + [int]$c.info.magic + ',"difficulty":' + [int]$c.info.difficulty + '}'
}
$text = "// Généré par tools/update-champion-official.ps1 depuis Data Dragon $version, source publique de Riot.`n// Portée et vitesse sont des statistiques du jeu. Les notes attack, defense, magic et difficulty sont des notes de 0 à 10 données par Riot, pas des mesures.`nconst CHAMPION_OFFICIAL = {`"version`":`"$version`",`"champions`":{" + (($rows | Sort-Object) -join ",`n") + "}};`n"
[IO.File]::WriteAllText($Out, $text, [Text.UTF8Encoding]::new($false))
"Écrit : $Out ($version, $(@($rows).Count) champions)"
