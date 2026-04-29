import React, { useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Dimensions,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { LinearGradient } from 'expo-linear-gradient';
import Animated, {
  useSharedValue,
  useAnimatedStyle,
  withSpring,
  withRepeat,
  withTiming,
  Easing,
  interpolate,
} from 'react-native-reanimated';
import { Ionicons } from '@expo/vector-icons';
import * as Haptics from 'expo-haptics';

import { Colors, Spacing, Radius, Typography, Shadows } from '@/constants/theme';
import { useI18n } from '@/context/I18nContext';
import { RewardModelView } from '@/components/reward-model-view';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

export default function RewardSuccessScreen() {
  const router = useRouter();
  const { t } = useI18n();
  const {
    rewardName,
    rewardDescription,
    spotName,
    rewardType = 'common'
  } = useLocalSearchParams<{
    rewardName: string;
    rewardDescription: string;
    spotName: string;
    rewardType: string;
  }>();

  const scale = useSharedValue(0);
  const opacity = useSharedValue(0);
  const floatingValue = useSharedValue(0);
  const glowOpacity = useSharedValue(0);

  useEffect(() => {
    scale.value = withSpring(1, { damping: 12, stiffness: 90 });
    opacity.value = withTiming(1, { duration: 600 });
    floatingValue.value = withRepeat(withTiming(1, { duration: 2000, easing: Easing.inOut(Easing.sin) }), -1, true);
    glowOpacity.value = withRepeat(withTiming(1, { duration: 1500, easing: Easing.inOut(Easing.sin) }), -1, true);
    Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
  }, []);

  const containerStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
    transform: [{ scale: scale.value }],
  }));

  const floatStyle = useAnimatedStyle(() => ({
    transform: [{ translateY: interpolate(floatingValue.value, [0, 1], [0, -20]) }],
  }));

  const glowStyle = useAnimatedStyle(() => ({
    opacity: interpolate(glowOpacity.value, [0, 1], [0.4, 0.8]),
    transform: [{ scale: interpolate(glowOpacity.value, [0, 1], [1, 1.2]) }],
  }));

  const handleContinue = () => {
    Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
    router.dismissAll();
    router.replace('/(tabs)');
  };

  const getRarityColor = () => {
    switch (rewardType) {
      case 'legendary': return '#FFD700';
      case 'epic': return '#A335EE';
      case 'rare': return Colors.pokeBlue;
      default: return Colors.pokeGreen;
    }
  };

  const rarityColor = getRarityColor();

  return (
    <View style={styles.container}>
      <LinearGradient colors={[Colors.pokeLight, Colors.paper]} style={StyleSheet.absoluteFillObject} />
      
      <Animated.View style={[styles.content, containerStyle]}>
        <View style={styles.header}>
          <Text style={styles.congratsText}>{t('reward.congratulations') || 'EXCELLENT!'}</Text>
          <Text style={styles.subText}>{t('reward.foundIn') || 'New Item Found at'}</Text>
          <Text style={styles.spotText}>{spotName}</Text>
        </View>

        <View style={styles.showcaseContainer}>
          <Animated.View style={[styles.glowRing, glowStyle, { borderColor: rarityColor }]} />
          <Animated.View style={[styles.rewardWrapper, floatStyle]}>
            <View style={[styles.rewardIconContainer, Shadows.float]}>
              <RewardModelView rarityColor={rarityColor} />
            </View>
            <View style={[styles.rarityBadge, { backgroundColor: rarityColor }]}>
              <Text style={styles.rarityText}>{rewardType.toUpperCase()}</Text>
            </View>
          </Animated.View>
        </View>

        <View style={[styles.detailsCard, Shadows.float]}>
          <Text style={styles.rewardTitle}>{rewardName}</Text>
          <Text style={styles.rewardDesc}>{rewardDescription}</Text>
        </View>

        <TouchableOpacity style={[styles.actionBtn, Shadows.float]} onPress={handleContinue} activeOpacity={0.9}>
          <Text style={styles.actionBtnText}>{t('reward.collectButton') || 'ADD TO BAG'}</Text>
          <Ionicons name="briefcase" size={20} color="#fff" />
        </TouchableOpacity>
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  content: { width: '100%', paddingHorizontal: Spacing.xl, alignItems: 'center', gap: 40 },
  header: { alignItems: 'center', gap: 8 },
  congratsText: { ...Typography.displayLg, color: Colors.pokeRed, fontSize: 44, textAlign: 'center' },
  subText: { ...Typography.body, color: Colors.textSecondary, fontSize: 16 },
  spotText: { ...Typography.heading, color: Colors.pokeBlue, fontSize: 22 },
  showcaseContainer: { width: 260, height: 260, justifyContent: 'center', alignItems: 'center' },
  glowRing: { position: 'absolute', width: 240, height: 240, borderRadius: 120, borderWidth: 4, opacity: 0.3 },
  rewardWrapper: { width: 200, height: 200, justifyContent: 'center', alignItems: 'center' },
  rewardIconContainer: { 
    width: 180, height: 180, borderRadius: 90, 
    backgroundColor: Colors.paper, justifyContent: 'center', alignItems: 'center',
  },
  rarityBadge: { 
    position: 'absolute', bottom: 10, paddingHorizontal: 16, paddingVertical: 6, 
    borderRadius: 20, borderWidth: 3, borderColor: Colors.paper,
  },
  rarityText: { ...Typography.label, color: Colors.paper, fontSize: 12, fontWeight: '900' },
  detailsCard: { 
    width: '100%', backgroundColor: Colors.paper, borderRadius: Radius.xl, 
    padding: Spacing.xl, alignItems: 'center', gap: 12,
  },
  rewardTitle: { ...Typography.displayMd, color: Colors.pokeDark, fontSize: 26, textAlign: 'center' },
  rewardDesc: { ...Typography.body, color: Colors.textSecondary, textAlign: 'center', fontSize: 15 },
  actionBtn: { 
    width: '100%', height: 60, borderRadius: 30, backgroundColor: Colors.pokeRed,
    flexDirection: 'row', justifyContent: 'center', alignItems: 'center', gap: 12,
  },
  actionBtnText: { ...Typography.heading, color: Colors.paper, fontSize: 18, letterSpacing: 1 },
});
