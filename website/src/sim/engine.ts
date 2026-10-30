// A deterministic, in-browser simulation of ShortcutMaster on a Windows PC with three terminals.
// It mirrors the messages of the real tool, but never touches the visitor's computer.

export type Shell = 'powershell' | 'cmd' | 'bash';
export type ActionType = 'command' | 'app' | 'folder' | 'url' | 'cd';

export interface Action {
  type: ActionType;
  target: string;
  arguments: string[];
}

export interface Shortcut {
  name: string;
  actions: Action[];
  trusted: boolean;
}

export interface State {
  /** Saved shortcuts, in insertion order (like the real JSON file). */
  shortcuts: Shortcut[];
  /** True after `shortcut install`. */
  installed: boolean;
  /** Shortcut names that the current terminal window loaded when it opened. */
  loaded: string[];
  /** Current folder, as path segments under the user's home folder. */
  cwd: string[];
}

export type LineKind = 'cmd' | 'out' | 'ok' | 'err' | 'dim' | 'hint';

export interface Line {
  kind: LineKind;
  text: string;
}

export interface Result {
  state: State;
  lines: Line[];
  clear?: boolean;
}

export const TYPES: ActionType[] = ['command', 'app', 'folder', 'url', 'cd'];

const HOME_WIN = 'C:\\Users\\you';
const DATA_FILE = 'C:\\Users\\you\\AppData\\Roaming\\ShortcutMaster\\shortcuts.json';

/** Folders that exist in the simulated home folder. */
const FOLDERS = ['', 'projects', 'projects/my-app', 'projects/website', 'Documents', 'Desktop', 'Downloads'];

/** Program files that exist on the simulated PC. */
const FILES = ['C:\\Windows\\notepad.exe', 'C:\\Windows\\System32\\calc.exe', 'C:\\Windows\\System32\\mspaint.exe'];

/** Names that already run something in a terminal (programs on PATH and shell built-ins). */
const TAKEN = [
  'alias', 'call', 'cd', 'clear', 'cls', 'copy', 'del', 'dir', 'echo', 'exit', 'export', 'history', 'md',
  'mkdir', 'move', 'popd', 'pushd', 'pwd', 'rd', 'ren', 'set', 'source', 'start', 'type',
  'git', 'npm', 'node', 'npx', 'code', 'dotnet', 'python', 'pip', 'notepad', 'explorer', 'calc', 'winget',
  'where', 'ping', 'ls', 'cat', 'help',
];

const RESERVED = ['shortcut', 'add', 'update', 'remove', 'list', 'run', 'install', 'uninstall', 'help'];

export function initialState(): State {
  return { shortcuts: [], installed: false, loaded: [], cwd: ['projects'] };
}

/** Opens a fresh terminal window: it loads whatever shortcuts are installed right now, in the home folder. */
export function newWindow(state: State): State {
  return {
    ...state,
    loaded: state.installed ? state.shortcuts.map((s) => s.name) : [],
    cwd: [],
  };
}

// ---------------------------------------------------------------- prompts and paths

export function prompt(shell: Shell, cwd: string[]): string {
  if (shell === 'powershell') return `PS ${winPath(cwd)}> `;
  if (shell === 'cmd') return `${winPath(cwd)}>`;
  return `you@pc ${bashPath(cwd)} $ `;
}

export function winPath(segs: string[]): string {
  return segs.length === 0 ? HOME_WIN : HOME_WIN + '\\' + segs.join('\\');
}

function bashPath(segs: string[]): string {
  return segs.length === 0 ? '~' : '~/' + segs.join('/');
}

/** Converts C:\Users\you\x to the Git Bash form /c/Users/you/x, like AliasScriptWriter.ToBashPath. */
export function toBashPath(path: string): string {
  const forward = path.replace(/\\/g, '/');
  return forward.length >= 2 && forward[1] === ':' ? '/' + forward[0]!.toLowerCase() + forward.slice(2) : forward;
}

/** Resolves a folder typed by the user to home-relative segments, or null if it is not a known folder. */
export function resolveFolder(input: string, cwd: string[]): string[] | null {
  let text = input.trim().replace(/\\/g, '/').replace(/\/+$/, '');
  let base: string[];

  const lower = text.toLowerCase();
  if (text === '~' || text.startsWith('~/')) {
    base = [];
    text = text.slice(1);
  } else if (lower === 'c:/users/you' || lower.startsWith('c:/users/you/')) {
    base = [];
    text = text.slice('c:/users/you'.length);
  } else if (lower === '/c/users/you' || lower.startsWith('/c/users/you/')) {
    base = [];
    text = text.slice('/c/users/you'.length);
  } else if (/^([a-z]:|\/)/i.test(text)) {
    return null;
  } else {
    base = [...cwd];
  }

  for (const part of text.split('/')) {
    if (part === '' || part === '.') continue;
    if (part === '..') {
      if (base.length === 0) return null;
      base.pop();
      continue;
    }
    const next = [...base, part];
    const match = FOLDERS.find((f) => f.toLowerCase() === next.join('/').toLowerCase());
    if (match === undefined) return null;
    base = match === '' ? [] : match.split('/');
  }
  return base;
}

function knownFile(text: string): string | null {
  return FILES.find((f) => f.toLowerCase() === text.toLowerCase()) ?? null;
}

function isUrl(text: string): boolean {
  return /^https?:\/\/[^\s/$.?#].[^\s]*$/i.test(text);
}

// ---------------------------------------------------------------- tokenizing

/** Splits a terminal line into words, honouring double quotes (and single quotes outside CMD). */
export function tokenize(line: string, shell: Shell): string[] {
  const out: string[] = [];
  let cur = '';
  let quote: string | null = null;
  let has = false;
  for (const c of line) {
    if (quote) {
      if (c === quote) quote = null;
      else cur += c;
    } else if (c === '"' || (c === "'" && shell !== 'cmd')) {
      quote = c;
      has = true;
    } else if (/\s/.test(c)) {
      if (has || cur) out.push(cur);
      cur = '';
      has = false;
    } else {
      cur += c;
      has = true;
    }
  }
  if (has || cur) out.push(cur);
  return out;
}

/** Splits an action the way ShortcutService.SplitCommandLine does: spaces, with "double quotes" kept together. */
function splitAction(text: string): string[] {
  return tokenize(text, 'cmd');
}

// ---------------------------------------------------------------- building actions

function parseAction(text: string, forced: ActionType | null): Action {
  let trimmed = text.trim();
  if (trimmed === '~' || trimmed.startsWith('~/') || trimmed.startsWith('~\\')) trimmed = HOME_WIN + trimmed.slice(1);

  const wholeFolder = resolveFolder(trimmed, []);
  const wholeFile = knownFile(trimmed);
  const parts = isUrl(trimmed) || wholeFolder !== null || wholeFile !== null ? [trimmed] : splitAction(trimmed);

  if (parts.length === 0) throw new SimError('An action cannot be empty.');

  let target = parts[0]!;
  const type: ActionType = forced ?? detectType(target);

  if (type === 'folder' || type === 'cd') {
    const segs = resolveFolder(target, []);
    if (segs !== null) target = winPath(segs);
    else if (!/^[a-z]:/i.test(target)) target = HOME_WIN + '\\' + target.replace(/\//g, '\\');
    if (type === 'cd' && segs === null) throw new SimError(`Folder '${target}' does not exist.`);
  }
  if (type === 'cd' && parts.length > 1) throw new SimError('A cd action takes only a folder path. Put other steps in --then.');

  return { type, target, arguments: parts.slice(1) };
}

function detectType(target: string): ActionType {
  if (isUrl(target)) return 'url';
  if (resolveFolder(target, []) !== null) return 'folder';
  if (knownFile(target) !== null) return 'app';
  return 'command';
}

export function describeAction(a: Action): string {
  const text = a.arguments.length === 0 ? a.target : a.target + ' ' + a.arguments.join(' ');
  return a.type === 'cd' ? 'cd ' + text : text;
}

export function describe(s: Shortcut): string {
  return s.actions.map(describeAction).join('  ->  ');
}

class SimError extends Error {}

// ---------------------------------------------------------------- the main entry

/** Runs one line typed into the simulated terminal. */
export function run(state: State, shell: Shell, input: string): Result {
  const words = tokenize(input, shell);
  const echo: Line = { kind: 'cmd', text: prompt(shell, state.cwd) + input };
  if (words.length === 0) return { state, lines: [echo] };

  const [first, ...rest] = words as [string, ...string[]];
  const name = first.toLowerCase();

  try {
    if (name === 'cls' || name === 'clear') return { state, lines: [], clear: true };
    if (name === 'shortcut' || name === 'shortcut.exe') {
      const r = runShortcutCli(state, shell, rest);
      return { state: r.state, lines: [echo, ...r.lines] };
    }
    if (name === 'cd' || name === 'set-location' || name === 'chdir') {
      const r = changeDir(state, shell, rest.filter((w) => w !== '/d').join(' '));
      return { state: r.state, lines: [echo, ...r.lines] };
    }
    const alias = state.loaded.includes(first) ? state.shortcuts.find((s) => s.name.toLowerCase() === name) : undefined;
    if (alias) {
      const r = runViaAlias(state, shell, alias, rest);
      return { state: r.state, lines: [echo, ...r.lines] };
    }
    const prog = runProgram(shell, words, state.cwd);
    const lines = [echo, ...prog.lines];
    if (prog.notFound && state.shortcuts.some((s) => s.name.toLowerCase() === name)) {
      lines.push({ kind: 'hint', text: state.installed ? 'hint.newWindow' : 'hint.install' });
    }
    return { state, lines };
  } catch (e) {
    if (e instanceof SimError) return { state, lines: [echo, { kind: 'err', text: 'Error: ' + e.message }] };
    throw e;
  }
}

// ---------------------------------------------------------------- the `shortcut` command itself

function runShortcutCli(state: State, shell: Shell, args: string[]): Result {
  const sub = args[0]?.toLowerCase();
  const rest = args.slice(1);
  switch (sub) {
    case undefined:
    case '-h':
    case '--help':
    case '-?':
    case 'help':
      return { state, lines: helpLines() };
    case '--version':
      return { state, lines: [{ kind: 'out', text: '1.0.0' }] };
    case 'add':
    case 'update':
      return addOrUpdate(state, sub, rest);
    case 'remove':
      return remove(state, rest);
    case 'list':
      return list(state);
    case 'run':
      return runCommand(state, shell, rest);
    case 'install':
      return install(state);
    case 'uninstall':
      return uninstall(state);
    default:
      return { state, lines: unrecognized(args[0]!, 'shortcut [command] [options]') };
  }
}

function helpLines(): Line[] {
  const t = [
    'Description:',
    '  ShortcutMaster - personal shortcuts for commands, apps, folders and websites.',
    '',
    'Usage:',
    '  shortcut [command] [options]',
    '',
    'Commands:',
    '  add <name> <action>     Create a new shortcut',
    '  update <name> <action>  Replace what an existing shortcut does',
    '  remove <name>           Delete a shortcut',
    '  list                    Show all shortcuts',
    '  run <name> <arguments>  Run a saved shortcut',
    '  install                 Let PowerShell, CMD and Bash/Zsh run shortcuts by their name alone',
    '  uninstall               Remove the terminal integration (your shortcuts are kept)',
  ];
  return t.map((text) => ({ kind: 'out', text }));
}

function unrecognized(token: string, usage: string): Line[] {
  return [
    { kind: 'err', text: `Unrecognized command or argument '${token}'.` },
    { kind: 'dim', text: `Usage: ${usage}` },
  ];
}

interface AddArgs {
  name: string;
  action: string;
  then: string[];
  type: ActionType | null;
  force: boolean;
}

function parseAddArgs(cmd: 'add' | 'update', args: string[]): AddArgs | Line[] {
  const positional: string[] = [];
  const then: string[] = [];
  let type: ActionType | null = null;
  let force = false;
  for (let i = 0; i < args.length; i++) {
    const a = args[i]!;
    const lower = a.toLowerCase();
    if (lower === '--then') {
      const v = args[++i];
      if (v === undefined) return [{ kind: 'err', text: "Required argument missing for option: '--then'." }];
      then.push(v);
    } else if (lower === '--type' || lower === '-t') {
      const v = args[++i];
      if (v === undefined) return [{ kind: 'err', text: `Required argument missing for option: '${a}'.` }];
      const t = TYPES.find((x) => x === v.toLowerCase());
      if (!t) {
        return [
          { kind: 'err', text: `Argument '${v}' not recognized. Must be one of:` },
          ...TYPES.map((x) => ({ kind: 'err' as const, text: `\t'${x}'` })),
        ];
      }
      type = t;
    } else if (lower === '--force' && cmd === 'add') {
      force = true;
    } else {
      positional.push(a);
    }
  }
  const usage = `Usage: shortcut ${cmd} <name> <action> [options]`;
  if (positional.length < 2) {
    return [
      { kind: 'err', text: `Required argument missing for command: '${cmd}'.` },
      { kind: 'dim', text: usage },
    ];
  }
  if (positional.length > 2) {
    return [
      { kind: 'err', text: `Unrecognized command or argument '${positional[2]}'.` },
      { kind: 'dim', text: usage },
      { kind: 'hint', text: 'hint.quotes' },
    ];
  }
  return { name: positional[0]!, action: positional[1]!, then, type, force };
}

function addOrUpdate(state: State, cmd: 'add' | 'update', args: string[]): Result {
  const parsed = parseAddArgs(cmd, args);
  if (Array.isArray(parsed)) return { state, lines: parsed };
  const { name, action, then, type, force } = parsed;

  const existing = state.shortcuts.find((s) => s.name.toLowerCase() === name.toLowerCase());

  if (cmd === 'add') {
    if (!/^[A-Za-z0-9_-]+$/.test(name)) throw new SimError("Name may contain only English letters, digits, '-' and '_'.");
    if (RESERVED.includes(name.toLowerCase())) throw new SimError(`'${name}' is a reserved word and cannot be used as a shortcut name.`);
    if (existing) throw new SimError(`Shortcut '${name}' already exists. Use 'shortcut update' to change it.`);
    if (!force && TAKEN.includes(name.toLowerCase())) {
      throw new SimError(`'${name}' is already a command on this computer. Choose another name, or add --force to override it.`);
    }
  } else if (!existing) {
    throw new SimError(`Shortcut '${name}' was not found. See all shortcuts with: shortcut list`);
  }

  const actions = [parseAction(action, type), ...then.map((t) => parseAction(t, null))];
  const shortcut: Shortcut = { name: existing?.name ?? name, actions, trusted: true };
  const shortcuts = existing
    ? state.shortcuts.map((s) => (s === existing ? shortcut : s))
    : [...state.shortcuts, shortcut];

  const verb = cmd === 'add' ? 'added' : 'updated';
  return {
    state: { ...state, shortcuts },
    lines: [{ kind: 'ok', text: `Shortcut '${shortcut.name}' was ${verb}: ${describe(shortcut)}` }, howToUse(state, shortcut.name)],
  };
}

function howToUse(state: State, name: string): Line {
  return state.installed
    ? { kind: 'out', text: `Open a new terminal window and type: ${name}` }
    : { kind: 'out', text: `Run it with: shortcut run ${name}   (run 'shortcut install' once to type just '${name}')` };
}

function remove(state: State, args: string[]): Result {
  if (args.length < 1) {
    return { state, lines: [{ kind: 'err', text: "Required argument missing for command: 'remove'." }] };
  }
  const name = args[0]!;
  const existing = state.shortcuts.find((s) => s.name.toLowerCase() === name.toLowerCase());
  if (!existing) throw new SimError(`Shortcut '${name}' was not found.`);
  return {
    state: { ...state, shortcuts: state.shortcuts.filter((s) => s !== existing) },
    lines: [{ kind: 'ok', text: `Shortcut '${name}' was removed.` }],
  };
}

function list(state: State): Result {
  if (state.shortcuts.length === 0) {
    return { state, lines: [{ kind: 'out', text: 'No shortcuts yet. Add one with: shortcut add <name> "<action>"' }] };
  }
  const sorted = [...state.shortcuts].sort((a, b) => a.name.toLowerCase().localeCompare(b.name.toLowerCase()));
  const lines: Line[] = sorted.map((s) => ({
    kind: 'out',
    text: `${s.name.padEnd(15)} [${s.actions.map((a) => a.type).join(',')}] ${describe(s)}`,
  }));
  lines.push({ kind: 'out', text: '' }, { kind: 'dim', text: `Saved in: ${DATA_FILE}` });
  return { state, lines };
}

function install(state: State): Result {
  const lines: Line[] = [
    'Terminal integration installed in:',
    '  PowerShell  C:\\Users\\you\\Documents\\WindowsPowerShell\\profile.ps1',
    '  PowerShell  C:\\Users\\you\\Documents\\PowerShell\\profile.ps1',
    '  Bash/Zsh    C:\\Users\\you\\.bashrc',
    '  CMD         HKEY_CURRENT_USER\\Software\\Microsoft\\Command Processor -> AutoRun',
    '',
    'Open a new terminal window, then type any shortcut name directly.',
    'If PowerShell says that running scripts is disabled, run this once in PowerShell:',
    '  Set-ExecutionPolicy -Scope CurrentUser RemoteSigned',
  ].map((text) => ({ kind: 'out', text }));
  lines.push({ kind: 'hint', text: 'hint.newWindow' });
  return { state: { ...state, installed: true }, lines };
}

function uninstall(state: State): Result {
  if (!state.installed) return { state, lines: [{ kind: 'out', text: 'The terminal integration was not installed.' }] };
  const lines: Line[] = [
    'Terminal integration removed from:',
    '  C:\\Users\\you\\Documents\\WindowsPowerShell\\profile.ps1',
    '  C:\\Users\\you\\Documents\\PowerShell\\profile.ps1',
    '  C:\\Users\\you\\.bashrc',
    '  HKEY_CURRENT_USER\\Software\\Microsoft\\Command Processor -> AutoRun',
    `Your shortcuts are still saved in: ${DATA_FILE}`,
  ].map((text) => ({ kind: 'out', text }));
  return { state: { ...state, installed: false }, lines };
}

function runCommand(state: State, shell: Shell, args: string[]): Result {
  if (args.length < 1) return { state, lines: [{ kind: 'err', text: "Required argument missing for command: 'run'." }] };
  const name = args[0]!;
  const extra = args.slice(1).filter((a, i) => !(i === 0 && a === '--'));
  const shortcut = state.shortcuts.find((s) => s.name.toLowerCase() === name.toLowerCase());
  if (!shortcut) throw new SimError(`Shortcut '${name}' was not found. See all shortcuts with: shortcut list`);
  return { state, lines: runActions(shell, shortcut, extra, false, state.cwd) };
}

// ---------------------------------------------------------------- running shortcuts

/** What the generated alias does: a leading cd moves the terminal itself, the rest goes to `shortcut __run`. */
function runViaAlias(state: State, shell: Shell, shortcut: Shortcut, extra: string[]): Result {
  let next = state;
  const first = shortcut.actions[0];
  if (first?.type === 'cd') {
    const segs = resolveFolder(first.target, []);
    if (segs === null) return { state, lines: [{ kind: 'err', text: `cd: ${first.target}: No such file or directory` }] };
    next = { ...state, cwd: segs };
  }
  if (!shortcut.actions.some((a) => a.type !== 'cd')) return { state: next, lines: [] };
  return { state: next, lines: runActions(shell, shortcut, extra, true, next.cwd) };
}

/** Mirrors ShortcutRunner.RunActions: in order, stop at the first failing command, extra args go to the last one. */
function runActions(shell: Shell, s: Shortcut, extra: string[], fromAlias: boolean, cwd: string[]): Line[] {
  const lines: Line[] = [];
  let folder = cwd;
  const last = s.actions.length - 1;
  for (let i = 0; i <= last; i++) {
    const a = s.actions[i]!;
    const args = i === last ? extra : [];
    if (a.type === 'cd') {
      folder = resolveFolder(a.target, []) ?? folder;
      if (!fromAlias) lines.push({ kind: 'out', text: `Note: to move your terminal into ${a.target}, type '${s.name}' directly.` });
      continue;
    }
    if (a.type === 'url') {
      lines.push({ kind: 'dim', text: `[opens ${[a.target, ...a.arguments, ...args].join(' ')} in your default browser]` });
      continue;
    }
    if (a.type === 'folder') {
      lines.push({ kind: 'dim', text: `[opens ${a.target} in File Explorer]` });
      continue;
    }
    if (a.type === 'app') {
      lines.push({ kind: 'dim', text: `[starts ${a.target}]` });
      continue;
    }
    const r = runProgram('cmd', [a.target, ...a.arguments, ...args], folder, shell);
    lines.push(...r.lines);
    if (r.exitCode !== 0) break;
  }
  return lines;
}

interface ProgramResult {
  lines: Line[];
  exitCode: number;
  notFound?: boolean;
}

/** Simulated programs. Commands from a shortcut run through cmd /c (as on real Windows). */
function runProgram(shell: Shell, words: string[], cwd: string[], displayShell: Shell = shell): ProgramResult {
  const [cmd = '', ...args] = words;
  const c = cmd.toLowerCase().replace(/\.exe$/, '');
  const a = args.join(' ');
  const out = (...t: string[]): ProgramResult => ({ lines: t.map((text) => ({ kind: 'out', text })), exitCode: 0 });
  const here = winPath(cwd);

  switch (c) {
    case 'echo':
    case 'write-output':
      return out(a);
    case 'pwd':
    case 'get-location':
      return out(displayShell === 'bash' ? toBashPath(here) : here);
    case 'ls':
    case 'dir': {
      const prefix = cwd.join('/');
      const kids = FOLDERS.filter((f) => f !== prefix && f.startsWith(prefix ? prefix + '/' : '') && !f.slice(prefix ? prefix.length + 1 : 0).includes('/'));
      const names = kids.map((k) => k.split('/').pop()!).concat(cwd.length === 2 ? ['package.json', 'README.md', 'src'] : []);
      return out(names.length ? names.join('   ') : '');
    }
    case 'git': {
      const sub = args[0];
      if (sub === 'status' && (args.includes('--short') || args.includes('-s'))) return out(' M src/app.js', '?? notes.txt');
      if (sub === 'status') {
        return out("On branch main", "Your branch is up to date with 'origin/main'.", '', 'Changes not staged for commit:', '        modified:   src/app.js', '');
      }
      if (sub === 'pull') return out('Already up to date.');
      if (sub === 'log') return out('a3f9c21 (HEAD -> main) Add login page', '7be04d8 Fix header on mobile', '19c55e2 First commit');
      if (sub === 'push') return out('Everything up-to-date');
      if (sub === '--version') return out('git version 2.47.1.windows.1');
      return out(`[git ${a}]`);
    }
    case 'npm': {
      if (args[0] === 'install' || args[0] === 'i') return out('', 'up to date, audited 214 packages in 2s', '', 'found 0 vulnerabilities');
      if (args[0] === 'test') return out('', '> my-app@1.0.0 test', '> vitest run', '', ' ✓ 12 tests passed');
      if (args[0] === 'run' && args[1]) {
        return {
          lines: [
            { kind: 'out', text: '' },
            { kind: 'out', text: `> my-app@1.0.0 ${args[1]}` },
            { kind: 'out', text: '> vite' },
            { kind: 'out', text: '' },
            { kind: 'ok', text: '  VITE ready in 318 ms' },
            { kind: 'out', text: '  ➜  Local:   http://localhost:5173/' },
            { kind: 'dim', text: '[keeps running until you press Ctrl+C]' },
          ],
          exitCode: 0,
        };
      }
      return out(`[npm ${a}]`);
    }
    case 'code':
      return { lines: [{ kind: 'dim', text: `[VS Code opens ${args[0] === '.' || args.length === 0 ? here : args.join(' ')}]` }], exitCode: 0 };
    case 'dotnet':
      return args[0] === '--version' ? out('8.0.404') : out(`[dotnet ${a}]`);
    case 'node':
      return args[0] === '--version' || args[0] === '-v' ? out('v22.12.0') : out(`[node ${a}]`);
    case 'notepad':
    case 'calc':
    case 'explorer':
      return { lines: [{ kind: 'dim', text: `[starts ${cmd}]` }], exitCode: 0 };
    case 'exit':
      return { lines: [{ kind: 'dim', text: '[demo: use “New window” instead]' }], exitCode: 0 };
    default:
      return { lines: notFound(shell, cmd), exitCode: 1, notFound: true };
  }
}

function notFound(shell: Shell, cmd: string): Line[] {
  if (shell === 'powershell') {
    return [
      { kind: 'err', text: `${cmd} : The term '${cmd}' is not recognized as the name of a cmdlet, function, script file, or` },
      { kind: 'err', text: 'operable program. Check the spelling of the name, or if a path was included, verify that the path is correct.' },
    ];
  }
  if (shell === 'cmd') {
    return [
      { kind: 'err', text: `'${cmd}' is not recognized as an internal or external command,` },
      { kind: 'err', text: 'operable program or batch file.' },
    ];
  }
  return [{ kind: 'err', text: `bash: ${cmd}: command not found` }];
}

function changeDir(state: State, shell: Shell, target: string): Result {
  if (target.trim() === '') {
    if (shell === 'cmd') return { state, lines: [{ kind: 'out', text: winPath(state.cwd) }] };
    return { state: { ...state, cwd: [] }, lines: [] };
  }
  const segs = resolveFolder(target, state.cwd);
  if (segs === null) {
    const msg =
      shell === 'bash'
        ? `bash: cd: ${target}: No such file or directory`
        : shell === 'cmd'
          ? 'The system cannot find the path specified.'
          : `Set-Location : Cannot find path '${target}' because it does not exist.`;
    return { state, lines: [{ kind: 'err', text: msg }] };
  }
  return { state: { ...state, cwd: segs }, lines: [] };
}

// ---------------------------------------------------------------- the files the real tool writes

/** shortcuts.json exactly as System.Text.Json writes it (camelCase, indented). */
export function shortcutsJson(state: State): string {
  const obj: Record<string, Shortcut> = {};
  for (const s of state.shortcuts) obj[s.name] = s;
  return JSON.stringify(obj, null, 2);
}

function startFolder(s: Shortcut): string | null {
  const f = s.actions[0];
  return f && f.type === 'cd' ? f.target : null;
}

function hasProgram(s: Shortcut): boolean {
  return s.actions.some((a) => a.type !== 'cd');
}

const HEADER = "Generated by ShortcutMaster. Do not edit by hand - use 'shortcut add/update/remove'.";

/** The alias file each terminal loads, as AliasScriptWriter generates it. */
export function aliasFile(state: State, shell: Shell): { path: string; text: string } {
  if (shell === 'powershell') {
    const lines = ['# ' + HEADER];
    for (const s of state.shortcuts) {
      const steps: string[] = [];
      const f = startFolder(s);
      if (f) steps.push(`Set-Location -LiteralPath '${f.replace(/'/g, "''")}'`);
      if (hasProgram(s)) steps.push(`shortcut __run ${s.name} @args`);
      lines.push(`Remove-Item -Path Alias:${s.name} -Force -ErrorAction SilentlyContinue`);
      lines.push(`function global:${s.name} { ${steps.join('; ')} }`);
    }
    return { path: 'C:\\Users\\you\\AppData\\Roaming\\ShortcutMaster\\aliases.ps1', text: lines.join('\n') };
  }
  if (shell === 'cmd') {
    const lines: string[] = [];
    for (const s of state.shortcuts) {
      const steps: string[] = [];
      const f = startFolder(s);
      if (f) steps.push(`cd /d "${f.replace(/\$/g, '$$$$')}"`);
      if (hasProgram(s)) steps.push(`shortcut __run ${s.name} $*`);
      if (steps.length === 0) steps.push('rem');
      lines.push(`${s.name}=${steps.join(' $T ')}`);
    }
    return { path: 'C:\\Users\\you\\AppData\\Roaming\\ShortcutMaster\\aliases.doskey', text: lines.join('\n') };
  }
  const lines = ['# ' + HEADER];
  for (const s of state.shortcuts) {
    const steps: string[] = [];
    const f = startFolder(s);
    if (f) steps.push(`cd '${toBashPath(f).replace(/'/g, "'\\''")}' || return`);
    if (hasProgram(s)) steps.push(`shortcut __run ${s.name} "$@"`);
    if (steps.length === 0) steps.push(':');
    lines.push(`unalias ${s.name} 2>/dev/null`);
    lines.push(`${s.name}() { ${steps.join('; ')}; }`);
  }
  return { path: 'C:\\Users\\you\\AppData\\Roaming\\ShortcutMaster\\aliases.sh', text: lines.join('\n') };
}

// ---------------------------------------------------------------- the command builder

export interface Draft {
  name: string;
  action: string;
  type: ActionType | 'auto';
  then: string[];
  force: boolean;
}

/** Quotes a value for the chosen terminal, if it needs quotes. */
function q(value: string): string {
  return /^[A-Za-z0-9_\-./:\\~]+$/.test(value) && value !== '' ? value : `"${value.replace(/"/g, '\\"')}"`;
}

/** Builds the `shortcut add` line for the options chosen in the builder. */
export function buildCommand(d: Draft): string {
  const parts = ['shortcut', 'add', d.name.trim() || '<name>'];
  if (d.type !== 'auto') parts.push('--type', d.type);
  parts.push(`"${d.action.trim() || '<action>'}"`);
  for (const t of d.then) if (t.trim()) parts.push('--then', `"${t.trim()}"`);
  if (d.force) parts.push('--force');
  return parts.map((p, i) => (i < 3 || p.startsWith('"') || p.startsWith('-') ? p : q(p))).join(' ');
}
