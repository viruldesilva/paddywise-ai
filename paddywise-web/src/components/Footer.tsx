import './Footer.css';

export default function Footer() {
  const currentYear = new Date().getFullYear(); // Will be 2026 based on requirements or dynamic

  return (
    <footer className="footer">
      <div className="container">
        <div className="footer-top">
          <div className="footer-brand">
            <div className="footer-logo-group">
              <svg
                className="footer-logo"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M12 22v-8" />
                <path d="M12 14c-3-2-6-3-6-6 0-3 3-4 6-4" />
                <path d="M12 14c3-2 6-3 6-6 0-3-3-4-6-4" />
                <path d="M12 4v10" />
              </svg>
              <span className="footer-title">Kumburu</span>
            </div>
            <p className="footer-description">
              A connected system for paddy cultivation, advisory and sale — built for Sri Lanka's Yala and Maha seasons.
            </p>
          </div>

          <div className="footer-links">
            <div className="footer-column">
              <h4 className="footer-heading">Product</h4>
              <ul>
                <li><a href="#features">Components</a></li>
                <li><a href="#workflow">Workflow</a></li>
                <li><a href="#roles">Roles</a></li>
              </ul>
            </div>
            <div className="footer-column">
              <h4 className="footer-heading">Project</h4>
              <ul>
                <li><a href="#repository">Repository</a></li>
                <li><a href="#architecture">Architecture decisions</a></li>
                <li><a href="#contact">Contact team</a></li>
              </ul>
            </div>
          </div>
        </div>

        <div className="footer-bottom">
          <p>© {currentYear} Kumburu. A student project.</p>
          <p>SE3090 — Software Engineering Frameworks, SLIIT</p>
        </div>
      </div>
    </footer>
  );
}
