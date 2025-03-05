import React from "react";
import Layout from "@theme/Layout";

import Hero from "@site/src/components/HomepageHero"
import Features from "@site/src/components/HomepageFeatures"
import Installation from "@site/src/components/HomepageInstallation"
import QuickStart from "@site/src/components/HomepageQuickStart"
import FeatureComparison from "@site/src/components/HomepageFeatureComparison";

export default function Home() {
  return (
    <Layout description="Discover the power of My Scene Manager, the ultimate Unity package for effortless scene transitions, async workflows, and seamless addressable scene management. Unlock a smoother game development experience with easy-to-follow guides and practical examples.">
      <Hero />
      <main>
        <Features />
        <QuickStart />
        <Installation />
        <FeatureComparison />
      </main>
    </Layout>
  );
}