import Link from "next/link";
import { Card, Typography } from "@heroui/react";
import { getNewsFeed, type NewsSortBy } from "@/lib/api";
import { formatDate } from "@/lib/formatDate";
import { buildNewsHref } from "@/lib/newsHref";
import { CategoryBadge, ImpactBadge, SentimentBadge } from "@/components/badges";
import { NewsFilters } from "@/components/NewsFilters";
import { NewsPagination } from "@/components/NewsPagination";

type NewsPageProps = {
  searchParams: Promise<{ category?: string; from?: string; to?: string; sortBy?: string; page?: string }>;
};

export default async function NewsPage({ searchParams }: NewsPageProps) {
  const { category, from, to, sortBy, page: pageParam } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);
  const sort: NewsSortBy = sortBy === "impact" ? "impact" : "date";
  const filters = { category, from, to, sortBy: sort };

  const { items, meta } = await getNewsFeed({ category, from, to, sortBy: sort, page });

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Typography.Heading level={1} className="text-2xl sm:text-3xl">
          News Feed
        </Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          {meta.totalItems} article{meta.totalItems === 1 ? "" : "s"}
          {category ? ` in ${category}` : ""}
          {from && to ? ` from ${from} to ${to}` : ""}.
        </Typography.Paragraph>
      </div>

      <NewsFilters category={category} from={from} to={to} sortBy={sort} />

      {items.length === 0 ? (
        <Card variant="transparent" className="items-center border border-dashed border-border text-center">
          <Typography.Paragraph color="muted">
            No articles found.
            {page > 1 && (
              <>
                {" "}
                <Link href={buildNewsHref(filters, 1)} className="font-medium text-accent hover:underline">
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

      <NewsPagination
        page={meta.page}
        totalPages={meta.totalPages}
        totalItems={meta.totalItems}
        pageSize={meta.pageSize}
        filters={filters}
      />
    </div>
  );
}