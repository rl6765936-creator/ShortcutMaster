import { useI18n } from '../i18n/index.tsx';
import { PROJECT } from '../config.ts';
import { Logo } from './Logo.tsx';
import { Icon } from './Icon.tsx';

export function Footer() {
  const { t, toggle } = useI18n();
  return (
    <footer className="site-footer">
      <div className="container site-footer__grid">
        <div>
          <Logo />
          <p className="site-footer__tagline">{t.footer.tagline}</p>
          <p className="site-footer__body">{t.footer.body}</p>
          <p className="site-footer__copy">© 2026 ShortcutMaster · v{PROJECT.version}</p>
        </div>
        <nav className="site-footer__links" aria-label="Footer">
          <a href={PROJECT.repoUrl} target="_blank" rel="noreferrer">
            <Icon name="github" size={15} /> {t.footer.links.github}
          </a>
          <a href={PROJECT.readmeUrl} target="_blank" rel="noreferrer">
            {t.footer.links.readme}
          </a>
          <a href="#install">{t.footer.links.install}</a>
          <a href="#faq">{t.footer.links.faq}</a>
          <button type="button" className="linklike" onClick={toggle}>
            <Icon name="globe" size={15} /> {t.footer.langBtn}
          </button>
        </nav>
      </div>
    </footer>
  );
}
