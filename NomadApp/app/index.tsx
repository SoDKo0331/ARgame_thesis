import React from 'react';
import { Redirect } from 'expo-router';

import { useApp } from '@/context/AppContext';

export default function IndexScreen() {
  const { user } = useApp();

  return <Redirect href={user ? '/(tabs)' : '/login'} />;
}
