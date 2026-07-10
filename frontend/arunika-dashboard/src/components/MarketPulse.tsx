import { Card, Chip, Typography } from "@heroui/react";
import type { MarketPulse as MarketPulseData } from "@/lib/api";
import { SENTIMENT_META } from "@/components/badges";

type MarketPulseProps = {
  pulse: MarketPulseData;
};

const CONCLUSION_COPY: Record<MarketPulseData["sentiment"], string> = {
  Bullish: "Markets are leaning risk-on today, with upbeat stories outweighing the negative ones.",
  Bearish: "Today's news skews risk-off, with more downside-driving stories than positive ones.",
  Neutral: "No clear direction yet — bullish and bearish stories are roughly balanced today.",
};

/** Dashboard hero: today's overall Bullish / Bearish / Neutral conclusion, with a legend breakdown. */
export function MarketPulse({ pulse }: MarketPulseProps) {
  const meta = SENTIMENT_META[pulse.sentiment];
  const { Icon } = meta;

  const rows: { key: MarketPulseData["sentiment"]; count: number }[] = [
    { key: "Bullish", count: pulse.bullishCount },
    { key: "Neutral", count: pulse.neutralCount },
    { key: "Bearish", count: pulse.bearishCount },
  ];

  return (
    <Card variant="default" className="p-6! gap-6!">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <div
            className={
              "flex size-14 shrink-0 items-center justify-center rounded-2xl " +
              (meta.color === "success"
                ? "bg-success/15 text-success"
                : meta.color === "danger"
                  ? "bg-danger/15 text-danger"
                  : "bg-default text-muted")
            }
          >
            <Icon className="size-7" />
          </div>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="xs" color="muted" className="uppercase tracking-wide">
              Today&apos;s conclusion
            </Typography.Paragraph>
            <div className="flex items-center gap-2">
              <Typography.Heading level={2} className="text-2xl">
                {meta.label}
              </Typography.Heading>
              <Chip color={meta.color} variant="soft" size="sm">
                {pulse.confidence}% confidence
              </Chip>
            </div>
            <Typography.Paragraph size="sm" color="muted" className="max-w-md">
              {CONCLUSION_COPY[pulse.sentiment]}
            </Typography.Paragraph>
          </div>
        </div>

        <div className="flex flex-col gap-1 sm:items-end">
          <Typography.Paragraph size="xs" color="muted">
            Avg. impact score
          </Typography.Paragraph>
          <Typography.Heading level={3} className="text-3xl">
            {pulse.averageImpact}
          </Typography.Heading>
          <Typography.Paragraph size="xs" color="muted">
            across {pulse.totalArticles} recent stories
          </Typography.Paragraph>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <div className="flex h-2 w-full overflow-hidden rounded-full bg-surface-secondary">
          {rows.map((row) => (
            <div
              key={row.key}
              className={
                row.key === "Bullish"
                  ? "bg-success"
                  : row.key === "Bearish"
                    ? "bg-danger"
                    : "bg-default"
              }
              style={{
                width: pulse.totalArticles > 0 ? `${(row.count / pulse.totalArticles) * 100}%` : 0,
              }}
            />
          ))}
        </div>

        <div className="flex flex-wrap gap-3">
          {rows.map((row) => {
            const rowMeta = SENTIMENT_META[row.key];
            const RowIcon = rowMeta.Icon;
            return (
              <div key={row.key} className="flex items-center gap-1.5 text-sm text-muted">
                <RowIcon
                  className={
                    row.key === "Bullish"
                      ? "size-3.5 text-success"
                      : row.key === "Bearish"
                        ? "size-3.5 text-danger"
                        : "size-3.5 text-muted"
                  }
                />
                <span className="font-medium text-foreground">{row.count}</span>
                <span>{rowMeta.label}</span>
              </div>
            );
          })}
        </div>
      </div>
    </Card>
  );
}
