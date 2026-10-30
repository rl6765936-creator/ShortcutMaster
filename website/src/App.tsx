import { useI18n } from './i18n/index.tsx';
import { Header } from './components/Header.tsx';
import { Hero } from './components/Hero.tsx';
import { Features } from './components/Features.tsx';
import { Demo } from './components/Demo.tsx';
import { Install } from './components/Install.tsx';
import { Faq } from './components/Faq.tsx';
import { Footer } from './components/Footer.tsx';
import { FloatingAction } from './components/FloatingAction.tsx';

export function App() {
  const { t } = useI18n();
  return (
    <>
      <a className="skip-link" href="#main">
        {t.a11y.skip}
      </a>
      <Header />
      <main id="main" tabIndex={-1}>
        <Hero />
        <Features />
        <Demo />
        <Install />
        <Faq />
      </main>
      <Footer />
      <FloatingAction />
    </>
  );
}
