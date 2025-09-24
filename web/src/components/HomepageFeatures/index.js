import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';
import Translate, {translate} from '@docusaurus/Translate';

const FeatureList = [
  {
    title: <Translate>Easy to Use</Translate>,
    Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        <Translate>
          The Geo-Thermometer is designed from the ground up to be easy to install and use, allowing you to quickly get started and accelerate your research process.
        </Translate>
      </>
    ),
  },
  {
    title: <Translate>Focus on What Matters</Translate>,
    Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        <Translate>
          Geo-Thermometer allows you to focus on the intricacies of scientific research, while we handle the tedious calculations and plotting for you.
        </Translate>
      </>
    ),
  },
  {
    title: <Translate>Powered by .NET</Translate>,
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        <Translate>
          Utilizing .NET technology, the software features a more modern user interface and provides a smooth operating experience.
        </Translate>
      </>
    ),
  },
];

function Feature({Svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
