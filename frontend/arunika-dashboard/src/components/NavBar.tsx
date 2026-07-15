"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@heroui/react";
import { Newspaper, Sparkles } from "lucide-react";
import { ThemeSwitch } from "@/components/ThemeSwitch";
import Image from "next/image";

const NAV_LINKS = [
  { href: "/", label: "Briefing", icon: Sparkles },
  { href: "/news", label: "News Feed", icon: Newspaper },
];

export function NavBar() {
  const pathname = usePathname();

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

        <nav className="flex items-center gap-1 rounded-full bg-surface-secondary p-1">
          {NAV_LINKS.map(({ href, label, icon: Icon }) => {
            const isActive =
              href === "/" ? pathname === "/" : pathname?.startsWith(href);
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  "flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors",
                  isActive
                    ? "bg-surface text-foreground shadow-surface"
                    : "text-muted hover:text-foreground",
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
