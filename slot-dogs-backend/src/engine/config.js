const crypto = require('crypto');

const SYMBOL_IDS = {
  HUSKY: 0,
  GOLDEN: 1,
  SHIBA: 2,
  PUG: 3,
  BEAGLE: 4,
  DACHSHUND: 5,
  WILD: 6,
  SCATTER: 7,
  BLANK: 8,
};

const SYMBOLS = [
  'Husky Siberiano',
  'Golden Retriever',
  'Shiba Inu',
  'Pug',
  'Beagle',
  'Dachshund',
  'Patinha Dourada (WILD)',
  'Ossinho (SCATTER)',
  'Blank',
];

const PAYTABLE = {
  [SYMBOL_IDS.HUSKY]: [35, 75, 500],       // 3x muito melhor (mais raro: 6.7%)
  [SYMBOL_IDS.GOLDEN]: [15, 40, 200],      // 3x melhorado (raro: 10%)
  [SYMBOL_IDS.SHIBA]: [12, 35, 175],       // 3x melhorado (raro: 10%)
  [SYMBOL_IDS.PUG]: [9, 22, 90],           // 3x melhorado (médio: 10%)
  [SYMBOL_IDS.BEAGLE]: [6, 18, 65],        // 3x melhorado (16.7%)
  [SYMBOL_IDS.DACHSHUND]: [5, 16, 55],     // calibrado para 98% RTP com scatter 6.5%
};

const SCATTER_MULTIPLIERS = {
  3: 2,
  4: 5,
  5: 20,
};

// Weights per symbol for each reel. Order of symbols: [Husky, Golden, Shiba, Pug, Beagle, Dachshund, Wild, Scatter, Blank]
// Scatter interno dobrado (1→2): bonus trigger mais acessível
const REEL_WEIGHTS = [
  [2, 3, 3, 3, 5, 10, 0, 2, 2],  // reel 1 — sem wild (outer)
  [2, 3, 3, 3, 4, 10, 2, 2, 1],  // reel 2 — scatter 1→2, blank 2→1
  [2, 3, 3, 3, 4, 10, 2, 2, 1],  // reel 3 — central, scatter 1→2, blank 2→1
  [2, 3, 3, 3, 4, 10, 2, 2, 1],  // reel 4 — scatter 1→2, blank 2→1
  [2, 3, 3, 3, 5, 10, 0, 2, 2],  // reel 5 — sem wild (outer)
];

function expandAndShuffle(weights) {
  const arr = [];
  for (let sym = 0; sym < weights.length; sym++) {
    const count = weights[sym];
    for (let i = 0; i < count; i++) arr.push(sym);
  }

  // Fisher-Yates shuffle using crypto
  for (let i = arr.length - 1; i > 0; i--) {
    const j = crypto.randomBytes(4).readUInt32BE(0) % (i + 1);
    const tmp = arr[i];
    arr[i] = arr[j];
    arr[j] = tmp;
  }

  return arr;
}

const REELS = REEL_WEIGHTS.map(expandAndShuffle);

const PAYLINES = [
  { id: 1,  name: 'Linha do meio',    path: [1, 1, 1, 1, 1] },
  { id: 2,  name: 'Linha de cima',    path: [0, 0, 0, 0, 0] },
  { id: 3,  name: 'Linha de baixo',   path: [2, 2, 2, 2, 2] },
  { id: 4,  name: 'V invertido',      path: [0, 1, 2, 1, 0] },
  { id: 5,  name: 'V normal',         path: [2, 1, 0, 1, 2] },
  { id: 6,  name: 'Diagonal ↘',      path: [0, 0, 1, 2, 2] },
  { id: 7,  name: 'Diagonal ↗',      path: [2, 2, 1, 0, 0] },
  { id: 10, name: 'Escada ↘',        path: [0, 1, 1, 2, 2] },
  { id: 11, name: 'Escada ↗',        path: [2, 1, 1, 0, 0] },
  { id: 14, name: 'Onda suave ↘',    path: [0, 1, 1, 1, 2] },
  { id: 15, name: 'Onda suave ↗',    path: [2, 1, 1, 1, 0] },
  { id: 20, name: 'Cruzada central',  path: [1, 0, 1, 2, 1] },
];

module.exports = {
  SYMBOL_IDS,
  SYMBOLS,
  PAYTABLE,
  SCATTER_MULTIPLIERS,
  REEL_WEIGHTS,
  REELS,
  PAYLINES,
  WILD_ID: SYMBOL_IDS.WILD,
  SCATTER_ID: SYMBOL_IDS.SCATTER,
  BLANK_ID: SYMBOL_IDS.BLANK,
};
