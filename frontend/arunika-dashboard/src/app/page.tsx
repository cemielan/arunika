import Link from "next/link";
import { getBriefing } from "@/lib/api";
import { ImpactBadge } from "@/components/badges";

export default async function Home() {
  const briefing = await getBriefing();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
          Today&apos;s Briefing
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          Top market-moving stories for {briefing.date}, ranked by impact score.
        </p>
      </div>

      {briefing.topStories.length === 0 ? (
        <div className="rounded-lg border border-dashed border-zinc-300 bg-white p-8 text-center text-sm text-zinc-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-400">
          No enriched stories yet for today. Check back once the news pipeline has
          run, or browse the{" "}
          <Link href="/news" className="font-medium text-blue-600 hover:underline dark:text-blue-400">
            full news feed
          </Link>
          .
        </div>
      ) : (
        <ol className="flex flex-col gap-3">
          {briefing.topStories.map((story, index) => (
            <li key={story.articleId}>
              <Link
                href={`/news/${story.articleId}`}
                className="flex items-start gap-4 rounded-lg border border-zinc-200 bg-white p-4 transition-colors hover:border-zinc-300 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:border-zinc-700 dark:hover:bg-zinc-800/60"
              >
                <span className="mt-0.5 text-sm font-semibold text-zinc-400 dark:text-zinc-600">
                  {index + 1}
                </span>
                <div className="flex flex-1 flex-col gap-2">
                  <h2 className="font-medium text-zinc-900 dark:text-zinc-50">{story.title}</h2>
                  <div className="flex flex-wrap items-center gap-2">
                    <ImpactBadge score={story.impactScore} />
                    {story.sectors.map((sector) => (
                      <span
                        key={sector}
                        className="inline-flex items-center rounded-full bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-600 dark:bg-zinc-800 dark:text-zinc-300"
                      >
                        {sector}
                      </span>
                    ))}
                  </div>
                </div>
              </Link>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}
