# Palette Android APK and OTA

Run commands from `frontend/` with Node 24.21.0 and npm. The existing feature
branch is `feat/eas-build-update`, starting at the local `develop` commit.

The linked project is [@palette-comp4350/palette](https://expo.dev/accounts/palette-comp4350/projects/palette).
The organization display name is **Palette**.
The public project ID is `94e3c136-d9bd-4b1a-ac21-5094e94c8e27`.
Its production API URL points to `https://palette-api-1ceq.onrender.com`, and
the existing local Clerk publishable key has been synchronized to EAS with
sensitive visibility. No environment values were committed.

The initial [Android OTA update](https://expo.dev/accounts/palette-comp4350/projects/palette/updates/341e3715-6052-4de8-b7c3-1292a5339659)
was published successfully to `production`. Its runtime fingerprint is
`6504ee7ceda834ee552f1f98194656cd3861990f`, matching the first APK build.
The first [APK build](https://expo.dev/accounts/palette-comp4350/projects/palette/builds/fbed7a13-7221-4346-9a26-e8717f6e227e)
completed successfully and uses version `1.0.0` / Android version code `1`.

For the current Android APK, open [GitHub Releases](https://github.com/ArionKennedy/PaletteProject/releases)
and download the `.apk` from the latest release’s **Assets** section.

## One-time project setup

1. Check the Expo account with `npx eas-cli@latest whoami`. Use
   `npx eas-cli@latest login` if necessary. Never paste credentials into chat.
2. Link the team's existing project using `npx eas-cli@latest init --id <project-id>`.
   If there is no project, run `npx eas-cli@latest init` and choose its owner.
   Keep `expo.slug` (`palette`) and `expo.owner` (`palette-comp4350`) aligned
   with the linked project. This repository is already linked; do not reinitialize
   it to a different project for routine builds or updates.
3. Run `npx eas-cli@latest update:configure`. Verify that `app.json` contains
   `extra.eas.projectId`, `updates.url` pointing to `https://u.expo.dev/<project-id>`,
   and `runtimeVersion.policy: fingerprint`. Project IDs are public identifiers.
4. In the Expo project dashboard, create these variables for **production**:

   | Variable | Value | Visibility |
   | --- | --- | --- |
   | `EXPO_PUBLIC_API_URL` | Render's public HTTPS origin, without a trailing slash | Plain text or sensitive |
   | `EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY` | The app's Clerk publishable key | Plain text or sensitive |

   Do not use secret visibility for these client variables: OTA bundling must
   read them. They are embedded in the app. Backend keys, Supabase database
   credentials, and Clerk secret keys belong only on the backend.

The Android application ID is `com.palette.app`. Confirm it before the first
build, then keep it stable so a new APK can replace the installed app.
No iOS cloud build or App Store distribution is configured.

## Build and install the standalone APK

```sh
npm ci
npm run lint
npm run typecheck
npm run build:android
```

The production profile produces an internally distributed **release APK**, not
a development client or Play Store AAB. It embeds the JS bundle and assets and
uses EAS's production variables. Its post-install hook checks release config.
On the first interactive build, let EAS generate/store the Android keystore;
keep the same signing key for subsequent builds. Never commit signing files.

Download the APK from the successful EAS build page, install it on an Android
device, and allow installation from that source when prompted. Stop Metro and
verify cold start, Clerk sign-in, movie/TV search, and detail requests against
Render over mobile data or Wi-Fi. An internet connection is still required for
authentication and backend data. The Render service may need time to wake up.

## Publish an Android OTA update

```sh
npx eas-cli@latest env:exec production 'npm run validate:release'
npm run update:android -- --message 'Describe the change'
```

Both the APK and OTA target channel `production` and environment `production`.
Using `--environment production` keeps a developer's local `.env` from changing
the published bundle. Updates use Expo's default startup check: open the app
online, allow the update to download, then fully close and reopen it to apply.

JS and asset changes can update an installed compatible APK. Native dependency,
SDK, config-plugin, and other native changes can change the fingerprint runtime;
build and install a new APK for those changes. Publishing an OTA alone will not
upgrade an incompatible APK. Use the same EAS environment for validation,
fingerprinting, building, and publishing.

## Tester installation and update behavior

Testers download and install the Android APK once. They can use Palette directly
with an internet connection; they need no source code, terminal commands, Expo
Go, Expo account, or development server.

For ordinary JavaScript/TypeScript, component, styling, and bundled asset
changes, a PR merged into `master` triggers the Android OTA publication after
checks pass. The installed app downloads a compatible update when opened online.
After it finishes downloading, fully close and reopen the app to apply it.
Testers usually do not need to download another APK or install an update manually.

**Exception: native changes require a new APK installation.** Examples include
upgrading Expo or React Native, adding or changing a native dependency, changing
native permissions or config plugins, and changing launcher icons or native
splash-screen configuration. These changes can produce a new runtime
fingerprint, so existing APKs continue using their compatible runtime until
testers install the new APK.

For a native release, the team builds and shares a new signed APK and tells
testers to install it as an update. Keep the Android package name and EAS signing
key unchanged, and increase the Android version code for subsequent APK releases.
Testers still only need the download link; the build commands are the team's
responsibility. OTA-only changes use the existing installed APK.

## GitHub Actions

The root workflow `.github/workflows/eas-update.yml` publishes **Android only**
after a PR containing frontend changes is merged into `master`, or on manual
dispatch from `master`. Direct pushes and PRs closed without merging do not
publish automatically.
It serializes publication, installs the frontend lockfile with Node 24.21.0,
runs lint/typecheck, validates EAS production variables, then publishes the
commit to `production`. Feature-branch pushes and unmerged PRs do not publish.

The automatic trigger uses `pull_request: closed` with `merged == true` and
checks out the merge commit. It is intended for the team's branches in this
repository. GitHub withholds repository secrets from fork PR workflows; for a
merged fork PR, use the manual entry point from `master` if the token check
fails. A manual entry point is visible once the workflow also exists on the
repository's default branch.

Create an Expo access token with access to the linked project and save it in
the GitHub repository's Actions secret named `EXPO_TOKEN`. Enter it directly in
GitHub; do not put it in a file, command argument, or chat. API and Clerk public
configuration stay in EAS rather than being duplicated as GitHub variables.

To enable automatic publication, merge the workflow into `master`.
Backend deployment remains in its existing workflow.

## References

- [SDK 58 expo-updates](https://docs.expo.dev/versions/v58.0.0/sdk/updates/)
- [APK builds](https://docs.expo.dev/build-reference/apk/)
- [EAS Update setup](https://docs.expo.dev/eas-update/getting-started/)
- [EAS environment usage](https://docs.expo.dev/eas/environment-variables/usage/)
- [Runtime versions](https://docs.expo.dev/eas-update/runtime-versions/)
- [GitHub Actions](https://docs.expo.dev/eas-update/github-actions/)
