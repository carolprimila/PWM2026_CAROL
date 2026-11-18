# Executa a API em modo de desenvolvimento usando as variáveis do arquivo .env da raiz do projeto.
# Pré-requisito: PostgreSQL em execução (docker compose up -d db).
param([int]$Port = 5080)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
if (-not (Test-Path $envFile)) { throw "Arquivo .env não encontrado em $root. Copie .env.example para .env e preencha." }

$vars = @{}
Get-Content $envFile | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
    $name, $value = $_ -split '=', 2
    $vars[$name.Trim()] = $value.Trim()
}

$dbPort = if ($vars['DB_PORT']) { $vars['DB_PORT'] } else { '5432' }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ConnectionStrings__Default = "Host=localhost;Port=$dbPort;Database=$($vars['POSTGRES_DB']);Username=$($vars['POSTGRES_USER']);Password=$($vars['POSTGRES_PASSWORD'])"
$env:Jwt__Key = $vars['JWT_KEY']
$env:Bootstrap__SuperAdminName = $vars['SUPERADMIN_NAME']
$env:Bootstrap__SuperAdminEmail = $vars['SUPERADMIN_EMAIL']
$env:Bootstrap__SuperAdminPassword = $vars['SUPERADMIN_PASSWORD']
$env:Bootstrap__ClubName = $vars['CLUB_NAME']
$env:Bootstrap__ClubAdminName = $vars['CLUB_ADMIN_NAME']
$env:Bootstrap__ClubAdminEmail = $vars['CLUB_ADMIN_EMAIL']
$env:Bootstrap__ClubAdminPassword = $vars['CLUB_ADMIN_PASSWORD']
$env:Payments__Provider = $vars['PAYMENTS_PROVIDER']
$env:Payments__SandboxSecret = $vars['PAYMENTS_SANDBOX_SECRET']
# Em desenvolvimento, os scripts de contas e dados de exemplo fazem muitos logins em sequência.
$env:RateLimit__AuthPerMinute = '300'
$env:Email__Smtp__Host = $vars['SMTP_HOST']
$env:Email__Smtp__From = $vars['SMTP_FROM']

dotnet run --project (Join-Path $PSScriptRoot 'Clubes.API') --no-launch-profile
