param([string]$BaseUrl = "http://127.0.0.1:5081")
$ErrorActionPreference = "Stop"
$apiProject = Join-Path $PSScriptRoot "..\CleanVander.Api\CleanVander.Api.csproj"
$adminKey = "CleanVander-Test-Admin"
$dbFile = Join-Path $env:TEMP ("cleanvander-test-{0}.db3" -f [guid]::NewGuid().ToString("N"))
$psi = [System.Diagnostics.ProcessStartInfo]::new("dotnet")
@("run", "--project", (Resolve-Path $apiProject).Path, "--no-build", "--no-launch-profile") | ForEach-Object { [void]$psi.ArgumentList.Add($_) }
$psi.WorkingDirectory = Split-Path (Resolve-Path $apiProject).Path
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.Environment["ASPNETCORE_URLS"] = $BaseUrl
$psi.Environment["Database__Path"] = $dbFile
$psi.Environment["Admin__ApiKey"] = $adminKey
$server = [System.Diagnostics.Process]::Start($psi)
$passed = 0
function Assert($condition, [string]$message) { if (-not $condition) { throw "FALHOU: $message" }; $script:passed++; Write-Host "OK  $message" -ForegroundColor Green }
function SendJson([string]$method, [string]$path, $body = $null, [hashtable]$headers = @{}) {
  $args = @{ Method=$method; Uri="$BaseUrl$path"; Headers=$headers; ErrorAction="Stop" }
  if ($null -ne $body) { $args.ContentType="application/json"; $args.Body=($body | ConvertTo-Json -Depth 12) }
  return Invoke-RestMethod @args
}
try {
  $ready=$false
  for($i=0;$i -lt 50;$i++){ try { $health=Invoke-RestMethod "$BaseUrl/health"; if($health.status -eq "ok"){$ready=$true;break} } catch { Start-Sleep -Milliseconds 300 } }
  Assert $ready "API inicia e responde health"
  $products=Invoke-RestMethod "$BaseUrl/api/products"
  Assert ($products.Count -ge 5) "catálogo inicial contém produtos de demonstração"
  $unauth=Invoke-WebRequest "$BaseUrl/api/orders" -SkipHttpErrorCheck
  Assert ($unauth.StatusCode -eq 401) "histórico recusa cliente sem chave"
  $client=SendJson POST "/api/clients" @{ tipoPessoa="Fisica"; nome="Cliente Teste CleanVander"; documento=(Get-Random -Minimum 10000000000 -Maximum 99999999999).ToString(); email="teste@cleanvander.local"; telefone="45999990000"; endereco="Rua de teste, 123" }
  Assert ($client.ClienteId -gt 0 -and $client.ChaveAcesso.Length -gt 20) "cadastro cria cliente e chave local"
  $clientHeaders=@{ "X-Client-Key"=$client.ChaveAcesso }
  $badKey=Invoke-WebRequest "$BaseUrl/api/orders" -Headers @{ "X-Client-Key"="chave-invalida" } -SkipHttpErrorCheck
  Assert ($badKey.StatusCode -eq 401) "chave inválida não acessa pedidos"
  $adminHeaders=@{ "X-Admin-Key"=$adminKey }
  $createdProduct=SendJson POST "/api/admin/products" @{ codigo="TEST-01"; descricao="Produto de teste"; categoria="Teste"; unidadeMedida="UN"; tamanhoVolume="1 L"; preco=10.25; ativo=$true } $adminHeaders
  Assert ($createdProduct.Id -gt 0) "empresa cadastra produto"
  $order=SendJson POST "/api/orders" @{ itens=@(@{produtoId=$createdProduct.Id; quantidade=2}) } $clientHeaders
  Assert ($order.Total -eq 20.50 -and $order.Status -eq "Recebido") "pedido aplica preço do catálogo e inicia recebido"
  $history=SendJson GET "/api/orders" $null $clientHeaders
  Assert ($history.Count -eq 1 -and $history[0].Numero -eq $order.Numero) "cliente consulta seu próprio histórico"
  $status=SendJson PUT "/api/admin/orders/$($order.Id)/status" @{status="EmSeparacao"} $adminHeaders
  Assert ($status.Status -eq "EmSeparacao") "empresa atualiza status do pedido"
  $clients=SendJson GET "/api/admin/clients" $null $adminHeaders
  Assert ($clients.Count -eq 1 -and $clients[0].Nome -eq "Cliente Teste CleanVander") "painel autorizado consulta cadastros"
  $edit=SendJson PUT "/api/admin/products/$($createdProduct.Id)" @{codigo="TEST-01";descricao="Produto de teste atualizado";categoria="Teste";unidadeMedida="UN";tamanhoVolume="2 L";preco=12.75;ativo=$true} $adminHeaders
  Assert ($edit.Preco -eq 12.75 -and $edit.TamanhoVolume -eq "2 L") "empresa atualiza produto"
  $invalidStatus=Invoke-WebRequest "$BaseUrl/api/admin/orders/$($order.Id)/status" -Method PUT -ContentType "application/json" -Body '{"status":"Estoque"}' -Headers $adminHeaders -SkipHttpErrorCheck
  Assert ($invalidStatus.StatusCode -eq 400) "API bloqueia status fora da lista prevista"
  Write-Host "`n$passed testes passaram." -ForegroundColor Cyan
}
finally {
  if ($server -and -not $server.HasExited) { $server.Kill($true); $server.WaitForExit() }
  foreach($suffix in @("", "-shm", "-wal")){ $file="$dbFile$suffix"; if(Test-Path -LiteralPath $file){Remove-Item -LiteralPath $file -Force} }
}

