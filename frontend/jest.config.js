module.exports = {
  preset: 'jest-expo',
  setupFilesAfterEnv: ['./jest.setup.ts'],
  clearMocks: true,
  moduleNameMapper: {
    '\\.css$': '<rootDir>/src/__mocks__/fileMock.js',
    '^react$': '<rootDir>/node_modules/react',
    '^react/(.*)$': '<rootDir>/node_modules/react/$1',
    '^react-native-css-interop/jsx-runtime$': '<rootDir>/src/__mocks__/react-native-css-interop-jsx-runtime.js',
    '^react-native-css-interop$': '<rootDir>/src/__mocks__/react-native-css-interop.js',
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  transformIgnorePatterns: [
    'node_modules/(?!(' +
      '(jest-)?react-native' +
      '|@react-native(-community)?' +
      '|expo(nent)?' +
      '|@expo(nent)?/.*' +
      '|@expo-google-fonts/.*' +
      '|react-navigation' +
      '|@react-navigation/.*' +
      '|@unimodules/.*' +
      '|unimodules' +
      '|nativewind' +
      '|lucide-react-native' +
      '|@rn-primitives/.*' +
      '|tailwind-merge' +
      '|class-variance-authority' +
      '|react-native-svg' +
    '))',
  ],
};
