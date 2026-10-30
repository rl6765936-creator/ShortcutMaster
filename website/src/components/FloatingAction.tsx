import { useEffect, useState } from 'react';
import { useI18n } from '../i18n/index.tsx';
import { Icon } from './Icon.tsx';

/** On small screens, a floating button to the demo appears after the hero and hides near it. */
export function FloatingAction() {
  const { t } = useI18n();
  const [show, setShow] = useState(false);

  useEffect(() => {
    const on = () => {
      const demo = document.getElementById('demo');
      const r = demo?.getBoundingClientRect();
      const nearDemo = r ? r.top < window.innerHeight && r.bottom > 0 : false;
      setShow(window.scrollY > window.innerHeight * 0.8 && !nearDemo);
    };
    on();
    window.addEventListener('scroll', on, { passive: true });
    return () => window.removeEventListener('scroll', on);
  }, []);

  return (
    <a className={`fab ${show ? 'is-shown' : ''}`} href="#demo" tabIndex={show ? 0 : -1} aria-hidden={!show}>
      <Icon name="play" size={13} />
      {t.fab}
    </a>
  );
}
