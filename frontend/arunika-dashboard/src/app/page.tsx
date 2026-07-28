import Link from "next/link";
import { Card, Chip, Typography } from "@heroui/react";
import { getBriefing } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { ImpactBadge } from "@/components/badges";
import { MarketPulse } from "@/components/MarketPulse";

function riskColor(risk: string | undefined): string {
  switch (risk) {
    case "High": return "text-red-600";
    case "Medium": return "text-amber-600";
    case "Low": return "text-green-600";
    default: return "text-muted";
  }
}

export default async function Home() {
  const briefing = await getBriefing();
  const { marketPulse } = briefing;

  return (
    <div className="flex flex-col gap-6 sm:gap-8">
      <div>
        <Typography.Paragraph size="sm" color="muted">
          {briefing.date || "Today"}
        </Typography.Paragraph>
        <Typography.Heading level={1} className="text-2xl sm:text-3xl">
          Today&apos;s Briefing
        </Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1 hidden sm:block">
          Top market-moving stories, ranked by impact score.
        </Typography.Paragraph>
      </div>

      {briefing.executiveSummary && (
        <Card variant="default" className="flex-col gap-3 p-4 sm:p-5">
          <div className="flex flex-wrap items-center gap-3">
            {briefing.overallSentiment && (
              <Chip color={briefing.overallSentiment === "Bullish" ? "success" : briefing.overallSentiment === "Bearish" ? "danger" : "default"} variant="primary" size="sm">
                {briefing.overallSentiment}
              </Chip>
            )}
            {briefing.riskLevel && (
              <Chip color="default" variant="soft" size="sm" className={riskColor(briefing.riskLevel)}>
                {briefing.riskLevel} risk
              </Chip>
            )}
            <Typography.Paragraph size="xs" color="muted">
              AI-generated
            </Typography.Paragraph>
          </div>
          <Typography.Paragraph className="text-sm leading-relaxed text-foreground/90 whitespace-pre-line">
            {briefing.executiveSummary}
          </Typography.Paragraph>
        </Card>
      )}

      <MarketPulse pulse={marketPulse} />

      <div>
        <Typography.Heading level={2} className="text-lg sm:text-xl">
          Most Impactful Stories
        </Typography.Heading>
        <Typography.Paragraph size="sm" color="muted" className="mt-1 hidden sm:block">
          {marketPulse.totalArticles > 0 ? (
            <>
              Ranked by impact score, out of {marketPulse.totalArticles} article
              {marketPulse.totalArticles === 1 ? "" : "s"} published{" "}
              {briefing.rangeStart && briefing.rangeEnd
                ? `${formatDateOnly(briefing.rangeStart)} – ${formatDateOnly(briefing.rangeEnd)}`
                : "recently"}{" "}
              (last {briefing.windowDays || 7} days).
            </>
          ) : (
            `Ranked by impact score across the last ${briefing.windowDays || 7} days.`
          )}
        </Typography.Paragraph>
      </div>

      {briefing.topStories.length === 0 ? (
        <Card variant="transparent" className="items-center border border-dashed border-border text-center">
          <Typography.Paragraph color="muted">
            No enriched stories yet for this window. Check back once the news pipeline has
            run, or browse the{" "}
            <Link href="/news" className="font-medium text-accent hover:underline">
              full news feed
            </Link>
            .
          </Typography.Paragraph>
        </Card>
      ) : (
        <ol className="flex flex-col gap-2 sm:gap-3">
          {briefing.topStories.map((story, index) => (
            <li key={story.articleId}>
              <Link href={`/news/${story.articleId}`} className="block">
                <Card
                  variant="default"
                  className="transition-colors hover:bg-surface-secondary"
                >
                  <div className="flex items-start gap-2 sm:gap-4">
                    <span className="mt-1 text-xs font-semibold text-muted sm:mt-0.5 sm:text-sm">
                      {index + 1}
                    </span>
                    <div className="flex flex-1 flex-col gap-1 sm:gap-2">
                      <div className="flex items-start justify-between gap-2">
                        <Typography.Paragraph weight="medium" className="text-sm sm:text-base">
                          {story.title}
                        </Typography.Paragraph>
                        <ImpactBadge score={story.impactScore} />
                      </div>
                      <div className="hidden flex-wrap items-center gap-2 sm:flex">
                        {story.sectors.map((sector) => (
                          <Chip key={sector} color="default" variant="soft" size="sm">
                            {sector}
                          </Chip>
                        ))}
                      </div>
                    </div>
                  </div>
                </Card>
              </Link>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}


