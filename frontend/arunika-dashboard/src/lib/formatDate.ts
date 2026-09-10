export function formatDate(iso: string): string {
  const date = new Date(iso);
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = months[date.getUTCMonth()];
  const year = date.getUTCFullYear();
  const hour = String(date.getUTCHours()).padStart(2, "0");
  const minute = String(date.getUTCMinutes()).padStart(2, "0");

  return `${day} ${month} ${year}, ${hour}:${minute} UTC`;
}

/**
 * Compact dateline for the briefing's column and list stories, e.g.
 * "10 Sep, 08:35". Drops the year and the UTC suffix — every story in the
 * briefing falls inside a seven-day window, so both are noise there, and the
 * full form wraps onto a second line in a narrow column.
 */
export function formatDateShort(iso: string): string {
  const date = new Date(iso);
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  const day = date.getUTCDate();
  const month = months[date.getUTCMonth()];
  const hour = String(date.getUTCHours()).padStart(2, "0");
  const minute = String(date.getUTCMinutes()).padStart(2, "0");

  return `${day} ${month}, ${hour}:${minute}`;
}

/** Formats a "yyyy-MM-dd" date-only string (no time component) as e.g. "Jul 8, 2026". */
export function formatDateOnly(dateOnly: string): string {
  if (!dateOnly) return "";
  const [year, month, day] = dateOnly.split("-").map(Number);
  if (!year || !month || !day) return dateOnly;

  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
  return `${months[month - 1]} ${day}, ${year}`;
}

