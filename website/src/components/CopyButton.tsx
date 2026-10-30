import { useCopy } from '../hooks.ts';
import { useI18n } from '../i18n/index.tsx';
import { Icon } from './Icon.tsx';

export function CopyButton({ text, className = '' }: { text: string; className?: string }) {
  const { t } = useI18n();
  const [copied, copy] = useCopy();
  return (
    <button type="button" className={`copy-btn ${copied ? 'is-copied' : ''} ${className}`} onClick={() => copy(text)}>
      <Icon name={copied ? 'check' : 'copy'} size={15} />
      <span aria-live="polite">{copied ? t.a11y.copied : t.a11y.copy}</span>
    </button>
  );
}

/** A copyable command line with a terminal prompt. */
export function CmdLine({ cmd, prompt = 'PS>' }: { cmd: string; prompt?: string }) {
  return (
    <div className="cmdline" dir="ltr">
      <code>
        <span className="t-prompt">{prompt}</span> {cmd}
      </code>
      <CopyButton text={cmd} />
    </div>
  );
}
