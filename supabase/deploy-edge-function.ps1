param(
    [Parameter(Mandatory = $true)]
    [string]$GmailAppPassword
)

$ErrorActionPreference = "Stop"
$projectRef = "sjeelltwotjsneyczroq"
$secret = Get-Content "D:\arunika\supabase\SEND_SECRET.txt"

Write-Host "1/3 Linking project..."
supabase link --project-ref $projectRef
if ($LASTEXITCODE -ne 0) { throw "supabase link failed" }

Write-Host "2/3 Setting secrets..."
supabase secrets set `
    "SEND_SECRET=$secret" `
    "SMTP_HOST=smtp.gmail.com" `
    "SMTP_PORT=465" `
    "SMTP_USER=arunika.noreply@gmail.com" `
    "SMTP_PASS=$GmailAppPassword" `
    "SMTP_FROM_EMAIL=arunika.noreply@gmail.com" `
    "SMTP_FROM_NAME=Arunika"
if ($LASTEXITCODE -ne 0) { throw "supabase secrets set failed" }

Write-Host "3/3 Deploying function..."
supabase functions deploy send-digest-email --no-verify-jwt
if ($LASTEXITCODE -ne 0) { throw "supabase functions deploy failed" }

Write-Host ""
Write-Host "DEPLOYED. Now in Render set env var:"
Write-Host "  Email__EdgeFunctionSecret = $secret"
