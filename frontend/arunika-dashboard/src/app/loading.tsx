function Block({ className }: { className: string }) {
  return <div className={`animate-pulse bg-surface-secondary ${className}`} />;
}

/**
 * Route skeleton shaped like the briefing: masthead, lede beside the pulse
 * rail, a lead story, then the column grid. Matching the real layout keeps the
 * page from reflowing when the content lands.
 */
export default function Loading() {
  return (
    <div className="flex flex-col gap-10 sm:gap-14" aria-busy="true">
      <div className="flex flex-col gap-4">
        <div className="flex items-baseline justify-between">
          <Block className="h-3 w-52" />
          <Block className="hidden h-3 w-44 sm:block" />
        </div>
        <div className="h-0.5 bg-foreground/85" />
        <Block className="h-11 w-80 sm:h-14 sm:w-md" />
        <Block className="h-4 w-72" />
      </div>

      <div className="grid gap-8 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] lg:gap-12">
        <div className="flex flex-col gap-3">
          <Block className="h-3 w-24" />
          <Block className="h-4 w-full" />
          <Block className="h-4 w-full" />
          <Block className="h-4 w-11/12" />
          <Block className="h-4 w-full" />
          <Block className="h-4 w-4/5" />
        </div>
        <div className="editorial-panel flex flex-col gap-5 p-5">
          <Block className="h-3 w-28" />
          <div className="flex items-start justify-between">
            <Block className="h-8 w-32" />
            <Block className="h-8 w-12" />
          </div>
          <Block className="h-2 w-full" />
          <Block className="h-3 w-full" />
          <Block className="h-3 w-4/5" />
        </div>
      </div>

      <div className="flex flex-col gap-6">
        <Block className="h-3 w-28" />
        <div className="flex gap-6">
          <Block className="hidden h-12 w-14 sm:block" />
          <div className="flex flex-1 flex-col gap-3">
            <Block className="h-8 w-full" />
            <Block className="h-8 w-3/4" />
            <Block className="h-3 w-56" />
            <Block className="mt-2 h-4 w-full" />
            <Block className="h-4 w-11/12" />
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-6">
        <Block className="h-3 w-32" />
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 lg:gap-10">
          {[0, 1, 2].map((i) => (
            <div key={i} className="flex flex-col gap-3 border-t-2 border-foreground/85 pt-4">
              <Block className="h-3 w-36" />
              <Block className="h-5 w-full" />
              <Block className="h-5 w-2/3" />
              <Block className="mt-2 h-3 w-full" />
              <Block className="h-3 w-full" />
              <Block className="h-3 w-3/4" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
