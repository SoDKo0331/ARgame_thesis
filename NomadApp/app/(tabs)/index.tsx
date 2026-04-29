import React from 'react';
import { Platform, StyleSheet, Text, View } from 'react-native';

import { Colors, Radius, Shadows, Spacing, Typography } from '@/constants/theme';

function HomeMapUnavailable({ message }: { message: string }) {
  return (
    <View style={styles.container}>
      <View style={[styles.card, Shadows.card]}>
        <Text style={styles.title}>Map screen unavailable</Text>
        <Text style={styles.body}>{message}</Text>
      </View>
    </View>
  );
}

export default function TabsIndexRoute() {
  if (Platform.OS === 'web') {
    return (
      <HomeMapUnavailable message="The interactive Mapbox screen is only enabled on iOS and Android builds." />
    );
  }

  try {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const HomeMapScreen = require('../../components/screens/home-map-screen').default as React.ComponentType;
    return <HomeMapScreen />;
  } catch (error) {
    console.warn('[TabsIndexRoute] Failed to load home map screen:', error);

    return (
      <HomeMapUnavailable message="The map module could not be loaded in this build. If you are using Expo Go, open the project in a development build instead." />
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.bg,
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.lg,
  },
  card: {
    width: '100%',
    backgroundColor: Colors.paper,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    padding: Spacing.lg,
    gap: Spacing.sm,
  },
  title: {
    ...Typography.displayMd,
    color: Colors.ink,
  },
  body: {
    ...Typography.body,
    color: Colors.ink,
  },
});
