export function SentimentBadge({ sentiment }: { sentiment: string | null }) {
  if (!sentiment) {
    return (
      <span className="inline-flex items-center rounded-full bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400">
        Pending
      </span>
    );
  }

  const styles: Record<string, string> = {
    Bullish: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300",
    Bearish: "bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300",
    Neutral: "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
  };

  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
        styles[sentiment] ?? styles.Neutral
      }`}
    >
      {sentiment}
    </span>
  );
}

export function ImpactBadge({ score }: { score: number | null }) {
  if (score === null) {
    return (
      <span className="inline-flex items-center rounded-full bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400">
        Impact —
      </span>
    );
  }

  let color = "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300";
  if (score >= 70) {
    color = "bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300";
  } else if (score >= 40) {
    color = "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300";
  }

  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${color}`}>
      Impact {score}
    </span>
  );
}

export function CategoryBadge({ category }: { category: string | null }) {
  if (!category) {
    return null;
  }
  return (
    <span className="inline-flex items-center rounded-full bg-blue-50 px-2.5 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-900/30 dark:text-blue-300">
      {category}
    </span>
  );
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}
