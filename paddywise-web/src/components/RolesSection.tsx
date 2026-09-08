import './RolesSection.css';

export default function RolesSection() {
  const roles = [
    {
      title: 'Farmer',
      badge: 'MOBILE',
      description: 'Logs cultivation cycles, reports issues from the field, and tracks harvest and sales.'
    },
    {
      title: 'Extension Officer',
      badge: 'WEB',
      description: 'Reviews evidence, approves treatment plans, and monitors activity across a division.'
    },
    {
      title: 'Buyer / Miller',
      badge: 'MOBILE',
      description: 'Browses harvest listings nearby and places offers directly to farmers.'
    },
    {
      title: 'Admin',
      badge: 'WEB',
      description: 'Manages accounts, the pesticide allow-list, and system-wide activity.'
    }
  ];

  return (
    <section id="roles" className="roles-section section-padding">
      <div className="container">
        <div className="roles-header reveal">
          <span className="eyebrow">BUILT FOR FOUR KINDS OF USERS</span>
          <h2 className="section-title">Everyone sees exactly what their role needs</h2>
        </div>

        <div className="roles-grid">
          {roles.map((role, index) => (
            <div 
              key={role.title} 
              className="role-card reveal"
              style={{ transitionDelay: `${index * 0.15}s` }}
            >
              <div className="role-header">
                <h3 className="role-title">{role.title}</h3>
                <span className="role-badge">{role.badge}</span>
              </div>
              <p className="role-description">{role.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
