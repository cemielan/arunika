import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import nodemailer from "npm:nodemailer";

// Guards against cross-origin access if the function is ever invoked from a
// browser; the backend always calls with the shared secret header.
const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, x-send-secret",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json", ...corsHeaders },
  });
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return json({ ok: false, error: "Method not allowed" }, 405);
  }

  const expectedSecret = Deno.env.get("SEND_SECRET") ?? "";
  if (!expectedSecret || req.headers.get("x-send-secret") !== expectedSecret) {
    return json({ ok: false, error: "Unauthorized" }, 401);
  }

  let body: { to?: string; subject?: string; html?: string };
  try {
    body = await req.json();
  } catch {
    return json({ ok: false, error: "Invalid JSON body" }, 400);
  }

  const { to, subject, html } = body;
  if (!to || !subject || !html) {
    return json({ ok: false, error: "Missing required fields: to, subject, html" }, 400);
  }

  const host = Deno.env.get("SMTP_HOST") ?? "smtp.gmail.com";
  const port = parseInt(Deno.env.get("SMTP_PORT") ?? "465", 10);
  const user = Deno.env.get("SMTP_USER") ?? "";
  const pass = Deno.env.get("SMTP_PASS") ?? "";

  if (!user || !pass) {
    return json({ ok: false, error: "SMTP_USER/SMTP_PASS secrets are not set" }, 500);
  }

  const transporter = nodemailer.createTransport({
    host,
    port,
    secure: port === 465, // implicit TLS on 465, STARTTLS otherwise
    auth: { user, pass },
    connectionTimeout: 15000,
    greetingTimeout: 15000,
    socketTimeout: 20000,
  });

  const fromEmail = Deno.env.get("SMTP_FROM_EMAIL") ?? user;
  const fromName = Deno.env.get("SMTP_FROM_NAME") ?? "Arunika";

  try {
    const info = await transporter.sendMail({
      from: `"${fromName}" <${fromEmail}>`,
      to,
      subject,
      html,
    });
    console.log(`Email sent to ${to}: ${info.messageId}`);
    return json({ ok: true, messageId: info.messageId });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error(`SMTP send failed for ${to}:`, message);
    return json({ ok: false, error: message }, 500);
  }
});
