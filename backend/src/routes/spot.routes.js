import { Router } from "express";
import {
  claimSpotReward,
  getSpotById,
  listNearbySpots,
  listSpots
} from "../controllers/spot.controller.js";
import { requireAuth } from "../middleware/requireAuth.js";

const router = Router();

router.get("/", listSpots);
router.get("/nearby", listNearbySpots);
router.get("/:id", getSpotById);
router.post("/:id/claim", requireAuth, claimSpotReward);

export default router;
