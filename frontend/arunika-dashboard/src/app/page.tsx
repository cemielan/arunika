import Link from "next/link";
import { Card, Chip, Typography } from "@heroui/react";
import { getBriefing } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { ImpactBadge } from "@/components/badges";
import { MarketPulse } from "@/components/MarketPulse";

export default async function Home() {
  const briefing = await getBriefing();
  const { marketPulse } = briefing;

  return (
    <div className="flex flex-col gap-8">
      <div>
        <Typography.Paragraph size="sm" color="muted">
          {briefing.date || "Today"}
        </Typography.Paragraph>
        <Typography.Heading level={1} className="text-3xl">
          Today&apos;s Briefing
        </Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Top market-moving stories, ranked by impact score.
        </Typography.Paragraph>
      </div>

      <MarketPulse pulse={marketPulse} />

      <div>
        <Typography.Heading level={2} className="text-xl">
          Most Impactful Stories
        </Typography.Heading>
        <Typography.Paragraph size="sm" color="muted" className="mt-1">
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
        <ol className="flex flex-col gap-3">
          {briefing.topStories.map((story, index) => (
            <li key={story.articleId}>
              <Link href={`/news/${story.articleId}`} className="block">
                <Card
                  variant="default"
                  className="transition-colors hover:bg-surface-secondary"
                >
                  <div className="flex items-start gap-4">
                    <span className="mt-0.5 text-sm font-semibold text-muted">{index + 1}</span>
                    <div className="flex flex-1 flex-col gap-2">
                      <Typography.Paragraph weight="medium">{story.title}</Typography.Paragraph>
                      <div className="flex flex-wrap items-center gap-2">
                        <ImpactBadge score={story.impactScore} />
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


