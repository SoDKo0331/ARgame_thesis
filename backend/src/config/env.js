import "dotenv/config";

function parsePort(value, fallback) {
  const parsed = Number.parseInt(value ?? "", 10);
  return Number.isNaN(parsed) ? fallback : parsed;
}

function parseBoolean(value, fallback) {
  if (value == null) {
    return fallback;
  }

  const normalized = value.trim().toLowerCase();

  if (["1", "true", "yes", "on"].includes(normalized)) {
    return true;
  }

  if (["0", "false", "no", "off"].includes(normalized)) {
    return false;
  }

  return fallback;
}

export const env = {
  port: parsePort(process.env.PORT, 4000),
  nodeEnv: process.env.NODE_ENV ?? "development",
  databaseUrl: process.env.DATABASE_URL ?? "",
  corsOrigin: process.env.CORS_ORIGIN ?? "*",
  jwtSecret: process.env.JWT_SECRET ?? "dev-insecure-jwt-secret",
  jwtExpiresInHours: parsePort(process.env.JWT_EXPIRES_IN_HOURS, 24 * 30),
  otpSecret: process.env.OTP_SECRET ?? process.env.JWT_SECRET ?? "dev-insecure-otp-secret",
  otpCodeLength: parsePort(process.env.OTP_CODE_LENGTH, 6),
  otpExpiresInMinutes: parsePort(process.env.OTP_EXPIRES_IN_MINUTES, 5),
  otpResendCooldownSeconds: parsePort(process.env.OTP_RESEND_COOLDOWN_SECONDS, 60),
  otpMaxAttempts: parsePort(process.env.OTP_MAX_ATTEMPTS, 5),
  gmailUser: process.env.GMAIL_USER ?? "",
  gmailAppPassword: process.env.GMAIL_APP_PASSWORD ?? "",
  mailFrom: process.env.MAIL_FROM ?? process.env.GMAIL_USER ?? "",
  allowDemoLogin: parseBoolean(
    process.env.ALLOW_DEMO_LOGIN,
    (process.env.NODE_ENV ?? "development") !== "production"
  ),
  demoLoginEmail: process.env.DEMO_LOGIN_EMAIL ?? "ssodko245@gmail.com",
  demoLoginPassword: process.env.DEMO_LOGIN_PASSWORD ?? "4123",
  demoLoginDisplayName: process.env.DEMO_LOGIN_DISPLAY_NAME ?? "Diploma Demo Explorer",
  demoLoginDeviceId: process.env.DEMO_LOGIN_DEVICE_ID ?? "demo-device-ssodko245",
  allowConsoleOtpFallback: parseBoolean(
    process.env.ALLOW_CONSOLE_OTP_FALLBACK,
    (process.env.NODE_ENV ?? "development") !== "production"
  )
};

if (!env.databaseUrl) {
  throw new Error("Missing required environment variable: DATABASE_URL");
}

if (env.nodeEnv === "production" && !process.env.JWT_SECRET) {
  throw new Error("Missing required environment variable: JWT_SECRET");
}
