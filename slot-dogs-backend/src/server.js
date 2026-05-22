require('dotenv').config();
const fastify = require('fastify');
const cors = require('@fastify/cors');
const { logger } = require('./logger/logger');
const config = require('./engine/config');

const app = fastify({ logger });

app.register(cors, { origin: process.env.CORS_ORIGIN || '*' });

// Register routes
app.register(require('./routes/session'));
app.register(require('./routes/spin'));
app.register(require('./routes/verify'));
app.register(require('./routes/logs'));

// Validate reels length
for (let i = 0; i < config.REELS.length; i++) {
  if (config.REELS[i].length !== 30) {
    app.log.error(`Reel ${i} length != 30 (${config.REELS[i].length})`);
    throw new Error('Reel configuration invalid');
  }
}

const port = process.env.PORT ? Number(process.env.PORT) : 3000;
const { version } = require('../package.json');

app.listen({ port, host: '0.0.0.0' }, (err, address) => {
  if (err) {
    app.log.error(err);
    process.exit(1);
  }
  app.log.info(`Servidor Slot Dogs rodando em ${address}`);
  app.log.info(`Versão: ${version} | Porta: ${port} | RTP alvo: 96% | Linhas: ${config.PAYLINES.length}`);
});
