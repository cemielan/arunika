export type NewsFilterParams = {
  category?: string;
  from?: string;
  to?: string;
  sortBy?: string;
};

export function buildNewsHref(filters: NewsFilterParams, page: number): string {
  const search = new URLSearchParams();
  if (filters.category) search.set("category", filters.category);
  if (filters.from) search.set("from", filters.from);
  if (filters.to) search.set("to", filters.to);
  if (filters.sortBy && filters.sortBy !== "date") search.set("sortBy", filters.sortBy);
  if (page > 1) search.set("page", String(page));
  const query = search.toString();
  return query ? `/news?${query}` : "/news";
}