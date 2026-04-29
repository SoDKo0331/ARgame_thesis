import React, { useEffect, useRef } from 'react';
import {
  ActivityIndicator,
  Animated,
  Easing,
  Image,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';

import { Colors, Gradients, Radius, Shadows, Spacing, Typography } from '@/constants/theme';

type AppLoaderProps = {
  title: string;
  message: string;
  statusLabel?: string;
};

export function AppLoader({ title, message, statusLabel }: AppLoaderProps) {
  const rotation = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const loop = Animated.loop(
      Animated.timing(rotation, {
        toValue: 1,
        duration: 1600,
        easing: Easing.linear,
        useNativeDriver: true,
      })
    );

    loop.start();

    return () => {
      loop.stop();
      rotation.setValue(0);
    };
  }, [rotation]);

  const spin = rotation.interpolate({
    inputRange: [0, 1],
    outputRange: ['0deg', '360deg'],
  });

  return (
    <LinearGradient colors={Gradients.dark} style={styles.container}>
      <View pointerEvents="none" style={styles.decorLayer}>
        <View style={styles.topStripe} />
        <View style={styles.bottomStripe} />
      </View>

      <View style={[styles.card, Shadows.card]}>
        <Animated.View style={[styles.orbitRing, { transform: [{ rotate: spin }] }]}>
          <View style={styles.orbitAccent} />
        </Animated.View>

        <View style={styles.logoWrap}>
          <Image source={require('@/assets/images/logo.png')} style={styles.logo} resizeMode="contain" />
        </View>

        <Text style={styles.kicker}>NOMAD ADVENTURE</Text>
        <Text style={styles.title}>{title}</Text>
        <Text style={styles.message}>{message}</Text>

        <View style={styles.loaderRow}>
          <ActivityIndicator size="small" color={Colors.paper} />
          {statusLabel ? <Text style={styles.loaderLabel}>{statusLabel}</Text> : null}
        </View>
      </View>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.bg,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: Spacing.xl,
  },
  decorLayer: {
    ...StyleSheet.absoluteFillObject,
  },
  topStripe: {
    position: 'absolute',
    top: 118,
    right: -22,
    width: 150,
    height: 54,
    borderRadius: Radius.md,
    backgroundColor: Colors.surge,
    transform: [{ rotate: '11deg' }],
    opacity: 0.92,
  },
  bottomStripe: {
    position: 'absolute',
    left: -16,
    bottom: 140,
    width: 132,
    height: 40,
    borderRadius: Radius.md,
    backgroundColor: Colors.ember,
    transform: [{ rotate: '-9deg' }],
    opacity: 0.92,
  },
  card: {
    width: '100%',
    maxWidth: 360,
    borderWidth: 4,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    backgroundColor: Colors.paper,
    paddingHorizontal: Spacing.xl,
    paddingTop: 64,
    paddingBottom: Spacing.xl,
    alignItems: 'center',
    overflow: 'hidden',
  },
  orbitRing: {
    position: 'absolute',
    top: 22,
    width: 112,
    height: 112,
    borderRadius: Radius.full,
    borderWidth: 3,
    borderColor: 'rgba(0, 173, 181, 0.25)',
    alignItems: 'center',
  },
  orbitAccent: {
    marginTop: -7,
    width: 18,
    height: 18,
    borderRadius: Radius.full,
    backgroundColor: Colors.ember,
    borderWidth: 3,
    borderColor: Colors.ink,
  },
  logoWrap: {
    width: 88,
    height: 88,
    borderRadius: Radius.full,
    backgroundColor: Colors.bg,
    borderWidth: 4,
    borderColor: Colors.ink,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    width: 54,
    height: 54,
  },
  kicker: {
    ...Typography.caption,
    color: Colors.surge,
    marginTop: Spacing.lg,
  },
  title: {
    ...Typography.displayLg,
    color: Colors.ink,
    marginTop: Spacing.sm,
    textAlign: 'center',
  },
  message: {
    ...Typography.body,
    color: Colors.ink,
    opacity: 0.76,
    textAlign: 'center',
    marginTop: Spacing.sm,
  },
  loaderRow: {
    marginTop: Spacing.xl,
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
    backgroundColor: Colors.bg,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    paddingHorizontal: Spacing.base,
    paddingVertical: Spacing.sm,
  },
  loaderLabel: {
    ...Typography.label,
    color: Colors.paper,
  },
});
