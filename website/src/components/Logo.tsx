/** The ShortcutMaster mark: a key cap with a prompt arrow and a gold cursor. */
export function LogoMark({ size = 28 }: { size?: number }) {
  return (
    <svg className="logo-mark" width={size} height={size} viewBox="0 0 64 64" aria-hidden="true">
      <defs>
        <linearGradient id="sm-g" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#8b6cff" />
          <stop offset="1" stopColor="#3be3d3" />
        </linearGradient>
      </defs>
      <rect x="4" y="4" width="56" height="56" rx="16" fill="#0e1440" />
      <rect x="4" y="4" width="56" height="56" rx="16" fill="none" stroke="url(#sm-g)" strokeWidth="3" />
      <path d="M19 22l11 10-11 10" fill="none" stroke="url(#sm-g)" strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M34 42h12" stroke="#e9bc63" strokeWidth="5" strokeLinecap="round" />
    </svg>
  );
}

export function Logo() {
  return (
    <span className="logo">
      <LogoMark />
      <span className="logo-word">
        Shortcut<b>Master</b>
      </span>
    </span>
  );
}
