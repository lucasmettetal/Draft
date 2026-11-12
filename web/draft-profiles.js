// Profils par rôle, export et import de configuration, décision sur le rôle attribué par le client.
// Logique pure, testée à part. Lit le moteur (NAMES, label, pickId) sans le modifier.
//
// L'ancienne clé draftProfile reste à jour avec le rôle actif : une version plus ancienne relancée retrouve son profil.

const PROFILES_KEY = "draftProfiles", PROFILES_BACKUP_KEY = "draftProfilesBackup", PROFILES_CORRUPT_KEY = "draftProfilesCorrupt";
const PROFILES_VERSION = 1, PROFILES_BACKUPS = 3;
const CONFIG_FORMAT = "draft-config", CONFIG_MAX_SIZE = 256 * 1024, POOL_MAX = 60;
const PROFILE_ROLES = ["top", "jungle", "mid", "adc", "support"];
const AUTO_ROLE_MODES = ["off", "suggest", "auto"];
// Seules ces préférences voyagent dans un export. Tout le reste est ignoré, à l'aller comme au retour.
const CONFIG_PREFS = {
  assumeFront: v => typeof v === "boolean",
  coachLevel: v => ["beginner", "intermediate", "advanced"].includes(v),
  autoRole: v => AUTO_ROLE_MODES.includes(v)
};

const knownPick = n => typeof n === "string" && NAMES.includes(label(pickId(n)));
const uniqueList = list => list.filter((x, i) => list.indexOf(x) === i);
const emptyRoleProfile = () => ({ pool:[], excluded:[], favorite:null });
function emptyProfiles() {
  const roles = {};
  PROFILE_ROLES.forEach(r => { roles[r] = emptyRoleProfile(); });
  return { version:PROFILES_VERSION, active:null, roles };
}

// Lecture tolérante du stockage local : un champion disparu du jeu est écarté sans perdre le reste.
function cleanRoleProfile(raw) {
  const out = emptyRoleProfile();
  if (!raw || typeof raw !== "object") return out;
  out.pool = uniqueList((Array.isArray(raw.pool) ? raw.pool : []).filter(knownPick).map(pickId));
  out.excluded = uniqueList((Array.isArray(raw.excluded) ? raw.excluded : []).filter(knownPick).map(pickId)).filter(p => out.pool.includes(p));
  out.favorite = knownPick(raw.favorite) && out.pool.includes(pickId(raw.favorite)) ? pickId(raw.favorite) : null;
  return out;
}
function normalizeProfiles(raw) {
  if (!raw || typeof raw !== "object" || Array.isArray(raw) || !raw.roles || typeof raw.roles !== "object") return null;
  if (typeof raw.version !== "number" || raw.version > PROFILES_VERSION) return null;
  const out = emptyProfiles();
  PROFILE_ROLES.forEach(r => { out.roles[r] = cleanRoleProfile(raw.roles[r]); });
  out.active = PROFILE_ROLES.includes(raw.active) ? raw.active : null;
  return out;
}

function readJson(storage, key) {
  let text;
  try { text = storage.getItem(key); } catch (e) { return { broken:true, text:"" }; }
  if (text === null || text === undefined) return { missing:true };
  try { return { value:JSON.parse(text), text }; } catch (e) { return { broken:true, text }; }
}

// Ordre : profils valides, dernière sauvegarde si le contenu est abîmé, puis ancien profil unique.
function loadProfiles(storage, prefs) {
  const read = readJson(storage, PROFILES_KEY);
  const found = read.missing || read.broken ? null : normalizeProfiles(read.value);
  if (found) return found;
  if (!read.missing) {
    // Un contenu abîmé est mis de côté, jamais effacé sans trace.
    try { storage.setItem(PROFILES_CORRUPT_KEY, String(read.text)); } catch (e) {}
    const backups = readJson(storage, PROFILES_BACKUP_KEY);
    if (Array.isArray(backups.value)) for (const b of backups.value) {
      const ok = normalizeProfiles(b && b.data);
      if (ok) { ok.recovered = true; try { persistProfiles(ok, storage); } catch (e) {} return ok; }
    }
  }
  const out = emptyProfiles();
  const old = readJson(storage, "draftProfile");
  if (old.value && PROFILE_ROLES.includes(old.value.role) && Array.isArray(old.value.pool)) {
    const excluded = prefs && Array.isArray(prefs.excluded) ? prefs.excluded : [];
    out.roles[old.value.role] = cleanRoleProfile({ pool:old.value.pool, excluded, favorite:prefs ? prefs.favorite : null });
    out.active = old.value.role;
    out.migrated = true;
    try { persistProfiles(out, storage); } catch (e) {}
  } else if (!read.missing) out.lost = true;
  return out;
}

function persistProfiles(profiles, storage) {
  storage.setItem(PROFILES_KEY, JSON.stringify({ version:PROFILES_VERSION, active:profiles.active, roles:profiles.roles }));
  // Pour une version plus ancienne : le rôle actif, seulement s'il a des champions.
  const active = profiles.active && profiles.roles[profiles.active];
  if (active && active.pool.length) storage.setItem("draftProfile", JSON.stringify({ role:profiles.active, pool:active.pool }));
  return true;
}

function backupProfiles(storage, reason, now) {
  const current = readJson(storage, PROFILES_KEY);
  if (current.missing || current.broken || !normalizeProfiles(current.value)) return false;
  const old = readJson(storage, PROFILES_BACKUP_KEY);
  const list = Array.isArray(old.value) ? old.value : [];
  list.unshift({ at:now || new Date().toISOString(), reason, data:current.value });
  storage.setItem(PROFILES_BACKUP_KEY, JSON.stringify(list.slice(0, PROFILES_BACKUPS)));
  return true;
}

function latestProfilesBackup(storage) {
  const old = readJson(storage, PROFILES_BACKUP_KEY);
  return Array.isArray(old.value) && old.value[0] && normalizeProfiles(old.value[0].data) ? old.value[0] : null;
}

function restoreLastBackup(storage) {
  const backup = latestProfilesBackup(storage);
  if (!backup) return null;
  const data = normalizeProfiles(backup.data);
  persistProfiles(data, storage);
  storage.setItem(PROFILES_BACKUP_KEY, JSON.stringify(readJson(storage, PROFILES_BACKUP_KEY).value.slice(1)));
  return data;
}

// ---------- Export ----------
// Liste fermée : ni clé, ni jeton, ni identifiant, ni historique, ni journal ne peuvent y entrer.
function exportConfig(profiles, prefs, appVersion, now) {
  const preferences = {};
  Object.keys(CONFIG_PREFS).forEach(k => { if (prefs && CONFIG_PREFS[k](prefs[k])) preferences[k] = prefs[k]; });
  const roles = {};
  PROFILE_ROLES.forEach(r => { roles[r] = cleanRoleProfile(profiles.roles[r]); });
  return JSON.stringify({ format:CONFIG_FORMAT, version:PROFILES_VERSION, exported:now || new Date().toISOString(), app:appVersion || "", active:profiles.active, roles, preferences }, null, 2);
}

// Noms de rôles tels que l'interface les affiche (messages d'import).
const PROFILE_ROLE_NAMES = { top:"Top", jungle:"Jungle", mid:"Mid", adc:"ADC", support:"Support" };
const profileRoleName = role => PROFILE_ROLE_NAMES[role] || role;

// ---------- Import : validation stricte, sans rien écrire ----------
function parseConfig(text) {
  const fail = error => ({ ok:false, error });
  if (typeof text !== "string" || !text.trim()) return fail(I18N.t("profiles.import.empty"));
  if (text.length > CONFIG_MAX_SIZE) return fail(I18N.t("profiles.import.tooBig"));
  let raw;
  try { raw = JSON.parse(text); } catch (e) { return fail(I18N.t("profiles.import.notJson")); }
  if (!raw || typeof raw !== "object" || Array.isArray(raw)) return fail(I18N.t("profiles.import.notConfig"));
  const warnings = [], roles = {};
  PROFILE_ROLES.forEach(r => { roles[r] = emptyRoleProfile(); });
  const readRole = (role, value) => {
    if (!value || typeof value !== "object" || Array.isArray(value)) return I18N.t("profiles.import.roleMalformed", { role: profileRoleName(role) });
    if (!Array.isArray(value.pool)) return I18N.t("profiles.import.roleNoPool", { role: profileRoleName(role) });
    if (value.pool.length > POOL_MAX) return I18N.t("profiles.import.roleTooMany", { role: profileRoleName(role) });
    const unknown = value.pool.filter(n => !knownPick(n));
    if (unknown.length) return I18N.t("profiles.import.unknownChampion", { role: profileRoleName(role), champions: unknown.slice(0, 3).map(String).join(", ") });
    if (value.excluded !== undefined && !Array.isArray(value.excluded)) return I18N.t("profiles.import.excludedMalformed", { role: profileRoleName(role) });
    const pool = uniqueList(value.pool.map(pickId));
    const excluded = uniqueList((value.excluded || []).filter(knownPick).map(pickId));
    roles[role] = { pool, excluded:excluded.filter(p => pool.includes(p)), favorite:knownPick(value.favorite) && pool.includes(pickId(value.favorite)) ? pickId(value.favorite) : null };
    if (value.favorite && !roles[role].favorite) warnings.push(I18N.t("profiles.import.favoriteIgnored", { role: profileRoleName(role) }));
    return null;
  };
  // Ancien format : le profil unique des versions précédentes.
  if (raw.format === undefined && raw.role !== undefined) {
    if (!PROFILE_ROLES.includes(raw.role)) return fail(I18N.t("profiles.import.legacyRoleUnknown"));
    const problem = readRole(raw.role, { pool:raw.pool });
    if (problem) return fail(problem);
    warnings.push(I18N.t("profiles.import.legacyFormat"));
    return { ok:true, roles, active:raw.role, preferences:{}, warnings, legacy:true };
  }
  if (raw.format !== CONFIG_FORMAT) return fail(I18N.t("profiles.import.notConfig"));
  if (typeof raw.version !== "number") return fail(I18N.t("profiles.import.noVersion"));
  if (raw.version > PROFILES_VERSION) return fail(I18N.t("profiles.import.tooRecent"));
  if (!raw.roles || typeof raw.roles !== "object" || Array.isArray(raw.roles)) return fail(I18N.t("profiles.import.noRoles"));
  const strange = Object.keys(raw.roles).filter(r => !PROFILE_ROLES.includes(r));
  if (strange.length) return fail(I18N.t("profiles.import.roleUnknown", { roles: strange.slice(0, 3).join(", ") }));
  for (const r of PROFILE_ROLES) {
    if (raw.roles[r] === undefined) { warnings.push(I18N.t("profiles.import.roleMissing", { role: profileRoleName(r) })); continue; }
    const problem = readRole(r, raw.roles[r]);
    if (problem) return fail(problem);
  }
  const preferences = {};
  if (raw.preferences && typeof raw.preferences === "object") Object.keys(CONFIG_PREFS).forEach(k => {
    if (raw.preferences[k] === undefined) return;
    if (CONFIG_PREFS[k](raw.preferences[k])) preferences[k] = raw.preferences[k];
    else warnings.push(I18N.t("profiles.import.prefIgnored", { name: k }));
  });
  return { ok:true, roles, active:PROFILE_ROLES.includes(raw.active) ? raw.active : null, preferences, warnings };
}

// Sauvegarde, écriture, relecture. Au moindre échec, l'état précédent est remis en place.
function applyConfig(parsed, current, storage, now) {
  if (!parsed || !parsed.ok) return { ok:false, error:I18N.t("profiles.import.invalid") };
  const get = k => { try { return storage.getItem(k); } catch (e) { return null; } };
  const before = get(PROFILES_KEY), beforeOld = get("draftProfile"), beforeBackup = get(PROFILES_BACKUP_KEY);
  const next = emptyProfiles();
  // Un ancien profil unique ne remplace que son rôle.
  PROFILE_ROLES.forEach(r => { next.roles[r] = parsed.legacy && r !== parsed.active ? cleanRoleProfile(current.roles[r]) : parsed.roles[r]; });
  next.active = parsed.active || current.active;
  try {
    backupProfiles(storage, "import", now);
    persistProfiles(next, storage);
    const check = normalizeProfiles(JSON.parse(storage.getItem(PROFILES_KEY)));
    if (!check || JSON.stringify(check.roles) !== JSON.stringify(next.roles) || check.active !== next.active) throw new Error("relecture");
    return { ok:true, profiles:next };
  } catch (e) {
    const put = (k, v) => { try { if (v === null) storage.removeItem(k); else storage.setItem(k, v); } catch (e2) {} };
    put(PROFILES_KEY, before); put("draftProfile", beforeOld); put(PROFILES_BACKUP_KEY, beforeBackup);
    return { ok:false, error:I18N.t("profiles.import.saveFailed") };
  }
}

// ---------- Rôle attribué par le client ----------
// assigned : rôle lu dans la session de sélection, ou null s'il est inconnu. Jamais déduit du champion choisi.
// session : { manual, dismissed, handled } pour la draft en cours.
function decideRoleAction(mode, session, active, assigned) {
  if (!assigned || !PROFILE_ROLES.includes(assigned) || assigned === active || mode === "off") return "none";
  if (mode === "auto" && !session.manual && session.handled !== assigned) return "switch";
  return session.dismissed === assigned ? "none" : "suggest";
}
