import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  aliasFile,
  buildCommand,
  initialState,
  newWindow,
  prompt,
  resolveFolder,
  run,
  shortcutsJson,
  tokenize,
  type Shell,
  type State,
} from './engine.ts';

/** Runs several lines in a row and returns the final state and all output text. */
function session(lines: string[], shell: Shell = 'powershell', start: State = initialState()) {
  let state = start;
  const out: string[] = [];
  for (const line of lines) {
    const r = run(state, shell, line);
    state = r.state;
    out.push(...r.lines.map((l) => l.text));
  }
  return { state, text: out.join('\n') };
}

test('tokenize keeps quoted text together', () => {
  assert.deepEqual(tokenize('shortcut add gs "git status"', 'powershell'), ['shortcut', 'add', 'gs', 'git status']);
  assert.deepEqual(tokenize("shortcut add x 'a b'", 'bash'), ['shortcut', 'add', 'x', 'a b']);
  assert.deepEqual(tokenize('echo ""', 'cmd'), ['echo', '']);
});

test('add then list shows the shortcut like the real tool', () => {
  const { state, text } = session(['shortcut add gs "git status"', 'shortcut list']);
  assert.equal(state.shortcuts.length, 1);
  assert.match(text, /Shortcut 'gs' was added: git status/);
  assert.match(text, /Run it with: shortcut run gs/);
  assert.match(text, /gs {14}\[command\] git status/);
});

test('detects url, folder, app and command types', () => {
  const { state } = session([
    'shortcut add github "https://github.com"',
    'shortcut add docs "~/Documents"',
    'shortcut add np "C:\\Windows\\notepad.exe"',
    'shortcut add dev "npm run dev"',
  ]);
  assert.deepEqual(
    state.shortcuts.map((s) => s.actions[0]!.type),
    ['url', 'folder', 'app', 'command'],
  );
  assert.equal(state.shortcuts[1]!.actions[0]!.target, 'C:\\Users\\you\\Documents');
});

test('refuses names that are taken, reserved, invalid or duplicated', () => {
  const { text } = session([
    'shortcut add git "echo x"',
    'shortcut add list "echo x"',
    'shortcut add "my name" "echo x"',
    'shortcut add a "echo 1"',
    'shortcut add a "echo 2"',
  ]);
  assert.match(text, /'git' is already a command on this computer/);
  assert.match(text, /'list' is a reserved word/);
  assert.match(text, /Name may contain only English letters/);
  assert.match(text, /Shortcut 'a' already exists/);
  const forced = session(['shortcut add git "echo mine" --force']);
  assert.equal(forced.state.shortcuts.length, 1);
});

test('missing quotes produce the System.CommandLine error', () => {
  const { text, state } = session(['shortcut add gs git status']);
  assert.match(text, /Unrecognized command or argument 'status'/);
  assert.equal(state.shortcuts.length, 0);
});

test('invalid --type lists the allowed values', () => {
  const { text } = session(['shortcut add x --type foo "y"']);
  assert.match(text, /Argument 'foo' not recognized/);
  assert.match(text, /'cd'/);
});

test('an alias is unknown until installed and a new window is opened', () => {
  let { state, text } = session(['shortcut add hi "echo hello"', 'hi']);
  assert.match(text, /The term 'hi' is not recognized/);

  ({ state, text } = session(['shortcut install', 'hi'], 'powershell', state));
  assert.match(text, /Terminal integration installed in/);
  assert.match(text, /The term 'hi' is not recognized/);

  state = newWindow(state);
  ({ state, text } = session(['hi there'], 'powershell', state));
  assert.match(text, /hello there/);
});

test('each shell has its own "not found" message', () => {
  assert.match(session(['nope'], 'cmd').text, /'nope' is not recognized as an internal or external command/);
  assert.match(session(['nope'], 'bash').text, /bash: nope: command not found/);
});

test('a cd shortcut moves the terminal when typed as an alias, but only notes it via run', () => {
  let { state } = session(['shortcut add work --type cd "~/projects/my-app" --then "code ."', 'shortcut install']);
  state = newWindow(state);
  const viaRun = session(['shortcut run work'], 'powershell', state);
  assert.match(viaRun.text, /Note: to move your terminal into C:\\Users\\you\\projects\\my-app/);
  assert.deepEqual(viaRun.state.cwd, []);

  const viaAlias = session(['work'], 'powershell', state);
  assert.deepEqual(viaAlias.state.cwd, ['projects', 'my-app']);
  assert.match(viaAlias.text, /VS Code opens C:\\Users\\you\\projects\\my-app/);
  assert.equal(prompt('powershell', viaAlias.state.cwd), 'PS C:\\Users\\you\\projects\\my-app> ');
});

test('cd to a missing folder is refused when adding', () => {
  const { text, state } = session(['shortcut add x -t cd "~/nowhere"']);
  assert.match(text, /does not exist/);
  assert.equal(state.shortcuts.length, 0);
});

test('a chain stops at the first failing command and extra args go to the last action', () => {
  let { state } = session(['shortcut add chain "echo one" --then "nosuchtool" --then "echo three"', 'shortcut install']);
  state = newWindow(state);
  const { text } = session(['chain'], 'powershell', state);
  assert.match(text, /one/);
  assert.match(text, /'nosuchtool' is not recognized/);
  assert.doesNotMatch(text, /three/);

  const r = session(['shortcut add g "git status"', 'shortcut run g -- --short']);
  assert.match(r.text, /M src\/app\.js/);
});

test('update and remove', () => {
  const { state, text } = session([
    'shortcut add hi "echo hello"',
    'shortcut update hi "echo changed"',
    'shortcut run hi',
    'shortcut remove hi',
    'shortcut remove hi',
  ]);
  assert.match(text, /was updated: echo changed/);
  assert.match(text, /^changed$/m);
  assert.match(text, /Shortcut 'hi' was removed\./);
  assert.match(text, /Shortcut 'hi' was not found\./);
  assert.equal(state.shortcuts.length, 0);
});

test('generated files match the real tool', () => {
  const { state } = session(['shortcut add gs "git status"', 'shortcut add work -t cd "~/projects/my-app" --then "code ."']);
  assert.match(shortcutsJson(state), /"actions": \[\n {6}\{\n {8}"type": "command",\n {8}"target": "git"/);
  assert.match(aliasFile(state, 'powershell').text, /function global:gs \{ shortcut __run gs @args \}/);
  assert.match(
    aliasFile(state, 'powershell').text,
    /function global:work \{ Set-Location -LiteralPath 'C:\\Users\\you\\projects\\my-app'; shortcut __run work @args \}/,
  );
  assert.match(aliasFile(state, 'cmd').text, /^work=cd \/d "C:\\Users\\you\\projects\\my-app" \$T shortcut __run work \$\*$/m);
  assert.match(aliasFile(state, 'bash').text, /^work\(\) \{ cd '\/c\/Users\/you\/projects\/my-app' \|\| return; shortcut __run work "\$@"; \}$/m);
});

test('resolveFolder understands the forms people type', () => {
  assert.deepEqual(resolveFolder('~/projects/my-app', []), ['projects', 'my-app']);
  assert.deepEqual(resolveFolder('C:\\Users\\you\\Documents', []), ['Documents']);
  assert.deepEqual(resolveFolder('/c/Users/you/projects', []), ['projects']);
  assert.deepEqual(resolveFolder('..', ['projects']), []);
  assert.equal(resolveFolder('D:\\Other', []), null);
});

test('buildCommand writes a runnable add command', () => {
  const cmd = buildCommand({ name: 'start', action: 'git pull', type: 'auto', then: ['npm install', ''], force: false });
  assert.equal(cmd, 'shortcut add start "git pull" --then "npm install"');
  const cd = buildCommand({ name: 'work', action: '~/projects/my-app', type: 'cd', then: ['code .'], force: true });
  assert.equal(cd, 'shortcut add work --type cd "~/projects/my-app" --then "code ." --force');
  const { state } = session([cd]);
  assert.equal(state.shortcuts[0]!.actions[0]!.type, 'cd');
});

test('cls clears and cd moves around', () => {
  assert.equal(run(initialState(), 'cmd', 'cls').clear, true);
  const { state } = session(['cd ..', 'cd Documents'], 'bash');
  assert.deepEqual(state.cwd, ['Documents']);
  assert.equal(prompt('bash', state.cwd), 'you@pc ~/Documents $ ');
});
