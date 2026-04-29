import { z } from "zod";
import { prisma } from "../lib/prisma.js";
import { asyncHandler } from "../utils/asyncHandler.js";
import { ApiError } from "../utils/apiError.js";
import { serializeUserReward } from "../utils/serializers.js";

const userIdSchema = z.object({
  id: z.string().trim().min(1).optional()
});

export const getUserRewards = asyncHandler(async (req, res) => {
  const { id } = userIdSchema.parse(req.params ?? {});
  const authUserId = req.auth?.userId;
  const requestedUserId = id ?? "me";
  const targetUserId = requestedUserId === "me" ? authUserId : requestedUserId;

  if (!authUserId || !targetUserId) {
    throw new ApiError(401, "Authentication required");
  }

  if (targetUserId !== authUserId) {
    throw new ApiError(403, "You can only access your own rewards");
  }

  const user = await prisma.user.findUnique({
    where: { id: targetUserId }
  });

  if (!user) {
    throw new ApiError(404, "User not found");
  }

  const rewards = await prisma.userReward.findMany({
    where: { userId: targetUserId },
    include: {
      reward: true,
      tourismSpot: {
        include: {
          reward: true
        }
      }
    },
    orderBy: { claimedAt: "desc" }
  });

  return res.status(200).json({
    userId: targetUserId,
    rewards: rewards.map(serializeUserReward)
  });
});
