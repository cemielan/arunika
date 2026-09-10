import Link from "next/link";
import { buildNewsHref, type NewsFilterParams } from "@/lib/newsHref";
import { Rubric } from "@/components/editorial";

type NewsPaginationProps = {
  page: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  filters: NewsFilterParams;
};

/** Builds a compact page list with ellipses, e.g. [1, "ellipsis", 4, 5, 6, "ellipsis", 12] */
function getPageNumbers(page: number, totalPages: number): (number | "ellipsis")[] {
  const pages = new Set<number>([1, totalPages]);
  for (let p = page - 1; p <= page + 1; p++) {
    if (p >= 1 && p <= totalPages) pages.add(p);
  }
  const sorted = Array.from(pages).sort((a, b) => a - b);

  const result: (number | "ellipsis")[] = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) result.push("ellipsis");
    result.push(p);
    prev = p;
  }
  return result;
}

/**
 * Pager set as type rather than as a control strip: plain numerals, a rule
 * over the whole row, and worded Previous/Next instead of chevrons.
 *
 * These are real links, so the pager works without JavaScript and pages can be
 * opened in a new tab — the previous version pushed routes from click handlers
 * on buttons, which neither middle-click nor a crawler could follow.
 */
export function NewsPagination({
  page,
  totalPages,
  totalItems,
  pageSize,
  filters,
}: NewsPaginationProps) {
  if (totalPages <= 1) return null;

  const start = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);
  const pageNumbers = getPageNumbers(page, totalPages);

  return (
    <nav
      aria-label="Pagination"
      className="flex flex-wrap items-center justify-between gap-4 border-t border-border pt-5"
    >
      <Rubric>
        {start}–{end} of {totalItems}
      </Rubric>

      <div className="flex items-center gap-4">
        {page > 1 ? (
          <Link
            href={buildNewsHref(filters, page - 1)}
            rel="prev"
            className="editorial-rubric -my-2 py-2 text-muted transition-colors hover:text-foreground"
          >
            Previous
          </Link>
        ) : (
          <span className="editorial-rubric py-2 text-muted opacity-40">Previous</span>
        )}

        <div className="flex items-center gap-1">
          {pageNumbers.map((p, index) =>
            p === "ellipsis" ? (
              <span key={`ellipsis-${index}`} className="px-1 font-mono text-xs text-muted">
                &hellip;
              </span>
            ) : p === page ? (
              <span
                key={p}
                aria-current="page"
                className="border-b border-foreground px-2.5 py-2 font-mono text-xs tabular-nums text-foreground sm:px-2 sm:py-1"
              >
                {p}
              </span>
            ) : (
              <Link
                key={p}
                href={buildNewsHref(filters, p)}
                className="border-b border-transparent px-2.5 py-2 font-mono text-xs tabular-nums text-muted transition-colors hover:border-border hover:text-foreground sm:px-2 sm:py-1"
              >
                {p}
              </Link>
            ),
          )}
        </div>

        {page < totalPages ? (
          <Link
            href={buildNewsHref(filters, page + 1)}
            rel="next"
            className="editorial-rubric -my-2 py-2 text-muted transition-colors hover:text-foreground"
          >
            Next
          </Link>
        ) : (
          <span className="editorial-rubric py-2 text-muted opacity-40">Next</span>
        )}
      </div>
    </nav>
  );
}
