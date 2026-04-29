import crypto from "crypto";
import { env } from "../config/env.js";

export function normalizeEmail(email) {
  return email.trim().toLowerCase();
}

export function generateOtpCode() {
  const min = 10 ** (env.otpCodeLength - 1);
  const max = 10 ** env.otpCodeLength;
  return String(crypto.randomInt(min, max));
}

export function hashOtp(email, code) {
  return crypto
    .createHash("sha256")
    .update(`${env.otpSecret}:${normalizeEmail(email)}:${code}`)
    .digest("hex");
}

export function maskEmail(email) {
  const normalized = normalizeEmail(email);
  const [localPart, domain = ""] = normalized.split("@");
  const visibleLocal = localPart.slice(0, 2);
  const hiddenLocal = Math.max(localPart.length - visibleLocal.length, 0);

  return `${visibleLocal}${"*".repeat(hiddenLocal)}@${domain}`;
}
