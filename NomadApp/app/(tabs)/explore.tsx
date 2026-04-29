import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Animated,
  FlatList,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';
import { Colors, Layout, Radius, Shadows, Spacing, Typography } from '@/constants/theme';
import type { Claim } from '@/services/api';

// A curated single-palette with clear visual hierarchy
const CARD_TONES = [
  { bg: Colors.paper, fg: Colors.ink, accent: Colors.ember, indexBg: Colors.ink, indexFg: Colors.paper },
  { bg: Colors.ink, fg: Colors.paper, accent: Colors.surge, indexBg: Colors.surge, indexFg: Colors.ink },
  { bg: Colors.surge, fg: Colors.ink, accent: Colors.ember, indexBg: Colors.ink, indexFg: Colors.surge },
] as const;

// ─── RewardCard ──────────────────────────────────────────────────────────────

function RewardCard({
  spot,
  claim,
  index,
  locked,
  onPress,
}: {
  spot: any;
  claim?: Claim;
  index: number;
  locked: boolean;
  onPress?: () => void;
}) {
  const { formatDate, t } = useI18n();
  const tone = CARD_TONES[index % CARD_TONES.length];

  const bgColor = locked ? '#F5F0EB' : tone.bg;
  const fgColor = locked ? 'rgba(48,56,65,0.35)' : tone.fg;
  const metaColor = locked
    ? 'rgba(48,56,65,0.3)'
    : tone.fg === Colors.paper
      ? 'rgba(238,238,238,0.75)'
      : 'rgba(48,56,65,0.6)';
  const borderColor = locked ? 'rgba(48,56,65,0.12)' : Colors.ink;
  const borderWidth = locked ? 2 : 4;

  const rewardName = claim?.reward?.name ?? spot?.reward?.name ?? t('home.mysteryReward');
  const rewardDesc = claim?.reward?.description ?? spot?.reward?.description ?? '???';
  const spotName = claim?.tourismSpot?.name ?? spot?.name ?? '???';

  const claimedDate = claim
    ? formatDate(claim.claimedAt, { month: 'short', day: 'numeric', year: 'numeric' })
    : t('inventory.notClaimed');

  const iconColor = locked
    ? 'rgba(48,56,65,0.25)'
    : tone.fg === Colors.paper
      ? Colors.ink
      : Colors.paper;

  return (
    <TouchableOpacity
      activeOpacity={locked || !onPress ? 1 : 0.88}
      disabled={locked || !onPress}
      onPress={onPress}
      style={[
        styles.card,
        { backgroundColor: bgColor, borderColor, borderWidth },
        !locked && Shadows.card,
        !locked && Shadows.glow(tone.accent),
      ]}
    >
      {/* Top row */}
      <View style={styles.cardTopRow}>
        <View style={[
          styles.cardIndexBadge,
          {
            backgroundColor: locked ? 'rgba(48,56,65,0.08)' : tone.indexBg,
            borderColor: locked ? 'rgba(48,56,65,0.15)' : Colors.ink,
          },
        ]}>
          <Text style={[styles.cardIndexText, { color: locked ? 'rgba(48,56,65,0.3)' : tone.indexFg }]}>
            {String(index + 1).padStart(2, '0')}
          </Text>
        </View>

        {!locked && (
          <View style={[styles.ownedBadge, { backgroundColor: tone.accent, borderColor: Colors.ink }]}>
            <Ionicons name="sparkles" size={12} color={iconColor} />
            <Text style={[styles.ownedText, { color: iconColor }]}>
              {t('common.owned')}
            </Text>
          </View>
        )}

        <View style={[
          styles.cardDateBadge,
          {
            borderColor: locked ? 'rgba(48,56,65,0.15)' : borderColor,
            backgroundColor: locked ? 'rgba(48,56,65,0.05)' : 'transparent',
          },
        ]}>
          <Text style={[styles.cardDateText, { color: fgColor }]}>{claimedDate}</Text>
        </View>
      </View>

      {/* Body */}
      <View style={styles.cardBody}>
        <View style={[
          styles.cardIcon,
          {
            backgroundColor: locked ? 'rgba(48,56,65,0.06)' : tone.accent,
            borderColor: locked ? 'rgba(48,56,65,0.12)' : Colors.ink,
          },
        ]}>
          <Ionicons
            name={locked ? 'lock-closed-outline' : 'gift-outline'}
            size={22}
            color={iconColor}
          />
        </View>

        <View style={styles.cardContent}>
          <Text style={[styles.cardTitle, { color: fgColor }]} numberOfLines={2}>
            {locked ? t('inventory.lockedTitle') : rewardName}
          </Text>
          <Text style={[styles.cardDesc, { color: metaColor }]} numberOfLines={3}>
            {locked ? t('inventory.lockedBody') : rewardDesc}
          </Text>
          <View style={styles.cardMeta}>
            <Ionicons name="location-outline" size={12} color={metaColor} />
            <Text style={[styles.cardMetaText, { color: metaColor }]} numberOfLines={1}>
              {spotName}
            </Text>
          </View>
        </View>
      </View>

      {/* Footer */}
      <View style={[
        styles.cardFooter,
        {
          borderTopColor: locked
            ? 'rgba(48,56,65,0.08)'
            : tone.fg === Colors.paper
              ? 'rgba(238,238,238,0.15)'
              : 'rgba(48,56,65,0.1)',
        },
      ]}>
        {!locked ? (
          <Text style={[styles.cardFooterNote, { color: tone.fg }]}>
            {t('inventory.previewAction')} →
          </Text>
        ) : (
          <View style={styles.lockedBadge}>
            <Ionicons name="map-outline" size={10} color="rgba(48,56,65,0.3)" />
            <Text style={styles.lockedBadgeText}>{t('inventory.findOnMap')}</Text>
          </View>
        )}
      </View>
    </TouchableOpacity>
  );
}

// ─── EmptyPanel ──────────────────────────────────────────────────────────────

function EmptyPanel({
  icon,
  title,
  body,
  actionLabel,
  onPress,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  body: string;
  actionLabel?: string;
  onPress?: () => void;
}) {
  return (
    <View style={[styles.emptyPanel, Shadows.card]}>
      <View style={styles.emptyIconWrap}>
        <Ionicons name={icon} size={34} color={Colors.ink} />
      </View>
      <Text style={styles.emptyTitle}>{title}</Text>
      <Text style={styles.emptyBody}>{body}</Text>
      {actionLabel && onPress ? (
        <TouchableOpacity style={styles.emptyAction} activeOpacity={0.86} onPress={onPress}>
          <Text style={styles.emptyActionText}>{actionLabel}</Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

// ─── FilterToggle ─────────────────────────────────────────────────────────────

/**
 * Animated segmented control. Keeps animation logic self-contained so
 * InventoryScreen doesn't need to manage Animated values.
 */
function FilterToggle({
  value,
  collectedCount,
  onChange,
}: {
  value: 'all' | 'collected';
  collectedCount: number;
  onChange: (v: 'all' | 'collected') => void;
}) {
  const { t } = useI18n();
  const slideAnim = useRef(new Animated.Value(0)).current;
  const isFirst = value === 'all';

  useEffect(() => {
    Animated.spring(slideAnim, {
      toValue: isFirst ? 0 : 1,
      useNativeDriver: true,
      damping: 18,
      stiffness: 200,
      mass: 0.8,
    }).start();
  }, [isFirst, slideAnim]);

  // We can't rely on screen width at definition time — measure on layout instead.
  const [pillWidth, setPillWidth] = useState(0);

  const translateX = slideAnim.interpolate({
    inputRange: [0, 1],
    outputRange: [0, pillWidth],
  });

  return (
    <View
      style={styles.filterRow}
      onLayout={(e) => {
        // Pill width = half the inner container minus its padding
        setPillWidth((e.nativeEvent.layout.width - 12) / 2);
      }}
    >
      {/* Sliding background pill */}
      <Animated.View
        style={[
          styles.filterActiveIndicator,
          { width: pillWidth, transform: [{ translateX }] },
        ]}
        pointerEvents="none"
      />

      <TouchableOpacity style={styles.filterBtn} activeOpacity={0.9} onPress={() => onChange('all')}>
        <Text style={[styles.filterBtnText, isFirst && styles.filterBtnTextActive]}>
          {t('common.all')}
        </Text>
      </TouchableOpacity>

      <TouchableOpacity style={styles.filterBtn} activeOpacity={0.9} onPress={() => onChange('collected')}>
        <Text style={[styles.filterBtnText, !isFirst && styles.filterBtnTextActive]}>
          {t('common.owned')} ({collectedCount})
        </Text>
      </TouchableOpacity>
    </View>
  );
}

// ─── InventoryScreen ──────────────────────────────────────────────────────────

export default function InventoryScreen() {
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const tabPadding = Layout.tabBarHeight + insets.bottom + Spacing.xxl;

  const { user, rewards, rewardsLoading, rewardsError, fetchRewards, fetchSpots, spots } = useApp();
  const { formatDisplayName, t } = useI18n();

  const [filterMode, setFilterMode] = useState<'all' | 'collected'>('all');

  const explorerName = useMemo(() => {
    const displayName = formatDisplayName(user?.displayName);
    return displayName.split(' ')[0] ?? t('common.guestName');
  }, [user?.displayName, formatDisplayName, t]);

  // Stable callbacks to avoid useEffect churn
  const stableFetchSpots = useCallback(fetchSpots, []);  // eslint-disable-line react-hooks/exhaustive-deps
  const stableFetchRewards = useCallback(fetchRewards, []);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    stableFetchSpots();
    stableFetchRewards();
  }, [stableFetchSpots, stableFetchRewards]);

  // Memoised list data — avoids recomputing on unrelated re-renders
  const listData = useMemo(
    () =>
      filterMode === 'collected'
        ? spots.filter((s) => rewards.some((r) => r.tourismSpot.id === s.id))
        : spots,
    [filterMode, spots, rewards],
  );

  const openRewardPreview = useCallback(
    (claim: Claim) => {
      router.push({
        pathname: '/unity-ar',
        params: {
          mode: 'collection-preview',
          rewardId: claim.reward.id,
          rewardName: claim.reward.name,
          rewardDescription: claim.reward.description,
          rewardImageUrl: claim.reward.imageUrl ?? '',
          previewPrefabKey:
            claim.tourismSpot.modelPrefabKey ?? claim.reward.previewPrefabKey ?? '',
          claimedAt: claim.claimedAt,
          previewSpotName: claim.tourismSpot.name,
        },
      });
    },
    [router],
  );

  const renderItem = useCallback(
    ({ item: spot, index }: { item: any; index: number }) => {
      const claim = rewards.find((r) => r.tourismSpot.id === spot.id);
      return (
        <RewardCard
          spot={spot}
          claim={claim}
          index={index}
          locked={!claim}
          onPress={claim ? () => openRewardPreview(claim) : undefined}
        />
      );
    },
    [rewards, openRewardPreview],
  );

  // ── Render ────────────────────────────────────────────────────────────────

  const showEmpty =
    !rewardsLoading && !rewardsError && filterMode === 'collected' && listData.length === 0;

  return (
    <View style={[styles.container, { paddingTop: insets.top }]}>
      {/* Decorative background accents */}
      <View pointerEvents="none" style={styles.decorLayer}>
        <View style={styles.topStripe} />
        <View style={styles.bottomStripe} />
      </View>

      {/* Header */}
      <View style={styles.headerShell}>
        <View style={[styles.headerCard, Shadows.card]}>
          <View style={styles.headerStamp}>
            <Text style={styles.headerStampText}>{t('inventory.headerStamp')}</Text>
          </View>

          <View style={styles.headerTopRow}>
            <View style={styles.headerTextBlock}>
              <Text style={styles.headerEyebrow}>{t('inventory.headerEyebrow')}</Text>
              <Text style={styles.headerTitle}>{t('inventory.title')}</Text>
              <Text style={styles.headerBody}>{t('inventory.body')}</Text>
            </View>

            <View style={styles.headerActions}>
              {/* Progress counter */}
              <View style={styles.progressBadge}>
                <Text style={styles.progressText}>
                  {rewards.length}
                  <Text style={styles.progressTotal}>/{spots.length}</Text>
                </Text>
              </View>

              <TouchableOpacity
                style={styles.headerButton}
                activeOpacity={0.86}
                onPress={fetchRewards}
                disabled={rewardsLoading}
              >
                <Ionicons name="refresh-outline" size={20} color={Colors.paper} />
              </TouchableOpacity>
            </View>
          </View>
        </View>

        <FilterToggle
          value={filterMode}
          collectedCount={rewards.length}
          onChange={setFilterMode}
        />
      </View>

      {/* Body */}
      {rewardsLoading ? (
        <View style={styles.centerStage}>
          <EmptyPanel
            icon="sync-outline"
            title={t('inventory.loadingTitle')}
            body={t('inventory.loadingBody')}
          />
          <ActivityIndicator size="small" color={Colors.paper} />
        </View>
      ) : rewardsError ? (
        <View style={styles.centerStage}>
          <EmptyPanel
            icon="cloud-offline-outline"
            title={t('inventory.offlineTitle')}
            body={t('inventory.offlineBody')}
            actionLabel={t('inventory.retry')}
            onPress={fetchRewards}
          />
        </View>
      ) : spots.length === 0 ? (
        <View style={styles.centerStage}>
          <EmptyPanel
            icon="trophy-outline"
            title={t('inventory.emptyTitle')}
            body={t('inventory.emptyBody')}
          />
        </View>
      ) : showEmpty ? (
        // Empty state specifically for "Collected" filter with no results
        <View style={styles.centerStage}>
          <EmptyPanel
            icon="gift-outline"
            title={t('inventory.noneCollectedTitle')}
            body={t('inventory.noneCollectedBody')}
            actionLabel={t('inventory.exploreAction')}
            onPress={() => setFilterMode('all')}
          />
        </View>
      ) : (
        <FlatList
          data={listData}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          contentContainerStyle={[styles.list, { paddingBottom: tabPadding }]}
          showsVerticalScrollIndicator={false}
          // Performance: only re-render cards when rewards or spots change
          extraData={rewards}
        />
      )}
    </View>
  );
}

// ─── Styles ───────────────────────────────────────────────────────────────────

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.bg,
  },
  decorLayer: {
    ...StyleSheet.absoluteFillObject,
  },
  topStripe: {
    position: 'absolute',
    top: 74,
    right: -28,
    width: 170,
    height: 52,
    backgroundColor: Colors.surge,
    borderRadius: Radius.md,
    transform: [{ rotate: '9deg' }],
  },
  bottomStripe: {
    position: 'absolute',
    left: -18,
    bottom: 168,
    width: 136,
    height: 38,
    backgroundColor: Colors.ember,
    borderRadius: Radius.md,
    transform: [{ rotate: '-8deg' }],
  },

  // ── Header ────────────────────────────────────────────────────────────────
  headerShell: {
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.sm,
    gap: Spacing.md,
  },
  headerCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.xxl,
    paddingBottom: Spacing.lg,
  },
  headerStamp: {
    position: 'absolute',
    top: -14,
    left: 16,
    paddingHorizontal: Spacing.md,
    paddingVertical: 8,
    backgroundColor: Colors.ember,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    transform: [{ rotate: '-4deg' }],
  },
  headerStampText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  headerTopRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: Spacing.md,
  },
  headerTextBlock: {
    flex: 1,
  },
  headerEyebrow: {
    ...Typography.caption,
    color: Colors.surge,
  },
  headerTitle: {
    ...Typography.displayLg,
    color: Colors.ink,
    marginTop: 2,
  },
  headerBody: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.8)',
    marginTop: 8,
  },
  headerActions: {
    alignItems: 'center',
    gap: Spacing.sm,
  },
  progressBadge: {
    paddingHorizontal: Spacing.sm,
    paddingVertical: 4,
    backgroundColor: Colors.surge,
    borderRadius: Radius.full,
    borderWidth: 2,
    borderColor: Colors.ink,
    alignItems: 'center',
  },
  progressText: {
    ...Typography.caption,
    color: Colors.ink,
    fontWeight: '900',
    fontSize: 13,
  },
  progressTotal: {
    fontWeight: '500',
    opacity: 0.55,
  },
  headerButton: {
    width: 54,
    height: 54,
    borderRadius: Radius.lg,
    backgroundColor: Colors.ink,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
    ...Shadows.punch,
  },

  // ── Filter toggle ─────────────────────────────────────────────────────────
  filterRow: {
    flexDirection: 'row',
    backgroundColor: Colors.ink,
    borderRadius: Radius.lg,
    padding: 5,
    borderWidth: 3,
    borderColor: Colors.ink,
    overflow: 'hidden',
    ...Shadows.punch,
  },
  filterActiveIndicator: {
    position: 'absolute',
    top: 5,
    left: 5,
    bottom: 5,
    backgroundColor: Colors.surge,
    borderRadius: Radius.md,
  },
  filterBtn: {
    flex: 1,
    height: 44,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1,
  },
  filterBtnText: {
    ...Typography.label,
    color: 'rgba(238,238,238,0.45)',
    fontWeight: '700',
    fontSize: 13,
  },
  filterBtnTextActive: {
    color: Colors.ink,
    fontWeight: '900',
    fontSize: 13,
  },

  // ── Center stage (loading / error / empty) ────────────────────────────────
  centerStage: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: Spacing.md,
    paddingHorizontal: Spacing.lg,
  },
  emptyPanel: {
    width: '100%',
    maxWidth: 420,
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.xl,
    alignItems: 'center',
    gap: Spacing.md,
  },
  emptyIconWrap: {
    width: 72,
    height: 72,
    borderRadius: Radius.lg,
    backgroundColor: Colors.surge,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
  },
  emptyTitle: {
    ...Typography.displayMd,
    color: Colors.ink,
    textAlign: 'center',
  },
  emptyBody: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.8)',
    textAlign: 'center',
  },
  emptyAction: {
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    borderRadius: Radius.full,
    backgroundColor: Colors.ember,
    borderWidth: 3,
    borderColor: Colors.ink,
  },
  emptyActionText: {
    ...Typography.caption,
    color: Colors.paper,
  },

  // ── Card list ─────────────────────────────────────────────────────────────
  list: {
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.lg,
    gap: Spacing.md,
  },
  card: {
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    gap: Spacing.md,
  },
  cardTopRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cardIndexBadge: {
    minWidth: 46,
    paddingHorizontal: 10,
    paddingVertical: 7,
    borderRadius: Radius.full,
    borderWidth: 2,
    alignItems: 'center',
  },
  cardIndexText: {
    ...Typography.caption,
    fontWeight: '900',
    fontSize: 11,
  },
  cardDateBadge: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: Radius.full,
    borderWidth: 2,
  },
  cardDateText: {
    ...Typography.caption,
    fontSize: 10,
  },
  cardBody: {
    flexDirection: 'row',
    gap: Spacing.md,
    alignItems: 'flex-start',
  },
  cardIcon: {
    width: 52,
    height: 52,
    borderRadius: Radius.md,
    borderWidth: 3,
    justifyContent: 'center',
    alignItems: 'center',
    flexShrink: 0,
  },
  cardContent: {
    flex: 1,
    gap: 5,
  },
  cardTitle: {
    ...Typography.displayMd,
    fontSize: 16,
  },
  cardDesc: {
    ...Typography.body,
    fontSize: 12,
    lineHeight: 17,
  },
  cardMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    marginTop: 2,
  },
  cardMetaText: {
    ...Typography.caption,
    flex: 1,
    fontSize: 10,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.md,
    borderTopWidth: 1,
    paddingTop: Spacing.sm,
    marginTop: 4,
  },
  ownedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: Radius.full,
    borderWidth: 2,
  },
  ownedText: {
    ...Typography.caption,
    fontSize: 10,
    fontWeight: '900',
  },
  cardFooterNote: {
    ...Typography.caption,
    fontSize: 11,
    fontWeight: '900',
  },
  lockedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    paddingHorizontal: 10,
    paddingVertical: 5,
    backgroundColor: 'rgba(48,56,65,0.05)',
    borderRadius: Radius.full,
    borderWidth: 1,
    borderColor: 'rgba(48,56,65,0.1)',
  },
  lockedBadgeText: {
    ...Typography.caption,
    color: 'rgba(48,56,65,0.35)',
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
});