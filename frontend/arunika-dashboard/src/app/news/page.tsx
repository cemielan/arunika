import Link from "next/link";
import { getNewsFeed, type NewsListItem, type NewsSortBy } from "@/lib/api";
import { buildNewsHref } from "@/lib/newsHref";
import { NewsFilters } from "@/components/NewsFilters";
import { NewsPagination } from "@/components/NewsPagination";
import {
  Dateline,
  ImpactMeter,
  PageHeader,
  Rubric,
  SectionRule,
  SentimentMark,
} from "@/components/editorial";

type NewsPageProps = {
  searchParams: Promise<{
    category?: string;
    search?: string;
    from?: string;
    to?: string;
    sortBy?: string;
    page?: string;
  }>;
};

/**
 * The archive to the briefing's cover. It keeps the same typographic language
 * — serif headlines, rubric datelines, the impact meter — but stays a dense,
 * scannable list, because this is the page people filter and page through.
 */
function NewsRow({ item, index }: { item: NewsListItem; index: number }) {
  return (
    <article
      className="editorial-reveal border-t border-border"
      style={{ animationDelay: `${Math.min(400, 60 + index * 25)}ms` }}
    >
      <Link href={`/news/${item.id}`} className="group flex flex-col gap-3 py-5">
        <div className="flex items-start justify-between gap-4">
          <h3 className="editorial-display text-lg text-foreground sm:text-xl">
            <span className="editorial-link-underline">{item.title}</span>
          </h3>
          <ImpactMeter score={item.impactScore} className="hidden shrink-0 pt-1 sm:flex" />
        </div>

        {item.summary && (
          <p className="line-clamp-2 max-w-3xl text-sm leading-relaxed text-muted">
            {item.summary}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
          <Dateline
            source={item.source}
            publishedAt={item.publishedAt}
            category={item.category}
          />
          <SentimentMark sentiment={item.sentiment} />
          {item.sentiment === null && <Rubric>Pending</Rubric>}
          <ImpactMeter score={item.impactScore} className="sm:hidden" />
        </div>
      </Link>
    </article>
  );
}

export default async function NewsPage({ searchParams }: NewsPageProps) {
  const { category, search, from, to, sortBy, page: pageParam } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);
  const sort: NewsSortBy = sortBy === "impact" ? "impact" : "date";
  const filters = { category, search, from, to, sortBy: sort };

  const { items, meta } = await getNewsFeed({ category, search, from, to, sortBy: sort, page });

  const scope = [
    category ? category : null,
    from && to ? `${from} – ${to}` : null,
    search ? `“${search}”` : null,
  ].filter(Boolean);

  return (
    <div className="flex flex-col gap-10 sm:gap-12">
      <PageHeader
        kicker="The archive"
        meta={`${meta.totalItems} article${meta.totalItems === 1 ? "" : "s"}`}
        title="News Feed"
        lede="Every story Arunika has collected and enriched, newest first or ranked by impact."
      />

      <section className="editorial-reveal flex flex-col gap-5" style={{ animationDelay: "60ms" }}>
        <SectionRule label="Filter" note={scope.length > 0 ? scope.join(" · ") : undefined} />
        <NewsFilters category={category} search={search} from={from} to={to} sortBy={sort} />
      </section>

      <section className="flex flex-col gap-4">
        <SectionRule
          label={sort === "impact" ? "Ranked by impact" : "Newest first"}
          note={`Page ${meta.page} of ${Math.max(1, meta.totalPages)}`}
        />

        {items.length === 0 ? (
          <div className="editorial-panel p-8 text-center">
            <p className="text-sm text-muted">
              No articles match these filters.
              {page > 1 && (
                <>
                  {" "}
                  <Link
                    href={buildNewsHref(filters, 1)}
                    className="font-medium text-accent hover:underline"
                  >
                    Back to page 1
                  </Link>
                  .
                </>
              )}
            </p>
          </div>
        ) : (
          <div className="border-b border-border">
            {items.map((item, index) => (
              <NewsRow key={item.id} item={item} index={index} />
            ))}
          </div>
        )}
      </section>

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
