import { Stack, usePathname, useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useFonts } from 'expo-font';
import { Caveat_600SemiBold, Caveat_700Bold } from '@expo-google-fonts/caveat';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { View, StyleSheet } from 'react-native';
import React, { useState, useEffect } from 'react';
import * as SplashScreen from 'expo-splash-screen';
import { Asset } from 'expo-asset';
import 'react-native-reanimated';

import { AppLoader } from '@/components/app-loader';
import { AppProvider, useApp } from '@/context/AppContext';
import { I18nProvider, useI18n } from '@/context/I18nContext';
import { Colors } from '@/constants/theme';

// Prevent native splash screen from auto-hiding
SplashScreen.preventAutoHideAsync();

// ─── Inner layout that has access to AppContext ────────────────────────────────
function AppNavigation() {
  const { user } = useApp();
  const { t } = useI18n();
  const router = useRouter();
  const pathname = usePathname();
  const [isAppReady, setAppReady] = useState(false);
  const [fontsLoaded, fontError] = useFonts({
    Caveat_600SemiBold,
    Caveat_700Bold,
  });

  useEffect(() => {
    async function prepare() {
      try {
        await Asset.loadAsync([require('@/assets/images/logo.png')]);
      } catch (e) {
        console.warn('[Layout] Preparation error:', e);
      } finally {
        setAppReady(true);
      }
    }
    prepare();
  }, []);

  useEffect(() => {
    if (fontsLoaded || fontError) {
      SplashScreen.hideAsync().catch(() => {
        /* ignore */
      });
    }
  }, [fontsLoaded, fontError]);

  useEffect(() => {
    if (!isAppReady || (!fontsLoaded && !fontError)) {
      return;
    }

    if (!user && pathname !== '/login') {
      router.replace('/login');
      return;
    }

    if (user && (pathname === '/' || pathname === '/login')) {
      router.replace('/(tabs)');
    }
  }, [fontError, fontsLoaded, isAppReady, pathname, router, user]);

  if (!isAppReady || (!fontsLoaded && !fontError)) {
    return (
      <AppLoader
        title={t('loader.bootTitle')}
        message={t('loader.bootBody')}
        statusLabel={t('loader.statusLabel')}
      />
    );
  }

  return (
    <View style={styles.container}>
      <Stack>
        <Stack.Screen
          name="index"
          options={{ headerShown: false }}
        />
        <Stack.Screen
          name="login"
          options={{ headerShown: false, animation: 'fade' }}
        />
        <Stack.Screen
          name="(tabs)"
          options={{ headerShown: false }}
        />
        <Stack.Screen
          name="unity-ar"
          options={{
            headerShown: false,
            animation: 'fade',
            gestureEnabled: false,
          }}
        />
        <Stack.Screen
          name="modal"
          options={{ presentation: 'modal', headerShown: false }}
        />
      </Stack>
      <StatusBar style="light" translucent />
    </View>
  );
}

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <I18nProvider>
        <AppProvider>
          <AppNavigation />
        </AppProvider>
      </I18nProvider>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.bg,
  },
});
