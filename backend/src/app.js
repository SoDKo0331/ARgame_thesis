import cors from "cors";
import express from "express";
import helmet from "helmet";
import morgan from "morgan";
import { env } from "./config/env.js";
import { errorHandler } from "./middleware/errorHandler.js";
import { notFoundHandler } from "./middleware/notFound.js";
import authRoutes from "./routes/auth.routes.js";
import spotRoutes from "./routes/spot.routes.js";
import userRoutes from "./routes/user.routes.js";

function buildCorsOptions() {
  if (env.corsOrigin === "*") {
    return { origin: true };
  }

  const allowedOrigins = env.corsOrigin
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean);

  return { origin: allowedOrigins };
}

export function createApp() {
  const app = express();

  app.use(helmet());
  app.use(cors(buildCorsOptions()));
  app.use(express.json({ limit: "1mb" }));
  app.use(morgan(env.nodeEnv === "development" ? "dev" : "combined"));

  app.get("/health", (req, res) => {
    res.status(200).json({
      status: "ok",
      service: "nomad-adventure-backend"
    });
  });

  app.use("/auth", authRoutes);
  app.use("/spots", spotRoutes);
  app.use("/users", userRoutes);

  app.use(notFoundHandler);
  app.use(errorHandler);

  return app;
}
