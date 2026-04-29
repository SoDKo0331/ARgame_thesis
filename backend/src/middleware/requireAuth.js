import { prisma } from "../lib/prisma.js";
import { ApiError } from "../utils/apiError.js";
import { verifyAccessToken } from "../utils/token.js";

export async function requireAuth(req, res, next) {
  try {
    const authorization = req.headers.authorization ?? "";

    if (!authorization.startsWith("Bearer ")) {
      throw new ApiError(401, "Authentication required");
    }

    const token = authorization.slice("Bearer ".length).trim();
    const payload = verifyAccessToken(token);
    const user = await prisma.user.findUnique({
      where: { id: payload.sub }
    });

    if (!user) {
      throw new ApiError(401, "Authenticated user was not found");
    }

    req.auth = {
      userId: user.id,
      user
    };

    return next();
  } catch (error) {
    return next(error instanceof ApiError ? error : new ApiError(401, error.message));
  }
}
