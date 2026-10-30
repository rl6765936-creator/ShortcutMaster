import { useEffect, useState } from 'react';
import { useReducedMotion } from '../hooks.ts';

// The same shortcut, typed in three different terminals.
const SCENES = [
  {
    shell: 'PowerShell',
    prompt: 'PS C:\\Users\\you> ',
    cmd: 'shortcut add gs "git status"',
    out: [{ c: 't-ok', t: "Shortcut 'gs' was added: git status" }],
  },
  {
    shell: 'PowerShell',
    prompt: 'PS C:\\Users\\you\\my-app> ',
    cmd: 'gs',
    out: [
      { c: '', t: 'On branch main' },
      { c: 't-dim', t: "Your branch is up to date with 'origin/main'." },
    ],
  },
  {
    shell: 'Command Prompt',
    prompt: 'C:\\Users\\you\\my-app>',
    cmd: 'gs --short',
    out: [{ c: '', t: ' M src/app.js' }],
  },
  {
    shell: 'Git Bash',
    prompt: 'you@pc ~/my-app $ ',
    cmd: 'gs',
    out: [
      { c: '', t: 'On branch main' },
      { c: 't-dim', t: 'nothing to commit, working tree clean' },
    ],
  },
];

export function HeroTerminal() {
  const reduced = useReducedMotion();
  const [scene, setScene] = useState(0);
  const [typed, setTyped] = useState(reduced ? SCENES[0]!.cmd.length : 0);
  const s = SCENES[scene]!;
  const done = typed >= s.cmd.length;

  useEffect(() => {
    if (reduced) return;
    const id = done
      ? window.setTimeout(() => {
          setScene((n) => (n + 1) % SCENES.length);
          setTyped(0);
        }, 2600)
      : window.setTimeout(() => setTyped((n) => n + 1), 55 + Math.random() * 45);
    return () => window.clearTimeout(id);
  }, [typed, done, reduced]);

  return (
    <div className="hero-term" dir="ltr" aria-hidden="true">
      <div className="term-chrome">
        <span className="dots">
          <i />
          <i />
          <i />
        </span>
        <span className="term-title">{s.shell}</span>
      </div>
      <pre className="hero-term__body">
        <span className="t-prompt">{s.prompt}</span>
        <span className="t-cmd">{s.cmd.slice(0, typed)}</span>
        {!done && <span className="t-caret" />}
        {done &&
          s.out.map((o, i) => (
            <span key={i} className={`hero-term__out ${o.c}`}>
              {'\n'}
              {o.t}
            </span>
          ))}
        {done && (
          <>
            {'\n'}
            <span className="t-prompt">{s.prompt}</span>
            <span className="t-caret" />
          </>
        )}
      </pre>
    </div>
  );
}
