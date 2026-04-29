import { z } from "zod";
import { prisma } from "../lib/prisma.js";
import { sendVerificationEmail } from "../lib/mailer.js";
import { env } from "../config/env.js";
import { asyncHandler } from "../utils/asyncHandler.js";
import { ApiError } from "../utils/apiError.js";
import { normalizeEmail, generateOtpCode, hashOtp, maskEmail } from "../utils/otp.js";
import { serializeUser } from "../utils/serializers.js";
import { createAccessToken } from "../utils/token.js";
import { prepareDemoScenario } from "../lib/demoScenario.js";

const VERIFY_EMAIL_PURPOSE = "verify_email";

const emailSchema = z
  .string()
  .trim()
  .min(3)
  .max(191)
  .email()
  .transform((value) => normalizeEmail(value));

const guestLoginSchema = z.object({
  deviceId: z.string().trim().min(1).max(191),
  displayName: z.string().trim().min(1).max(80).optional()
});

const demoLoginSchema = z.object({
  email: emailSchema,
  password: z.string().trim().min(1).max(64)
});

const requestEmailOtpSchema = z.object({
  email: emailSchema
});

const verifyEmailOtpSchema = z.object({
  email: emailSchema,
  code: z
    .string()
    .trim()
    .length(env.otpCodeLength)
    .regex(/^\d+$/, "Verification code must contain digits only")
});

function buildGuestDisplayName(deviceId, requestedName) {
  if (requestedName) {
    return requestedName;
  }

  return `Guest-${deviceId.slice(-6).toUpperCase()}`;
}

function buildSessionResponse(user, extra = {}) {
  return {
    user: serializeUser(user),
    accessToken: createAccessToken(user.id),
    ...extra
  };
}

export const guestLogin = asyncHandler(async (req, res) => {
  const payload = guestLoginSchema.parse(req.body ?? {});
  const now = new Date();

  const existingUser = await prisma.user.findUnique({
    where: { deviceId: payload.deviceId }
  });

  const displayName = buildGuestDisplayName(payload.deviceId, payload.displayName);

  if (existingUser) {
    const updatedUser = await prisma.user.update({
      where: { id: existingUser.id },
      data: {
        displayName,
        lastLoginAt: now
      }
    });

    return res.status(200).json(
      buildSessionResponse(updatedUser, {
        isNewUser: false
      })
    );
  }

  const createdUser = await prisma.user.create({
    data: {
      deviceId: payload.deviceId,
      displayName,
      lastLoginAt: now
    }
  });

  return res.status(201).json(
    buildSessionResponse(createdUser, {
      isNewUser: true
    })
  );
});

export const demoLogin = asyncHandler(async (req, res) => {
  if (!env.allowDemoLogin) {
    throw new ApiError(404, "Demo login is not available");
  }

  const payload = demoLoginSchema.parse(req.body ?? {});
  if (payload.email !== normalizeEmail(env.demoLoginEmail) || payload.password !== env.demoLoginPassword) {
    throw new ApiError(401, "Invalid demo credentials");
  }

  const { user } = await prepareDemoScenario({
    identity: {
      email: env.demoLoginEmail,
      displayName: env.demoLoginDisplayName,
      deviceId: env.demoLoginDeviceId
    }
  });

  return res.status(200).json(
    buildSessionResponse(user, {
      isNewUser: false
    })
  );
});

export const requestEmailOtp = asyncHandler(async (req, res) => {
  const { email } = requestEmailOtpSchema.parse(req.body ?? {});
  const user = req.auth?.user;
  const now = new Date();

  if (!user) {
    throw new ApiError(401, "Authentication required");
  }

  if (user.email === email && user.emailVerifiedAt) {
    return res.status(200).json(
      buildSessionResponse(user, {
        alreadyVerified: true,
        email,
        maskedEmail: maskEmail(email),
        deliveryMethod: "verified"
      })
    );
  }

  const conflictingUser = await prisma.user.findFirst({
    where: {
      email,
      NOT: {
        id: user.id
      }
    },
    select: {
      id: true
    }
  });

  if (conflictingUser) {
    throw new ApiError(409, "This email address is already linked to another account");
  }

  const cooldownStart = new Date(Date.now() - env.otpResendCooldownSeconds * 1000);
  const recentOtp = await prisma.emailOtp.findFirst({
    where: {
      userId: user.id,
      email,
      purpose: VERIFY_EMAIL_PURPOSE,
      consumedAt: null,
      createdAt: {
        gte: cooldownStart
      }
    },
    orderBy: {
      createdAt: "desc"
    }
  });

  if (recentOtp) {
    throw new ApiError(
      429,
      `Please wait ${env.otpResendCooldownSeconds} seconds before requesting another code`
    );
  }

  await prisma.emailOtp.updateMany({
    where: {
      userId: user.id,
      purpose: VERIFY_EMAIL_PURPOSE,
      consumedAt: null
    },
    data: {
      consumedAt: now
    }
  });

  const code = generateOtpCode();
  const otpRecord = await prisma.emailOtp.create({
    data: {
      userId: user.id,
      email,
      purpose: VERIFY_EMAIL_PURPOSE,
      codeHash: hashOtp(email, code),
      expiresAt: new Date(Date.now() + env.otpExpiresInMinutes * 60 * 1000)
    }
  });

  try {
    const { deliveryMethod } = await sendVerificationEmail({ email, code });

    return res.status(200).json({
      email,
      maskedEmail: maskEmail(email),
      deliveryMethod,
      expiresInMinutes: env.otpExpiresInMinutes
    });
  } catch (error) {
    await prisma.emailOtp.delete({
      where: {
        id: otpRecord.id
      }
    });

    throw error;
  }
});

export const verifyEmailOtp = asyncHandler(async (req, res) => {
  const { email, code } = verifyEmailOtpSchema.parse(req.body ?? {});
  const user = req.auth?.user;
  const now = new Date();

  if (!user) {
    throw new ApiError(401, "Authentication required");
  }

  const activeOtp = await prisma.emailOtp.findFirst({
    where: {
      userId: user.id,
      email,
      purpose: VERIFY_EMAIL_PURPOSE,
      consumedAt: null
    },
    orderBy: {
      createdAt: "desc"
    }
  });

  if (!activeOtp) {
    throw new ApiError(404, "No verification code was found for this email");
  }

  if (activeOtp.expiresAt <= now) {
    await prisma.emailOtp.update({
      where: { id: activeOtp.id },
      data: {
        consumedAt: now
      }
    });

    throw new ApiError(400, "Verification code expired. Please request a new one");
  }

  if (activeOtp.attempts >= env.otpMaxAttempts) {
    await prisma.emailOtp.update({
      where: { id: activeOtp.id },
      data: {
        consumedAt: now
      }
    });

    throw new ApiError(429, "Too many invalid attempts. Please request a new code");
  }

  const expectedCodeHash = hashOtp(email, code);

  if (expectedCodeHash !== activeOtp.codeHash) {
    const nextAttempts = activeOtp.attempts + 1;

    await prisma.emailOtp.update({
      where: { id: activeOtp.id },
      data: {
        attempts: {
          increment: 1
        },
        consumedAt: nextAttempts >= env.otpMaxAttempts ? now : undefined
      }
    });

    throw new ApiError(
      nextAttempts >= env.otpMaxAttempts ? 429 : 400,
      nextAttempts >= env.otpMaxAttempts
        ? "Too many invalid attempts. Please request a new code"
        : "Invalid verification code"
    );
  }

  const conflictingUser = await prisma.user.findFirst({
    where: {
      email,
      NOT: {
        id: user.id
      }
    },
    select: {
      id: true
    }
  });

  if (conflictingUser) {
    throw new ApiError(409, "This email address is already linked to another account");
  }

  const verifiedUser = await prisma.$transaction(async (tx) => {
    await tx.emailOtp.update({
      where: { id: activeOtp.id },
      data: {
        consumedAt: now
      }
    });

    return tx.user.update({
      where: { id: user.id },
      data: {
        email,
        emailVerifiedAt: now,
        lastLoginAt: now
      }
    });
  });

  return res.status(200).json(
    buildSessionResponse(verifiedUser, {
      emailVerified: true
    })
  );
});
