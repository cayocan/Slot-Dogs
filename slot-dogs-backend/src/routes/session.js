const pf = require('../provablyfair/pfManager');

module.exports = async function (fastify, opts) {
  fastify.post('/session', async (req, reply) => {
    const s = pf.createSession();
    return reply.code(201).send(s);
  });

  fastify.post('/session/:id/seed', async (req, reply) => {
    const { id } = req.params;
    const { clientSeed } = req.body || {};
    if (!clientSeed || String(clientSeed).length < 8) {
      return reply.code(400).send({ ok: false, error: 'clientSeed must be at least 8 characters' });
    }
    const res = pf.setClientSeed(id, clientSeed);
    if (!res.ok) return reply.code(404).send(res);
    return reply.send({ ok: true });
  });

  fastify.get('/session/:id', async (req, reply) => {
    const { id } = req.params;
    const s = pf.getSession(id);
    if (!s) return reply.code(404).send({ error: 'session_not_found' });
    return reply.send(s);
  });

  fastify.post('/session/:id/rotate', async (req, reply) => {
    const { id } = req.params;
    const res = pf.rotate(id);
    if (!res.ok) return reply.code(404).send(res);
    return reply.send(res);
  });
};
