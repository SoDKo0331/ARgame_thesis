import { Router } from "express";
import {
  demoLogin,
  guestLogin,
  requestEmailOtp,
  verifyEmailOtp
} from "../controllers/auth.controller.js";
import { requireAuth } from "../middleware/requireAuth.js";

const router = Router();

router.post("/guest-login", guestLogin);
router.post("/demo-login", demoLogin);
router.post("/email/request-otp", requireAuth, requestEmailOtp);
router.post("/email/verify-otp", requireAuth, verifyEmailOtp);

export default router;
