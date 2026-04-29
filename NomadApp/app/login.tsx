import React, { useEffect, useState } from 'react';
import {
  Alert,
  Image,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { Redirect } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { AppLoader } from '@/components/app-loader';
import { Colors, Gradients, Radius, Shadows, Spacing, Typography } from '@/constants/theme';
import { useApp } from '@/context/AppContext';
import { useI18n } from '@/context/I18nContext';

export default function LoginScreen() {
  const insets = useSafeAreaInsets();
  const {
    user,
    isSigningIn,
    authError,
    signInAsGuest,
    signInWithDemoCredentials,
    clearAuthError,
    getSuggestedGuestDisplayName,
  } = useApp();
  const { language, toggleLanguage, t } = useI18n();
  const [displayName, setDisplayName] = useState('');
  const [demoEmail, setDemoEmail] = useState(__DEV__ ? 'ssodko245@gmail.com' : '');
  const [demoPassword, setDemoPassword] = useState(__DEV__ ? '4123' : '');

  useEffect(() => {
    setDisplayName((current) => current || getSuggestedGuestDisplayName());
  }, [getSuggestedGuestDisplayName]);

  if (user) {
    return <Redirect href="/(tabs)" />;
  }

  if (isSigningIn) {
    return (
      <AppLoader
        title={t('loader.loginTitle')}
        message={t('loader.loginBody')}
        statusLabel={t('loader.statusLabel')}
      />
    );
  }

  const handleSubmit = async () => {
    const trimmedName = displayName.trim();

    if (!trimmedName) {
      Alert.alert(t('auth.errorTitle'), t('auth.errorNameRequired'));
      return;
    }

    try {
      await signInAsGuest(trimmedName);
    } catch {
      // Error state is surfaced from context.
    }
  };

  const handleDemoSubmit = async () => {
    const trimmedEmail = demoEmail.trim();
    const trimmedPassword = demoPassword.trim();

    if (!trimmedEmail || !trimmedPassword) {
      Alert.alert(t('auth.errorTitle'), t('auth.demoErrorRequired'));
      return;
    }

    try {
      await signInWithDemoCredentials(trimmedEmail, trimmedPassword);
    } catch {
      // Error state is surfaced from context.
    }
  };

  return (
    <LinearGradient colors={Gradients.dark} style={styles.container}>
      <View pointerEvents="none" style={styles.decorLayer}>
        <View style={[styles.topStripe, { top: insets.top + 80 }]} />
        <View style={[styles.bottomStripe, { bottom: insets.bottom + 120 }]} />
      </View>

      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.keyboardShell}>
        <ScrollView
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
          contentContainerStyle={[
            styles.scrollContent,
            { paddingBottom: Math.max(insets.bottom, Spacing.lg) + Spacing.lg },
          ]}>
          <View style={[styles.topRow, { paddingTop: Math.max(insets.top, Spacing.lg) }]}>
            <View style={[styles.badge, Shadows.punch]}>
              <Text style={styles.badgeText}>{t('auth.badge')}</Text>
            </View>

            <TouchableOpacity
              style={[styles.languageToggle, Shadows.punch]}
              activeOpacity={0.86}
              onPress={toggleLanguage}>
              <Text style={styles.languageToggleText}>{language === 'en' ? 'MN' : 'EN'}</Text>
            </TouchableOpacity>
          </View>

          <View style={styles.content}>
            <View style={[styles.heroCard, Shadows.card]}>
              <View style={styles.logoRing}>
                <Image source={require('@/assets/images/logo.png')} style={styles.logo} resizeMode="contain" />
              </View>

              <Text style={styles.title}>{t('auth.title')}</Text>
              <Text style={styles.subtitle}>{t('auth.subtitle')}</Text>

              <View style={styles.inputWrap}>
                <Text style={styles.inputLabel}>{t('auth.nameLabel')}</Text>
                <TextInput
                  value={displayName}
                  onChangeText={(value) => {
                    if (authError) {
                      clearAuthError();
                    }

                    setDisplayName(value);
                  }}
                  autoCapitalize="words"
                  autoCorrect={false}
                  placeholder={t('auth.namePlaceholder')}
                  placeholderTextColor="rgba(48, 56, 65, 0.42)"
                  style={styles.input}
                  returnKeyType="go"
                  onSubmitEditing={handleSubmit}
                />
              </View>

              {authError ? (
                <View style={[styles.errorBanner, Shadows.punch]}>
                  <Ionicons name="alert-circle" size={18} color={Colors.paper} />
                  <View style={styles.errorCopy}>
                    <Text style={styles.errorTitle}>{t('auth.errorTitle')}</Text>
                    <Text style={styles.errorText}>{authError}</Text>
                  </View>
                </View>
              ) : null}

              <TouchableOpacity
                style={[styles.primaryButton, Shadows.punch]}
                activeOpacity={0.9}
                onPress={handleSubmit}>
                <Ionicons name="log-in-outline" size={20} color={Colors.paper} />
                <Text style={styles.primaryButtonText}>{t('auth.continueGuest')}</Text>
              </TouchableOpacity>

              <Text style={styles.verificationNote}>{t('auth.verificationNote')}</Text>

              {__DEV__ ? (
                <View style={styles.demoSection}>
                  <View style={styles.demoDivider}>
                    <View style={styles.demoDividerLine} />
                    <Text style={styles.demoDividerText}>{t('auth.demoTitle')}</Text>
                    <View style={styles.demoDividerLine} />
                  </View>

                  <Text style={styles.demoNote}>{t('auth.demoNote')}</Text>

                  <View style={styles.demoInputStack}>
                    <View>
                      <Text style={styles.inputLabel}>{t('auth.demoEmailLabel')}</Text>
                      <TextInput
                        value={demoEmail}
                        onChangeText={(value) => {
                          if (authError) {
                            clearAuthError();
                          }

                          setDemoEmail(value);
                        }}
                        autoCapitalize="none"
                        autoCorrect={false}
                        keyboardType="email-address"
                        placeholder={t('auth.demoEmailPlaceholder')}
                        placeholderTextColor="rgba(48, 56, 65, 0.42)"
                        style={styles.input}
                        returnKeyType="next"
                      />
                    </View>

                    <View>
                      <Text style={styles.inputLabel}>{t('auth.demoPasswordLabel')}</Text>
                      <TextInput
                        value={demoPassword}
                        onChangeText={(value) => {
                          if (authError) {
                            clearAuthError();
                          }

                          setDemoPassword(value);
                        }}
                        autoCapitalize="none"
                        autoCorrect={false}
                        secureTextEntry
                        textContentType="password"
                        placeholder={t('auth.demoPasswordPlaceholder')}
                        placeholderTextColor="rgba(48, 56, 65, 0.42)"
                        style={styles.input}
                        returnKeyType="go"
                        onSubmitEditing={handleDemoSubmit}
                      />
                    </View>
                  </View>

                  <TouchableOpacity
                    style={[styles.secondaryButton, Shadows.punch]}
                    activeOpacity={0.9}
                    onPress={handleDemoSubmit}>
                    <Ionicons name="flask-outline" size={20} color={Colors.paper} />
                    <Text style={styles.secondaryButtonText}>{t('auth.demoContinue')}</Text>
                  </TouchableOpacity>
                </View>
              ) : null}
            </View>

            <View style={[styles.helperCard, Shadows.card]}>
              <Text style={styles.helperTitle}>{t('auth.helperTitle')}</Text>

              <View style={styles.helperRow}>
                <View style={[styles.helperIconWrap, { backgroundColor: Colors.surge }]}>
                  <Ionicons name="walk-outline" size={18} color={Colors.paper} />
                </View>
                <Text style={styles.helperText}>{t('auth.helperBody')}</Text>
              </View>

              <View style={styles.helperFootnote}>
                <Ionicons name="mail-unread-outline" size={16} color={Colors.ember} />
                <Text style={styles.helperFootnoteText}>{t('auth.helperFootnote')}</Text>
              </View>

              <Text style={styles.retryHint}>{t('auth.retryHint')}</Text>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.bg,
  },
  keyboardShell: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
  },
  decorLayer: {
    ...StyleSheet.absoluteFillObject,
  },
  topStripe: {
    position: 'absolute',
    top: 120,
    right: -28,
    width: 170,
    height: 56,
    borderRadius: Radius.md,
    backgroundColor: Colors.surge,
    transform: [{ rotate: '9deg' }],
  },
  bottomStripe: {
    position: 'absolute',
    left: -18,
    bottom: 148,
    width: 148,
    height: 44,
    borderRadius: Radius.md,
    backgroundColor: Colors.ember,
    transform: [{ rotate: '-8deg' }],
  },
  topRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
  },
  badge: {
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.full,
    backgroundColor: Colors.paper,
    paddingHorizontal: Spacing.base,
    paddingVertical: Spacing.sm,
    transform: [{ rotate: '-3deg' }],
  },
  badgeText: {
    ...Typography.caption,
    color: Colors.ink,
  },
  languageToggle: {
    width: 58,
    height: 52,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.md,
    backgroundColor: Colors.ember,
    alignItems: 'center',
    justifyContent: 'center',
  },
  languageToggleText: {
    ...Typography.heading,
    color: Colors.paper,
  },
  content: {
    paddingHorizontal: Spacing.lg,
    paddingTop: Spacing.base,
    gap: Spacing.lg,
    justifyContent: 'center',
    flexGrow: 1,
  },
  heroCard: {
    borderWidth: 4,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    backgroundColor: Colors.paper,
    paddingHorizontal: Spacing.xl,
    paddingTop: Spacing.xxxl,
    paddingBottom: Spacing.xl,
  },
  logoRing: {
    alignSelf: 'center',
    width: 100,
    height: 100,
    borderRadius: Radius.full,
    borderWidth: 4,
    borderColor: Colors.ink,
    backgroundColor: Colors.paper,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: Spacing.base,
    transform: [{ rotate: '-4deg' }],
    ...Shadows.punch,
  },
  logo: {
    width: 56,
    height: 56,
    borderRadius: Radius.md,
  },
  title: {
    ...Typography.displayLg,
    color: Colors.ink,
    textAlign: 'center',
  },
  subtitle: {
    ...Typography.body,
    color: 'rgba(48, 56, 65, 0.78)',
    textAlign: 'center',
    marginTop: Spacing.sm,
  },
  inputWrap: {
    marginTop: Spacing.xl,
  },
  inputLabel: {
    ...Typography.label,
    color: Colors.surge,
    marginBottom: Spacing.sm,
  },
  input: {
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    backgroundColor: '#F8F6F1',
    color: Colors.ink,
    paddingHorizontal: Spacing.base,
    paddingVertical: 15,
    fontSize: 16,
    fontFamily: Typography.body.fontFamily,
    fontWeight: '700',
  },
  errorBanner: {
    marginTop: Spacing.base,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    backgroundColor: Colors.ember,
    padding: Spacing.base,
    flexDirection: 'row',
    gap: Spacing.sm,
    alignItems: 'flex-start',
  },
  errorCopy: {
    flex: 1,
  },
  errorTitle: {
    ...Typography.label,
    color: Colors.paper,
  },
  errorText: {
    ...Typography.body,
    color: Colors.paper,
    marginTop: 2,
  },
  primaryButton: {
    marginTop: Spacing.lg,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    backgroundColor: Colors.surge,
    minHeight: 58,
    paddingHorizontal: Spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.sm,
  },
  primaryButtonText: {
    ...Typography.heading,
    color: Colors.paper,
  },
  verificationNote: {
    ...Typography.caption,
    color: 'rgba(48, 56, 65, 0.64)',
    textAlign: 'center',
    marginTop: Spacing.base,
  },
  demoSection: {
    marginTop: Spacing.lg,
  },
  demoDivider: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  demoDividerLine: {
    flex: 1,
    height: 3,
    borderRadius: Radius.full,
    backgroundColor: 'rgba(48, 56, 65, 0.14)',
  },
  demoDividerText: {
    ...Typography.caption,
    color: Colors.ember,
  },
  demoNote: {
    ...Typography.body,
    color: Colors.ink,
    marginTop: Spacing.base,
  },
  demoInputStack: {
    marginTop: Spacing.base,
    gap: Spacing.base,
  },
  secondaryButton: {
    marginTop: Spacing.base,
    borderWidth: 3,
    borderColor: Colors.ink,
    borderRadius: Radius.lg,
    backgroundColor: Colors.ember,
    minHeight: 58,
    paddingHorizontal: Spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.sm,
  },
  secondaryButtonText: {
    ...Typography.heading,
    color: Colors.paper,
  },
  helperCard: {
    borderWidth: 4,
    borderColor: Colors.ink,
    borderRadius: Radius.xl,
    backgroundColor: Colors.bgGlass,
    padding: Spacing.lg,
    gap: Spacing.base,
  },
  helperTitle: {
    ...Typography.title,
    color: Colors.ink,
  },
  helperRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: Spacing.sm,
  },
  helperIconWrap: {
    width: 38,
    height: 38,
    borderRadius: Radius.md,
    borderWidth: 3,
    borderColor: Colors.ink,
    alignItems: 'center',
    justifyContent: 'center',
  },
  helperText: {
    flex: 1,
    ...Typography.body,
    color: Colors.ink,
  },
  helperFootnote: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  helperFootnoteText: {
    ...Typography.body,
    color: Colors.ink,
  },
  retryHint: {
    ...Typography.caption,
    color: 'rgba(48, 56, 65, 0.64)',
  },
});
