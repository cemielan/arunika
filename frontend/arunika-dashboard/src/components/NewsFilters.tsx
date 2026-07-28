"use client";

import { useLayoutEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  Button,
  Popover,
  RangeCalendar,
  Tag,
  TagGroup,
  Typography,
  cn,
} from "@heroui/react";
import type { Selection } from "react-aria-components";
import {
  CalendarDate,
  getLocalTimeZone,
  parseDate,
  today,
} from "@internationalized/date";
import {
  Bitcoin,
  CalendarRange,
  Cpu,
  Landmark,
  LineChart,
  Newspaper,
  Package,
  TrendingUp,
  Vote,
  X,
} from "lucide-react";
import { CATEGORIES } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { useSpringVector } from "@/app/hooks/useSpringVector";

const CATEGORY_ICONS: Record<string, typeof Newspaper> = {
  Politics: Vote,
  Economy: LineChart,
  Markets: TrendingUp,
  Banking: Landmark,
  Technology: Cpu,
  Commodities: Package,
  Crypto: Bitcoin,
};

type NewsFiltersProps = {
  category?: string;
  from?: string;
  to?: string;
  sortBy?: string;
};

type NextParams = Partial<
  Pick<NewsFiltersProps, "category" | "from" | "to" | "sortBy">
>;

/** Client-side filter bar for the news feed: category (HeroUI TagGroup), sort
 * order (spring-driven sliding toggle), and a published-date range (HeroUI
 * RangeCalendar in a popover). Every change updates the URL's search params
 * so the server component re-fetches with the new filter. */
export function NewsFilters({ category, from, to, sortBy }: NewsFiltersProps) {
  const router = useRouter();
  const pathname = usePathname();

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

  const { values, velocities } = useSpringVector([pillTarget.left, pillTarget.width]);
  const [animLeft, animWidth] = values;
  const stretch = Math.min(Math.abs(velocities[0]) / 900, 0.12);

  function navigate(next: NextParams) {
    const merged = { category, from, to, sortBy, ...next };
    const params = new URLSearchParams();
    if (merged.category) params.set("category", merged.category);
    if (merged.from) params.set("from", merged.from);
    if (merged.to) params.set("to", merged.to);
    if (merged.sortBy && merged.sortBy !== "date")
      params.set("sortBy", merged.sortBy);
    const query = params.toString();
    router.push(query ? `${pathname}?${query}` : pathname);
  }

  function handleCategorySelectionChange(keys: Selection) {
    if (keys === "all") {
      return;
    }
    const [key] = Array.from(keys);
    navigate({ category: key && key !== "all" ? String(key) : undefined });
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

  const rangeLabel =
    from && to
      ? `${formatDateOnly(from)} – ${formatDateOnly(to)}`
      : "Date range";

  return (
    <div className="flex flex-col gap-4">
      <div className="overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        <TagGroup
          aria-label="Filter by category"
          selectionMode="single"
          disallowEmptySelection
          selectedKeys={new Set([category ?? "all"])}
          onSelectionChange={handleCategorySelectionChange}
        >
          <TagGroup.List>
            <Tag id="all">
              <Newspaper className="size-3.5" />
              All
            </Tag>
            {CATEGORIES.map((c) => {
              const Icon = CATEGORY_ICONS[c] ?? Newspaper;
              return (
                <Tag key={c} id={c}>
                  <Icon className="size-3.5" />
                  {c}
                </Tag>
              );
            })}
          </TagGroup.List>
        </TagGroup>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        {/* Fluid sort toggle */}
        <div
          ref={trackRef}
          className="relative flex items-center gap-1 rounded-full bg-surface-secondary p-1"
        >
          <span
            aria-hidden
            className="absolute inset-y-1 rounded-full bg-accent shadow-sm will-change-transform"
            style={{
              left: animLeft,
              width: animWidth,
              transform: `scaleX(${1 + stretch})`,
            }}
          />
          <button
            ref={dateBtnRef}
            type="button"
            onClick={() => navigate({ sortBy: "date" })}
            className={cn(
              "relative z-10 rounded-full px-3 py-1.5 text-xs font-medium transition-colors duration-200",
              !isImpact ? "text-accent-foreground" : "text-muted hover:text-foreground",
            )}
          >
            Newest first
          </button>
          <button
            ref={impactBtnRef}
            type="button"
            onClick={() => navigate({ sortBy: "impact" })}
            className={cn(
              "relative z-10 rounded-full px-3 py-1.5 text-xs font-medium transition-colors duration-200",
              isImpact ? "text-accent-foreground" : "text-muted hover:text-foreground",
            )}
          >
            Highest impact
          </button>
        </div>

        <Popover.Root isOpen={isRangeOpen} onOpenChange={setIsRangeOpen}>
          <Popover.Trigger>
            <Button variant="outline" size="sm" className="gap-1.5!">
              <CalendarRange className="size-4 shrink-0" />
              <span className="max-w-[120px] truncate sm:max-w-none">{rangeLabel}</span>
              {from && to && (
                <X
                  className="size-3.5 shrink-0 text-muted hover:text-foreground"
                  onClick={(event) => {
                    event.stopPropagation();
                    clearRange();
                  }}
                />
              )}
            </Button>
          </Popover.Trigger>
          <Popover.Content placement="bottom start">
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
                    {(day) => (
                      <RangeCalendar.HeaderCell>{day}</RangeCalendar.HeaderCell>
                    )}
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
                  <Button
                    variant="primary"
                    size="sm"
                    onPress={applyRange}
                    isDisabled={!range}
                  >
                    Apply
                  </Button>
                </div>
              </div>
            </Popover.Dialog>
          </Popover.Content>
        </Popover.Root>
      </div>
    </div>
  );
}