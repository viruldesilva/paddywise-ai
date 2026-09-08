import { Link } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';
import './CTASection.css';

export default function CTASection() {
  return (
    <section className="cta-section section-padding">
      <div className="container">
        <div className="cta-panel reveal">
          <div className="cta-content">
            <h2 className="cta-title">Ready to see a season end differently?</h2>
            <p className="cta-description">
              Request access to the demo environment, or explore the repository to see how each component fits together.
            </p>
            <div className="cta-actions">
              <Link to="/register" className="btn btn-primary">Request a demo</Link>
              <a href="#repository" className="btn btn-secondary btn-secondary-light">
                View the repository <ArrowRight size={18} className="icon-right" />
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
