import type { MarketPulse as MarketPulseData } from "@/lib/api";
import { SENTIMENT_META } from "@/components/badges";

type MarketPulseProps = {
  pulse: MarketPulseData;
};

const CONCLUSION_COPY: Record<MarketPulseData["sentiment"], string> = {
  Bullish: "Markets are leaning risk-on this week, with upbeat stories outweighing the negative ones.",
  Bearish: "This week's news skews risk-off, with more downside-driving stories than positive ones.",
  Neutral: "No clear direction yet — bullish and bearish stories are roughly balanced this week.",
};

/**
 * The briefing's standing sidebar: one overall Bullish / Bearish / Neutral
 * conclusion for the window, the average impact, and the split it was drawn
 * from. Styled as a boxed rail rather than a dashboard card so it reads as a
 * sidebar beside the lede.
 */
export function MarketPulse({ pulse }: MarketPulseProps) {
  const meta = SENTIMENT_META[pulse.sentiment];
  const { Icon } = meta;

  const rows: { key: MarketPulseData["sentiment"]; count: number }[] = [
    { key: "Bullish", count: pulse.bullishCount },
    { key: "Neutral", count: pulse.neutralCount },
    { key: "Bearish", count: pulse.bearishCount },
  ];

  const tone =
    meta.color === "success" ? "text-success" : meta.color === "danger" ? "text-danger" : "text-foreground";

  return (
    <aside className="flex flex-col gap-5 border border-border bg-surface-secondary/40 p-5">
      <div className="editorial-rule">
        <span className="editorial-rubric text-muted">Market pulse</span>
      </div>

      <div className="flex items-start justify-between gap-4">
        <div>
          <div className={`flex items-center gap-2 ${tone}`}>
            <Icon className="size-6" />
            <span className="editorial-display text-3xl">{meta.label}</span>
          </div>
          <p className="editorial-rubric mt-2 text-muted">
            {pulse.confidence}% of {pulse.totalArticles} stories
          </p>
        </div>

        <div className="text-right">
          <p className="editorial-display text-3xl text-foreground tabular-nums">
            {pulse.averageImpact}
          </p>
          <p className="editorial-rubric mt-1 text-muted">Avg. impact</p>
        </div>
      </div>

      {/*
        Stacked split of the same window. Segments are separated by a 2px gap in
        the surface colour so neighbouring fills stay individually readable
        instead of merging into one band.
      */}
      <div className="flex h-2 gap-0.5" role="img" aria-label={`${pulse.bullishCount} bullish, ${pulse.neutralCount} neutral, ${pulse.bearishCount} bearish stories`}>
        {rows.map((row) => (
          <div
            key={row.key}
            className={
              "h-full first:rounded-l-full last:rounded-r-full " +
              (row.key === "Bullish" ? "bg-success" : row.key === "Bearish" ? "bg-danger" : "bg-default")
            }
            style={{
              width: pulse.totalArticles > 0 ? `${(row.count / pulse.totalArticles) * 100}%` : "0%",
            }}
          />
        ))}
      </div>

      <dl className="flex flex-col gap-2">
        {rows.map((row) => {
          const rowMeta = SENTIMENT_META[row.key];
          const RowIcon = rowMeta.Icon;
          const rowTone =
            row.key === "Bullish" ? "text-success" : row.key === "Bearish" ? "text-danger" : "text-muted";

          return (
            <div key={row.key} className="flex items-center gap-2 text-sm">
              <RowIcon className={`size-3.5 shrink-0 ${rowTone}`} />
              <dt className="editorial-rubric text-muted">{rowMeta.label}</dt>
              <dd className="ml-auto font-mono text-xs tabular-nums text-foreground">{row.count}</dd>
            </div>
          );
        })}
      </dl>

      <p className="text-sm leading-relaxed text-muted">{CONCLUSION_COPY[pulse.sentiment]}</p>
    </aside>
  );
}
