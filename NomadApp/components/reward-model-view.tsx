import React, { useRef } from 'react';
import { View, StyleSheet } from 'react-native';
import { Canvas, useFrame } from '@react-three/fiber/native';
import * as THREE from 'three';

function RewardModel({ color = '#00ADB5' }: { color?: string }) {
  const meshRef = useRef<THREE.Mesh>(null!);

  useFrame((state, delta) => {
    if (meshRef.current) {
      // Manual floating and rotation
      meshRef.current.rotation.y += delta * 0.5;
      meshRef.current.rotation.z += delta * 0.2;
      meshRef.current.position.y = Math.sin(state.clock.elapsedTime) * 0.2;
    }
  });

  return (
    <mesh ref={meshRef}>
      <torusKnotGeometry args={[1, 0.3, 128, 32]} />
      <meshStandardMaterial
        color={color}
        metalness={0.7}
        roughness={0.3}
      />
    </mesh>
  );
}

export function RewardModelView({ rarityColor }: { rarityColor: string }) {
  return (
    <View style={styles.container}>
      <Canvas style={styles.canvas}>
        <ambientLight intensity={0.6} />
        <pointLight position={[5, 5, 5]} intensity={1.5} />
        <pointLight position={[-5, -5, -5]} intensity={0.5} color={rarityColor} />
        
        <RewardModel color={rarityColor} />
      </Canvas>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: 200,
    height: 200,
    justifyContent: 'center',
    alignItems: 'center',
  },
  canvas: {
    width: 200,
    height: 200,
    backgroundColor: 'transparent',
  },
});
