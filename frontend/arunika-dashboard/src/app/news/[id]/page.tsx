import Link from "next/link";
import { notFound } from "next/navigation";
import { getArticleDetail } from "@/lib/api";
import {
  Dateline,
  ImpactMeter,
  Rubric,
  SectionRule,
  SentimentMark,
} from "@/components/editorial";

type ArticleDetailPageProps = {
  params: Promise<{ id: string }>;
};

const DIRECTION_TONE: Record<string, string> = {
  Positive: "text-success",
  Negative: "text-danger",
};

/**
 * The article page is the one screen that is pure reading, so it drops the
 * card stack entirely: a masthead, the headline in the display face, then
 * ruled sections down a single measure.
 */
export default async function ArticleDetailPage({ params }: ArticleDetailPageProps) {
  const { id } = await params;
  const article = await getArticleDetail(id);

  if (!article) {
    notFound();
  }

  return (
    <article className="mx-auto flex w-full max-w-3xl flex-col gap-10">
      <header className="editorial-reveal flex flex-col gap-4">
        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <Link
            href="/news"
            className="group -m-2 inline-flex items-center gap-1.5 p-2 text-muted transition-colors hover:text-foreground"
          >
            <span className="inline-block leading-none">&larr;</span>
            <Rubric>Back to the feed</Rubric>
          </Link>
          {article.category && <Rubric>{article.category}</Rubric>}
        </div>

        <div className="editorial-rule-strong" />

        <h1 className="editorial-display text-3xl text-foreground sm:text-5xl">
          {article.title}
        </h1>

        <Dateline
          source={article.source}
          publishedAt={article.publishedAt}
          variant="full"
        />

        <div className="flex flex-wrap items-center gap-x-5 gap-y-3 border-t border-border pt-4">
          <ImpactMeter score={article.impactScore} />
          <SentimentMark sentiment={article.sentiment} />
          {article.sentimentConfidence !== null && (
            <Rubric>
              Confidence{" "}
              <span className="font-mono text-foreground">
                {Math.round(article.sentimentConfidence * 100)}%
              </span>
            </Rubric>
          )}
          <a
            href={article.url}
            target="_blank"
            rel="noopener noreferrer"
            className="group ml-auto inline-flex items-center gap-1.5 text-accent"
          >
            <Rubric className="text-accent!">View original</Rubric>
            <span className="editorial-arrow inline-block leading-none">&rarr;</span>
          </a>
        </div>
      </header>

      {article.enrichmentStatus !== "Completed" && (
        <div className="editorial-panel border-warning! p-4">
          <p className="text-sm text-warning">
            AI enrichment status: {article.enrichmentStatus}. Some fields below may be unavailable.
          </p>
        </div>
      )}

      {article.summary && (
        <section className="editorial-reveal flex flex-col gap-4" style={{ animationDelay: "80ms" }}>
          <SectionRule label="Summary" />
          <p className="editorial-dropcap text-[0.9375rem] leading-[1.75] text-foreground/90 sm:text-base">
            {article.summary}
          </p>
        </section>
      )}

      {article.impactRationale && (
        <section className="editorial-reveal flex flex-col gap-4" style={{ animationDelay: "120ms" }}>
          <SectionRule label="Why it matters" />
          <p className="border-l border-border pl-4 text-[0.9375rem] leading-relaxed text-muted sm:text-base">
            {article.impactRationale}
          </p>
        </section>
      )}

      {article.sectors.length > 0 && (
        <section className="editorial-reveal flex flex-col gap-4" style={{ animationDelay: "160ms" }}>
          <SectionRule label="Sector impact" />
          <dl className="flex flex-col">
            {article.sectors.map((sector) => (
              <div
                key={sector.sector}
                className="flex items-center justify-between gap-4 border-t border-border py-3"
              >
                <dt className="editorial-display text-base text-foreground">{sector.sector}</dt>
                <dd className="flex items-center gap-3">
                  <Rubric className={DIRECTION_TONE[sector.direction] ?? "text-muted"}>
                    {sector.direction}
                  </Rubric>
                  <span className="font-mono text-xs tabular-nums text-foreground">
                    {sector.magnitude}
                  </span>
                </dd>
              </div>
            ))}
          </dl>
        </section>
      )}

      {article.keywords.length > 0 && (
        <section className="editorial-reveal flex flex-col gap-4" style={{ animationDelay: "200ms" }}>
          <SectionRule label="Keywords" />
          <div className="flex flex-wrap gap-2">
            {article.keywords.map((keyword) => (
              <span key={keyword} className="editorial-tag cursor-default">
                {keyword}
              </span>
            ))}
          </div>
        </section>
      )}

      {article.duplicates.length > 0 && (
        <section className="editorial-reveal flex flex-col gap-4" style={{ animationDelay: "240ms" }}>
          <SectionRule label="Also reported by" />
          <ul className="flex flex-col">
            {article.duplicates.map((duplicate) => (
              <li key={duplicate.id} className="border-t border-border">
                <Link href={`/news/${duplicate.id}`} className="group flex flex-col gap-1 py-3">
                  <span className="editorial-display text-base text-foreground">
                    <span className="editorial-link-underline">{duplicate.title}</span>
                  </span>
                  <Rubric>{duplicate.source}</Rubric>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}
    </article>
  );
}
