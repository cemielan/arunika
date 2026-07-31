import { unstable_cache } from "next/cache";

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

export type MarketSentiment = "Bullish" | "Bearish" | "Neutral";

export type MarketPulse = {
  /** Overall conclusion derived from every enriched article in the briefing window. */
  sentiment: MarketSentiment;
  bullishCount: number;
  bearishCount: number;
  neutralCount: number;
  totalArticles: number;
  /** Average impact score (0-100) across articles in the window. */
  averageImpact: number;
  /** Share of the dominant sentiment among all articles in the window, 0-100. */
  confidence: number;
};

export type Briefing = {
  /** The last day of the rolling window (defaults to today, UTC). */
  date: string;
  /** First day of the rolling window (inclusive). */
  rangeStart: string;
  /** Last day of the rolling window (inclusive) — same as `date`. */
  rangeEnd: string;
  /** Size of the rolling window in days (currently 7 — see BriefingController). */
  windowDays: number;
  /** Sentiment/impact conclusion computed across every article in the window. */
  marketPulse: MarketPulse;
  topStories: TopStory[];
  /** AI-generated executive summary (when GenerateDailyBriefingJob has run). */
  executiveSummary?: string;
  /** AI-generated overall sentiment (when GenerateDailyBriefingJob has run). */
  overallSentiment?: string;
  /** AI-generated risk level (when GenerateDailyBriefingJob has run). */
  riskLevel?: string;
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

export class ApiRequestError extends Error {
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

type RawMarketPulse = {
  sentiment: MarketSentiment;
  bullishCount: number;
  bearishCount: number;
  neutralCount: number;
  totalArticles: number;
  averageImpactScore: number;
  confidence: number;
};

type RawBriefing = {
  date: string;
  rangeStart: string;
  rangeEnd: string;
  windowDays: number;
  marketPulse: RawMarketPulse;
  topStories: TopStory[];
  executiveSummary?: string;
  overallSentiment?: string;
  riskLevel?: string;
};

function emptyBriefing(date?: string): Briefing {
  return {
    date: date ?? "",
    rangeStart: "",
    rangeEnd: date ?? "",
    windowDays: 7,
    marketPulse: {
      sentiment: "Neutral",
      bullishCount: 0,
      bearishCount: 0,
      neutralCount: 0,
      totalArticles: 0,
      averageImpact: 0,
      confidence: 0,
    },
    topStories: [],
  };
}

/**
 * Fetches the daily briefing: top stories and an aggregate "market pulse"
 * (sentiment/impact conclusion), both computed by the backend from the same
 * rolling 7-day window (`BriefingController.WindowDays`) so the two never
 * disagree about what counts as "recent".
 */
export const getBriefing = unstable_cache(
  async (date?: string): Promise<Briefing> => {
    const query = date ? `?date=${encodeURIComponent(date)}` : "";
    const result = await apiGet<RawBriefing>(`/v1/briefing${query}`, 300);
    if ("notFound" in result) {
      return emptyBriefing(date);
    }

    const { marketPulse, executiveSummary, overallSentiment, riskLevel, ...rest } = result.data;
    return {
      ...rest,
      executiveSummary,
      overallSentiment,
      riskLevel,
      marketPulse: {
        sentiment: marketPulse.sentiment,
        bullishCount: marketPulse.bullishCount,
        bearishCount: marketPulse.bearishCount,
        neutralCount: marketPulse.neutralCount,
        totalArticles: marketPulse.totalArticles,
        averageImpact: marketPulse.averageImpactScore,
        confidence: marketPulse.confidence,
      },
    };
  },
  ["briefing"],
  { revalidate: 300 },
);

export type NewsSortBy = "date" | "impact";

export const getNewsFeed = unstable_cache(
  async (params: {
    category?: string;
    /** Full-text search query against article title and content. */
    search?: string;
    /** Published-date range filter, inclusive, formatted "yyyy-MM-dd". */
    from?: string;
    to?: string;
    sortBy?: NewsSortBy;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: NewsListItem[]; meta: PageMeta }> => {
    const search = new URLSearchParams();
    if (params.category) search.set("category", params.category);
    if (params.search) search.set("search", params.search);
    if (params.from) search.set("from", params.from);
    if (params.to) search.set("to", params.to);
    if (params.sortBy) search.set("sortBy", params.sortBy);
    search.set("page", String(params.page ?? 1));
    search.set("pageSize", String(params.pageSize ?? 20));

    const result = await apiGet<NewsListItem[]>(`/v1/news?${search.toString()}`, 60);
    if ("notFound" in result) {
      return { items: [], meta: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 } };
    }
    return { items: result.data, meta: result.meta as PageMeta };
  },
  ["news-feed"],
  { revalidate: 60 },
);

export const getArticleDetail = unstable_cache(
  async (id: string): Promise<ArticleDetail | null> => {
    const result = await apiGet<ArticleDetail>(`/v1/articles/${id}`, 60);
    if ("notFound" in result) {
      return null;
    }
    return result.data;
  },
  ["article-detail"],
  { revalidate: 60 },
);

export async function upsertMe(token: string): Promise<{ id: string; email: string; role: string }> {
  const res = await fetch(`${API_BASE_URL}/v1/users/me`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    let message = "Failed to sync profile";
    try {
      const body = await res.json() as ApiErrorEnvelope;
      message = body.error?.message ?? message;
    } catch { }
    throw new ApiRequestError(message, res.status);
  }

  const body = await res.json() as { data: { id: string; email: string; role: string } };
  return body.data;
}

export type SectorAggregation = {
  sector: string;
  articleCount: number;
  averageImpactScore: number;
  bullishCount: number;
  bearishCount: number;
  neutralCount: number;
  dominantSentiment: string;
};

export const getSectors = unstable_cache(
  async (): Promise<SectorAggregation[]> => {
    const result = await apiGet<SectorAggregation[]>("/v1/sectors", 300);
    if ("notFound" in result) {
      return [];
    }
    return result.data;
  },
  ["sectors"],
  { revalidate: 300 },
);


