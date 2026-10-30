# ShortcutMaster website

The project site, published at https://rl6765936-creator.github.io/ShortcutMaster/.
Built with React, TypeScript and Vite, in English and Hebrew.

| Path | Contents |
| --- | --- |
| `src/components/` | Page sections (hero, features, demo, install, FAQ). |
| `src/sim/` | The in-browser demo: a simulation of ShortcutMaster in PowerShell, CMD and Git Bash, with tests. |
| `src/i18n/` | English and Hebrew text. |
| `src/config.ts` | Repository links and version. |

```sh
npm ci
npm run dev      # local server at http://localhost:5173/ShortcutMaster/
npm test         # demo engine tests
npm run build    # type-check and build into dist/
```

`.github/workflows/pages.yml` builds, tests and deploys the site on every push to `main` that changes `website/`.
