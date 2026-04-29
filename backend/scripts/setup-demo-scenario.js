import "dotenv/config";
import { diplomaDemoScenario } from "../prisma/seed-data.js";
import { prisma } from "../src/lib/prisma.js";
import { prepareDemoScenario } from "../src/lib/demoScenario.js";

async function main() {
  const { user, claimedSpots, previewSpot } = await prepareDemoScenario({
    identity: {
      email: diplomaDemoScenario.email,
      displayName: diplomaDemoScenario.displayName,
      deviceId: diplomaDemoScenario.deviceId
    },
    claimedSpotNames: diplomaDemoScenario.claimedSpotNames,
    previewSpotName: diplomaDemoScenario.previewSpotName
  });

  console.log("Demo scenario is ready.");
  console.log(JSON.stringify({
    demoLogin: {
      email: diplomaDemoScenario.email,
      password: diplomaDemoScenario.password,
      displayName: diplomaDemoScenario.displayName,
      deviceId: diplomaDemoScenario.deviceId
    },
    user: {
      id: user.id,
      email: user.email,
      displayName: user.displayName
    },
    claimedSpots: claimedSpots.map((spot) => ({
      name: spot.name,
      reward: spot.reward?.name ?? null
    })),
    previewSpot: previewSpot
      ? {
          name: previewSpot.name,
          latitude: Number(previewSpot.latitude),
          longitude: Number(previewSpot.longitude),
          reward: previewSpot.reward?.name ?? null,
          radiusMeters: previewSpot.radiusMeters
        }
      : null
  }, null, 2));
}

main()
  .catch(async (error) => {
    console.error(error);
    process.exitCode = 1;
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
