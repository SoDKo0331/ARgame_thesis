# Nomad Adventure VR / AR 🛶

A premium React Native + Unity adventure app built for exploring Mongolia's heritage through interactive 3D and AR experiences.

## 🚀 Quick Start

1. **Install Dependencies**
   ```bash
   npm install
   ```

2. **Run on iOS Simulator (Mock Mode)**
   This project includes a special "Mock Unity" mode that allows you to develop the UI and navigation on the iOS Simulator without needing a physical device.
   ```bash
   MOCK_UNITY=true npm run ios
   ```

3. **Run on Physical iOS Device (Production AR)**
   To experience the full AR features, connect a physical iPhone and run:
   ```bash
   npm run ios:device
   ```

## 🛠 Project Configuration

### Environment Variables (`.env`)
Create a `.env` file in the root directory (one has been provided):
- `EXPO_PUBLIC_API_URL`: The URL of your backend service.
- `RNMAPBOX_MAPS_DOWNLOAD_TOKEN`: Your secret Mapbox download token (moved from `app.json` for security).

### Unity Integration
The app uses a custom Expo Config Plugin (`withUnity.js`) to bridge native Unity frameworks. If you are updating the Unity part:
1. Export the Unity project as an **iOS/Android Framework**.
2. Place the build artifacts in the `unity/builds` directory.
3. Run `npx expo prebuild` to regenerate the native folders.

## ✨ Features

- **Dynamic Map**: Custom Mapbox styling with interactive tourism spots.
- **Quest Ledger**: A brutalist game UI to track collected rewards.
- **Profile HUD**: Centrally managed account settings and explorer stats.
- **Hybrid Platform**: Seamlessly switches between React Native UI and Unity 3D view.

## 🇲🇳 Localization
Supports **English (EN)** and **Mongolian (MN)**. Language can be toggled instantly from the **Profile** tab.

---
Built with Expo, React Native, and Unity.
