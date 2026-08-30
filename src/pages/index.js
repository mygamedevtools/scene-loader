import React from "react";
import Layout from "@theme/Layout";

import Hero from "@site/src/components/HomepageHero"
import Proof from "@site/src/components/HomepageProof"
import Features from "@site/src/components/HomepageFeatures"
import Installation from "@site/src/components/HomepageInstallation"
import QuickStart from "@site/src/components/HomepageQuickStart"
import Operation from "@site/src/components/HomepageOperation"
import CaseStudy from "@site/src/components/HomepageCaseStudy"
import FeatureComparison from "@site/src/components/HomepageFeatureComparison";

export default function Home() {
  return (
    <Layout description="Load, unload and transition Unity scenes in one line. One API for Build Settings and Addressables, with progress, phases and cancellation on every operation.">
      <Hero />
      <main>
        {/* The argument runs claim -> proof -> detail -> ask. The code comparison is the
            most convincing thing on the page, so it goes first; Installation goes last,
            because asking for the install while an argument is still pending loses both. */}
        <Proof />
        <QuickStart />
        <CaseStudy />
        <Operation />
        <Features />
        <FeatureComparison />
        <Installation />
      </main>
    </Layout>
  );
}
