'use strict';

const Fastify = require('fastify');
const cors = require('@fastify/cors');
const config = require('./engine/config');

/**
 * Constrói e configura a instância Fastify.
 * Separado de server.js para permitir testes via app.inject().
 *
 * @param {import('fastify').FastifyServerOptions} opts
 */
function buildApp(opts = {}) {
  const app = Fastify(opts);

  app.register(cors, { origin: process.env.CORS_ORIGIN || '*' });
  app.register(require('./routes/session'));
  app.register(require('./routes/spin'));
  app.register(require('./routes/verify'));
  app.register(require('./routes/logs'));

  // Valida reel strips na inicialização
  for (let i = 0; i < config.REELS.length; i++) {
    if (config.REELS[i].length !== 30) {
      throw new Error(`Reel ${i} tem ${config.REELS[i].length} símbolos; esperado 30`);
    }
  }

  return app;
}

module.exports = { buildApp };
