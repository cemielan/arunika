"use client";

import { startTransition, useEffect, useState } from "react";
import { useTheme } from "@heroui/react";

/**
 * Theme toggle written as a word rather than a sun/moon glyph, so the header
 * carries no decorative iconography. The label names the mode you will get,
 * which is also less ambiguous than the icon it replaces.
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
      className={`editorial-rubric text-muted transition-colors hover:text-foreground ${className}`}
    >
      {/*
        Reserve the width of the longer word so the header does not reflow when
        the label swaps; `invisible` keeps it out of the accessibility tree.
      */}
      <span className="relative inline-grid">
        <span className="invisible col-start-1 row-start-1" aria-hidden>
          Light
        </span>
        <span className="col-start-1 row-start-1 text-left">{isDark ? "Light" : "Dark"}</span>
      </span>
    </button>
  );
}
