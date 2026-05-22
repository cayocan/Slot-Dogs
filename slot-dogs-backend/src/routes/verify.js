const pf = require('../provablyfair/pfManager');

module.exports = async function (fastify, opts) {
  fastify.post('/verify', async (req, reply) => {
    const { serverSeed, clientSeed, nonce, stops } = req.body || {};
    if (!serverSeed || !clientSeed || typeof nonce !== 'number' || !Array.isArray(stops)) {
      return reply.code(400).send({ error: 'serverSeed, clientSeed, nonce, stops required' });
    }

    const res = pf.verifySpin(serverSeed, clientSeed, nonce, stops);
    return reply.send(res);
  });
};
