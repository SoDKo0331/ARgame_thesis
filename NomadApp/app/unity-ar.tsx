import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Animated,
  Linking,
  NativeModules,
  PermissionsAndroid,
  Platform,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import UnityView from '@azesmway/react-native-unity';
import { LinearGradient } from 'expo-linear-gradient';
import { Camera } from 'expo-camera';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import * as Location from 'expo-location';

import { Colors, Gradients, Spacing, Radius, Typography, Shadows } from '@/constants/theme';
import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';

const isUnityAvailable = !!NativeModules.RNUnityViewManager;
const UNITY_LOAD_TIMEOUT_MS = 45000;
const UNITY_PAYLOAD_RETRY_INTERVAL_MS = 1200;
const iosCameraPermissionModule = NativeModules.NomadCameraPermissionModule as
  | {
      getCameraPermissionStatus?: () => Promise<{
        granted?: boolean;
        canAskAgain?: boolean;
        status?: string;
      }>;
      requestCameraPermission?: () => Promise<{
        granted?: boolean;
        canAskAgain?: boolean;
        status?: string;
      }>;
    }
  | undefined;

export default function UnityARScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const {
    spotId,
    spotName,
    mode,
    rewardId,
    rewardName,
    rewardDescription,
    rewardImageUrl,
    previewPrefabKey,
    claimedAt,
    previewSpotName,
    spotDescription,
    spotLatitude,
    spotLongitude,
    spotRadiusMeters,
    modelPrefabKey,
    rewardPreviewPrefabKey,
  } = useLocalSearchParams<{
    spotId?: string;
    spotName?: string;
    mode?: string;
    rewardId?: string;
    rewardName?: string;
    rewardDescription?: string;
    rewardImageUrl?: string;
    previewPrefabKey?: string;
    claimedAt?: string;
    previewSpotName?: string;
    spotDescription?: string;
    spotLatitude?: string;
    spotLongitude?: string;
    spotRadiusMeters?: string;
    modelPrefabKey?: string;
    rewardPreviewPrefabKey?: string;
  }>();
  const { user, spots, nearbySpots, rewards, claimReward } = useApp();
  const { t } = useI18n();
  const unityRef = useRef<UnityView>(null);

  useEffect(() => {
    console.log('[UnityAR] Mounted. Params:', { mode, spotId, spotName, rewardId, rewardName });
  }, [mode, spotId, spotName, rewardId, rewardName]);

  const [isLoading, setIsLoading] = useState(true);
  const [isClaiming, setIsClaiming] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [loadErrorMessage, setLoadErrorMessage] = useState<string | null>(null);
  const [cameraPermissionGranted, setCameraPermissionGranted] = useState(
    Platform.OS === 'web' || !isUnityAvailable,
  );
  const [cameraPermissionResolved, setCameraPermissionResolved] = useState(
    Platform.OS === 'web' || !isUnityAvailable,
  );
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const loadTimer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const payloadRetryTimer = useRef<ReturnType<typeof setInterval>>(undefined);
  const hasReceivedUnityActivity = useRef(false);

  const resolveArContext = useCallback(async () => {
    try {
      let permission = await Location.getForegroundPermissionsAsync();
      if (!permission.granted && permission.canAskAgain) {
        permission = await Location.requestForegroundPermissionsAsync();
      }

      if (!permission.granted) {
        return null;
      }

      const lastKnownPosition = await Location.getLastKnownPositionAsync();
      const position =
        lastKnownPosition ??
        (await Location.getCurrentPositionAsync({
          accuracy: Location.Accuracy.Balanced,
        }));

      let headingDegrees: number | undefined;
      try {
        const heading = await Location.getHeadingAsync();
        const resolvedHeading =
          Number.isFinite(heading.trueHeading) && heading.trueHeading >= 0
            ? heading.trueHeading
            : heading.magHeading;

        if (Number.isFinite(resolvedHeading) && resolvedHeading >= 0) {
          headingDegrees = resolvedHeading;
        }
      } catch (headingError) {
        console.warn('[UnityARScreen] heading lookup error:', headingError);
      }

      return {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        horizontalAccuracyMeters: position.coords.accuracy ?? undefined,
        headingDegrees,
      };
    } catch (locationError) {
      console.warn('[UnityARScreen] location lookup error:', locationError);
      return null;
    }
  }, []);

  const buildUnityPayload = useCallback(async () => {
    if (mode === 'collection-preview') {
      return JSON.stringify({
        mode: 'collectionPreview',
        userId: user?.id ?? '',
        rewardId: rewardId ?? '',
        rewardName: rewardName ?? '',
        rewardDescription: rewardDescription ?? '',
        rewardImageUrl: rewardImageUrl ?? '',
        previewPrefabKey: previewPrefabKey ?? '',
        claimedAtRaw: claimedAt ?? '',
        spotName: previewSpotName ?? '',
        cameraPermissionGranted,
      });
    }

    const selectedSpot =
      nearbySpots.find((candidate) => candidate.id === spotId) ??
      spots.find((candidate) => candidate.id === spotId) ??
      rewards.find((claim) => claim.tourismSpot.id === spotId)?.tourismSpot ??
      null;

    const arContext = await resolveArContext();
    const parsedLatitude = spotLatitude ? Number(spotLatitude) : undefined;
    const parsedLongitude = spotLongitude ? Number(spotLongitude) : undefined;
    const parsedRadius = spotRadiusMeters ? Number(spotRadiusMeters) : undefined;

    return JSON.stringify({
      mode: 'spot',
      userId: user?.id ?? '',
      spotId: spotId ?? '',
      spotName: selectedSpot?.name ?? spotName ?? '',
      spotDescription: selectedSpot?.description ?? spotDescription ?? '',
      spotLatitude: selectedSpot?.latitude ?? parsedLatitude ?? 0,
      spotLongitude: selectedSpot?.longitude ?? parsedLongitude ?? 0,
      spotRadiusMeters: selectedSpot?.radiusMeters ?? parsedRadius ?? 0,
      modelPrefabKey: selectedSpot?.modelPrefabKey ?? modelPrefabKey ?? '',
      rewardId: selectedSpot?.reward?.id ?? rewardId ?? '',
      rewardName: selectedSpot?.reward?.name ?? rewardName ?? '',
      rewardDescription: selectedSpot?.reward?.description ?? rewardDescription ?? '',
      rewardImageUrl: selectedSpot?.reward?.imageUrl ?? rewardImageUrl ?? '',
      rewardPreviewPrefabKey:
        selectedSpot?.reward?.previewPrefabKey ??
        rewardPreviewPrefabKey ??
        previewPrefabKey ??
        '',
      previewPrefabKey:
        selectedSpot?.reward?.previewPrefabKey ??
        rewardPreviewPrefabKey ??
        previewPrefabKey ??
        '',
      hasCurrentLocation: !!arContext,
      currentLatitude: arContext?.latitude,
      currentLongitude: arContext?.longitude,
      currentHorizontalAccuracyMeters: arContext?.horizontalAccuracyMeters,
      hasCurrentHeading: typeof arContext?.headingDegrees === 'number',
      currentHeadingDegrees: arContext?.headingDegrees,
      cameraPermissionGranted,
    });
  }, [
    cameraPermissionGranted,
    mode,
    modelPrefabKey,
    nearbySpots,
    previewPrefabKey,
    previewSpotName,
    resolveArContext,
    rewardDescription,
    rewardId,
    rewardImageUrl,
    rewardName,
    rewardPreviewPrefabKey,
    rewards,
    spotDescription,
    claimedAt,
    spotId,
    spotLatitude,
    spotLongitude,
    spotName,
    spotRadiusMeters,
    spots,
    user?.id,
  ]);

  const sendUnityPayload = useCallback(async () => {
    if (!unityRef.current) {
      return false;
    }

    const payload = await buildUnityPayload();
    console.log(`[RN→Unity] Sending payload (mode: ${mode}):`, JSON.parse(payload));
    unityRef.current.postMessage('NativeBridgeManager', 'ReceiveSpotDataFromRN', payload);
    return true;
  }, [buildUnityPayload, mode]);

  const stopPayloadRetryLoop = useCallback(() => {
    if (payloadRetryTimer.current) {
      clearInterval(payloadRetryTimer.current);
      payloadRetryTimer.current = undefined;
    }
  }, []);

  const armLoadTimeout = useCallback(() => {
    if (loadTimer.current) {
      clearTimeout(loadTimer.current);
    }

    if (!isLoading || loadError) {
      return;
    }

    loadTimer.current = setTimeout(() => {
      stopPayloadRetryLoop();
      setLoadErrorMessage(t('unity.failedLoadBody'));
      setLoadError(true);
    }, UNITY_LOAD_TIMEOUT_MS);
  }, [isLoading, loadError, stopPayloadRetryLoop, t]);

  // Give Unity enough time to boot, load ARScene, and either respond with a real
  // ready/error message or hit its own internal AR timeout.
  useEffect(() => {
    if (!isLoading || !cameraPermissionResolved) return;

    armLoadTimeout();

    return () => {
      if (loadTimer.current) clearTimeout(loadTimer.current);
    };
  }, [armLoadTimeout, cameraPermissionResolved, isLoading]);

  // Begin sending payload only after Unity is fully ready
  const startPayloadPumping = useCallback(() => {
    if (payloadRetryTimer.current) return;

    // Send immediately once the native Unity view is mounted.
    void sendUnityPayload();

    // Keep retrying until Unity acknowledges that it received the payload.
    payloadRetryTimer.current = setInterval(() => {
      if (hasReceivedUnityActivity.current || loadError) {
        stopPayloadRetryLoop();
        return;
      }
      void sendUnityPayload();
    }, UNITY_PAYLOAD_RETRY_INTERVAL_MS);
  }, [loadError, sendUnityPayload, stopPayloadRetryLoop]);

  const markReady = useCallback(() => {
    if (loadTimer.current) clearTimeout(loadTimer.current);
    setLoadError(false);
    setLoadErrorMessage(null);
    setIsLoading(false);
    Animated.timing(fadeAnim, { toValue: 1, duration: 400, useNativeDriver: true }).start();
  }, [fadeAnim]);

  useEffect(() => {
    if (!cameraPermissionGranted) return;
    hasReceivedUnityActivity.current = false;
    return () => {
      stopPayloadRetryLoop();
    };
  }, [cameraPermissionGranted, stopPayloadRetryLoop]);

  useEffect(() => {
    let cancelled = false;

    const resolveIosCameraPermission = async () => {
      try {
        if (
          iosCameraPermissionModule?.getCameraPermissionStatus &&
          iosCameraPermissionModule?.requestCameraPermission
        ) {
          let permission = await iosCameraPermissionModule.getCameraPermissionStatus();
          console.log('[UnityARScreen] Native iOS camera permission status:', permission);

          if (!permission.granted && permission.canAskAgain !== false) {
            permission = await iosCameraPermissionModule.requestCameraPermission();
            console.log('[UnityARScreen] Native iOS camera permission request result:', permission);
          }

          if (permission.granted) {
            return true;
          }

          if (permission.canAskAgain === false) {
            return false;
          }
        }
      } catch (nativePermissionError) {
        console.warn('[UnityARScreen] Native iOS camera permission fallback error:', nativePermissionError);
      }

      let permission = await Camera.getCameraPermissionsAsync();
      console.log('[UnityARScreen] Expo camera permission status:', permission);

      if (!permission.granted && permission.canAskAgain) {
        permission = await Camera.requestCameraPermissionsAsync();
        console.log('[UnityARScreen] Expo camera permission request result:', permission);
      }

      return permission.granted;
    };

    const ensureCameraPermission = async () => {
      try {
        if (!isUnityAvailable || Platform.OS === 'web') {
          if (!cancelled) {
            setCameraPermissionGranted(true);
            setCameraPermissionResolved(true);
          }
          return;
        }

        if (Platform.OS === 'android') {
          const alreadyGranted = await PermissionsAndroid.check(PermissionsAndroid.PERMISSIONS.CAMERA);
          if (cancelled) {
            return;
          }

          if (alreadyGranted) {
            setCameraPermissionGranted(true);
            setCameraPermissionResolved(true);
            return;
          }

          const result = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.CAMERA);
          if (cancelled) {
            return;
          }

          if (result === PermissionsAndroid.RESULTS.GRANTED) {
            setCameraPermissionGranted(true);
            setCameraPermissionResolved(true);
            return;
          }

          setCameraPermissionGranted(false);
          setCameraPermissionResolved(true);
          Alert.alert(
            t('unity.cameraPermissionTitle'),
            t('unity.cameraPermissionBody'),
            [{ text: t('unity.cameraPermissionButton'), onPress: () => router.back() }],
          );
          return;
        }

        const granted = await resolveIosCameraPermission();
        if (cancelled) {
          return;
        }

        if (granted) {
          setCameraPermissionGranted(true);
          setCameraPermissionResolved(true);
          return;
        }

        setCameraPermissionGranted(false);
        setCameraPermissionResolved(true);
        Alert.alert(t('unity.cameraPermissionTitle'), t('unity.cameraPermissionBody'), [
          {
            text: t('unity.cameraPermissionButton'),
            onPress: () => {
              void Linking.openSettings().catch(() => router.back());
            },
          },
        ]);
      } catch (error) {
        console.warn('[UnityARScreen] camera permission error:', error);
        if (!cancelled) {
          setLoadErrorMessage(t('unity.failedCameraPermissionBody'));
          setLoadError(true);
          setCameraPermissionResolved(true);
        }
      }
    };

    ensureCameraPermission();

    return () => {
      cancelled = true;
    };
  }, [router, t]);

  useEffect(() => {
    if (!isUnityAvailable || !cameraPermissionResolved || !cameraPermissionGranted || loadError || !isLoading) {
      return;
    }

    startPayloadPumping();
  }, [
    cameraPermissionGranted,
    cameraPermissionResolved,
    isLoading,
    loadError,
    startPayloadPumping,
  ]);

  const getUnityLoadErrorMessage = useCallback((code?: string, message?: string) => {
    switch (code) {
      case 'camera_permission_denied':
        return t('unity.failedCameraPermissionBody');
      case 'camera_frame_timeout':
        return t('unity.failedCameraTimeoutBody');
      case 'ar_unsupported':
        return t('unity.failedARUnsupportedBody');
      case 'ar_setup_missing':
        return t('unity.failedARSetupBody');
      case 'ar_availability_timeout':
        return t('unity.failedARAvailabilityBody');
      default:
        return message?.trim() || t('unity.failedLoadBody');
    }
  }, [t]);

  const handleUnityMessage = (event: any) => {
    const raw: string = event?.nativeEvent?.message ?? '';
    console.log('[Unity→RN]', raw);
    try {
      const data = JSON.parse(raw);
      
      switch (data.status) {
        case 'payload_received':
        case 'payload_received_duplicate':
          console.log('[RN] Unity acknowledged payload.');
          hasReceivedUnityActivity.current = true;
          stopPayloadRetryLoop();
          return;
        case 'loading_scene':
        case 'reloading_scene':
        case 'ar_initializing':
        case 'requesting_camera_permission':
        case 'camera_permission_pending':
        case 'waiting_for_camera_frame':
        case 'native_unity_initialized':
          hasReceivedUnityActivity.current = true;
          armLoadTimeout();
          console.log('[Unity→RN] Progress update:', data.status);
          if (data.status === 'native_unity_initialized') {
            void sendUnityPayload();
          }
          return;
        case 'ready':
          console.log('[Unity→RN] Unity AR scene is READY.');
          markReady();
          return;
        case 'error':
          console.warn('[Unity→RN] ERROR received:', data);
          if (loadTimer.current) clearTimeout(loadTimer.current);
          stopPayloadRetryLoop();
          setLoadErrorMessage(getUnityLoadErrorMessage(data.code, data.message));
          setLoadError(true);
          return;
        case 'collected':
          console.log('[Unity→RN] Reward collected signal.');
          if (mode !== 'collection-preview') handleClaim();
          return;
        default:
          console.log('[Unity→RN] Unhandled status:', data.status);
          return;
      }
    } catch {
      console.warn('[Unity→RN] Failed to parse message:', raw);
      if (raw.toLowerCase().includes('ready')) markReady();
    }
  };

  const getClaimErrorMessage = useCallback((error: unknown) => {
    const fallbackMessage = t('unity.alertClaimFailedBody');
    const rawMessage = error instanceof Error ? error.message.trim() : '';

    if (!rawMessage) {
      return fallbackMessage;
    }

    const moveCloserMatch = /^Move closer to (.+) to collect this reward\.?$/i.exec(rawMessage);
    if (moveCloserMatch) {
      return t('unity.claimMoveCloser', { spotName: moveCloserMatch[1] });
    }

    if (/Current location is required to claim this reward/i.test(rawMessage)) {
      return t('unity.claimLocationRequired');
    }

    return rawMessage;
  }, [t]);

  // ── Real Claim Call ───────────────────────────────────────────────────────
  const handleClaim = async () => {
    if (!spotId || isClaiming) return;
    setIsClaiming(true);
    try {
      const result = await claimReward(spotId);

      const rewardName = result.claim.reward?.name ?? t('unity.rewardFallback');
      const alreadyHad = result.alreadyClaimed;

      Alert.alert(
        alreadyHad ? t('unity.alertAlreadyCollectedTitle') : t('unity.alertRewardCollectedTitle'),
        alreadyHad
          ? t('unity.alertAlreadyCollectedBody', { rewardName })
          : t('unity.alertRewardCollectedBody', { rewardName }),
        [{ text: t('unity.alertBackToMap'), onPress: () => router.back() }]
      );
    } catch (error) {
      Alert.alert(t('unity.alertClaimFailedTitle'), getClaimErrorMessage(error));
    } finally {
      setIsClaiming(false);
    }
  };

  return (
    <View style={styles.container}>
      {isUnityAvailable ? (
        cameraPermissionGranted ? (
          <UnityView
            ref={unityRef}
            style={StyleSheet.absoluteFillObject}
            onUnityMessage={handleUnityMessage}
          />
        ) : (
          <View style={[StyleSheet.absoluteFillObject, styles.permissionAwaitingBackground]} />
        )
      ) : (
        <View style={[StyleSheet.absoluteFillObject, { backgroundColor: '#1a1a1a', justifyContent: 'center', alignItems: 'center' }]}>
          <LinearGradient colors={['#1a1a1a', '#0a0a0a']} style={StyleSheet.absoluteFillObject} />
          <Ionicons name="cube-outline" size={120} color={Colors.cyanDim} style={{ opacity: 0.2 }} />
          <Text style={{ color: Colors.textSecondary, marginTop: 20, textAlign: 'center', paddingHorizontal: 40 }}>
            {t('unity.simulatorTitle')}
            {'\n'}
            ({t('unity.simulatorMode')})
          </Text>
          <TouchableOpacity
            style={{ marginTop: 40, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: Colors.cyanDim, borderRadius: Radius.md }}
            onPress={() => markReady()}
          >
            <Text style={{ color: Colors.cyan, fontWeight: 'bold' }}>{t('unity.simulatorButton')}</Text>
          </TouchableOpacity>
        </View>
      )}

      {/* ── Loading Overlay ─────────────────────────────────────────────── */}
      {isLoading && !loadError && (
        <View style={styles.overlay}>
          <LinearGradient colors={[Colors.bg, '#0d1225']} style={StyleSheet.absoluteFillObject} />
          <View style={styles.centeredContent}>
            <View style={[styles.iconRing, Shadows.glow(Colors.cyan)]}>
              <Ionicons name="game-controller" size={36} color={Colors.cyan} />
            </View>
            <Text style={styles.loadingTitle}>
              {mode === 'collection-preview' && rewardName
                ? t('unity.enteringRewardPreview', { rewardName: rewardName ?? '' })
                : spotName
                ? t('unity.enteringSpot', { spotName: spotName ?? '' })
                : t('unity.enteringWorld')}
            </Text>
            <Text style={styles.loadingSubtitle}>{t('unity.initializing')}</Text>
            <ActivityIndicator size="small" color={Colors.cyan} style={{ marginTop: Spacing.lg }} />
          </View>
        </View>
      )}

      {/* ── Error Overlay ───────────────────────────────────────────────── */}
      {loadError && (
        <View style={styles.overlay}>
          <LinearGradient colors={[Colors.bg, '#1a0a0a']} style={StyleSheet.absoluteFillObject} />
          <View style={styles.centeredContent}>
            <Ionicons name="warning-outline" size={48} color={Colors.red} />
            <Text style={[styles.loadingTitle, { color: Colors.red }]}>{t('unity.failedLoad')}</Text>
            <Text style={styles.loadingSubtitle}>{loadErrorMessage ?? t('unity.failedLoadBody')}</Text>
            <TouchableOpacity style={styles.errorBtn} onPress={() => router.back()}>
              <Text style={styles.errorBtnText}>{t('common.backToMap')}</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}

      {/* ── HUD (visible after Unity loads) ────────────────────────────── */}
      {!isLoading && !loadError && (
        <Animated.View style={[styles.hud, { opacity: fadeAnim }]}>
          {/* Back */}
          <TouchableOpacity
            style={[styles.backBtn, { top: insets.top + Spacing.sm }]}
            onPress={() => router.back()}
            activeOpacity={0.8}
          >
            <LinearGradient colors={['rgba(10,14,24,0.85)', 'rgba(10,14,24,0.75)']} style={styles.backBtnInner}>
              <Ionicons name="arrow-back" size={22} color="#fff" />
            </LinearGradient>
          </TouchableOpacity>

          {/* Spot Name Pill */}
          <View style={[styles.namePill, { top: insets.top + Spacing.sm }]}>
            <View style={styles.arDot} />
            <Text style={styles.namePillText} numberOfLines={1}>
              {mode === 'collection-preview'
                ? rewardName ?? t('unity.arView')
                : spotName ?? t('unity.arView')}
            </Text>
          </View>

          {/* ── Collection Preview HUD ── */}
          {mode === 'collection-preview' && (
            <View style={styles.collectionHud} pointerEvents="box-none">
              <View style={[styles.collectionCard, Shadows.card]}>
                <View style={styles.collectionCardHeader}>
                  <View style={styles.collectionStamp}>
                    <Text style={styles.collectionStampText}>{t('inventory.headerStamp')}</Text>
                  </View>
                  <Text style={styles.collectionTitle}>{rewardName}</Text>
                  <Text style={styles.collectionSubtitle}>
                    {t('inventory.collectedFrom', { spot: previewSpotName ?? '' })}
                  </Text>
                </View>
                <Text style={styles.collectionDesc}>{rewardDescription}</Text>
                <View style={styles.collectionFooter}>
                  <Ionicons name="calendar-outline" size={12} color={Colors.surge} />
                  <Text style={styles.collectionDate}>
                    {t('inventory.collectedOn', { 
                      date: claimedAt ? new Date(claimedAt).toLocaleDateString() : new Date().toLocaleDateString() 
                    })}
                  </Text>
                </View>
              </View>
            </View>
          )}

          {/* Bottom Collect Button */}
          {mode !== 'collection-preview' ? (
            <View style={[styles.bottomControls, { paddingBottom: insets.bottom + Spacing.base }]}>
              <LinearGradient colors={['rgba(10,14,24,0)', 'rgba(10,14,24,0.9)']} style={styles.bottomGradient} />
              <TouchableOpacity
                style={[styles.captureBtn, isClaiming && styles.captureBtnDisabled]}
                onPress={handleClaim}
                disabled={isClaiming}
                activeOpacity={0.85}
              >
                <LinearGradient colors={Gradients.green} style={styles.captureBtnInner}>
                  {isClaiming
                    ? <ActivityIndicator size="small" color="#fff" />
                    : <Ionicons name="hand-right-outline" size={22} color="#fff" />
                  }
                  <Text style={styles.captureBtnText}>
                    {isClaiming ? t('unity.claiming') : t('unity.collectReward')}
                  </Text>
                </LinearGradient>
              </TouchableOpacity>
            </View>
          ) : null}
        </Animated.View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.bg },
  permissionAwaitingBackground: { backgroundColor: '#0d1225' },

  overlay: { ...StyleSheet.absoluteFillObject, justifyContent: 'center', alignItems: 'center' },
  centeredContent: { alignItems: 'center', gap: Spacing.md, paddingHorizontal: Spacing.xxxl },
  iconRing: {
    width: 88, height: 88, borderRadius: 44, backgroundColor: Colors.cyanDim,
    borderWidth: 2, borderColor: Colors.cyan, justifyContent: 'center', alignItems: 'center', marginBottom: Spacing.sm,
  },
  loadingTitle: { ...Typography.displayMd, color: Colors.textPrimary, textAlign: 'center' },
  loadingSubtitle: { ...Typography.body, color: Colors.textSecondary, textAlign: 'center', lineHeight: 22 },
  errorBtn: {
    marginTop: Spacing.lg, paddingHorizontal: Spacing.xl, paddingVertical: Spacing.md,
    borderRadius: Radius.full, borderWidth: 1, borderColor: Colors.red, backgroundColor: Colors.redDim,
  },
  errorBtnText: { ...Typography.label, color: Colors.red },

  hud: { ...StyleSheet.absoluteFillObject, pointerEvents: 'box-none' },
  backBtn: { position: 'absolute', left: Spacing.lg, borderRadius: Radius.full, overflow: 'hidden', ...Shadows.card },
  backBtnInner: {
    width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center',
    borderWidth: 1, borderColor: 'rgba(255,255,255,0.15)',
  },
  namePill: {
    position: 'absolute', right: Spacing.lg,
    flexDirection: 'row', alignItems: 'center', gap: 6,
    backgroundColor: 'rgba(10,14,24,0.8)', paddingHorizontal: Spacing.md, paddingVertical: 8,
    borderRadius: Radius.full, borderWidth: 1, borderColor: 'rgba(56,239,125,0.3)', maxWidth: '55%',
  },
  arDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: Colors.green, flexShrink: 0 },
  namePillText: { ...Typography.caption, color: Colors.green, letterSpacing: 0.8 },

  bottomControls: { position: 'absolute', bottom: 0, left: 0, right: 0, alignItems: 'center' },
  bottomGradient: { position: 'absolute', bottom: 0, left: 0, right: 0, height: 160 },
  captureBtn: { borderRadius: Radius.full, overflow: 'hidden', ...Shadows.glow(Colors.green) },
  captureBtnDisabled: { opacity: 0.7 },
  captureBtnInner: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.sm,
    paddingHorizontal: Spacing.xl, paddingVertical: Spacing.base,
    minWidth: 200, justifyContent: 'center',
  },
  captureBtnText: { ...Typography.body, color: '#fff', fontWeight: '800' },
  collectionHud: {
    position: 'absolute',
    bottom: 40,
    left: Spacing.lg,
    right: Spacing.lg,
  },
  collectionCard: {
    backgroundColor: 'rgba(10,14,24,0.85)',
    borderWidth: 2,
    borderColor: 'rgba(56,239,125,0.4)',
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    gap: Spacing.sm,
  },
  collectionCardHeader: {
    gap: 4,
  },
  collectionStamp: {
    position: 'absolute',
    top: -24,
    right: -10,
    paddingHorizontal: 12,
    paddingVertical: 6,
    backgroundColor: Colors.surge,
    borderWidth: 2,
    borderColor: Colors.paper,
    borderRadius: Radius.full,
    transform: [{ rotate: '8deg' }],
  },
  collectionStampText: {
    ...Typography.caption,
    color: Colors.paper,
    fontWeight: '900',
    fontSize: 10,
  },
  collectionTitle: {
    ...Typography.displayMd,
    color: Colors.paper,
    fontSize: 22,
  },
  collectionSubtitle: {
    ...Typography.caption,
    color: Colors.surge,
    fontWeight: '800',
  },
  collectionDesc: {
    ...Typography.body,
    color: 'rgba(238, 238, 238, 0.8)',
    fontSize: 13,
    lineHeight: 18,
  },
  collectionFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 4,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: 'rgba(238, 238, 238, 0.1)',
  },
  collectionDate: {
    ...Typography.caption,
    color: 'rgba(238, 238, 238, 0.5)',
  },
});
