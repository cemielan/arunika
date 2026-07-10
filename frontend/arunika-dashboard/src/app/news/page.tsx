import Link from "next/link";
import { CATEGORIES, getNewsFeed } from "@/lib/api";
import { CategoryBadge, ImpactBadge, SentimentBadge, formatDate } from "@/components/badges";

type NewsPageProps = {
  searchParams: Promise<{ category?: string; page?: string }>;
};

function buildHref(category: string | undefined, page: number): string {
  const search = new URLSearchParams();
  if (category) search.set("category", category);
  if (page > 1) search.set("page", String(page));
  const query = search.toString();
  return query ? `/news?${query}` : "/news";
}

export default async function NewsPage({ searchParams }: NewsPageProps) {
  const { category, page: pageParam } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);

  const { items, meta } = await getNewsFeed({ category, page });

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
          News Feed
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          {meta.totalItems} article{meta.totalItems === 1 ? "" : "s"}
          {category ? ` in ${category}` : ""}.
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        <Link
          href="/news"
          className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
            !category
              ? "border-zinc-900 bg-zinc-900 text-white dark:border-zinc-50 dark:bg-zinc-50 dark:text-zinc-900"
              : "border-zinc-300 text-zinc-600 hover:border-zinc-400 dark:border-zinc-700 dark:text-zinc-400"
          }`}
        >
          All
        </Link>
        {CATEGORIES.map((c) => (
          <Link
            key={c}
            href={buildHref(c, 1)}
            className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
              category === c
                ? "border-zinc-900 bg-zinc-900 text-white dark:border-zinc-50 dark:bg-zinc-50 dark:text-zinc-900"
                : "border-zinc-300 text-zinc-600 hover:border-zinc-400 dark:border-zinc-700 dark:text-zinc-400"
            }`}
          >
            {c}
          </Link>
        ))}
      </div>

      {items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-zinc-300 bg-white p-8 text-center text-sm text-zinc-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-400">
          No articles found.
        </div>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((item) => (
            <li key={item.id}>
              <Link
                href={`/news/${item.id}`}
                className="flex flex-col gap-2 rounded-lg border border-zinc-200 bg-white p-4 transition-colors hover:border-zinc-300 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:border-zinc-700 dark:hover:bg-zinc-800/60"
              >
                <div className="flex flex-wrap items-center gap-2">
                  <CategoryBadge category={item.category} />
                  <SentimentBadge sentiment={item.sentiment} />
                  <ImpactBadge score={item.impactScore} />
                </div>
                <h2 className="font-medium text-zinc-900 dark:text-zinc-50">{item.title}</h2>
                {item.summary && (
                  <p className="line-clamp-2 text-sm text-zinc-600 dark:text-zinc-400">{item.summary}</p>
                )}
                <p className="text-xs text-zinc-400 dark:text-zinc-500">
                  {item.source} &middot; {formatDate(item.publishedAt)}
                </p>
              </Link>
            </li>
          ))}
        </ul>
      )}

      {meta.totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-zinc-200 pt-4 text-sm dark:border-zinc-800">
          {page > 1 ? (
            <Link href={buildHref(category, page - 1)} className="font-medium text-blue-600 hover:underline dark:text-blue-400">
              &larr; Previous
            </Link>
          ) : (
            <span />
          )}
          <span className="text-zinc-500 dark:text-zinc-400">
            Page {meta.page} of {meta.totalPages}
          </span>
          {page < meta.totalPages ? (
            <Link href={buildHref(category, page + 1)} className="font-medium text-blue-600 hover:underline dark:text-blue-400">
              Next &rarr;
            </Link>
          ) : (
            <span />
          )}
        </div>
      )}
    </div>
  );
}
