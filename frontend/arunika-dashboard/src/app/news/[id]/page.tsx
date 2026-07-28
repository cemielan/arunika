import Link from "next/link";
import { notFound } from "next/navigation";
import { Card, Chip, Link as HeroLink, Typography, buttonVariants } from "@heroui/react";
import { TrendingDown, TrendingUp } from "lucide-react";
import { getArticleDetail } from "@/lib/api";
import { formatDate } from "@/lib/formatDate";
import { CategoryBadge, ImpactBadge, SentimentBadge } from "@/components/badges";

type ArticleDetailPageProps = {
  params: Promise<{ id: string }>;
};

export default async function ArticleDetailPage({ params }: ArticleDetailPageProps) {
  const { id } = await params;
  const article = await getArticleDetail(id);

  if (!article) {
    notFound();
  }

  return (
    <article className="flex flex-col gap-6">
      <div>
        <Link href="/news" className={buttonVariants({ variant: "ghost", size: "sm" }) + " -ml-2 sm:ml-0"}>
          &larr; <span className="hidden sm:inline">Back to news feed</span><span className="sm:hidden">Back</span>
        </Link>
      </div>

      <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
        <CategoryBadge category={article.category} />
        <SentimentBadge sentiment={article.sentiment} />
        <ImpactBadge score={article.impactScore} />
      </div>
        <Typography.Heading level={1} className="text-xl sm:text-2xl">
          {article.title}
        </Typography.Heading>
        <Typography.Paragraph size="sm" color="muted" className="flex flex-wrap gap-x-1">
          <span>{article.source}</span>
          <span className="hidden sm:inline">&middot;</span>
          <span>{formatDate(article.publishedAt)}</span>
          <span>&middot;</span>
          <HeroLink href={article.url} target="_blank" rel="noopener noreferrer">
            View original
          </HeroLink>
        </Typography.Paragraph>
      </div>

      {article.summary && (
        <Card variant="default">
          <Typography.Paragraph size="sm" color="muted" weight="semibold" className="uppercase tracking-wide">
            Summary
          </Typography.Paragraph>
          <Typography.Paragraph>{article.summary}</Typography.Paragraph>
        </Card>
      )}

      {article.impactRationale && (
        <Card variant="default">
          <Typography.Paragraph size="sm" color="muted" weight="semibold" className="uppercase tracking-wide">
            Impact rationale
          </Typography.Paragraph>
          <Typography.Paragraph>{article.impactRationale}</Typography.Paragraph>
        </Card>
      )}

      {article.sectors.length > 0 && (
        <Card variant="default">
          <Typography.Paragraph size="sm" color="muted" weight="semibold" className="uppercase tracking-wide">
            Sector impact
          </Typography.Paragraph>
          <ul className="flex flex-col gap-2">
            {article.sectors.map((s) => {
              const isPositive = s.direction === "Positive";
              const isNegative = s.direction === "Negative";
              const Icon = isPositive ? TrendingUp : isNegative ? TrendingDown : null;
              return (
                <li key={s.sector} className="flex items-center justify-between text-sm">
                  <span className="text-foreground">{s.sector}</span>
                  <span
                    className={
                      "flex items-center gap-1 font-medium " +
                      (isPositive ? "text-success" : isNegative ? "text-danger" : "text-muted")
                    }
                  >
                    {Icon && <Icon className="size-3.5" />}
                    {s.direction} &middot; {s.magnitude}
                  </span>
                </li>
              );
            })}
          </ul>
        </Card>
      )}

      {article.keywords.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {article.keywords.map((keyword) => (
            <Chip key={keyword} color="default" variant="soft" size="sm">
              #{keyword}
            </Chip>
          ))}
        </div>
      )}

      {article.enrichmentStatus !== "Completed" && (
        <Card variant="default" className="border border-dashed border-warning bg-warning/10">
          <Typography.Paragraph size="sm" className="text-warning">
            AI enrichment status: {article.enrichmentStatus}. Some fields above may be unavailable.
          </Typography.Paragraph>
        </Card>
      )}

      {article.duplicates.length > 0 && (
        <Card variant="default">
          <Typography.Paragraph size="sm" color="muted" weight="semibold" className="uppercase tracking-wide">
            Also reported by
          </Typography.Paragraph>
          <ul className="flex flex-col gap-2">
            {article.duplicates.map((d) => (
              <li key={d.id}>
                <HeroLink href={`/news/${d.id}`}>{d.title}</HeroLink>
                <span className="ml-2 text-xs text-muted">{d.source}</span>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </article>
  );
}

