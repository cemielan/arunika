"use client";

import { startTransition, useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Button, Popover, RangeCalendar, Typography, cn } from "@heroui/react";
import {
  CalendarDate,
  getLocalTimeZone,
  parseDate,
  today,
} from "@internationalized/date";
import { CATEGORIES } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { useSpringVector } from "@/app/hooks/useSpringVector";

type NewsFiltersProps = {
  category?: string;
  search?: string;
  from?: string;
  to?: string;
  sortBy?: string;
};

type NextParams = Partial<
  Pick<NewsFiltersProps, "category" | "search" | "from" | "to" | "sortBy">
>;

/**
 * Client-side filter bar for the news feed: full-text search, category, sort
 * order and a published-date range. Every change updates the URL's search
 * params so the server component re-fetches with the new filter.
 *
 * The category row is plain typographic tags rather than icon pills — a row of
 * small pictograms was the loudest "generated template" signal on this page,
 * and the category names read faster without them.
 */
export function NewsFilters({ category, search, from, to, sortBy }: NewsFiltersProps) {
  const router = useRouter();
  const pathname = usePathname();

  const [searchInput, setSearchInput] = useState(search ?? "");
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  const handleSearchChange = useCallback(
    (value: string) => {
      setSearchInput(value);
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        const merged = { category, search: value || undefined, from, to, sortBy };
        const params = new URLSearchParams();
        if (merged.category) params.set("category", merged.category);
        if (merged.search) params.set("search", merged.search);
        if (merged.from) params.set("from", merged.from);
        if (merged.to) params.set("to", merged.to);
        if (merged.sortBy && merged.sortBy !== "date") params.set("sortBy", merged.sortBy);
        const query = params.toString();
        router.push(query ? `${pathname}?${query}` : pathname);
      }, 300);
    },
    [category, from, to, sortBy, pathname, router],
  );

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  // Sync search input when URL changes externally (browser back/forward).
  useEffect(() => {
    startTransition(() => setSearchInput(search ?? ""));
  }, [search]);

  const [range, setRange] = useState<{
    start: CalendarDate;
    end: CalendarDate;
  } | null>(from && to ? { start: parseDate(from), end: parseDate(to) } : null);
  const [isRangeOpen, setIsRangeOpen] = useState(false);

  const isImpact = sortBy === "impact";
  const trackRef = useRef<HTMLDivElement>(null);
  const dateBtnRef = useRef<HTMLButtonElement>(null);
  const impactBtnRef = useRef<HTMLButtonElement>(null);
  const [pillTarget, setPillTarget] = useState({ left: 0, width: 0 });

  useLayoutEffect(() => {
    const activeEl = isImpact ? impactBtnRef.current : dateBtnRef.current;
    const track = trackRef.current;
    if (activeEl && track) {
      const trackRect = track.getBoundingClientRect();
      const elRect = activeEl.getBoundingClientRect();
      setPillTarget({ left: elRect.left - trackRect.left, width: elRect.width });
    }
  }, [isImpact]);

  const { values } = useSpringVector([pillTarget.left, pillTarget.width]);
  const [animLeft, animWidth] = values;

  function navigate(next: NextParams) {
    const merged = { category, search, from, to, sortBy, ...next };
    const params = new URLSearchParams();
    if (merged.category) params.set("category", merged.category);
    if (merged.search) params.set("search", merged.search);
    if (merged.from) params.set("from", merged.from);
    if (merged.to) params.set("to", merged.to);
    if (merged.sortBy && merged.sortBy !== "date") params.set("sortBy", merged.sortBy);
    const query = params.toString();
    router.push(query ? `${pathname}?${query}` : pathname);
  }

  function applyRange() {
    if (!range) return;
    navigate({ from: range.start.toString(), to: range.end.toString() });
    setIsRangeOpen(false);
  }

  function clearRange() {
    setRange(null);
    navigate({ from: undefined, to: undefined });
    setIsRangeOpen(false);
  }

  const rangeLabel = from && to ? `${formatDateOnly(from)} – ${formatDateOnly(to)}` : "Date range";
  const activeCategory = category ?? "all";

  return (
    <div className="flex flex-col gap-5">
      <input
        value={searchInput}
        onChange={(e) => handleSearchChange(e.target.value)}
        placeholder="Search articles…"
        aria-label="Search articles"
        className="editorial-input text-base"
      />

      <div
        role="group"
        aria-label="Filter by category"
        className="flex flex-wrap gap-2"
      >
        <button
          type="button"
          aria-pressed={activeCategory === "all"}
          onClick={() => navigate({ category: undefined })}
          className="editorial-tag"
        >
          All
        </button>
        {CATEGORIES.map((c) => (
          <button
            key={c}
            type="button"
            aria-pressed={activeCategory === c}
            onClick={() => navigate({ category: c })}
            className="editorial-tag"
          >
            {c}
          </button>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-4">
        {/* Sort toggle: a sliding rule under the active label, not a filled pill. */}
        <div ref={trackRef} className="relative flex items-center gap-5 pb-1.5">
          <span
            aria-hidden
            className="absolute bottom-0 h-px bg-foreground will-change-transform"
            style={{ left: animLeft, width: animWidth }}
          />
          <button
            ref={dateBtnRef}
            type="button"
            onClick={() => navigate({ sortBy: "date" })}
            className={cn(
              "editorial-rubric transition-colors duration-200",
              !isImpact ? "text-foreground" : "text-muted hover:text-foreground",
            )}
          >
            Newest first
          </button>
          <button
            ref={impactBtnRef}
            type="button"
            onClick={() => navigate({ sortBy: "impact" })}
            className={cn(
              "editorial-rubric transition-colors duration-200",
              isImpact ? "text-foreground" : "text-muted hover:text-foreground",
            )}
          >
            Highest impact
          </button>
        </div>

        <div className="ml-auto flex items-center gap-3">
          <Popover.Root isOpen={isRangeOpen} onOpenChange={setIsRangeOpen}>
            <Popover.Trigger>
              <button type="button" className="editorial-tag">
                <span className="max-w-37.5 truncate">{rangeLabel}</span>
              </button>
            </Popover.Trigger>
            <Popover.Content placement="bottom end">
              <Popover.Dialog className="flex flex-col gap-3 p-3">
                <RangeCalendar.Root
                  value={range}
                  onChange={setRange}
                  minValue={today(getLocalTimeZone()).subtract({ days: 6 })}
                  maxValue={today(getLocalTimeZone())}
                >
                  <RangeCalendar.Header>
                    <RangeCalendar.NavButton slot="previous" />
                    <RangeCalendar.Heading />
                    <RangeCalendar.NavButton slot="next" />
                  </RangeCalendar.Header>
                  <RangeCalendar.Grid>
                    <RangeCalendar.GridHeader>
                      {(day) => <RangeCalendar.HeaderCell>{day}</RangeCalendar.HeaderCell>}
                    </RangeCalendar.GridHeader>
                    <RangeCalendar.GridBody>
                      {(date) => <RangeCalendar.Cell date={date} />}
                    </RangeCalendar.GridBody>
                  </RangeCalendar.Grid>
                </RangeCalendar.Root>
                <div className="flex items-center justify-between gap-2 border-t border-border pt-3">
                  <Typography.Paragraph size="xs" color="muted">
                    {range
                      ? `${range.start.toString()} – ${range.end.toString()}`
                      : "Pick a start and end date"}
                  </Typography.Paragraph>
                  <div className="flex gap-2">
                    <Button variant="ghost" size="sm" onPress={clearRange}>
                      Clear
                    </Button>
                    <Button variant="primary" size="sm" onPress={applyRange} isDisabled={!range}>
                      Apply
                    </Button>
                  </div>
                </div>
              </Popover.Dialog>
            </Popover.Content>
          </Popover.Root>

          {from && to && (
            <button
              type="button"
              onClick={clearRange}
              className="editorial-rubric text-muted transition-colors hover:text-foreground"
            >
              Clear dates
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
