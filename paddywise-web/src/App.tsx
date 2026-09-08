import Navbar from './components/Navbar';
import Hero from './components/Hero';
import ProblemSection from './components/ProblemSection';
import FeaturesSection from './components/FeaturesSection';
import WorkflowSection from './components/WorkflowSection';
import RolesSection from './components/RolesSection';
import CTASection from './components/CTASection';
import ContactSection from './components/ContactSection';
import Footer from './components/Footer';
import { useReveal } from './hooks/useReveal';

function App() {
  // Initialize scroll reveal animations
  useReveal();

  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <ProblemSection />
        <FeaturesSection />
        <WorkflowSection />
        <RolesSection />
        <CTASection />
        <ContactSection />
      </main>
      <Footer />
    </>
  );
}

export default App;
