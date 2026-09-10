import type { ReactNode } from "react";
import { TrendingDown, TrendingUp, Minus } from "lucide-react";
import { formatDate, formatDateShort } from "@/lib/formatDate";

/**
 * Shared building blocks for Arunika's editorial layout. Every page draws its
 * masthead, section rules, datelines and data marks from here so the briefing,
 * the feed, the sector page and the auth screens stay in one visual key.
 *
 * Decorative iconography is deliberately absent: the only marks left are the
 * three sentiment arrows, which encode data and always appear beside their own
 * label rather than standing in for it.
 */

export const SENTIMENTS = ["Bullish", "Bearish", "Neutral"] as const;
export type SentimentName = (typeof SENTIMENTS)[number];

const SENTIMENT_MARKS: Record<SentimentName, { Icon: typeof Minus; tone: string }> = {
  Bullish: { Icon: TrendingUp, tone: "text-success" },
  Bearish: { Icon: TrendingDown, tone: "text-danger" },
  Neutral: { Icon: Minus, tone: "text-muted" },
};

export function isSentiment(value: string | null | undefined): value is SentimentName {
  return value === "Bullish" || value === "Bearish" || value === "Neutral";
}

/** Small-caps kicker used above headings and as a standalone label. */
export function Rubric({
  children,
  tone = "muted",
  className = "",
}: {
  children: ReactNode;
  tone?: "muted" | "foreground";
  className?: string;
}) {
  return (
    <span
      className={`editorial-rubric ${tone === "muted" ? "text-muted" : "text-foreground"} ${className}`}
    >
      {children}
    </span>
  );
}

/**
 * Page masthead. `meta` sits opposite the kicker on the same baseline, the way
 * an issue number faces a date on a printed cover.
 */
export function PageHeader({
  kicker,
  meta,
  title,
  lede,
}: {
  kicker?: string;
  meta?: ReactNode;
  title: string;
  lede?: ReactNode;
}) {
  return (
    <header className="editorial-reveal flex flex-col gap-4">
      {(kicker || meta) && (
        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          {kicker ? <Rubric>{kicker}</Rubric> : <span />}
          {meta && <Rubric>{meta}</Rubric>}
        </div>
      )}

      <div className="editorial-rule-strong" />

      <div>
        <h1 className="editorial-display text-4xl text-foreground sm:text-6xl">{title}</h1>
        {lede && (
          <p className="mt-3 max-w-xl text-sm leading-relaxed text-muted sm:text-base">{lede}</p>
        )}
      </div>
    </header>
  );
}

/** Section heading with a hairline running to the right margin. */
export function SectionRule({ label, note }: { label: string; note?: ReactNode }) {
  return (
    <div className="editorial-rule">
      <h2 className="editorial-rubric text-foreground">{label}</h2>
      {note && <span className="editorial-rubric hidden text-muted sm:inline">{note}</span>}
    </div>
  );
}

/** Ranked position, set in the display face — "01", "02", … */
export function Ordinal({ index, className = "" }: { index: number; className?: string }) {
  return (
    <span className={`editorial-display tabular-nums text-muted ${className}`} aria-hidden>
      {String(index + 1).padStart(2, "0")}
    </span>
  );
}

/**
 * Magnitude mark for a single value: one hue, thin, rounded, and always shown
 * next to its own number so the score is never carried by the bar alone.
 */
export function ImpactMeter({
  score,
  variant = "labelled",
  className = "",
}: {
  score: number | null;
  /**
   * `bare` drops the label and the numeral, for the one place where the score
   * is already set as a hero number directly above the bar. Everywhere else
   * the value stays written out beside the mark.
   */
  variant?: "labelled" | "bare";
  className?: string;
}) {
  if (score === null) {
    return variant === "bare" ? null : (
      <div className={`flex items-center gap-2 ${className}`}>
        <Rubric>Impact</Rubric>
        <span className="font-mono text-xs text-muted">—</span>
      </div>
    );
  }

  const clamped = Math.max(0, Math.min(100, score));

  const bar = (
    <div
      className={
        variant === "bare"
          ? "h-0.75 w-full overflow-hidden rounded-full bg-surface-secondary"
          : "h-0.75 w-12 overflow-hidden rounded-full bg-surface-secondary sm:w-16"
      }
      aria-hidden
    >
      <div className="h-full rounded-full bg-accent" style={{ width: `${clamped}%` }} />
    </div>
  );

  if (variant === "bare") {
    return <div className={className}>{bar}</div>;
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <Rubric>Impact</Rubric>
      {bar}
      <span className="font-mono text-xs tabular-nums text-foreground">{clamped}</span>
    </div>
  );
}

/** Sentiment as arrow + word. Colour alone would not survive a colourblind reader. */
export function SentimentMark({
  sentiment,
  className = "",
}: {
  sentiment: string | null;
  className?: string;
}) {
  if (!isSentiment(sentiment)) {
    return null;
  }

  const { Icon, tone } = SENTIMENT_MARKS[sentiment];

  return (
    <span className={`inline-flex items-center gap-1 ${tone} ${className}`}>
      <Icon className="size-3.5 shrink-0" />
      <span className="editorial-rubric">{sentiment}</span>
    </span>
  );
}

/** Source · timestamp · category dateline. */
export function Dateline({
  source,
  publishedAt,
  category,
  variant = "short",
  className = "",
}: {
  source?: string | null;
  publishedAt?: string | null;
  category?: string | null;
  variant?: "full" | "short";
  className?: string;
}) {
  const timestamp = publishedAt
    ? variant === "full"
      ? formatDate(publishedAt)
      : formatDateShort(publishedAt)
    : null;

  const parts = [source, timestamp, category].filter((part): part is string => Boolean(part));

  if (parts.length === 0) {
    return null;
  }

  return (
    <p className={`editorial-rubric text-muted ${className}`}>
      {parts.map((part, index) => (
        <span key={`${part}-${index}`}>
          {index > 0 && <span className="mx-1.5 opacity-50">&middot;</span>}
          {part}
        </span>
      ))}
    </p>
  );
}

/** Labelled form field with an underlined input and its error message. */
export function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <label className="flex flex-col gap-1.5">
      <Rubric>{label}</Rubric>
      {children}
      {error && <span className="text-xs leading-relaxed text-danger">{error}</span>}
    </label>
  );
}

/**
 * Framing for the auth screens. Narrow measure, masthead-style heading and a
 * bordered panel, so signing in feels like the same publication as the
 * briefing rather than a separate product.
 */
export function AuthShell({
  kicker,
  title,
  lede,
  children,
  footer,
}: {
  kicker: string;
  title: string;
  lede?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-8 py-6 sm:py-16">
      <header className="editorial-reveal flex flex-col gap-4">
        <Rubric>{kicker}</Rubric>
        <div className="editorial-rule-strong" />
        <div>
          <h1 className="editorial-display text-3xl text-foreground sm:text-4xl">{title}</h1>
          {lede && <p className="mt-3 text-sm leading-relaxed text-muted">{lede}</p>}
        </div>
      </header>

      <div className="editorial-reveal editorial-panel p-6 sm:p-8" style={{ animationDelay: "80ms" }}>
        {children}
      </div>

      {footer && (
        <div className="editorial-reveal text-center text-sm text-muted" style={{ animationDelay: "140ms" }}>
          {footer}
        </div>
      )}
    </div>
  );
}
