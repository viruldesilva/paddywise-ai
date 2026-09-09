import './ProblemSection.css';

export default function ProblemSection() {
  const problems = [
    {
      number: '01',
      title: 'A report that goes nowhere',
      description: "Pest and disease reports travel by phone call or paper — with no record of who saw it, or when it was actually acted on.",
    },
    {
      number: '02',
      title: 'Advice without a paper trail',
      description: "Treatment recommendations are rarely checked against safe dosage limits, and there's no approval step before a farmer sprays.",
    },
    {
      number: '03',
      title: 'Harvest sold blind',
      description: "Without visibility into nearby buyer offers, farmers often sell to the first mill that calls — not the fairest price available.",
    }
  ];

  return (
    <section id="problem" className="problem-section section-padding">
      <div className="container">
        <div className="problem-header reveal">
          <span className="eyebrow">THE PROBLEM ON THE GROUND</span>
          <h2 className="section-title">Three gaps that cost a season's income</h2>
        </div>

        <div className="problem-grid">
          {problems.map((problem, index) => (
            <div 
              key={problem.number} 
              className="problem-card reveal" 
              style={{ transitionDelay: `${index * 0.15}s` }}
            >
              <div className="problem-number">{problem.number}</div>
              <h3 className="problem-title">{problem.title}</h3>
              <p className="problem-description">{problem.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
