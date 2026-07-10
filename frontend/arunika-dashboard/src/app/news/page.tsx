import Link from "next/link";
import { Card, Typography, buttonVariants, cn } from "@heroui/react";
import { CATEGORIES, getNewsFeed } from "@/lib/api";
import { formatDate } from "@/lib/formatDate";
import { CategoryBadge, ImpactBadge, SentimentBadge } from "@/components/badges";

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
        <Typography.Heading level={1} className="text-3xl">
          News Feed
        </Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          {meta.totalItems} article{meta.totalItems === 1 ? "" : "s"}
          {category ? ` in ${category}` : ""}.
        </Typography.Paragraph>
      </div>

      <div className="flex flex-wrap gap-2">
        <Link
          href="/news"
          className={cn(
            "rounded-full px-3 py-1.5 text-xs font-medium transition-colors",
            !category
              ? "bg-accent text-accent-foreground"
              : "bg-surface-secondary text-muted hover:text-foreground",
          )}
        >
          All
        </Link>
        {CATEGORIES.map((c) => (
          <Link
            key={c}
            href={buildHref(c, 1)}
            className={cn(
              "rounded-full px-3 py-1.5 text-xs font-medium transition-colors",
              category === c
                ? "bg-accent text-accent-foreground"
                : "bg-surface-secondary text-muted hover:text-foreground",
            )}
          >
            {c}
          </Link>
        ))}
      </div>

      {items.length === 0 ? (
        <Card variant="transparent" className="items-center border border-dashed border-border text-center">
          <Typography.Paragraph color="muted">
            No articles found.
            {page > 1 && (
              <>
                {" "}
                <Link href={buildHref(category, 1)} className="font-medium text-accent hover:underline">
                  Back to page 1
                </Link>
                .
              </>
            )}
          </Typography.Paragraph>
        </Card>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((item) => (
            <li key={item.id}>
              <Link href={`/news/${item.id}`} className="block">
                <Card variant="default" className="transition-colors hover:bg-surface-secondary">
                  <div className="flex flex-wrap items-center gap-2">
                    <CategoryBadge category={item.category} />
                    <SentimentBadge sentiment={item.sentiment} />
                    <ImpactBadge score={item.impactScore} />
                  </div>
                  <Typography.Paragraph weight="medium">{item.title}</Typography.Paragraph>
                  {item.summary && (
                    <Typography.Paragraph size="sm" color="muted" truncate className="line-clamp-2">
                      {item.summary}
                    </Typography.Paragraph>
                  )}
                  <Typography.Paragraph size="xs" color="muted">
                    {item.source} &middot; {formatDate(item.publishedAt)}
                  </Typography.Paragraph>
                </Card>
              </Link>
            </li>
          ))}
        </ul>
      )}

      {meta.totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-border pt-4 text-sm">
          {page > 1 ? (
            <Link
              href={buildHref(category, page - 1)}
              className={buttonVariants({ variant: "outline", size: "sm" })}
            >
              &larr; Previous
            </Link>
          ) : (
            <span />
          )}
          <Typography.Paragraph size="sm" color="muted">
            Page {meta.page} of {meta.totalPages}
          </Typography.Paragraph>
          {page < meta.totalPages ? (
            <Link
              href={buildHref(category, page + 1)}
              className={buttonVariants({ variant: "outline", size: "sm" })}
            >
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

