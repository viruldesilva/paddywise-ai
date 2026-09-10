import { ArrowRight } from 'lucide-react';
import logoImage from '../assets/logo.png';
import './Hero.css';

export default function Hero() {
  return (
    <section className="hero section-padding">
      <div className="container hero-container">
        <div className="hero-content reveal">
          <span className="eyebrow">BUILT FOR THE YALA & MAHA SEASONS</span>
          <h1 className="hero-title">
            Every paddy field,<br />
            <span className="hero-highlight">one clear season.</span>
          </h1>
          <p className="hero-description">
            Kumburu connects farmers, extension officers and buyers on one system — so a pest report gets answered in hours, not weeks, and the harvest finds a fair price.
          </p>

          <div className="hero-actions">
            <a href="#features" className="btn btn-primary">See how it works</a>
            <a href="#roles" className="btn btn-secondary">
              View the roles <ArrowRight size={18} className="icon-right" />
            </a>
          </div>

          <div className="hero-stats">
            <div className="stat-item">
              <span className="stat-number">4</span>
              <span className="stat-label">connected business components</span>
            </div>
            <div className="stat-item">
              <span className="stat-number">3</span>
              <span className="stat-label">growth stages tracked per cycle</span>
            </div>
            <div className="stat-item">
              <span className="stat-number">1</span>
              <span className="stat-label">approval step before any advice ships</span>
            </div>
          </div>
        </div>

        <div className="hero-visual reveal" style={{ transitionDelay: '0.2s' }}>
          <img src={logoImage} alt="Hero Image" />

        </div>
      </div>
    </section>
  );
}
