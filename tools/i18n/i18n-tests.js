// Banc du prototype i18n-runtime.js, ouvert par tools/i18n/run-i18n-tests.ps1 dans Chrome sans écran (copie sous %TEMP%).
// Aucun eval, aucun script en ligne : mêmes règles que la page de DRAFT.
(function () {
  "use strict";
  const res = { checks: [], plural: { fr: {}, en: {} } };
  const check = (ok, name) => res.checks.push({ ok: !!ok, name });
  const { t, tp, tx, fmtDate } = I18N;
  const lang = I18N.lang;
  res.lang = lang;
  if (lang === "fr") {
    check(t("advice.card.playButton", { champion: "Ahri" }) === "Je joue Ahri", "Variable nommée (fr)");
    check(tp("settings.journal.imported", 0) === "0 partie ajoutée à ton journal.", "Pluriel fr : 0 au singulier");
    check(tp("settings.journal.imported", 1) === "1 partie ajoutée à ton journal.", "Pluriel fr : 1");
    check(tp("settings.journal.imported", 3) === "3 parties ajoutées à ton journal.", "Pluriel fr : 3");
    check(tp("settings.journal.imported", 1.5) === "1,5 partie ajoutée à ton journal.", "Pluriel fr : 1,5 au singulier, virgule décimale");
    check(t("literal.braces") === "Les accolades { et } s'écrivent doublées.", "Accolades doublées");
    const parts = tx("advice.change.overtakes", { champion: { b: "Ahri" }, other: "Zed" });
    check(parts.length === 2 && parts[0].b === "Ahri" && parts[1] === " passe devant Zed", "tx : variable gardée comme élément (gras sans HTML dans le catalogue)");
    check(fmtDate("2026-11-11T12:00:00Z") === new Date("2026-11-11T12:00:00Z").toLocaleDateString("fr-FR"), "Date fr : identique au code actuel");
    check(document.documentElement.lang === "fr", "Attribut lang de la page");
  }
  if (lang === "en") {
    check(t("advice.card.playButton", { champion: "Ahri" }) === "Play Ahri", "Variable nommée (en)");
    check(tp("settings.journal.imported", 1) === "1 game added to your journal." && tp("settings.journal.imported", 0) === "0 games added to your journal.", "Pluriel en : 1 / 0");
    check(t("native.only.fr") === "Clé présente seulement en français (test du repli)", "Clé absente en anglais : repli sur le français");
    check(I18N.missing().some(m => m.includes("native.only.fr")), "Repli signalé en mode test");
    check(t("nexiste.pas") === "[nexiste.pas]", "Clé absente partout : [clé]");
    check(t("advice.card.playButton", {}) === "Play {champion}" && I18N.missing().some(m => m.includes("variable absente : champion")), "Variable absente : visible et signalée");
    check(fmtDate("2026-11-11T12:00:00Z") === "Nov 11, 2026", "Date en : sans ambiguïté");
    check(document.documentElement.lang === "en", "Attribut lang de la page");
  }
  if (lang === "qps") {
    check(/^\[.*\]$/.test(t("advice.card.badgeClear")), "Pseudo-langue : texte entre crochets");
    check(t("advice.card.playButton", { champion: "Ahri" }).includes("Ahri"), "Pseudo-langue : variables intactes");
  }
  // Table des catégories de pluriel d'Intl.PluralRules, comparée au programme (Strings.PluralCategory).
  const numbers = [0, 1, 1.5, 2, 3, 10, 21, 100, 101, 1000, 1000000, 2000000, 1000001, 0.5, 1.25];
  for (const code of ["fr", "en"]) { const pr = new Intl.PluralRules(code); for (const n of numbers) res.plural[code][String(n)] = pr.select(n); }
  document.getElementById("out").textContent = JSON.stringify(res);
})();
