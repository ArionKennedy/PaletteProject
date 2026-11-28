const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const ts = require('../../frontend/node_modules/typescript');

const sourceRoot = path.join(__dirname, '../../frontend/src');
const compiledModules = new Map();
const email = 'reader@example.com';

function createHarness(signIn, signUpState) {
  const loadedModules = new Map();
  const routes = [];
  let screen;
  const element = (type, props) => ({ type, props });
  const modules = {
    'react/jsx-runtime': { jsx: element, jsxs: element, Fragment: 'Fragment' },
    'react-native': { Platform: { OS: 'web' }, View: 'View', Pressable: 'Pressable' },
    react: {
      useState(initial) {
        const owner = screen;
        const slot = owner.index++;
        if (!(slot in owner.slots)) {
          owner.slots[slot] = typeof initial === 'function' ? initial() : initial;
        }
        return [owner.slots[slot], value => {
          const nextValue = typeof value === 'function' ? value(owner.slots[slot]) : value;
          owner.dirty ||= !Object.is(owner.slots[slot], nextValue);
          owner.slots[slot] = nextValue;
        }];
      },
      useRef(initial) {
        const slot = screen.index++;
        return screen.slots[slot] ?? (screen.slots[slot] = { current: initial });
      },
      useCallback(callback, dependencies) {
        const slot = screen.index++;
        const previous = screen.slots[slot];
        if (previous && dependencies.every((value, index) => Object.is(value, previous.dependencies[index]))) {
          return previous.callback;
        }
        screen.slots[slot] = { callback, dependencies };
        return callback;
      },
      useEffect(callback, dependencies) {
        const slot = screen.index++;
        const previous = screen.slots[slot];
        if (previous && dependencies.every((value, index) => Object.is(value, previous[index]))) return;
        screen.slots[slot] = dependencies;
        screen.pendingEffects.push([slot, callback]);
      },
    },
    '@clerk/expo': {
      useAuth: () => ({ isLoaded: true }),
      useSignIn: () => ({ signIn, fetchStatus: 'idle' }),
      useSignUp: () => signUpState,
      isClerkAPIResponseError: error => Array.isArray(error.errors),
    },
    'expo-router': {
      router: { replace: route => routes.push(route) },
      useFocusEffect(callback) {
        const slot = screen.index++;
        if (screen.effects.get(slot)?.callback !== callback) {
          screen.pendingEffects.push([slot, callback]);
        }
      },
    },
    'lucide-react-native': { AlertCircleIcon: 'AlertCircleIcon' },
    '@/components/ui/button': { Button: 'Button' },
    '@/components/ui/card': {
      Card: 'Card', CardContent: 'CardContent', CardDescription: 'CardDescription',
      CardHeader: 'CardHeader', CardTitle: 'CardTitle',
    },
    '@/components/ui/input': { Input: 'Input' },
    '@/components/ui/icon': { Icon: 'Icon' },
    '@/components/ui/label': { Label: 'Label' },
    '@/components/ui/text': { Text: 'Text' },
    '@/components/ui/alert': { Alert: 'Alert', AlertDescription: 'AlertDescription', AlertTitle: 'AlertTitle' },
    '@/lib/utils': { cn: (...classes) => classes.filter(Boolean).join(' ') },
  };

  function loadModule(name) {
    if (Object.hasOwn(modules, name)) return modules[name];
    if (loadedModules.has(name)) return loadedModules.get(name);

    if (!compiledModules.has(name)) {
      const relativePath = path.join(sourceRoot, name.slice(2));
      const file = [`${relativePath}.ts`, `${relativePath}.tsx`].find(file => fs.existsSync(file));
      assert.ok(file, `Missing test module ${name}`);
      compiledModules.set(name, ts.transpileModule(fs.readFileSync(file, 'utf8'), {
        compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
      }).outputText);
    }
    const exports = {};
    vm.runInNewContext(compiledModules.get(name), {
      exports, require: loadModule, Date, setTimeout, clearTimeout, setInterval, clearInterval,
      console: { error() {} },
    });
    loadedModules.set(name, exports);
    return exports;
  }

  return {
    routes,
    mount(name = 'reset-password-form', props) {
      const [Component] = Object.values(loadModule(`@/components/${name}`));
      const instance = { slots: [], effects: new Map(), focused: true };
      return {
        render() {
          screen = instance;
          let tree;
          // Flush effects and their state updates after each render.
          for (let pass = 0; pass < 10; pass++) {
            instance.index = 0;
            instance.dirty = false;
            instance.pendingEffects = [];
            tree = Component(props);
            if (instance.focused) {
              for (const [slot, callback] of instance.pendingEffects) {
                instance.effects.get(slot)?.cleanup?.();
                instance.effects.set(slot, { callback, cleanup: callback() });
              }
            }
            if (!instance.dirty) return tree;
          }
          assert.fail('Form did not stabilize after running its focus effects');
        },
        blur() {
          for (const { cleanup } of instance.effects.values()) cleanup?.();
          instance.effects.clear();
          instance.focused = false;
        },
      };
    },
  };
}

function find(node, matches) {
  if (Array.isArray(node)) return node.map(child => find(child, matches)).find(Boolean);
  if (!node?.props) return undefined;
  return matches(node) ? node : find(node.props.children, matches);
}

function button(tree, label) {
  return find(tree, node => node.type === 'Button'
    && find(node.props.children, child => child.type === 'Text' && child.props.children === label)).props;
}

function input(tree, id) {
  return find(tree, node => node.props.id === id
    && (node.type === 'Input' || node.type?.name === 'NewPasswordField'))?.props;
}

test('new password field forwards keyboard props and updates visibility and length feedback', () => {
  const ref = { current: null };
  const onSubmitEditing = () => {};
  const props = {
    id: 'password',
    label: 'New password',
    value: '1234567890123🔑',
    ref,
    returnKeyType: 'next',
    submitBehavior: 'submit',
    onSubmitEditing,
  };
  const field = createHarness().mount('new-password-field', props);
  const initial = field.render();
  const initialInput = input(initial, 'password');
  assert.equal(initialInput.ref, ref);
  assert.equal(initialInput.returnKeyType, 'next');
  assert.equal(initialInput.submitBehavior, 'submit');
  assert.equal(initialInput.onSubmitEditing, onSubmitEditing);
  assert.equal(initialInput.secureTextEntry, true);
  assert.ok(find(initial, node => node.props.accessibilityLabel === 'At least 15 characters, not met'));

  find(initial, node => node.props.accessibilityLabel === 'Show password').props.onPress();
  props.value = '12345678901234🔑';
  const visible = field.render();
  assert.equal(input(visible, 'password').secureTextEntry, false);
  assert.equal(input(visible, 'password').value, props.value);
  assert.ok(find(visible, node => node.props.accessibilityLabel === 'At least 15 characters, met'));

  find(visible, node => node.props.accessibilityLabel === 'Hide password').props.onPress();
  assert.equal(input(field.render(), 'password').secureTextEntry, true);
});

test('sign up explains invalid email on blur and guards both submit paths', async () => {
  const requests = [];
  const passwordError = 'Meet the password requirements above to continue.';
  const signUpState = {
    errors: {},
    fetchStatus: 'idle',
    signUp: {
      async password(params) {
        requests.push(params);
        signUpState.errors = { fields: { emailAddress: { longMessage: 'Email already registered' } } };
        return { error: {} };
      },
    },
  };
  const form = createHarness(undefined, signUpState).mount('sign-up-form');
  const initial = form.render();
  assert.equal(input(initial, 'email')['aria-invalid'], false);
  assert.equal(button(initial, 'Continue').disabled, true);
  input(initial, 'email').onChangeText('name@');
  input(initial, 'password').onChangeText('long-test-password');
  const invalid = form.render();
  assert.equal(input(invalid, 'email')['aria-invalid'], false);
  input(invalid, 'email').onBlur();
  const blurred = form.render();
  assert.equal(input(blurred, 'email')['aria-invalid'], true);
  assert.equal(button(blurred, 'Continue').disabled, true);
  await input(blurred, 'password').onSubmitEditing();
  assert.equal(requests.length, 0);

  input(blurred, 'email').onChangeText('name+tag@example.museum');
  input(blurred, 'password').onChangeText('too-short');
  const short = form.render();
  assert.equal(input(short, 'email')['aria-invalid'], false);
  assert.equal(input(short, 'password')['aria-invalid'], false);
  assert.equal(button(short, 'Continue').disabled, true);
  await input(short, 'password').onSubmitEditing();
  assert.equal(requests.length, 0);

  const rejectedPassword = form.render();
  assert.equal(input(rejectedPassword, 'password')['aria-invalid'], true);
  assert.ok(find(rejectedPassword, node => node.type === 'Text' && node.props.children === passwordError));

  input(rejectedPassword, 'password').onChangeText('long-test-password');
  const ready = form.render();
  assert.equal(input(ready, 'password')['aria-invalid'], false);
  assert.equal(find(ready, node => node.type === 'Text' && node.props.children === passwordError), undefined);
  assert.equal(button(ready, 'Continue').disabled, false);
  await button(ready, 'Continue').onPress();
  assert.equal(requests[0].emailAddress, 'name+tag@example.museum');
  const rejected = form.render();
  assert.ok(find(rejected, node => node.type === 'AlertDescription'
    && node.props.children === 'Email already registered'));
  input(rejected, 'email').onChangeText('another@example.com');
  assert.equal(input(form.render(), 'email')['aria-invalid'], false);
});

test('reset shows password feedback on blur and reuses an accepted verification code', async t => {
  t.mock.timers.enable({ apis: ['setTimeout'] });
  const passwords = [];
  const passwordError = 'Meet the password requirements above to continue.';
  const isPasswordError = node => node.type === 'Text' && node.props.children === passwordError;
  let verificationCalls = 0;
  const signIn = {
    id: 'reset-attempt',
    status: 'needs_first_factor',
    resetPasswordEmailCode: {
      async verifyCode() {
        assert.equal(signIn.status, 'needs_first_factor');
        verificationCalls++;
        signIn.status = 'needs_new_password';
        return { error: null };
      },
      async submitPassword({ password }) {
        assert.equal(signIn.status, 'needs_new_password');
        passwords.push(password);
        if (passwords.length === 1) return { error: { code: 'form_password_pwned' } };
        signIn.status = 'complete';
        return { error: null };
      },
    },
    async finalize({ navigate }) {
      navigate();
    },
  };
  const app = createHarness(signIn);
  const form = app.mount();
  const initial = form.render();
  let codeFocused = false;
  input(initial, 'code').ref.current = { focus: () => { codeFocused = true; } };
  assert.equal(input(initial, 'password').returnKeyType, 'next');
  input(initial, 'password').onSubmitEditing();
  assert.equal(codeFocused, true);
  input(initial, 'password').onChangeText('too-short');
  input(initial, 'code').onChangeText('123456');
  const short = form.render();
  assert.equal(button(short, 'Reset Password').disabled, true);
  assert.equal(find(short, isPasswordError), undefined);
  input(short, 'password').onBlur();
  const blurred = form.render();
  assert.ok(find(blurred, isPasswordError));
  assert.equal(input(blurred, 'password')['aria-invalid'], true);
  await input(blurred, 'code').onSubmitEditing();
  assert.equal(verificationCalls, 0);

  input(blurred, 'password').onChangeText('rejected-test-password');
  const ready = form.render();
  assert.equal(find(ready, isPasswordError), undefined);
  assert.equal(input(ready, 'password')['aria-invalid'], false);
  await button(ready, 'Reset Password').onPress();

  const verified = form.render();
  assert.equal(input(verified, 'code'), undefined);
  assert.equal(button(verified, 'Reset Password').disabled, false);
  assert.equal(input(verified, 'password').returnKeyType, 'send');
  input(verified, 'password').onChangeText('too-short');
  const shortAfterVerification = form.render();
  assert.equal(button(shortAfterVerification, 'Reset Password').disabled, true);
  await input(shortAfterVerification, 'password').onSubmitEditing();
  assert.equal(passwords.length, 1);

  input(shortAfterVerification, 'password').onChangeText('replacement-test-password');
  await input(form.render(), 'password').onSubmitEditing();

  assert.equal(verificationCalls, 1);
  assert.deepEqual(passwords, ['rejected-test-password', 'replacement-test-password']);
  assert.deepEqual(app.routes, ['/(app)/(tabs)/movies']);
  form.blur();
});

test('a thrown resend error is displayed and the user can resend again', async t => {
  t.mock.timers.enable({ apis: ['Date', 'setTimeout', 'setInterval'] });
  let resendCalls = 0;
  const form = createHarness({
    id: 'reset-attempt',
    identifier: email,
    status: 'needs_first_factor',
    resetPasswordEmailCode: {
      async sendCode() {
        resendCalls++;
        if (resendCalls === 1) throw new Error('Network unavailable');
        return { error: null };
      },
    },
  }).mount();

  input(form.render(), 'code').onChangeText('123456');
  await button(form.render(), 'Resend code').onPress();

  const failed = form.render();
  const feedback = find(failed, node => node.type === 'AlertDescription');
  assert.match(feedback.props.children, /Could not resend/);
  assert.equal(button(failed, 'Resend code').disabled, false);
  await button(failed, 'Resend code').onPress();

  const resent = form.render();
  assert.equal(input(resent, 'code').value, '');
  assert.equal(button(resent, 'Resend in 30s').disabled, true);
  assert.equal(resendCalls, 2);
  form.blur();
});

test('resending without an active attempt returns to Forgot password', async () => {
  const app = createHarness({ id: null, status: 'needs_identifier' });
  const form = app.mount();

  await button(form.render(), 'Resend code').onPress();

  assert.deepEqual(app.routes, ['/forgot-password']);
  form.blur();
});

test('going back and re-entering the same email resumes the active attempt', async () => {
  let sends = 0;
  const app = createHarness({
    id: 'reset-attempt',
    identifier: email,
    status: 'needs_first_factor',
    firstFactorVerification: {
      strategy: 'reset_password_email_code',
      status: 'unverified',
      expireAt: new Date(Date.now() + 60_000),
    },
    async create() {
      assert.fail('The existing reset attempt should be reused');
    },
    resetPasswordEmailCode: {
      async sendCode() {
        sends++;
        return { error: null };
      },
    },
  });

  const reset = app.mount();
  button(reset.render(), 'Back to Sign in').onPress();
  reset.blur();

  const forgot = app.mount('forgot-password-form');
  input(forgot.render(), 'email').onChangeText(' Reader@Example.com ');
  await button(forgot.render(), 'Reset your password').onPress();

  assert.equal(sends, 0);
  assert.deepEqual(app.routes, ['/login', '/reset-password']);
  forgot.blur();
});

test('the first code starts the resend countdown and a resend starts it again', async t => {
  t.mock.timers.enable({ apis: ['Date', 'setTimeout', 'setInterval'] });
  let sends = 0;
  let verifiedCode;
  const signIn = {
    status: 'needs_identifier',
    async create({ identifier }) {
      signIn.identifier = identifier;
      signIn.id = 'reset-attempt';
      signIn.status = 'needs_first_factor';
      return { error: null };
    },
    resetPasswordEmailCode: {
      async sendCode() {
        sends++;
        return { error: null };
      },
      async verifyCode({ code }) {
        verifiedCode = code;
        return { error: { code: 'form_code_incorrect' } };
      },
    },
  };
  const app = createHarness(signIn);
  const forgot = app.mount('forgot-password-form');
  input(forgot.render(), 'email').onChangeText(email);
  await button(forgot.render(), 'Reset your password').onPress();
  assert.deepEqual(app.routes, ['/reset-password']);
  forgot.blur();

  const reset = app.mount();
  const resend = button(reset.render(), 'Resend in 30s');
  assert.equal(resend.disabled, true);
  await resend.onPress();
  assert.equal(sends, 1);

  input(reset.render(), 'password').onChangeText('example-test-password');
  input(reset.render(), 'code').onChangeText('123456');
  const submit = button(reset.render(), 'Reset Password');
  assert.equal(submit.disabled, false);
  await submit.onPress();
  assert.equal(verifiedCode, '123456');

  t.mock.timers.tick(29_000);
  assert.equal(button(reset.render(), 'Resend in 1s').disabled, true);
  t.mock.timers.tick(1_000);
  const ready = button(reset.render(), 'Resend code');
  assert.equal(ready.disabled, false);
  await ready.onPress();
  assert.equal(sends, 2);
  assert.equal(button(reset.render(), 'Resend in 30s').disabled, true);
  reset.blur();
});

test('a code request error clears after five seconds and when leaving the page', async t => {
  t.mock.timers.enable({ apis: ['Date', 'setTimeout', 'setInterval'] });
  const app = createHarness({
    async create() {
      return { error: null };
    },
    resetPasswordEmailCode: {
      async sendCode() {
        return {
          error: {
            status: 429,
            retryAfter: 60,
            errors: [{ code: 'verification_code_too_many_requests', message: 'Too many code requests.' }],
          },
        };
      },
    },
  });
  const forgot = app.mount('forgot-password-form');
  const readError = form => find(form.render(), node => node.type === 'AlertTitle')?.props.children;
  input(forgot.render(), 'email').onChangeText(email);
  await button(forgot.render(), 'Reset your password').onPress();

  assert.equal(readError(forgot), 'Unable to send verification code');
  assert.equal(button(forgot.render(), 'Request a new code in 60s').disabled, true);
  t.mock.timers.tick(4_999);
  assert.equal(readError(forgot), 'Unable to send verification code');
  t.mock.timers.tick(1);
  assert.equal(readError(forgot), undefined);
  assert.equal(button(forgot.render(), 'Request a new code in 55s').disabled, true);

  t.mock.timers.tick(55_000);
  await button(forgot.render(), 'Reset your password').onPress();
  assert.equal(readError(forgot), 'Unable to send verification code');
  button(forgot.render(), 'Back to Sign in').onPress();
  forgot.blur();

  assert.deepEqual(app.routes, ['/login']);
  assert.equal(readError(forgot), undefined);
  const login = app.mount('sign-in-form');
  assert.equal(readError(login), undefined);
  login.blur();
});
