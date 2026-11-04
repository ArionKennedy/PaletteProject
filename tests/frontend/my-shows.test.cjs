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
        useAuth: () => ({ getToken: async () => userId, isLoaded: true, isSignedIn: true, userId, sessionId }),
      };
      if (name === 'expo-router') return { useFocusEffect(callback) {
        hooks.useEffect(() => {
          cleanup?.();
          focusCallback = callback;
          cleanup = callback();
        }, [callback]);
      } };
      if (name === '@/components/show-list') return { ShowList: 'ShowList' };
      if (name === '@/services/show-logs') return { getShowLogs: async token => {
        calls++;
        return { items: [{ mediaId: token, title: token }], page: 1, hasMore: false };
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
