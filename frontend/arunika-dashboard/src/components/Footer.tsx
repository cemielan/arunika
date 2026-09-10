import { Rubric } from "@/components/editorial";

export function Footer() {
  return (
    <footer className="mt-auto border-t border-border">
      {/* Extra bottom padding clears the home-indicator safe area so the
          disclaimer isn't the thing sitting under the gesture bar. */}
      <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 pt-8 pb-[calc(2rem+env(safe-area-inset-bottom))] sm:px-6">
        <Rubric>Arunika</Rubric>
        <p className="max-w-2xl text-xs leading-relaxed text-muted">
          Arunika content — summaries, sentiment, and impact scores — is generated for
          informational purposes only and does not constitute financial or investment advice.
        </p>
      </div>
    </footer>
  );
}
