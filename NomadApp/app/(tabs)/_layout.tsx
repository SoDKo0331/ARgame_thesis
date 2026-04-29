import { Colors, Fonts, Radius } from '@/constants/theme';
import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';
import { Ionicons } from '@expo/vector-icons';
import { Redirect, Tabs } from 'expo-router';
import React from 'react';
import { StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

function TabBarIcon({ name, color, focused }: { name: any; color: string; focused: boolean }) {
  return (
    <View style={[styles.iconWrap, focused && styles.iconWrapActive]}>
      <Ionicons name={name} size={22} color={focused ? Colors.ink : color} />
    </View>
  );
}

export default function TabLayout() {
  const { user } = useApp();
  const { t } = useI18n();
  const insets = useSafeAreaInsets();

  if (!user) {
    return <Redirect href="/login" />;
  }

  const bottomPosition = Math.max(insets.bottom, 16) + 8;

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarStyle: {
          position: 'absolute',
          left: 20,
          right: 20,
          bottom: bottomPosition,
          backgroundColor: Colors.paper,
          borderWidth: 4,
          borderColor: Colors.ink,
          borderRadius: Radius.xl,
          height: 84,
          shadowColor: Colors.ink,
          shadowOffset: { width: 6, height: 6 },
          shadowOpacity: 0.3,
          shadowRadius: 0,
          elevation: 10,
          paddingBottom: 22,
          paddingTop: 12,
        },
        tabBarActiveTintColor: Colors.ink,
        tabBarInactiveTintColor: Colors.ink,
        tabBarLabelStyle: {
          fontSize: 13,
          fontFamily: Fonts.display,
          marginTop: -2,
          marginBottom: 2,
        },
      }}>
      <Tabs.Screen
        name="index"
        options={{
          title: t('tabs.map'),
          tabBarIcon: ({ color, focused }) => (
            <TabBarIcon name={focused ? 'map' : 'map-outline'} color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="explore"
        options={{
          title: t('tabs.explore'),
          tabBarIcon: ({ color, focused }) => (
            <TabBarIcon name={focused ? 'sparkles' : 'sparkles-outline'} color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: t('tabs.profile'),
          tabBarIcon: ({ color, focused }) => (
            <TabBarIcon name={focused ? 'person' : 'person-outline'} color={color} focused={focused} />
          ),
        }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  iconWrap: {
    width: 60,
    height: 42,
    borderRadius: Radius.md,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: 'transparent',
  },
  iconWrapActive: {
    backgroundColor: Colors.blue,
    borderColor: Colors.ink,
    borderWidth: 3,
    transform: [{ rotate: '-4deg' }, { scale: 1.1 }],
    shadowColor: Colors.ink,
    shadowOffset: { width: 4, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 0,
  },
});
