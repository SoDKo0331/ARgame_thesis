/**
 * context/AppContext.tsx
 *
 * Global game state: current user session + spot data.
 * Persists userId to expo-constants sessionId as a stable identifier.
 */
import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';
import Constants from 'expo-constants';
import * as Location from 'expo-location';
import * as api from '@/services/api';
import type { User, Spot, Claim } from '@/services/api';

// ─── Types ───────────────────────────────────────────────────────────────────
interface AppState {
  user: User | null;
  spots: Spot[];
  nearbySpots: Spot[];
  rewards: Claim[];
  isAuthReady: boolean;
  isSigningIn: boolean;
  authError: string | null;
  spotsLoading: boolean;
  spotsError: string | null;
  nearbySpotsLoading: boolean;
  nearbySpotsError: string | null;
  rewardsLoading: boolean;
  rewardsError: string | null;
}

interface AppActions {
  initAuth: (displayName?: string) => Promise<void>;
  signInAsGuest: (displayName?: string) => Promise<void>;
  signInWithDemoCredentials: (email: string, password: string) => Promise<void>;
  clearAuthError: () => void;
  getSuggestedGuestDisplayName: () => string;
  fetchSpots: () => Promise<void>;
  fetchNearbySpots: () => Promise<void>;
  fetchRewards: () => Promise<void>;
  claimReward: (spotId: string) => Promise<api.ClaimResponse>;
  requestEmailVerification: (email: string) => Promise<api.RequestEmailOtpResponse>;
  verifyEmailCode: (email: string, code: string) => Promise<api.VerifyEmailOtpResponse>;
}

type AppContextValue = AppState & AppActions;

// ─── Context ─────────────────────────────────────────────────────────────────
const AppContext = createContext<AppContextValue | null>(null);

// ─── Provider ────────────────────────────────────────────────────────────────
export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [spots, setSpots] = useState<Spot[]>([]);
  const [nearbySpots, setNearbySpots] = useState<Spot[]>([]);
  const [rewards, setRewards] = useState<Claim[]>([]);
  const [isAuthReady, setAuthReady] = useState(true);
  const [isSigningIn, setIsSigningIn] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [spotsLoading, setSpotsLoading] = useState(false);
  const [spotsError, setSpotsError] = useState<string | null>(null);
  const [nearbySpotsLoading, setNearbySpotsLoading] = useState(false);
  const [nearbySpotsError, setNearbySpotsError] = useState<string | null>(null);
  const [rewardsLoading, setRewardsLoading] = useState(false);
  const [rewardsError, setRewardsError] = useState<string | null>(null);

  const applySession = useCallback((session: { user: User; accessToken: string }) => {
    api.setAccessToken(session.accessToken);
    setUser(session.user);
  }, []);

  /**
   * Stable device identifier using expo-constants installationId.
   * On physical iOS devices this is the IDFV-based identifier.
   */
  const getDeviceId = useCallback((): string => {
    return (
      Constants.sessionId ??   // expo-constants stable session id
      Constants.deviceName ??  // fallback device name
      'unknown-device'
    );
  }, []);

  const getSuggestedGuestDisplayName = useCallback((): string => {
    const rawDeviceId = getDeviceId();
    const compactCode = rawDeviceId.replace(/[^a-zA-Z0-9]/g, '').slice(-6).toUpperCase();
    return compactCode ? `Guest ${compactCode}` : 'Nomad Guest';
  }, [getDeviceId]);

  /**
   * Guest Login — creates or reuses backend user.
   * Called from the login screen.
   */
  const initAuth = useCallback(async (displayName?: string) => {
    const trimmedDisplayName = displayName?.trim();

    try {
      setIsSigningIn(true);
      setAuthReady(false);
      setAuthError(null);
      const deviceId = getDeviceId();
      const session = await api.guestLogin(
        deviceId,
        trimmedDisplayName || getSuggestedGuestDisplayName()
      );
      applySession(session);
    } catch (err: any) {
      const message = err?.message ?? 'Guest login failed';
      setAuthError(message);
      console.warn('[AppContext] Guest login failed:', message);
      throw err;
    } finally {
      setAuthReady(true);
      setIsSigningIn(false);
    }
  }, [applySession, getDeviceId, getSuggestedGuestDisplayName]);

  const signInWithDemoCredentials = useCallback(
    async (email: string, password: string) => {
      const normalizedEmail = email.trim().toLowerCase();
      const trimmedPassword = password.trim();

      try {
        setIsSigningIn(true);
        setAuthReady(false);
        setAuthError(null);
        const session = await api.demoLogin(normalizedEmail, trimmedPassword);
        applySession(session);
      } catch (err: any) {
        const message = err?.message ?? 'Demo login failed';
        setAuthError(message);
        console.warn('[AppContext] Demo login failed:', message);
        throw err;
      } finally {
        setAuthReady(true);
        setIsSigningIn(false);
      }
    },
    [applySession]
  );

  const clearAuthError = useCallback(() => {
    setAuthError(null);
  }, []);

  const getCurrentLocationPayload = useCallback(
    async (
      accuracy: Location.LocationAccuracy = Location.Accuracy.Balanced
    ): Promise<api.ClaimLocationPayload | undefined> => {
      try {
        let permission = await Location.getForegroundPermissionsAsync();
        if (!permission.granted && permission.canAskAgain) {
          permission = await Location.requestForegroundPermissionsAsync();
        }

        if (!permission.granted) {
          return undefined;
        }

        const lastKnownPosition = await Location.getLastKnownPositionAsync();
        const position =
          lastKnownPosition ??
          (await Location.getCurrentPositionAsync({
            accuracy,
          }));

        return {
          hasLocation: true,
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          horizontalAccuracyMeters: position.coords.accuracy ?? undefined,
        };
      } catch (locationError: any) {
        console.warn(
          '[AppContext] location read error:',
          locationError?.message ?? locationError
        );
        return undefined;
      }
    },
    []
  );

  /** Fetch all active tourism spots for profile, ledger, and account flows. */
  const fetchSpots = useCallback(async () => {
    setSpotsLoading(true);
    setSpotsError(null);
    try {
      const data = await api.getSpots();
      setSpots(data);
    } catch (err: any) {
      setSpotsError(err.message ?? 'Failed to load spots');
      console.warn('[AppContext] fetchSpots error:', err.message);
    } finally {
      setSpotsLoading(false);
    }
  }, []);

  /** Fetch nearby active spots for the map, otherwise fall back to all active spots. */
  const fetchNearbySpots = useCallback(async () => {
    setNearbySpotsLoading(true);
    setNearbySpotsError(null);
    try {
      const location = await getCurrentLocationPayload();
      let data: Spot[];

      if (location) {
        try {
          data = await api.getNearbySpots({
            latitude: location.latitude,
            longitude: location.longitude,
            radiusMeters: 5000,
            limit: 100,
          });
        } catch (nearbyError: any) {
          console.warn(
            '[AppContext] fetchNearbySpots fallback:',
            nearbyError?.message ?? nearbyError
          );
          data = await api.getSpots();
        }
      } else {
        data = await api.getSpots();
      }

      setNearbySpots(data);
    } catch (err: any) {
      setNearbySpotsError(err.message ?? 'Failed to load nearby spots');
      console.warn('[AppContext] fetchNearbySpots error:', err.message);
    } finally {
      setNearbySpotsLoading(false);
    }
  }, [getCurrentLocationPayload]);

  /** Fetch this user's claimed rewards */
  const fetchRewards = useCallback(async () => {
    if (!user) return;
    setRewardsLoading(true);
    setRewardsError(null);
    try {
      const data = await api.getUserRewards();
      setRewards(data);
    } catch (err: any) {
      setRewardsError(err.message ?? 'Failed to load rewards');
      console.warn('[AppContext] fetchRewards error:', err.message);
    } finally {
      setRewardsLoading(false);
    }
  }, [user]);

  /** Claim reward for a spot — throws on failure so the UI can show the real reason. */
  const claimReward = useCallback(
    async (spotId: string): Promise<api.ClaimResponse> => {
      if (!user) {
        console.warn('[AppContext] Cannot claim — no user logged in');
        throw new Error('No active session');
      }
      try {
        const locationPayload = await getCurrentLocationPayload(
          Location.Accuracy.BestForNavigation
        );
        const result = await api.claimSpotReward(spotId, locationPayload);
        // Optimistically refresh rewards after a successful claim
        fetchRewards();
        return result;
      } catch (err: any) {
        console.warn('[AppContext] claimReward error:', err.message);
        throw err;
      }
    },
    [user, fetchRewards, getCurrentLocationPayload]
  );

  const requestEmailVerification = useCallback(
    async (email: string): Promise<api.RequestEmailOtpResponse> => {
      const response = await api.requestEmailOtp(email);

      if (response.user && response.accessToken) {
        applySession({
          user: response.user,
          accessToken: response.accessToken,
        });
      }

      return response;
    },
    [applySession]
  );

  const verifyEmailCode = useCallback(
    async (email: string, code: string): Promise<api.VerifyEmailOtpResponse> => {
      const response = await api.verifyEmailOtp(email, code);
      applySession(response);
      return response;
    },
    [applySession]
  );

  return (
    <AppContext.Provider
      value={{
        user,
        spots,
        nearbySpots,
        rewards,
        isAuthReady,
        isSigningIn,
        authError,
        spotsLoading,
        spotsError,
        nearbySpotsLoading,
        nearbySpotsError,
        rewardsLoading,
        rewardsError,
        initAuth,
        signInAsGuest: initAuth,
        signInWithDemoCredentials,
        clearAuthError,
        getSuggestedGuestDisplayName,
        fetchSpots,
        fetchNearbySpots,
        fetchRewards,
        claimReward,
        requestEmailVerification,
        verifyEmailCode,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

// ─── Hook ─────────────────────────────────────────────────────────────────────
export function useApp(): AppContextValue {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useApp must be used inside <AppProvider>');
  return ctx;
}
