'use strict';

const { describe, test } = require('node:test');
const assert = require('node:assert/strict');
const config = require('../../src/engine/config');

const { REELS, PAYLINES, PAYTABLE, SCATTER_MULTIPLIERS, WILD_ID, SCATTER_ID, BLANK_ID } = config;

describe('config — reel strips', () => {
  test('existem exatamente 5 reels', () => {
    assert.equal(REELS.length, 5);
  });

  test('cada reel tem exatamente 30 símbolos', () => {
    for (let i = 0; i < REELS.length; i++) {
      assert.equal(REELS[i].length, 30, `Reel ${i} deve ter 30 símbolos`);
    }
  });

  test('todos os símbolos nos reels são IDs válidos (0–8)', () => {
    for (let i = 0; i < REELS.length; i++) {
      for (const sym of REELS[i]) {
        assert.ok(sym >= 0 && sym <= 8, `Reel ${i}: ID inválido ${sym}`);
      }
    }
  });

  test('reels 1 e 5 (índice 0 e 4) não contêm WILD (weight=0)', () => {
    assert.ok(!REELS[0].includes(WILD_ID), 'Reel 1 não deve conter WILD');
    assert.ok(!REELS[4].includes(WILD_ID), 'Reel 5 não deve conter WILD');
  });
});

describe('config — paytable', () => {
  test('PAYTABLE tem entradas para os 6 símbolos de raça (IDs 0–5)', () => {
    for (let id = 0; id <= 5; id++) {
      assert.ok(PAYTABLE[id], `PAYTABLE sem entrada para ID ${id}`);
      assert.equal(PAYTABLE[id].length, 3, `PAYTABLE[${id}] deve ter 3 multiplicadores`);
    }
  });

  test('multiplicadores crescem por contagem (3×, 4×, 5×) para cada símbolo', () => {
    for (let id = 0; id <= 5; id++) {
      const [m3, m4, m5] = PAYTABLE[id];
      assert.ok(m3 > 0 && m4 > m3 && m5 > m4,
        `PAYTABLE[${id}] deve ter multiplicadores crescentes`);
    }
  });

  test('Husky tem o maior multiplicador 5-in-a-row (500)', () => {
    assert.equal(PAYTABLE[0][2], 500);
  });
});

describe('config — paylines', () => {
  test('existem exatamente 15 paylines', () => {
    assert.equal(PAYLINES.length, 15);
  });

  test('cada payline tem exatamente 5 colunas', () => {
    for (const pl of PAYLINES) {
      assert.equal(pl.path.length, 5, `Payline ${pl.id} (${pl.name}) deve ter 5 colunas`);
    }
  });

  test('índices de linha das paylines estão no range 0–2', () => {
    for (const pl of PAYLINES) {
      for (const rowIdx of pl.path) {
        assert.ok(rowIdx >= 0 && rowIdx <= 2,
          `Payline ${pl.id}: rowIdx ${rowIdx} fora do range 0-2`);
      }
    }
  });

  test('todas as paylines têm id e name definidos', () => {
    for (const pl of PAYLINES) {
      assert.ok(pl.id, `Payline sem id`);
      assert.ok(pl.name, `Payline ${pl.id} sem name`);
    }
  });
});

describe('config — scatter multipliers', () => {
  test('SCATTER_MULTIPLIERS[3] = 2', () => assert.equal(SCATTER_MULTIPLIERS[3], 2));
  test('SCATTER_MULTIPLIERS[4] = 5', () => assert.equal(SCATTER_MULTIPLIERS[4], 5));
  test('SCATTER_MULTIPLIERS[5] = 20', () => assert.equal(SCATTER_MULTIPLIERS[5], 20));
});
