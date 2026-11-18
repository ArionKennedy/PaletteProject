import '@testing-library/react-native/extend-expect';

// SafeAreaView has no native implementation in Jest — replace with a plain View passthrough
jest.mock('react-native-safe-area-context', () => {
  const { View } = require('react-native');
  return {
    SafeAreaView: ({ children }: { children: React.ReactNode }) => children,
    useSafeAreaInsets: () => ({ top: 0, right: 0, bottom: 0, left: 0 }),
  };
});

// expo-image has no native renderer in Jest — replace with a plain string tag
jest.mock('expo-image', () => ({
  Image: 'Image',
}));
