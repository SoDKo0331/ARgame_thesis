declare module '@azesmway/react-native-unity' {
  import { Component } from 'react';
  import { ViewStyle } from 'react-native';

  export interface UnityViewProps {
    style?: ViewStyle;
    onUnityMessage?: (result: any) => void;
  }

  export default class UnityView extends Component<UnityViewProps> {
    postMessage(gameObject: string, methodName: string, message: string): void;
    unloadUnity(): void;
    pauseUnity(pause: boolean): void;
    resumeUnity(): void;
    windowFocusChanged(hasFocus: boolean): void;
  }
}
