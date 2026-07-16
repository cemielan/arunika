"use client";

import { useRouter } from "next/navigation";
import { Pagination } from "@heroui/react";
import {buildNewsHref, type NewsFilterParams } from "@/lib/newsHref";

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

export function NewsPagination({
  page,
  totalPages,
  totalItems,
  pageSize,
  filters,
}: NewsPaginationProps) {
  const router = useRouter();

  if (totalPages <= 1) return null;

  const start = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);
  const pageNumbers = getPageNumbers(page, totalPages);

  function goTo(target: number) {
    if (target < 1 || target > totalPages || target === page) return;
    router.push(buildNewsHref(filters, target));
  }

  return (
    <Pagination>
      <Pagination.Summary>
        Showing {start}-{end} of {totalItems} result{totalItems === 1 ? "" : "s"}
      </Pagination.Summary>
      <Pagination.Content>
        <Pagination.Item>
          <Pagination.Previous isDisabled={page <= 1} onPress={() => goTo(page - 1)}>
            <Pagination.PreviousIcon />
            <span>Previous</span>
          </Pagination.Previous>
        </Pagination.Item>

        {pageNumbers.map((p, i) =>
          p === "ellipsis" ? (
            <Pagination.Item key={`ellipsis-${i}`}>
              <Pagination.Ellipsis />
            </Pagination.Item>
          ) : (
            <Pagination.Item key={p}>
              <Pagination.Link isActive={p === page} onPress={() => goTo(p)}>
                {p}
              </Pagination.Link>
            </Pagination.Item>
          ),
        )}

        <Pagination.Item>
          <Pagination.Next isDisabled={page >= totalPages} onPress={() => goTo(page + 1)}>
            <span>Next</span>
            <Pagination.NextIcon />
          </Pagination.Next>
        </Pagination.Item>
      </Pagination.Content>
    </Pagination>
  );
}