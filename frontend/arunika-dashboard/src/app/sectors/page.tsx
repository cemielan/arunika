import { Card, Typography } from "@heroui/react";
import { getSectors } from "@/lib/api";

function sentimentColor(sentiment: string): string {
  switch (sentiment) {
    case "Bullish": return "text-green-600";
    case "Bearish": return "text-red-600";
    default: return "text-muted";
  }
}

export default async function SectorsPage() {
  const sectors = await getSectors();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Typography.Heading level={1} className="text-2xl sm:text-3xl">
          Sector Analysis
        </Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Sentiment and impact aggregated by sector over the last 7 days.
        </Typography.Paragraph>
      </div>

      {sectors.length === 0 ? (
        <Card variant="transparent" className="items-center border border-dashed border-border text-center p-8">
          <Typography.Paragraph color="muted">
            No sector data yet. Check back once articles have been enriched.
          </Typography.Paragraph>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sectors.map((s) => (
            <Card key={s.sector} variant="default" className="flex-col gap-3 p-4">
              <div className="flex items-center justify-between">
                <Typography.Paragraph weight="medium" className="text-sm">
                  {s.sector}
                </Typography.Paragraph>
                <Typography.Paragraph size="sm" className={sentimentColor(s.dominantSentiment)}>
                  {s.dominantSentiment}
                </Typography.Paragraph>
              </div>
              <div className="flex items-baseline gap-1">
                <Typography.Paragraph className="text-2xl font-bold">
                  {s.averageImpactScore}
                </Typography.Paragraph>
                <Typography.Paragraph size="sm" color="muted">
                  avg impact
                </Typography.Paragraph>
              </div>
              <div className="flex gap-3 text-xs text-muted">
                <span>📈 {s.bullishCount}</span>
                <span>📉 {s.bearishCount}</span>
                <span>➖ {s.neutralCount}</span>
              </div>
              <Typography.Paragraph size="xs" color="muted">
                {s.articleCount} article{s.articleCount === 1 ? "" : "s"}
              </Typography.Paragraph>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
