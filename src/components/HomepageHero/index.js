import React, { useEffect, useRef } from "react";
import Link from "@docusaurus/Link";
import clsx from "clsx";
import styles from "./styles.module.css";
import Translate from '@docusaurus/Translate';

const SCENE_START = require("@site/static/img/scene-start.jpg").default;
const SCENE_LEVEL1 = require("@site/static/img/scene-level1.jpg").default;

/** Two real frames from the 3D Game Kit, rendered out of the editor. */
const SCENES = [
  { url: SCENE_START, name: "Start.unity" },
  { url: SCENE_LEVEL1, name: "Level1.unity" },
];

const SLANT = 32;   // how far the cut leans over the hero's height, in % of width
const LINE = 0.18;  // the lit edge itself, in % of width

/**
 * SceneOperationState, in the order a transition reports it. ScreenIn is the
 * loading screen arriving and ScreenOut is it leaving, not the other way round.
 */
const STATES = [
  ["Resolving", 0.0, 0.1],
  ["Screen In", 0.1, 0.22],
  ["Unloading", 0.22, 0.34],
  ["Loading", 0.34, 0.68],
  ["Activating", 0.68, 0.76],
  ["Screen Out", 0.76, 0.92],
  ["Completed", 0.92, 1.01],
];

/** The frame the server renders, and the one anyone with reduced motion keeps. */
const REST = 0.45;

const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
const inv = (t, a, b) => clamp((t - a) / (b - a), 0, 1);
const ease = (t) => t * t * (3 - 2 * t);

const poly = (pts) =>
  `polygon(${pts.map(([x, y]) => `${x.toFixed(2)}% ${y.toFixed(2)}%`).join(",")})`;

const outgoingClip = (t) => {
  const x = t * (100 + SLANT);
  return poly([[x, 0], [150, 0], [150, 100], [x - SLANT, 100]]);
};

const cutClip = (t) => {
  const x = t * (100 + SLANT);
  return poly([
    [x, 0],
    [x + LINE, 0],
    [x + LINE - SLANT, 100],
    [x - SLANT, 100],
  ]);
};

const stateAt = (t) => {
  for (let i = 0; i < STATES.length; i++) {
    if (t >= STATES[i][1] && t < STATES[i][2]) return i;
  }
  return STATES.length - 1;
};

const progressAt = (t) => (t < 0.34 ? 0 : inv(t, 0.34, 0.68));

const chipClass = (i, active) => {
  if (i === active) {
    // Completed carries its own colour in the sample's OperationHud
    return clsx(styles.chip, active === STATES.length - 1 ? styles.chipDone : styles.chipNow);
  }
  return clsx(styles.chip, i < active ? styles.chipPast : null);
};

export default function Hero() {
  const outRef = useRef(null);
  const cutRef = useRef(null);
  const inRef = useRef(null);
  const tagOutRef = useRef(null);
  const tagInRef = useRef(null);
  const chipRefs = useRef([]);
  const barRef = useRef(null);
  const pctRef = useRef(null);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return undefined;

    const SWEEP = 5200;
    const HOLD = 1500;
    const cycle = SWEEP + HOLD;

    let outIdx = 0;
    let inIdx = 1;
    let elapsed = 0;
    let last = 0;
    let raf = 0;

    const applyScenes = () => {
      if (outRef.current) outRef.current.style.backgroundImage = `url(${SCENES[outIdx].url})`;
      if (inRef.current) inRef.current.style.backgroundImage = `url(${SCENES[inIdx].url})`;
      if (tagOutRef.current) tagOutRef.current.textContent = SCENES[outIdx].name;
      if (tagInRef.current) tagInRef.current.textContent = SCENES[inIdx].name;
    };

    const render = (t) => {
      if (outRef.current) outRef.current.style.clipPath = outgoingClip(t);
      if (cutRef.current) {
        cutRef.current.style.clipPath = cutClip(t);
        cutRef.current.style.opacity = String(
          ease(inv(t, 0.02, 0.1)) * (1 - ease(inv(t, 0.9, 0.98)))
        );
      }
      if (tagOutRef.current) tagOutRef.current.style.opacity = String(1 - ease(inv(t, 0.6, 0.8)));
      if (tagInRef.current) tagInRef.current.style.opacity = String(ease(inv(t, 0.1, 0.32)));

      const active = stateAt(t);
      chipRefs.current.forEach((el, i) => {
        if (el) el.className = chipClass(i, active);
      });

      const p = progressAt(t);
      if (barRef.current) barRef.current.style.width = `${p * 100}%`;
      if (pctRef.current) pctRef.current.textContent = `${Math.round(p * 100)}%`;
    };

    const step = (now) => {
      if (!last) last = now;
      elapsed += now - last;
      last = now;
      if (elapsed >= cycle) {
        elapsed -= cycle;
        outIdx = inIdx;
        inIdx = (inIdx + 1) % SCENES.length;
        applyScenes();
      }
      render(clamp(elapsed / SWEEP, 0, 1));
      raf = requestAnimationFrame(step);
    };

    raf = requestAnimationFrame(step);
    return () => cancelAnimationFrame(raf);
  }, []);

  const restActive = stateAt(REST);
  const restProgress = progressAt(REST);

  return (
    <header className={clsx("hero hero--primary", styles.hero)}>
      <div className={styles.heroBackground} aria-hidden="true">
        <div
          className={styles.sceneLayer}
          ref={inRef}
          style={{ backgroundImage: `url(${SCENES[1].url})` }}
        />
        <div
          className={styles.sceneLayer}
          ref={outRef}
          style={{
            backgroundImage: `url(${SCENES[0].url})`,
            clipPath: outgoingClip(REST),
          }}
        />
        <div className={styles.cut} ref={cutRef} style={{ clipPath: cutClip(REST) }} />
        <div className={styles.heroOverlay} />
        <span className={clsx(styles.sceneTag, styles.sceneTagIn)} ref={tagInRef}>
          {SCENES[1].name}
        </span>
        <span className={clsx(styles.sceneTag, styles.sceneTagOut)} ref={tagOutRef}>
          {SCENES[0].name}
        </span>
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

        <div className={styles.rail} aria-hidden="true">
          <div className={styles.railOp}>
            MySceneManager.<span className={styles.railFn}>TransitionAsync</span>(
            <span className={styles.railStr}>&quot;Level1&quot;</span>,{" "}
            <span className={styles.railStr}>&quot;Loading&quot;</span>)
          </div>
          <div className={styles.chips}>
            {STATES.map(([label], i) => (
              <span
                key={label}
                className={chipClass(i, restActive)}
                ref={(el) => {
                  chipRefs.current[i] = el;
                }}
              >
                {label}
              </span>
            ))}
          </div>
          <div className={styles.meter}>
            <div className={styles.track}>
              <i ref={barRef} style={{ width: `${restProgress * 100}%` }} />
            </div>
            <span className={styles.pct} ref={pctRef}>
              {Math.round(restProgress * 100)}%
            </span>
          </div>
        </div>
      </div>
    </header>
  );
}
