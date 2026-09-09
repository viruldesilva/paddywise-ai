import { Sprout, Search, Wheat, CloudSun } from 'lucide-react';
import './FeaturesSection.css';

export default function FeaturesSection() {
  const features = [
    {
      id: 'A',
      icon: <Sprout size={32} strokeWidth={1.5} />,
      title: 'Cultivation tracking',
      description: 'Register your kumbura, log sowing dates, and follow each growth stage from nursery to maturity.',
      tag: 'COMPONENT A'
    },
    {
      id: 'B',
      icon: <Search size={32} strokeWidth={1.5} />,
      title: 'Pest & disease advisory',
      description: 'Photograph the damage and get an AI-assisted diagnosis, reviewed and approved by your extension officer.',
      tag: 'COMPONENT B'
    },
    {
      id: 'C',
      icon: <Wheat size={32} strokeWidth={1.5} />,
      title: 'Harvest & market',
      description: 'Record your yield and grade, then compare live offers from buyers and mills near your field.',
      tag: 'COMPONENT C'
    },
    {
      id: 'D',
      icon: <CloudSun size={32} strokeWidth={1.5} />,
      title: 'Resources & weather',
      description: "Track fertilizer stock and subsidy status, with spray and irrigation timing based on the week's forecast.",
      tag: 'COMPONENT D'
    }
  ];

  return (
    <section id="features" className="features-section section-padding">
      <div className="container">
        <div className="features-header reveal">
          <span className="eyebrow">FOUR CONNECTED COMPONENTS</span>
          <h2 className="section-title">One shared system, from nursery to sale</h2>
          <p className="features-description">
            Every component runs on the same backend, database and identity — so nothing gets re-entered twice.
          </p>
        </div>

        <div className="features-grid">
          {features.map((feature, index) => (
            <div 
              key={feature.id} 
              className="feature-card reveal"
              style={{ transitionDelay: `${index * 0.15}s` }}
            >
              <div className="feature-tag">{feature.tag}</div>
              <div className="feature-icon">{feature.icon}</div>
              <h3 className="feature-title">{feature.title}</h3>
              <p className="feature-text">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
