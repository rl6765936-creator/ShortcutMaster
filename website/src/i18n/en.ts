// English text. he.ts must have exactly the same shape (checked by TypeScript).

export const en = {
  a11y: {
    skip: 'Skip to content',
    menu: 'Menu',
    language: 'Language',
    copy: 'Copy',
    copied: 'Copied',
  },
  nav: {
    features: 'Features',
    demo: 'Try the demo',
    install: 'Install',
    faq: 'FAQ',
    github: 'GitHub',
  },
  hero: {
    titleA: 'Your commands,',
    titleB: 'one word',
    titleC: 'away.',
    lead:
      'ShortcutMaster is a small command-line tool. Give any command, app, folder or website a short name once — then type just that name in PowerShell, CMD, Git Bash, Bash or Zsh, and it runs.',
    primary: 'Get it on GitHub',
    secondary: 'Try it in the browser',
    ticks: ['Free and open source', 'Every terminal you use', 'Saved once, works everywhere'],
    artLabel: 'Your shortcuts live in one file and reach every terminal',
  },
  features: {
    title: 'What it does',
    lead: 'Every card below is a real option of the installed command, shortcut. Combine them freely.',
    anything: {
      title: 'One short name for anything',
      body: 'The type is detected for you: a terminal command, a program, a folder, a website — or a folder your terminal should move into.',
      types: {
        command: 'command',
        app: 'app',
        folder: 'folder',
        url: 'website',
        cd: 'move here',
      },
    },
    shells: {
      title: 'Works in every terminal',
      body: 'One shortcut install connects PowerShell, CMD, Git Bash, Bash and Zsh — including the terminal inside VS Code.',
    },
    chain: {
      title: 'Several steps, one word',
      body: 'Add --then to run more actions in order. If a step fails, the rest is skipped.',
    },
    args: {
      title: 'Arguments pass through',
      body: 'Whatever you type after the name is added to the last action.',
    },
    cd: {
      title: 'Really changes folder',
      body: 'A program cannot move the terminal that started it — so a cd shortcut is run by the terminal itself.',
    },
    safe: {
      title: 'Safe by default',
      body: 'Names that already run something (git, dir, cd…) are refused unless you add --force. A shortcut added to the file by hand asks before its first run.',
      taken: "'git' is already a command on this computer.",
      ask: 'Run it and trust it from now on? (y/N)',
    },
    saved: {
      title: 'Saved once, for good',
      body: 'Everything is kept in one small JSON file in your personal folder — easy to back up and to read.',
    },
    notesTitle: 'Good to know',
    notes: [
      'A new shortcut works by name in new terminal windows. In the window that is already open, use shortcut run.',
      'Write the action in quotes: shortcut add gs "git status".',
      'Your profile files keep their content — ShortcutMaster only adds one marked block that uninstall removes.',
      'No administrator rights are needed, not even for CMD.',
    ],
  },
  demo: {
    badge: 'Browser simulation',
    title: 'Try it right here',
    lead:
      'A simulated Windows PC with three terminals, running in your browser. Nothing touches your computer — the installed tool works the same way on your real one.',
    shell: 'Terminal',
    presetsLabel: 'Quick examples',
    presets: {
      git: 'Git status',
      web: 'Open a website',
      work: 'Jump into a project',
      chain: 'Start the day',
    },
    builder: {
      name: 'Name',
      nameHint: 'What you will type. Letters, digits, - and _.',
      action: 'Action',
      actionHint: 'A command, a program path, a folder or a URL.',
      type: 'Type',
      typeHint: 'Leave on auto unless the first step should move the terminal (cd).',
      auto: 'auto',
      then: 'Then',
      thenHint: 'More actions, run in order after the first one.',
      addStep: 'Add a step',
      removeStep: 'Remove step',
      force: 'Allow a name that is already a command',
    },
    command: 'Command',
    add: 'Add',
    tabs: {
      terminal: 'Terminal',
      json: 'shortcuts.json',
      alias: 'Alias file',
    },
    newWindow: 'New window',
    reset: 'Reset',
    inputLabel: 'Type a command and press Enter',
    placeholder: 'type here — try: shortcut --help',
    welcome: 'Simulated terminal. Build a shortcut on the left and press Add, or type commands here.',
    steps: ['Add a shortcut', 'Run shortcut install', 'Open a new window', 'Type its name'],
    emptyJson: '{}  (no shortcuts yet)',
    hints: {
      'hint.newWindow': 'Terminals load your shortcuts when they open — press “New window”, then type the name.',
      'hint.install': 'This shortcut exists, but the terminal does not know it yet. Run shortcut install, then open a new window.',
      'hint.quotes': 'Put the action in quotes, for example: shortcut add gs "git status"',
    } as Record<string, string>,
    footnote: 'Simulated output. Program results (git, npm…) are examples; ShortcutMaster messages match the real tool.',
  },
  install: {
    title: 'Install and run',
    lead: 'Setup takes a few minutes, needs the free .NET 8 SDK, and no administrator rights.',
    cardTitle: 'ShortcutMaster',
    cardBody: 'Windows, macOS and Linux. Installed as a global .NET tool, so shortcut works from any folder.',
    source: 'View source on GitHub',
    zip: 'Download ZIP',
    readme: 'README',
    dotnet: 'Get .NET 8',
    stepsTitle: 'Four steps',
    steps: [
      { title: 'Get the code', body: 'Clone the repository, or download the ZIP and extract it.' },
      { title: 'Build and install the command', body: 'From the ShortcutMaster folder. Afterwards, shortcut works in any folder.' },
      { title: 'Connect your terminals', body: 'Once. Adds a loader to PowerShell, CMD, Bash and Zsh.' },
      { title: 'Add a shortcut and use it', body: 'Open a new terminal window, then type just the name.' },
    ],
    examplesTitle: 'More examples',
    examples: [
      { label: 'A terminal command', cmd: 'shortcut add gs "git status"' },
      { label: 'A website', cmd: 'shortcut add github "https://github.com"' },
      { label: 'Move into a project, then open VS Code', cmd: 'shortcut add work --type cd "C:\\Projects\\MyApp" --then "code ."' },
      { label: 'Several steps in a row', cmd: 'shortcut add start "git pull" --then "npm install" --then "npm run dev"' },
      { label: 'Change what a shortcut does', cmd: 'shortcut update gs "git status --short"' },
      { label: 'See everything you saved', cmd: 'shortcut list' },
    ],
    troubleTitle: 'Troubleshooting',
    trouble: [
      {
        q: '“shortcut is not recognized…”',
        a: 'The global tools folder is not on your PATH yet. Close and reopen the terminal. On Linux/macOS, add ~/.dotnet/tools to PATH — the install message shows the exact line.',
      },
      {
        q: 'PowerShell: “running scripts is disabled on this system”',
        a: 'PowerShell blocks profile scripts by default on some PCs. Run once: Set-ExecutionPolicy -Scope CurrentUser RemoteSigned — then open a new window.',
      },
      {
        q: 'I added a shortcut but typing its name does nothing',
        a: 'Terminals read your shortcuts when they open. Open a new window — or use shortcut run <name> in the current one. Also check that shortcut install was run once.',
      },
      {
        q: '“… is already a command on this computer”',
        a: 'The name would hide an existing command. Pick another name, or add --force if you really want to override it.',
      },
      {
        q: '“Unrecognized command or argument …” when adding',
        a: 'The action needs quotes: shortcut add gs "git status". Without them, status is read as an extra argument.',
      },
    ],
    uninstallTitle: 'Removing it',
    uninstallBody: 'shortcut uninstall disconnects the terminals; dotnet tool uninstall --global ShortcutMaster removes the command. Your shortcuts file stays until you delete it.',
  },
  faq: {
    title: 'Questions and answers',
    items: [
      {
        q: 'Which systems and terminals are supported?',
        a: 'Windows, macOS and Linux with .NET 8. Terminals: Windows PowerShell 5.1, PowerShell 7, CMD, Git Bash, Bash and Zsh — and VS Code’s terminal, which uses one of them.',
      },
      {
        q: 'How does typing just the name work?',
        a: 'shortcut install adds one line to each terminal’s startup file (and CMD’s AutoRun setting). That line loads a small generated file with one function per shortcut — gs becomes a function that calls shortcut __run gs.',
      },
      {
        q: 'Where are my shortcuts saved?',
        a: 'In shortcuts.json, in %APPDATA%\\ShortcutMaster on Windows or ~/.config/ShortcutMaster on Linux and macOS. Back up that one file to keep them.',
      },
      {
        q: 'Why do I need a new terminal window after adding a shortcut?',
        a: 'A terminal loads its functions once, when it starts. Windows that are already open keep their old list. shortcut run <name> works everywhere immediately.',
      },
      {
        q: 'Can a shortcut change the folder I am in?',
        a: 'Yes. Add it with --type cd and a folder. The generated function moves the terminal itself, then runs any --then steps there.',
      },
      {
        q: 'Does it need administrator rights?',
        a: 'No. Everything is written to your own user profile and your own registry branch.',
      },
      {
        q: 'Is the online demo the real tool?',
        a: 'No — it is a simulation that runs in your browser. It mirrors the real messages and the files the tool writes, but it never touches your computer.',
      },
      {
        q: 'How do I remove everything?',
        a: 'Run shortcut uninstall, then dotnet tool uninstall --global ShortcutMaster. Delete the ShortcutMaster data folder if you also want your shortcuts gone.',
      },
    ],
  },
  footer: {
    tagline: 'One word. Any command.',
    body: 'ShortcutMaster is free, open-source software written in C# / .NET 8.',
    links: { github: 'GitHub', install: 'Install', faq: 'FAQ', readme: 'README' },
    langBtn: 'עברית',
  },
  fab: 'Try the demo',
};

export type Dict = typeof en;
