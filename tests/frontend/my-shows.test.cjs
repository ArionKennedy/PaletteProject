const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const ts = require('../../frontend/node_modules/typescript');

test('My Shows does not reload for a changing getToken reference; account changes and refocus do reload', async () => {
  const slots = [];
  let index = 0, pending = [], dirty = false, calls = 0, cleanup, focusCallback;
  let userId = 'user-one';
  let sessionId = 'session-one';
  let tree;
  const same = (a, b) => a && a.length === b.length && a.every((v, i) => Object.is(v, b[i]));
  const hooks = {
    useState(initial) {
      const i = index++;
      if (!(i in slots)) slots[i] = initial;
      return [slots[i], value => {
        const next = typeof value === 'function' ? value(slots[i]) : value;
        if (!Object.is(next, slots[i])) { slots[i] = next; dirty = true; }
      }];
    },
    useRef(initial) {
      const i = index++;
      return slots[i] ?? (slots[i] = { current: initial });
    },
    useCallback(callback, deps) {
      const i = index++;
      if (!slots[i] || !same(slots[i].deps, deps)) slots[i] = { callback, deps };
      return slots[i].callback;
    },
    useEffect(callback, deps) {
      const i = index++;
      if (!slots[i] || !same(slots[i], deps)) {
        slots[i] = deps;
        pending.push(callback);
      }
    },
  };
  const exports = {};
  const source = fs.readFileSync(path.join(__dirname, '../../frontend/src/app/(app)/(tabs)/my-shows.tsx'), 'utf8');
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
  }).outputText;
  vm.runInNewContext(code, {
    exports,
    require(name) {
      if (name === 'react') return hooks;
      if (name === 'react/jsx-runtime') return { jsx: (type, props) => ({ type, props }) };
      if (name === '@clerk/expo') return {
        // Deliberately create a new function on EVERY render, reproducing the reported trigger.
        useAuth: () => ({ getToken: async () => { throw new Error('Logging must not request a token'); }, isLoaded: true, isSignedIn: true, userId, sessionId }),
      };
      if (name === 'expo-router') return { useFocusEffect(callback) {
        hooks.useEffect(() => {
          cleanup?.();
          focusCallback = callback;
          cleanup = callback();
        }, [callback]);
      } };
      if (name === '@/components/show-list') return { ShowList: 'ShowList' };
      if (name === '@/services/show-logs') return { getShowLogs: async requestedUserId => {
        calls++;
        return { items: [{ mediaId: requestedUserId, title: requestedUserId }], page: 1, hasMore: false };
      } };
      throw new Error('Unexpected import: ' + name);
    },
  });
  async function render() {
    dirty = false;
    index = 0;
    pending = [];
    tree = exports.default();
    pending.forEach(effect => effect());
    await new Promise(resolve => setImmediate(resolve));
  }
  async function settle() {
    for (let i = 0; i < 12; i++) {
      await render();
      if (!dirty) return;
    }
    assert.fail('Screen kept rerendering instead of settling');
  }
  await settle();
  assert.equal(calls, 1);
  for (let i = 0; i < 5; i++) await render();
  assert.equal(calls, 1, 'Unrelated renders must not refetch');
  userId = 'user-two';
  sessionId = 'session-two';
  await settle();
  assert.equal(calls, 2);
  assert.equal(tree.props.shows[0].mediaId, 'user-two');
  cleanup();
  cleanup = focusCallback();
  await settle();
  assert.equal(calls, 3, 'Returning to the screen should refetch');
  cleanup();
});

test('history requests send Clerk userId in JSON without a bearer token', async () => {
  const source = fs.readFileSync(path.join(__dirname, '../../frontend/src/services/show-logs.ts'), 'utf8');
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS },
  }).outputText;
  const exports = {};
  let request;
  const page = { items: [], page: 2, hasMore: false };
  vm.runInNewContext(code, {
    exports, AbortController, setTimeout, clearTimeout,
    require(name) {
      assert.equal(name, '@/constants/logging');
      return { LOGGING_API_URL: 'http://localhost:5251', LOGGING_REQUEST_TIMEOUT_MS: 1000 };
    },
    fetch: async (url, options) => {
      request = { url, options };
      return { ok: true, json: async () => page };
    },
  });
  assert.equal(await exports.getShowLogs('user_test', 2), page);
  assert.equal(request.url, 'http://localhost:5251/api/me/shows/query');
  assert.equal(request.options.method, 'POST');
  assert.deepEqual(JSON.parse(request.options.body), { userId: 'user_test', page: 2 });
  assert.equal(request.options.headers.Authorization, undefined);
  assert.equal(request.options.headers['Content-Type'], 'application/json');
  request = undefined;
  await assert.rejects(exports.getShowLogs(null), /Sign in/);
  assert.equal(request, undefined, 'Missing identity must not trigger a request');
});
