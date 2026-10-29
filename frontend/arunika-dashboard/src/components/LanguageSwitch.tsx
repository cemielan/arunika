"use client";

import { startTransition, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LANGUAGE_COOKIE, parseLanguage, type Language } from "@/lib/language";

function readLanguageCookie(): Language {
  const match = document.cookie.match(new RegExp(`(?:^|; )${LANGUAGE_COOKIE}=([^;]*)`));
  return parseLanguage(match?.[1]);
}

/**
 * Switches the AI-written text between English and Bahasa Indonesia. Shows
 * both options with the current one highlighted: a single word left readers
 * unsure whether it named the current language or the one they'd get. The
 * pages read the cookie on the server, so a refresh re-renders them.
 */
export function LanguageSwitch({ className = "" }: { className?: string }) {
  const router = useRouter();
  const [language, setLanguage] = useState<Language>("en");

  // document.cookie doesn't exist on the server; read it after hydration so
  // the first client render matches the server's markup.
  useEffect(() => {
    startTransition(() => setLanguage(readLanguageCookie()));
  }, []);

  const next: Language = language === "id" ? "en" : "id";

  const handleClick = () => {
    document.cookie = `${LANGUAGE_COOKIE}=${next}; path=/; max-age=31536000; samesite=lax`;
    setLanguage(next);
    router.refresh();
  };

  return (
    <button
      type="button"
      aria-label={next === "id" ? "Show summaries in Bahasa Indonesia" : "Show summaries in English"}
      onClick={handleClick}
      className={`editorial-rubric -m-2 p-2 text-muted transition-colors hover:text-foreground ${className}`}
    >
      <span className={language === "en" ? "text-foreground" : undefined}>EN</span>
      <span className="mx-1 opacity-50">/</span>
      <span className={language === "id" ? "text-foreground" : undefined}>ID</span>
    </button>
  );
}
