/**
 * services/api.ts
 *
 * Central API client for the Nomad Adventure backend.
 *
 * Base URL:
 *  - iOS Simulator: http://localhost:4000
 *  - Physical device: replace with your LAN IP, e.g. http://192.168.1.XX:4000
 *    or set EXPO_PUBLIC_API_URL in your .env file.
 */

// ─── Config ───────────────────────────────────────────────────────────────────
const BASE_URL =
  (process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:4000').replace(/\/$/, '');

const DEFAULT_TIMEOUT_MS = 10_000;

// ─── Types ────────────────────────────────────────────────────────────────────
export interface Reward {
  id: string;
  name: string;
  description: string;
  imageUrl: string | null;
  previewPrefabKey: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Spot {
  id: string;
  name: string;
  description: string;
  latitude: number;
  longitude: number;
  distanceMeters?: number;
  radiusMeters: number;
  isActive: boolean;
  modelPrefabKey: string | null;
  createdAt: string;
  updatedAt: string;
  reward: Reward | null;
}

export interface User {
  id: string;
  deviceId: string;
  displayName: string;
  email: string | null;
  emailVerifiedAt: string | null;
  isEmailVerified: boolean;
  lastLoginAt: string;
  createdAt: string;
  updatedAt: string;
}

export interface Claim {
  id: string;
  userId: string;
  claimedAt: string;
  reward: Reward;
  tourismSpot: Spot;
}

export interface SessionResponse {
  user: User;
  accessToken: string;
}

export interface GuestLoginResponse extends SessionResponse {
  isNewUser: boolean;
}

export interface DemoLoginResponse extends SessionResponse {
  isNewUser: boolean;
}

export interface RequestEmailOtpResponse {
  email: string;
  maskedEmail: string;
  deliveryMethod: 'email' | 'console' | 'verified';
  expiresInMinutes?: number;
  alreadyVerified?: boolean;
  user?: User;
  accessToken?: string;
}

export interface SpotsResponse {
  spots: Spot[];
}

export interface SpotResponse {
  spot: Spot;
}

export interface ClaimResponse {
  claim: Claim;
  alreadyClaimed: boolean;
}

export interface ClaimLocationPayload {
  hasLocation: boolean;
  latitude: number;
  longitude: number;
  horizontalAccuracyMeters?: number;
}

export interface NearbySpotsParams {
  latitude: number;
  longitude: number;
  radiusMeters?: number;
  limit?: number;
}

export interface UserRewardsResponse {
  userId: string;
  rewards: Claim[];
}

export interface VerifyEmailOtpResponse extends SessionResponse {
  emailVerified: boolean;
}

let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

function buildQueryString(params: Record<string, string | number | undefined>): string {
  const parts = Object.entries(params)
    .filter(([, value]) => typeof value !== 'undefined')
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);

  return parts.length > 0 ? `?${parts.join('&')}` : '';
}

// ─── Core Fetcher ─────────────────────────────────────────────────────────────
async function request<T>(
  path: string,
  options: RequestInit = {},
  timeoutMs = DEFAULT_TIMEOUT_MS
): Promise<T> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const res = await fetch(`${BASE_URL}${path}`, {
      ...options,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...(options.headers ?? {}),
      },
    });

    clearTimeout(timer);

    if (!res.ok) {
      let message = `HTTP ${res.status}`;
      try {
        const err = await res.json();
        message = err?.error?.message ?? message;
      } catch {}
      throw new ApiError(message, res.status);
    }

    return (await res.json()) as T;
  } catch (err: any) {
    clearTimeout(timer);
    if (err.name === 'AbortError') throw new ApiError('Request timed out', 408);
    throw err;
  }
}

// ─── ApiError ─────────────────────────────────────────────────────────────────
export class ApiError extends Error {
  constructor(message: string, public statusCode?: number) {
    super(message);
    this.name = 'ApiError';
  }
}

// ─── Endpoints ────────────────────────────────────────────────────────────────

/** POST /auth/guest-login — create or reuse guest user */
export async function guestLogin(
  deviceId: string,
  displayName: string
): Promise<GuestLoginResponse> {
  return request<GuestLoginResponse>('/auth/guest-login', {
    method: 'POST',
    body: JSON.stringify({ deviceId, displayName }),
  });
}

/** POST /auth/demo-login — development-only email/password demo login */
export async function demoLogin(
  email: string,
  password: string
): Promise<DemoLoginResponse> {
  return request<DemoLoginResponse>('/auth/demo-login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

/** POST /auth/email/request-otp — send email verification code */
export async function requestEmailOtp(email: string): Promise<RequestEmailOtpResponse> {
  return request<RequestEmailOtpResponse>('/auth/email/request-otp', {
    method: 'POST',
    body: JSON.stringify({ email }),
  });
}

/** POST /auth/email/verify-otp — verify code and upgrade account */
export async function verifyEmailOtp(
  email: string,
  code: string
): Promise<VerifyEmailOtpResponse> {
  return request<VerifyEmailOtpResponse>('/auth/email/verify-otp', {
    method: 'POST',
    body: JSON.stringify({ email, code }),
  });
}

/** GET /spots — all active tourism spots */
export async function getSpots(): Promise<Spot[]> {
  const data = await request<SpotsResponse>('/spots');
  return data.spots;
}

/** GET /spots/nearby — active tourism spots near a player location */
export async function getNearbySpots(params: NearbySpotsParams): Promise<Spot[]> {
  const query = buildQueryString({
    latitude: params.latitude,
    longitude: params.longitude,
    radiusMeters: params.radiusMeters,
    limit: params.limit,
  });
  const data = await request<SpotsResponse>(`/spots/nearby${query}`);
  return data.spots;
}

/** GET /spots/:id — single spot */
export async function getSpot(spotId: string): Promise<Spot> {
  const data = await request<SpotResponse>(`/spots/${spotId}`);
  return data.spot;
}

/** POST /spots/:id/claim — claim reward for the authenticated user */
export async function claimSpotReward(
  spotId: string,
  location?: ClaimLocationPayload
): Promise<ClaimResponse> {
  return request<ClaimResponse>(`/spots/${spotId}/claim`, {
    method: 'POST',
    body: JSON.stringify(location ?? {}),
  });
}

/** GET /users/me/rewards — claimed rewards for the authenticated user */
export async function getUserRewards(): Promise<Claim[]> {
  const data = await request<UserRewardsResponse>('/users/me/rewards');
  return data.rewards;
}
