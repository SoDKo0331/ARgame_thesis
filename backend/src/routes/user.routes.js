import { Router } from "express";
import { getUserRewards } from "../controllers/user.controller.js";
import { requireAuth } from "../middleware/requireAuth.js";

const router = Router();

router.get("/me/rewards", requireAuth, getUserRewards);
router.get("/:id/rewards", requireAuth, getUserRewards);

export default router;
