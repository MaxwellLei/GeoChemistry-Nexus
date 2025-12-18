import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import useBaseUrl from '@docusaurus/useBaseUrl';

import Heading from '@theme/Heading';
import styles from './index.module.css';
import Translate, {translate} from '@docusaurus/Translate';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={styles.heroBanner}>
      <div className="container">
        <Heading as="h1" className={styles.heroTitle}>
          {siteConfig.title}
        </Heading>
        <p className={styles.heroSubtitle}>
          <Translate id="home.tagline">
            Next-Gen Geochemistry & Petrology Discrimination Diagrams and Calculation Tool — A Geoscientist's Best Friend
          </Translate>
        </p>
        <div className={styles.buttons}>
          <Link
            className={clsx('button button--primary', styles.buttonPrimary)}
            to="/docs/intro">
            <Translate id="home.button.getStarted">Get Started</Translate>
          </Link>
          <Link
            className={styles.buttonSecondary}
            to="https://github.com/MaxwellLei/GeoChemistry-Nexus">
            <Translate id="home.button.github">View on GitHub</Translate> >
          </Link>
        </div>
        
        <div className={styles.heroImageContainer}>
            {/* Placeholder for Main App Screenshot */}
            <img 
                src={useBaseUrl('/img/home.webp')}
                alt="GeoChemistry Nexus Dashboard" 
                className={styles.heroImage}
            />
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`Home`}
      description="Next-Gen Geochemistry & Petrology Tool">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
