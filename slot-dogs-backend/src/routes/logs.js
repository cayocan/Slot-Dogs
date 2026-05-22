const log = require('../logger/logger');

module.exports = async function (fastify, opts) {
  fastify.get('/logs/stream', async (req, reply) => {
    // register SSE client
    log.registerSSE(reply);
    // do not end the reply so connection stays open
    return reply.raw;
  });
};
