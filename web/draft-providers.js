// Connecteurs de draft. L'interface ne connaît que ces événements, jamais leur origine.
// Événements : session-start, enemy-added {champion, role}, enemy-changed {from, to},
// enemy-removed {champion}, role-changed {champion, role}, session-end.
class DraftProvider {
  constructor(onEvent) { this.onEvent = onEvent; }
  get id() { return "base"; }
  get enabled() { return true; }
  start() {}
  stop() {}
}

// Saisie utilisateur : toujours disponible, ne dépend de rien d'autre.
class ManualDraftProvider extends DraftProvider {
  get id() { return "manual"; }
  add(champion, role) { this.onEvent({ type:"enemy-added", champion, role }); }
  remove(champion) { this.onEvent({ type:"enemy-removed", champion }); }
}

// Données simulées, pour développer et tester sans client League of Legends.
const DEMO_SCRIPT = [
  { type:"session-start" },
  { type:"enemy-added", champion:"Sett" },
  { type:"enemy-added", champion:"Vi" },
  { type:"enemy-added", champion:"Galio" },
  { type:"enemy-added", champion:"Syndra" },
  { type:"enemy-changed", from:"Syndra", to:"Samira" },
  { type:"role-changed", champion:"Samira", role:"adc" },
  { type:"enemy-added", champion:"Janna" },
  { type:"session-end" }
];

class MockDraftProvider extends DraftProvider {
  constructor(onEvent, script = DEMO_SCRIPT, stepMs = 900) { super(onEvent); this.script = script; this.stepMs = stepMs; this.timers = []; }
  get id() { return "mock"; }
  start() { this.stop(); this.script.forEach((ev, i) => this.timers.push(setTimeout(() => this.onEvent(ev), i * this.stepMs))); }
  stop() { this.timers.forEach(clearTimeout); this.timers = []; }
}

// Désactivé côté interface : cette page ne se connecte jamais au client.
// La lecture éventuelle se fait côté application, verrouillée en attendant une réponse de Riot : voir docs/LCU.md.
class RiotDraftProvider extends DraftProvider {
  constructor(onEvent) { super(onEvent); this.detected = []; }
  get id() { return "riot"; }
  get enabled() { return false; }
  start() { throw new Error("RiotDraftProvider est désactivé : conditions d'utilisation à confirmer auprès de Riot."); }
  reset() { this.detected = []; }
  // Reçoit de l'application les champions lus : cette classe ne se connecte à rien.
  update(names) {
    diffDetected(this.detected, names).forEach(this.onEvent);
    this.detected = names;
  }
}

// Ne traduit que ce qui change entre deux lectures : les corrections manuelles restent intactes.
function diffDetected(before, after) {
  const events = [];
  before.filter(n => !after.includes(n)).forEach(champion => events.push({ type:"enemy-removed", champion }));
  after.filter(n => !before.includes(n)).forEach(champion => events.push({ type:"enemy-added", champion }));
  return events;
}

// Applique un événement via des opérations fournies par l'interface : même chemin que la saisie manuelle.
function applyDraftEvent(ev, api) {
  switch (ev.type) {
    case "session-start": api.reset(); api.setActive(true); break;
    case "enemy-added": api.add(ev.champion, ev.role); break;
    case "enemy-changed": api.replace(ev.from, ev.to); break;
    case "enemy-removed": api.remove(ev.champion); break;
    case "role-changed": api.setRole(ev.champion, ev.role); break;
    case "session-end": api.setActive(false); break;
    default: throw new Error(`Événement de draft inconnu : ${ev.type}`);
  }
}