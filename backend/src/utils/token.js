import crypto from "crypto";
import { env } from "../config/env.js";

function base64UrlEncode(value) {
  return Buffer.from(value).toString("base64url");
}

function base64UrlDecode(value) {
  return Buffer.from(value, "base64url").toString("utf8");
}

function signSegment(value) {
  return crypto
    .createHmac("sha256", env.jwtSecret)
    .update(value)
    .digest("base64url");
}

export function createAccessToken(userId) {
  const issuedAt = Math.floor(Date.now() / 1000);
  const expiresAt = issuedAt + env.jwtExpiresInHours * 60 * 60;
  const header = base64UrlEncode(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const payload = base64UrlEncode(
    JSON.stringify({
      sub: userId,
      type: "access",
      iat: issuedAt,
      exp: expiresAt
    })
  );
  const signature = signSegment(`${header}.${payload}`);

  return `${header}.${payload}.${signature}`;
}

export function verifyAccessToken(token) {
  const [header, payload, signature] = token.split(".");

  if (!header || !payload || !signature) {
    throw new Error("Malformed token");
  }

  const expectedSignature = signSegment(`${header}.${payload}`);
  const actualBuffer = Buffer.from(signature, "utf8");
  const expectedBuffer = Buffer.from(expectedSignature, "utf8");

  if (
    actualBuffer.length !== expectedBuffer.length ||
    !crypto.timingSafeEqual(actualBuffer, expectedBuffer)
  ) {
    throw new Error("Invalid token signature");
  }

  const decodedHeader = JSON.parse(base64UrlDecode(header));
  const decodedPayload = JSON.parse(base64UrlDecode(payload));

  if (decodedHeader.alg !== "HS256" || decodedPayload.type !== "access") {
    throw new Error("Unsupported token");
  }

  if (typeof decodedPayload.exp !== "number" || decodedPayload.exp <= Math.floor(Date.now() / 1000)) {
    throw new Error("Token expired");
  }

  if (typeof decodedPayload.sub !== "string" || !decodedPayload.sub.trim()) {
    throw new Error("Invalid token subject");
  }

  return decodedPayload;
}
