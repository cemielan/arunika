/**
 * Readers are in Indonesia, so timestamps are shown in WIB (UTC+7, no DST)
 * rather than UTC. A fixed offset keeps server and client output identical.
 */
function toJakarta(iso: string): Date {
  return new Date(new Date(iso).getTime() + 7 * 60 * 60 * 1000);
}

export function formatDate(iso: string): string {
  const date = toJakarta(iso);
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = months[date.getUTCMonth()];
  const year = date.getUTCFullYear();
  const hour = String(date.getUTCHours()).padStart(2, "0");
  const minute = String(date.getUTCMinutes()).padStart(2, "0");

  return `${day} ${month} ${year}, ${hour}:${minute} WIB`;
}

/**
 * Compact dateline for the briefing's column and list stories, e.g.
 * "10 Sep, 08:35 WIB". Drops the year — every story in the
 * briefing falls inside a seven-day window, so it is noise there.
 */
export function formatDateShort(iso: string): string {
  const date = toJakarta(iso);
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  const day = date.getUTCDate();
  const month = months[date.getUTCMonth()];
  const hour = String(date.getUTCHours()).padStart(2, "0");
  const minute = String(date.getUTCMinutes()).padStart(2, "0");

  return `${day} ${month}, ${hour}:${minute} WIB`;
}

/** Formats a "yyyy-MM-dd" date-only string (no time component) as e.g. "Jul 8, 2026". */
export function formatDateOnly(dateOnly: string): string {
  if (!dateOnly) return "";
  const [year, month, day] = dateOnly.split("-").map(Number);
  if (!year || !month || !day) return dateOnly;

  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
  return `${months[month - 1]} ${day}, ${year}`;
}

