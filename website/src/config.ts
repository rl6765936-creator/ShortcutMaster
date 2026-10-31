// Project links. Change REPO if the repository moves.
const REPO = 'https://github.com/rl6765936-creator/ShortcutMaster';

export const PROJECT = {
  version: '1.0.0',
  repoUrl: REPO,
  zipUrl: `${REPO}/archive/refs/heads/main.zip`,
  /** Always points at shortcut.exe of the newest release. */
  exeUrl: `${REPO}/releases/latest/download/shortcut.exe`,
  releasesUrl: `${REPO}/releases/latest`,
  readmeUrl: `${REPO}#readme`,
  issuesUrl: `${REPO}/issues`,
  cloneCommand: `git clone ${REPO}.git`,
  dotnetUrl: 'https://dotnet.microsoft.com/download/dotnet/8.0',
} as const;
