import { rewards, tourismSpots, diplomaDemoScenario } from "../../prisma/seed-data.js";
import { prisma } from "./prisma.js";

function resolveDemoIdentity(identity = {}) {
  return {
    email: identity.email ?? diplomaDemoScenario.email,
    displayName: identity.displayName ?? diplomaDemoScenario.displayName,
    deviceId: identity.deviceId ?? diplomaDemoScenario.deviceId
  };
}

function resolveClaimedSpotNames(claimedSpotNames) {
  return [...new Set(claimedSpotNames ?? diplomaDemoScenario.claimedSpotNames)];
}

export async function ensureRewards() {
  for (const reward of rewards) {
    await prisma.reward.upsert({
      where: { name: reward.name },
      update: {
        description: reward.description,
        imageUrl: reward.imageUrl,
        previewPrefabKey: reward.previewPrefabKey
      },
      create: reward
    });
  }

  const rewardMap = new Map();
  const storedRewards = await prisma.reward.findMany();
  for (const reward of storedRewards) {
    rewardMap.set(reward.name, reward.id);
  }

  return rewardMap;
}

export async function ensureSpots(rewardMap) {
  for (const spot of tourismSpots) {
    const rewardId = rewardMap.get(spot.rewardName);
    if (!rewardId) {
      throw new Error(`Missing reward "${spot.rewardName}" for spot "${spot.name}".`);
    }

    await prisma.tourismSpot.upsert({
      where: { name: spot.name },
      update: {
        description: spot.description,
        latitude: spot.latitude,
        longitude: spot.longitude,
        radiusMeters: spot.radiusMeters,
        modelPrefabKey: spot.modelPrefabKey,
        isActive: true,
        rewardId
      },
      create: {
        name: spot.name,
        description: spot.description,
        latitude: spot.latitude,
        longitude: spot.longitude,
        radiusMeters: spot.radiusMeters,
        modelPrefabKey: spot.modelPrefabKey,
        rewardId
      }
    });
  }

  return prisma.tourismSpot.findMany({
    where: {
      name: {
        in: tourismSpots.map((spot) => spot.name)
      }
    },
    include: {
      reward: true
    }
  });
}

export async function upsertDemoUser(identity = {}) {
  const demoIdentity = resolveDemoIdentity(identity);
  const now = new Date();
  const existingUser = await prisma.user.findFirst({
    where: {
      OR: [
        { email: demoIdentity.email },
        { deviceId: demoIdentity.deviceId }
      ]
    }
  });

  if (existingUser) {
    return prisma.user.update({
      where: { id: existingUser.id },
      data: {
        deviceId: demoIdentity.deviceId,
        displayName: demoIdentity.displayName,
        email: demoIdentity.email,
        emailVerifiedAt: now,
        lastLoginAt: now
      }
    });
  }

  return prisma.user.create({
    data: {
      deviceId: demoIdentity.deviceId,
      displayName: demoIdentity.displayName,
      email: demoIdentity.email,
      emailVerifiedAt: now,
      lastLoginAt: now
    }
  });
}

export async function ensureDemoClaims(user, spots, claimedSpotNames = diplomaDemoScenario.claimedSpotNames) {
  const effectiveClaimedSpotNames = resolveClaimedSpotNames(claimedSpotNames);
  const spotByName = new Map(spots.map((spot) => [spot.name, spot]));
  const claimedSpotIds = [];

  for (const spotName of effectiveClaimedSpotNames) {
    const spot = spotByName.get(spotName);
    if (!spot) {
      throw new Error(`Claim spot "${spotName}" was not found.`);
    }

    claimedSpotIds.push(spot.id);

    await prisma.userReward.upsert({
      where: {
        userId_tourismSpotId: {
          userId: user.id,
          tourismSpotId: spot.id
        }
      },
      update: {
        rewardId: spot.rewardId
      },
      create: {
        userId: user.id,
        rewardId: spot.rewardId,
        tourismSpotId: spot.id
      }
    });
  }

  const managedSpotIds = spots.map((spot) => spot.id);
  const staleSpotIds = managedSpotIds.filter((spotId) => !claimedSpotIds.includes(spotId));

  if (staleSpotIds.length > 0) {
    await prisma.userReward.deleteMany({
      where: {
        userId: user.id,
        tourismSpotId: {
          in: staleSpotIds
        }
      }
    });
  }
}

export async function prepareDemoScenario(options = {}) {
  const rewardMap = await ensureRewards();
  const spots = await ensureSpots(rewardMap);
  const user = await upsertDemoUser(options.identity);
  const claimedSpotNames = resolveClaimedSpotNames(options.claimedSpotNames);

  await ensureDemoClaims(user, spots, claimedSpotNames);

  const spotByName = new Map(spots.map((spot) => [spot.name, spot]));

  return {
    user,
    spots,
    claimedSpots: claimedSpotNames
      .map((spotName) => spotByName.get(spotName))
      .filter(Boolean),
    previewSpot: options.previewSpotName
      ? spotByName.get(options.previewSpotName) ?? null
      : null
  };
}
