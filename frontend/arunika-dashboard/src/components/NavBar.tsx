import Link from "next/link";

export function NavBar() {
  return (
    <header className="border-b border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
        <Link href="/" className="flex items-baseline gap-2">
          <span className="text-lg font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
            Arunika
          </span>
          <span className="hidden text-xs text-zinc-500 sm:inline dark:text-zinc-400">
            The First Light of Market Intelligence
          </span>
        </Link>
        <nav className="flex gap-6 text-sm font-medium text-zinc-600 dark:text-zinc-400">
          <Link href="/" className="hover:text-zinc-900 dark:hover:text-zinc-50">
            Briefing
          </Link>
          <Link href="/news" className="hover:text-zinc-900 dark:hover:text-zinc-50">
            News Feed
          </Link>
        </nav>
      </div>
    </header>
  );
}
