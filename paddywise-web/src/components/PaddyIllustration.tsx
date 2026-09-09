import './PaddyIllustration.css';

export default function PaddyIllustration() {
  return (
    <div className="illustration-wrapper">
      <svg
        className="paddy-svg"
        viewBox="0 0 500 400"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        {/* Background / Sky or far mountains */}
        <path d="M0 100 C 150 50, 350 150, 500 80 L 500 400 L 0 400 Z" fill="#F6F1E3" opacity="0.3" />

        {/* Harvest Stage (Bottom - Clay/Browns) */}
        <path className="field-layer field-harvest" d="M -50 450 C 100 320, 300 380, 550 300 L 550 450 L -50 450 Z" />
        
        {/* Golden Crops (Middle Bottom - Gold) */}
        <path className="field-layer field-gold" d="M -50 350 C 150 250, 350 320, 550 220 L 550 450 L -50 450 Z" />

        {/* Mature Green Crops (Middle Top - Shoot/Forest) */}
        <path className="field-layer field-mature" d="M -50 280 C 120 180, 380 250, 550 150 L 550 450 L -50 450 Z" />

        {/* Young Green Crops (Top - Light Green) */}
        <path className="field-layer field-young" d="M -50 220 C 150 100, 350 180, 550 100 L 550 450 L -50 450 Z" />

        {/* Terraces Lines / Accents */}
        <path d="M -50 220 C 150 100, 350 180, 550 100" stroke="#7FA66C" strokeWidth="2" fill="none" opacity="0.6"/>
        <path d="M -50 280 C 120 180, 380 250, 550 150" stroke="#4B5645" strokeWidth="2" fill="none" opacity="0.4"/>
        <path d="M -50 350 C 150 250, 350 320, 550 220" stroke="#E1A63B" strokeWidth="2" fill="none" opacity="0.8"/>
        <path d="M -50 450 C 100 320, 300 380, 550 300" stroke="#A6693F" strokeWidth="2" fill="none" opacity="0.5"/>

        {/* Abstract Paddy Stalks - Decorative */}
        <g className="stalks">
          {/* Young */}
          <path d="M 80 180 Q 85 160 95 150" stroke="#F6F1E3" strokeWidth="1.5" strokeLinecap="round" opacity="0.5"/>
          <path d="M 120 160 Q 125 140 135 130" stroke="#F6F1E3" strokeWidth="1.5" strokeLinecap="round" opacity="0.5"/>
          
          {/* Mature */}
          <path d="M 150 250 Q 155 220 170 210" stroke="#E1A63B" strokeWidth="1.5" strokeLinecap="round" opacity="0.4"/>
          <path d="M 220 220 Q 225 190 240 180" stroke="#E1A63B" strokeWidth="1.5" strokeLinecap="round" opacity="0.4"/>

          {/* Gold */}
          <path d="M 280 320 Q 290 280 310 270" stroke="#212D1E" strokeWidth="1.5" strokeLinecap="round" opacity="0.3"/>
          <path d="M 380 290 Q 390 250 410 240" stroke="#212D1E" strokeWidth="1.5" strokeLinecap="round" opacity="0.3"/>
        </g>
      </svg>
    </div>
  );
}
