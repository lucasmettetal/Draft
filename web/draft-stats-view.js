// Mes statistiques, tableau de bord (proposition A validée) : rendu seulement. Tous les nombres viennent de draft-stats-calc.js.
// Fichier chargé avant le script principal : les fonctions de la page (html, t, Portrait, loadJournal...) sont lues au moment du rendu.
// Écritures : uniquement par updateEntry (origine confirmée par le joueur) et setResult (résultat saisi). Rien n'est supprimé.
(function (root) {
  "use strict";
  const C = root.STATS_CALC;
  const MIN = C.MIN_GAMES;
  const ROLE_KEY = { top: "role.name.top", jungle: "role.name.jungle", mid: "role.name.mid", adc: "role.name.adc", support: "role.name.support", unknown: "stats.role.unknown" };
  const roleName = r => t(ROLE_KEY[r] || ROLE_KEY.unknown);
  const pct = rate => t("stats.percent", { value: Math.round(rate * 100) });
  const rec = b => t("stats.record", { wins: b.wins, losses: b.losses });
  const mins = sec => t("stats.minutes", { count: Math.round(sec / 60) });
  const TABS = ["overview", "journal", "history"];
  const TAB_KEY = { overview: "stats.tabs.overview", journal: "stats.tabs.journal", history: "stats.tabs.history" };

  function readAll() {
    const read = C.readHistory(root.DRAFT_HISTORY || null, root.DRAFT_STATS || null, toName);
    return { read, games: read.games, journal: C.journal(loadJournal()) };
  }

  // Barre de victoires au-dessus du seuil (repère à 50 %, neutre sous 50 %) ; jauge de crans en dessous.
  const WinBar = ({ b, name }) => b.games < MIN
    ? html`<${SampleGauge} count=${b.games} />`
    : html`<span className=${`sd-track${b.rate < .5 ? " under" : ""}`} role="img" aria-label=${t("stats.winbar.aria", { name, record: rec(b) })}>
        <i style=${{ width: `${(b.rate * 100).toFixed(1)}%` }}></i><b aria-hidden="true"></b></span>`;
  const RecRate = ({ b }) => html`<span className="sd-rec num"><b>${rec(b)}</b>${b.rate !== null ? html`, ${pct(b.rate)}` : null}</span>`;
  const Icon = ({ path, box }) => html`<svg viewBox=${box || "0 0 20 20"} aria-hidden="true"><path d=${path} fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" /></svg>`;
  const ICON_RIOT = "M4 16.5v-6M10 16.5v-12M16 16.5v-8";
  const ICON_DRAFT = "M4 20V9l6-5h10v5l-6 11z M9 15l6-6";

  // ---------- Historique Riot ----------
  function ImportButton() {
    if (!hostState.available) return null;
    return html`<button type="button" className="ghost-btn" data-test="stats-import-history" onClick=${() => hostSend({ cmd: "importHistory" })}>${hostState.historyRunning ? t("stats.history.stop") : hostState.historyHasMore ? t("stats.history.more") : t("stats.history.import")}</button>`;
  }
  function RiotHead({ read }) {
    const when = read.generated ? t("stats.riot.importedOn", { date: fmtDate(read.generated) }) : "";
    return html`<header className="sd-head">
      <span className="sd-mark"><${Icon} path=${ICON_RIOT} /></span>
      <div className="sd-titles"><h2>${t("stats.history.title")}</h2>
        <p>${t("stats.riot.source")}${when ? ` · ${when}` : ""}${read.source === "script" ? ` · ${t("stats.riot.legacy")}` : ""}</p></div>
      <${ImportButton} />
    </header>
    ${hostState.historyStatus ? html`<p className="small" role="status" data-test="stats-history-status">${nativeText(hostState.historyStatus)}</p>` : null}`;
  }
  // Valeur non mesurable : un trait, jamais un zéro qui passerait pour une mesure.
  const DASH = "–";
  const Kpi = ({ test, label, value, sub, extra }) => html`<div data-test=${test}><span>${label}</span> <b className="num">${value === DASH ? html`<span className="sd-dash">${DASH}</span>` : value}</b>${sub ? html` <small>${sub}</small>` : null}${extra || null}</div>`;
  function Hero({ s }) {
    const r = s.record;
    return html`<div className="sd-hero" data-test="stats-hero">
      <${Kpi} test="kpi-games" label=${t("stats.riot.kpi.games")} value=${r.games} />
      ${r.rate !== null
        ? html`<${Kpi} test="kpi-rate" label=${t("stats.riot.kpi.rate")} value=${pct(r.rate)} sub=${`${tp("stats.riot.kpi.rateOf", r.games)} · ${rec(r)}`} />`
        : html`<${Kpi} test="kpi-rate" label=${t("stats.riot.kpi.rate")} value=${rec(r)} sub=${t("stats.riot.kpi.noPercent", { min: MIN })} extra=${html`<${SampleGauge} count=${r.games} />`} />`}
      <${Kpi} test="kpi-kda" label=${t("stats.riot.kpi.kda")} value=${s.kda ? I18N.fmtDecimal(s.kda.ratio, 1) : DASH} sub=${s.kda ? (s.kda.deathless ? t("stats.riot.kpi.kdaDeathless") : t("stats.riot.kpi.kdaHow")) : ""} />
      <${Kpi} test="kpi-duration" label=${t("stats.riot.kpi.duration")} value=${s.duration ? mins(s.duration.meanSec) : DASH} sub=${s.duration ? t("stats.riot.kpi.durationHow") : ""} />
    </div>`;
  }
  // Lignes incomplètes : seulement les écarts non nuls, avec leur nombre.
  const QUALITY_KEYS = [["duplicates", "stats.quality.duplicates"], ["unreadable", "stats.quality.unreadable"], ["noResult", "stats.quality.noResult"], ["remakes", "stats.quality.remakes"],
    ["msConverted", "stats.quality.msConverted"], ["noDuration", "stats.quality.noDuration"], ["noTime", "stats.quality.noTime"], ["noPatch", "stats.quality.noPatch"],
    ["noKda", "stats.quality.noKda"], ["unknownRole", "stats.quality.unknownRole"], ["noId", "stats.quality.noId"]];
  function Sample({ s, read }) {
    const p = s.period, q = read.quality;
    const issues = QUALITY_KEYS.filter(([k]) => q[k] > 0);
    return html`<p className="stats-sample" data-test="stats-sample">
        ${p ? html`<b className="num">${p.from === p.to ? t("stats.sample.periodOne", { date: fmtDate(p.from) }) : t("stats.sample.period", { from: fmtDate(p.from), to: fmtDate(p.to) })}</b>` : null}
        <span className="num">${tp("stats.riot.sample.games", s.record.games)}</span>
        ${s.patches.count ? html`<span className="num">${tp("stats.riot.sample.patches", s.patches.count, { first: s.patches.first, last: s.patches.last })}</span>` : null}
        <span>${tp("stats.riot.sample.accounts", s.accounts)}</span>
        ${s.mainRole ? html`<span>${t("stats.riot.sample.mainRole", { role: roleName(s.mainRole) })}</span>` : null}
      </p>
      ${issues.length ? html`<p className="sd-quality" data-test="stats-quality"><span>${t("stats.quality.title")}</span>${issues.map(([k, key]) => html`<span key=${k} className="num">${tp(key, q[k])}</span>`)}</p>` : null}`;
  }

  // Répartition par rôle : couleurs de rôle de DRAFT, rôle inconnu écrit et neutre.
  function Roles({ games }) {
    const list = C.byRole(games).filter(r => r.games);
    return html`<section className="sd-panel" data-test="stats-roles" aria-labelledby="sd-roles-title">
      <h3 id="sd-roles-title">${t("stats.roles.title")}</h3>
      <div className="sd-split" role="img" aria-label=${list.map(r => `${roleName(r.role)} ${r.games}`).join(", ")}>
        ${list.map(r => html`<i key=${r.role} data-slot=${r.role === "unknown" ? null : r.role} className=${r.role === "unknown" ? "unknown" : ""} style=${{ flexGrow: r.games }}></i>`)}
      </div>
      <ul className="sd-roles">${list.map(r => html`<li key=${r.role} data-slot=${r.role === "unknown" ? null : r.role}>
        <span className="sd-role-name">${r.role === "unknown" ? html`<span className="sd-dot"></span>` : html`<span className="sd-role-icon"><${RoleIcon} role=${r.role} /></span>`}${roleName(r.role)}</span> <b className="num">${r.games}</b> <${RecRate} b=${r} /></li>`)}</ul>
    </section>`;
  }

  // Chronologie : frise secondaire repliée. Victoires au-dessus du trait, défaites au-dessous ; tranches de 20 avec la dernière.
  function Timeline({ games }) {
    const tl = C.timeline(games);
    if (!tl.points.length) return null;
    const a = tl.points[0].time, z = tl.points[tl.points.length - 1].time, span = Math.max(1, z - a), W = 1000;
    const x = tm => 2 + ((tm - a) / span) * (W - 4);
    return html`<details className="sd-panel sd-timeline" data-test="stats-timeline">
      <summary><span>${t("stats.timeline.title")}</span><small className="num">${tp("stats.timeline.games", tl.points.length)}</small></summary>
      <p className="sd-legend"><span><i className="win"></i>${t("journal.result.win")}</span><span><i className="loss"></i>${t("journal.result.loss")}</span></p>
      <svg viewBox=${`0 0 ${W} 64`} preserveAspectRatio="none" role="img" aria-label=${t("stats.timeline.aria", { games: tl.points.length, from: fmtDate(a), to: fmtDate(z) })}>
        <line x1="0" y1="32" x2=${W} y2="32" className="axis" />
        ${tl.points.map((p, i) => html`<rect key=${i} x=${x(p.time).toFixed(1)} y=${p.win ? 5 : 35} width="2.4" height="24" rx="1" className=${p.win ? "win" : "loss"} />`)}
      </svg>
      <p className="sd-axis num"><span>${fmtDate(a)}</span><span>${fmtDate(z)}</span></p>
      <ol className="sd-slices">${tl.slices.map(s => html`<li key=${s.from} className=${s.complete ? "" : "current"}>
        <span className="num">${t("stats.range", { from: s.from, to: s.to })}${s.complete ? "" : ` · ${t("stats.slices.current")}`}</span>
        <${WinBar} b=${s} name=${t("stats.range", { from: s.from, to: s.to })} /><${RecRate} b=${s} /></li>`)}</ol>
      <p className="sd-note">${t("stats.slices.note", { min: C.THRESHOLDS.trend })}${tl.undated ? ` ${tp("stats.timeline.undated", tl.undated)}` : ""}</p>
    </details>`;
  }

  // Champions : les plus joués mis en avant, puis une liste compacte ; la liste complète est dans l'onglet Historique Riot.
  const ChampMeta = ({ c }) => html`<span className="sd-meta">${c.mainRole ? html`<span className="sd-role-icon" data-slot=${c.mainRole}><${RoleIcon} role=${c.mainRole} /></span>` : null}${c.mainRole ? roleName(c.mainRole) : roleName("unknown")}</span>`;
  const champLabel = c => c.champ || t("stats.champions.unknown");
  const Kda = ({ k }) => k ? html`<span className="sd-kda num">${t("stats.champions.kda", { value: I18N.fmtDecimal(k.ratio, 1) })}${k.deathless ? html` <small>${t("stats.champions.deathless")}</small>` : null}</span>` : html`<span className="sd-kda">–</span>`;
  function ChampionRows({ list, max }) {
    return html`<table className="sd-table sd-champs"><thead><tr><th scope="col">${t("stats.champions.col.champion")}</th><th scope="col" className="num">${t("stats.riot.kpi.games")}</th><th scope="col">${t("stats.riot.kpi.rate")}</th><th scope="col"></th><th scope="col">KDA</th></tr></thead>
      <tbody>${list.map(c => html`<tr key=${c.champ || "?"}><th scope="row"><span className="sd-champ">${c.champ ? html`<span data-slot=${c.mainRole || null} className="sd-face"><${Portrait} name=${c.champ} size=${30} /></span>` : null}<span><b>${champLabel(c)}</b><${ChampMeta} c=${c} /></span></span></th>
        <td className="num sd-games"><span className="sd-vol"><i style=${{ width: `${(100 * c.games / max).toFixed(1)}%` }}></i></span>${c.games}</td>
        <td><${WinBar} b=${c} name=${champLabel(c)} /></td><td><${RecRate} b=${c} /></td><td><${Kda} k=${c.kda} /></td></tr>`)}</tbody></table>`;
  }
  function Champions({ games, onAll }) {
    const all = C.byChampion(games);
    if (!all.length) return null;
    const top = all.slice(0, 3), rest = all.slice(3, 8);
    return html`<section className="sd-block" data-test="stats-champions" aria-labelledby="sd-champs-title">
      <div className="sd-block-head"><h3 id="sd-champs-title">${t("stats.champions.title")}</h3><small>${t("stats.champions.sub")}</small></div>
      <div className="sd-podium">${top.map(c => html`<article key=${c.champ || "?"} className="sd-top">
        <div className="sd-top-id">${c.champ ? html`<span data-slot=${c.mainRole || null} className="sd-face big"><${Portrait} name=${c.champ} size=${56} /></span>` : null}
          <div><h4>${champLabel(c)}</h4><${ChampMeta} c=${c} /></div></div>
        <p className="sd-top-num"><b className="num">${c.games}</b><span>${tp("stats.champions.gamesUnit", c.games)}</span></p>
        <${WinBar} b=${c} name=${champLabel(c)} />
        <p className="sd-top-val"><${RecRate} b=${c} /> · <${Kda} k=${c.kda} /></p>
        ${c.rate === null ? html`<p className="sd-few">${t("stats.champions.few", { min: MIN })}</p>` : null}
      </article>`)}</div>
      ${rest.length ? html`<${ChampionRows} list=${rest} max=${all[0].games} />` : null}
      ${all.length > 3 ? html`<p className="sd-more"><button type="button" className="ghost-btn" data-test="stats-champions-all" onClick=${onAll}>${tp("stats.champions.all", all.length)}</button></p>` : null}
    </section>`;
  }

  // Vis-à-vis : mid seulement ; filtres par rôle et par champion joué ; aucun « favorable » ni « défavorable ».
  function Matchups({ games }) {
    const [role, setRole] = React.useState("mid");
    const [champ, setChamp] = React.useState("");
    const [all, setAll] = React.useState(false);
    const m = C.matchups(games, { role, champ: champ || null });
    const shown = all ? m.opponents : m.opponents.slice(0, 10);
    return html`<section className="sd-block" data-test="stats-matchups" aria-labelledby="sd-mu-title">
      <div className="sd-block-head"><h3 id="sd-mu-title">${t("stats.matchups.title")}</h3><small>${t("stats.matchups.sub")}</small></div>
      <div className="sd-filters">
        <div className="seg-tabs" role="group" aria-label=${t("stats.matchups.roleFilter")}>${C.ROLES.map(r => html`<button key=${r} type="button" className=${`seg-tab${role === r ? " on" : ""}`} aria-pressed=${role === r} data-test=${`matchups-role-${r}`} onClick=${() => { setRole(r); setChamp(""); setAll(false); }}>${roleName(r)}</button>`)}</div>
        ${m.available && m.played.length > 1 ? html`<label className="sd-select"><span>${t("stats.matchups.champFilter")}</span>
          <select value=${champ} data-test="matchups-champ" onChange=${e => { setChamp(e.target.value); setAll(false); }}>
            <option value="">${t("stats.matchups.allChampions")}</option>
            ${m.played.map(p => html`<option key=${p.name} value=${p.name}>${p.name || t("stats.champions.unknown")} (${p.games})</option>`)}
          </select></label>` : null}
      </div>
      ${!m.available ? html`<p className="empty-note" data-test="matchups-unavailable">${tp("stats.matchups.unavailable", m.games, { role: roleName(role) })}</p>`
        : !m.opponents.length ? html`<p className="empty-note">${t("stats.matchups.none")}</p>`
        : html`<table className="sd-table sd-mu"><thead><tr><th scope="col">${t("stats.matchups.col.opponent")}</th><th scope="col" className="num">${t("stats.matchups.col.games")}</th><th scope="col">${t("stats.riot.kpi.rate")}</th><th scope="col"></th><th scope="col">${t("stats.matchups.col.played")}</th></tr></thead>
          <tbody>${shown.map(o => html`<tr key=${o.name}><th scope="row"><span className="sd-champ"><span data-slot="mid" className="sd-face"><${Portrait} name=${o.name} size=${28} /></span><b>${o.name}</b></span></th>
            <td className="num">${o.games}</td><td><${WinBar} b=${o} name=${o.name} /></td><td><${RecRate} b=${o} /></td>
            <td className="sd-played">${o.champs.map(c => html`<span key=${c.name}>${c.name || t("stats.champions.unknown")} <b className="num">${c.games}</b></span>`)}<span className="sd-meta">${roleName("mid")}</span></td></tr>`)}</tbody></table>
          ${m.opponents.length > 10 ? html`<p className="sd-more"><button type="button" className="ghost-btn" data-test="matchups-all" onClick=${() => setAll(!all)}>${all ? t("stats.matchups.less") : tp("stats.matchups.allOpponents", m.opponents.length)}</button></p>` : null}`}
      <p className="sd-note">${t("stats.matchups.midOnly")} ${t("stats.matchups.noPercent", { min: MIN })}${m.available && m.withoutOpponent ? ` ${tp("stats.matchups.withoutOpponent", m.withoutOpponent)}` : ""}</p>
    </section>`;
  }

  // « Pas encore » : progression vers les seuils de l'étude, sans conclusion.
  function NotYet({ games, j }) {
    const p = C.progress(games, j);
    const line = (test, have, need, text) => html`<li key=${test} data-test=${test}>
      <span className="sd-prog" role="img" aria-label=${t("stats.notYet.aria", { have, need })}><i style=${{ width: `${Math.min(100, 100 * have / need).toFixed(0)}%` }}></i></span>
      <span className="num sd-prog-n">${t("stats.notYet.count", { have: Math.min(have, need), need })}</span><span>${text}</span></li>`;
    return html`<section className="sd-panel sd-notyet" data-test="stats-notyet" aria-labelledby="sd-ny-title">
      <h3 id="sd-ny-title">${t("stats.notYet.title")}</h3>
      <ul>
        ${line("notyet-champion", p.champion.have, p.champion.need, p.champion.subject ? t("stats.notYet.champion", { champion: p.champion.subject, min: p.champion.need }) : t("stats.notYet.championNone", { min: p.champion.need }))}
        ${line("notyet-trend", p.trend.have, p.trend.need, t("stats.notYet.trend", { role: roleName(p.trend.subject || "unknown"), min: p.trend.need }))}
        ${line("notyet-opponent", p.opponent.have, p.opponent.need, p.opponent.subject ? t("stats.notYet.opponent", { champion: p.opponent.subject, min: p.opponent.need }) : t("stats.notYet.opponentNone", { min: p.opponent.need }))}
        ${line("notyet-advice", p.advice.have, p.advice.need, t("stats.notYet.advice", { followed: p.advice.followed, other: p.advice.other, min: p.advice.need }))}
      </ul>
      <p className="sd-note">${t("stats.notYet.note")}</p>
    </section>`;
  }

  function RiotColumn({ data, onAll }) {
    const { read, games } = data;
    if (!games.length) return html`<section className="sd-riot" data-test="stats-history" aria-label=${t("stats.history.title")}>
      <${RiotHead} read=${read} />
      <p className="empty-note" data-test="stats-history-summary">${read.quality.read ? tp("stats.riot.noneUsable", read.quality.read) : t("stats.history.none")}</p>
      ${hostState.available ? null : html`<p className="small">${t("stats.history.app")}</p>`}
      <p className="small" data-test="stats-collect">${t("stats.history.collect")}</p>
    </section>`;
    const s = C.summary(read);
    return html`<section className="sd-riot" data-test="stats-history" aria-label=${t("stats.history.title")}>
      <${RiotHead} read=${read} />
      <${Hero} s=${s} />
      <${Sample} s=${s} read=${read} />
      <div className="sd-row"><${Roles} games=${games} /><${NotYet} games=${games} j=${data.journal} /></div>
      <${Timeline} games=${games} />
      <${Champions} games=${games} onAll=${onAll} />
      <${Matchups} games=${games} />
    </section>`;
  }

  // ---------- Journal DRAFT ----------
  const RESULT = { win: ["win", "journal.result.win"], loss: ["loss", "journal.result.loss"], skip: ["", "journal.status.ignored"] };
  const changeOrigin = (id, value) => { updateEntry(id, { userOrigin: value }); publish(null); };
  // Saisie : origine d'abord pour une entrée sans marque ; résultat seulement pour une draft réelle ou confirmée réelle.
  function EntryActions({ row }) {
    const e = row.entry;
    if (row.origin === "unknown") return html`<span className="sd-acts" data-test="journal-origin">
      <span className="badge-origin">${t("stats.journal.origin.unknown")}</span>
      <button type="button" className="mini-btn" data-test="origin-real" onClick=${() => changeOrigin(e.id, "real")}>${t("stats.journal.origin.markReal")}</button>
      <button type="button" className="mini-btn" data-test="origin-sim" onClick=${() => changeOrigin(e.id, "simulation")}>${t("stats.journal.origin.markSim")}</button></span>`;
    if (row.origin === "simulation") return html`<span className="sd-acts"><span className="badge-origin">${t("stats.journal.origin.simulation")}</span>
      <button type="button" className="mini-btn" data-test="origin-undo" onClick=${() => changeOrigin(e.id, undefined)}>${t("stats.journal.origin.undo")}</button></span>`;
    const r = RESULT[e.result];
    if (r) return html`<span className="sd-acts"><span className=${`result-tag ${r[0]}`}>${t(r[1])}</span></span>`;
    return html`<span className="sd-acts" data-test="journal-result">${["win", "loss", "skip"].map(k => html`<button key=${k} type="button" className="mini-btn" data-result=${k} onClick=${() => setResult(e.id, k)}>${t(k === "win" ? "journal.result.win" : k === "loss" ? "journal.result.loss" : "journal.result.skip")}</button>`)}</span>`;
  }
  const Followed = ({ row }) => html`<span className=${`sd-tag${row.followed ? " followed" : ""}`}>${t(row.followed ? "stats.journal.tag.followed" : "stats.journal.tag.other")}${row.tied ? ` · ${t("stats.journal.tag.tie")}` : ""}</span>`;
  function JournalRow({ row }) {
    const e = row.entry, same = e.picked === e.suggested;
    return html`<li className=${`sd-jrow${row.counted ? "" : " is-out"}`}>
      <span className="sd-pair">${e.suggested ? html`<span className="sd-face advice"><${Portrait} name=${label(e.suggested)} size=${30} /></span>` : null}
        ${!same && e.picked ? html`<span className="sd-arrow" aria-hidden="true">→</span><span className="sd-face"><${Portrait} name=${label(e.picked)} size=${30} /></span>` : null}</span>
      <span className="sd-jtext"><b>${e.mid ? t("stats.journal.row.playedVs", { champion: label(e.picked || "-"), opponent: e.mid }) : t("stats.journal.row.played", { champion: label(e.picked || "-") })}</b>
        <small>${t("stats.journal.row.advised", { champion: label(e.suggested || "-") })}</small></span>
      <span className="sd-jmeta"><span className="num">${fmtDate(e.date || e.id)}</span>${row.counted ? html`<${Followed} row=${row} />` : null}</span>
      <${EntryActions} row=${row} />
    </li>`;
  }
  function JournalNotes({ j }) {
    return html`${j.pending ? html`<p className="sd-info">${tp("stats.journal.pending", j.pending)}</p>` : null}
      ${j.unknown ? html`<p className="sd-info" data-test="journal-unknown">${tp("stats.journal.unknown", j.unknown)}</p>` : null}
      ${j.simulations ? html`<p className="small">${tp("stats.journal.simulations", j.simulations)}</p>` : null}`;
  }
  function JournalKpis({ j }) {
    const f = j.finished;
    return html`<div className="sd-hero sm">
      <${Kpi} test="kpi-drafts" label=${t("stats.kpi.drafts")} value=${j.drafts} />
      <${Kpi} test="kpi-finished" label=${t("stats.journal.kpi.finished")} value=${f.games} sub=${f.games ? `${rec(f)}${f.rate !== null ? `, ${pct(f.rate)}` : ""}` : t("stats.journal.kpi.finishedHow")} extra=${f.games < MIN ? html`<${SampleGauge} count=${f.games} />` : null} />
      <${Kpi} test="kpi-followed" label=${t("stats.journal.kpi.followed")} value=${f.games ? j.followed.games : DASH} sub=${f.games ? tp("stats.journal.kpi.followedOf", f.games) : t("stats.journal.kpi.followedNone")} />
    </div>`;
  }
  function JournalPanel({ j, onAll }) {
    const head = html`<header className="sd-head"><span className="sd-mark"><${Icon} path=${ICON_DRAFT} box="0 0 24 24" /></span><div className="sd-titles"><h2>${t("stats.journal.heading")}</h2><p>${t("stats.journal.source")}</p></div></header>`;
    if (!j.total) return html`<section className="sd-journal box" data-test="stats-journal">${head}<p className="empty-note">${t("stats.overview.emptyJournal")}</p></section>`;
    return html`<section className="sd-journal box" data-test="stats-journal">${head}
      <${JournalKpis} j=${j} /><${JournalNotes} j=${j} />
      <h3>${t("stats.recent.title")}</h3>
      <ul className="sd-jlist" data-test="stats-recent">${j.entries.slice(0, 5).map(r => html`<${JournalRow} key=${r.entry.id} row=${r} />`)}</ul>
      <p className="sd-more"><button type="button" className="ghost-btn" data-test="stats-all-journal" onClick=${onAll}>${t("stats.recent.all")}</button></p>
      <p className="sd-note">${t("stats.sources.separate")}</p>
    </section>`;
  }
  function JournalCompare({ j }) {
    const s = j.followed, o = j.other;
    if (s.games < MIN || o.games < MIN) return html`<p className="small" data-test="journal-compare">${t("stats.journal.compare.notEnough", { min: MIN, followed: s.games, other: o.games })}</p>`;
    return html`<div className="sd-cmp" data-test="journal-compare">${[["stats.journal.tag.followed", s], ["stats.journal.tag.other", o]].map(([k, b]) => html`<p key=${k}><span>${t(k)}</span><${WinBar} b=${b} name=${t(k)} /><${RecRate} b=${b} /></p>`)}
      <p className="sd-note">${t("stats.journal.compare.warning")}</p></div>`;
  }
  // Onglet Journal : toutes les entrées, origine, saisie, comparaison du conseil, import et export.
  function JournalFull() {
    const j = C.journal(loadJournal());
    return html`<section className="box box-pad sd-journal-full" data-test="stats-journal-full">
      <h2 className="box-title">${t("stats.journal.title")}</h2>
      ${j.total ? html`<${JournalKpis} j=${j} /><${JournalNotes} j=${j} />
        <h3>${t("stats.journal.compare.title")}</h3><${JournalCompare} j=${j} />
        <p className="small">${t("stats.journal.followedDef")} ${t("stats.journal.tieNote")}</p>
        <h3>${t("stats.journal.all")}</h3>
        <ul className="sd-jlist full" data-test="journal-table">${j.entries.map(r => html`<${JournalRow} key=${r.entry.id} row=${r} />`)}</ul>
        <p className="sd-more"><button type="button" className="ghost-btn" onClick=${exportJournal}>${t("journal.download")}</button></p>`
        : html`<p className="empty-note">${t("stats.overview.emptyJournal")}</p>`}
      <${JournalImport} />
    </section>`;
  }

  // ---------- Onglet Historique Riot : import, liste complète des champions, anciens imports ----------
  function HistoryTab() {
    const { read, games } = readAll();
    const all = C.byChampion(games);
    return html`<div className="sd-history">
      <section className="box box-pad" data-test="stats-history">
        <h2 className="box-title">${t("stats.history.title")}</h2>
        <p className=${games.length ? "stats-line ok" : "empty-note"} data-test="stats-history-summary">${read.source ? tp("stats.history.imported", games.length, { date: read.generated ? fmtDate(read.generated) : "?" }) : t("stats.history.none")}</p>
        ${hostState.historyStatus ? html`<p className="small" role="status" data-test="stats-history-status">${nativeText(hostState.historyStatus)}</p>` : null}
        ${hostState.available ? html`<p className="set-row"><${ImportButton} /></p>` : html`<p className="small">${t("stats.history.app")}</p>`}
        <p className="small">${t("stats.history.key")}</p>
        <p className="small" data-test="stats-collect">${t("stats.history.collect")}</p>
      </section>
      ${all.length ? html`<section className="box box-pad" data-test="stats-champion-table"><h2 className="box-title">${t("stats.champions.titleAll")} <small>${tp("stats.champions.count", all.length)}</small></h2>
        <${ChampionRows} list=${all} max=${all[0].games} /></section>` : null}
      <section className="box box-pad" data-test="stats-old"><h2 className="box-title">${t("stats.old.title")}</h2><${StatsTab} />
        ${hostState.available ? html`<p className="set-row"><button type="button" className="ghost-btn" data-test="import-stats" onClick=${() => hostSend({ cmd: "importStats" })}>${t("stats.oldFile.stats")}</button></p>` : null}</section>
    </div>`;
  }

  // ---------- Vue d'ensemble ----------
  function EmptySteps() {
    return html`<section className="sd-empty" data-test="stats-empty">
      <h2>${t("stats.empty.title")}</h2><p>${t("stats.empty.lead")}</p>
      <ol>
        <li><b>${t("stats.history.title")}</b><p>${t("stats.empty.riot")}</p><p className="small">${t("stats.history.collect")}</p>
          ${hostState.available ? html`<${ImportButton} />` : html`<p className="small">${t("stats.history.app")}</p>`}
          ${hostState.historyStatus ? html`<p className="small" role="status" data-test="stats-history-status">${nativeText(hostState.historyStatus)}</p>` : null}</li>
        <li><b>${t("stats.journal.heading")}</b><p>${t("stats.empty.journal")}</p></li>
      </ol>
      <p className="sd-note">${t("stats.sources.separate")}</p>
    </section>`;
  }
  function Dashboard({ onTab }) {
    const data = readAll();
    if (!data.games.length && !data.journal.total && !data.read.quality.read) return html`<${EmptySteps} />`;
    return html`<div className=${`sd-board${data.games.length ? "" : " no-riot"}`} data-test="stats-dashboard">
      <${RiotColumn} data=${data} onAll=${() => onTab("history")} />
      <${JournalPanel} j=${data.journal} onAll=${() => onTab("journal")} />
    </div>`;
  }

  function StatsView() {
    const [tab, setTab] = React.useState(() => { const s = new URLSearchParams(location.search).get("section"); return TABS.includes(s) ? s : "overview"; });
    return html`<div className="view-page sd-page">
      <${ViewHead} title=${t("nav.stats")} sub=${t("stats.sub")} />
      <div className="stats-tabs" role="tablist" aria-label=${t("nav.stats")}>
        ${TABS.map(k => html`<button key=${k} type="button" role="tab" aria-selected=${tab === k} data-test=${`stats-tab-${k}`} className=${`stats-tab${tab === k ? " on" : ""}`} onClick=${() => setTab(k)}>${t(TAB_KEY[k])}</button>`)}
      </div>
      ${tab === "overview" ? html`<${Dashboard} onTab=${setTab} />` : null}
      ${tab === "journal" ? html`<${JournalFull} />` : null}
      ${tab === "history" ? html`<${HistoryTab} />` : null}
    </div>`;
  }

  root.DraftStatsView = Object.freeze({ StatsView });
})(window);
