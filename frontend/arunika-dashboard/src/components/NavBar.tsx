"use client";

import { useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { cn } from "@heroui/react";
import { LogIn, LogOut, Menu, Newspaper, Sparkles, User, X } from "lucide-react";
import Image from "next/image";
import { ThemeSwitch } from "@/components/ThemeSwitch";
import { useSpringVector } from "@/app/hooks/useSpringVector";
import { useAuth } from "@/lib/auth-context";

const NAV_LINKS = [
  { href: "/", label: "Briefing", icon: Sparkles },
  { href: "/news", label: "News Feed", icon: Newspaper },
  { href: "/sectors", label: "Sectors", icon: User },
];

export function NavBar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, user, logout, showNotification } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  const activeIndex = NAV_LINKS.findIndex(({ href }) =>
    href === "/" ? pathname === "/" : pathname?.startsWith(href),
  );
  const activeHref = NAV_LINKS[activeIndex]?.href ?? NAV_LINKS[0].href;

  const trackRef = useRef<HTMLElement>(null);
  const linkRefs = useRef<Record<string, HTMLAnchorElement | null>>({});
  const [pillTarget, setPillTarget] = useState({ left: 0, width: 0 });

  useLayoutEffect(() => {
    if (window.innerWidth < 640) return;
    const track = trackRef.current;
    const activeEl = linkRefs.current[activeHref];
    if (track && activeEl) {
      const trackRect = track.getBoundingClientRect();
      const elRect = activeEl.getBoundingClientRect();
      setPillTarget({ left: elRect.left - trackRect.left, width: elRect.width });
    }
  }, [activeHref]);

  const { values, velocities } = useSpringVector([pillTarget.left, pillTarget.width]);
  const [animLeft, animWidth] = values;
  const stretch = Math.min(Math.abs(velocities[0]) / 900, 0.12);

  const handleLogout = () => {
    logout();
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
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-2 px-4 py-3 sm:px-6">
        <Link href="/" className="flex items-center gap-2 sm:gap-3">
          <Image
            src="/logo-dark.svg"
            alt="Arunika"
            width={32}
            height={32}
            className="block dark:hidden sm:w-9 sm:h-9"
            priority
          />
          <Image
            src="/logo-light.svg"
            alt="Arunika"
            width={32}
            height={32}
            className="hidden dark:block sm:w-9 sm:h-9"
            priority
          />
          <span className="text-lg font-semibold tracking-tight sm:text-xl">
            Arunika.
          </span>
        </Link>

        {/* Desktop nav */}
        <nav
          ref={trackRef}
          className="relative hidden items-center gap-1 rounded-full bg-surface-secondary p-1 sm:flex"
        >
          <span
            aria-hidden
            className="absolute inset-y-1 rounded-full bg-surface shadow-surface will-change-transform"
            style={{
              left: animLeft,
              width: animWidth,
              transform: `scaleX(${1 + stretch})`,
            }}
          />
          {NAV_LINKS.map(({ href, label, icon: Icon }) => {
            const isActive = href === activeHref;
            return (
              <Link
                key={href}
                href={href}
                ref={(el) => {
                  linkRefs.current[href] = el;
                }}
                className={cn(
                  "relative z-10 flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors duration-200",
                  isActive ? "text-foreground" : "text-muted hover:text-foreground",
                )}
              >
                <Icon className="size-3.5" />
                {label}
              </Link>
            );
          })}
        </nav>

        {/* Desktop right section */}
        <div className="hidden items-center gap-3 sm:flex">
          {isAuthenticated ? (
            <>
              <span className="flex items-center gap-1.5 text-sm text-muted">
                <User className="size-3.5" />
                <span className="max-w-28 truncate">{user?.email}</span>
              </span>
              <button
                onClick={handleLogout}
                className="flex items-center gap-1 rounded-full px-3 py-1.5 text-sm font-medium text-muted transition-colors hover:text-foreground"
              >
                <LogOut className="size-3.5" />
                Logout
              </button>
            </>
          ) : (
            <Link
              href="/login"
              className="flex items-center gap-1 rounded-full px-3 py-1.5 text-sm font-medium text-muted transition-colors hover:text-foreground"
            >
              <LogIn className="size-3.5" />
              Sign In
            </Link>
          )}
          <ThemeSwitch />
        </div>

        {/* Mobile hamburger */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className="flex items-center sm:hidden"
          aria-label="Toggle menu"
        >
          {menuOpen ? <X className="size-5" /> : <Menu className="size-5" />}
        </button>
      </div>

      {/* Mobile menu */}
      {menuOpen && (
        <div className="border-t border-border px-4 pb-4 pt-2 sm:hidden">
          <div className="flex flex-col gap-2">
            {NAV_LINKS.map(({ href, label, icon: Icon }) => {
              const isActive = href === activeHref;
              return (
                <Link
                  key={href}
                  href={href}
                  onClick={() => setMenuOpen(false)}
                  className={cn(
                    "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                    isActive ? "bg-surface-secondary text-foreground" : "text-muted hover:text-foreground",
                  )}
                >
                  <Icon className="size-4" />
                  {label}
                </Link>
              );
            })}
            <hr className="border-border" />
            {isAuthenticated ? (
              <>
                <span className="flex items-center gap-2 px-3 py-2 text-sm text-muted">
                  <User className="size-4" />
                  {user?.email}
                </span>
                <button
                  onClick={handleLogout}
                  className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-muted transition-colors hover:text-foreground"
                >
                  <LogOut className="size-4" />
                  Logout
                </button>
              </>
            ) : (
              <Link
                href="/login"
                onClick={() => setMenuOpen(false)}
                className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-muted transition-colors hover:text-foreground"
              >
                <LogIn className="size-4" />
                Sign In
              </Link>
            )}
            <div className="flex items-center gap-2 px-3 py-2">
              <span className="text-sm text-muted">Theme</span>
              <ThemeSwitch />
            </div>
          </div>
        </div>
      )}
    </header>
  );
}
