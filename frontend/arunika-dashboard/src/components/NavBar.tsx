"use client";

import { useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@heroui/react";
import { Newspaper, Sparkles } from "lucide-react";
import Image from "next/image";
import { ThemeSwitch } from "@/components/ThemeSwitch";
import { useSpringVector } from "@/app/hooks/useSpringVector";

const NAV_LINKS = [
  { href: "/", label: "Briefing", icon: Sparkles },
  { href: "/news", label: "News Feed", icon: Newspaper },
];

export function NavBar() {
  const pathname = usePathname();

  const activeIndex = NAV_LINKS.findIndex(({ href }) =>
    href === "/" ? pathname === "/" : pathname?.startsWith(href),
  );
  const activeHref = NAV_LINKS[activeIndex]?.href ?? NAV_LINKS[0].href;

  const trackRef = useRef<HTMLElement>(null);
  const linkRefs = useRef<Record<string, HTMLAnchorElement | null>>({});
  const [pillTarget, setPillTarget] = useState({ left: 0, width: 0 });

  useLayoutEffect(() => {
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

  return (
    <header
      className="sticky top-0 z-40 border-b border-border bg-background/80 
      backdrop-blur transition-[background-color,border-color] duration-120 ease-linear 
      supports-backdrop-filter:bg-background/60"
    >
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-3">
        <Link href="/" className="flex items-center gap-3">
          <Image
            src="/logo-dark.svg"
            alt="Arunika"
            width={36}
            height={36}
            className="block dark:hidden"
            priority
          />
          <Image
            src="/logo-light.svg"
            alt="Arunika"
            width={36}
            height={36}
            className="hidden dark:block"
            priority
          />

          <span className="text-xl font-semibold tracking-tight">
            Arunika.
          </span>
        </Link>

        <nav
          ref={trackRef}
          className="relative flex items-center gap-1 rounded-full bg-surface-secondary p-1"
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

        <ThemeSwitch />
      </div>
    </header>
  );
}