/**
 * Language of the AI-written text (summaries, "Why it matters", the briefing
 * lede). The interface itself stays in English. Kept in a cookie rather than
 * localStorage so server components can render the chosen language directly,
 * with no flash of the other one after hydration.
 */
export type Language = "en" | "id";

export const LANGUAGE_COOKIE = "arunika-lang";

export function parseLanguage(value: string | undefined | null): Language {
  return value === "id" ? "id" : "en";
}

/**
 * The text to show, and the language it is actually in: content enriched
 * before translations existed has no Indonesian copy and falls back to English.
 * The language goes on the element's `lang`, so screen readers pronounce it right.
 */
export function localize(
  language: Language,
  english: string | null | undefined,
  indonesian: string | null | undefined,
): { text: string | null; lang: Language } {
  return language === "id" && indonesian
    ? { text: indonesian, lang: "id" }
    : { text: english ?? null, lang: "en" };
}
