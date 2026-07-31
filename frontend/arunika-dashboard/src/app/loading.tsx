import { Card } from "@heroui/react";

function SkeletonBlock({ className }: { className: string }) {
  return <div className={`animate-pulse rounded-full bg-surface-secondary ${className}`} />;
}

function SkeletonCard() {
  return (
    <Card variant="default">
      <div className="flex items-start gap-2 sm:gap-4">
        <SkeletonBlock className="mt-1 size-4 sm:mt-0.5" />
        <div className="flex flex-1 flex-col gap-2">
          <div className="flex items-center justify-between gap-4">
            <SkeletonBlock className="h-4 flex-1" />
            <SkeletonBlock className="hidden size-12 shrink-0 sm:block" />
          </div>
          <div className="hidden flex-wrap gap-2 sm:flex">
            <SkeletonBlock className="h-5 w-16" />
            <SkeletonBlock className="h-5 w-20" />
            <SkeletonBlock className="h-5 w-14" />
          </div>
        </div>
      </div>
    </Card>
  );
}

export default function Loading() {
  return (
    <div className="flex flex-col gap-6 sm:gap-8" aria-busy="true">
      <div className="flex flex-col gap-2">
        <SkeletonBlock className="h-4 w-28" />
        <SkeletonBlock className="h-8 w-52 sm:h-9 sm:w-64" />
        <SkeletonBlock className="hidden h-4 w-80 sm:block" />
      </div>

      <Card variant="default" className="gap-3! p-4! sm:gap-6! sm:p-6!">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-3 sm:gap-4">
            <SkeletonBlock className="size-10 sm:size-14 rounded-2xl" />
            <div className="flex flex-col gap-2">
              <SkeletonBlock className="h-3 w-24" />
              <SkeletonBlock className="h-6 w-32" />
            </div>
          </div>
          <div className="flex flex-col items-end gap-2">
            <SkeletonBlock className="h-3 w-14" />
            <SkeletonBlock className="h-7 w-12" />
          </div>
        </div>
        <SkeletonBlock className="hidden h-3 w-72 sm:block" />
        <SkeletonBlock className="h-2 w-full rounded-full" />
        <div className="flex gap-3">
          <SkeletonBlock className="h-3 w-16" />
          <SkeletonBlock className="h-3 w-16" />
          <SkeletonBlock className="h-3 w-16" />
        </div>
      </Card>

      <div className="flex flex-col gap-3">
        <SkeletonBlock className="h-6 w-48" />
        <div className="flex flex-col gap-2 sm:gap-3">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      </div>
    </div>
  );
}
