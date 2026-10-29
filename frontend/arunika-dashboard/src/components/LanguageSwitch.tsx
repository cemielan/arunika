"use client";

import { startTransition, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LANGUAGE_COOKIE, parseLanguage, type Language } from "@/lib/language";

function readLanguageCookie(): Language {
  const match = document.cookie.match(new RegExp(`(?:^|; )${LANGUAGE_COOKIE}=([^;]*)`));
  return parseLanguage(match?.[1]);
}

/**
 * Switches the AI-written text between English and Bahasa Indonesia. Like
 * ThemeSwitch, the label names the language you will get. The pages read the
 * cookie on the server, so a refresh re-renders them in the new language.
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
      {/* Reserve the longer label's width so the header doesn't reflow on swap. */}
      <span className="relative inline-grid">
        <span className="invisible col-start-1 row-start-1" aria-hidden>
          Indonesia
        </span>
        <span className="col-start-1 row-start-1 text-left">
          {next === "id" ? "Indonesia" : "English"}
        </span>
      </span>
    </button>
  );
}
