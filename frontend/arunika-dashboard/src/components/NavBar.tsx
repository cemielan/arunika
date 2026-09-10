"use client";

import { useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { usePathname, useRouter } from "next/navigation";
import { cn } from "@heroui/react";
import { ThemeSwitch } from "@/components/ThemeSwitch";
import { useSpringVector } from "@/app/hooks/useSpringVector";
import { useAuth } from "@/lib/auth-context";

const NAV_LINKS = [
  { href: "/", label: "Briefing" },
  { href: "/news", label: "News Feed" },
  { href: "/sectors", label: "Sectors" },
];

/**
 * Masthead bar. Navigation is set in the same small-caps rubric as the section
 * rules below it, and the active item is marked by a rule that slides between
 * labels — the pill-and-icon treatment it replaces read as a generic app
 * shell rather than as the top of a publication.
 */
export function NavBar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, ready, user, signOut, showNotification } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  const activeIndex = NAV_LINKS.findIndex(({ href }) =>
    href === "/" ? pathname === "/" : pathname?.startsWith(href),
  );
  // On pages outside the three sections (auth, verify) nothing is marked
  // active. The rule still needs somewhere to sit, so it measures the first
  // link but stays hidden.
  const isNavPage = activeIndex !== -1;
  const activeHref = NAV_LINKS[activeIndex]?.href ?? NAV_LINKS[0].href;
  const isActiveLink = (href: string) => isNavPage && href === activeHref;

  const trackRef = useRef<HTMLElement>(null);
  const linkRefs = useRef<Record<string, HTMLAnchorElement | null>>({});
  const [ruleTarget, setRuleTarget] = useState({ left: 0, width: 0 });

  useLayoutEffect(() => {
    if (window.innerWidth < 768) return;
    const track = trackRef.current;
    const activeEl = linkRefs.current[activeHref];
    if (track && activeEl) {
      const trackRect = track.getBoundingClientRect();
      const elRect = activeEl.getBoundingClientRect();
      setRuleTarget({ left: elRect.left - trackRect.left, width: elRect.width });
    }
  }, [activeHref]);

  const { values } = useSpringVector([ruleTarget.left, ruleTarget.width]);
  const [animLeft, animWidth] = values;

  const handleLogout = async () => {
    await signOut();
    showNotification({ status: "success", title: "Signed out", message: "You have been signed out successfully." });
    setMenuOpen(false);
    router.push("/");
  };

  return (
    <header
      className="sticky top-0 z-40 border-b border-border bg-background/80
      backdrop-blur transition-[background-color,border-color] duration-120 ease-linear
      supports-backdrop-filter:bg-background/60"
    >
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
        <Link href="/" className="flex shrink-0 items-center gap-2 sm:gap-3">
          <Image
            src="/logo-dark.svg"
            alt=""
            width={30}
            height={30}
            className="block dark:hidden"
            priority
          />
          <Image
            src="/logo-light.svg"
            alt=""
            width={30}
            height={30}
            className="hidden dark:block"
            priority
          />
          <span className="editorial-display text-xl leading-none text-foreground sm:text-2xl">
            Arunika.
          </span>
        </Link>

        {/* Desktop nav */}
        <nav ref={trackRef} className="relative hidden items-center gap-7 self-stretch md:flex">
          {isNavPage && (
            <span
              aria-hidden
              className="absolute bottom-1 h-px bg-foreground will-change-transform"
              style={{ left: animLeft, width: animWidth }}
            />
          )}
          {NAV_LINKS.map(({ href, label }) => {
            const isActive = isActiveLink(href);
            return (
              <Link
                key={href}
                href={href}
                ref={(el) => {
                  linkRefs.current[href] = el;
                }}
                aria-current={isActive ? "page" : undefined}
                className={cn(
                  "editorial-rubric flex items-center transition-colors duration-200",
                  isActive ? "text-foreground" : "text-muted hover:text-foreground",
                )}
              >
                {label}
              </Link>
            );
          })}
        </nav>

        {/* Desktop right section */}
        <div className="hidden shrink-0 items-center gap-5 md:flex">
          {!ready ? null : isAuthenticated ? (
            <>
              <span className="editorial-rubric hidden max-w-36 truncate text-muted lg:block">
                {user?.email}
              </span>
              <button
                onClick={handleLogout}
                className="editorial-rubric text-muted transition-colors hover:text-foreground"
              >
                Logout
              </button>
            </>
          ) : (
            <Link
              href="/login"
              className="editorial-rubric text-muted transition-colors hover:text-foreground"
            >
              Sign In
            </Link>
          )}
          <ThemeSwitch />
        </div>

        {/* Mobile toggle */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className="editorial-rubric text-muted transition-colors hover:text-foreground md:hidden"
          aria-expanded={menuOpen}
        >
          {menuOpen ? "Close" : "Menu"}
        </button>
      </div>

      {/* Mobile menu */}
      {menuOpen && (
        <div className="border-t border-border px-4 pb-4 pt-1 md:hidden">
          <nav className="flex flex-col">
            {NAV_LINKS.map(({ href, label }) => {
              const isActive = isActiveLink(href);
              return (
                <Link
                  key={href}
                  href={href}
                  onClick={() => setMenuOpen(false)}
                  aria-current={isActive ? "page" : undefined}
                  className={cn(
                    "editorial-rubric border-b border-border py-3 transition-colors",
                    isActive ? "text-foreground" : "text-muted hover:text-foreground",
                  )}
                >
                  {label}
                </Link>
              );
            })}
          </nav>

          <div className="flex flex-col gap-3 pt-4">
            {!ready ? null : isAuthenticated ? (
              <>
                <span className="editorial-rubric truncate text-muted">{user?.email}</span>
                <button
                  onClick={handleLogout}
                  className="editorial-rubric self-start text-muted transition-colors hover:text-foreground"
                >
                  Logout
                </button>
              </>
            ) : (
              <Link
                href="/login"
                onClick={() => setMenuOpen(false)}
                className="editorial-rubric self-start text-muted transition-colors hover:text-foreground"
              >
                Sign In
              </Link>
            )}
            <ThemeSwitch className="self-start" />
          </div>
        </div>
      )}
    </header>
  );
}
