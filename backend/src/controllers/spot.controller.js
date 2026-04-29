import { z } from "zod";
import { Prisma } from "@prisma/client";
import { prisma } from "../lib/prisma.js";
import { asyncHandler } from "../utils/asyncHandler.js";
import { ApiError } from "../utils/apiError.js";
import {
  serializeReward,
  serializeSpot,
  serializeUserReward
} from "../utils/serializers.js";

const spotIdSchema = z.object({
  id: z.string().trim().min(1)
});

const nearbySpotsQuerySchema = z.object({
  latitude: z.coerce.number().finite().min(-90).max(90),
  longitude: z.coerce.number().finite().min(-180).max(180),
  radiusMeters: z.coerce.number().finite().positive().max(50000).default(5000),
  limit: z.coerce.number().int().positive().max(100).default(50)
});

const claimRequestSchema = z.object({
  userId: z.string().trim().min(1).optional(),
  hasLocation: z.boolean().optional(),
  latitude: z.number().finite().min(-90).max(90).optional(),
  longitude: z.number().finite().min(-180).max(180).optional(),
  horizontalAccuracyMeters: z.number().finite().nonnegative().max(5000).optional()
});

function buildClaimResponse(claim, alreadyClaimed) {
  return {
    alreadyClaimed,
    reward: claim.reward ? serializeReward(claim.reward) : null,
    tourismSpot: claim.tourismSpot ? serializeSpot(claim.tourismSpot) : null,
    claim: serializeUserReward(claim)
  };
}

function buildGeographyPoint(longitude, latitude) {
  return Prisma.sql`ST_SetSRID(ST_MakePoint(${longitude}, ${latitude}), 4326)::geography`;
}

function toRadians(value) {
  return (value * Math.PI) / 180;
}

function toFiniteNumber(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

let postGisSpotQueriesAvailable;
let postGisSpotQueriesCheckPromise;
let hasLoggedPostGisSpotQueryFallback = false;

function getErrorMessage(error) {
  return String(error?.meta?.message ?? error?.message ?? error);
}

function logPostGisSpotQueryFallback(reason) {
  if (hasLoggedPostGisSpotQueryFallback) {
    return;
  }

  hasLoggedPostGisSpotQueryFallback = true;
  console.warn(
    "[Spots] PostGIS proximity queries unavailable. Falling back to latitude/longitude distance:",
    reason
  );
}

function disablePostGisSpotQueries(reason) {
  postGisSpotQueriesAvailable = false;
  logPostGisSpotQueryFallback(reason);
}

async function canUsePostGisSpotQueries() {
  if (typeof postGisSpotQueriesAvailable === "boolean") {
    return postGisSpotQueriesAvailable;
  }

  if (!postGisSpotQueriesCheckPromise) {
    postGisSpotQueriesCheckPromise = prisma.$queryRaw`
      SELECT
        EXISTS (
          SELECT 1
          FROM information_schema.columns
          WHERE table_schema = current_schema()
            AND table_name = 'tourism_spots'
            AND column_name = 'coordinates'
        ) AS "has_coordinates",
        EXISTS (
          SELECT 1
          FROM pg_extension
          WHERE extname = 'postgis'
        ) AS "has_postgis_extension"
    `
      .then(([row]) => {
        const isAvailable =
          Boolean(row?.has_coordinates) && Boolean(row?.has_postgis_extension);

        if (!isAvailable) {
          const reasons = [];

          if (!row?.has_postgis_extension) {
            reasons.push("PostGIS extension is not installed");
          }

          if (!row?.has_coordinates) {
            reasons.push('column "tourism_spots.coordinates" is missing');
          }

          disablePostGisSpotQueries(
            reasons.length > 0 ? reasons.join("; ") : "database does not support PostGIS spot queries"
          );
        }

        postGisSpotQueriesAvailable = isAvailable;
        return isAvailable;
      })
      .catch((error) => {
        disablePostGisSpotQueries(getErrorMessage(error));
        return false;
      });
  }

  postGisSpotQueriesAvailable = await postGisSpotQueriesCheckPromise;
  return postGisSpotQueriesAvailable;
}

function calculateDistanceMeters(fromLatitude, fromLongitude, toLatitude, toLongitude) {
  const earthRadiusMeters = 6371000;
  const latitudeDelta = toRadians(toLatitude - fromLatitude);
  const longitudeDelta = toRadians(toLongitude - fromLongitude);
  const startLatitude = toRadians(fromLatitude);
  const endLatitude = toRadians(toLatitude);

  const haversine =
    Math.sin(latitudeDelta / 2) ** 2 +
    Math.cos(startLatitude) *
      Math.cos(endLatitude) *
      Math.sin(longitudeDelta / 2) ** 2;

  return 2 * earthRadiusMeters * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
}

function isPostGisUnavailableError(error) {
  if (
    !(error instanceof Prisma.PrismaClientKnownRequestError) ||
    error.code !== "P2010"
  ) {
    return false;
  }

  const databaseCode = typeof error.meta?.code === "string" ? error.meta.code : "";
  const databaseMessage = String(error.meta?.message ?? error.message).toLowerCase();

  return (
    databaseCode === "42703" ||
    databaseCode === "42883" ||
    databaseCode === "58P01" ||
    databaseMessage.includes("postgis") ||
    databaseMessage.includes("coordinates")
  );
}

export const listSpots = asyncHandler(async (req, res) => {
  const spots = await prisma.tourismSpot.findMany({
    where: { isActive: true },
    include: { reward: true },
    orderBy: { name: "asc" }
  });

  return res.status(200).json({
    spots: spots.map(serializeSpot)
  });
});

export const listNearbySpots = asyncHandler(async (req, res) => {
  const { latitude, longitude, radiusMeters, limit } = nearbySpotsQuerySchema.parse(req.query);
  const playerPoint = buildGeographyPoint(longitude, latitude);

  if (await canUsePostGisSpotQueries()) {
    try {
      const nearbyRows = await prisma.$queryRaw`
        SELECT
          ts."id",
          ST_Distance(ts."coordinates", ${playerPoint}) AS "distance_meters"
        FROM "tourism_spots" ts
        WHERE ts."is_active" = TRUE
          AND ts."coordinates" IS NOT NULL
          AND ST_DWithin(ts."coordinates", ${playerPoint}, ${radiusMeters})
        ORDER BY "distance_meters" ASC
        LIMIT ${limit}
      `;

      if (!Array.isArray(nearbyRows) || nearbyRows.length === 0) {
        return res.status(200).json({ spots: [] });
      }

      const orderedSpotIds = nearbyRows.map((row) => row.id);
      const distanceBySpotId = new Map(
        nearbyRows.map((row) => [row.id, toFiniteNumber(row.distance_meters)])
      );

      const spots = await prisma.tourismSpot.findMany({
        where: {
          id: {
            in: orderedSpotIds
          }
        },
        include: { reward: true }
      });

      const spotById = new Map(spots.map((spot) => [spot.id, spot]));
      const orderedNearbySpots = orderedSpotIds
        .map((spotId) => {
          const spot = spotById.get(spotId);
          if (!spot) {
            return null;
          }

          const distanceMeters = distanceBySpotId.get(spotId);
          return distanceMeters === null || typeof distanceMeters === "undefined"
            ? spot
            : { ...spot, distanceMeters };
        })
        .filter(Boolean);

      return res.status(200).json({
        spots: orderedNearbySpots.map(serializeSpot)
      });
    } catch (error) {
      if (!isPostGisUnavailableError(error)) {
        throw error;
      }

      disablePostGisSpotQueries(getErrorMessage(error));
    }
  }

  const spots = await prisma.tourismSpot.findMany({
    where: { isActive: true },
    include: { reward: true }
  });

  const nearbySpots = spots
    .map((spot) => ({
      ...spot,
      distanceMeters: calculateDistanceMeters(
        latitude,
        longitude,
        Number(spot.latitude),
        Number(spot.longitude)
      )
    }))
    .filter((spot) => spot.distanceMeters <= radiusMeters)
    .sort((left, right) => left.distanceMeters - right.distanceMeters)
    .slice(0, limit);

  return res.status(200).json({
    spots: nearbySpots.map(serializeSpot)
  });
});

export const getSpotById = asyncHandler(async (req, res) => {
  const { id } = spotIdSchema.parse(req.params);

  const spot = await prisma.tourismSpot.findUnique({
    where: { id },
    include: { reward: true }
  });

  if (!spot) {
    throw new ApiError(404, "Tourism spot not found");
  }

  return res.status(200).json({
    spot: serializeSpot(spot)
  });
});

export const claimSpotReward = asyncHandler(async (req, res) => {
  const { id } = spotIdSchema.parse(req.params);
  const payload = claimRequestSchema.parse(req.body ?? {});
  const userId = req.auth?.userId;

  if (!userId) {
    throw new ApiError(401, "Authentication required");
  }

  const [spot, user] = await Promise.all([
    prisma.tourismSpot.findUnique({
      where: { id },
      include: { reward: true }
    }),
    prisma.user.findUnique({
      where: { id: userId }
    })
  ]);

  if (!spot) {
    throw new ApiError(404, "Tourism spot not found");
  }

  if (!user) {
    throw new ApiError(404, "User not found");
  }

  if (!payload.hasLocation) {
    throw new ApiError(400, "Current location is required to claim this reward");
  }

  if (typeof payload.latitude !== "number" || typeof payload.longitude !== "number") {
    throw new ApiError(400, "Current location is required to claim this reward");
  }

  const accuracyAllowanceMeters =
    typeof payload.horizontalAccuracyMeters === "number"
      ? Math.min(Math.max(payload.horizontalAccuracyMeters, 0), 25)
      : 0;
  const allowedRadiusMeters = Math.max(spot.radiusMeters, 5) + accuracyAllowanceMeters;
  let isWithinAllowedRadius = false;
  let distanceMeters = null;

  if (await canUsePostGisSpotQueries()) {
    try {
      const playerPoint = buildGeographyPoint(payload.longitude, payload.latitude);
      const [proximityResult] = await prisma.$queryRaw`
        SELECT
          ST_Distance(ts."coordinates", ${playerPoint}) AS "distance_meters",
          ST_DWithin(ts."coordinates", ${playerPoint}, ${allowedRadiusMeters}) AS "is_within_allowed_radius"
        FROM "tourism_spots" ts
        WHERE ts."id" = ${spot.id}
        LIMIT 1
      `;

      isWithinAllowedRadius = Boolean(proximityResult?.is_within_allowed_radius);
      distanceMeters = toFiniteNumber(proximityResult?.distance_meters);
    } catch (error) {
      if (!isPostGisUnavailableError(error)) {
        throw error;
      }

      disablePostGisSpotQueries(getErrorMessage(error));
    }
  }

  if (distanceMeters === null) {
    distanceMeters = calculateDistanceMeters(
      payload.latitude,
      payload.longitude,
      Number(spot.latitude),
      Number(spot.longitude)
    );
    isWithinAllowedRadius = distanceMeters <= allowedRadiusMeters;
  }

  if (!isWithinAllowedRadius || distanceMeters === null || distanceMeters > allowedRadiusMeters) {
    throw new ApiError(403, `Move closer to ${spot.name} to collect this reward`);
  }

  const includeConfig = {
    reward: true,
    tourismSpot: {
      include: {
        reward: true
      }
    }
  };

  const existingClaim = await prisma.userReward.findUnique({
    where: {
      userId_tourismSpotId: {
        userId,
        tourismSpotId: spot.id
      }
    },
    include: includeConfig
  });

  if (existingClaim) {
    return res.status(200).json(buildClaimResponse(existingClaim, true));
  }

  try {
    const claim = await prisma.userReward.create({
      data: {
        userId,
        rewardId: spot.rewardId,
        tourismSpotId: spot.id
      },
      include: includeConfig
    });

    return res.status(201).json(buildClaimResponse(claim, false));
  } catch (error) {
    if (
      error instanceof Prisma.PrismaClientKnownRequestError &&
      error.code === "P2002"
    ) {
      const racedExistingClaim = await prisma.userReward.findUnique({
        where: {
          userId_tourismSpotId: {
            userId,
            tourismSpotId: spot.id
          }
        },
        include: includeConfig
      });

      if (racedExistingClaim) {
        return res.status(200).json(buildClaimResponse(racedExistingClaim, true));
      }
    }

    throw error;
  }
});
