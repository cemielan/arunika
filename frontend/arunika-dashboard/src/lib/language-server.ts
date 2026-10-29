import { cookies } from "next/headers";
import { LANGUAGE_COOKIE, parseLanguage, type Language } from "@/lib/language";

/** The reader's chosen language, for server components. */
export async function getLanguage(): Promise<Language> {
  return parseLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
}
