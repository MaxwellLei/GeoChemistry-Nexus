import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';
import Translate, {translate} from '@docusaurus/Translate';
import useBaseUrl from '@docusaurus/useBaseUrl';

const FeatureList = [
  {
    title: <Translate>Comprehensive Plotting Toolkit</Translate>,
    // Placeholder image for Plotting Toolkit
    image: '/img/plotting_toolkit.webp', 
    description: (
      <Translate>
        GeoChemistry Nexus includes a built-in Geoscience Illustrated Plotting Module with templates for Tectonic Setting Discrimination, Rock Classification, and more. It offers a complete workflow from data import to publication-quality export (SVG supported).
      </Translate>
    ),
  },
  {
    title: <Translate>Excel-like Geothermometers</Translate>,
    // Placeholder image for Geothermometers
    image: '/img/excel_like_geothermometers.webp',
    description: (
      <Translate>
        Perform geothermometer calculations for minerals like Zircon and Biotite using an intuitive, Excel-like interface. Use built-in templates or custom functions (e.g., =FuncName(args)) to speed up your research without needing programming skills.
      </Translate>
    ),
  },
  {
    title: <Translate>Customizable & Extensible</Translate>,
    // Placeholder image for Customizability
    image: '/img/customizable_extensible.webp',
    description: (
      <Translate>
        Create and share your own diagram templates using our JSON/ZIP based extension system. The platform supports multi-language templates, custom scripting (JavaScript), and seamless integration with third-party software like Inkscape and CorelDRAW.
      </Translate>
    ),
  },
];

function Feature({image, title, description}) {
  return (
    <div className={styles.featureSection}>
      <div className={styles.featureContent}>
        <Heading as="h2" className={styles.featureTitle}>
          {title}
        </Heading>
        <p className={styles.featureDescription}>{description}</p>
      </div>
      <div className={styles.featureImageContainer}>
        <img src={useBaseUrl(image)} alt={title} className={styles.featureImage} />
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        {FeatureList.map((props, idx) => (
          <Feature key={idx} {...props} />
        ))}
      </div>
    </section>
  );
}
