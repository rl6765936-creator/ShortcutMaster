import { useEffect, useMemo, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { useI18n } from '../i18n/index.tsx';
import { useReveal } from '../hooks.ts';
import {
  TYPES,
  aliasFile,
  buildCommand,
  initialState,
  newWindow,
  prompt,
  run,
  shortcutsJson,
  type ActionType,
  type Draft,
  type Line,
  type Shell,
  type State,
} from '../sim/engine.ts';
import { ParticleField } from './ParticleField.tsx';
import { CopyButton } from './CopyButton.tsx';
import { Icon } from './Icon.tsx';

const SHELLS: { id: Shell; label: string }[] = [
  { id: 'powershell', label: 'PowerShell' },
  { id: 'cmd', label: 'CMD' },
  { id: 'bash', label: 'Git Bash' },
];

const PRESETS: Record<'git' | 'web' | 'work' | 'chain', Draft> = {
  git: { name: 'gs', action: 'git status', type: 'auto', then: [], force: false },
  web: { name: 'github', action: 'https://github.com', type: 'auto', then: [], force: false },
  work: { name: 'work', action: '~/projects/my-app', type: 'cd', then: ['code .'], force: false },
  chain: { name: 'start', action: 'git pull', type: 'auto', then: ['npm install', 'npm run dev'], force: false },
};

type Tab = 'terminal' | 'json' | 'alias';

export function Demo() {
  const { t } = useI18n();
  const d = t.demo;
  const head = useReveal<HTMLDivElement>();

  const [state, setState] = useState<State>(initialState);
  const [shell, setShell] = useState<Shell>('powershell');
  const [lines, setLines] = useState<Line[]>([]);
  const [tab, setTab] = useState<Tab>('terminal');
  const [draft, setDraft] = useState<Draft>(PRESETS.git);
  const [preset, setPreset] = useState<keyof typeof PRESETS | null>('git');
  const [input, setInput] = useState('');
  const [history, setHistory] = useState<string[]>([]);
  const [histPos, setHistPos] = useState(-1);
  const [usedAlias, setUsedAlias] = useState(false);

  const screenRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const command = useMemo(() => buildCommand(draft), [draft]);
  const draftReady = draft.name.trim() !== '' && draft.action.trim() !== '';

  useEffect(() => {
    const el = screenRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [lines, tab]);

  /** Progress through the four steps that make an alias work. */
  const progress = [state.shortcuts.length > 0, state.installed, state.loaded.length > 0, usedAlias];

  function exec(text: string) {
    const first = text.trim().split(/\s+/)[0] ?? '';
    if (state.loaded.some((n) => n.toLowerCase() === first.toLowerCase())) setUsedAlias(true);
    const r = run(state, shell, text);
    setState(r.state);
    setLines((prev) => (r.clear ? [] : [...prev, ...r.lines]));
    if (text.trim()) {
      setHistory((h) => [...h.filter((x) => x !== text), text]);
    }
    setHistPos(-1);
    setTab('terminal');
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    exec(input);
    setInput('');
  }

  function onKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'ArrowUp' && history.length) {
      e.preventDefault();
      const pos = histPos === -1 ? history.length - 1 : Math.max(0, histPos - 1);
      setHistPos(pos);
      setInput(history[pos]!);
    } else if (e.key === 'ArrowDown' && histPos !== -1) {
      e.preventDefault();
      const pos = histPos + 1;
      if (pos >= history.length) {
        setHistPos(-1);
        setInput('');
      } else {
        setHistPos(pos);
        setInput(history[pos]!);
      }
    } else if (e.key === 'l' && e.ctrlKey) {
      e.preventDefault();
      setLines([]);
    }
  }

  function openWindow() {
    setState((s) => newWindow(s));
    setLines([]);
    setTab('terminal');
    inputRef.current?.focus();
  }

  function reset() {
    setState(initialState());
    setLines([]);
    setHistory([]);
    setInput('');
    setUsedAlias(false);
    setTab('terminal');
  }

  function choosePreset(key: keyof typeof PRESETS) {
    setPreset(key);
    setDraft(PRESETS[key]);
  }

  function editDraft(patch: Partial<Draft>) {
    setPreset(null);
    setDraft((x) => ({ ...x, ...patch }));
  }

  function suggestNext(): string | null {
    const last = state.shortcuts[state.shortcuts.length - 1];
    if (!last) return null;
    if (!state.installed) return 'shortcut install';
    if (state.loaded.includes(last.name)) return last.name;
    return null;
  }

  const next = suggestNext();
  const file = aliasFile(state, shell);

  return (
    <section className="section section--dark section--demo" id="demo">
      <ParticleField density={0.00007} />
      <div className="container">
        <div ref={head} className="section-head reveal">
          <span className="badge">
            <Icon name="play" size={11} /> {d.badge}
          </span>
          <h2>{d.title}</h2>
          <p>{d.lead}</p>
        </div>

        <ol className="demo-steps" aria-label="Progress">
          {d.steps.map((s, i) => (
            <li key={s} className={progress[i] ? 'is-done' : ''}>
              <span className="demo-steps__dot">{progress[i] ? <Icon name="check" size={12} /> : i + 1}</span>
              {s}
            </li>
          ))}
        </ol>

        <div className="demo-bar">
          <div className="seg" role="radiogroup" aria-label={d.shell}>
            {SHELLS.map((s) => (
              <button
                key={s.id}
                type="button"
                role="radio"
                aria-checked={shell === s.id}
                className={shell === s.id ? 'is-on' : ''}
                onClick={() => setShell(s.id)}
              >
                {s.label}
              </button>
            ))}
          </div>
          <div className="presets">
            <span className="presets__label">{d.presetsLabel}</span>
            {(Object.keys(PRESETS) as (keyof typeof PRESETS)[]).map((k) => (
              <button key={k} type="button" className={`preset ${preset === k ? 'is-on' : ''}`} onClick={() => choosePreset(k)}>
                {d.presets[k]}
              </button>
            ))}
          </div>
        </div>

        <div className="demo-grid">
          <form className="controls" onSubmit={(e) => e.preventDefault()}>
            <div className="field">
              <div className="field__top">
                <label htmlFor="d-name">{d.builder.name}</label>
                <code className="flag">&lt;name&gt;</code>
              </div>
              <p className="field__hint">{d.builder.nameHint}</p>
              <input
                id="d-name"
                className="input"
                dir="ltr"
                spellCheck={false}
                autoComplete="off"
                value={draft.name}
                onChange={(e) => editDraft({ name: e.target.value })}
              />
            </div>

            <div className="field">
              <div className="field__top">
                <label htmlFor="d-action">{d.builder.action}</label>
                <code className="flag">"&lt;action&gt;"</code>
              </div>
              <p className="field__hint">{d.builder.actionHint}</p>
              <input
                id="d-action"
                className="input"
                dir="ltr"
                spellCheck={false}
                autoComplete="off"
                value={draft.action}
                onChange={(e) => editDraft({ action: e.target.value })}
              />
            </div>

            <div className="field">
              <div className="field__top">
                <span className="field__label" id="d-type">
                  {d.builder.type}
                </span>
                <code className="flag">--type</code>
              </div>
              <p className="field__hint">{d.builder.typeHint}</p>
              <div className="chips" role="radiogroup" aria-labelledby="d-type" dir="ltr">
                {(['auto', ...TYPES] as (ActionType | 'auto')[]).map((ty) => (
                  <button
                    key={ty}
                    type="button"
                    role="radio"
                    aria-checked={draft.type === ty}
                    className={`ltoggle ${draft.type === ty ? 'is-on' : ''}`}
                    onClick={() => editDraft({ type: ty })}
                  >
                    {ty === 'auto' ? d.builder.auto : ty}
                  </button>
                ))}
              </div>
            </div>

            <div className="field">
              <div className="field__top">
                <span className="field__label">{d.builder.then}</span>
                <code className="flag">--then</code>
              </div>
              <p className="field__hint">{d.builder.thenHint}</p>
              {draft.then.map((step, i) => (
                <div className="then-row" key={i}>
                  <input
                    className="input"
                    dir="ltr"
                    spellCheck={false}
                    autoComplete="off"
                    aria-label={`${d.builder.then} ${i + 1}`}
                    value={step}
                    onChange={(e) => editDraft({ then: draft.then.map((x, j) => (j === i ? e.target.value : x)) })}
                  />
                  <button
                    type="button"
                    className="icon-btn"
                    aria-label={d.builder.removeStep}
                    onClick={() => editDraft({ then: draft.then.filter((_, j) => j !== i) })}
                  >
                    <Icon name="minus" size={16} />
                  </button>
                </div>
              ))}
              {draft.then.length < 4 && (
                <button type="button" className="add-step" onClick={() => editDraft({ then: [...draft.then, ''] })}>
                  <Icon name="plus" size={15} /> {d.builder.addStep}
                </button>
              )}
            </div>

            <label className="switch">
              <input type="checkbox" checked={draft.force} onChange={(e) => editDraft({ force: e.target.checked })} />
              <span className="switch__track" aria-hidden="true" />
              <span>
                {d.builder.force} <code className="flag">--force</code>
              </span>
            </label>
          </form>

          <div className="console-col">
            <div className="cmd-bar" dir="ltr">
              <span className="cmd-bar__label">{d.command}</span>
              <code className="cmd-bar__code">{command}</code>
              <CopyButton text={command} />
              <button type="button" className="btn btn--run btn--sm" disabled={!draftReady} onClick={() => exec(command)}>
                <Icon name="play" size={12} /> {d.add}
              </button>
            </div>

            <div className="console">
              <div className="console__chrome">
                <span className="dots" aria-hidden="true">
                  <i />
                  <i />
                  <i />
                </span>
                <div className="tabs" role="tablist">
                  {(['terminal', 'json', 'alias'] as Tab[]).map((k) => (
                    <button
                      key={k}
                      type="button"
                      role="tab"
                      aria-selected={tab === k}
                      className={tab === k ? 'is-on' : ''}
                      onClick={() => setTab(k)}
                    >
                      {k === 'alias' ? file.path.split('\\').pop() : d.tabs[k]}
                    </button>
                  ))}
                </div>
                <div className="console__tools">
                  <button type="button" className="tool-btn" onClick={openWindow}>
                    <Icon name="window" size={15} /> <span>{d.newWindow}</span>
                  </button>
                  <button type="button" className="tool-btn" onClick={reset} aria-label={d.reset} title={d.reset}>
                    <Icon name="reset" size={15} />
                  </button>
                </div>
              </div>

              <div className="console__screen" ref={screenRef} dir="ltr" onClick={() => tab === 'terminal' && inputRef.current?.focus()}>
                {tab === 'terminal' && (
                  <>
                    {lines.length === 0 && <p className="tl tl--welcome">{d.welcome}</p>}
                    {lines.map((l, i) =>
                      l.kind === 'hint' ? (
                        <p key={i} className="tl tl--hint" dir="auto">
                          <Icon name="arrow" size={13} /> {d.hints[l.text] ?? l.text}
                        </p>
                      ) : (
                        <pre key={i} className={`tl tl--${l.kind}`}>
                          {l.text || ' '}
                        </pre>
                      ),
                    )}
                    <form className="console__input" onSubmit={onSubmit}>
                      <label htmlFor="d-input" className="t-prompt">
                        {prompt(shell, state.cwd)}
                      </label>
                      <input
                        id="d-input"
                        ref={inputRef}
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        onKeyDown={onKeyDown}
                        spellCheck={false}
                        autoComplete="off"
                        autoCapitalize="off"
                        aria-label={d.inputLabel}
                        placeholder={lines.length === 0 ? d.placeholder : ''}
                      />
                    </form>
                  </>
                )}
                {tab === 'json' && (
                  <>
                    <p className="file-path">C:\Users\you\AppData\Roaming\ShortcutMaster\shortcuts.json</p>
                    <pre className="file-view">{state.shortcuts.length ? shortcutsJson(state) : d.emptyJson}</pre>
                  </>
                )}
                {tab === 'alias' && (
                  <>
                    <p className="file-path">{file.path}</p>
                    <pre className="file-view">{file.text || ' '}</pre>
                  </>
                )}
              </div>

              {next && tab === 'terminal' && (
                <div className="console__suggest" dir="ltr">
                  <button type="button" className="suggest-btn" onClick={() => exec(next)}>
                    <Icon name="play" size={11} /> {next}
                  </button>
                </div>
              )}
            </div>
            <p className="demo-foot">{d.footnote}</p>
          </div>
        </div>
      </div>
    </section>
  );
}
