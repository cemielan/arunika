const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

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
