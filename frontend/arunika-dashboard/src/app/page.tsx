export const dynamic = "force-dynamic";

import Link from "next/link";
import { getBriefing, type Briefing } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { MarketPulse } from "@/components/MarketPulse";
import { StoryColumn, StoryLead, StoryRow } from "@/components/BriefingStories";
import { PageHeader, Rubric, SectionRule } from "@/components/editorial";

/**
 * The briefing reads as a newsletter issue rather than a dashboard list: a
 * masthead, a lede with the AI executive summary, one lead story, a column
 * grid, then the tail of the ranking. Nothing here needs client JavaScript —
 * the hover states, the staggered entrance and the "Why it matters"
 * disclosures are CSS and native <details>, so the page stays a server
 * component.
 */

const RISK_TONE: Record<string, string> = {
  High: "text-danger",
  Medium: "text-warning",
  Low: "text-success",
};

// Mirrors BriefingController.TopStoryCount — the API sends the stories but not
// the cap, and on a quiet window fewer than this arrive.
const TOP_STORY_COUNT = 10;

/**
 * Readers arriving cold ask what a briefing is and where it comes from. The
 * masthead lede answers both in a sentence; this carries the longer version for
 * anyone who wants it, without spending a tour or an onboarding step on it.
 *
 * A floating panel rather than a hover tooltip, and built on native <details>:
 * hover has no touch equivalent, so a CSS-only tooltip would be unreachable on
 * a phone. <details> opens on tap, click and keyboard alike, and keeps the page
 * a server component. The trade-off is that it closes on a second click of the
 * trigger rather than on an outside click — dismissing on outside click would
 * mean shipping JavaScript for it.
 */
function Colophon({ windowDays }: { windowDays: number }) {
  return (
    // z-30 on this wrapper, not just on the panel: the sections below are also
    // .editorial-reveal, and their entrance animation gives each one its own
    // stacking context, so a z-index set inside this one can't reach past them.
    // Lifting the whole disclosure is what keeps the open panel above the lede.
    <div className="relative z-30">
      <details className="editorial-disclosure group relative inline-block">
        <summary className="-my-2 inline-flex items-center gap-1.5 py-2 text-muted transition-colors hover:text-foreground">
          <Rubric className="text-inherit!">How this briefing is made</Rubric>
          <span className="editorial-disclosure-sign inline-block leading-none">+</span>
        </summary>

        <div className="absolute top-full left-0 z-30 mt-2.5 w-[min(88vw,34rem)] border border-border bg-background p-5 shadow-[0_12px_32px_-12px_rgb(0_0_0/0.35)] sm:p-6">
          <span
            className="absolute -top-[6.5px] left-7 size-3 rotate-45 border-t border-l border-border bg-background"
            aria-hidden
          />

          <div className="text-sm leading-relaxed text-muted">
            <p>
              <strong className="font-medium text-foreground">One -</strong> Arunika reads
              every story that reached the{" "}
              <Link href="/news" className="text-accent hover:underline">
                news feed
              </Link>{" "}
              in the last {windowDays} days.
            </p>
            <p className="mt-2">
              <strong className="font-medium text-foreground">Two -</strong> Each story is
              scored 0–100 for market impact. Stories about the same event are grouped,
              and the {TOP_STORY_COUNT} highest are kept.
            </p>
            <p className="mt-2">
              <strong className="font-medium text-foreground">Three -</strong>  AI writes the
              summary at the top of the page and the &ldquo;Why it matters&rdquo; note under
              each story.
            </p>
            <p className="mt-3 border-t border-border pt-3">
              The briefing is rebuilt every morning. The {windowDays}-day window rolls
              forward with it, so a story stays in contention until it ages out.
            </p>
          </div>
        </div>
      </details>
    </div>
  );
}

/** Long-form dateline for the masthead, e.g. "Wednesday, 10 September 2026". */
function formatMastheadDate(dateOnly: string): string {
  if (!dateOnly) return "";
  const [year, month, day] = dateOnly.split("-").map(Number);
  if (!year || !month || !day) return dateOnly;

  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-GB", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  });
}

/** Short dateline for the archive pager, e.g. "Wed 10 Sep". */
function formatPagerDate(dateOnly: string): string {
  const [year, month, day] = dateOnly.split("-").map(Number);
  if (!year || !month || !day) return dateOnly;

  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "short",
    timeZone: "UTC",
  });
}

/**
 * Steps through past briefings one published day at a time. Plain links to
 * `/?date=`, so every archived issue has its own shareable URL and the page
 * stays a server component.
 */
function ArchivePager({ briefing }: { briefing: Briefing }) {
  const { previousBriefingDate, nextBriefingDate, isArchive } = briefing;
  if (!previousBriefingDate && !isArchive) {
    return null;
  }

  return (
    <nav aria-label="Briefing archive" className="flex items-center gap-6">
      {previousBriefingDate ? (
        <Link href={`/?date=${previousBriefingDate}`} className="group inline-flex items-center gap-2 text-muted transition-colors hover:text-foreground">
          <span className="inline-block leading-none">&larr;</span>
          <Rubric className="text-inherit!">{formatPagerDate(previousBriefingDate)}</Rubric>
        </Link>
      ) : (
        <span />
      )}

      {isArchive && (
        <Link
          href={nextBriefingDate ? `/?date=${nextBriefingDate}` : "/"}
          className="group inline-flex items-center gap-2 text-accent"
        >
          <Rubric className="text-accent!">
            {nextBriefingDate ? formatPagerDate(nextBriefingDate) : "Today"}
          </Rubric>
          <span className="editorial-arrow inline-block leading-none">&rarr;</span>
        </Link>
      )}
    </nav>
  );
}

const DATE_PARAM = /^\d{4}-\d{2}-\d{2}$/;

export default async function Home({ searchParams }: { searchParams: Promise<{ date?: string }> }) {
  const { date } = await searchParams;
  // Anything that isn't a plain yyyy-MM-dd falls back to today rather than a 400 from the API.
  const briefing = await getBriefing(date && DATE_PARAM.test(date) ? date : undefined);
  const { marketPulse, topStories } = briefing;
  const executiveSummary = briefing.executiveSummary;

  const [lead, ...rest] = topStories;
  const columns = rest.slice(0, 3);
  const remainder = rest.slice(3);

  const windowDays = briefing.windowDays || 7;

  const windowNote =
    marketPulse.totalArticles > 0 && briefing.rangeStart && briefing.rangeEnd
      ? `${formatDateOnly(briefing.rangeStart)} – ${formatDateOnly(briefing.rangeEnd)}`
      : `Last ${windowDays} days`;

  return (
    <div className="flex flex-col gap-10 sm:gap-14">
      {/* Header, archive pager and colophon form one block, so the controls
          read as belonging to the masthead instead of floating below it. */}
      <div className="flex flex-col gap-6">
        <PageHeader
          kicker={formatMastheadDate(briefing.date) || "Today"}
          meta={`${marketPulse.totalArticles} stories · ${windowNote}`}
          title={briefing.isArchive ? "From the Archive" : "Today's Briefing"}
          lede={
            briefing.isArchive
              ? "This briefing as it was published that day: the same ranking, summary and market pulse."
              : `The ${TOP_STORY_COUNT} highest-impact stories of the past ${windowDays} days, ranked and summarised by AI. Rebuilt every morning.`
          }
        />

        <div className="editorial-reveal relative z-30 flex flex-wrap items-center justify-between gap-x-6 gap-y-3 border-t border-border pt-4">
          <ArchivePager briefing={briefing} />
          <Colophon windowDays={windowDays} />
        </div>
      </div>

      {(executiveSummary || marketPulse.totalArticles > 0) && (
        <section className="editorial-reveal grid gap-8 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] lg:gap-12">
          {executiveSummary ? (
            <div className="flex flex-col gap-4">
              <SectionRule label="The lede" note="AI-generated" />

              <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
                {briefing.overallSentiment && (
                  <Rubric>
                    Outlook <span className="text-foreground">{briefing.overallSentiment}</span>
                  </Rubric>
                )}
                {briefing.riskLevel && (
                  <Rubric>
                    Risk{" "}
                    <span className={RISK_TONE[briefing.riskLevel] ?? "text-foreground"}>
                      {briefing.riskLevel}
                    </span>
                  </Rubric>
                )}
              </div>

              <p
                className="editorial-dropcap max-w-[68ch] whitespace-pre-line text-[0.9375rem] leading-[1.75] text-foreground/90 sm:text-base"
              >
                {executiveSummary}
              </p>
            </div>
          ) : (
            <div />
          )}

          <MarketPulse pulse={marketPulse} />
        </section>
      )}

      {topStories.length === 0 ? (
        <section className="flex flex-col gap-4">
          <SectionRule label="Most impactful stories" />
          <div className="border border-dashed border-border p-8 text-center">
            <p className="text-sm text-muted">
              No enriched stories yet for this window. Check back once the news pipeline has
              run, or browse the{" "}
              <Link href="/news" className="font-medium text-accent hover:underline">
                full news feed
              </Link>
              .
            </p>
          </div>
        </section>
      ) : (
        <>
          <section className="flex flex-col gap-6">
            <SectionRule label="Lead story" note={`Highest impact of ${marketPulse.totalArticles}`} />
            <StoryLead story={lead} />
          </section>

          {columns.length > 0 && (
            <section className="flex flex-col gap-6">
              <SectionRule label="Also this week" />
              <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 lg:gap-10">
                {columns.map((story, index) => (
                  <StoryColumn key={story.articleId} story={story} index={index + 1} />
                ))}
              </div>
            </section>
          )}

          {remainder.length > 0 && (
            <section className="flex flex-col gap-4">
              <SectionRule label="The rest of the ranking" />
              <div className="border-b border-border">
                {remainder.map((story, index) => (
                  <StoryRow key={story.articleId} story={story} index={index + 4} />
                ))}
              </div>
            </section>
          )}
        </>
      )}

      <footer className="editorial-reveal flex flex-col gap-4 pb-2">
        <div className="editorial-rule-strong" />
        <Link href="/news" className="group inline-flex items-center gap-2 self-start text-accent">
          <Rubric className="text-accent!">Browse the full news feed</Rubric>
          <span className="editorial-arrow inline-block leading-none">&rarr;</span>
        </Link>
      </footer>
    </div>
  );
}
