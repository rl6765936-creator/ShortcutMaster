import { useI18n } from '../i18n/index.tsx';
import { useReveal } from '../hooks.ts';
import { PROJECT } from '../config.ts';
import { Icon } from './Icon.tsx';
import { CmdLine } from './CopyButton.tsx';

const QUICK_COMMANDS: string[][] = [
  [
    '$dir = "$env:LOCALAPPDATA\\ShortcutMaster\\bin"; New-Item -ItemType Directory -Force $dir | Out-Null; Move-Item -Force "$HOME\\Downloads\\shortcut.exe" "$dir\\shortcut.exe"; Unblock-File "$dir\\shortcut.exe"; $p = [Environment]::GetEnvironmentVariable(\'Path\', \'User\'); if ($p -notlike "*$dir*") { [Environment]::SetEnvironmentVariable(\'Path\', "$p;$dir", \'User\') }',
  ],
  ['shortcut install'],
];

const SOURCE_COMMANDS: string[][] = [
  [PROJECT.cloneCommand, 'cd ShortcutMaster/ShortcutMaster'],
  ['dotnet pack -c Release', 'dotnet tool install --global --add-source ./nupkg ShortcutMaster'],
  ['shortcut install'],
  ['shortcut add gs "git status"', 'gs'],
];

function Steps({ steps, commands }: { steps: { title: string; body: string }[]; commands: string[][] }) {
  return (
    <ol className="steps">
      {steps.map((s, n) => (
        <li key={s.title} className="step">
          <span className="step__num">{n + 1}</span>
          <div className="step__body">
            <h3>{s.title}</h3>
            <p>{s.body}</p>
            {(commands[n] ?? []).map((c) => (
              <CmdLine key={c} cmd={c} />
            ))}
          </div>
        </li>
      ))}
    </ol>
  );
}

export function Install() {
  const { t } = useI18n();
  const i = t.install;
  const head = useReveal<HTMLDivElement>();
  const grid = useReveal<HTMLDivElement>();
  const more = useReveal<HTMLDivElement>();

  return (
    <section className="section section--light" id="install">
      <div className="container">
        <div ref={head} className="section-head reveal">
          <h2>{i.title}</h2>
          <p>{i.lead}</p>
        </div>

        <div ref={grid} className="install-grid reveal">
          <aside className="dl-card" id="download">
            <div className="dl-card__glow" aria-hidden="true" />
            <span className="dl-card__icon">
              <Icon name="terminal" size={26} />
            </span>
            <h3>
              {i.cardTitle} <span className="dl-card__ver">{PROJECT.version}</span>
            </h3>
            <p>{i.cardBody}</p>
            <a className="btn btn--gold btn--lg btn--block" href={PROJECT.exeUrl}>
              <Icon name="download" size={18} />
              <span>{i.exe}</span>
            </a>
            <div className="dl-card__links">
              <a href={PROJECT.repoUrl} target="_blank" rel="noreferrer">
                <Icon name="github" size={15} /> {i.source}
              </a>
              <a href={PROJECT.releasesUrl} target="_blank" rel="noreferrer">
                <Icon name="file" size={16} /> {i.releases}
              </a>
              <a href={PROJECT.readmeUrl} target="_blank" rel="noreferrer">
                <Icon name="book" size={16} /> {i.readme}
              </a>
            </div>
            <code className="dl-card__file" dir="ltr">
              shortcut.exe · win-x64 · SHA256SUMS.txt
            </code>
          </aside>

          <div className="install-flows">
            <h3 className="sub-title">{i.quickTitle}</h3>
            <Steps steps={i.quick} commands={QUICK_COMMANDS} />
            <p className="note-small">{i.smartscreen}</p>

            <h3 className="sub-title sub-title--spaced">{i.sourceTitle}</h3>
            <div className="source-links">
              <a href={PROJECT.zipUrl}>
                <Icon name="zip" size={16} /> {i.zip}
              </a>
              <a href={PROJECT.dotnetUrl} target="_blank" rel="noreferrer">
                <Icon name="download" size={16} /> {i.dotnet}
              </a>
            </div>
            <Steps steps={i.steps} commands={SOURCE_COMMANDS} />
          </div>
        </div>

        <div ref={more} className="more-grid reveal">
          <div>
            <h3 className="sub-title">{i.examplesTitle}</h3>
            {i.examples.map((e) => (
              <div key={e.cmd} className="example">
                <p className="example__label">{e.label}</p>
                <CmdLine cmd={e.cmd} />
              </div>
            ))}
          </div>
          <div>
            <h3 className="sub-title">{i.troubleTitle}</h3>
            <div className="disclosures">
              {i.trouble.map((x) => (
                <details key={x.q} className="disclosure">
                  <summary>
                    <span dir="auto">{x.q}</span>
                    <Icon name="chevron" size={18} />
                  </summary>
                  <p>{x.a}</p>
                </details>
              ))}
            </div>
            <h3 className="sub-title sub-title--spaced">{i.uninstallTitle}</h3>
            <p className="muted-p">{i.uninstallBody}</p>
          </div>
        </div>
      </div>
    </section>
  );
}
