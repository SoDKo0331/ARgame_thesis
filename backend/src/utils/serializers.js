export function serializeUser(user) {
  return {
    id: user.id,
    deviceId: user.deviceId,
    displayName: user.displayName,
    email: user.email ?? null,
    emailVerifiedAt: user.emailVerifiedAt ? user.emailVerifiedAt.toISOString() : null,
    isEmailVerified: Boolean(user.emailVerifiedAt),
    lastLoginAt: user.lastLoginAt.toISOString(),
    createdAt: user.createdAt.toISOString(),
    updatedAt: user.updatedAt.toISOString()
  };
}

export function serializeReward(reward) {
  return {
    id: reward.id,
    name: reward.name,
    description: reward.description,
    imageUrl: reward.imageUrl,
    previewPrefabKey: reward.previewPrefabKey ?? null,
    createdAt: reward.createdAt.toISOString(),
    updatedAt: reward.updatedAt.toISOString()
  };
}

function toFiniteNumber(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

export function serializeSpot(spot) {
  const serializedSpot = {
    id: spot.id,
    name: spot.name,
    description: spot.description,
    latitude: Number(spot.latitude),
    longitude: Number(spot.longitude),
    radiusMeters: spot.radiusMeters,
    isActive: spot.isActive,
    modelPrefabKey: spot.modelPrefabKey ?? null,
    createdAt: spot.createdAt.toISOString(),
    updatedAt: spot.updatedAt.toISOString(),
    reward: spot.reward ? serializeReward(spot.reward) : null
  };

  const distanceMeters = toFiniteNumber(spot.distanceMeters);
  if (distanceMeters !== null) {
    serializedSpot.distanceMeters = distanceMeters;
  }

  return serializedSpot;
}

export function serializeUserReward(userReward) {
  return {
    id: userReward.id,
    userId: userReward.userId,
    claimedAt: userReward.claimedAt.toISOString(),
    reward: userReward.reward ? serializeReward(userReward.reward) : null,
    tourismSpot: userReward.tourismSpot ? serializeSpot(userReward.tourismSpot) : null
  };
}
