import './WorkflowSection.css';

export default function WorkflowSection() {
  const steps = [
    {
      number: '01',
      title: 'Report',
      description: 'A farmer submits a pest photo and symptoms from their field, in-app.',
      color: '#B4CB9C' // shoot-light
    },
    {
      number: '02',
      title: 'Analyze',
      description: 'The advisory agent classifies the issue against known patterns.',
      color: '#7FA66C' // shoot
    },
    {
      number: '03',
      title: 'Recommend',
      description: 'A treatment plan is calculated and checked against safe dosage limits.',
      color: '#E1A63B' // gold
    },
    {
      number: '04',
      title: 'Approve',
      description: 'An extension officer reviews the evidence and approves, rejects, or revises.',
      color: '#C48A28' // gold-deep
    },
    {
      number: '05',
      title: 'Act',
      description: 'The farmer receives the approved plan with clear instructions on their phone.',
      color: '#F6F1E3' // cream
    }
  ];

  return (
    <section id="workflow" className="workflow-section dark-section section-padding">
      <div className="container">
        <div className="workflow-header reveal">
          <span className="eyebrow">A REAL WORKFLOW, START TO FINISH</span>
          <h2 className="section-title">From a photo in the field to an approved treatment plan</h2>
          <p className="workflow-description">
            Every step is planned, validated and logged — and nothing reaches a farmer until an authorized officer signs off.
          </p>
        </div>

        <div className="workflow-track">
          {steps.map((step, index) => (
            <div 
              key={step.number} 
              className="workflow-step reveal"
              style={{ transitionDelay: `${index * 0.15}s` }}
            >
              <div className="workflow-number" style={{ color: step.color }}>
                {step.number}
              </div>
              <div className="workflow-content">
                <h3 className="workflow-title">{step.title}</h3>
                <p className="workflow-text">{step.description}</p>
              </div>
              {/* Connector line for desktop */}
              {index < steps.length - 1 && <div className="workflow-connector"></div>}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
