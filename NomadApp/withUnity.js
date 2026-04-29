// withUnity.js - Custom Expo Config Plugin for Unity Library integration
const { withSettingsGradle, withProjectBuildGradle, withAppBuildGradle, withGradleProperties, withStringsXml, withDangerousMod } = require('@expo/config-plugins');
const path = require('path');
const fs = require('fs');

// 1. Settings Gradle: include ':unityLibrary'
const withUnitySettingsGradle = (config) => {
  return withSettingsGradle(config, (mod) => {
    const contents = mod.modResults.contents;
    const unityIncludes = `
include ':unityLibrary'
project(':unityLibrary').projectDir = new File("$rootDir/../unity/builds/android/unityLibrary")
`;
    if (!contents.includes("':unityLibrary'")) {
      mod.modResults.contents = contents + unityIncludes;
    }
    return mod;
  });
};

// 2. Root Build Gradle: add flatDir for unity libs
const withUnityProjectBuildGradle = (config) => {
  return withProjectBuildGradle(config, (mod) => {
    const contents = mod.modResults.contents;
    const flatDirBlock = `
allprojects {
    repositories {
        flatDir {
            dirs "\${project(':unityLibrary').projectDir}/libs"
        }
    }
}
`;
    if (!contents.includes('unityLibrary') && !contents.includes('flatDir')) {
      mod.modResults.contents = contents + flatDirBlock;
    }
    return mod;
  });
};

// 3. App Build Gradle: add unity implementation dependencies
const withUnityAppBuildGradle = (config) => {
  return withAppBuildGradle(config, (mod) => {
    const contents = mod.modResults.contents;
    const deps = `    implementation project(':unityLibrary')
    implementation files("\${project(':unityLibrary').projectDir}/libs/unity-classes.jar")
`;
    if (!contents.includes("project(':unityLibrary')")) {
      mod.modResults.contents = contents.replace(
        /dependencies\s*\{/,
        `dependencies {\n${deps}`
      );
    }
    return mod;
  });
};

// 4. Gradle Properties: add unityStreamingAssets
const withUnityGradleProperties = (config) => {
  return withGradleProperties(config, (mod) => {
    const hasKey = mod.modResults.some((item) => item.key === 'unityStreamingAssets');
    if (!hasKey) {
      mod.modResults.push({ type: 'property', key: 'unityStreamingAssets', value: '.unity3d' });
    }
    return mod;
  });
};

// 5. Strings XML: add game_view_content_description
const withUnityStringsXml = (config) => {
  return withStringsXml(config, (mod) => {
    const strings = mod.modResults.resources.string || [];
    const hasKey = strings.some((s) => s.$.name === 'game_view_content_description');
    if (!hasKey) {
      strings.push({ $: { name: 'game_view_content_description' }, _: 'Game view' });
      mod.modResults.resources.string = strings;
    }
    return mod;
  });
};

// 6. iOS Podfile: inject search paths and react-native-unity install script
const withUnityPodfile = (config) => {
  return withDangerousMod(config, [
    'ios',
    async (mod) => {
      const podfilePath = path.join(mod.modRequest.platformProjectRoot, 'Podfile');
      if (fs.existsSync(podfilePath)) {
        let podfileContents = fs.readFileSync(podfilePath, 'utf-8');
        
        // 1. Inject Search Paths into react-native-unity target
        const searchPathScript = `
    installer.pods_project.targets.each do |target|
      if target.name == 'react-native-unity'
        target.build_configurations.each do |config|
          config.build_settings['FRAMEWORK_SEARCH_PATHS'] ||= ['$(inherited)']
          config.build_settings['FRAMEWORK_SEARCH_PATHS'] << '"$(PODS_TARGET_SRCROOT)/ios"'
          config.build_settings['HEADER_SEARCH_PATHS'] ||= ['$(inherited)']
          config.build_settings['HEADER_SEARCH_PATHS'] << '"$(PODS_TARGET_SRCROOT)/ios/UnityFramework.framework/Headers"'
        end
      end
    end`;

        if (!podfileContents.includes("target.name == 'react-native-unity'")) {
          podfileContents = podfileContents.replace(
            /post_install do \|installer\|/,
            `post_install do |installer|\n${searchPathScript}`
          );
        }

        // 2. Add install script if missing (keeping existing logic but making it safer)
        const installScript = `system("node ../node_modules/@azesmway/react-native-unity/scripts/install.js")`;
        if (fs.existsSync(path.join(mod.modRequest.projectRoot, 'node_modules/@azesmway/react-native-unity/scripts/install.js'))) {
          if (!podfileContents.includes(installScript)) {
            podfileContents = podfileContents.replace(
              'prepare_react_native_project!',
              `prepare_react_native_project!\n\n${installScript}`
            );
          }
        }

        fs.writeFileSync(podfilePath, podfileContents);
      }
      return mod;
    },
  ]);
};

// Compose all plugins
const withUnity = (config) => {
  if (process.env.MOCK_UNITY === 'true') {
    console.log('⚠️ MOCK_UNITY=true detected. Skipping Unity native integration to support simulator build.');
    return config;
  }
  
  config = withUnitySettingsGradle(config);
  config = withUnityProjectBuildGradle(config);
  config = withUnityAppBuildGradle(config);
  config = withUnityGradleProperties(config);
  config = withUnityStringsXml(config);
  config = withUnityPodfile(config);
  return config;
};

module.exports = withUnity;
