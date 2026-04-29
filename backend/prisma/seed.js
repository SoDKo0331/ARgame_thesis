import { PrismaClient } from "@prisma/client";
import { rewards, tourismSpots } from "./seed-data.js";

const prisma = new PrismaClient();

async function main() {
  console.log("Seeding database...");

  await prisma.userReward.deleteMany();
  await prisma.tourismSpot.deleteMany();
  await prisma.reward.deleteMany();
  await prisma.user.deleteMany();

  for (const reward of rewards) {
    await prisma.reward.create({
      data: reward
    });
  }

  const rewardMap = new Map();
  const storedRewards = await prisma.reward.findMany();
  for (const reward of storedRewards) {
    rewardMap.set(reward.name, reward.id);
  }

  for (const spot of tourismSpots) {
    const rewardId = rewardMap.get(spot.rewardName);
    if (!rewardId) {
      throw new Error(
        `Seed error: tourism spot "${spot.name}" references missing reward "${spot.rewardName}".`
      );
    }

    await prisma.tourismSpot.create({
      data: {
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

  const sampleUser = await prisma.user.create({
    data: {
      deviceId: "seed-device-001",
      displayName: "Guest-SEED01"
    }
  });

  const firstSpot = await prisma.tourismSpot.findUnique({
    where: { name: "Sukhbaatar Square" }
  });

  if (firstSpot) {
    await prisma.userReward.create({
      data: {
        userId: sampleUser.id,
        rewardId: firstSpot.rewardId,
        tourismSpotId: firstSpot.id
      }
    });
  }

  console.log("Seed completed.");
}

main()
  .catch(async (error) => {
    console.error(error);
    process.exitCode = 1;
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
