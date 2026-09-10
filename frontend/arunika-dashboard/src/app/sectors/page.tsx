export const dynamic = "force-dynamic";

import { getSectors, type SectorAggregation } from "@/lib/api";
import {
  ImpactMeter,
  PageHeader,
  Rubric,
  SectionRule,
  SentimentMark,
} from "@/components/editorial";

/**
 * Sector board. Each sector is a bordered column rather than a rounded card,
 * with the average impact set in the display face so the page scans as a table
 * of figures — the sentiment split is spelled out in words and counts instead
 * of the arrow emoji it used to carry.
 */
function SectorEntry({ sector, index }: { sector: SectorAggregation; index: number }) {
  const splits: { key: "Bullish" | "Neutral" | "Bearish"; count: number }[] = [
    { key: "Bullish", count: sector.bullishCount },
    { key: "Neutral", count: sector.neutralCount },
    { key: "Bearish", count: sector.bearishCount },
  ];

  return (
    <article
      className="editorial-reveal flex h-full flex-col gap-4 border-t-2 border-foreground/85 pt-4"
      style={{ animationDelay: `${60 + index * 45}ms` }}
    >
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="editorial-display text-xl text-foreground">{sector.sector}</h3>
        <SentimentMark sentiment={sector.dominantSentiment} />
      </div>

      <div className="flex items-baseline gap-2">
        <span className="editorial-display text-4xl tabular-nums text-foreground">
          {sector.averageImpactScore}
        </span>
        <Rubric>Avg. impact</Rubric>
      </div>

      {/* Bare: the score is already set as the hero number directly above. */}
      <ImpactMeter score={sector.averageImpactScore} variant="bare" />

      <dl className="mt-auto flex flex-col gap-1.5 border-t border-border pt-3">
        {splits.map((split) => (
          <div key={split.key} className="flex items-center gap-2 text-sm">
            <dt>
              <SentimentMark sentiment={split.key} />
            </dt>
            <dd className="ml-auto font-mono text-xs tabular-nums text-foreground">
              {split.count}
            </dd>
          </div>
        ))}
      </dl>

      <Rubric>
        {sector.articleCount} article{sector.articleCount === 1 ? "" : "s"}
      </Rubric>
    </article>
  );
}

export default async function SectorsPage() {
  const sectors = await getSectors();
  const totalArticles = sectors.reduce((sum, sector) => sum + sector.articleCount, 0);

  return (
    <div className="flex flex-col gap-10 sm:gap-12">
      <PageHeader
        kicker="Last 7 days"
        meta={sectors.length > 0 ? `${sectors.length} sectors · ${totalArticles} articles` : undefined}
        title="Sector Analysis"
        lede="Sentiment and impact aggregated by sector, across every enriched story in the window."
      />

      <section className="flex flex-col gap-6">
        <SectionRule label="By average impact" />

        {sectors.length === 0 ? (
          <div className="editorial-panel p-8 text-center">
            <p className="text-sm text-muted">
              No sector data yet. Check back once articles have been enriched.
            </p>
          </div>
        ) : (
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 lg:gap-10">
            {sectors.map((sector, index) => (
              <SectorEntry key={sector.sector} sector={sector} index={index} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
