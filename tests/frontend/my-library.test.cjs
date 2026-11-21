const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const ts = require('../../frontend/node_modules/typescript');

function loadModule(file, modules = {}, globals = {}) {
  const source = fs.readFileSync(path.join(__dirname, '../../frontend/src', file), 'utf8');
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
  }).outputText;
  const exports = {};
  vm.runInNewContext(code, {
    exports,
    Error,
    ...globals,
    require(name) {
      if (Object.hasOwn(modules, name)) return modules[name];
      throw new Error('Unexpected import: ' + name);
    },
  });
  return exports;
}

function createRenderer(modules = {}) {
  const slots = [];
  let index, effects, dirty, focusCallback, cleanup, currentSubject;
  const hooks = {
    useState(initial) {
      const i = index++;
      if (!(i in slots)) slots[i] = initial;
      return [slots[i], value => {
        const next = typeof value === 'function' ? value(slots[i]) : value;
        if (!Object.is(next, slots[i])) {
          slots[i] = next;
          dirty = true;
        }
      }];
    },
    useRef(initial) {
      const i = index++;
      return slots[i] ?? (slots[i] = { current: initial });
    },
    useCallback(callback, deps) {
      const i = index++;
      if (!slots[i] || deps.some((value, j) => !Object.is(value, slots[i].deps[j]))) {
        slots[i] = { callback, deps };
      }
      return slots[i].callback;
    },
  };
  const jsx = (type, props) => ({ type, props });
  const imports = {
    react: hooks,
    'react/jsx-runtime': { jsx, jsxs: jsx, Fragment: 'Fragment' },
    'expo-router': {
      Link: 'Link',
      Stack: { Screen: 'Screen' },
      useFocusEffect(callback) {
        const i = index++;
        if (slots[i] === callback) return;
        slots[i] = callback;
        effects.push(() => {
          cleanup?.();
          focusCallback = callback;
          cleanup = focusCallback();
        });
      },
    },
    'react-native': {
      ActivityIndicator: 'ActivityIndicator', FlatList: 'FlatList', Pressable: 'Pressable',
      ScrollView: 'ScrollView', Text: 'Text', TouchableOpacity: 'TouchableOpacity', View: 'View',
      Platform: { OS: 'ios' }, StyleSheet: { create: styles => styles, absoluteFill: {} },
    },
    '@/hooks/use-theme': { useTheme: () => ({}) },
    '@/lib/theme': { Spacing: { two: 8, three: 16, four: 24 }, BottomTabInset: 0 },
    '@/constants/moviesConstants': { POSTER_ASPECT_RATIO: 2 / 3 },
    ...modules,
  };

  return {
    hooks,
    load: file => loadModule(file, imports),
    refocus() {
      cleanup?.();
      cleanup = focusCallback();
    },
    async render(subject) {
      if (currentSubject !== subject) {
        cleanup?.();
        cleanup = undefined;
        focusCallback = undefined;
        slots.length = 0;
        currentSubject = subject;
      }
      for (let attempt = 0; attempt < 12; attempt++) {
        index = 0;
        effects = [];
        dirty = false;
        const result = subject();
        effects.forEach(effect => effect());
        await new Promise(resolve => setImmediate(resolve));
        if (!dirty) return result;
      }
      assert.fail('Component kept rerendering instead of settling');
    },
  };
}

function findNodes(node, predicate) {
  if (Array.isArray(node)) return node.flatMap(child => findNodes(child, predicate));
  if (!node?.props) return [];
  return [...(predicate(node) ? [node] : []), ...findNodes(node.props.children, predicate)];
}

test('Library categories reload on refocus without refetching on unrelated renders', async () => {
  let calls = 0;
  let externalId = '123';
  const renderer = createRenderer({
    '@/components/movie': {},
    '@/components/ui/button': {},
    '@/services/library': {
      async getLibraryPage(type, page) {
        assert.equal(type, 'movie');
        assert.equal(page, 1);
        calls++;
        return { items: [{ externalId, title: externalId }], page: 1, hasMore: false };
      },
    },
  });
  const { LibraryList } = renderer.load('components/library-list.tsx');
  const subject = () => LibraryList({ type: 'movie', title: 'Movies' });

  await renderer.render(subject);
  assert.equal(calls, 1);
  await renderer.render(subject);
  assert.equal(calls, 1);

  externalId = '456';
  renderer.refocus();
  const tree = await renderer.render(subject);
  assert.equal(calls, 2);
  const [list] = findNodes(tree, node => node.type === 'FlatList');
  assert.equal(list.props.data[0].externalId, '456');
  assert.equal(findNodes(list.props.ListHeaderComponent, node => node.props.accessibilityLabel?.includes('Refresh')).length, 0);
});

test('home renders inset category links and separate horizontal cover links, and spins only on manual refresh', async () => {
  let requests = 0;
  const rotations = [];
  const book = { title: 'A book', externalId: 'volume+1', posterUrl: 'https://covers.example.test/book.jpg' };
  const renderer = createRenderer({
    '@clerk/expo': { useAuth: () => ({ sessionId: 'test-session' }) },
    'react-native-safe-area-context': { SafeAreaView: 'SafeAreaView' },
    'expo-image': { Image: 'Image' },
    'expo-symbols': { SymbolView: 'SymbolView' },
    'lucide-react-native': { ChevronRight: 'ChevronRight', RefreshCw: 'RefreshCw' },
    'react-native-reanimated': {
      default: { View: 'AnimatedView' },
      Easing: { out: easing => easing },
      ReduceMotion: { System: 'system' },
      useSharedValue(initial) {
        return renderer.hooks.useRef({
          get: () => rotations.at(-1) ?? initial,
          set: value => rotations.push(value),
        }).current;
      },
      useAnimatedStyle: getStyle => getStyle(),
      withTiming: (value, config) => ({ value, config }),
    },
    '@/components/page-header': { PageHeader: 'PageHeader' },
    '@/constants/media-tab-icons': loadModule('constants/media-tab-icons.ts'),
    '@/types/library': loadModule('types/library.ts'),
    '@/services/library': {
      async getLibraryPreviews() {
        requests++;
        return { movie: [], tv: [], book: [book], game: [] };
      },
    },
  });
  const { default: LibraryHome } = renderer.load('app/(app)/(tabs)/my-library/index.tsx');
  const subject = () => LibraryHome().type();
  const tree = await renderer.render(subject);
  assert.equal(requests, 1);
  assert.equal(rotations.length, 0);

  const sections = findNodes(tree, node => node.type === 'Link' && node.props.href.pathname === '/my-library/[type]');
  assert.deepEqual(sections.map(node => node.props.href.params.type), ['movie', 'tv', 'book', 'game']);
  for (const { props: { children: heading } } of sections) {
    assert.equal(heading.props.style.flexDirection, 'row');
    assert.equal(heading.props.children[0].type, 'SymbolView');
    assert.equal(heading.props.children[2].type, 'ChevronRight');
  }
  const cards = findNodes(tree, node => node.type === 'View' && node.props.children?.[0]?.type === 'Link');
  assert.equal(cards.length, 4);
  assert.ok(cards.every(card => card.props.style[0].marginHorizontal === 16));
  const [coverRow] = findNodes(tree, node => node.type === 'ScrollView' && node.props.horizontal);
  const [coverLink] = findNodes(coverRow, node => node.type === 'Link');
  assert.equal(coverLink.props.href.pathname, '/my-library/[type]/[id]');
  assert.equal(coverLink.props.href.params.type, 'book');
  assert.equal(coverLink.props.href.params.id, book.externalId);
  assert.ok(coverLink.props.children.props.style.height > 0);
  const [header] = findNodes(tree, node => node.type === 'PageHeader');
  assert.equal(header.props.titleAction.props.accessibilityLabel, 'Refresh My Library');
  header.props.titleAction.props.onPress();
  assert.equal(requests, 2);
  assert.equal(rotations[0], 0);
  assert.equal(rotations[1].value, 360);
  assert.equal(rotations[1].config.reduceMotion, 'system');
  assert.equal(rotations.length, 2);

  const cover = coverLink.props.children.props.children;
  const coverSubject = () => cover.type(cover.props);
  const coverView = await renderer.render(coverSubject);
  const [image] = findNodes(coverView, node => node.type === 'Image');
  assert.equal(image.props.source.uri, book.posterUrl);
  image.props.onError();
  const fallback = await renderer.render(coverSubject);
  assert.equal(fallback.props.children.props.children, book.title);
});

test('detail action uses the provided membership, switches immediately, and rolls back on failure', async () => {
  const mutations = [];
  const errors = [];
  let reads = 0;
  let resolveMutation, rejectMutation;
  const renderer = createRenderer({
    'lucide-react-native': { Minus: 'Minus', Plus: 'Plus' },
    '@/services/library': {
      async getLibraryMembership() {
        reads++;
        return { saved: true };
      },
      setLibraryMembership(...args) {
        mutations.push(args);
        return new Promise((resolve, reject) => {
          resolveMutation = resolve;
          rejectMutation = reject;
        });
      },
    },
  });
  const { LibraryAction } = renderer.load('components/library-action.tsx');
  const props = { type: 'tv', externalId: '42', initialSaved: false, onError: message => errors.push(message) };
  const subject = () => LibraryAction(props);

  const add = await renderer.render(subject);
  assert.equal(reads, 0);
  assert.equal(add.props.children.type, 'Plus');
  const attempt = add.props.onPress();
  const saving = await renderer.render(subject);
  assert.equal(saving.props.children.type, 'Minus');
  assert.equal(saving.props.disabled, true);

  rejectMutation(new Error('Unable to save'));
  await attempt;
  const restored = await renderer.render(subject);
  assert.equal(restored.props.children.type, 'Plus');
  assert.equal(errors.at(-1), 'Unable to save');

  const retry = restored.props.onPress();
  resolveMutation({ saved: true });
  await retry;
  const remove = await renderer.render(subject);
  assert.equal(remove.props.accessibilityLabel, 'Remove from My Library');
  assert.equal(remove.props.disabled, false);
  assert.deepEqual(mutations, [['tv', '42', true], ['tv', '42', true]]);
});

function loadLibraryService({ token = 'clerk-session-token', respond } = {}) {
  let request;
  const transpile = file => ts.transpileModule(
    fs.readFileSync(path.join(__dirname, '../../frontend/src/services', file), 'utf8'),
    { compilerOptions: { module: ts.ModuleKind.CommonJS } },
  ).outputText;
  const api = {};
  vm.runInNewContext(transpile('api.ts'), {
    exports: api, Headers,
    process: { env: { EXPO_PUBLIC_API_URL: 'https://api.palette.test' } },
    require(name) {
      assert.equal(name, '@clerk/expo');
      return { getClerkInstance: () => ({ session: { getToken: async () => token } }) };
    },
    fetch: async (url, options) => {
      request = { url, options };
      return respond ? respond(options) : { ok: true, json: async () => ({ items: [], page: 2, hasMore: false }) };
    },
  });
  const service = {};
  vm.runInNewContext(transpile('library.ts'), {
    exports: service, AbortController, setTimeout, clearTimeout,
    require(name) {
      if (name === '@/services/api') return api;
      assert.equal(name, '@/constants/logging');
      return { LOGGING_REQUEST_TIMEOUT_MS: 10 };
    },
  });
  return { ...service, request: () => request };
}

test('library list sends the Clerk bearer token and only the requested category and page', async () => {
  const service = loadLibraryService();
  const result = await service.getLibraryPage('book', 2);
  const { url, options } = service.request();
  assert.equal(result.page, 2);
  assert.equal(url, 'https://api.palette.test/api/me/library/book?page=2');
  assert.equal(options.method, 'GET');
  assert.equal(options.body, undefined);
  assert.equal(options.headers.get('Authorization'), 'Bearer clerk-session-token');
});

test('an expired session produces a sign-in error', async () => {
  const service = loadLibraryService({ respond: () => ({ ok: false, status: 401 }) });
  await assert.rejects(service.getLibraryPreviews(), /Sign in again/);
});

test('a signed-out client cannot manufacture an authenticated request', async () => {
  const service = loadLibraryService({ token: null, respond: options => {
    assert.equal(options.headers.get('Authorization'), null);
    return { ok: false, status: 401 };
  } });
  await assert.rejects(service.getLibraryPreviews(), /Sign in again/);
});

test('library requests time out and report a useful error', async () => {
  const service = loadLibraryService({ respond: ({ signal }) => new Promise((resolve, reject) => {
    signal.addEventListener('abort', () => reject(new Error('aborted')), { once: true });
  }) });
  await assert.rejects(service.getLibraryPreviews(), /request timed out/);
});

test('add and remove use authenticated item routes with encoded IDs', async () => {
  const service = loadLibraryService({ respond: ({ method }) => ({
    ok: true,
    json: async () => ({ saved: method === 'PUT' }),
  }) });
  assert.equal((await service.setLibraryMembership('book', 'book+id', true)).saved, true);
  assert.equal(service.request().url, 'https://api.palette.test/api/me/library/book/book%2Bid');
  assert.equal(service.request().options.method, 'PUT');
  assert.equal(service.request().options.headers.get('Authorization'), 'Bearer clerk-session-token');
  assert.equal((await service.setLibraryMembership('book', 'book+id', false)).saved, false);
  assert.equal(service.request().options.method, 'DELETE');
});
