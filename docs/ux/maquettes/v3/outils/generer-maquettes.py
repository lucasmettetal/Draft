# Génère les maquettes statiques DRAFT 3.0 (aucun script dans les pages produites).
import sys, pathlib
OUT = pathlib.Path(sys.argv[1])

def ico(d, cls="i"):
    return f'<svg class="{cls}" viewBox="0 0 24 24" aria-hidden="true"><path d="{d}"/></svg>'

I = dict(
    home="M4 11l8-7 8 7v9h-5v-6h-6v6H4z",
    pool="M4 4h7v7H4z M13 4h7v7h-7z M4 13h7v7H4z M13 13h7v7h-7z",
    sim="M6 4l13 8-13 8z",
    stats="M5 20V10 M12 20V4 M19 20v-7",
    gear="M12 9a3 3 0 1 0 0 6a3 3 0 1 0 0-6z M12 2v3 M12 19v3 M2 12h3 M19 12h3 M4.9 4.9l2.1 2.1 M17 17l2.1 2.1 M4.9 19.1L7 17 M17 7l2.1-2.1",
    check="M5 12l5 5 9-10",
    x="M6 6l12 12 M18 6L6 18",
    two="M3 5h7v14H3z M14 5h7v14h-7z",
    one="M4 5h16v14H4z",
    search="M10.5 4a6.5 6.5 0 1 0 0 13a6.5 6.5 0 1 0 0-13z M20 20l-4.8-4.8",
    expand="M14 4h6v6 M10 20H4v-6 M20 4l-7 7 M4 20l7-7",
    star="M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z",
    refresh="M20 12a8 8 0 1 1-2.4-5.7 M20 4v5h-5",
    down="M12 4v11 M7 10l5 5 5-5 M5 20h14",
    up="M12 20V9 M7 14l5-5 5 5 M5 4h14",
    info="M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18z M12 11v6 M12 7.5v.5",
    alert="M12 3l10 18H2z M12 10v5 M12 18v.5",
    shield="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z",
    plug="M9 3v5 M15 3v5 M6 8h12v3a6 6 0 0 1-12 0z M12 17v4",
    pulse="M3 12h4l3-7 4 14 3-7h4",
    monitor="M3 4h18v12H3z M8 20h8 M12 16v4",
    key="M8 14a4 4 0 1 1 0-8a4 4 0 0 1 0 8z M11.5 11.5L21 21 M17 17l2-2 M15 15l2-2",
    undo="M9 7L4 12l5 5 M4 12h11a5 5 0 0 1 0 10h-3",
    chev="M9 6l6 6-6 6",
    clock="M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18z M12 7v5l3 3",
    globe="M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18z M3 12h18 M12 3c3 3 3 15 0 18 M12 3c-3 3-3 15 0 18",
    eye="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z M12 9a3 3 0 1 0 0 6a3 3 0 1 0 0-6z",
    folder="M3 6h6l2 2h10v11H3z",
    play="M6 4l13 8-13 8z",
)
ROLE = dict(
    top="M3 3h14v4H7v10H3z M11 11h6v6h-6z",
    jungle="M10 2c1 5 5 6 5 11a5 5 0 0 1-10 0c0-5 4-6 5-11z",
    mid="M3 17 15 3h2v2L5 17z M3 3h6v3H6v3H3z M17 17h-6v-3h3v-3h3z",
    adc="M17 17H3v-4h10V3h4z M3 3h6v6H3z",
    support="M10 2l7 3v5c0 4-3 7-7 8-4-1-7-4-7-8V5z",
)
RN = dict(top="Top", jungle="Jungle", mid="Mid", adc="ADC", support="Support")
def role(r): return f'<svg class="role" viewBox="0 0 20 20" aria-hidden="true"><path d="{ROLE[r]}"/></svg>'
LOGO = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 20V9l6-5h10v5l-6 11z M9 15l6-6"/></svg>'

def mono(name, size="", tone=1):
    ini = "".join(w[0] for w in name.replace("'", " ").split()[:2]).upper() if " " in name or "'" in name else name[:2].capitalize()
    return f'<span class="mono {size} t{tone}" aria-hidden="true">{ini}</span>'

FLAG = '<p class="mock-flag">Maquette non validée. Données d\'exemple.</p>'

def head(title, extra_class=""):
    return f'''<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Maquette non validée : {title}</title>
<!-- MAQUETTE NON VALIDÉE. DRAFT 3.0 bêta, option B. Statique : aucun script, aucune ressource externe. Données d'exemple, aucune donnée personnelle. -->
<link rel="stylesheet" href="v3.css">
</head>
<body{(' class="' + extra_class + '"') if extra_class else ''}>
'''

NAVS = [("accueil.html", "home", "Accueil", ""), ("mon-pool.html", "pool", "Mon pool", ""), ("simulateur.html", "sim", "Simulateur", ""), ("statistiques.html", "stats", "Mes statistiques", "")]

def nav(active, league=("good", "Connecté"), draft="Aucune"):
    links = "".join(f'<a class="nav-link" href="{h}"{" aria-current=\"page\"" if k == active else ""}>{ico(I[k])}<span>{t}</span></a>' for h, k, t, _ in NAVS)
    sett = f'<a class="nav-link" href="reglages.html"{" aria-current=\"page\"" if active == "gear" else ""}>{ico(I["gear"])}<span>Réglages</span></a>'
    return f'''<nav class="nav" aria-label="Navigation principale">
  <div class="brand">{LOGO}<span>DRAFT</span><small>bêta</small></div>
  <div class="nav-list">{links}</div>
  <div class="nav-grow"></div>
  <div class="nav-list">{sett}</div>
  <div class="nav-state" aria-label="État">
    <p class="line"><i class="dot {league[0]}"></i><span class="lbl">League</span><span>{league[1]}</span></p>
    <p class="line">{role("mid")}<span class="lbl">Rôle joué</span><span>Mid</span></p>
  </div>
  <p class="nav-legal">Version 3.0.0 bêta. Non approuvé par Riot Games.</p>
</nav>'''

def page(fname, title, active, body, wide=False, league=("good", "Connecté")):
    html = head(title) + f'''<div class="app">
{nav(active, league)}
<div class="main"><main class="page{' wide' if wide else ''}">
{body}
</main></div>
</div>
{FLAG}
</body>
</html>
'''
    (OUT / fname).write_text(html, encoding="utf-8")

# ------------------------------------------------------------------ Accueil
accueil = f'''<header class="page-head">
  <div><h1>Accueil</h1><p>Ce qu'il faut savoir avant ta prochaine draft.</p></div>
  <span class="example">Exemple</span>
</header>
<section class="ready">
  <div class="box ready-main" aria-labelledby="pret">
    <h2 class="ready-title" id="pret">Tout est prêt pour ta prochaine draft</h2>
    <p class="ready-sub">Aucune draft en cours. Les panneaux s'ouvriront à côté du client dès la sélection des champions.</p>
    <ul class="checks">
      <li><span class="ico ok">{ico(I["check"])}</span><span class="what"><b>Client League connecté</b><span>Lecture du client active. Les adversaires se remplissent seuls.</span></span><a class="btn btn-sm btn-secondary" href="reglages.html#connexion">Connexion League</a></li>
      <li><span class="ico ok">{ico(I["check"])}</span><span class="what"><b>Rôle joué : Mid, 3 champions</b><span>Ahri, Fizz, Kog'Maw.</span></span><a class="btn btn-sm btn-secondary" href="mon-pool.html">Modifier mon pool</a></li>
      <li><span class="ico ok">{ico(I["check"])}</span><span class="what"><b>Panneaux automatiques</b><span>Pour les afficher ou les masquer à tout moment : <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>D</kbd></span></span><a class="btn btn-sm btn-secondary" href="reglages.html#affichage">Affichage</a></li>
    </ul>
    <div class="ready-actions">
      <a class="btn btn-primary" href="simulateur.html">{ico(I["play"])}Simuler une draft</a>
      <button class="btn btn-secondary" type="button">Afficher les panneaux</button>
    </div>
  </div>
  <div class="box preview" aria-labelledby="sans">
    <h2 class="h2" id="sans">Ton conseil sans adversaire</h2>
    <p class="small muted">Ce que DRAFT te proposera en début de draft, avant le premier pick adverse.</p>
    <div class="big">{mono("Ahri", "s44", 2)}<b>Ahri</b><span class="tag net">Recommandé</span></div>
    <ul class="reasons small"><li>Sûre en lane contre la plupart des mids.</li><li>Fizz et Kog'Maw restent proches : la draft adverse départagera.</li></ul>
    <div class="nav-grow"></div>
    <p class="small muted" style="margin-top:16px">Ban conseillé : <b>Zed</b>, le plus gênant pour ton pool.</p>
  </div>
</section>
<section class="ready" style="margin-top:16px">
  <div class="box">
    <div class="box-head"><h2>Mise à jour</h2><p>Version 3.0.0 bêta</p></div>
    <ul class="rows" style="margin-top:8px">
      <li>{ico(I["check"])}<span>DRAFT est à jour.</span><span class="meta">Vérifié aujourd'hui à 10:42</span></li>
      <li><span class="small muted">DRAFT ne contacte GitHub que lorsque tu le demandes.</span><button class="btn btn-sm btn-secondary" type="button" style="margin-left:auto">Rechercher</button></li>
    </ul>
  </div>
  <div class="box">
    <div class="box-head"><h2>Données</h2><p>Ce qui affine tes conseils</p><div class="tools"><a class="btn-link btn small" href="reglages.html#donnees">Gérer</a></div></div>
    <ul class="rows" style="margin-top:8px">
      <li><span>Rôles des champions</span><span class="meta">Calculés le 03/11/2026</span></li>
      <li><span>Matchups du rôle Mid</span><span class="meta">Calculés le 03/11/2026, 4 820 parties</span></li>
      <li><span>Mon historique Riot</span><span class="meta">100 parties, importées le 08/11/2026</span></li>
    </ul>
  </div>
</section>'''
page("accueil.html", "Accueil", "home", accueil)
live = accueil.replace('''<section class="ready">''', f'''<div class="band band-live" role="status" style="margin-bottom:16px"><i class="dot good"></i><b>Draft en cours</b><span>3 adversaires lus dans le client. Conseil actuel : Fizz.</span><span class="grow"></span><button class="btn btn-sm btn-secondary" type="button">Afficher les panneaux</button><a class="btn btn-sm btn-secondary" href="simulateur.html">{ico(I["expand"])}Détails</a></div>
<section class="ready">''', 1).replace("Aucune draft en cours. Les panneaux s'ouvriront à côté du client dès la sélection des champions.", "Sélection en cours. Les panneaux sont affichés à côté du client et se masqueront au lancement de la partie.").replace("Tout est prêt pour ta prochaine draft", "Ta draft est suivie")
page("accueil-draft-en-cours.html", "Accueil, draft en cours", "home", live)

# ------------------------------------------------------------------ Accueil premier lancement
first = f'''<header class="page-head">
  <div><h1>Bienvenue dans DRAFT</h1><p>Trois étapes, environ deux minutes. Tu peux les faire dans l'ordre que tu veux.</p></div>
</header>
<section class="ready">
  <div class="box ready-main" aria-labelledby="pret">
    <h2 class="ready-title" id="pret">Prépare ta première draft</h2>
    <p class="ready-sub">DRAFT t'aide à choisir ton champion pendant la sélection. Des panneaux s'ouvrent à côté du client League, puis se masquent au lancement de la partie.</p>
    <ol class="checks">
      <li><span class="ico ok">{ico(I["check"])}</span><span class="what"><b>Ton rôle et tes champions</b><span>Mid : Ahri, Fizz, Kog'Maw. <span class="saved">{ico(I["check"])}Enregistré</span></span></span><a class="btn btn-sm btn-secondary" href="mon-pool.html">Modifier</a></li>
      <li><span class="ico todo">2</span><span class="what"><b>La lecture du client League</b><span>Les adversaires se remplissent seuls. Lecture seule, rien n'est modifié dans le client. Riot ne l'a pas confirmée : c'est ton choix.</span></span><a class="btn btn-sm btn-primary" href="reglages.html#connexion" style="font-size:var(--fs-md)">Choisir</a></li>
      <li><span class="ico">3</span><span class="what"><b>Un essai des panneaux</b><span>Une fausse draft, pour voir où ils s'affichent. Ta vraie draft n'est jamais touchée.</span></span><button class="btn btn-sm btn-secondary" type="button">Essayer</button></li>
    </ol>
    <p class="small muted" style="margin-top:16px">Fenêtre fermée, DRAFT reste actif près de l'horloge. Pour revenir ici : menu Démarrer, ou clic sur son icône.</p>
  </div>
  <div class="box preview" aria-labelledby="sans">
    <h2 class="h2" id="sans">Ton conseil sans adversaire</h2>
    <p class="small muted">Il apparaît dès que ton pool contient un champion.</p>
    <div class="big">{mono("Ahri", "s44", 2)}<b>Ahri</b><span class="tag net">Recommandé</span></div>
    <ul class="reasons small"><li>Sûre en lane contre la plupart des mids.</li></ul>
    <div class="nav-grow"></div>
    <p class="small muted" style="margin-top:16px">Sans statistiques calculées, le conseil repose sur des fiches écrites à la main : c'est un avis général.</p>
  </div>
</section>'''
page("accueil-premier-lancement.html", "Accueil, premier lancement", "home", first, league=("wait", "À choisir"))

# ------------------------------------------------------------------ Mon pool
def rolerow(r, n, cur=False, played=False):
    return f'<a class="role-item" href="#"{" aria-current=\"true\"" if cur else ""}>{role(r)}<span>{RN[r]}{" <span class=\"played\">Joué</span>" if played else ""}</span><span class="n num">{n}</span></a>'
pool_rows = [("Darius", 3, True, False), ("Garen", 1, False, False), ("Mordekaiser", 5, False, True), ("Sett", 4, False, False)]
trs = "".join(f'''<tr><td><span class="name">{mono(n, "s32", t)}{n}</span></td><td class="c"><span class="radio{" on" if fav else ""}" role="img" aria-label="{'Préféré à égalité' if fav else 'Non préféré'}"></span></td><td class="c"><span class="check{" on" if ex else ""}" role="img" aria-label="{'Exclu' if ex else 'Disponible'}">{ico(I["check"]) if ex else ""}</span></td><td class="c"><button class="btn btn-icon" type="button" aria-label="Retirer {n} du pool Top">{ico(I["x"])}</button></td></tr>''' for n, t, fav, ex in pool_rows)
sugg = "".join(f'<button class="chip add" type="button">{mono(n, "s24", (i % 5) + 1)}{n}</button>' for i, n in enumerate(["Aatrox", "Camille", "Fiora", "Jax", "K'Sante", "Malphite", "Ornn", "Renekton", "Shen", "Volibear"]))
pool = f'''<header class="page-head">
  <div><h1>Mon pool</h1><p>Chaque rôle garde ses champions. Modifier un pool ne change pas ton rôle joué.</p></div>
  <span class="example">Exemple</span>
</header>
<div class="band band-info" style="margin-bottom:16px">{role("mid")}<b>Rôle joué</b><span class="muted">Utilisé pour tes conseils et dans les panneaux.</span><span class="grow"></span>
  <div class="seg" role="group" aria-label="Rôle joué">{"".join(f'<button type="button" aria-pressed="{str(r=="mid").lower()}">{role(r)}{RN[r]}</button>' for r in ROLE)}</div>
</div>
<div class="pool-layout">
  <nav class="box role-list" aria-label="Pool à modifier">
    {rolerow("top", 4, cur=True)}{rolerow("jungle", 0)}{rolerow("mid", 3, played=True)}{rolerow("adc", 2)}{rolerow("support", 0)}
  </nav>
  <div class="stack-4">
    <section class="box" aria-labelledby="pt">
      <div class="box-head"><h2 id="pt">Pool Top</h2><p>4 champions</p><div class="tools"><span class="saved" role="status">{ico(I["check"])}Enregistré : Sett ajouté au pool Top.</span><button class="btn btn-sm btn-secondary" type="button">{ico(I["undo"])}Annuler</button></div></div>
      <p class="hint" style="padding:4px 24px 0">Tu modifies le pool Top. Ton rôle joué reste Mid.</p>
      <div style="padding:16px 24px 8px">
        <label class="search"><span class="sr">Ajouter un champion au pool Top</span>{ico(I["search"])}<input class="input is-focus" type="search" value="mor" aria-describedby="aide-recherche"><kbd>Entrée</kbd></label>
        <p class="hint" id="aide-recherche" style="margin-top:6px">Nom ou surnom : « tf », « kata », « mf ». Entrée ajoute le premier résultat.</p>
        <div class="suggest" style="margin-top:10px"><button class="chip sel" type="button">{mono("Mordekaiser", "s24", 5)}Mordekaiser <span class="muted xs">déjà dans le pool</span></button><button class="chip add" type="button">{mono("Morgana", "s24", 2)}Morgana</button></div>
      </div>
      <table class="champ-table" aria-label="Champions du pool Top">
        <thead><tr><th>Champion</th><th class="c">Préféré à égalité</th><th class="c">Exclu pour l'instant</th><th class="c"><span class="sr">Retirer</span></th></tr></thead>
        <tbody>{trs}</tbody>
      </table>
    </section>
    <section class="box" aria-labelledby="sg">
      <div class="box-head"><h2 id="sg">Souvent joués en Top</h2><p>Un clic les ajoute au pool Top.</p></div>
      <div class="suggest" style="padding:12px 24px 20px">{sugg}</div>
    </section>
    <section class="box" aria-labelledby="sv">
      <div class="box-head"><h2 id="sv">Sauvegarde des profils</h2><p>Rôles, champions et préférences. Sans historique ni clé.</p></div>
      <div class="cluster" style="padding:12px 24px 20px"><button class="btn btn-secondary" type="button">{ico(I["down"])}Exporter mes profils</button><button class="btn btn-secondary" type="button">{ico(I["up"])}Importer des profils</button><button class="btn btn-secondary" type="button">{ico(I["undo"])}Annuler le dernier import</button></div>
    </section>
  </div>
</div>'''
page("mon-pool.html", "Mon pool", "pool", pool)

# ------------------------------------------------------------------ Simulateur
def slot(r, name=None, traits="", t=1, mine=False, em=""):
    if not name:
        return f'<div class="slot empty"><span class="lane">{role(r)}{RN[r]}</span><span class="ghost"></span><span class="who">Ajouter le {RN[r]}</span><span></span></div>'
    return f'<div class="slot{" mine" if mine else ""}"><span class="lane">{role(r)}{RN[r]}</span>{mono(name, "", t)}<span class="who"><b>{name}</b><span>{f"<em>{em}</em>, " if em else ""}{traits}</span></span><button class="btn btn-icon" type="button" aria-label="Retirer {name}">{ico(I["x"])}</button></div>'
meters = [("Frontline", 2, False), ("Carrys fragiles", 2, False), ("Plongeurs", 1, True), ("Contrôles ciblés", 1, True), ("Engage", 1, False)]
mhtml = "".join(f'<div class="meter{" hot" if d else ""}"><span>{l}</span><span class="pips" role="img" aria-label="{l} : {c} sur 5">{"".join("<i class=\"f\"></i>" if i < c else "<i></i>" for i in range(5))}</span></div>' for l, c, d in meters)
sim = f'''<header class="page-head">
  <div><h1>Simulateur</h1><p>Prépare une draft à l'avance. Rien n'est envoyé aux panneaux. <span class="example">Exemple</span></p></div>
  <div class="tools"><div class="seg gold" role="group" aria-label="Source de la vue"><button type="button" aria-pressed="true">Simulation</button><button type="button" aria-pressed="false">Draft en cours</button></div></div>
</header>
<div class="band band-sim" role="status" style="margin-bottom:16px">{ico(I["info"])}<b>Simulation</b><span>Ta draft en cours n'est pas modifiée.</span><span class="grow"></span>
  <button class="btn btn-sm btn-secondary" type="button">Draft aléatoire</button>
  <button class="btn btn-sm btn-secondary" type="button">Nouvelle simulation</button>
  <button class="btn btn-sm btn-secondary is-disabled" type="button" disabled title="Aucune sélection en cours">Reprendre la draft en cours</button>
</div>
<div class="desk">
  <section class="box box-pad" aria-labelledby="ca">
    <h2 class="h2" id="ca" style="display:flex">Composition adverse <span class="muted small" style="margin-left:auto">4 sur 5</span></h2>
    <label class="search" style="display:block;margin:12px 0 6px"><span class="sr">Ajouter un adversaire</span>{ico(I["search"])}<input class="input" type="search" placeholder="Adversaire : ahri, tf, kata..."></label>
    <p class="hint" style="margin-bottom:12px">Entrée ajoute. Échap vide le champ, jamais la draft.</p>
    {slot("top", "Ornn", "Tank, Engage", 3)}{slot("jungle", "Lee Sin", "Bruiser, Plongeur", 5)}{slot("mid", "Syndra", "Carry, Contrôle", 2, True, "Mid probable")}{slot("adc", "Jinx", "Carry", 1)}{slot("support")}
    <h3 class="h2" style="margin:20px 0 10px">Profil de l'équipe</h3>
    {mhtml}
    <p class="small" style="margin-top:10px"><span style="color:var(--threat)">Leurs forces : frontline.</span> <span style="color:var(--accent)">Leurs failles : plusieurs cibles fragiles.</span></p>
  </section>
  <div class="stack-4">
    <section class="box stage" aria-label="Recommandation">
      <div class="art" data-label="Illustration du champion"></div><div class="art-fade"></div>
      <div class="cluster"><span class="tag net">Choix net</span><span class="tag">4/5 adversaires</span></div>
      <p class="verdict-name">Fizz</p>
      <p class="small"><span class="muted">Sinon</span> <b>Ahri</b></p>
      <ul class="reasons" style="margin-top:10px"><li>1er choix contre Syndra</li><li>2 cibles fragiles : Syndra, Jinx</li></ul>
      <div class="danger" style="margin-top:12px">{mono("Syndra", "s32", 2)}<div><b>Danger : Syndra</b><p>Son contrôle ciblé peut stopper ton plongeon.</p></div></div>
      <div class="cluster" style="margin-top:16px"><button class="btn btn-primary" type="button">Je joue Fizz</button><span class="hint">Note ton pick dans ton journal.</span></div>
    </section>
    <section class="box box-pad" aria-labelledby="tp">
      <h2 class="h2" id="tp" style="display:flex">Ton pool <span class="muted small" style="margin-left:auto">Clique sur le champion que tu joues</span></h2>
      <div style="margin-top:12px">
        <div class="rank first">{mono("Fizz", "", 1)}<span class="who"><b>Fizz</b><span class="bar"><i style="width:92%"></i></span></span><span class="tag solid">Recommandé</span></div>
        <div class="rank">{mono("Ahri", "", 2)}<span class="who"><b>Ahri</b><span class="bar"><i style="width:70%"></i></span></span><span class="tag">Alternative</span></div>
        <div class="rank">{mono("Kog'Maw", "", 4)}<span class="who"><b>Kog'Maw</b><span class="bar"><i style="width:38%"></i></span></span><span class="tag">Possible</span></div>
      </div>
    </section>
  </div>
  <section class="box side" aria-labelledby="dt">
    <div class="box-head"><h2 id="dt">Plan de jeu</h2><p>avec Fizz</p></div>
    <div style="padding:12px 24px 0"><p class="small" style="color:var(--accent)">Gagne si</p><p>Pousse ta vague, roam, et tue Syndra avant l'objectif.</p></div>
    <div class="tabs" role="tablist" aria-label="Détails" style="margin-top:16px">
      <button role="tab" aria-selected="true" type="button">Plan</button><button role="tab" aria-selected="false" type="button">Critères</button><button role="tab" aria-selected="false" type="button">Comparer</button><button role="tab" aria-selected="false" type="button">Conseils</button><button role="tab" aria-selected="false" type="button">Duel</button>
    </div>
    <div style="padding:16px 24px 20px">
      <div class="seg" role="group" aria-label="Phase de jeu"><button type="button" aria-pressed="true">Lane</button><button type="button" aria-pressed="false">Mid game</button><button type="button" aria-pressed="false">Teamfights</button></div>
      <ul class="tips" style="margin-top:12px"><li>Syndra a un contrôle ciblé : attends de le voir partir avant de lancer ton all-in.</li><li>Contre Syndra, cherche des échanges courts, puis ressors avec ton E.</li><li>Lee Sin peut venir en gank : place une ward avant de pousser ta vague.</li></ul>
      <p class="hint" style="margin-top:12px">Avis général basé sur les 4 champions adverses visibles. <a class="btn-link btn" href="#">Comment DRAFT classe tes picks</a></p>
    </div>
  </section>
</div>'''
page("simulateur.html", "Simulateur", "sim", sim, wide=True)

# ------------------------------------------------------------------ Mes statistiques
games = [("Fizz", "Syndra", "win", "Suivi", "10/11"), ("Ahri", "Zed", "loss", "Alternative", "10/11"), ("Fizz", "Orianna", "win", "Suivi", "09/11"), ("Kog'Maw", "Yasuo", "none", "Suivi", "09/11"), ("Ahri", "Viktor", "win", "Suivi", "08/11")]
gt = "".join(f'<tr><td><span class="name" style="display:flex;align-items:center;gap:10px">{mono(p, "s24", i % 5 + 1)}{p}</span></td><td>{m}</td><td>{f}</td><td><span class="res {r}">{dict(win="Victoire", loss="Défaite", none="Non noté")[r]}</span></td><td class="muted num">{d}</td></tr>' for i, (p, m, r, f, d) in enumerate(games))
def wb(n, w, l): return f'<div class="winbar"><span>{n}</span><span class="track" role="img" aria-label="{n} : {w} victoires, {l} défaites"><i class="w" style="width:{100*w/(w+l):.0f}%"></i><i class="l" style="width:{100*l/(w+l):.0f}%"></i></span><span class="r num">{w} V, {l} D</span></div>'
stats = f'''<header class="page-head">
  <div><h1>Mes statistiques</h1><p>Ton journal DRAFT et ton historique Riot, au même endroit.</p></div>
  <span class="example">Exemple</span>
</header>
<div class="tabs" role="tablist" aria-label="Statistiques" style="margin-bottom:20px"><a role="tab" aria-selected="true" href="#">Vue d'ensemble</a><a role="tab" aria-selected="false" href="#">Journal des drafts</a><a role="tab" aria-selected="false" href="#">Historique Riot</a></div>
<dl class="kpis">
  <div><dt>Drafts notées</dt><dd class="num">24</dd></div>
  <div><dt>Victoires</dt><dd class="num">13 <small>sur 22 parties terminées</small></dd></div>
  <div><dt>Conseil suivi</dt><dd class="num">19 <small>sur 24</small></dd></div>
  <div><dt>Historique Riot</dt><dd class="num">100 <small>parties, 08/11/2026</small></dd></div>
</dl>
<div class="grid-2" style="margin-top:16px">
  <section class="box" aria-labelledby="vc">
    <div class="box-head"><h2 id="vc">Victoires par champion joué</h2><p>Journal DRAFT</p></div>
    <div style="padding:8px 24px 16px">{wb("Fizz", 6, 3)}{wb("Ahri", 5, 4)}{wb("Kog'Maw", 2, 2)}</div>
    <div class="box-head" style="padding-top:0"><h2>Selon le type de mid adverse</h2></div>
    <div style="padding:8px 24px 20px">{wb("Contrôle", 5, 2)}{wb("Assassin", 3, 5)}{wb("Mage de poke", 5, 2)}</div>
  </section>
  <section class="box" aria-labelledby="hr">
    <div class="box-head"><h2 id="hr">Historique Riot</h2><p>Ton expérience, jamais le classement.</p></div>
    <ul class="rows" style="margin-top:8px">
      <li>{ico(I["check"])}<span>100 parties importées</span><span class="meta">le 08/11/2026, compte Exemple#EUW</span></li>
      <li><span class="small muted">Tu saisis ta clé Riot dans une fenêtre DRAFT. Elle sert à un seul import et n'est jamais enregistrée.</span></li>
      <li><button class="btn btn-secondary" type="button">{ico(I["key"])}Importer plus de parties</button><span class="meta">Environ 3 minutes pour 100 parties</span></li>
    </ul>
    <div class="box-head" style="padding-top:8px"><h2>Le plus joué en Mid</h2></div>
    <div style="padding:8px 24px 20px">{wb("Ahri", 18, 14)}{wb("Fizz", 12, 9)}{wb("Syndra (hors pool)", 4, 6)}</div>
  </section>
</div>
<section class="box" style="margin-top:16px" aria-labelledby="jd">
  <div class="box-head"><h2 id="jd">Dernières drafts</h2><p>Notées avec « Je joue ». DRAFT garde le pick, le mid adverse et le résultat, rien d'autre.</p><div class="tools"><button class="btn btn-sm btn-secondary" type="button">Voir tout le journal</button></div></div>
  <table class="table" style="margin-top:8px"><thead><tr><th>Ton pick</th><th>Mid adverse</th><th>Conseil</th><th>Résultat</th><th>Date</th></tr></thead><tbody>{gt}</tbody></table>
</section>
<details class="box box-pad" style="margin-top:16px"><summary class="small">Importer un ancien fichier (journal DRAFT ou statistiques exportées)</summary><p class="hint" style="margin-top:8px">Rien n'est supprimé. Un fichier invalide ne modifie rien.</p></details>'''
page("statistiques.html", "Mes statistiques", "stats", stats)

# ------------------------------------------------------------------ Réglages
def tline(title, sub, on=True, locked=False):
    return f'<label class="toggle{" on" if on else ""}{" off-locked" if locked else ""}"><span class="sw" aria-hidden="true"></span><span class="txt"><b>{title}</b><span>{sub}</span></span></label>'
def sline(title, sub, ctrl):
    return f'<div class="set-line"><span class="txt"><b style="font-weight:400">{title}</b><span>{sub}</span></span>{ctrl}</div>'
def sel(opts, label):
    return f'<select class="select" aria-label="{label}">' + "".join(f"<option>{o}</option>" for o in opts) + "</select>"
SUB = [("general", "gear", "Général"), ("connexion", "plug", "Connexion League"), ("affichage", "monitor", "Affichage et fenêtres"), ("donnees", "shield", "Données et confidentialité"), ("mises-a-jour", "refresh", "Mises à jour"), ("diagnostic", "pulse", "Diagnostic et informations")]
subnav = "".join(f'<a href="#{k}"{" class=\"on\"" if k == "general" else ""}>{ico(I[i])}{t}</a>' for k, i, t in SUB)
sections = f'''
<section class="set-section default" id="general" aria-labelledby="h-gen">
  <h2 id="h-gen">Général</h2><p class="lede">Ce qui vaut partout dans DRAFT.</p>
  <div class="box set-group">
    {sline("Langue", "Plus tard : français ou anglais. Automatique suit la langue de Windows.", sel(["Automatique (Windows)", "Français", "English"], "Langue"))}
    {sline("Niveau des conseils", "Change la quantité d'explications, jamais le classement.", sel(["Intermédiaire", "Débutant", "Avancé"], "Niveau des conseils"))}
  </div>
  <div class="box set-group">
    {tline("Supposer une frontline dans mon équipe", "Le calcul compte sur un tank ou un bruiser allié. Désactive-le si ton équipe en manque souvent.", True)}
    {tline("Démarrer avec Windows", "DRAFT se lance discrètement près de l'horloge, sans ouvrir de fenêtre.", True)}
  </div>
</section>
<section class="set-section" id="connexion" aria-labelledby="h-con">
  <h2 id="h-con">Connexion League</h2><p class="lede">Comment DRAFT sait qu'une draft commence et qui est en face.</p>
  <div class="band band-live" role="status" style="margin-bottom:16px"><i class="dot good"></i><b>Connecté au client League</b><span class="muted">Dernière lecture il y a 2 secondes.</span></div>
  <div class="box set-group">
    {tline("Lire la draft depuis le client League", "Les panneaux s'ouvrent au bon moment et les adversaires se remplissent seuls. Lecture seule : rien n'est modifié dans le client. Riot n'a pas confirmé que c'est autorisé : c'est ton choix.", True)}
    {sline("Quand League t'attribue un autre rôle", "Seulement avec la lecture du client. Rien n'est déduit de ton champion.", sel(["Me proposer de changer", "Changer de rôle joué", "Ne rien faire"], "Quand League t'attribue un autre rôle"))}
  </div>
  <div class="box set-group">
    <h3>Sans lecture du client</h3>
    <p class="small muted" style="padding-bottom:8px">DRAFT estime la draft à partir de l'image. Si les panneaux ne viennent pas, ou viennent à tort, apprends-lui l'écran depuis le menu de l'icône, pendant que le client est visible : « Détection de la draft », puis « C'est une draft » ou « Ce n'est pas une draft ».</p>
  </div>
  <div class="box set-group">
    {sline("Diagnostic de connexion", "Un texte à copier dans un message d'aide. Aucune donnée personnelle.", '<a class="btn btn-secondary" href="#diagnostic">Ouvrir le diagnostic</a>')}
  </div>
</section>
<section class="set-section" id="affichage" aria-labelledby="h-aff">
  <h2 id="h-aff">Affichage et fenêtres</h2><p class="lede">Où et quand les panneaux de draft s'affichent.</p>
  <div class="box set-group">
    {tline("Afficher les panneaux automatiquement pendant la draft", "Sinon, Ctrl+Alt+D ou le menu de l'icône les affiche.", True)}
    {tline("Coller les panneaux au client League", "Ils suivent le client quand tu le déplaces. Sans place autour, ils vont sur un autre écran.", True)}
    {tline("Un seul panneau", "Composition adverse et conseil réunis. DRAFT y passe seul quand la place manque.", False)}
    {tline("Toujours au premier plan", "Les panneaux restent visibles au-dessus des autres fenêtres.", True)}
  </div>
  <div class="box set-group">
    {sline("Replacer les panneaux", "Les remet à côté du client et réactive le collage.", '<button class="btn btn-secondary" type="button">Replacer</button>')}
    {sline("Raccourci clavier", "Affiche ou masque les panneaux, partout dans Windows.", '<span><kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>D</kbd></span>')}
  </div>
</section>
<section class="set-section" id="donnees" aria-labelledby="h-don">
  <h2 id="h-don">Données et confidentialité</h2><p class="lede">Ce que DRAFT calcule, ce qu'il garde, et ce qu'il ne garde jamais.</p>
  <div class="box set-group">
    <h3>Calculs avec ta clé Riot</h3>
    {sline("Rôles des champions", "Calculés le 03/11/2026 sur 2 000 parties récentes. Servent à deviner le mid adverse. Environ 40 minutes.", '<button class="btn btn-secondary" type="button">Mettre à jour</button>')}
    {sline("Matchups du rôle Mid", "Calculés le 03/11/2026. Ahri : 1 640 parties, Fizz : 1 210, Kog'Maw : 85. Sous 100 parties, le matchup n'utilise pas le bilan. Environ 65 minutes.", '<button class="btn btn-secondary" type="button">Mettre à jour</button>')}
  </div>
  <div class="box set-group">
    <h3>Ce que DRAFT garde sur ce PC</h3>
    <ul class="small" style="padding-bottom:12px;display:grid;gap:6px">
      <li>Tes profils, ton journal de drafts et tes réglages, dans ton dossier Windows.</li>
      <li>Ta clé Riot : jamais. Tu la saisis pour un calcul ou un import, puis elle est oubliée.</li>
      <li>Aucun envoi automatique : DRAFT ne contacte Riot ou GitHub que lorsque tu le demandes. Les illustrations viennent de Data Dragon.</li>
    </ul>
  </div>
  <details class="box set-group"><summary style="padding:12px 0">Sources de données (6)</summary><p class="small muted" style="padding-bottom:12px">Fiches des champions écrites à la main, rôles, matchups, historique, illustrations. Pour chacune : date, patch, nombre de parties.</p></details>
</section>
<section class="set-section" id="mises-a-jour" aria-labelledby="h-maj">
  <h2 id="h-maj">Mises à jour</h2><p class="lede">DRAFT ne contacte GitHub que lorsque tu cliques.</p>
  <div class="box set-group">
    {sline("Version 3.0.0 bêta", "Vérifié aujourd'hui à 10:42 : DRAFT est à jour.", '<button class="btn btn-secondary" type="button">' + ico(I["refresh"]) + 'Rechercher une mise à jour</button>')}
    <p class="small muted" style="padding:0 0 12px">Pendant une draft, une partie, un import ou un calcul, l'installation attend. Si la nouvelle version ne démarre pas, DRAFT revient seul à la précédente. Tes données sont conservées.</p>
  </div>
</section>
<section class="set-section" id="diagnostic" aria-labelledby="h-dia">
  <h2 id="h-dia">Diagnostic et informations</h2><p class="lede">Pour comprendre un problème ou demander de l'aide.</p>
  <div class="box set-group">
    {sline("Diagnostic de connexion League", "Copie un texte court à coller dans ton message.", '<button class="btn btn-secondary" type="button">Copier le diagnostic</button>')}
    {tline("Journal de diagnostic de la détection", "Chiffres seulement, aucune image ni nom de joueur.", False)}
    <p class="small muted" style="padding:0 0 12px">État : client fermé. Placement : panneaux de chaque côté du client.</p>
  </div>
  <div class="box set-group">
    <h3>À propos</h3>
    <p class="small muted" style="padding-bottom:12px">DRAFT 3.0.0 bêta, ton assistant stratégique pour League of Legends. DRAFT n'est pas approuvé par Riot Games et ne reflète pas les opinions de Riot Games.</p>
  </div>
</section>'''
sett = f'''<header class="page-head"><div><h1>Réglages</h1><p>Six rubriques. Chaque changement est enregistré tout de suite.</p></div></header>
<div class="settings">
  <nav class="subnav" aria-label="Rubriques des réglages">{subnav}</nav>
  <div>{sections}</div>
</div>'''
page("reglages.html", "Réglages", "gear", sett)

# ------------------------------------------------------------------ Overlay
TITLE = lambda compact: f'''<header class="p-title"><span class="brand-s">{LOGO}DRAFT</span><span class="grow"></span>
  <button class="btn btn-icon" type="button" aria-label="{'Passer à deux panneaux' if compact else 'Passer au panneau unique'}" title="{'Deux panneaux' if compact else 'Panneau unique'}">{ico(I["two"] if compact else I["one"])}</button>
  <button class="btn btn-icon" type="button" aria-label="Réglages d'affichage" title="Réglages d'affichage">{ico(I["gear"])}</button>
  <button class="btn btn-icon" type="button" aria-label="Masquer les panneaux" title="Masquer (Ctrl+Alt+D)">{ico(I["x"])}</button></header>'''
ROLESW = f'<div class="p-role"><span class="now">Mid</span><div class="seg" role="group" aria-label="Rôle joué">{"".join(f"<button type=\"button\" aria-pressed=\"{str(r=="mid").lower()}\" aria-label=\"{RN[r]}\">{role(r)}</button>" for r in ROLE)}</div></div>'
def pslot(r, name=None, traits="", t=1, mine=False, em=""):
    if not name:
        return f'<div class="p-slot empty"><span class="lane">{role(r)}{RN[r]}</span><span class="ghost"></span><span class="who">Ajouter le {RN[r]}</span><span></span></div>'
    return f'<div class="p-slot{" mine" if mine else ""}"><span class="lane">{role(r)}{RN[r]}</span>{mono(name, "", t)}<span class="who"><b>{name}</b>{f"<em>{em}</em>" if em else ""}<span>{traits}</span></span><button class="btn btn-icon" type="button" aria-label="Retirer {name}" style="width:24px;min-height:24px">{ico(I["x"])}</button></div>'
pmeters = "".join(f'<div class="meter{" hot" if d else ""}" style="grid-template-columns:minmax(0,1fr) 100px"><span>{l}</span><span class="pips" role="img" aria-label="{l} : {c} sur 5">{"".join("<i class=\"f\"></i>" if i < c else "<i></i>" for i in range(5))}</span></div>' for l, c, d in meters)
LEFT_BODY = f'''<div class="p-body">
  <h2 class="p-sec">Composition adverse <small>4 sur 5</small></h2>
  <label class="search"><span class="sr">Ajouter un adversaire</span>{ico(I["search"])}<input class="input" type="search" placeholder="Adversaire : ahri, tf..."></label>
  <div>{pslot("top", "Ornn", "Tank, Engage", 3)}{pslot("jungle", "Lee Sin", "Bruiser, Plongeur", 5)}{pslot("mid", "Syndra", "Carry, Contrôle", 2, True, "Mid probable")}{pslot("adc", "Jinx", "Carry", 1)}{pslot("support")}</div>
  <h2 class="p-sec">Profil de l'équipe</h2>
  <div>{pmeters}</div>
</div>'''
RIGHT_BODY = f'''<div class="p-body">
  <p class="sr" aria-live="polite">Fizz conseillé.</p>
  <div class="cluster"><span class="tag net">Choix net</span><span class="tag">4/5 adversaires</span><span class="tag" title="Lecture du client active">{ico(I["plug"])}Lecture du client</span></div>
  <section class="p-reco" aria-label="Champion conseillé"><div class="art" data-label="Illustration"></div><div class="art-fade"></div><span class="tag solid" style="justify-self:start;width:max-content">Recommandé</span><p class="p-name">Fizz</p></section>
  <button class="p-alt" type="button">{mono("Ahri", "", 2)}<span><small>Alternative</small><b>Ahri</b></span><span class="muted xs">Comparer</span></button>
  <ul class="reasons"><li>1er choix contre Syndra</li><li>2 cibles fragiles : Syndra, Jinx</li></ul>
  <div class="danger">{mono("Syndra", "s32", 2)}<div><b>Danger : Syndra</b><p>Son contrôle ciblé peut stopper ton plongeon.</p></div></div>
  <details class="p-more"><summary>Plan de jeu et ban</summary></details>
</div>
<div class="p-actions"><button class="btn btn-primary" type="button">Je joue Fizz</button><button class="btn btn-secondary" type="button">{ico(I["expand"])}Détails</button></div>'''
ov2 = head("Overlay, deux panneaux", "ov-page") + f'''<p class="ov-flag">Maquette non validée. Données d'exemple. Taille réelle.</p><div class="ov-row"><div class="panel" aria-label="Panneau gauche : composition adverse">{TITLE(False)}{LEFT_BODY}</div>
<div class="panel" aria-label="Panneau droit : conseil">{TITLE(False)}{ROLESW}{RIGHT_BODY}</div></div>
{FLAG}
</body>
</html>
'''
(OUT / "overlay-deux-panneaux.html").write_text(ov2, encoding="utf-8")
SINGLE_BODY = f'''<div class="p-body">
  <label class="search"><span class="sr">Ajouter un adversaire</span>{ico(I["search"])}<input class="input" type="search" placeholder="Adversaire : ahri, tf..."></label>
  <div class="cluster">{"".join(f'<span class="chip{" sel" if n=="Syndra" else ""}">{mono(n, "s24", t)}{n}</span>' for n, t in [("Ornn", 3), ("Lee Sin", 5), ("Syndra", 2), ("Jinx", 1)])}<span class="chip add">Support ?</span></div>
  <div class="cluster"><span class="tag net">Choix net</span><span class="tag">4/5 adversaires</span></div>
  <section class="p-reco" aria-label="Champion conseillé" style="min-height:96px"><div class="art" data-label="Illustration"></div><div class="art-fade"></div><span class="tag solid" style="width:max-content">Recommandé</span><p class="p-name">Fizz</p></section>
  <button class="p-alt" type="button">{mono("Ahri", "", 2)}<span><small>Alternative</small><b>Ahri</b></span><span class="muted xs">Comparer</span></button>
  <ul class="reasons"><li>1er choix contre Syndra</li><li>2 cibles fragiles : Syndra, Jinx</li></ul>
  <div class="danger">{mono("Syndra", "s32", 2)}<div><b>Danger : Syndra</b><p>Son contrôle ciblé peut stopper ton plongeon.</p></div></div>
  <details class="p-more"><summary>Plan de jeu et ban</summary></details>
</div>
<div class="p-actions"><button class="btn btn-primary" type="button">Je joue Fizz</button><button class="btn btn-secondary" type="button">{ico(I["expand"])}Détails</button></div>'''
ov1 = head("Overlay, panneau unique", "ov-page") + f'''<p class="ov-flag">Maquette non validée. Données d'exemple. Taille réelle.</p><div class="panel single" aria-label="Panneau unique">{TITLE(True)}{ROLESW}{SINGLE_BODY}</div>
{FLAG}
</body>
</html>
'''
(OUT / "overlay-panneau-unique.html").write_text(ov1, encoding="utf-8")

# États de l'overlay
def mini(label, body, foot=""):
    return f'<figure style="display:grid;gap:8px"><div class="panel" style="height:520px">{TITLE(False)}{body}{foot}</div><figcaption class="small muted" style="max-width:340px">{label}</figcaption></figure>'
st1 = mini("Sans profil : une seule action, vers Mon pool.", f'<div class="p-body"><div class="empty"><h3>Aucun champion pour l\'instant</h3><p>Choisis ton rôle et tes champions pour obtenir un conseil.</p><button class="btn btn-secondary" type="button">Ouvrir Mon pool</button></div></div>')
st2 = mini("Lecture du client désactivée : la détection est estimée, les adversaires se saisissent à la main.", f'<div class="p-body"><div class="band band-info" style="align-items:flex-start">{ico(I["info"])}<span><b style="display:block">Draft estimée</b><span class="muted">Saisis les adversaires au fil des picks.</span></span></div><label class="search"><span class="sr">Ajouter un adversaire</span>{ico(I["search"])}<input class="input is-focus" type="search" value="syn"></label><div class="suggest"><span class="chip sel">{mono("Syndra", "s24", 2)}Syndra</span><span class="chip add">{mono("Sylas", "s24", 3)}Sylas</span></div><p class="hint">Entrée ajoute. Retour arrière retire le dernier.</p></div>')
st3 = mini("Fin de draft : le résultat se note ici, puis dans Mes statistiques.", f'<div class="p-body"><p class="p-sec">Dernière partie : Fizz</p><p class="small muted">Note le résultat pour suivre tes victoires par champion.</p><div class="p-results"><button class="btn btn-secondary" type="button">Victoire</button><button class="btn btn-secondary" type="button">Défaite</button><button class="btn btn-secondary" type="button">Ignorer</button></div></div>')
toast = f'<div class="toast" role="status" style="position:absolute;left:24px;bottom:24px">{ico(I["eye"])}<span class="grow">Panneaux masqués. <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>D</kbd> pour les rappeler.</span></div>'
ovs = head("Overlay, états", "") + f'''<main style="position:relative;display:flex;gap:24px;padding:24px;min-height:100vh;background:#05080c">{st1}{st2}{st3}{toast}</main>
{FLAG}
</body>
</html>
'''
(OUT / "overlay-etats.html").write_text(ovs, encoding="utf-8")

# ------------------------------------------------------------------ Menu de l'icône
menu = head("Menu de l'icône") + f'''<main style="position:relative;min-height:100vh;padding:24px;background:#05080c">
<style></style>
<div class="box" role="menu" aria-label="Menu de DRAFT" style="position:absolute;left:24px;top:24px;width:300px;padding:6px;box-shadow:var(--shadow-float);border-color:var(--line-strong)">
  <p class="small muted" style="display:flex;align-items:center;gap:8px;padding:8px 10px"><i class="dot good"></i>DRAFT : connecté, aucune draft</p>
  <div class="nav-sep" style="margin:4px 0"></div>
  <a class="menu-item" role="menuitem" href="accueil.html"><b style="font-weight:600">Ouvrir DRAFT</b></a>
  <a class="menu-item" role="menuitem" href="#">Afficher les panneaux<span class="count">Ctrl+Alt+D</span></a>
  <a class="menu-item" role="menuitem" href="simulateur.html">Simuler une draft</a>
  <a class="menu-item" role="menuitem" href="#" style="background:var(--hover)">Détection de la draft<span class="count">{ico(I["chev"])}</span></a>
  <div class="nav-sep" style="margin:4px 0"></div>
  <a class="menu-item" role="menuitem" href="reglages.html">Réglages</a>
  <a class="menu-item" role="menuitem" href="#">Rechercher une mise à jour</a>
  <div class="nav-sep" style="margin:4px 0"></div>
  <a class="menu-item" role="menuitem" href="#">Quitter</a>
</div>
<div class="box" role="menu" aria-label="Détection de la draft" style="position:absolute;left:316px;top:150px;width:300px;padding:6px;box-shadow:var(--shadow-float);border-color:var(--line-strong)">
  <a class="menu-item" role="menuitem" href="#">C'est une draft : retenir cet écran</a>
  <a class="menu-item" role="menuitem" href="#">Ce n'est pas une draft : retenir cet écran</a>
  <div class="nav-sep" style="margin:4px 0"></div>
  <a class="menu-item" role="menuitem" href="#">Copier le diagnostic de connexion</a>
</div>
<div class="toast" role="status" style="position:absolute;left:24px;top:400px;width:592px">{ico(I["check"])}<span class="grow"><b style="display:block">DRAFT est à jour</b><span class="muted">Version 3.0.0 bêta. Résultat aussi visible sur l'Accueil.</span></span></div>
<p class="small muted" style="position:absolute;left:24px;top:490px;max-width:592px">Huit entrées, état compris. Double-clic sur l'icône : ouvrir DRAFT. Le menu natif garde le thème de Windows ; la maquette montre l'ordre et les libellés, pas le rendu exact.</p>
</main>
{FLAG}
</body>
</html>
'''.replace("<style></style>\n", "")
(OUT / "menu-icone.html").write_text(menu, encoding="utf-8")

# ------------------------------------------------------------------ Dialogue de clé (natif, harmonisé)
dlg = head("Dialogue de clé Riot") + f'''<main style="min-height:100vh;padding:24px;background:#05080c">
<div class="box" style="width:520px;box-shadow:var(--shadow-float);border-color:var(--line-strong);overflow:hidden">
  <div style="display:flex;align-items:center;gap:8px;height:34px;padding:0 12px;background:#0d131b;border-bottom:1px solid var(--line);font-size:var(--fs-sm)">{LOGO.replace("<svg", '<svg style="width:16px;height:16px;fill:none;stroke:var(--accent);stroke-width:2.2"')}DRAFT : clé Riot<span style="margin-left:auto" class="muted">{ico(I["x"])}</span></div>
  <div style="padding:20px 24px" class="stack-4">
    <div><p class="h2">Importer mon historique Riot</p><p class="small muted" style="margin-top:4px">DRAFT lit tes dernières parties pour afficher ton expérience. Le classement ne change pas.</p></div>
    <div class="band band-info small">{ico(I["clock"])}<span>100 parties, environ 3 minutes. Tu peux continuer à utiliser DRAFT.</span></div>
    <label class="field"><span>Comptes (Nom#TAG, un par ligne)</span><input class="input" value="Exemple#EUW"></label>
    <label class="field"><span>Clé Riot <a class="btn-link btn xs" href="#">Obtenir une clé</a></span><input class="input is-focus" value="••••••••••••••••••••••••"><span class="hint">Utilisée pour cet import, puis oubliée. Jamais enregistrée, jamais envoyée ailleurs qu'à Riot.</span></label>
    <div class="grid-2"><label class="field"><span>Région</span>{sel(["Europe de l'Ouest", "Europe du Nord et de l'Est", "Amérique du Nord", "Corée"], "Région")}</label><label class="field"><span>Parties</span>{sel(["100", "200", "50"], "Parties")}</label></div>
    <label class="toggle" style="padding:0"><span class="check on" aria-hidden="true">{ico(I["check"])}</span><span class="txt"><b>J'accepte que DRAFT lise ces parties avec ma clé</b><span>Rien n'est écrit chez Riot. Tu peux arrêter l'import à tout moment.</span></span></label>
  </div>
  <div style="display:flex;justify-content:flex-end;gap:8px;padding:12px 24px;border-top:1px solid var(--line);background:#0d131b"><button class="btn btn-secondary" type="button">Annuler</button><button class="btn btn-primary" type="button">Importer</button></div>
</div>
<p class="small muted" style="margin-top:12px;max-width:520px">Même dialogue pour les trois usages : historique, rôles, matchups. Seuls le titre, la phrase « ce qui sera lancé » et la durée changent. Fenêtre native (la clé ne passe jamais par la page), barre de titre sombre.</p>
</main>
{FLAG}
</body>
</html>
'''
(OUT / "dialogue-cle-riot.html").write_text(dlg, encoding="utf-8")

# ------------------------------------------------------------------ Planche de composants
comp = head("Composants") + f'''<main class="page wide stack-5" style="margin:0">
<header class="page-head"><div><h1>Composants DRAFT 3.0</h1><p>États au repos, survol, focus, désactivé. Voir docs/ux/COMPOSANTS-V3.md.</p></div></header>
<section class="box box-pad stack-3"><h2 class="h2">Boutons</h2>
  <div class="cluster"><button class="btn btn-primary" type="button">Je joue Fizz</button><button class="btn btn-primary is-hover" type="button">Survol</button><button class="btn btn-primary is-focus" type="button">Focus</button><button class="btn btn-primary is-disabled" type="button" disabled>Désactivé</button><button class="btn is-done" type="button">{ico(I["check"])}Fizz noté</button></div>
  <div class="cluster"><button class="btn btn-secondary" type="button">Exporter mes profils</button><button class="btn btn-secondary is-hover" type="button">Survol</button><button class="btn btn-secondary is-focus" type="button">Focus</button><button class="btn btn-secondary is-disabled" type="button" disabled>Désactivé</button><button class="btn btn-danger" type="button">Effacer la simulation</button><button class="btn btn-sm btn-secondary" type="button">Petit</button></div>
  <div class="cluster"><a class="btn btn-link" href="#">Comment DRAFT classe tes picks</a><button class="btn btn-icon" type="button" aria-label="Réglages d'affichage">{ico(I["gear"])}</button><button class="btn btn-icon is-hover" type="button" aria-label="Masquer">{ico(I["x"])}</button><button class="btn btn-icon is-focus" type="button" aria-label="Deux panneaux">{ico(I["two"])}</button></div>
</section>
<section class="grid-2">
  <div class="box box-pad stack-3"><h2 class="h2">Champs</h2>
    <label class="search"><span class="sr">Recherche</span>{ico(I["search"])}<input class="input" type="search" placeholder="Ajouter un champion : ahri, tf, kata..."></label>
    <label class="search"><span class="sr">Recherche active</span>{ico(I["search"])}<input class="input is-focus" type="search" value="kog"><kbd>Entrée</kbd></label>
    <label class="field"><span>Désactivé, avec la raison</span><input class="input is-disabled" disabled placeholder="Choisis d'abord un rôle"></label>
    <p class="hint err">{ico(I["alert"]).replace('class="i"', 'class="i" style="width:14px;height:14px;vertical-align:-2px"')} Aucun champion trouvé pour « zzz ». Vérifie l'orthographe.</p>
    <div class="cluster"><div class="seg" role="group" aria-label="Exemple"><button type="button" aria-pressed="true">Lane</button><button type="button" aria-pressed="false">Mid game</button></div><div class="seg gold" role="group" aria-label="Source"><button type="button" aria-pressed="true">Simulation</button><button type="button" aria-pressed="false">Draft en cours</button></div></div>
    {tline("Interrupteur actif", "Texte d'aide en dessous.", True)}{tline("Interrupteur inactif", "Même structure.", False)}
  </div>
  <div class="box box-pad stack-3"><h2 class="h2">Navigation</h2>
    <div style="width:220px;padding:8px;background:var(--surface);border:1px solid var(--line);border-radius:10px"><a class="nav-link" href="#" aria-current="page" style="margin-left:4px">{ico(I["home"])}Accueil</a><a class="nav-link" href="#" style="margin-left:4px">{ico(I["pool"])}Mon pool</a></div>
    <div class="tabs" role="tablist" aria-label="Exemple"><button role="tab" aria-selected="true" type="button">Plan</button><button role="tab" aria-selected="false" type="button">Critères</button><button role="tab" aria-selected="false" type="button">Comparer</button></div>
    <div class="cluster"><span class="tag net">Choix net</span><span class="tag close">Choix serré</span><span class="tag bad">À éviter</span><span class="tag">4/5 adversaires</span><span class="tag solid">Recommandé</span><span class="played">Joué</span></div>
    <div class="cluster"><span class="chip">{mono("Ahri", "s24", 2)}Ahri<span class="x">{ico(I["x"]).replace('class="i"', 'class="i" style="width:14px;height:14px"')}</span></span><span class="chip sel">{mono("Fizz", "s24", 1)}Fizz</span><span class="chip add">{mono("Kog'Maw", "s24", 4)}Kog'Maw</span></div>
  </div>
</section>
<section class="grid-3">
  <div class="stack-3"><h2 class="h2">Bandeaux d'état</h2>
    <div class="band band-live"><i class="dot good"></i><b>Draft en cours</b><span>3 adversaires lus.</span></div>
    <div class="band band-sim">{ico(I["info"])}<b>Simulation</b><span>Ta draft n'est pas modifiée.</span></div>
    <div class="band band-warn">{ico(I["alert"])}<b>Connexion impossible</b><span>Client ouvert, lecture refusée.</span></div>
  </div>
  <div class="stack-3"><h2 class="h2">Retours</h2>
    <div class="toast" role="status">{ico(I["check"])}<span class="grow">Draft effacée.</span><button class="btn btn-link" type="button">Annuler</button></div>
    <div class="toast err" role="alert">{ico(I["alert"])}<span class="grow">Ce fichier n'est pas un import DRAFT valide : rien n'a été modifié.</span></div>
    <p class="saved">{ico(I["check"])}Enregistré</p>
  </div>
  <div class="stack-3"><h2 class="h2">Chargement</h2>
    <div class="box box-pad stack-2"><p class="small">Import de l'historique : 46 sur 100 parties</p><div class="progress" role="progressbar" aria-valuenow="46" aria-valuemin="0" aria-valuemax="100" aria-label="Import"><i style="width:46%"></i></div><button class="btn btn-sm btn-secondary" type="button">Arrêter l'import</button></div>
    <div class="box box-pad stack-2" aria-hidden="true"><span class="skel" style="width:60%"></span><span class="skel" style="width:90%"></span><span class="skel" style="width:40%"></span></div>
  </div>
</section>
<section class="grid-3">
  <div class="stack-3"><h2 class="h2">État vide</h2><div class="empty"><h3>Aucune partie importée</h3><p>Importe ton historique Riot pour voir ton expérience par champion.</p><button class="btn btn-secondary" type="button">{ico(I["key"])}Importer mon historique Riot</button></div></div>
  <div class="stack-3"><h2 class="h2">Erreur</h2><div class="error" role="alert"><b>L'import s'est arrêté</b><span>Riot a refusé la clé : elle a peut-être expiré (24 heures). Les 46 parties déjà lues sont conservées.</span><span><button class="btn btn-sm btn-secondary" type="button">Réessayer avec une autre clé</button></span></div></div>
  <div class="stack-3"><h2 class="h2">Classement</h2><div><div class="rank first">{mono("Fizz", "", 1)}<span class="who"><b>Fizz</b><span class="bar"><i style="width:92%"></i></span></span><span class="tag solid">Recommandé</span></div><div class="rank picked">{mono("Ahri", "", 2)}<span class="who"><b>Ahri</b><span class="bar"><i style="width:70%"></i></span></span><span class="tag">Choisi</span></div></div></div>
</section>
<section class="box box-pad" style="position:relative;min-height:240px"><h2 class="h2">Modale de confirmation</h2>
  <div style="position:absolute;inset:48px 0 0;display:grid;place-items:center;background:rgba(5,8,12,.72);border-radius:0 0 10px 10px"><div class="modal" role="alertdialog" aria-labelledby="mt"><h2 id="mt">Retirer Fizz du pool Mid ?</h2><p>Ton journal garde ses parties. Tu pourras l'ajouter de nouveau.</p><div class="actions"><button class="btn btn-secondary" type="button">Garder</button><button class="btn btn-danger" type="button">Retirer Fizz</button></div></div></div>
</section>
</main>
{FLAG}
</body>
</html>
'''
(OUT / "composants.html").write_text(comp, encoding="utf-8")

# ------------------------------------------------------------------ Sommaire
items = [("accueil.html", "Accueil"), ("accueil-draft-en-cours.html", "Accueil, draft en cours"), ("accueil-premier-lancement.html", "Accueil, premier lancement"), ("mon-pool.html", "Mon pool"), ("simulateur.html", "Simulateur"), ("statistiques.html", "Mes statistiques"), ("reglages.html", "Réglages (rubriques par #general, #connexion, #affichage, #donnees, #mises-a-jour, #diagnostic)"), ("overlay-deux-panneaux.html", "Overlay, deux panneaux (340 px chacun)"), ("overlay-panneau-unique.html", "Overlay, panneau unique (374 px)"), ("overlay-etats.html", "Overlay, états"), ("menu-icone.html", "Menu de l'icône"), ("dialogue-cle-riot.html", "Dialogue de clé Riot (natif, harmonisé)"), ("composants.html", "Planche de composants")]
idx = head("Sommaire") + f'''<main class="page stack-4">
<header class="page-head"><div><h1>Maquettes DRAFT 3.0 bêta</h1><p>Option B, navigation en cinq sections. Maquettes non validées, données d'exemple.</p></div></header>
<ul class="box rows">{"".join(f'<li><a class="btn-link btn" href="{h}">{t}</a></li>' for h, t in items)}</ul>
<p class="hint">Captures PNG : dossier captures/. Documents : docs/ux/NAVIGATION-V3.md, COMPOSANTS-V3.md, MIGRATION-V3.md, CRITERES-VALIDATION-V3.md.</p>
</main>
{FLAG}
</body>
</html>
'''
(OUT / "index.html").write_text(idx, encoding="utf-8")
print("ok")
