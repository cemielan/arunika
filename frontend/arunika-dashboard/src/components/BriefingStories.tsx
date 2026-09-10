import Link from "next/link";
import type { TopStory } from "@/lib/api";
import {
  Dateline,
  ImpactMeter,
  Ordinal,
  Rubric,
  SentimentMark,
} from "@/components/editorial";

/**
 * Story presentation for the briefing page. Three treatments share one visual
 * language and descend in prominence: the lead story runs full width, stories
 * two-to-four run as a column grid, and the remainder run as a compact ranked
 * list.
 */

/** Native-disclosure "Why it matters", carrying the AI impact rationale. */
function WhyItMatters({ rationale }: { rationale: string | null }) {
  if (!rationale) {
    return null;
  }

  return (
    <details className="editorial-disclosure mt-3">
      <summary className="inline-flex items-center gap-1.5 text-muted transition-colors hover:text-foreground">
        <span className="editorial-disclosure-sign inline-block leading-none">+</span>
        <Rubric>Why it matters</Rubric>
      </summary>
      <p className="mt-2 border-l border-border pl-3 text-sm leading-relaxed text-muted">
        {rationale}
      </p>
    </details>
  );
}

/** Text affordance replacing the old arrow glyph. */
function ReadCue({ label = "Read the story" }: { label?: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-accent">
      <Rubric className="text-accent!">{label}</Rubric>
      <span className="editorial-arrow inline-block leading-none">&rarr;</span>
    </span>
  );
}

/**
 * Lead story. Sized to carry the page on its own, since the feed has no
 * imagery — the headline, the excerpt and the rationale are all the visual
 * interest there is.
 */
export function StoryLead({ story }: { story: TopStory }) {
  return (
    <article className="editorial-reveal" style={{ animationDelay: "80ms" }}>
      <div className="flex flex-col gap-4 sm:flex-row sm:gap-6">
        <Ordinal index={0} className="text-3xl leading-none sm:text-5xl" />

        <div className="flex-1">
          <Link href={`/news/${story.articleId}`} className="group block">
            <h3 className="editorial-display text-2xl text-foreground sm:text-4xl">
              <span className="editorial-link-underline">{story.title}</span>
            </h3>
          </Link>

          <Dateline
            source={story.source}
            publishedAt={story.publishedAt}
            category={story.category}
            variant="full"
            className="mt-3"
          />

          {story.summary && (
            <p className="mt-4 max-w-2xl text-[0.9375rem] leading-relaxed text-foreground/85 sm:text-base">
              {story.summary}
            </p>
          )}

          <WhyItMatters rationale={story.impactRationale} />

          <div className="mt-5 flex flex-wrap items-center gap-x-5 gap-y-3 border-t border-border pt-4">
            <ImpactMeter score={story.impactScore} />
            <SentimentMark sentiment={story.sentiment} />
            {story.sectors.slice(0, 3).map((sector) => (
              <Rubric key={sector}>{sector}</Rubric>
            ))}
            <Link href={`/news/${story.articleId}`} className="group ml-auto">
              <ReadCue />
            </Link>
          </div>
        </div>
      </div>
    </article>
  );
}

/** Stories two through four: the column grid under "Also this week". */
export function StoryColumn({ story, index }: { story: TopStory; index: number }) {
  return (
    <article
      className="editorial-reveal flex h-full flex-col border-t-2 border-foreground/85 pt-4"
      style={{ animationDelay: `${140 + index * 70}ms` }}
    >
      <div className="flex items-baseline gap-3">
        <Ordinal index={index} className="text-lg" />
        <Dateline
          source={story.source}
          publishedAt={story.publishedAt}
          className="min-w-0 flex-1"
        />
      </div>

      <Link href={`/news/${story.articleId}`} className="group mt-2 block">
        <h3 className="editorial-display text-lg text-foreground sm:text-xl">
          <span className="editorial-link-underline">{story.title}</span>
        </h3>
      </Link>

      {story.summary && (
        <p className="mt-3 line-clamp-4 text-sm leading-relaxed text-muted">{story.summary}</p>
      )}

      <WhyItMatters rationale={story.impactRationale} />

      <div className="mt-auto flex flex-wrap items-center gap-x-4 gap-y-2 pt-4">
        <ImpactMeter score={story.impactScore} />
        <SentimentMark sentiment={story.sentiment} />
      </div>
    </article>
  );
}

/** The tail of the ranking: dense, scannable, one line of context each. */
export function StoryRow({ story, index }: { story: TopStory; index: number }) {
  return (
    <article
      className="editorial-reveal border-t border-border"
      // Capped so a long tail never leaves a row faded out for noticeably
      // longer than the ones above it.
      style={{ animationDelay: `${Math.min(560, 300 + index * 30)}ms` }}
    >
      <Link
        href={`/news/${story.articleId}`}
        className="group flex items-start gap-3 py-4 sm:gap-5"
      >
        <Ordinal index={index} className="w-6 shrink-0 pt-0.5 text-sm sm:text-base" />

        <div className="min-w-0 flex-1">
          <h3 className="editorial-display text-base text-foreground sm:text-lg">
            <span className="editorial-link-underline">{story.title}</span>
          </h3>
          <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-2">
            <Dateline source={story.source} publishedAt={story.publishedAt} />
            <SentimentMark sentiment={story.sentiment} />
          </div>
        </div>

        <ImpactMeter score={story.impactScore} className="hidden shrink-0 pt-1 sm:flex" />
      </Link>
    </article>
  );
}
