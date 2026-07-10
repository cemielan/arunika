const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

export type ApiErrorEnvelope = { error: { code: string; message: string } };

export type PageMeta = {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type NewsListItem = {
  id: string;
  title: string;
  source: string;
  publishedAt: string;
  category: string | null;
  sentiment: string | null;
  impactScore: number | null;
  summary: string | null;
};

export type SectorImpact = {
  sector: string;
  direction: string;
  magnitude: number;
};

export type DuplicateArticle = {
  id: string;
  title: string;
  source: string;
};

export type ArticleDetail = {
  id: string;
  title: string;
  url: string;
  source: string;
  publishedAt: string;
  enrichmentStatus: string;
  summary: string | null;
  category: string | null;
  sentiment: string | null;
  sentimentConfidence: number | null;
  impactScore: number | null;
  impactRationale: string | null;
  sectors: SectorImpact[];
  keywords: string[];
  duplicateOfId: string | null;
  duplicates: DuplicateArticle[];
};

export type TopStory = {
  articleId: string;
  title: string;
  impactScore: number;
  sectors: string[];
};

export type Briefing = {
  date: string;
  topStories: TopStory[];
};

/** The seven fixed categories seeded in the database (design doc §4). */
export const CATEGORIES = [
  "Politics",
  "Economy",
  "Markets",
  "Banking",
  "Technology",
  "Commodities",
  "Crypto",
] as const;

class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "ApiRequestError";
  }
}

async function apiGet<T>(
  path: string,
  revalidateSeconds: number,
): Promise<{ data: T; meta: unknown } | { notFound: true }> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    next: { revalidate: revalidateSeconds },
  });

  if (res.status === 404) {
    return { notFound: true };
  }

  if (!res.ok) {
    let message = `Request to ${path} failed with status ${res.status}`;
    try {
      const body = (await res.json()) as ApiErrorEnvelope;
      message = body.error?.message ?? message;
    } catch {
      // response body wasn't JSON; keep the default message
    }
    throw new ApiRequestError(message, res.status);
  }

  const body = (await res.json()) as { data: T; meta: unknown };
  return { data: body.data, meta: body.meta };
}

export async function getBriefing(date?: string): Promise<Briefing> {
  const query = date ? `?date=${encodeURIComponent(date)}` : "";
  const result = await apiGet<Briefing>(`/v1/briefing${query}`, 300);
  if ("notFound" in result) {
    return { date: date ?? "", topStories: [] };
  }
  return result.data;
}

export async function getNewsFeed(params: {
  category?: string;
  page?: number;
  pageSize?: number;
}): Promise<{ items: NewsListItem[]; meta: PageMeta }> {
  const search = new URLSearchParams();
  if (params.category) search.set("category", params.category);
  search.set("page", String(params.page ?? 1));
  search.set("pageSize", String(params.pageSize ?? 20));

  const result = await apiGet<NewsListItem[]>(`/v1/news?${search.toString()}`, 60);
  if ("notFound" in result) {
    return { items: [], meta: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 } };
  }
  return { items: result.data, meta: result.meta as PageMeta };
}

export async function getArticleDetail(id: string): Promise<ArticleDetail | null> {
  const result = await apiGet<ArticleDetail>(`/v1/articles/${id}`, 60);
  if ("notFound" in result) {
    return null;
  }
  return result.data;
}

export type MarketSentiment = "Bullish" | "Bearish" | "Neutral";

export type MarketPulse = {
  /** Overall conclusion derived from the most recent batch of articles. */
  sentiment: MarketSentiment;
  bullishCount: number;
  bearishCount: number;
  neutralCount: number;
  totalArticles: number;
  /** Average impact score (0-100) across articles that have one. */
  averageImpact: number;
  /** Share of the dominant sentiment among all sampled articles, 0-100. */
  confidence: number;
};

/**
 * Aggregates sentiment across the most recent news items to produce a single
 * "today's conclusion" (Bullish / Bearish / Neutral) for the dashboard summary.
 */
export async function getMarketPulse(): Promise<MarketPulse> {
  const { items } = await getNewsFeed({ pageSize: 50 });

  let bullishCount = 0;
  let bearishCount = 0;
  let neutralCount = 0;
  let impactSum = 0;
  let impactCount = 0;

  for (const item of items) {
    if (item.sentiment === "Bullish") bullishCount += 1;
    else if (item.sentiment === "Bearish") bearishCount += 1;
    else neutralCount += 1;

    if (item.impactScore !== null) {
      impactSum += item.impactScore;
      impactCount += 1;
    }
  }

  const totalArticles = items.length;
  const averageImpact = impactCount > 0 ? Math.round(impactSum / impactCount) : 0;

  let sentiment: MarketSentiment = "Neutral";
  if (bullishCount > bearishCount && bullishCount > neutralCount) sentiment = "Bullish";
  else if (bearishCount > bullishCount && bearishCount > neutralCount) sentiment = "Bearish";

  const dominantCount = Math.max(bullishCount, bearishCount, neutralCount);
  const confidence = totalArticles > 0 ? Math.round((dominantCount / totalArticles) * 100) : 0;

  return {
    sentiment,
    bullishCount,
    bearishCount,
    neutralCount,
    totalArticles,
    averageImpact,
    confidence,
  };
}
