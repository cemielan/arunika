import type { MarketPulse as MarketPulseData } from "@/lib/api";
import { Rubric, SentimentMark } from "@/components/editorial";

type MarketPulseProps = {
  pulse: MarketPulseData;
};

const CONCLUSION_COPY: Record<MarketPulseData["sentiment"], string> = {
  Bullish: "Markets are leaning risk-on this week, with upbeat stories outweighing the negative ones.",
  Bearish: "This week's news skews risk-off, with more downside-driving stories than positive ones.",
  Neutral: "No clear direction yet — bullish and bearish stories are roughly balanced this week.",
};

const TONE: Record<MarketPulseData["sentiment"], string> = {
  Bullish: "text-success",
  Bearish: "text-danger",
  Neutral: "text-foreground",
};

const FILL: Record<MarketPulseData["sentiment"], string> = {
  Bullish: "bg-success",
  Bearish: "bg-danger",
  Neutral: "bg-default",
};

/**
 * The briefing's standing sidebar: one overall Bullish / Bearish / Neutral
 * conclusion for the window, the average impact, and the split it was drawn
 * from. Styled as a bordered rail rather than a dashboard card so it reads as
 * a sidebar beside the lede.
 */
export function MarketPulse({ pulse }: MarketPulseProps) {
  const rows: { key: MarketPulseData["sentiment"]; count: number }[] = [
    { key: "Bullish", count: pulse.bullishCount },
    { key: "Neutral", count: pulse.neutralCount },
    { key: "Bearish", count: pulse.bearishCount },
  ];

  return (
    <aside className="editorial-panel editorial-panel-filled flex flex-col gap-5 p-5">
      <div className="editorial-rule">
        <Rubric>Market pulse</Rubric>
      </div>

      <div className="flex items-start justify-between gap-4">
        <div>
          <p className={`editorial-display text-3xl ${TONE[pulse.sentiment]}`}>{pulse.sentiment}</p>
          <Rubric className="mt-2 block">
            {pulse.confidence}% of {pulse.totalArticles} stories
          </Rubric>
        </div>

        <div className="text-right">
          <p className="editorial-display text-3xl tabular-nums text-foreground">
            {pulse.averageImpact}
          </p>
          <Rubric className="mt-1 block">Avg. impact</Rubric>
        </div>
      </div>

      {/*
        Stacked split of the same window. Segments are separated by a 2px gap in
        the surface colour so neighbouring fills stay individually readable
        instead of merging into one band.
      */}
      <div
        className="flex h-2 gap-0.5"
        role="img"
        aria-label={`${pulse.bullishCount} bullish, ${pulse.neutralCount} neutral, ${pulse.bearishCount} bearish stories`}
      >
        {rows.map((row) => (
          <div
            key={row.key}
            className={`h-full first:rounded-l-full last:rounded-r-full ${FILL[row.key]}`}
            style={{
              width: pulse.totalArticles > 0 ? `${(row.count / pulse.totalArticles) * 100}%` : "0%",
            }}
          />
        ))}
      </div>

      <dl className="flex flex-col gap-2">
        {rows.map((row) => (
          <div key={row.key} className="flex items-center gap-2 text-sm">
            <dt>
              <SentimentMark sentiment={row.key} />
            </dt>
            <dd className="ml-auto font-mono text-xs tabular-nums text-foreground">{row.count}</dd>
          </div>
        ))}
      </dl>

      <p className="text-sm leading-relaxed text-muted">{CONCLUSION_COPY[pulse.sentiment]}</p>
    </aside>
  );
}
