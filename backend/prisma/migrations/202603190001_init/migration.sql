-- CreateTable
CREATE TABLE "users" (
    "id" TEXT NOT NULL,
    "device_id" TEXT NOT NULL,
    "display_name" TEXT NOT NULL,
    "last_login_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,
    CONSTRAINT "users_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "rewards" (
    "id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "description" TEXT NOT NULL,
    "image_url" TEXT,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,
    CONSTRAINT "rewards_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "tourism_spots" (
    "id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "description" TEXT,
    "latitude" DECIMAL(9,6) NOT NULL,
    "longitude" DECIMAL(9,6) NOT NULL,
    "radius_meters" DOUBLE PRECISION NOT NULL DEFAULT 50,
    "is_active" BOOLEAN NOT NULL DEFAULT true,
    "reward_id" TEXT NOT NULL,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,
    CONSTRAINT "tourism_spots_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "user_rewards" (
    "id" TEXT NOT NULL,
    "user_id" TEXT NOT NULL,
    "reward_id" TEXT NOT NULL,
    "tourism_spot_id" TEXT NOT NULL,
    "claimed_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "user_rewards_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "users_device_id_key" ON "users"("device_id");

-- CreateIndex
CREATE UNIQUE INDEX "rewards_name_key" ON "rewards"("name");

-- CreateIndex
CREATE UNIQUE INDEX "tourism_spots_name_key" ON "tourism_spots"("name");

-- CreateIndex
CREATE INDEX "tourism_spots_is_active_idx" ON "tourism_spots"("is_active");

-- CreateIndex
CREATE UNIQUE INDEX "user_rewards_user_id_tourism_spot_id_key" ON "user_rewards"("user_id", "tourism_spot_id");

-- CreateIndex
CREATE INDEX "user_rewards_reward_id_idx" ON "user_rewards"("reward_id");

-- CreateIndex
CREATE INDEX "user_rewards_tourism_spot_id_idx" ON "user_rewards"("tourism_spot_id");

-- AddForeignKey
ALTER TABLE "tourism_spots"
ADD CONSTRAINT "tourism_spots_reward_id_fkey"
FOREIGN KEY ("reward_id") REFERENCES "rewards"("id")
ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "user_rewards"
ADD CONSTRAINT "user_rewards_user_id_fkey"
FOREIGN KEY ("user_id") REFERENCES "users"("id")
ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "user_rewards"
ADD CONSTRAINT "user_rewards_reward_id_fkey"
FOREIGN KEY ("reward_id") REFERENCES "rewards"("id")
ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "user_rewards"
ADD CONSTRAINT "user_rewards_tourism_spot_id_fkey"
FOREIGN KEY ("tourism_spot_id") REFERENCES "tourism_spots"("id")
ON DELETE RESTRICT ON UPDATE CASCADE;
