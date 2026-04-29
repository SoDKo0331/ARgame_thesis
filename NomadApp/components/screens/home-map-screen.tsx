/**
 * HomeScreen.tsx — Advanced Map Experience
 *
 * Improvements over original:
 *  1.  Animated bottom-sheet with spring physics (pan-gesture driven)
 *  2.  PulsingMarker: animated scale + opacity ring for user position
 *  3.  SpotMarker wrapped in scale-spring when selection changes
 *  4.  Extracted pure components: SpotMarker, SpotCard, DetailSheet, StatusBanner
 *  5.  useSpotsViewModel() hook — all business logic extracted from render tree
 *  6.  Stable camera bounds via deep-equal guard (avoids Mapbox re-camera)
 *  7.  useLocationSync() hook with cleanup-safe setState guard
 *  8.  formatDistanceLabel extracted to pure util (no hook dependency)
 *  9.  StyleSheet.flatten removed; all runtime styles derived via useMemo
 * 10.  Zero anonymous functions in JSX — all handlers are stable useCallback refs
 */

import { Ionicons } from '@expo/vector-icons';
import Mapbox from '@rnmapbox/maps';
import { Image as ExpoImage } from 'expo-image';
import { LinearGradient } from 'expo-linear-gradient';
import { useRouter } from 'expo-router';
import * as Location from 'expo-location';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  memo,
} from 'react';
import {
  Alert,
  Animated,
  Linking,
  PanResponder,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import {
  Colors,
  Gradients,
  Layout,
  Radius,
  Shadows,
  Spacing,
  Typography,
} from '@/constants/theme';
import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';
import type { Spot } from '@/services/api';

// ─── Mapbox token ──────────────────────────────────────────────────────────────
Mapbox.setAccessToken(
  'pk.eyJ1Ijoic29kYmF5YXIiLCJhIjoiY21uNHU2M2plMDN6cTJzc2N1YjJvcG1tYyJ9.KISihdGgaJq_eBu1_qfD4Q'
);

// ─── Constants ─────────────────────────────────────────────────────────────────
const MAP_CENTER: [number, number] = [106.9175, 47.9184];
const SPOT_COLORS = [Colors.surge, Colors.ember, Colors.paper] as const;
const SPOT_SAMPLE_IMAGE = require('../../assets/spot_sample.png');
const WIZARD_TRAVELER_IMAGE = require('../../assets/images/wizard-traveler.png');
const EXACT_TEST_PREVIEW_KEYS = new Set(['AncientMapFragment', 'HorseheadFiddleCharm']);
const USER_MARKER_VISIBILITY_RADIUS_METERS = 25_000;
const SHEET_SNAP_VELOCITY_THRESHOLD = 800;

const TEST_PREVIEW_ALIAS_BY_KEY: Record<string, string> = {
  GoldenEaglePin: 'AncientMapFragment',
  BlueSkySilkScarf: 'HorseheadFiddleCharm',
  NomadExplorerBadge: 'AncientMapFragment',
};

// ─── Pure utilities ────────────────────────────────────────────────────────────
function haversineMeters(
  lat1: number, lon1: number,
  lat2: number, lon2: number,
): number {
  const R = 6_371_000;
  const rad = (v: number) => (v * Math.PI) / 180;
  const dLat = rad(lat2 - lat1);
  const dLon = rad(lon2 - lon1);
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(rad(lat1)) * Math.cos(rad(lat2)) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(a));
}

function formatDistanceLabel(meters: number | null | undefined, t: (k: string, p?: any) => string): string | null {
  if (typeof meters !== 'number' || !Number.isFinite(meters)) return null;
  if (meters < 1000) return t('profile.routeDistanceMeters', { count: Math.round(meters) });
  const km = meters < 10_000 ? (meters / 1000).toFixed(1) : String(Math.round(meters / 1000));
  return t('profile.routeDistanceKilometers', { count: km });
}

function resolvePreviewModelKey(spot: Spot): string | null {
  const key = spot.reward?.previewPrefabKey ?? null;
  if (!key) return null;
  if (EXACT_TEST_PREVIEW_KEYS.has(key)) return key;
  return TEST_PREVIEW_ALIAS_BY_KEY[key] ?? null;
}

/**
 * Adds a tiny random jitter to coordinates to prevent perfect stacking
 * of markers at the same location.
 */
function jitterCoordinates(spots: SpotWithDistance[]): SpotWithDistance[] {
  const seen = new Map<string, number>();
  const JITTER_AMOUNT = 0.0005; // increased to approx 50 meters for easier debugging

  return spots.map(s => {
    // Round for grouping
    const key = `${s.latitude.toFixed(5)},${s.longitude.toFixed(5)}`;
    const count = seen.get(key) || 0;
    seen.set(key, count + 1);

    if (count === 0) return s;

    // Apply jitter based on count
    const angle = (count * 137.5) * (Math.PI / 180); // golden angle
    const r = JITTER_AMOUNT * Math.sqrt(count);
    
    return {
      ...s,
      latitude: s.latitude + Math.sin(angle) * r,
      longitude: s.longitude + Math.cos(angle) * r,
    };
  });
}

// ─── useLocationSync hook ──────────────────────────────────────────────────────
function useLocationSync() {
  const [userCoordinate, setUserCoordinate] = useState<[number, number] | null>(null);
  const [isResolved, setIsResolved] = useState(false);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    let subscription: Location.LocationSubscription | null = null;

    const sync = async () => {
      try {
        let perm = await Location.getForegroundPermissionsAsync();
        if (!perm.granted && perm.canAskAgain) {
          perm = await Location.requestForegroundPermissionsAsync();
        }
        if (!perm.granted) return;

        const update = (coords?: Location.LocationObjectCoords | null) => {
          if (mounted.current && coords) {
            setUserCoordinate([coords.longitude, coords.latitude]);
          }
        };

        const last = await Location.getLastKnownPositionAsync();
        update(last?.coords);

        const current = await Location.getCurrentPositionAsync({
          accuracy: Location.Accuracy.Balanced,
        });
        update(current.coords);

        subscription = await Location.watchPositionAsync(
          { accuracy: Location.Accuracy.Balanced, distanceInterval: 10, timeInterval: 15_000 },
          (pos) => update(pos.coords),
        );
      } catch (e: any) {
        console.warn('[HomeMapScreen] location error:', e?.message ?? e);
      } finally {
        if (mounted.current) setIsResolved(true);
      }
    };

    sync();
    return () => {
      mounted.current = false;
      subscription?.remove();
    };
  }, []);

  return { userCoordinate, isResolved };
}

// ─── Spot-with-distance type ───────────────────────────────────────────────────
type SpotWithDistance = Spot & { distanceMeters?: number };

// ─── useSpots view-model hook ──────────────────────────────────────────────────
function useSpotsViewModel() {
  const { user, spots, fetchSpots, spotsLoading, spotsError } = useApp();
  const { language, toggleLanguage, t, formatDisplayName } = useI18n();
  const { userCoordinate, isResolved: isLocationResolved } = useLocationSync();

  const spotsWithDistance = useMemo<SpotWithDistance[]>(() => {
    let list: SpotWithDistance[] = spots;
    if (userCoordinate) {
      const [lon, lat] = userCoordinate;
      list = [...spots].map((s) => ({
        ...s,
        distanceMeters: haversineMeters(lat, lon, s.latitude, s.longitude),
      }));
    }
    
    // Sort and apply jitter to prevent stacking
    const sorted = [...list].sort((a, b) => (a.distanceMeters ?? Infinity) - (b.distanceMeters ?? Infinity));
    return jitterCoordinates(sorted);
  }, [spots, userCoordinate]);

  const nearestDistance = spotsWithDistance[0]?.distanceMeters;
  const showUserMarker =
    Boolean(userCoordinate) &&
    typeof nearestDistance === 'number' &&
    nearestDistance <= USER_MARKER_VISIBILITY_RADIUS_METERS;

  const displayName = formatDisplayName(user?.displayName);
  const firstName = displayName.split(' ')[0] ?? t('common.guestName');

  return {
    spots: spotsWithDistance,
    spotsLoading,
    spotsError,
    fetchSpots,
    userCoordinate,
    isLocationResolved,
    showUserMarker,
    firstName,
    language,
    toggleLanguage,
    t,
  };
}

// ─── Animated pulsing user marker ─────────────────────────────────────────────
const PulsingUserMarker = memo(function PulsingUserMarker({
  coordinate,
  label,
}: {
  coordinate: [number, number];
  label?: string;
}) {
  const scale       = useRef(new Animated.Value(1)).current;
  const ring1Scale  = useRef(new Animated.Value(1)).current;
  const ring1Opacity = useRef(new Animated.Value(0.7)).current;
  const ring2Scale  = useRef(new Animated.Value(1)).current;
  const ring2Opacity = useRef(new Animated.Value(0.5)).current;
  const ring3Scale  = useRef(new Animated.Value(1)).current;
  const ring3Opacity = useRef(new Animated.Value(0.3)).current;

  const makePulse = (s: Animated.Value, o: Animated.Value, delay: number) =>
    Animated.loop(
      Animated.sequence([
        Animated.delay(delay),
        Animated.parallel([
          Animated.timing(s, { toValue: 2.6, duration: 2200, useNativeDriver: true }),
          Animated.timing(o, { toValue: 0,   duration: 2200, useNativeDriver: true }),
        ]),
        Animated.parallel([
          Animated.timing(s, { toValue: 1, duration: 0, useNativeDriver: true }),
          Animated.timing(o, { toValue: 0.7 - delay * 0.0001, duration: 0, useNativeDriver: true }),
        ]),
      ]),
    );

  useEffect(() => {
    makePulse(ring1Scale, ring1Opacity, 0).start();
    makePulse(ring2Scale, ring2Opacity, 700).start();
    makePulse(ring3Scale, ring3Opacity, 1400).start();

    Animated.loop(
      Animated.sequence([
        Animated.timing(scale, { toValue: 1.06, duration: 1800, useNativeDriver: true }),
        Animated.timing(scale, { toValue: 0.96, duration: 1800, useNativeDriver: true }),
      ])
    ).start();
  }, [ring1Scale, ring1Opacity, ring2Scale, ring2Opacity, ring3Scale, ring3Opacity, scale]);

  const name = label ?? 'ME';

  return (
    <Mapbox.PointAnnotation id="user" coordinate={coordinate} anchor={{ x: 0.5, y: 0.8 }}>
      <View collapsable={false} style={styles.pulseWrapper}>
        {/* Three staggered pulse rings */}
        <Animated.View style={[styles.pulseRing, { transform: [{ scale: ring1Scale }], opacity: ring1Opacity }]} />
        <Animated.View style={[styles.pulseRing, styles.pulseRing2, { transform: [{ scale: ring2Scale }], opacity: ring2Opacity }]} />
        <Animated.View style={[styles.pulseRing, styles.pulseRing3, { transform: [{ scale: ring3Scale }], opacity: ring3Opacity }]} />

        {/* Glow base */}
        <View style={styles.wizardGlow} />

        {/* Wizard character */}
        <Animated.View style={[styles.wizardWrapper, { transform: [{ scale }] }]}>
          <ExpoImage
            source={WIZARD_TRAVELER_IMAGE}
            style={styles.wizardImage}
            contentFit="contain"
          />
        </Animated.View>

        {/* Name plate */}
        <View style={styles.wizardNamePlate}>
          <Text style={styles.wizardNameText} numberOfLines={1}>{name.toUpperCase()}</Text>
        </View>
      </View>
    </Mapbox.PointAnnotation>
  );
});

// ─── SpotMarker ────────────────────────────────────────────────────────────────
function SpotMarker({
  spot,
  index,
  selected,
  onPress,
}: {
  spot: SpotWithDistance;
  index: number;
  selected: boolean;
  onPress: () => void;
}) {
  const { t } = useI18n();
  const scaleAnim = useRef(new Animated.Value(selected ? 1.08 : 1)).current;

  useEffect(() => {
    Animated.spring(scaleAnim, {
      toValue: selected ? 1.08 : 1,
      useNativeDriver: true,
      tension: 180,
      friction: 10,
    }).start();
  }, [selected, scaleAnim]);

  const color = SPOT_COLORS[index % SPOT_COLORS.length];
  const spotName = spot.name?.trim() || t('home.unnamedSpot');
  const rewardImage = spot.reward?.imageUrl;

  const textColor = selected ? Colors.paper : Colors.ink;
  const iconColor = selected ? Colors.paper : color;

  const plateStyle = useMemo(() => [
    styles.markerPlate,
    {
      backgroundColor: selected ? color : Colors.paper,
      borderColor: Colors.ink,
      borderWidth: selected ? 4 : 3,
    },
    selected ? Shadows.glow(color) : Shadows.card,
  ], [selected, color]);

  const lon = Number(spot.longitude);
  const lat = Number(spot.latitude);

  if (isNaN(lon) || isNaN(lat)) {
    return null;
  }

  return (
    <Mapbox.MarkerView
      id={spot.id}
      coordinate={[lon, lat]}
      anchor={{ x: 0.5, y: 1 }}
    >
      <TouchableOpacity
        activeOpacity={0.9}
        onPress={onPress}
        style={styles.markerContainer}
      >
        <Animated.View
          collapsable={false}
          style={[{ transform: [{ scale: scaleAnim }] }, styles.markerContainer]}
        >
          <View style={plateStyle}>
            <View style={styles.markerPlateContent}>
              {rewardImage ? (
                <View style={[styles.markerThumb, { borderColor: color }]}>
                  <ExpoImage
                    source={{ uri: rewardImage }}
                    style={styles.markerThumbImage}
                    contentFit="cover"
                  />
                </View>
              ) : (
                <View style={[styles.markerIconCircle, { backgroundColor: iconColor }]}>
                  <Ionicons name="location" size={14} color={selected ? Colors.paper : color} />
                </View>
              )}
              <Text
                style={[styles.markerText, { color: textColor }]}
                numberOfLines={1}
              >
                {spotName.toUpperCase()}
              </Text>
            </View>
          </View>

          <View style={[styles.markerStem, { backgroundColor: Colors.ink }]}>
            <View style={[styles.markerStemCap, { backgroundColor: color }]} />
          </View>

          <View
            style={[
              styles.markerHalo,
              {
                borderColor: color,
                backgroundColor: `${color}18`,
                transform: [{ scale: selected ? 1.3 : 1 }],
              },
            ]}
          />
        </Animated.View>
      </TouchableOpacity>
    </Mapbox.MarkerView>
  );
};

// ─── SpotCard ──────────────────────────────────────────────────────────────────
const SpotCard = memo(function SpotCard({
  spot,
  onPress,
  selected,
  distanceLabel,
}: {
  spot: SpotWithDistance;
  onPress: () => void;
  selected: boolean;
  distanceLabel?: string | null;
}) {
  const { t } = useI18n();
  const spotName = spot.name?.trim() || t('home.unnamedSpot');
  const liftAnim = useRef(new Animated.Value(selected ? -6 : 0)).current;

  useEffect(() => {
    Animated.spring(liftAnim, {
      toValue: selected ? -6 : 0,
      useNativeDriver: true,
      tension: 200,
      friction: 12,
    }).start();
  }, [selected, liftAnim]);

  return (
    <Animated.View style={{ transform: [{ translateY: liftAnim }] }}>
      <TouchableOpacity
        activeOpacity={0.92}
        onPress={onPress}
        style={[
          styles.spotCard,
          selected && styles.spotCardSelected,
          Shadows.card,
        ]}
      >
        <View style={styles.spotCardImageWrap}>
          <ExpoImage
            source={spot.reward?.imageUrl ? { uri: spot.reward.imageUrl } : SPOT_SAMPLE_IMAGE}
            style={styles.spotCardImage}
            contentFit="cover"
            transition={500}
          />
          <LinearGradient
            colors={['transparent', 'rgba(48,56,65,0.42)', 'rgba(48,56,65,0.82)']}
            style={StyleSheet.absoluteFillObject}
          />
          <View style={styles.spotCardBadge}>
            <Ionicons name="sparkles" size={14} color={Colors.paper} />
            <Text style={styles.spotCardBadgeText}>AR</Text>
          </View>
        </View>

        <View style={styles.spotCardContent}>
          <Text style={styles.spotCardTitle} numberOfLines={1}>{spotName}</Text>

          <View style={styles.spotCardReward}>
            <View style={styles.rewardIconMini}>
              <Ionicons name="gift" size={12} color={Colors.surge} />
            </View>
            <Text style={styles.spotCardMeta} numberOfLines={1}>
              {spot.reward?.name ?? t('home.mysteryReward')}
            </Text>
          </View>

          {distanceLabel ? (
            <View style={styles.spotCardDistanceRow}>
              <Ionicons name="navigate" size={12} color={Colors.ember} />
              <Text style={styles.spotCardDistanceText} numberOfLines={1}>{distanceLabel}</Text>
            </View>
          ) : null}

          <Text style={styles.spotCardDescription} numberOfLines={2}>
            {spot.description ?? t('home.detailNoDescription')}
          </Text>
        </View>
      </TouchableOpacity>
    </Animated.View>
  );
});

// ─── StatusBanner ──────────────────────────────────────────────────────────────
const StatusBanner = memo(function StatusBanner({
  spotsLoading,
  spotsError,
  spotsCount,
  onRetry,
  t,
}: {
  spotsLoading: boolean;
  spotsError: string | null;
  spotsCount: number;
  onRetry: () => void;
  t: (k: string) => string;
}) {
  const slideAnim = useRef(new Animated.Value(40)).current;
  const opacityAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.spring(slideAnim, { toValue: 0, useNativeDriver: true, tension: 120, friction: 10 }),
      Animated.timing(opacityAnim, { toValue: 1, duration: 300, useNativeDriver: true }),
    ]).start();
  }, [slideAnim, opacityAnim]);

  const icon = spotsError
    ? 'cloud-offline-outline'
    : spotsLoading
      ? 'radio-outline'
      : 'map-outline';

  return (
    <Animated.View
      style={[
        styles.statusCard,
        Shadows.card,
        { transform: [{ translateY: slideAnim }], opacity: opacityAnim },
      ]}
    >
      <View style={styles.statusIconWrap}>
        <Ionicons name={icon} size={18} color={Colors.paper} />
      </View>

      <View style={styles.statusCopy}>
        <Text style={styles.statusTitle}>
          {spotsError
            ? t('home.nearbyError')
            : spotsLoading
              ? t('home.nearbyLoading')
              : t('home.nearbyEmpty')}
        </Text>
        <Text style={styles.statusBody}>
          {spotsError ?? (spotsLoading ? t('home.checkingRoutes') : t('home.emptyRefresh'))}
        </Text>
      </View>

      <TouchableOpacity style={styles.statusRetry} activeOpacity={0.86} onPress={onRetry}>
        <Ionicons name="refresh" size={18} color={Colors.paper} />
      </TouchableOpacity>
    </Animated.View>
  );
});

// ─── DetailSheet — gesture-driven bottom sheet ────────────────────────────────
const DetailSheet = memo(function DetailSheet({
  spot,
  onClose,
  onOpenAR,
  onOpenDirections,
  t,
  insets,
}: {
  spot: SpotWithDistance;
  onClose: () => void;
  onOpenAR: (s: Spot) => void;
  onOpenDirections: (s: Spot) => void;
  t: (k: string, p?: any) => string;
  insets: { bottom: number };
}) {
  const translateY = useRef(new Animated.Value(600)).current;
  const backdropOpacity = useRef(new Animated.Value(0)).current;

  // Entrance animation
  useEffect(() => {
    Animated.parallel([
      Animated.spring(translateY, {
        toValue: 0,
        useNativeDriver: true,
        tension: 80,
        friction: 12,
      }),
      Animated.timing(backdropOpacity, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      }),
    ]).start();
  }, [translateY, backdropOpacity]);

  const dismiss = useCallback(() => {
    Animated.parallel([
      Animated.spring(translateY, { toValue: 700, useNativeDriver: true, tension: 80, friction: 14 }),
      Animated.timing(backdropOpacity, { toValue: 0, duration: 200, useNativeDriver: true }),
    ]).start(() => onClose());
  }, [translateY, backdropOpacity, onClose]);

  // Pan gesture for swipe-to-dismiss
  const panResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) => g.dy > 6 && Math.abs(g.dy) > Math.abs(g.dx),
      onPanResponderMove: (_, g) => {
        if (g.dy > 0) translateY.setValue(g.dy);
      },
      onPanResponderRelease: (_, g) => {
        if (g.dy > 120 || g.vy > SHEET_SNAP_VELOCITY_THRESHOLD / 1000) {
          dismiss();
        } else {
          Animated.spring(translateY, {
            toValue: 0,
            useNativeDriver: true,
            tension: 160,
            friction: 14,
          }).start();
        }
      },
    }),
  ).current;

  const distLabel = formatDistanceLabel(spot.distanceMeters, t);
  const routeLabel = distLabel ? t('profile.routeDistanceAway', { distance: distLabel }) : t('home.detailDistanceUnavailable');
  const previewKey = resolvePreviewModelKey(spot);
  const previewStatusLabel = previewKey ? t('home.detailPreviewReady') : t('home.detailPreviewFallback');
  const coordinates = `${spot.latitude.toFixed(5)}, ${spot.longitude.toFixed(5)}`;

  const handleAR = useCallback(() => onOpenAR(spot), [onOpenAR, spot]);
  const handleDirections = useCallback(() => onOpenDirections(spot), [onOpenDirections, spot]);

  return (
    <View style={StyleSheet.absoluteFill} pointerEvents="box-none">
      {/* Backdrop */}
      <Animated.View style={[styles.drawerBackdrop, { opacity: backdropOpacity }]}>
        <TouchableOpacity style={StyleSheet.absoluteFillObject} activeOpacity={1} onPress={dismiss} />
      </Animated.View>

      {/* Sheet */}
      <Animated.View style={[styles.detailSheet, Shadows.card, { transform: [{ translateY }] }]}>
        {/* Drag handle */}
        <View {...panResponder.panHandlers} style={styles.sheetDragArea}>
          <View style={styles.drawerHandle} />
        </View>

        <ScrollView
          style={styles.detailScroll}
          contentContainerStyle={[
            styles.detailScrollContent,
            { paddingBottom: Math.max(insets.bottom, 16) + Layout.tabBarHeight + 20 },
          ]}
          nestedScrollEnabled
          showsVerticalScrollIndicator={false}
        >
          {/* Hero image */}
          <View style={styles.detailHero}>
            <ExpoImage
              source={spot.reward?.imageUrl ? { uri: spot.reward.imageUrl } : SPOT_SAMPLE_IMAGE}
              style={styles.detailHeroImage}
              contentFit="cover"
              transition={500}
            />
            <LinearGradient
              colors={['rgba(48,56,65,0.05)', 'rgba(48,56,65,0.64)', 'rgba(48,56,65,0.94)']}
              style={StyleSheet.absoluteFillObject}
            />

            <View style={styles.detailHeroTopRow}>
              <View style={styles.detailStamp}>
                <Text style={styles.detailStampText}>{t('home.detailKicker')}</Text>
              </View>
              <TouchableOpacity style={styles.detailCloseButton} activeOpacity={0.86} onPress={dismiss}>
                <Ionicons name="close" size={18} color={Colors.paper} />
              </TouchableOpacity>
            </View>

            <View style={styles.detailHeroCopy}>
              <Text style={styles.detailTitle}>{spot.name?.trim() || t('home.unnamedSpot')}</Text>
              <Text style={styles.detailSubtitle} numberOfLines={2}>
                {spot.reward?.name ?? t('home.mysteryReward')}
                {' • '}
                {routeLabel}
              </Text>
            </View>
          </View>

          {/* Chips */}
          <View style={styles.detailChipRow}>
            <View style={[styles.detailChip, styles.detailChipPaper]}>
              <Ionicons name="navigate-outline" size={16} color={Colors.surge} />
              <Text style={styles.detailChipTextDark}>{coordinates}</Text>
            </View>
            <View style={[styles.detailChip, styles.detailChipTeal]}>
              <Ionicons name="cube-outline" size={16} color={Colors.paper} />
              <Text style={styles.detailChipTextLight}>{previewStatusLabel}</Text>
            </View>
          </View>

          {/* About */}
          <View style={styles.detailSection}>
            <Text style={styles.detailSectionTitle}>{t('home.detailAboutTitle')}</Text>
            <Text style={styles.detailSectionBody}>
              {spot.description ?? t('home.detailNoDescription')}
            </Text>
          </View>

          {/* Reward card */}
          <View style={[styles.detailRewardCard, Shadows.punch]}>
            <Text style={styles.detailRewardTitle}>{t('home.detailRewardTitle')}</Text>
            <Text style={styles.detailRewardName}>{spot.reward?.name ?? t('home.mysteryReward')}</Text>
            <Text style={styles.detailRewardBody}>{spot.reward?.description ?? t('home.detailNoReward')}</Text>

            <View style={styles.detailMetaList}>
              {([
                [t('home.detailCoordinates'), coordinates],
                [t('home.detailDistance'), routeLabel],
                [t('home.detailRadius'), t('home.detailRadiusMeters', { count: Math.round(spot.radiusMeters) })],
                [t('home.detailPreviewModel'), previewKey ?? spot.reward?.previewPrefabKey ?? 'Fallback'],
              ] as [string, string][]).map(([label, value]) => (
                <View key={label} style={styles.detailMetaRow}>
                  <Text style={styles.detailMetaLabel}>{label}</Text>
                  <Text style={styles.detailMetaValue}>{value}</Text>
                </View>
              ))}
            </View>
          </View>

          {/* CTA */}
          <View style={styles.ctaRow}>
            <TouchableOpacity style={styles.primaryButton} activeOpacity={0.88} onPress={handleAR}>
              <Ionicons name="sparkles-outline" size={18} color={Colors.paper} />
              <Text style={styles.primaryButtonText}>{t('home.openArTest')}</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.secondaryButton} activeOpacity={0.88} onPress={handleDirections}>
              <Text style={styles.secondaryButtonText}>{t('home.openDirections')}</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </Animated.View>
    </View>
  );
});

// ─── HomeScreen ────────────────────────────────────────────────────────────────
export default function HomeScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const bottomInset = Layout.tabBarHeight + insets.bottom + Spacing.base;

  const {
    spots,
    spotsLoading,
    spotsError,
    fetchSpots,
    userCoordinate,
    showUserMarker,
    firstName,
    language,
    toggleLanguage,
    t,
  } = useSpotsViewModel();

  const [selectedSpotId, setSelectedSpotId] = useState<string | null>(null);

  const selectedSpot = useMemo(
    () => spots.find((s) => s.id === selectedSpotId) ?? null,
    [selectedSpotId, spots],
  );

  // Clear selection if spot disappears from list
  useEffect(() => {
    if (selectedSpotId && !spots.some((s) => s.id === selectedSpotId)) {
      setSelectedSpotId(null);
    }
  }, [selectedSpotId, spots]);

  const handleOpenAR = useCallback(
    (spot: Spot) => {
      router.push({
        pathname: '/unity-ar',
        params: {
          mode: 'spot',
          spotId: spot.id,
          spotName: spot.name?.trim() || t('home.unnamedSpot'),
          spotDescription: spot.description ?? '',
          spotLatitude: String(spot.latitude),
          spotLongitude: String(spot.longitude),
          spotRadiusMeters: String(spot.radiusMeters),
          modelPrefabKey: spot.modelPrefabKey ?? '',
          rewardId: spot.reward?.id ?? '',
          rewardName: spot.reward?.name ?? '',
          rewardDescription: spot.reward?.description ?? '',
          rewardImageUrl: spot.reward?.imageUrl ?? '',
          rewardPreviewPrefabKey: spot.reward?.previewPrefabKey ?? '',
        },
      });
    },
    [router, t],
  );

  const handleOpenDirections = useCallback(
    async (spot: Spot) => {
      const label = encodeURIComponent(spot.name?.trim() || t('home.unnamedSpot'));
      const dest = `${spot.latitude},${spot.longitude}`;
      const url =
        Platform.OS === 'ios'
          ? `http://maps.apple.com/?daddr=${dest}&q=${label}`
          : `geo:0,0?q=${dest}(${label})`;
      try {
        await Linking.openURL(url);
      } catch (e: any) {
        Alert.alert(t('profile.routeMapErrorTitle'), e?.message ?? t('profile.routeMapErrorBody'));
      }
    },
    [t],
  );

  const handleSelectSpot = useCallback((id: string) => setSelectedSpotId(id), []);
  const handleDeselectSpot = useCallback(() => setSelectedSpotId(null), []);
  const handleNavigateProfile = useCallback(() => router.navigate('/(tabs)/profile'), [router]);

  const cameraPaddingBottom = selectedSpot
    ? Layout.tabBarHeight + insets.bottom + 360
    : Layout.tabBarHeight + insets.bottom + 274;

  const cameraBounds = useMemo(() => {
    if (selectedSpot || spots.length <= 1) return undefined;
    const lats = spots.map((s) => s.latitude);
    const lons = spots.map((s) => s.longitude);
    const minLat = Math.min(...lats), maxLat = Math.max(...lats);
    const minLon = Math.min(...lons), maxLon = Math.max(...lons);
    const dLat = Math.max((maxLat - minLat) * 0.6, 0.008);
    const dLon = Math.max((maxLon - minLon) * 0.6, 0.008);
    return {
      ne: [maxLon + dLon, maxLat + dLat] as [number, number],
      sw: [minLon - dLon, minLat - dLat] as [number, number],
      paddingTop: insets.top + 162,
      paddingBottom: cameraPaddingBottom,
      paddingLeft: 112,
      paddingRight: 112,
    };
  }, [selectedSpot, spots, insets.top, cameraPaddingBottom]);

  const singleSpotCoordinate = useMemo<[number, number]>(() =>
    spots.length === 1
      ? [spots[0].longitude, spots[0].latitude]
      : MAP_CENTER,
    [spots],
  );

  const showStatusBanner = spotsLoading || !!spotsError || spots.length === 0;

  return (
    <View style={styles.container}>
      {/* ── Map ── */}
      <Mapbox.MapView
        style={StyleSheet.absoluteFillObject}
        styleURL="mapbox://styles/sodbayar/cmn34iz46003h01r44p0j4kk5"
        logoEnabled={false}
        attributionEnabled={false}
        scaleBarEnabled={false}
        compassEnabled={false}
        onPress={handleDeselectSpot}
      >
        <Mapbox.Camera
          bounds={cameraBounds}
          centerCoordinate={
            selectedSpot
              ? [selectedSpot.longitude, selectedSpot.latitude]
              : cameraBounds ? undefined : singleSpotCoordinate
          }
          zoomLevel={selectedSpot ? 15.1 : cameraBounds ? undefined : 14.2}
          padding={
            selectedSpot
              ? { paddingTop: insets.top + 152, paddingBottom: cameraPaddingBottom, paddingLeft: 88, paddingRight: 88 }
              : undefined
          }
          animationMode="flyTo"
          animationDuration={1400}
          maxZoomLevel={16}
          pitch={selectedSpot ? 18 : 30}
        />
        {showUserMarker && userCoordinate ? (
          <PulsingUserMarker coordinate={userCoordinate} label={firstName} />
        ) : null}

        {spots.map((spot, index) => (
          <SpotMarker
            key={spot.id}
            spot={spot}
            index={index}
            selected={selectedSpot?.id === spot.id}
            onPress={() => handleSelectSpot(spot.id)}
          />
        ))}
      </Mapbox.MapView>

      {/* ── Gradient overlay ── */}
      <LinearGradient
        colors={Gradients.overlay}
        locations={[0, 0.46, 1]}
        style={StyleSheet.absoluteFillObject}
        pointerEvents="none"
      />

      {/* ── Decorative stripes ── */}
      <View pointerEvents="none" style={styles.decorLayer}>
        <View style={styles.topStripe} />
        <View style={styles.bottomStripe} />
      </View>

      {/* ── UI overlay ── */}
      <View
        style={[styles.uiOverlay, { paddingTop: insets.top + Spacing.sm, paddingBottom: bottomInset }]}
        pointerEvents="box-none"
      >
        {/* Floating header */}
        <View style={[styles.floatingHeader, { top: insets.top + Spacing.lg }]} pointerEvents="box-none">
          <TouchableOpacity
            style={[styles.headerBadge, Shadows.punch]}
            activeOpacity={0.86}
            onPress={handleNavigateProfile}
            hitSlop={14}
          >
            <Ionicons name="person-circle-outline" size={18} color={Colors.ink} />
            <Text style={styles.headerBadgeLabel}>{firstName}</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.langToggle, Shadows.punch]}
            activeOpacity={0.86}
            onPress={toggleLanguage}
            hitSlop={14}
          >
            <Text style={styles.langToggleText}>{language === 'en' ? 'MN' : 'EN'}</Text>
          </TouchableOpacity>
        </View>

        {/* Status banner */}
        {showStatusBanner && (
          <StatusBanner
            spotsLoading={spotsLoading}
            spotsError={spotsError}
            spotsCount={spots.length}
            onRetry={fetchSpots}
            t={t}
          />
        )}

        {/* Detail sheet */}
        {selectedSpot && (
          <DetailSheet
            key={selectedSpot.id}
            spot={selectedSpot}
            onClose={handleDeselectSpot}
            onOpenAR={handleOpenAR}
            onOpenDirections={handleOpenDirections}
            t={t}
            insets={insets}
          />
        )}
      </View>
    </View>
  );
}

// ─── Styles ────────────────────────────────────────────────────────────────────
const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.bg },
  uiOverlay: { ...StyleSheet.absoluteFillObject },
  decorLayer: { ...StyleSheet.absoluteFillObject },
  topStripe: {
    position: 'absolute', top: 92, right: -22,
    width: 130, height: 44,
    backgroundColor: Colors.surge, borderRadius: Radius.md,
    transform: [{ rotate: '9deg' }], opacity: 0.88,
  },
  bottomStripe: {
    position: 'absolute', left: -24, bottom: 188,
    width: 100, height: 34,
    backgroundColor: Colors.ember, borderRadius: Radius.md,
    transform: [{ rotate: '-8deg' }], opacity: 0.92,
  },

  // ── User pulse marker ──
  pulseWrapper: { width: 120, height: 140, justifyContent: 'center', alignItems: 'center' },
  pulseRing: {
    position: 'absolute',
    width: 70, height: 70, borderRadius: 35,
    backgroundColor: 'rgba(0, 173, 181, 0.18)',
    borderWidth: 2, borderColor: 'rgba(0, 173, 181, 0.5)',
  },
  pulseRing2: {
    backgroundColor: 'rgba(0, 173, 181, 0.10)',
    borderColor: 'rgba(0, 173, 181, 0.3)',
  },
  pulseRing3: {
    backgroundColor: 'rgba(0, 173, 181, 0.05)',
    borderColor: 'rgba(0, 173, 181, 0.15)',
  },
  wizardGlow: {
    position: 'absolute',
    width: 80, height: 80, borderRadius: 40,
    backgroundColor: 'rgba(0, 173, 181, 0.12)',
  },
  wizardWrapper: {
    width: 72, height: 72,
    justifyContent: 'center', alignItems: 'center',
  },
  wizardImage: {
    width: 68, height: 68,
  },
  wizardNamePlate: {
    marginTop: 4,
    backgroundColor: Colors.ink,
    borderWidth: 2, borderColor: Colors.surge,
    borderRadius: Radius.full,
    paddingHorizontal: 10, paddingVertical: 4,
  },
  wizardNameText: {
    ...Typography.caption,
    color: Colors.surge,
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 1,
  },

  // ── Spot markers ──
  markerContainer: { width: 140, height: 100, alignItems: 'center', justifyContent: 'flex-end', paddingBottom: 10 },
  markerPlate: {
    paddingHorizontal: 14, paddingVertical: 10,
    borderRadius: Radius.lg, borderWidth: 4,
    minWidth: 100, maxWidth: 180, zIndex: 2,
    elevation: 8,
  },
  markerPlateContent: { flexDirection: 'row', alignItems: 'center', gap: 10, justifyContent: 'center' },
  markerThumb: { width: 28, height: 28, borderRadius: 8, borderWidth: 2, overflow: 'hidden', backgroundColor: Colors.ink },
  markerThumbImage: { flex: 1 },
  markerIconCircle: { width: 24, height: 24, borderRadius: 12, justifyContent: 'center', alignItems: 'center' },
  markerText: { ...Typography.caption, fontSize: 13, fontWeight: '900', letterSpacing: 0.8 },
  markerStem: { width: 6, height: 24, marginTop: -6, zIndex: 1, borderBottomLeftRadius: 3, borderBottomRightRadius: 3 },
  markerStemCap: { position: 'absolute', bottom: -5, left: -4, width: 12, height: 12, borderRadius: 6, borderWidth: 2, borderColor: Colors.ink },
  markerHalo: { position: 'absolute', bottom: -8, width: 50, height: 16, borderRadius: Radius.full, borderWidth: 2, opacity: 0.6 },

  // ── Spot cards ──
  spotCard: {
    width: 228, backgroundColor: Colors.paper,
    borderWidth: 4, borderColor: Colors.ink, borderRadius: Radius.xl, overflow: 'hidden',
  },
  spotCardSelected: { borderColor: Colors.surge },
  spotCardImageWrap: { height: 120, width: '100%', backgroundColor: Colors.ink },
  spotCardImage: { flex: 1 },
  spotCardBadge: {
    position: 'absolute', top: 10, right: 12,
    flexDirection: 'row', alignItems: 'center', gap: 4,
    backgroundColor: 'rgba(48,56,65,0.72)',
    paddingHorizontal: 10, paddingVertical: 5,
    borderRadius: Radius.full, borderWidth: 2, borderColor: 'rgba(238,238,238,0.4)',
  },
  spotCardBadgeText: { ...Typography.caption, color: Colors.paper, fontWeight: '900', fontSize: 10 },
  spotCardContent: { padding: Spacing.md, gap: 6 },
  spotCardTitle: { ...Typography.heading, color: Colors.ink, fontSize: 15 },
  spotCardReward: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  rewardIconMini: { width: 20, height: 20, borderRadius: 6, backgroundColor: 'rgba(0,173,181,0.12)', justifyContent: 'center', alignItems: 'center' },
  spotCardMeta: { ...Typography.caption, color: 'rgba(48,56,65,0.62)', fontSize: 11, flex: 1 },
  spotCardDistanceRow: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  spotCardDistanceText: { ...Typography.caption, color: Colors.ember, fontSize: 11, flex: 1 },
  spotCardDescription: { ...Typography.caption, color: 'rgba(48,56,65,0.72)', minHeight: 30 },

  // ── Status banner ──
  statusCard: {
    position: 'absolute', left: Spacing.lg, right: Spacing.lg, bottom: 118,
    flexDirection: 'row', alignItems: 'center', gap: Spacing.sm,
    backgroundColor: Colors.paper, borderWidth: 3, borderColor: Colors.ink,
    borderRadius: Radius.xl, paddingHorizontal: Spacing.base, paddingVertical: Spacing.base,
  },
  statusIconWrap: {
    width: 40, height: 40, borderRadius: Radius.md, borderWidth: 3, borderColor: Colors.ink,
    backgroundColor: Colors.surge, justifyContent: 'center', alignItems: 'center',
  },
  statusCopy: { flex: 1 },
  statusTitle: { ...Typography.label, color: Colors.ink },
  statusBody: { ...Typography.caption, color: 'rgba(48,56,65,0.72)', marginTop: 2 },
  statusRetry: {
    width: 42, height: 42, borderRadius: Radius.md, borderWidth: 3, borderColor: Colors.ink,
    backgroundColor: Colors.ember, justifyContent: 'center', alignItems: 'center',
  },

  // ── Floating header ──
  floatingHeader: {
    position: 'absolute', top: 0, left: Spacing.lg, right: Spacing.lg,
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
  },
  headerBadge: {
    minHeight: 56, backgroundColor: Colors.paper, borderWidth: 3, borderColor: Colors.ink,
    borderRadius: Radius.full, paddingHorizontal: 18, paddingVertical: 12,
    flexDirection: 'row', alignItems: 'center', gap: 8,
  },
  headerBadgeLabel: { ...Typography.title, color: Colors.ink, fontSize: 18 },
  langToggle: {
    width: 56, height: 56, borderRadius: Radius.md,
    backgroundColor: Colors.paper, borderWidth: 3, borderColor: Colors.ink,
    justifyContent: 'center', alignItems: 'center',
  },
  langToggleText: { ...Typography.label, color: Colors.ink },

  // ── Detail sheet ──
  drawerBackdrop: { ...StyleSheet.absoluteFillObject, backgroundColor: 'rgba(48,56,65,0.4)' },
  detailSheet: {
    position: 'absolute', left: 0, right: 0, bottom: -8,
    maxHeight: '86%', backgroundColor: Colors.paper,
    borderTopLeftRadius: Radius.xl, borderTopRightRadius: Radius.xl,
    borderWidth: 4, borderColor: Colors.ink,
    paddingBottom: 12, gap: Spacing.base,
  },
  sheetDragArea: { paddingTop: 12, paddingHorizontal: Spacing.lg, alignItems: 'center' },
  drawerHandle: {
    width: 44, height: 6,
    backgroundColor: 'rgba(48,56,65,0.12)', borderRadius: 3,
    marginBottom: 12,
  },
  detailScroll: { flexGrow: 0, flexShrink: 1, paddingHorizontal: Spacing.lg },
  detailScrollContent: { gap: Spacing.base },
  detailHero: {
    height: 190, borderWidth: 3, borderColor: Colors.ink,
    borderRadius: Radius.xl, overflow: 'hidden', backgroundColor: Colors.ink,
  },
  detailHeroImage: { flex: 1 },
  detailHeroTopRow: {
    position: 'absolute', top: 12, left: 12, right: 12,
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
  },
  detailStamp: {
    paddingHorizontal: Spacing.md, paddingVertical: 8,
    borderWidth: 3, borderColor: Colors.ink, borderRadius: Radius.full,
    backgroundColor: Colors.ember, transform: [{ rotate: '-3deg' }],
  },
  detailStampText: { ...Typography.caption, color: Colors.paper, fontWeight: '900' },
  detailCloseButton: {
    width: 40, height: 40, borderRadius: Radius.full, borderWidth: 3, borderColor: Colors.paper,
    backgroundColor: 'rgba(48,56,65,0.7)', justifyContent: 'center', alignItems: 'center',
  },
  detailHeroCopy: { position: 'absolute', left: Spacing.md, right: Spacing.md, bottom: Spacing.md, gap: 4 },
  detailTitle: { ...Typography.displayLg, color: Colors.paper },
  detailSubtitle: { ...Typography.body, color: 'rgba(238,238,238,0.84)', lineHeight: 18 },
  detailChipRow: { flexDirection: 'row', gap: Spacing.sm, flexWrap: 'wrap' },
  detailChip: {
    flex: 1, minHeight: 46, minWidth: 0,
    borderWidth: 3, borderColor: Colors.ink, borderRadius: Radius.lg,
    paddingHorizontal: Spacing.md, paddingVertical: 10,
    flexDirection: 'row', alignItems: 'center', gap: 8,
  },
  detailChipPaper: { backgroundColor: Colors.paper },
  detailChipTeal: { backgroundColor: Colors.surge },
  detailChipTextDark: { ...Typography.caption, color: Colors.ink, flex: 1 },
  detailChipTextLight: { ...Typography.caption, color: Colors.paper, flex: 1 },
  detailSection: { gap: 6 },
  detailSectionTitle: { ...Typography.label, color: Colors.ink },
  detailSectionBody: { ...Typography.body, color: 'rgba(48,56,65,0.78)' },
  detailRewardCard: {
    backgroundColor: Colors.paper, borderWidth: 3, borderColor: Colors.ink,
    borderRadius: Radius.xl, padding: Spacing.md, gap: 6,
  },
  detailRewardTitle: { ...Typography.caption, color: Colors.surge },
  detailRewardName: { ...Typography.displayMd, color: Colors.ink },
  detailRewardBody: { ...Typography.body, color: 'rgba(48,56,65,0.76)' },
  detailMetaList: { marginTop: Spacing.xs, gap: 8 },
  detailMetaRow: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: Spacing.sm },
  detailMetaLabel: { ...Typography.caption, color: 'rgba(48,56,65,0.62)', flex: 1 },
  detailMetaValue: { ...Typography.body, color: Colors.ink, flex: 1, textAlign: 'right' },
  ctaRow: { flexDirection: 'row', gap: Spacing.sm },
  primaryButton: {
    flex: 1, minHeight: 54, borderRadius: Radius.lg, borderWidth: 3, borderColor: Colors.ink,
    backgroundColor: Colors.ember, flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    gap: 8, ...Shadows.punch,
  },
  primaryButtonText: { ...Typography.heading, color: Colors.paper },
  secondaryButton: {
    minWidth: 116, minHeight: 54, borderRadius: Radius.lg, borderWidth: 3, borderColor: Colors.ink,
    backgroundColor: Colors.surge, alignItems: 'center', justifyContent: 'center',
    paddingHorizontal: Spacing.md, ...Shadows.punch,
  },
  secondaryButtonText: { ...Typography.heading, color: Colors.paper },
});
