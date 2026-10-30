import { useI18n } from '../i18n/index.tsx';

// Satellites around the central shortcuts.json card. Coordinates are in a 520×360 box.
const NODES = [
  { label: 'gs', x: 260, y: 34, color: '#8b6cff' },
  { label: 'dev', x: 430, y: 92, color: '#e9bc63' },
  { label: 'github', x: 462, y: 214, color: '#3be3d3' },
  { label: 'work', x: 360, y: 318, color: '#ff8fb1' },
  { label: 'start', x: 160, y: 318, color: '#7cc4ff' },
  { label: 'docs', x: 58, y: 214, color: '#9be37c' },
  { label: 'np', x: 90, y: 92, color: '#f6a35c' },
];

const CX = 260;
const CY = 180;

export function HeroArt() {
  const { t } = useI18n();
  return (
    <div className="hero-art" role="img" aria-label={t.hero.artLabel} dir="ltr">
      <svg viewBox="0 0 520 360" className="hero-art__svg" aria-hidden="true">
        <defs>
          <radialGradient id="ha-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0" stopColor="#8b6cff" stopOpacity="0.45" />
            <stop offset="1" stopColor="#8b6cff" stopOpacity="0" />
          </radialGradient>
        </defs>
        <circle cx={CX} cy={CY} r="150" fill="url(#ha-glow)" />
        {NODES.map((n, i) => {
          const mx = (CX + n.x) / 2 + (n.y - CY) * 0.18;
          const my = (CY + n.y) / 2 - (n.x - CX) * 0.18;
          const d = `M${CX},${CY} Q${mx},${my} ${n.x},${n.y}`;
          return (
            <g key={n.label}>
              <path d={d} className="ha-link" stroke={n.color} />
              <circle r="3" fill={n.color} className="ha-pulse">
                <animateMotion dur={`${2.6 + (i % 3) * 0.7}s`} begin={`${i * 0.45}s`} repeatCount="indefinite" path={d} />
              </circle>
            </g>
          );
        })}
      </svg>

      <div className="ha-core" style={{ left: `${(CX / 520) * 100}%`, top: `${(CY / 360) * 100}%` }}>
        <span className="ha-core__brace">{'{'}</span>
        <span className="ha-core__row" style={{ width: '78%', background: '#8b6cff' }} />
        <span className="ha-core__row" style={{ width: '62%', background: '#e9bc63' }} />
        <span className="ha-core__row" style={{ width: '84%', background: '#3be3d3' }} />
        <span className="ha-core__row" style={{ width: '56%', background: '#ff8fb1' }} />
        <span className="ha-core__row" style={{ width: '70%', background: '#7cc4ff' }} />
        <span className="ha-core__brace">{'}'}</span>
        <span className="ha-core__name">shortcuts.json</span>
      </div>

      {NODES.map((n) => (
        <span
          key={n.label}
          className="ha-node"
          style={{ left: `${(n.x / 520) * 100}%`, top: `${(n.y / 360) * 100}%`, ['--c' as string]: n.color }}
        >
          <i />
          {n.label}
        </span>
      ))}
    </div>
  );
}
