import Link from "@docusaurus/Link";
import clsx from "clsx";
import styles from "./styles.module.css";
import Translate from '@docusaurus/Translate';

export default function Hero() {
  return (
    <header className={clsx("hero hero--primary", styles.hero)}>
      <div className={styles.heroBackground}>
        <video
          autoPlay
          loop
          muted
          playsInline
          className={styles.heroVideo}
          poster={require("@site/static/img/hero.jpg").default}
        >
          <source src={require("@site/static/img/hero.mp4").default} type="video/mp4" />
          Your browser does not support the video tag.
        </video>
        <div className={styles.heroOverlay}></div>
      </div>
      <div className={clsx("container", styles.heroContent)}>
        <h1 className={clsx("hero__title", styles.heroTitle)}>My Scene Manager</h1>
        <p className={clsx("hero__subtitle", styles.heroSubtitle)}>
          <Translate id="homepage.heroSubtitle">Enhance your scene management experience in Unity.</Translate>
        </p>
        <div className={styles.indexCta}>
          <Link className={clsx("button", styles.buttonCta)} to="/docs/intro">
            <Translate id="homepage.callToAction.label">Get Started</Translate>
          </Link>
          <span className={styles.indexCtaGitHubButtonWrapper}>
            <iframe
              className={styles.indexCtaGitHubButton}
              src="https://ghbtns.com/github-btn.html?user=mygamedevtools&amp;repo=scene-loader&amp;type=star&amp;count=true&amp;size=large"
              width={160}
              height={30}
              title="GitHub Stars"
            />
          </span>
        </div>
      </div>
    </header>
  );
}