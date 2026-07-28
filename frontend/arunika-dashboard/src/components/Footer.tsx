import { Separator, Typography } from "@heroui/react";

export function Footer() {
  return (
    <footer className="mt-auto">
      <Separator />
      <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
        <Typography.Paragraph size="xs" color="muted">
          Arunika content — summaries, sentiment, and impact scores — is generated
          for informational purposes only and does not constitute financial or
          investment advice.
        </Typography.Paragraph>
      </div>
    </footer>
  );
}
