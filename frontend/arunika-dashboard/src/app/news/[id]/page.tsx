import Link from "next/link";
import { notFound } from "next/navigation";
import { getArticleDetail } from "@/lib/api";
import { CategoryBadge, ImpactBadge, SentimentBadge, formatDate } from "@/components/badges";

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
        <Link href="/news" className="text-sm font-medium text-blue-600 hover:underline dark:text-blue-400">
          &larr; Back to news feed
        </Link>
      </div>

      <div className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <CategoryBadge category={article.category} />
          <SentimentBadge sentiment={article.sentiment} />
          <ImpactBadge score={article.impactScore} />
        </div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
          {article.title}
        </h1>
        <p className="text-sm text-zinc-500 dark:text-zinc-400">
          {article.source} &middot; {formatDate(article.publishedAt)} &middot;{" "}
          <a href={article.url} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline dark:text-blue-400">
            View original
          </a>
        </p>
      </div>

      {article.summary && (
        <div className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-2 text-sm font-semibold text-zinc-500 dark:text-zinc-400">Summary</h2>
          <p className="text-zinc-800 dark:text-zinc-200">{article.summary}</p>
        </div>
      )}

      {article.impactRationale && (
        <div className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-2 text-sm font-semibold text-zinc-500 dark:text-zinc-400">Impact rationale</h2>
          <p className="text-zinc-800 dark:text-zinc-200">{article.impactRationale}</p>
        </div>
      )}

      {article.sectors.length > 0 && (
        <div className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 text-sm font-semibold text-zinc-500 dark:text-zinc-400">Sector impact</h2>
          <ul className="flex flex-col gap-2">
            {article.sectors.map((s) => (
              <li key={s.sector} className="flex items-center justify-between text-sm">
                <span className="text-zinc-800 dark:text-zinc-200">{s.sector}</span>
                <span
                  className={`font-medium ${
                    s.direction === "Positive"
                      ? "text-emerald-600 dark:text-emerald-400"
                      : s.direction === "Negative"
                        ? "text-red-600 dark:text-red-400"
                        : "text-zinc-500 dark:text-zinc-400"
                  }`}
                >
                  {s.direction} &middot; {s.magnitude}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {article.keywords.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {article.keywords.map((keyword) => (
            <span
              key={keyword}
              className="inline-flex items-center rounded-full bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-600 dark:bg-zinc-800 dark:text-zinc-300"
            >
              #{keyword}
            </span>
          ))}
        </div>
      )}

      {article.enrichmentStatus !== "Completed" && (
        <div className="rounded-lg border border-dashed border-amber-300 bg-amber-50 p-4 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-900/20 dark:text-amber-300">
          AI enrichment status: {article.enrichmentStatus}. Some fields above may be unavailable.
        </div>
      )}

      {article.duplicates.length > 0 && (
        <div className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 text-sm font-semibold text-zinc-500 dark:text-zinc-400">
            Also reported by
          </h2>
          <ul className="flex flex-col gap-2">
            {article.duplicates.map((d) => (
              <li key={d.id}>
                <Link href={`/news/${d.id}`} className="text-sm text-blue-600 hover:underline dark:text-blue-400">
                  {d.title}
                </Link>
                <span className="ml-2 text-xs text-zinc-400 dark:text-zinc-500">{d.source}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </article>
  );
}
