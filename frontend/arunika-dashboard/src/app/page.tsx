export const dynamic = "force-dynamic";

import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { getBriefing } from "@/lib/api";
import { formatDateOnly } from "@/lib/formatDate";
import { MarketPulse } from "@/components/MarketPulse";
import { StoryColumn, StoryLead, StoryRow } from "@/components/BriefingStories";

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

function SectionRule({ label, note }: { label: string; note?: string }) {
  return (
    <div className="editorial-rule">
      <h2 className="editorial-rubric text-foreground">{label}</h2>
      {note && <span className="editorial-rubric hidden text-muted sm:inline">{note}</span>}
    </div>
  );
}

export default async function Home() {
  const briefing = await getBriefing();
  const { marketPulse, topStories } = briefing;

  const [lead, ...rest] = topStories;
  const columns = rest.slice(0, 3);
  const remainder = rest.slice(3);

  const windowNote =
    marketPulse.totalArticles > 0 && briefing.rangeStart && briefing.rangeEnd
      ? `${formatDateOnly(briefing.rangeStart)} – ${formatDateOnly(briefing.rangeEnd)}`
      : `Last ${briefing.windowDays || 7} days`;

  return (
    <div className="flex flex-col gap-10 sm:gap-14">
      <header className="editorial-reveal flex flex-col gap-4">
        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <p className="editorial-rubric text-muted">
            {formatMastheadDate(briefing.date) || "Today"}
          </p>
          <p className="editorial-rubric text-muted">
            {marketPulse.totalArticles} stories &middot; {windowNote}
          </p>
        </div>

        <div className="editorial-rule-strong" />

        <div>
          <h1 className="editorial-display text-4xl text-foreground sm:text-6xl">
            Today&apos;s Briefing
          </h1>
          <p className="mt-3 max-w-xl text-sm leading-relaxed text-muted sm:text-base">
            The week&apos;s market-moving stories, read and ranked by impact score.
          </p>
        </div>
      </header>

      {(briefing.executiveSummary || marketPulse.totalArticles > 0) && (
        <section className="editorial-reveal grid gap-8 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] lg:gap-12">
          {briefing.executiveSummary ? (
            <div className="flex flex-col gap-4">
              <div className="editorial-rule">
                <span className="editorial-rubric text-foreground">The lede</span>
                <span className="editorial-rubric text-muted">AI-generated</span>
              </div>

              <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
                {briefing.overallSentiment && (
                  <span className="editorial-rubric text-muted">
                    Outlook{" "}
                    <span className="text-foreground">{briefing.overallSentiment}</span>
                  </span>
                )}
                {briefing.riskLevel && (
                  <span className="editorial-rubric text-muted">
                    Risk{" "}
                    <span className={RISK_TONE[briefing.riskLevel] ?? "text-foreground"}>
                      {briefing.riskLevel}
                    </span>
                  </span>
                )}
              </div>

              <p className="editorial-dropcap whitespace-pre-line text-[0.9375rem] leading-[1.75] text-foreground/90 sm:text-base">
                {briefing.executiveSummary}
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
          <span className="editorial-rubric">Browse the full news feed</span>
          <ArrowRight className="editorial-arrow size-4" />
        </Link>
      </footer>
    </div>
  );
}
