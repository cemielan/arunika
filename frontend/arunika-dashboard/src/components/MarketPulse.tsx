import { Card, Chip, Typography } from "@heroui/react";
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

/** Dashboard hero: overall Bullish / Bearish / Neutral conclusion for the briefing window, with a legend breakdown. */
export function MarketPulse({ pulse }: MarketPulseProps) {
  const meta = SENTIMENT_META[pulse.sentiment];
  const { Icon } = meta;

  const rows: { key: MarketPulseData["sentiment"]; count: number }[] = [
    { key: "Bullish", count: pulse.bullishCount },
    { key: "Neutral", count: pulse.neutralCount },
    { key: "Bearish", count: pulse.bearishCount },
  ];

  return (
    <Card variant="default" className="gap-3! p-4! sm:gap-6! sm:p-6!">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 sm:gap-4">
          <div
            className={
              "flex size-10 shrink-0 items-center justify-center rounded-2xl sm:size-14 " +
              (meta.color === "success"
                ? "bg-success/15 text-success"
                : meta.color === "danger"
                  ? "bg-danger/15 text-danger"
                  : "bg-default text-muted")
            }
          >
            <Icon className="size-5 sm:size-7" />
          </div>
          <div className="flex flex-col">
            <Typography.Paragraph size="xs" color="muted" className="uppercase tracking-wide">
              This week&apos;s conclusion
            </Typography.Paragraph>
            <div className="flex items-center gap-2">
              <Typography.Heading level={2} className="text-xl sm:text-2xl">
                {meta.label}
              </Typography.Heading>
              <Chip color={meta.color} variant="soft" size="sm">
                {pulse.confidence}%
              </Chip>
            </div>
          </div>
        </div>
        <div className="flex flex-col items-end gap-0">
          <Typography.Paragraph size="xs" color="muted">
            Avg. impact
          </Typography.Paragraph>
          <Typography.Heading level={3} className="text-xl sm:text-3xl">
            {pulse.averageImpact}
          </Typography.Heading>
        </div>
      </div>

      <Typography.Paragraph size="sm" color="muted" className="hidden sm:block max-w-md">
        {CONCLUSION_COPY[pulse.sentiment]}
      </Typography.Paragraph>

      <div className="flex items-center gap-2">
        <div className="flex h-2 flex-1 overflow-hidden rounded-full bg-surface-secondary">
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
        <Typography.Paragraph size="xs" color="muted" className="shrink-0">
          {pulse.totalArticles} stories
        </Typography.Paragraph>
      </div>

      <div className="flex gap-3">
        {rows.map((row) => {
          const rowMeta = SENTIMENT_META[row.key];
          const RowIcon = rowMeta.Icon;
          return (
            <div key={row.key} className="flex items-center gap-1 text-xs text-muted sm:text-sm">
              <RowIcon
                className={
                  row.key === "Bullish"
                    ? "size-3 text-success sm:size-3.5"
                    : row.key === "Bearish"
                      ? "size-3 text-danger sm:size-3.5"
                      : "size-3 text-muted sm:size-3.5"
                }
              />
              <span className="font-medium text-foreground">{row.count}</span>
              <span className="hidden sm:inline">{rowMeta.label}</span>
            </div>
          );
        })}
      </div>
    </Card>
  );
}
