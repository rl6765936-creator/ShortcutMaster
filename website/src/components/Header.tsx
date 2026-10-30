import { useEffect, useState } from 'react';
import { useI18n } from '../i18n/index.tsx';
import { PROJECT } from '../config.ts';
import { Logo } from './Logo.tsx';
import { Icon } from './Icon.tsx';

export function Header() {
  const { t, locale, toggle } = useI18n();
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const on = () => setScrolled(window.scrollY > 12);
    on();
    window.addEventListener('scroll', on, { passive: true });
    return () => window.removeEventListener('scroll', on);
  }, []);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open]);

  const links = [
    { href: '#features', label: t.nav.features },
    { href: '#demo', label: t.nav.demo },
    { href: '#install', label: t.nav.install },
    { href: '#faq', label: t.nav.faq },
  ];

  return (
    <header className={`site-header ${scrolled ? 'is-scrolled' : ''} ${open ? 'is-open' : ''}`}>
      <div className="container site-header__inner">
        <a href="#top" className="logo-link" aria-label="ShortcutMaster" onClick={() => setOpen(false)}>
          <Logo />
        </a>
        <nav className="site-nav" id="site-nav" aria-label="Main">
          {links.map((l) => (
            <a key={l.href} href={l.href} onClick={() => setOpen(false)}>
              {l.label}
            </a>
          ))}
        </nav>
        <div className="site-header__actions">
          <button type="button" className="lang" onClick={toggle} aria-label={t.a11y.language}>
            <Icon name="globe" size={16} />
            <span>{locale === 'en' ? 'EN' : 'עב'}</span>
          </button>
          <a className="btn btn--gold btn--sm" href={PROJECT.repoUrl} target="_blank" rel="noreferrer">
            <Icon name="github" size={16} />
            <span>{t.nav.github}</span>
          </a>
          <button
            type="button"
            className="menu-btn"
            aria-expanded={open}
            aria-controls="site-nav"
            aria-label={t.a11y.menu}
            onClick={() => setOpen((o) => !o)}
          >
            <Icon name={open ? 'close' : 'menu'} size={20} />
          </button>
        </div>
      </div>
    </header>
  );
}
