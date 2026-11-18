# Clubes — aplicativo móvel (.NET MAUI)

Aplicativo para **associados**, **dependentes** e **portaria** da plataforma Clubes (multi-clube). O nome do clube exibido vem sempre do usuário autenticado; nada no app é fixo para um clube específico.

O app **não contém regras de negócio**: permissões, inadimplência, capacidade, conflitos de horário e limites são validados pela API. O app apenas exibe o estado devolvido pelo servidor, mostra a mensagem de erro da API (`message`) e nunca apresenta uma operação como concluída antes da confirmação do servidor.

## Pré-requisitos

- .NET SDK 10 com as cargas de trabalho MAUI: `dotnet workload install maui-android maui-ios` (no Windows, `maui-windows` é usado apenas como verificação rápida de compilação).
- Android: Android SDK (API 36) e um emulador ou aparelho com depuração USB.
- iOS: um Mac com Xcode (compilação e execução exigem macOS; no Windows é preciso parear com um Mac pelo Visual Studio).
- A API em execução (por padrão `http://localhost:5080`, Swagger em `/swagger`).

## Como executar

```powershell
cd mobile\Clubes.Mobile

# Compilar
dotnet build -f net10.0-android -c Debug

# Executar no emulador/aparelho Android conectado
dotnet build -f net10.0-android -c Debug -t:Run

# iOS (em um Mac, simulador)
dotnet build -f net10.0-ios -c Debug -t:Run

# Verificação rápida de compilação no Windows (não é um alvo de distribuição)
dotnet build -f net10.0-windows10.0.19041.0 -c Debug
```

## Endereço da API

| Plataforma | Padrão |
| --- | --- |
| Emulador Android | `http://10.0.2.2:5080` (a máquina host vista pelo emulador) |
| Simulador iOS / Windows | `http://localhost:5080` |

Para apontar para outro servidor (aparelho físico, homologação, produção): na tela de login, **toque duas vezes no texto da versão** (rodapé) para exibir o campo **Servidor** e informe a URL (ex.: `http://192.168.0.10:5080` ou `https://api.seuclube.com.br`). O valor fica em `Preferences` (não é segredo) e pode ser restaurado para o padrão no mesmo lugar. Os padrões estão em `Services/SessionStore.cs` (`AppConfig.DefaultBaseUrl`).

Em aparelho físico Android com a API na sua máquina, use o IP da máquina na rede local ou `adb reverse tcp:5080 tcp:5080` e a URL `http://localhost:5080`.

### HTTP sem TLS (somente Debug)

- **Android**: `Platforms/Android/MainApplication.cs` define `UsesCleartextTraffic = true` apenas com `#if DEBUG`. Em Release o tráfego sem TLS é bloqueado.
- **iOS**: `Platforms/iOS/Info.Debug.plist` (exceção de ATS) é mesclado ao `Info.plist` apenas na configuração Debug (item `PartialAppManifest` no `.csproj`).

Em produção a API deve ser servida por HTTPS.

## Sessão e segurança

- Os tokens de acesso e de renovação ficam **somente no `SecureStorage`** (Keystore/Keychain). Nada é gravado em `Preferences` ou arquivos.
- O `ApiClient` anexa o token, e em `401` renova a sessão **uma vez** (`POST /api/auth/refresh`). O token de renovação é de uso único (rotação): as renovações são serializadas por um `SemaphoreSlim` e o novo token é persistido antes de repetir a chamada. Se a renovação for recusada, a sessão é apagada e o app volta ao login.
- Sair chama `POST /api/auth/logout` e limpa o `SecureStorage`.
- Ao alterar a senha o servidor encerra todas as sessões; o app entra novamente com a nova senha recém-digitada (que não é armazenada).
- O QR Code da carteirinha é de uso único: é pedido ao servidor só com a tela visível e o app em primeiro plano, nunca é guardado nem reutilizado, e é renovado quando expira.

## Notificações

- **Tempo real (SignalR)**: o app conecta em `{servidor}/hubs/clube` após o login (reconexão automática). Eventos: `notification` (atualiza o contador de não lidas e exibe um aviso local do sistema via `Plugin.LocalNotification`), `availabilityChanged` (recarrega a grade de horários aberta) e `occupancyChanged` (recarrega as telas da portaria). O contador também é atualizado ao voltar para o app.
- Os avisos locais só aparecem enquanto o app está em execução. No Android 13+ o app pede a permissão de notificações no primeiro acesso.

### Push (FCM/APNs) — ainda não habilitado

O registro do aparelho está implementado (`IPushRegistration` → `POST /api/notificacoes/dispositivos {token, platform}`; `DELETE` ao sair), mas o provedor de token padrão (`NotConfiguredPushTokenProvider`, em `Services/PushRegistration.cs`) devolve `null`, porque o clube ainda não possui credenciais Firebase/APNs. Nesse estado nada é registrado e nenhum token é inventado.

Para habilitar:

1. **Firebase**: crie o projeto e cadastre os apps com os identificadores do `.csproj` (`ApplicationId`, hoje `br.com.clubes.app`): Android (nome do pacote) e iOS (Bundle ID).
2. **Android**: baixe o `google-services.json`, coloque em `Clubes.Mobile/Platforms/Android/` e inclua no `.csproj`: `<GoogleServicesJson Include="Platforms\Android\google-services.json" />`. Adicione um pacote de FCM (por exemplo `Plugin.Firebase.CloudMessaging` ou `Xamarin.Firebase.Messaging`).
3. **iOS**: no portal da Apple, habilite *Push Notifications* no App ID e gere a chave APNs (`.p8`), enviando-a ao Firebase (Cloud Messaging → Apple app configuration). Baixe o `GoogleService-Info.plist`, coloque em `Clubes.Mobile/Platforms/iOS/` com ação de compilação `BundleResource`, e adicione `Platforms/iOS/Entitlements.plist` com `aps-environment` (`development`/`production`).
4. **Código**: crie uma implementação de `IPushTokenProvider` por plataforma que devolva o token do FCM (`Platform` = `"android"` ou `"ios"`) e troque o registro em `MauiProgram.cs` (`AddSingleton<IPushTokenProvider, ...>`). O restante (envio do token após o login e remoção ao sair) já está ligado em `AppRouter`.
5. **Servidor**: configure as credenciais de envio (conta de serviço do Firebase) na API.

## Estrutura do projeto

```
Clubes.Mobile/
  MauiProgram.cs        Injeção de dependência (serviços, view models, páginas)
  App.xaml(.cs)         Recursos globais e ciclo de vida da janela
  AppShell.cs           Abas inferiores por perfil e rotas de navegação
  Models/               DTOs do contrato da API, enums e rótulos em português
  Services/
    ApiClient.cs        HttpClient tipado: Bearer, renovação em 401, ApiException(Code, Message, Status)
    SessionStore.cs     AppConfig (URL da API) e sessão no SecureStorage
    AuthService.cs      Login, sessão, perfil de uso (associado, dependente, portaria)
    ClubApi.cs          Todas as rotas usadas pelo app
    RealtimeService.cs  SignalR
    NotificationState.cs   Contador de não lidas e avisos locais
    PushRegistration.cs IPushRegistration / IPushTokenProvider
    FileService.cs      Imagens e downloads autenticados, envio de fotos
    AppRouter.cs        Tela raiz conforme o estado da sessão
    Dialogs.cs          Diálogos, navegação e máscaras
  ViewModels/           MVVM (CommunityToolkit.Mvvm)
  Views/                Páginas XAML (bindings compilados)
  Controls/             StatusChip, StateView (carregando/erro), EmptyView, FormField, ChoiceChip, MenuRow, AuthImage
  Resources/Styles/     Design system: Colors.xaml e Styles.xaml
  Resources/Images/     Ícones vetoriais (SVG)
  Platforms/            Manifestos e pontos de entrada de Android, iOS e Windows
```

## Perfis

- **Associado**: Início, Carteirinha, Reservas, Comunicados, Perfil — e, a partir do Início/Perfil: Convidados, Eventos, Financeiro, Manifestações, Assembleias e votações, Dependentes, Notificações.
- **Dependente**: Início, Carteirinha, Reservas (somente com a permissão `reservations.request`), Comunicados, Perfil. Não vê financeiro, assembleias/votações nem gestão de dependentes. Convidados e inscrição em eventos dependem de `guests.register` e `events.register`.
- **Equipe com `gate.operate`**: Portaria (Leitor de QR Code e código numérico, Convidados do dia, Busca e registro manual, Presentes, Perfil).
- **Demais usuários da equipe e superadministrador**: tela informando que o app é destinado a associados, dependentes e portaria.
