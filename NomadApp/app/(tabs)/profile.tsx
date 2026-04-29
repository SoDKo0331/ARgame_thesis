import React, { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Linking,
  Platform,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
  ScrollView,
  Modal,
  Pressable,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useRouter } from 'expo-router';
import Constants from 'expo-constants';

import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';
import { Colors, Radius, Shadows, Spacing, Typography, Layout } from '@/constants/theme';
import type { Spot } from '@/services/api';

export default function ProfileScreen() {
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const {
    user,
    rewards,
    spots,
    fetchSpots,
    fetchRewards,
    spotsLoading,
    rewardsLoading,
    requestEmailVerification,
    verifyEmailCode,
  } = useApp();
  const { language, toggleLanguage, formatDisplayName, t } = useI18n();
  const [isRouteBoardOpen, setIsRouteBoardOpen] = useState(false);
  const [emailInput, setEmailInput] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [emailNotice, setEmailNotice] = useState<string | null>(null);
  const [authAction, setAuthAction] = useState<'request' | 'verify' | null>(null);
  const tabPadding = Layout.tabBarHeight + insets.bottom + Spacing.xxxl + 40;
  const isRefreshing = spotsLoading || rewardsLoading;
  const appVersion = Constants.expoConfig?.version ?? '1.0.0';
  const isEmailVerified = Boolean(user?.isEmailVerified);
  const directionsCtaLabel =
    Platform.OS === 'ios'
      ? t('profile.routeOpenMaps')
      : t('profile.routeOpenDirections');

  const displayName = formatDisplayName(user?.displayName);
  const initials = displayName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
  const languageLabel = useMemo(
    () => (language === 'mn' ? t('languages.mongolian') : t('languages.english')),
    [language, t]
  );

  useEffect(() => {
    fetchSpots();
    fetchRewards();
  }, [fetchSpots, fetchRewards]);

  useEffect(() => {
    if (user?.email) {
      setEmailInput(user.email);
    }
  }, [user?.email]);

  const handleRefreshData = () => {
    fetchSpots();
    fetchRewards();
  };

  const formatDistanceLabel = (distanceMeters?: number) => {
    if (typeof distanceMeters !== 'number' || !Number.isFinite(distanceMeters)) {
      return null;
    }

    if (distanceMeters < 1000) {
      return t('profile.routeDistanceMeters', { count: Math.round(distanceMeters) });
    }

    const distanceKilometers =
      distanceMeters < 10_000
        ? (distanceMeters / 1000).toFixed(1)
        : String(Math.round(distanceMeters / 1000));

    return t('profile.routeDistanceKilometers', { count: distanceKilometers });
  };

  const formatCoordinateLabel = (spot: Spot) =>
    `${spot.latitude.toFixed(5)}, ${spot.longitude.toFixed(5)}`;

  const handleOpenSpotDirections = async (spot: Spot) => {
    const encodedLabel = encodeURIComponent(spot.name);
    const destination = `${spot.latitude},${spot.longitude}`;
    const url =
      Platform.OS === 'ios'
        ? `http://maps.apple.com/?daddr=${destination}&q=${encodedLabel}`
        : `geo:0,0?q=${destination}(${encodedLabel})`;

    try {
      await Linking.openURL(url);
    } catch (error: any) {
      Alert.alert(
        t('profile.routeMapErrorTitle'),
        error?.message ?? t('profile.routeMapErrorBody')
      );
    }
  };

  const handleRequestOtp = async () => {
    const nextEmail = emailInput.trim().toLowerCase();

    if (!nextEmail) {
      Alert.alert(t('profile.emailErrorTitle'), t('profile.emailMissingBody'));
      return;
    }

    try {
      setAuthAction('request');
      const response = await requestEmailVerification(nextEmail);

      if (response.alreadyVerified) {
        setEmailNotice(t('profile.emailAlreadyVerified', { email: response.maskedEmail }));
        return;
      }

      setEmailNotice(
        response.deliveryMethod === 'console'
          ? t('profile.emailSentConsole', { email: response.maskedEmail })
          : t('profile.emailSentBody', {
              email: response.maskedEmail,
              minutes: response.expiresInMinutes ?? 5,
            })
      );
    } catch (error: any) {
      Alert.alert(t('profile.emailErrorTitle'), error?.message ?? t('profile.emailRequestFailed'));
    } finally {
      setAuthAction(null);
    }
  };

  const handleVerifyOtp = async () => {
    const nextEmail = emailInput.trim().toLowerCase();
    const normalizedCode = otpCode.replace(/\s+/g, '');

    if (!nextEmail) {
      Alert.alert(t('profile.emailErrorTitle'), t('profile.emailMissingBody'));
      return;
    }

    if (normalizedCode.length !== 6) {
      Alert.alert(t('profile.emailErrorTitle'), t('profile.emailCodeMissingBody'));
      return;
    }

    try {
      setAuthAction('verify');
      const response = await verifyEmailCode(nextEmail, normalizedCode);
      setOtpCode('');
      setEmailNotice(
        t('profile.emailVerifiedBodyMessage', {
          email: response.user.email ?? nextEmail,
        })
      );
    } catch (error: any) {
      Alert.alert(t('profile.emailErrorTitle'), error?.message ?? t('profile.emailVerifyFailed'));
    } finally {
      setAuthAction(null);
    }
  };

  const SettingButton = ({ 
    icon, 
    label, 
    value, 
    onPress, 
    color = Colors.paper 
  }: { 
    icon: keyof typeof Ionicons.glyphMap; 
    label: string; 
    value?: string; 
    onPress: () => void;
    color?: string;
  }) => (
    <TouchableOpacity 
      style={[styles.settingBtn, { backgroundColor: color }, Shadows.punch]} 
      activeOpacity={0.86} 
      onPress={onPress}
    >
      <View style={styles.settingBtnInner}>
        <View style={styles.settingIconWrap}>
          <Ionicons name={icon} size={22} color={Colors.ink} />
        </View>
        <Text style={styles.settingLabel}>{label}</Text>
      </View>
      <View style={styles.settingValueRow}>
        {value && <Text style={styles.settingValue}>{value}</Text>}
        <Ionicons name="chevron-forward" size={18} color={Colors.ink} />
      </View>
    </TouchableOpacity>
  );

  return (
    <>
      <ScrollView
        style={styles.container}
        contentContainerStyle={{ paddingTop: insets.top, paddingBottom: tabPadding }}
        showsVerticalScrollIndicator={false}
      >
        <View pointerEvents="none" style={styles.decorLayer}>
          <View style={styles.topStripe} />
          <View style={styles.bottomStripe} />
        </View>

        <View style={styles.headerShell}>
          <View style={[styles.profileCard, Shadows.card]}>
            <View style={styles.profileStamp}>
              <Text style={styles.profileStampText}>{t('profile.levelStamp')}</Text>
            </View>

            <View style={styles.profileTop}>
              <View style={styles.avatarWrap}>
                <Text style={styles.avatarText}>{initials}</Text>
              </View>
              <View style={styles.profileInfo}>
                <Text style={styles.profileName}>{displayName}</Text>
                <Text style={styles.profileSub}>{t('home.profileRole')}</Text>
              </View>
            </View>
          </View>

          <View style={styles.statsGrid}>
            <View style={[styles.statBox, { backgroundColor: Colors.surge }, Shadows.punch]}>
              <Text style={styles.statValue}>{spots.length}</Text>
              <Text style={styles.statLabel}>{t('inventory.spots')}</Text>
            </View>
            <View style={[styles.statBox, { backgroundColor: Colors.ember }, Shadows.punch]}>
              <Text style={styles.statValue}>{rewards.length}</Text>
              <Text style={styles.statLabel}>{t('inventory.collected')}</Text>
            </View>
          </View>

          <View style={[styles.emailCard, Shadows.card]}>
            <View style={styles.emailHeaderRow}>
              <View style={styles.emailHeaderCopy}>
                <Text style={styles.emailKicker}>{t('profile.emailKicker')}</Text>
                <Text style={styles.emailTitle}>{t('profile.emailTitle')}</Text>
                <Text style={styles.emailBodyText}>
                  {isEmailVerified ? t('profile.emailVerifiedHelp') : t('profile.emailHelp')}
                </Text>
              </View>
              <View
                style={[
                  styles.emailStatusChip,
                  isEmailVerified ? styles.emailStatusVerified : styles.emailStatusPending,
                ]}>
                <Text style={styles.emailStatusText}>
                  {isEmailVerified ? t('profile.emailVerified') : t('profile.emailPending')}
                </Text>
              </View>
            </View>

            <Text style={styles.inputLabel}>{t('profile.emailInputLabel')}</Text>
            <TextInput
              value={emailInput}
              onChangeText={setEmailInput}
              placeholder={t('profile.emailPlaceholder')}
              placeholderTextColor="rgba(48, 56, 65, 0.42)"
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              style={styles.input}
            />

            <Text style={styles.inputLabel}>{t('profile.codeInputLabel')}</Text>
            <TextInput
              value={otpCode}
              onChangeText={setOtpCode}
              placeholder={t('profile.codePlaceholder')}
              placeholderTextColor="rgba(48, 56, 65, 0.42)"
              keyboardType="number-pad"
              maxLength={6}
              style={styles.input}
            />

            {emailNotice ? <Text style={styles.emailNotice}>{emailNotice}</Text> : null}

            <View style={styles.emailActions}>
              <TouchableOpacity
                style={[styles.emailActionBtn, styles.emailActionPrimary, Shadows.punch]}
                activeOpacity={0.86}
                onPress={handleRequestOtp}
                disabled={authAction !== null}>
                <Text style={styles.emailActionPrimaryText}>
                  {authAction === 'request' ? t('profile.emailSending') : t('profile.emailSendCode')}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.emailActionBtn, styles.emailActionSecondary, Shadows.punch]}
                activeOpacity={0.86}
                onPress={handleVerifyOtp}
                disabled={authAction !== null}>
                <Text style={styles.emailActionSecondaryText}>
                  {authAction === 'verify' ? t('profile.emailVerifying') : t('profile.emailVerify')}
                </Text>
              </TouchableOpacity>
            </View>
          </View>

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>{t('profile.sectionTitle')}</Text>

            <SettingButton
              icon="language-outline"
              label={t('profile.languageLabel')}
              value={languageLabel}
              onPress={toggleLanguage}
            />

            <SettingButton
              icon="information-circle-outline"
              label={t('profile.boardLabel')}
              onPress={() => setIsRouteBoardOpen(true)}
            />

            <SettingButton
              icon="refresh-outline"
              label={t('profile.refreshLabel')}
              value={isRefreshing ? t('profile.refreshLoading') : t('profile.refreshIdle')}
              onPress={handleRefreshData}
            />

            <SettingButton
              icon="sparkles-outline"
              label={t('profile.guideLabel')}
              value={t('profile.guideValue')}
              onPress={() => router.push('/modal')}
            />

            <SettingButton
              icon="map-outline"
              label={t('profile.mapLabel')}
              onPress={() => router.push('/(tabs)')}
            />

            <SettingButton
              icon="journal-outline"
              label={t('profile.ledgerLabel')}
              onPress={() => router.push('/(tabs)/explore')}
            />
          </View>

          <View style={styles.routeSection}>
            <View style={styles.routeSectionHeader}>
              <View style={styles.routeSectionCopy}>
                <Text style={styles.routeSectionTitle}>{t('profile.routeTitle')}</Text>
                <Text style={styles.routeSectionBody}>{t('profile.routeBody')}</Text>
              </View>
              <View style={[styles.routeSectionBadge, Shadows.punch]}>
                <Text style={styles.routeSectionBadgeText}>{spots.length}</Text>
              </View>
            </View>

            {spotsLoading ? (
              <View style={[styles.routeStateCard, Shadows.card]}>
                <ActivityIndicator size="small" color={Colors.surge} />
                <Text style={styles.routeStateText}>{t('profile.routeLoading')}</Text>
              </View>
            ) : spots.length === 0 ? (
              <View style={[styles.routeStateCard, Shadows.card]}>
                <Ionicons name="trail-sign-outline" size={24} color={Colors.ink} />
                <Text style={styles.routeStateText}>{t('profile.routeEmpty')}</Text>
              </View>
            ) : (
              <View style={styles.routeCards}>
                {spots.map((spot) => {
                  const distanceLabel = formatDistanceLabel(spot.distanceMeters);

                  return (
                    <View key={spot.id} style={[styles.routeCard, Shadows.card]}>
                      <View style={styles.routeCardTopRow}>
                        <View style={styles.routeIconWrap}>
                          <Ionicons name="location" size={18} color={Colors.paper} />
                        </View>
                        {distanceLabel ? (
                          <View style={styles.routeDistanceBadge}>
                            <Text style={styles.routeDistanceBadgeText}>
                              {t('profile.routeDistanceAway', { distance: distanceLabel })}
                            </Text>
                          </View>
                        ) : null}
                      </View>

                      <Text style={styles.routeCardTitle}>{spot.name}</Text>
                      <Text style={styles.routeCardBody}>
                        {spot.description ?? t('profile.routeNoDescription')}
                      </Text>

                      <View style={styles.routeMetaList}>
                        <View style={styles.routeMetaRow}>
                          <Ionicons name="navigate-outline" size={16} color={Colors.surge} />
                          <Text style={styles.routeMetaLabel}>{t('profile.routeCoordinates')}</Text>
                          <Text style={styles.routeMetaValue}>{formatCoordinateLabel(spot)}</Text>
                        </View>

                        {spot.reward ? (
                          <View style={styles.routeMetaRow}>
                            <Ionicons name="gift-outline" size={16} color={Colors.ember} />
                            <Text style={styles.routeMetaLabel}>{t('profile.routeReward')}</Text>
                            <Text style={styles.routeMetaValue} numberOfLines={1}>
                              {spot.reward.name}
                            </Text>
                          </View>
                        ) : null}
                      </View>

                      <TouchableOpacity
                        style={[styles.routeActionBtn, Shadows.punch]}
                        activeOpacity={0.86}
                        onPress={() => handleOpenSpotDirections(spot)}
                      >
                        <Ionicons name="navigate-circle-outline" size={18} color={Colors.paper} />
                        <Text style={styles.routeActionText}>{directionsCtaLabel}</Text>
                      </TouchableOpacity>
                    </View>
                  );
                })}
              </View>
            )}
          </View>

          <View style={[styles.versionChip, Shadows.punch]}>
            {isRefreshing ? <ActivityIndicator size="small" color={Colors.paper} /> : null}
            <Text style={styles.versionChipText}>
              {t('profile.versionLabel')} {appVersion}
            </Text>
            <Text style={styles.versionChipDivider}>•</Text>
            <Text style={styles.versionChipText}>{languageLabel}</Text>
          </View>
        </View>

        <View style={{ height: Spacing.base }} />
      </ScrollView>

      <Modal
        animationType="fade"
        transparent
        visible={isRouteBoardOpen}
        onRequestClose={() => setIsRouteBoardOpen(false)}
      >
        <View style={styles.modalBackdrop}>
          <Pressable
            style={StyleSheet.absoluteFillObject}
            onPress={() => setIsRouteBoardOpen(false)}
          />

          <View style={[styles.modalCard, Shadows.card]}>
            <View style={styles.heroStamp}>
              <Text style={styles.heroStampText}>{t('common.live')}</Text>
            </View>

            <TouchableOpacity
              style={styles.modalClose}
              activeOpacity={0.85}
              onPress={() => setIsRouteBoardOpen(false)}
            >
              <Ionicons name="close" size={18} color={Colors.paper} />
            </TouchableOpacity>

            <Text style={styles.heroLabel}>{t('home.heroLabel')}</Text>
            <Text style={styles.heroTitle}>{t('home.heroTitle')}</Text>
            <Text style={styles.heroSubtitle}>
              {t('home.heroSubtitle', { name: displayName.split(' ')[0] ?? t('common.guestName') })}
            </Text>

            <View style={styles.heroChips}>
              <View style={[styles.heroChip, styles.heroChipPaper]}>
                <Text style={styles.heroChipNumberDark}>{spots.length}</Text>
                <Text style={styles.heroChipLabelDark}>{t('common.spots')}</Text>
              </View>
              <View style={[styles.heroChip, styles.heroChipTeal]}>
                <Text style={styles.heroChipNumberLight}>{rewards.length}</Text>
                <Text style={styles.heroChipLabelLight}>{t('common.rewards')}</Text>
              </View>
            </View>
          </View>
        </View>
      </Modal>
    </>
  );
}

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
    top: 60,
    left: -20,
    width: 140,
    height: 48,
    backgroundColor: Colors.surge,
    borderRadius: Radius.md,
    transform: [{ rotate: '-8deg' }],
  },
  bottomStripe: {
    position: 'absolute',
    right: -24,
    top: 400,
    width: 120,
    height: 40,
    backgroundColor: Colors.ember,
    borderRadius: Radius.md,
    transform: [{ rotate: '12deg' }],
  },
  headerShell: {
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.sm,
    gap: Spacing.xl,
  },
  profileCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    paddingTop: Spacing.xxl,
  },
  profileStamp: {
    position: 'absolute',
    top: -14,
    left: 20,
    paddingHorizontal: Spacing.md,
    paddingVertical: 8,
    backgroundColor: Colors.ember,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    transform: [{ rotate: '-3deg' }],
  },
  profileStampText: {
    ...Typography.caption,
    color: Colors.paper,
    fontWeight: '900',
  },
  profileTop: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.md,
  },
  avatarWrap: {
    width: 80,
    height: 80,
    borderRadius: Radius.xl,
    backgroundColor: Colors.surge,
    borderWidth: 4,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    ...Typography.displayMd,
    color: Colors.paper,
    fontSize: 28,
  },
  profileInfo: {
    flex: 1,
    gap: 4,
  },
  profileName: {
    ...Typography.displayMd,
    color: Colors.ink,
  },
  profileSub: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.65)',
  },
  statsGrid: {
    flexDirection: 'row',
    gap: Spacing.md,
  },
  statBox: {
    flex: 1,
    height: 100,
    borderRadius: Radius.lg,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
    gap: 2,
  },
  statValue: {
    ...Typography.displayMd,
    color: Colors.paper,
    fontSize: 32,
  },
  statLabel: {
    ...Typography.caption,
    color: Colors.paper,
    textTransform: 'uppercase',
    letterSpacing: 1,
  },
  emailCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    gap: Spacing.sm,
  },
  emailHeaderRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: Spacing.md,
  },
  emailHeaderCopy: {
    flex: 1,
    gap: 4,
  },
  emailKicker: {
    ...Typography.caption,
    color: Colors.surge,
  },
  emailTitle: {
    ...Typography.displayMd,
    color: Colors.ink,
  },
  emailBodyText: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.76)',
  },
  emailStatusChip: {
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: 8,
  },
  emailStatusVerified: {
    backgroundColor: Colors.surge,
  },
  emailStatusPending: {
    backgroundColor: Colors.ember,
  },
  emailStatusText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  inputLabel: {
    ...Typography.label,
    color: Colors.ink,
    marginTop: 4,
  },
  input: {
    ...Typography.body,
    color: Colors.ink,
    backgroundColor: 'rgba(48, 56, 65, 0.05)',
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: 12,
  },
  emailNotice: {
    ...Typography.body,
    color: Colors.surge,
    marginTop: 4,
  },
  emailActions: {
    flexDirection: 'row',
    gap: Spacing.sm,
    marginTop: 4,
  },
  emailActionBtn: {
    flex: 1,
    minHeight: 50,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: Spacing.sm,
  },
  emailActionPrimary: {
    backgroundColor: Colors.ember,
  },
  emailActionSecondary: {
    backgroundColor: Colors.surge,
  },
  emailActionPrimaryText: {
    ...Typography.heading,
    color: Colors.paper,
    textAlign: 'center',
  },
  emailActionSecondaryText: {
    ...Typography.heading,
    color: Colors.paper,
    textAlign: 'center',
  },
  section: {
    gap: Spacing.sm,
  },
  sectionTitle: {
    ...Typography.label,
    color: Colors.paper,
    marginLeft: 4,
    marginBottom: 4,
    opacity: 0.8,
  },
  settingBtn: {
    borderRadius: Radius.lg,
    borderWidth: 3,
    borderColor: Colors.ink,
    padding: Spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  settingBtnInner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.md,
  },
  settingIconWrap: {
    width: 40,
    height: 40,
    borderRadius: Radius.md,
    backgroundColor: 'rgba(48, 56, 65, 0.05)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  settingLabel: {
    ...Typography.heading,
    color: Colors.ink,
  },
  settingValueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  settingValue: {
    ...Typography.caption,
    color: Colors.surge,
    fontWeight: '800',
  },
  routeSection: {
    gap: Spacing.md,
  },
  routeSectionHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: Spacing.md,
  },
  routeSectionCopy: {
    flex: 1,
    gap: 4,
  },
  routeSectionTitle: {
    ...Typography.displayMd,
    color: Colors.paper,
  },
  routeSectionBody: {
    ...Typography.body,
    color: 'rgba(238, 238, 238, 0.82)',
  },
  routeSectionBadge: {
    minWidth: 52,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: Colors.surge,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    paddingHorizontal: Spacing.md,
    paddingVertical: 10,
  },
  routeSectionBadgeText: {
    ...Typography.title,
    color: Colors.paper,
  },
  routeStateCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.lg,
    alignItems: 'center',
    gap: Spacing.sm,
  },
  routeStateText: {
    ...Typography.body,
    color: Colors.ink,
    textAlign: 'center',
  },
  routeCards: {
    gap: Spacing.md,
  },
  routeCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    gap: Spacing.sm,
  },
  routeCardTopRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  routeIconWrap: {
    width: 40,
    height: 40,
    borderRadius: Radius.md,
    backgroundColor: Colors.ember,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
  },
  routeDistanceBadge: {
    backgroundColor: 'rgba(15, 118, 110, 0.12)',
    borderWidth: 2,
    borderColor: Colors.surge,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: 7,
  },
  routeDistanceBadgeText: {
    ...Typography.caption,
    color: Colors.surge,
    fontWeight: '800',
  },
  routeCardTitle: {
    ...Typography.displayMd,
    color: Colors.ink,
  },
  routeCardBody: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.76)',
  },
  routeMetaList: {
    gap: Spacing.xs,
  },
  routeMetaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  routeMetaLabel: {
    ...Typography.caption,
    color: 'rgba(48, 56, 65, 0.7)',
    minWidth: 76,
  },
  routeMetaValue: {
    ...Typography.body,
    color: Colors.ink,
    flex: 1,
  },
  routeActionBtn: {
    minHeight: 52,
    marginTop: 4,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    backgroundColor: Colors.surge,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingHorizontal: Spacing.md,
  },
  routeActionText: {
    ...Typography.heading,
    color: Colors.paper,
  },
  versionChip: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 8,
    backgroundColor: Colors.ink,
    borderWidth: 3,
    borderColor: Colors.paper,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: 10,
  },
  versionChipText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  versionChipDivider: {
    ...Typography.caption,
    color: Colors.paper,
    opacity: 0.7,
  },
  modalBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(48, 56, 65, 0.58)',
    justifyContent: 'center',
    paddingHorizontal: Spacing.lg,
  },
  modalCard: {
    width: '100%',
    maxWidth: 420,
    alignSelf: 'center',
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.xxl,
    paddingBottom: Spacing.lg,
  },
  modalClose: {
    position: 'absolute',
    top: 14,
    right: 14,
    width: 42,
    height: 42,
    borderRadius: Radius.full,
    backgroundColor: Colors.ink,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
  },
  heroStamp: {
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
  heroStampText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  heroLabel: {
    ...Typography.caption,
    color: Colors.surge,
    marginBottom: 6,
  },
  heroTitle: {
    ...Typography.displayLg,
    color: Colors.ink,
  },
  heroSubtitle: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.82)',
    marginTop: 8,
  },
  heroChips: {
    flexDirection: 'row',
    gap: Spacing.sm,
    marginTop: Spacing.base,
  },
  heroChip: {
    minWidth: 96,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    paddingHorizontal: Spacing.md,
    paddingVertical: 10,
  },
  heroChipPaper: {
    backgroundColor: Colors.paper,
  },
  heroChipTeal: {
    backgroundColor: Colors.surge,
  },
  heroChipNumberDark: {
    ...Typography.title,
    color: Colors.ink,
  },
  heroChipNumberLight: {
    ...Typography.title,
    color: Colors.paper,
  },
  heroChipLabelDark: {
    ...Typography.caption,
    color: Colors.ink,
  },
  heroChipLabelLight: {
    ...Typography.caption,
    color: Colors.paper,
  },
});
