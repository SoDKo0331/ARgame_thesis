import React from 'react';
import Constants from 'expo-constants';
import { useRouter } from 'expo-router';
import { Image } from 'expo-image';
import {
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { useI18n } from '@/context/I18nContext';
import { Colors, Radius, Shadows, Spacing, Typography } from '@/constants/theme';

const GUIDE_LOGO = require('@/assets/images/logo.png');

export default function ModalScreen() {
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const { language, t } = useI18n();
  const appVersion = Constants.expoConfig?.version ?? '1.0.0';
  const currentLanguage = language === 'mn' ? t('languages.mongolian') : t('languages.english');
  const steps = [
    {
      icon: 'location-outline' as const,
      title: t('guide.stepOneTitle'),
      body: t('guide.stepOneBody'),
      color: Colors.paper,
    },
    {
      icon: 'sparkles-outline' as const,
      title: t('guide.stepTwoTitle'),
      body: t('guide.stepTwoBody'),
      color: Colors.surge,
    },
    {
      icon: 'gift-outline' as const,
      title: t('guide.stepThreeTitle'),
      body: t('guide.stepThreeBody'),
      color: Colors.ember,
    },
  ];

  return (
    <View style={styles.container}>
      <ScrollView
        contentContainerStyle={{
          paddingTop: insets.top + Spacing.base,
          paddingBottom: insets.bottom + Spacing.xxl,
        }}
        showsVerticalScrollIndicator={false}
      >
        <View pointerEvents="none" style={styles.decorLayer}>
          <View style={styles.topStripe} />
          <View style={styles.bottomStripe} />
        </View>

        <View style={styles.shell}>
          <TouchableOpacity
            style={[styles.closeButton, Shadows.punch]}
            activeOpacity={0.86}
            onPress={() => router.back()}
          >
            <Ionicons name="close" size={18} color={Colors.paper} />
            <Text style={styles.closeText}>{t('common.close')}</Text>
          </TouchableOpacity>

          <View style={[styles.heroCard, Shadows.card]}>
            <View style={styles.heroStamp}>
              <Text style={styles.heroStampText}>{t('common.live')}</Text>
            </View>

            <Image
              source={GUIDE_LOGO}
              style={styles.logo}
              contentFit="contain"
            />

            <Text style={styles.heroTitle}>{t('guide.title')}</Text>
            <Text style={styles.heroBody}>{t('guide.subtitle')}</Text>

            <View style={styles.heroMeta}>
              <View style={[styles.metaChip, styles.metaChipDark]}>
                <Text style={styles.metaChipTextLight}>{t('guide.versionPrefix', { version: appVersion })}</Text>
              </View>
              <View style={[styles.metaChip, styles.metaChipTeal]}>
                <Text style={styles.metaChipTextLight}>{t('guide.currentLanguage')}: {currentLanguage}</Text>
              </View>
            </View>
          </View>

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>{t('guide.howTitle')}</Text>
            {steps.map((step, index) => (
              <View
                key={step.title}
                style={[styles.stepCard, { backgroundColor: step.color }, Shadows.card]}
              >
                <View style={styles.stepRow}>
                  <View style={styles.stepBadge}>
                    <Text style={styles.stepBadgeText}>{index + 1}</Text>
                  </View>
                  <View style={styles.stepIcon}>
                    <Ionicons name={step.icon} size={18} color={Colors.ink} />
                  </View>
                </View>
                <Text style={styles.stepTitle}>{step.title}</Text>
                <Text style={styles.stepBody}>{step.body}</Text>
              </View>
            ))}
          </View>

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>{t('guide.quickActionsTitle')}</Text>
            <View style={styles.actionsRow}>
              <TouchableOpacity
                style={[styles.actionButton, styles.actionButtonOrange, Shadows.punch]}
                activeOpacity={0.86}
                onPress={() => router.replace('/(tabs)')}
              >
                <Text style={styles.actionText}>{t('guide.openMap')}</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.actionButton, styles.actionButtonTeal, Shadows.punch]}
                activeOpacity={0.86}
                onPress={() => router.replace('/(tabs)/explore')}
              >
                <Text style={styles.actionText}>{t('guide.openLedger')}</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.actionButton, styles.actionButtonPaper, Shadows.punch]}
                activeOpacity={0.86}
                onPress={() => router.replace('/(tabs)/profile')}
              >
                <Text style={styles.actionTextDark}>{t('guide.openProfile')}</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </ScrollView>
    </View>
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
    top: 108,
    right: -24,
    width: 150,
    height: 44,
    backgroundColor: Colors.surge,
    borderRadius: Radius.md,
    transform: [{ rotate: '8deg' }],
  },
  bottomStripe: {
    position: 'absolute',
    left: -14,
    bottom: 180,
    width: 122,
    height: 38,
    backgroundColor: Colors.ember,
    borderRadius: Radius.md,
    transform: [{ rotate: '-8deg' }],
  },
  shell: {
    paddingHorizontal: Spacing.lg,
    gap: Spacing.lg,
  },
  closeButton: {
    alignSelf: 'flex-end',
    backgroundColor: Colors.ink,
    borderWidth: 3,
    borderColor: Colors.paper,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: 10,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  closeText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  heroCard: {
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.xxl,
    paddingBottom: Spacing.lg,
    alignItems: 'center',
  },
  heroStamp: {
    position: 'absolute',
    top: -14,
    left: 18,
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
  logo: {
    width: 132,
    height: 132,
    marginBottom: Spacing.sm,
  },
  heroTitle: {
    ...Typography.displayLg,
    color: Colors.ink,
    textAlign: 'center',
  },
  heroBody: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.82)',
    textAlign: 'center',
    marginTop: 8,
  },
  heroMeta: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: Spacing.sm,
    marginTop: Spacing.base,
  },
  metaChip: {
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: 8,
  },
  metaChipDark: {
    backgroundColor: Colors.ink,
  },
  metaChipTeal: {
    backgroundColor: Colors.surge,
  },
  metaChipTextLight: {
    ...Typography.caption,
    color: Colors.paper,
  },
  section: {
    gap: Spacing.sm,
  },
  sectionTitle: {
    ...Typography.label,
    color: Colors.paper,
    marginLeft: 4,
  },
  stepCard: {
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.lg,
  },
  stepRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  stepBadge: {
    width: 34,
    height: 34,
    borderRadius: 17,
    backgroundColor: Colors.ink,
    borderWidth: 2,
    borderColor: Colors.paper,
    justifyContent: 'center',
    alignItems: 'center',
  },
  stepBadgeText: {
    ...Typography.caption,
    color: Colors.paper,
  },
  stepIcon: {
    width: 38,
    height: 38,
    borderRadius: Radius.md,
    backgroundColor: 'rgba(48, 56, 65, 0.08)',
    borderWidth: 2,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
  },
  stepTitle: {
    ...Typography.title,
    color: Colors.ink,
    marginTop: Spacing.md,
  },
  stepBody: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.82)',
    marginTop: 6,
  },
  actionsRow: {
    gap: Spacing.sm,
  },
  actionButton: {
    minHeight: 54,
    borderRadius: Radius.lg,
    borderWidth: 3,
    borderColor: Colors.ink,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
  },
  actionButtonOrange: {
    backgroundColor: Colors.ember,
  },
  actionButtonTeal: {
    backgroundColor: Colors.surge,
  },
  actionButtonPaper: {
    backgroundColor: Colors.paper,
  },
  actionText: {
    ...Typography.heading,
    color: Colors.paper,
  },
  actionTextDark: {
    ...Typography.heading,
    color: Colors.ink,
  },
});
