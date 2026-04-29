import { env } from "./config/env.js";
import { prisma } from "./lib/prisma.js";
import { createApp } from "./app.js";

const app = createApp();
const host = "0.0.0.0";

const server = app.listen(env.port, host, () => {
  console.log(`Nomad Adventure backend listening on http://${host}:${env.port}`);
});

async function shutdown(signal) {
  console.log(`Received ${signal}. Shutting down...`);

  server.close(async () => {
    await prisma.$disconnect();
    process.exit(0);
  });

  setTimeout(() => {
    process.exit(1);
  }, 10000).unref();
}

process.on("SIGINT", () => {
  shutdown("SIGINT");
});

process.on("SIGTERM", () => {
  shutdown("SIGTERM");
});
