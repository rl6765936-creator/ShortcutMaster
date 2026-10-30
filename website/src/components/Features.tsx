import type { ReactNode } from 'react';
import { useI18n } from '../i18n/index.tsx';
import { useReveal } from '../hooks.ts';
import { Icon, type IconName } from './Icon.tsx';

function Tile({ icon, title, body, flag, className = '', children }: {
  icon: IconName;
  title: string;
  body: string;
  flag?: string;
  className?: string;
  children?: ReactNode;
}) {
  const ref = useReveal<HTMLElement>();
  return (
    <article ref={ref} className={`tile reveal ${className}`}>
      <div className="tile__head">
        <span className="tile__icon">
          <Icon name={icon} size={20} />
        </span>
        {flag && <code className="flag">{flag}</code>}
      </div>
      <h3>{title}</h3>
      <p>{body}</p>
      {children && <div className="tile__visual">{children}</div>}
    </article>
  );
}

export function Features() {
  const { t } = useI18n();
  const f = t.features;
  const head = useReveal<HTMLDivElement>();
  const notes = useReveal<HTMLDivElement>();

  const types = [
    { key: 'command', ex: 'gs → git status', c: '#8b6cff' },
    { key: 'app', ex: 'np → notepad.exe', c: '#f6a35c' },
    { key: 'folder', ex: 'docs → Documents', c: '#9be37c' },
    { key: 'url', ex: 'github → github.com', c: '#3be3d3' },
    { key: 'cd', ex: 'work → C:\\Projects\\MyApp', c: '#ff8fb1' },
  ] as const;

  return (
    <section className="section section--light" id="features">
      <div className="container">
        <div ref={head} className="section-head reveal">
          <h2>{f.title}</h2>
          <p>{f.lead}</p>
        </div>

        <div className="tiles">
          <Tile icon="link" title={f.anything.title} body={f.anything.body} flag="--type" className="tile--wide">
            <ul className="type-list" dir="ltr">
              {types.map((x) => (
                <li key={x.key} style={{ ['--c' as string]: x.c }}>
                  <span className="chip">
                    <i />
                    {f.anything.types[x.key]}
                  </span>
                  <code>{x.ex}</code>
                </li>
              ))}
            </ul>
          </Tile>

          <Tile icon="terminal" title={f.shells.title} body={f.shells.body} flag="install">
            <div className="shell-badges" dir="ltr">
              {['PowerShell', 'CMD', 'Git Bash', 'Bash', 'Zsh', 'VS Code'].map((s) => (
                <span key={s} className="shell-badge">
                  {s}
                </span>
              ))}
            </div>
          </Tile>

          <Tile icon="chain" title={f.chain.title} body={f.chain.body} flag="--then">
            <ol className="mini-flow" dir="ltr">
              <li>git pull</li>
              <li>npm install</li>
              <li>npm run dev</li>
            </ol>
          </Tile>

          <Tile icon="arrow" title={f.args.title} body={f.args.body}>
            <pre className="mini-code" dir="ltr">
              <span className="c-gold">gs</span> --short{'\n'}
              <span className="c-dim"># runs: git status --short</span>
            </pre>
          </Tile>

          <Tile icon="folder" title={f.cd.title} body={f.cd.body} flag="--type cd">
            <pre className="mini-code" dir="ltr">
              <span className="c-dim">PS C:\Users\you&gt;</span> <span className="c-gold">work</span>
              {'\n'}
              <span className="c-ok">PS C:\Projects\MyApp&gt;</span>
            </pre>
          </Tile>

          <Tile icon="shield" title={f.safe.title} body={f.safe.body} flag="--force" className="tile--wide">
            <pre className="mini-code" dir="ltr">
              <span className="c-dim">&gt; shortcut add git "echo hi"</span>
              {'\n'}
              <span className="c-err">Error: {f.safe.taken}</span>
              {'\n'}
              <span className="c-gold">{f.safe.ask}</span>
            </pre>
          </Tile>

          <Tile icon="file" title={f.saved.title} body={f.saved.body} className="tile--wide-sm">
            <pre className="mini-code" dir="ltr">
              {'{'}
              {'\n  '}
              <span className="c-gold">"gs"</span>: {'{ '}
              <span className="c-dim">"actions"</span>: [ {'{ '}
              <span className="c-dim">"type"</span>: <span className="c-ok">"command"</span>, …{' } ] }'}
              {'\n}'}
            </pre>
          </Tile>
        </div>

        <div ref={notes} className="notes reveal">
          <h3>{f.notesTitle}</h3>
          <ul>
            {f.notes.map((n) => (
              <li key={n}>{n}</li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
