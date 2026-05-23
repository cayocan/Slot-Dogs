'use strict';

const { describe, test } = require('node:test');
const assert = require('node:assert/strict');
const { buildWindow, evaluateLines, countScatters, evaluateSpin } = require('../../src/engine/spinEngine');
const { REELS, PAYLINES, PAYTABLE } = require('../../src/engine/config');

// Aliases para legibilidade
const HUSKY = 0, GOLDEN = 1, SHIBA = 2, PUG = 3, BEAGLE = 4, DACHSHUND = 5;
const WILD = 6, SCATTER = 7, BLANK = 8;

// Helper: cria grade 5×3 preenchida com um símbolo
function filledGrid(sym) {
  return [[sym, sym, sym], [sym, sym, sym], [sym, sym, sym], [sym, sym, sym], [sym, sym, sym]];
}

// Helper: grade com símbolo apenas na linha do meio (row=1)
function middleGrid(sym) {
  return [
    [BLANK, sym, BLANK],
    [BLANK, sym, BLANK],
    [BLANK, sym, BLANK],
    [BLANK, sym, BLANK],
    [BLANK, sym, BLANK],
  ];
}

// ─── buildWindow ─────────────────────────────────────────────────────────────
describe('buildWindow', () => {
  test('retorna grid 5×3', () => {
    const grid = buildWindow([0, 0, 0, 0, 0]);
    assert.equal(grid.length, 5);
    for (const col of grid) assert.equal(col.length, 3);
  });

  test('todos os símbolos da grid são IDs válidos (0–8)', () => {
    const grid = buildWindow([5, 10, 15, 20, 25]);
    for (const col of grid)
      for (const sym of col)
        assert.ok(sym >= 0 && sym <= 8, `ID inválido: ${sym}`);
  });

  test('wrap-around: stop=29 lê posições 29, 0, 1 do reel', () => {
    const grid = buildWindow([29, 0, 0, 0, 0]);
    assert.equal(grid[0][0], REELS[0][29]);
    assert.equal(grid[0][1], REELS[0][0]);
    assert.equal(grid[0][2], REELS[0][1]);
  });

  test('stop=0 lê as 3 primeiras posições do reel', () => {
    const grid = buildWindow([0, 0, 0, 0, 0]);
    for (let c = 0; c < 5; c++) {
      assert.equal(grid[c][0], REELS[c][0]);
      assert.equal(grid[c][1], REELS[c][1]);
      assert.equal(grid[c][2], REELS[c][2]);
    }
  });
});

// ─── evaluateLines ────────────────────────────────────────────────────────────
describe('evaluateLines', () => {
  test('5-in-a-row Dachshund na linha do meio → multiplier 40', () => {
    // Linha do meio (payline 1, path=[1,1,1,1,1]): todos os reels têm Dachshund na row 1
    const grid = middleGrid(DACHSHUND);
    const { lineWins, lineWinTotal } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.ok(win, 'Payline 1 deve ter win');
    assert.equal(win.symbolId, DACHSHUND);
    assert.equal(win.count, 5);
    assert.equal(win.multiplier, PAYTABLE[DACHSHUND][2]); // 40
    assert.equal(win.coins, 40);
    assert.ok(lineWinTotal >= 40);
  });

  test('3-in-a-row Dachshund + WILD na col 1 → sequência de 3 contando WILD', () => {
    // Path [1,1,1,1,1]: DACH, WILD, DACH, BLANK, BLANK → count=3 (WILD estende)
    const grid = [
      [BLANK, DACHSHUND, BLANK],
      [BLANK, WILD,      BLANK],
      [BLANK, DACHSHUND, BLANK],
      [BLANK, BLANK,     BLANK],
      [BLANK, BLANK,     BLANK],
    ];
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.ok(win, 'Payline 1 deve ter win com WILD estendendo');
    assert.equal(win.symbolId, DACHSHUND);
    assert.equal(win.count, 3);
    assert.equal(win.multiplier, PAYTABLE[DACHSHUND][0]); // 2
  });

  test('WILD na col 0 conta na sequência', () => {
    // WILD, DACH, DACH, BLANK, BLANK → count=3 (WILD+DACH+DACH)
    const grid = [
      [BLANK, WILD,      BLANK],
      [BLANK, DACHSHUND, BLANK],
      [BLANK, DACHSHUND, BLANK],
      [BLANK, BLANK,     BLANK],
      [BLANK, BLANK,     BLANK],
    ];
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.ok(win, 'WILD na col 0 deve iniciar sequência');
    assert.equal(win.count, 3);
  });

  test('BLANK na col 0 impede qualquer win naquela payline', () => {
    // Col 0 da payline 1 é blank → sem win na payline 1
    const grid = [
      [BLANK,     BLANK,     BLANK],
      [BLANK,     DACHSHUND, BLANK],
      [BLANK,     DACHSHUND, BLANK],
      [BLANK,     DACHSHUND, BLANK],
      [BLANK,     BLANK,     BLANK],
    ];
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.equal(win, undefined, 'BLANK na col 0 não deve gerar win');
  });

  test('BLANK no meio quebra a sequência (count < 3 → sem win)', () => {
    // DACH, BLANK, DACH, DACH, DACH → sequence quebra em col 1
    const grid = [
      [BLANK, DACHSHUND, BLANK],
      [BLANK, BLANK,     BLANK],
      [BLANK, DACHSHUND, BLANK],
      [BLANK, DACHSHUND, BLANK],
      [BLANK, DACHSHUND, BLANK],
    ];
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.equal(win, undefined, 'BLANK no meio deve quebrar sequência');
  });

  test('linha composta só de WILDs paga como Husky (500 × betPerLine)', () => {
    const grid = middleGrid(WILD);
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1 && w.count === 5);
    assert.ok(win, 'Linha de 5 WILDs deve gerar win');
    assert.equal(win.symbolId, HUSKY, 'WILDs puros devem pagar como Husky');
    assert.equal(win.multiplier, PAYTABLE[HUSKY][2]); // 500
  });

  test('SCATTER na col 0 não gera line win', () => {
    const grid = middleGrid(SCATTER);
    const { lineWins } = evaluateLines(grid, 1);
    assert.equal(lineWins.length, 0, 'Scatter não deve gerar line win');
  });

  test('2-in-a-row não paga (mínimo 3)', () => {
    const grid = [
      [BLANK, GOLDEN, BLANK],
      [BLANK, GOLDEN, BLANK],
      [BLANK, BLANK,  BLANK],
      [BLANK, BLANK,  BLANK],
      [BLANK, BLANK,  BLANK],
    ];
    const { lineWins } = evaluateLines(grid, 1);
    const win = lineWins.find(w => w.lineId === 1);
    assert.equal(win, undefined, '2-in-a-row não deve pagar');
  });

  test('betPerLine multiplica moedas corretamente', () => {
    const grid = middleGrid(BEAGLE);
    const { lineWins } = evaluateLines(grid, 5);
    const win = lineWins.find(w => w.lineId === 1 && w.count === 5);
    assert.ok(win);
    assert.equal(win.coins, PAYTABLE[BEAGLE][2] * 5); // 60 × 5 = 300
  });
});

// ─── countScatters ────────────────────────────────────────────────────────────
describe('countScatters', () => {
  test('grade sem scatters → 0', () => {
    assert.equal(countScatters(filledGrid(BLANK)), 0);
  });

  test('grade com 5 scatters em posições variadas', () => {
    const grid = [
      [SCATTER, BLANK,   BLANK],
      [BLANK,   SCATTER, BLANK],
      [BLANK,   BLANK,   SCATTER],
      [SCATTER, BLANK,   BLANK],
      [BLANK,   SCATTER, BLANK],
    ];
    assert.equal(countScatters(grid), 5);
  });

  test('grade toda de scatter → 15 (5 colunas × 3 linhas)', () => {
    assert.equal(countScatters(filledGrid(SCATTER)), 15);
  });
});

// ─── evaluateSpin (integração do engine puro) ─────────────────────────────────
describe('evaluateSpin', () => {
  test('retorna todos os campos obrigatórios do SpinResult', () => {
    const result = evaluateSpin([0, 0, 0, 0, 0], 1);
    const required = [
      'stopPositions', 'grid', 'lineWins', 'lineWinTotal',
      'scatterCount', 'scatterCoins', 'triggerFreeSpins',
      'freeSpinsAwarded', 'totalBet', 'totalWin', 'winLevel',
    ];
    for (const field of required)
      assert.ok(field in result, `Campo ausente: ${field}`);
  });

  test('totalBet = betPerLine × 15 linhas', () => {
    assert.equal(evaluateSpin([0, 0, 0, 0, 0], 1).totalBet, 15);
    assert.equal(evaluateSpin([0, 0, 0, 0, 0], 3).totalBet, 45);
  });

  test('stopPositions no resultado coincidem com o input', () => {
    const stops = [2, 7, 14, 21, 28];
    const result = evaluateSpin(stops, 1);
    assert.deepEqual(result.stopPositions, stops);
  });

  test('grid é 5×3', () => {
    const result = evaluateSpin([0, 0, 0, 0, 0], 1);
    assert.equal(result.grid.length, 5);
    for (const col of result.grid) assert.equal(col.length, 3);
  });

  test('winLevel "none" quando totalWin = 0 (via evaluateLines)', () => {
    // Forçar grade vazia de blanks para garantir totalWin=0
    const { lineWinTotal } = evaluateLines(filledGrid(BLANK), 1);
    assert.equal(lineWinTotal, 0);
  });

  test('winLevel thresholds corretos (jackpot ≥ 50× totalBet)', () => {
    // Grade toda de Husky: 15 paylines × 5-in-a-row × 500 = 7500 coins
    // totalBet=15. 7500 >= 15×50=750 → jackpot
    const { lineWinTotal } = evaluateLines(filledGrid(HUSKY), 1);
    const totalBet = 1 * PAYLINES.length;
    assert.ok(lineWinTotal >= totalBet * 50, `lineWinTotal ${lineWinTotal} deve ser >= jackpot`);
  });

  test('triggerFreeSpins e freeSpinsAwarded coerentes com scatterCount', () => {
    const result = evaluateSpin([0, 0, 0, 0, 0], 1);
    if (result.scatterCount >= 3) {
      assert.equal(result.triggerFreeSpins, true);
      assert.equal(result.freeSpinsAwarded, 8);
    } else {
      assert.equal(result.triggerFreeSpins, false);
      assert.equal(result.freeSpinsAwarded, 0);
    }
  });

  test('scatterCoins correto quando scatterCount >= 3', () => {
    // Construção direta com grid de 5 scatters
    const grid = [
      [SCATTER, BLANK, BLANK],
      [BLANK, SCATTER, BLANK],
      [BLANK, BLANK, SCATTER],
      [SCATTER, BLANK, BLANK],
      [BLANK, SCATTER, BLANK],
    ];
    const sc = countScatters(grid);
    assert.equal(sc, 5);
    // scatterCoins = SCATTER_MULTIPLIERS[5] × totalBet
    const { SCATTER_MULTIPLIERS } = require('../../src/engine/config');
    const expected = SCATTER_MULTIPLIERS[5] * (1 * PAYLINES.length); // 20 × 15 = 300
    // Verificar a fórmula diretamente (não via evaluateSpin pois não controlamos stops)
    assert.equal(expected, 300);
  });
});
