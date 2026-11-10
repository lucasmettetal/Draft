// Relevé injecté par CDP dans une page de DRAFT (lecture seule) : contrastes, tailles de texte, cibles, focus.
// Contraste approché : couleur du texte contre le premier fond opaque des ancêtres (les images de fond sont ignorées).
(() => {
  const parse = c => { const m = c.match(/rgba?\(([^)]+)\)/); if (!m) return null; const p = m[1].split(',').map(Number); return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 }; };
  const lum = c => { const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }; return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b); };
  const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); };
  const bgOf = el => { for (let e = el; e; e = e.parentElement) { const c = parse(getComputedStyle(e).backgroundColor); if (c && c.a > 0.9) return c; } return { r: 10, g: 15, b: 22, a: 1 }; };
  const visible = e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none' && +s.opacity > 0.05 && r.bottom > 0 && r.top < innerHeight; };
  const low = [], small = [], targets = [];
  const seen = new Set();
  const tw = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  let n;
  while ((n = tw.nextNode())) {
    const t = n.nodeValue.trim(); const el = n.parentElement;
    if (!t || !el || ['SCRIPT', 'STYLE', 'OPTION'].includes(el.tagName) || !visible(el) || el.closest('.sr')) continue;
    const s = getComputedStyle(el); const fg = parse(s.color); if (!fg) continue;
    const r = ratio(fg, bgOf(el)) * 1; const fs = parseFloat(s.fontSize); const big = fs >= 24 || (fs >= 18.66 && +s.fontWeight >= 700);
    const key = el.className + '|' + s.color + '|' + fs;
    if (r < (big ? 3 : 4.5) && !seen.has(key)) { seen.add(key); low.push({ text: t.slice(0, 40), cls: String(el.className).slice(0, 40), color: s.color, ratio: Math.round(r * 100) / 100, px: fs }); }
    if (fs < 12 && !seen.has('s' + key)) { seen.add('s' + key); small.push({ text: t.slice(0, 30), cls: String(el.className).slice(0, 30), px: fs }); }
  }
  document.querySelectorAll('button, a, input, select, summary, [role=tab]').forEach(e => { if (!visible(e)) return; const r = e.getBoundingClientRect(); if (r.width < 24 || r.height < 24) targets.push({ el: e.tagName + '.' + String(e.className).slice(0, 30), label: (e.getAttribute('aria-label') || e.textContent || '').trim().slice(0, 30), w: Math.round(r.width), h: Math.round(r.height) }); });
  const focusables = [...document.querySelectorAll('button, a[href], input, select, summary, [tabindex]')].filter(visible).length;
  return JSON.stringify({ url: location.search, viewport: innerWidth + 'x' + innerHeight, dpr: devicePixelRatio, lowContrast: low, smallText: small, smallTargets: targets, focusables, h1: document.querySelectorAll('h1').length, h2: document.querySelectorAll('h2').length, landmarks: document.querySelectorAll('main,nav,aside,header').length });
})()
