import { Mail, MapPin, Phone } from 'lucide-react';
import './ContactSection.css';

export default function ContactSection() {
  return (
    <section id="contact" className="contact-section section-padding">
      <div className="container">
        <div className="contact-header reveal">
          <span className="eyebrow">GET IN TOUCH</span>
          <h2 className="section-title">Let's connect your field</h2>
          <p className="contact-description">
            Have questions about integrating Kumburu in your district? Reach out to our team or request a direct callback.
          </p>
        </div>

        <div className="contact-grid">
          <div className="contact-info reveal" style={{ transitionDelay: '0.15s' }}>
            <div className="contact-item">
              <div className="contact-icon">
                <Mail size={24} />
              </div>
              <div className="contact-details">
                <h3>Email us</h3>
                <p>hello@kumburu.lk</p>
              </div>
            </div>
            
            <div className="contact-item">
              <div className="contact-icon">
                <Phone size={24} />
              </div>
              <div className="contact-details">
                <h3>Call our team</h3>
                <p>+94 11 234 5678</p>
              </div>
            </div>
            
            <div className="contact-item">
              <div className="contact-icon">
                <MapPin size={24} />
              </div>
              <div className="contact-details">
                <h3>Visit us</h3>
                <p>SLIIT, New Kandy Road<br/>Malabe, Sri Lanka</p>
              </div>
            </div>
          </div>

          <div className="contact-form-container reveal" style={{ transitionDelay: '0.3s' }}>
            <form className="contact-form" onSubmit={(e) => e.preventDefault()}>
              <div className="form-group">
                <label htmlFor="name">Full Name</label>
                <input type="text" id="name" placeholder="E.g. Amara Silva" />
              </div>
              
              <div className="form-group">
                <label htmlFor="email">Email Address</label>
                <input type="email" id="email" placeholder="amara@example.com" />
              </div>
              
              <div className="form-group">
                <label htmlFor="role">Your Role</label>
                <select id="role">
                  <option value="">Select a role</option>
                  <option value="farmer">Farmer</option>
                  <option value="extension_officer">Extension Officer</option>
                  <option value="buyer">Buyer / Miller</option>
                  <option value="other">Other</option>
                </select>
              </div>
              
              <div className="form-group">
                <label htmlFor="message">Message</label>
                <textarea id="message" rows={4} placeholder="How can we help you?"></textarea>
              </div>
              
              <button type="submit" className="btn btn-primary submit-btn">
                Send Message
              </button>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}
