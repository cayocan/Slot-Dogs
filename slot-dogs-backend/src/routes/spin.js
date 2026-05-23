const pf = require('../provablyfair/pfManager');
const engine = require('../engine/spinEngine');
const logger = require('../logger/logger');

function deductCoins(session, amount) {
  session.coins -= amount;
}

module.exports = async function (fastify, opts) {
  fastify.post('/spin', async (req, reply) => {
    const { sessionId, betPerLine } = req.body || {};
    if (!sessionId) return reply.code(400).send({ error: 'sessionId_required' });

    const session = pf._internal.sessions.get(sessionId);
    if (!session) return reply.code(404).send({ error: 'session_not_found' });
    if (!session.clientSeed) return reply.code(400).send({ error: 'clientSeed_required' });

    // Valida betPerLine se fornecido explicitamente
    if (betPerLine !== undefined && betPerLine !== null) {
      if (!Number.isInteger(betPerLine) || betPerLine < 1) {
        return reply.code(400).send({ error: 'betPerLine deve ser inteiro >= 1' });
      }
    }
    const bet = (betPerLine !== undefined && betPerLine !== null) ? betPerLine : session.betPerLine;

    // Decrementa free spins antes de qualquer lógica de custo
    const isFreePin = session.freeSpinsRemaining > 0;
    if (isFreePin) session.freeSpinsRemaining -= 1;

    // TODO: ENABLE_COST — descontar moedas quando ativado
    // if (!isFreePin) deductCoins(session, bet * 15);

    const floatsRes = pf.getSpinFloats(sessionId);
    if (!floatsRes.ok) return reply.code(400).send(floatsRes);

    const { stops, nonce } = floatsRes;

    const spin = engine.evaluateSpin(stops, bet);

    // Adiciona ganhos à sessão
    session.coins = (session.coins || 0) + spin.totalWin;
    if (spin.triggerFreeSpins) session.freeSpinsRemaining = (session.freeSpinsRemaining || 0) + spin.freeSpinsAwarded;

    // Log via pino + broadcast SSE
    const logObj = {
      level: 'info',
      time: Date.now(),
      msg: 'spin',
      sessionId: session.sessionId,
      nonce,
      totalBet: spin.totalBet,
      totalWin: spin.totalWin,
      winLevel: spin.winLevel,
      scatterCount: spin.scatterCount,
      triggerFreeSpins: spin.triggerFreeSpins,
      stopPositions: spin.stopPositions,
    };
    logger.info(logObj);

    return reply.send({
      spin,
      session: {
        coins: session.coins,
        freeSpinsRemaining: session.freeSpinsRemaining,
        nonce: session.nonce,
        serverSeedHash: session.serverSeedHash,
      },
      provablyFair: {
        serverSeedHash: session.serverSeedHash,
        clientSeed: session.clientSeed,
        nonce,
      },
    });
  });
};
