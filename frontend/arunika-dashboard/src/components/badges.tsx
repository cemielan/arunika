import { Chip } from "@heroui/react";
import { Minus, TrendingDown, TrendingUp } from "lucide-react";
import type { ComponentType } from "react";

type SentimentMeta = {
  label: string;
  color: "success" | "danger" | "default";
  Icon: ComponentType<{ className?: string }>;
};

/** Shared legend so every sentiment surface (badges, market pulse, stat cards) stays in sync. */
export const SENTIMENT_META: Record<"Bullish" | "Bearish" | "Neutral", SentimentMeta> = {
  Bullish: { label: "Bullish", color: "success", Icon: TrendingUp },
  Bearish: { label: "Bearish", color: "danger", Icon: TrendingDown },
  Neutral: { label: "Neutral", color: "default", Icon: Minus },
};

function resolveSentimentMeta(sentiment: string | null): SentimentMeta | null {
  if (!sentiment) return null;
  if (sentiment === "Bullish" || sentiment === "Bearish" || sentiment === "Neutral") {
    return SENTIMENT_META[sentiment];
  }
  return null;
}

export function SentimentBadge({ sentiment }: { sentiment: string | null }) {
  const meta = resolveSentimentMeta(sentiment);

  if (!meta) {
    return (
      <Chip color="default" variant="soft" size="sm">
        Pending
      </Chip>
    );
  }

  const { Icon } = meta;

  return (
    <Chip color={meta.color} variant="soft" size="sm" className="gap-1">
      <Icon className="size-3" />
      <Chip.Label>{meta.label}</Chip.Label>
    </Chip>
  );
}

export function ImpactBadge({ score }: { score: number | null }) {
  if (score === null) {
    return (
      <Chip color="default" variant="soft" size="sm">
        Impact —
      </Chip>
    );
  }

  const color = score >= 70 ? "danger" : score >= 40 ? "warning" : "default";

  return (
    <Chip color={color} variant="soft" size="sm">
      Impact {score}
    </Chip>
  );
}

export function CategoryBadge({ category }: { category: string | null }) {
  if (!category) {
    return null;
  }
  return (
    <Chip color="accent" variant="soft" size="sm">
      {category}
    </Chip>
  );
}

