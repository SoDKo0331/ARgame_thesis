const { spawnSync } = require('node:child_process');

const userArgs = process.argv.slice(2);
const wantsHelp = userArgs.includes('--help') || userArgs.includes('-h');
const requestedSimulator = userArgs.some(
  (arg) => arg === '--simulator' || arg === '-s' || arg.startsWith('--simulator=')
);
const requestedDevice = userArgs.some(
  (arg) => arg === '--device' || arg.startsWith('--device=')
);

const getConnectedIOSDevices = () => {
  const result = spawnSync('xcrun', ['xcdevice', 'list'], {
    encoding: 'utf8',
  });

  if (result.error || result.status !== 0 || !result.stdout) {
    return null;
  }

  try {
    const devices = JSON.parse(result.stdout);
    return devices.filter(
      (device) =>
        device &&
        device.available === true &&
        device.simulator === false &&
        device.platform === 'com.apple.platform.iphoneos'
    );
  } catch {
    return null;
  }
};

if (requestedSimulator) {
  console.log('🏗️  Simulator requested. Enabling Unity Mock mode to allow the build to proceed.');
  process.env.MOCK_UNITY = 'true';
}

const expoArgs = ['expo', 'run:ios'];

if (!wantsHelp && !requestedDevice && !requestedSimulator) {
  // Default to device if nothing specified
  const connectedDevices = getConnectedIOSDevices();
  if (connectedDevices && connectedDevices.length > 0) {
    expoArgs.push('--device');
  } else {
    // If no device is connected, check if we should fallback to simulator or show error
    const result = spawnSync('xcrun', ['xcdevice', 'list'], { encoding: 'utf8' });
    let additionalInfo = '';
    if (result.status === 0 && result.stdout) {
      try {
        const allDevices = JSON.parse(result.stdout);
        const unavailable = allDevices.filter(d => d && d.platform === 'com.apple.platform.iphoneos' && !d.simulator && !d.available);
        if (unavailable.length > 0) {
          additionalInfo = `\n\nDetected ${unavailable.length} device(s) that are not currently available:\n` + 
            unavailable.map(d => `- ${d.name || 'Unknown'} (${d.modelName}): ${d.error?.description || 'Locked or not trusted'}`).join('\n') +
            '\n\nTip: Ensure the device is unlocked, trusted, and has Developer Mode enabled.';
        }
      } catch (e) {}
    }

    console.warn('⚠️ No physical device found. Falling back to Simulator with Unity mocked.');
    console.warn('Physical device info:' + additionalInfo);
    console.log('\nUse `yarn ios --device` to force device mode (requires connected phone).');
    process.env.MOCK_UNITY = 'true';
    userArgs.push('--simulator');
  }
}

if (!wantsHelp && requestedDevice) {
  const connectedDevices = getConnectedIOSDevices();

  if (!connectedDevices || connectedDevices.length === 0) {
    // Check if there are ANY devices that are just NOT available right now
    const result = spawnSync('xcrun', ['xcdevice', 'list'], { encoding: 'utf8' });
    let additionalInfo = '';
    if (result.status === 0 && result.stdout) {
      try {
        const allDevices = JSON.parse(result.stdout);
        const unavailable = allDevices.filter(d => d && d.platform === 'com.apple.platform.iphoneos' && !d.simulator && !d.available);
        if (unavailable.length > 0) {
          additionalInfo = `\n\nDetected ${unavailable.length} device(s) that are not currently available:\n` + 
            unavailable.map(d => `- ${d.name || 'Unknown'} (${d.modelName}): ${d.error?.description || 'Locked or not trusted'}`).join('\n') +
            '\n\nTip: Ensure the device is unlocked, trusted, and has Developer Mode enabled.';
        }
      } catch (e) {}
    }

    console.error('No physical iPhone or iPad is currently available to Xcode.');
    console.error('Connect a device, trust this computer if prompted, then run `yarn ios` again.' + additionalInfo);
    process.exit(1);
  }
}

expoArgs.push(...userArgs);

const result = spawnSync('npx', expoArgs, {
  stdio: 'inherit',
  env: { ...process.env, MOCK_UNITY: process.env.MOCK_UNITY }
});

if (result.error) {
  console.error(result.error.message);
  process.exit(1);
}

process.exit(result.status ?? 1);
