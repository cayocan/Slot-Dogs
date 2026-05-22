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
  [SYMBOL_IDS.HUSKY]: [15, 75, 500],
  [SYMBOL_IDS.GOLDEN]: [8, 40, 200],
  [SYMBOL_IDS.SHIBA]: [6, 30, 150],
  [SYMBOL_IDS.PUG]: [4, 20, 80],
  [SYMBOL_IDS.BEAGLE]: [3, 15, 60],
  [SYMBOL_IDS.DACHSHUND]: [2, 10, 40],
};

const SCATTER_MULTIPLIERS = {
  3: 2,
  4: 5,
  5: 20,
};

// Weights per symbol for each reel. Order of symbols: [Husky, Golden, Shiba, Pug, Beagle, Dachshund, Wild, Scatter, Blank]
// Tuned for ~96% RTP (lab validated)
const REEL_WEIGHTS = [
  [1, 2, 2, 3, 5, 12, 0, 2, 3],  // reel 1 — sem wild (outer)
  [1, 2, 2, 3, 5, 11, 2, 1, 3],  // reel 2
  [1, 2, 2, 3, 4, 11, 2, 1, 4],  // reel 3 — central
  [1, 2, 2, 3, 5, 11, 2, 1, 3],  // reel 4
  [1, 2, 2, 3, 5, 12, 0, 2, 3],  // reel 5 — sem wild (outer)
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
  { id: 1, name: 'Linha do meio', path: [1, 1, 1, 1, 1] },
  { id: 2, name: 'Linha de cima', path: [0, 0, 0, 0, 0] },
  { id: 3, name: 'Linha de baixo', path: [2, 2, 2, 2, 2] },
  { id: 4, name: 'V invertido', path: [0, 1, 2, 1, 0] },
  { id: 5, name: 'V normal', path: [2, 1, 0, 1, 2] },
  { id: 6, name: 'Diagonal ↘', path: [0, 0, 1, 2, 2] },
  { id: 7, name: 'Diagonal ↗', path: [2, 2, 1, 0, 0] },
  { id: 8, name: 'Z cima→baixo', path: [0, 0, 1, 2, 2] },
  { id: 9, name: 'Z invertido', path: [2, 2, 1, 0, 0] },
  { id: 10, name: 'Escada ↘', path: [0, 1, 1, 2, 2] },
  { id: 11, name: 'Escada ↗', path: [2, 1, 1, 0, 0] },
  { id: 14, name: 'Onda suave ↘', path: [0, 1, 1, 1, 2] },
  { id: 15, name: 'Onda suave ↗', path: [2, 1, 1, 1, 0] },
  { id: 16, name: 'Topo-meio-baixo', path: [0, 1, 2, 1, 0] },
  { id: 20, name: 'Cruzada central', path: [1, 0, 1, 2, 1] },
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
