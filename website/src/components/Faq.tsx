import { useI18n } from '../i18n/index.tsx';
import { useReveal } from '../hooks.ts';
import { ParticleField } from './ParticleField.tsx';
import { Icon } from './Icon.tsx';

export function Faq() {
  const { t } = useI18n();
  const ref = useReveal<HTMLDivElement>();
  return (
    <section className="section section--dark section--faq" id="faq">
      <ParticleField density={0.00006} />
      <div ref={ref} className="container container--narrow reveal">
        <h2 className="faq-title">{t.faq.title}</h2>
        <div className="disclosures">
          {t.faq.items.map((item) => (
            <details key={item.q} className="disclosure disclosure--dark">
              <summary>
                <span dir="auto">{item.q}</span>
                <Icon name="chevron" size={18} />
              </summary>
              <p>{item.a}</p>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}
