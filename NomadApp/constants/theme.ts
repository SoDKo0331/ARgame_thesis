import { Dimensions, Platform } from 'react-native';

const { width: SCREEN_WIDTH, height: SCREEN_HEIGHT } = Dimensions.get('window');

export const Colors = {
  ink: '#303841',
  surge: '#00ADB5',
  paper: '#EEEEEE',
  ember: '#FF5722',

  // Compatibility aliases used across the app
  cyan: '#00ADB5',
  cyanDim: 'rgba(0, 173, 181, 0.18)',
  green: '#00ADB5',
  greenDim: 'rgba(0, 173, 181, 0.16)',
  blue: '#EEEEEE',
  blueDim: 'rgba(238, 238, 238, 0.18)',
  red: '#FF5722',
  redDim: 'rgba(255, 87, 34, 0.18)',
  yellow: '#EEEEEE',
  yellowDim: 'rgba(238, 238, 238, 0.18)',
  gold: '#FF5722',

  bg: '#303841',
  bgCard: '#EEEEEE',
  bgGlass: 'rgba(238, 238, 238, 0.96)',
  bgGlassDark: 'rgba(48, 56, 65, 0.9)',
  bgOverlay: 'rgba(48, 56, 65, 0.52)',

  textPrimary: '#EEEEEE',
  textSecondary: 'rgba(238, 238, 238, 0.76)',
  textMuted: 'rgba(238, 238, 238, 0.48)',
  textDark: '#303841',

  border: 'rgba(238, 238, 238, 0.24)',
  borderActive: '#00ADB5',

  light: {
    text: '#303841',
    background: '#EEEEEE',
    tint: '#00ADB5',
    icon: '#303841',
    tabIconDefault: '#5E6670',
    tabIconSelected: '#00ADB5',
  },
  dark: {
    text: '#EEEEEE',
    background: '#303841',
    tint: '#00ADB5',
    icon: '#C4C8CD',
    tabIconDefault: '#C4C8CD',
    tabIconSelected: '#00ADB5',
  },
};

export const Gradients = {
  cyan: ['#00ADB5', '#47CCD2'] as const,
  green: ['#00ADB5', '#2FD3C1'] as const,
  red: ['#FF5722', '#FF8A5B'] as const,
  dark: ['rgba(48, 56, 65, 0.98)', 'rgba(48, 56, 65, 0.9)'] as const,
  overlay: ['rgba(48, 56, 65, 0.06)', 'rgba(48, 56, 65, 0.28)', 'rgba(48, 56, 65, 0.86)'] as const,
};

export const Spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  base: 16,
  lg: 20,
  xl: 24,
  xxl: 32,
  xxxl: 40,
};

export const Radius = {
  sm: 8,
  md: 14,
  lg: 20,
  xl: 28,
  full: 999,
};

export const Fonts = Platform.select({
  ios: {
    display: 'Caveat_700Bold',
    accent: 'Caveat_600SemiBold',
    sans: 'Avenir Next',
    mono: 'Menlo',
  },
  android: {
    display: 'Caveat_700Bold',
    accent: 'Caveat_600SemiBold',
    sans: 'sans-serif-medium',
    mono: 'monospace',
  },
  web: {
    display: '"Caveat_700Bold", "Caveat", cursive',
    accent: '"Caveat_600SemiBold", "Caveat", cursive',
    sans: '"Trebuchet MS", "Avenir Next", system-ui, sans-serif',
    mono: '"IBM Plex Mono", "SFMono-Regular", monospace',
  },
  default: {
    display: 'Caveat_700Bold',
    accent: 'Caveat_600SemiBold',
    sans: 'sans-serif',
    mono: 'monospace',
  },
});

export const Typography = {
  displayLg: {
    fontSize: 34,
    lineHeight: 36,
    fontFamily: Fonts.display,
  },
  displayMd: {
    fontSize: 24,
    lineHeight: 26,
    fontFamily: Fonts.display,
  },
  title: {
    fontSize: 19,
    lineHeight: 22,
    fontFamily: Fonts.accent,
  },
  heading: {
    fontSize: 16,
    lineHeight: 20,
    fontFamily: Fonts.sans,
    fontWeight: '800' as const,
  },
  body: {
    fontSize: 14,
    lineHeight: 19,
    fontFamily: Fonts.sans,
    fontWeight: '600' as const,
  },
  label: {
    fontSize: 11,
    lineHeight: 14,
    fontFamily: Fonts.sans,
    fontWeight: '800' as const,
    letterSpacing: 0.6,
  },
  caption: {
    fontSize: 10,
    lineHeight: 13,
    fontFamily: Fonts.sans,
    fontWeight: '700' as const,
    letterSpacing: 0.4,
  },
};

export const Layout = {
  screen: { width: SCREEN_WIDTH, height: SCREEN_HEIGHT },
  tabBarHeight: 72,
  paddingH: Spacing.lg,
};

export const Shadows = {
  glow: (color: string) => ({
    shadowColor: color,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.36,
    shadowRadius: 18,
    elevation: 8,
  }),
  card: {
    shadowColor: Colors.ink,
    shadowOffset: { width: 6, height: 6 },
    shadowOpacity: 0.28,
    shadowRadius: 0,
    elevation: 8,
  },
  punch: {
    shadowColor: Colors.ink,
    shadowOffset: { width: 4, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 0,
    elevation: 6,
  },
};
