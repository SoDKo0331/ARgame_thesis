import { Prisma } from "@prisma/client";
import { ZodError } from "zod";
import { env } from "../config/env.js";
import { ApiError } from "../utils/apiError.js";

export function errorHandler(error, req, res, next) {
  if (res.headersSent) {
    return next(error);
  }

  if (error instanceof ZodError) {
    return res.status(400).json({
      error: {
        message: "Validation failed",
        details: error.flatten()
      }
    });
  }

  if (error instanceof ApiError) {
    return res.status(error.statusCode).json({
      error: {
        message: error.message,
        details: error.details
      }
    });
  }

  if (error instanceof Prisma.PrismaClientKnownRequestError) {
    return res.status(400).json({
      error: {
        message: "Database request failed",
        details: {
          code: error.code,
          meta: error.meta
        }
      }
    });
  }

  const statusCode = 500;

  return res.status(statusCode).json({
    error: {
      message: "Internal server error",
      details:
        env.nodeEnv === "development"
          ? {
              message: error.message,
              stack: error.stack
            }
          : null
    }
  });
}
