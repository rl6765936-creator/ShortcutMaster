import { useI18n } from '../i18n/index.tsx';
import { PROJECT } from '../config.ts';
import { Icon } from './Icon.tsx';
import { HeroArt } from './HeroArt.tsx';
import { HeroTerminal } from './HeroTerminal.tsx';
import { ParticleField } from './ParticleField.tsx';

export function Hero() {
  const { t } = useI18n();
  return (
    <section className="hero" id="top">
      <ParticleField />
      <div className="container hero__grid">
        <div className="hero__copy">
          <h1>
            {t.hero.titleA} <span className="grad">{t.hero.titleB}</span> {t.hero.titleC}
          </h1>
          <p className="hero__lead">{t.hero.lead}</p>
          <div className="hero__cta">
            <a className="btn btn--gold btn--lg" href={PROJECT.repoUrl} target="_blank" rel="noreferrer">
              <Icon name="github" size={18} />
              <span>{t.hero.primary}</span>
            </a>
            <a className="btn btn--ghost btn--lg" href="#demo">
              <Icon name="play" size={14} />
              <span>{t.hero.secondary}</span>
            </a>
          </div>
          <ul className="hero__ticks">
            {t.hero.ticks.map((x) => (
              <li key={x}>{x}</li>
            ))}
          </ul>
        </div>
        <div className="hero__visual">
          <HeroArt />
          <HeroTerminal />
        </div>
      </div>
    </section>
  );
}
