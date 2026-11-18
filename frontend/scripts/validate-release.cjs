// Validate names and formats only. Never log environment variable values.
const { expo } = require('../app.json');

function fail(message) {
  console.error(message);
  process.exit(1);
}

const projectId = expo.extra?.eas?.projectId;
if (!projectId || !/^[\da-f]{8}-(?:[\da-f]{4}-){3}[\da-f]{12}$/i.test(projectId)) {
  fail('Link the EAS project with eas init before building or publishing.');
}
if (expo.updates?.url !== `https://u.expo.dev/${projectId}`) {
  fail('Run eas update:configure: updates.url must match extra.eas.projectId.');
}

let apiUrl;
try {
  apiUrl = new URL(process.env.EXPO_PUBLIC_API_URL);
} catch {
  fail('EXPO_PUBLIC_API_URL must be a public HTTPS backend URL.');
}
// Release builds must never depend on a workstation, emulator, or private LAN.
const host = apiUrl.hostname.toLowerCase();
if (
  apiUrl.protocol !== 'https:' || apiUrl.username || apiUrl.password ||
  apiUrl.search || apiUrl.hash || apiUrl.pathname !== '/' ||
  host === 'localhost' || host.endsWith('.localhost') || host.endsWith('.local') ||
  !host.includes('.') || host.includes(':') || /^[\d.]+$/.test(host)
) {
  fail('EXPO_PUBLIC_API_URL must be a public HTTPS origin without credentials, query, or path.');
}
if (process.env.EXPO_PUBLIC_API_URL.endsWith('/')) {
  fail('Remove the trailing slash from EXPO_PUBLIC_API_URL (API paths already start with /).');
}
if (!/^pk_(test|live)_[A-Za-z0-9+/=]+$/.test(process.env.EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY ?? '')) {
  fail('Set EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY to a Clerk publishable key in EAS production.');
}
console.log('Release configuration validated. No environment values were printed.');
