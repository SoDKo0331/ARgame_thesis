export const rewards = [
  {
    name: "Golden Eagle Pin",
    description: "Awarded for discovering the city from a high vantage point.",
    imageUrl: null,
    previewPrefabKey: "MongolBow/source/MongolBow"
  },
  {
    name: "Blue Sky Silk Scarf",
    description: "A symbolic scarf inspired by Mongolian hospitality and heritage.",
    imageUrl: null,
    previewPrefabKey: "MongolHelm/source/MongolHelm"
  },
  {
    name: "Ancient Map Fragment",
    description: "A collectible map piece tied to old trade and travel routes.",
    imageUrl: null,
    previewPrefabKey: "MongolSwordAndScabbard/source/MongolSwordAndScabbard"
  },
  {
    name: "Horsehead Fiddle Charm",
    description: "A musical keepsake inspired by the morin khuur.",
    imageUrl: null,
    previewPrefabKey: "HorseheadFiddleCharm"
  },
  {
    name: "Nomad Explorer Badge",
    description: "Given to players who reach one of the capital's iconic landmarks.",
    imageUrl: null,
    previewPrefabKey: "NomadExplorerBadge"
  }
];

export const tourismSpots = [
  {
    name: "Sukhbaatar Square",
    description: "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
    latitude: "47.918467",
    longitude: "106.917701",
    radiusMeters: 220,
    modelPrefabKey: "MongolHelm/source/MongolHelm",
    rewardName: "Blue Sky Silk Scarf"
  },
  {
    name: "Zaisan Memorial",
    description: "A panoramic hilltop monument overlooking the city.",
    latitude: "47.885563",
    longitude: "106.915520",
    radiusMeters: 200,
    modelPrefabKey: "MongolBow/source/MongolBow",
    rewardName: "Golden Eagle Pin"
  },
  {
    name: "Bogd Khaan Palace Museum",
    description: "Historic royal residence showcasing late Mongolian imperial history.",
    latitude: "47.899078",
    longitude: "106.910935",
    radiusMeters: 180,
    modelPrefabKey: "MongolSwordAndScabbard/source/MongolSwordAndScabbard",
    rewardName: "Ancient Map Fragment"
  },
  {
    name: "Choijin Lama Temple Museum",
    description: "A preserved temple complex in the city center.",
    latitude: "47.913423",
    longitude: "106.922833",
    radiusMeters: 70,
    modelPrefabKey: "ChoijinLamaTempleMuseum",
    rewardName: "Horsehead Fiddle Charm"
  },
  {
    name: "National Museum of Mongolia",
    description: "A central history museum covering Mongolia from prehistory to modern times.",
    latitude: "47.919124",
    longitude: "106.919945",
    radiusMeters: 200,
    modelPrefabKey: "NationalMuseumOfMongolia",
    rewardName: "Nomad Explorer Badge"
  },
  {
    name: "Test Spot 2",
    description: "Test location",
    latitude: "47.925306",
    longitude: "106.913250",
    radiusMeters: 200,
    modelPrefabKey: "TestSpot2",
    rewardName: "Horsehead Fiddle Charm"
  }
];

export const diplomaDemoScenario = {
  email: "ssodko245@gmail.com",
  password: "4123",
  displayName: "Diploma Demo Explorer",
  deviceId: "demo-device-ssodko245",
  claimedSpotNames: [
    "Sukhbaatar Square",
    "Zaisan Memorial",
    "Bogd Khaan Palace Museum"
  ],
  previewSpotName: "Sukhbaatar Square"
};
