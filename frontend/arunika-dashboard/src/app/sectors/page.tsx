export const dynamic = "force-dynamic";

import {
  getSectorHistory,
  getSectors,
  type SectorAggregation,
  type SectorHistoryPoint,
} from "@/lib/api";
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
const HISTORY_DAYS = 30;

/** Bullish minus bearish, as a share of the day's stories: -100 (all bearish) to +100. */
function netSentiment(point: SectorHistoryPoint): number {
  return point.articleCount > 0
    ? Math.round(((point.bullishCount - point.bearishCount) / point.articleCount) * 100)
    : 0;
}

function signed(value: number): string {
  return value > 0 ? `+${value}` : String(value);
}

function dayIndex(date: string, firstDay: number): number {
  return Math.round((Date.parse(`${date}T00:00:00Z`) - firstDay) / 86_400_000);
}

/**
 * Net sentiment per day over the last {@link HISTORY_DAYS} days. A single
 * series, so no legend: the label above names it. Days place by date, so a
 * day with no stories shows as a straight run between its neighbours rather
 * than squeezing the line. Each day has a hover target wider than the line,
 * and a hidden table carries the same figures for screen readers.
 */
function SectorTrend({ points }: { points: SectorHistoryPoint[] }) {
  if (points.length < 2) {
    return null;
  }

  const today = new Date();
  const firstDay = Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate()) - HISTORY_DAYS * 86_400_000;
  const width = HISTORY_DAYS;
  const height = 32;
  const x = (point: SectorHistoryPoint) => dayIndex(point.date, firstDay);
  // -100..+100 onto the box, positive up.
  const y = (value: number) => height / 2 - (value / 100) * (height / 2 - 1);

  const values = points.map(netSentiment);
  const latest = values[values.length - 1];
  const path = points.map((point, i) => `${i === 0 ? "M" : "L"}${x(point)},${y(values[i])}`).join(" ");

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between gap-2">
        <Rubric>Net sentiment · {HISTORY_DAYS} days</Rubric>
        <span className="font-mono text-xs tabular-nums text-foreground">{signed(latest)}</span>
      </div>

      <svg
        viewBox={`0 0 ${width} ${height}`}
        preserveAspectRatio="none"
        className="h-8 w-full text-muted"
        aria-hidden
      >
        <line x1={0} x2={width} y1={height / 2} y2={height / 2} stroke="var(--border)" strokeWidth={1} vectorEffect="non-scaling-stroke" />
        <path d={path} fill="none" stroke="currentColor" strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" vectorEffect="non-scaling-stroke" />
        {points.map((point, i) => (
          <rect key={point.date} x={x(point) - 0.5} y={0} width={1} height={height} fill="transparent">
            <title>
              {`${point.date}: ${signed(values[i])} (${point.bullishCount} bullish, ${point.bearishCount} bearish of ${point.articleCount})`}
            </title>
          </rect>
        ))}
      </svg>

      <table className="sr-only">
        <caption>Net sentiment by day</caption>
        <tbody>
          {points.map((point, i) => (
            <tr key={point.date}>
              <th scope="row">{point.date}</th>
              <td>{signed(values[i])}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SectorEntry({
  sector,
  index,
  history,
}: {
  sector: SectorAggregation;
  index: number;
  history: SectorHistoryPoint[];
}) {
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

      <SectorTrend points={history} />

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
  const [sectors, history] = await Promise.all([getSectors(), getSectorHistory(HISTORY_DAYS)]);

  const historyBySector = new Map<string, SectorHistoryPoint[]>();
  for (const point of history) {
    historyBySector.set(point.sector, [...(historyBySector.get(point.sector) ?? []), point]);
  }

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
              <SectorEntry
                key={sector.sector}
                sector={sector}
                index={index}
                history={historyBySector.get(sector.sector) ?? []}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
