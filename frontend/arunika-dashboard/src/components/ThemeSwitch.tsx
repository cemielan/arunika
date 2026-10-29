"use client";

import { startTransition, useEffect, useState } from "react";
import { useTheme } from "@heroui/react";

/**
 * Theme toggle written as words rather than a sun/moon glyph, so the header
 * carries no decorative iconography. Both modes are shown with the current one
 * highlighted.
 */
export function ThemeSwitch({ className = "" }: { className?: string }) {
  const { resolvedTheme, setTheme } = useTheme();

  // `resolvedTheme` is `undefined` on the server and resolves synchronously on
  // the client from `matchMedia`/localStorage, so using it directly on the
  // first render would make the client's markup differ from the server-
  // rendered HTML (hydration mismatch). Gate on `mounted` so the first client
  // render matches the server's (always non-dark) output; the real theme is
  // applied on the next render, after hydration has committed.
  const [mounted, setMounted] = useState(false);
  useEffect(() => { startTransition(() => setMounted(true)); }, []);
  const isDark = mounted && resolvedTheme === "dark";

  return (
    <button
      type="button"
      aria-label={`Switch to ${isDark ? "light" : "dark"} theme`}
      onClick={() => setTheme(isDark ? "light" : "dark")}
      // Negative margin expands the tap target for comfortable thumb use
      // without growing the visible label or shifting surrounding layout.
      className={`editorial-rubric -m-2 p-2 text-muted transition-colors hover:text-foreground ${className}`}
    >
      <span className={mounted && isDark ? "text-foreground" : undefined}>Dark</span>
      <span className="mx-1 opacity-50">/</span>
      <span className={mounted && !isDark ? "text-foreground" : undefined}>Light</span>
    </button>
  );
}
