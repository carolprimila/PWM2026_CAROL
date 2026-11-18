# Clubes Web

Painel administrativo do sistema de gestão de clubes (React 19 + TypeScript + Vite + Tailwind CSS 4). Prioriza notebooks e desktops.
Consome exclusivamente a API REST (`backend/Clubes.API`) — nenhuma regra de negócio vive aqui.

## Executar

```bash
npm install
npm run dev        # http://localhost:5173 (API em http://localhost:5080 por padrão)
npm run typecheck  # verificação de tipos
npm run build      # build de produção em dist/
npm run gen:api    # regenera src/lib/api-types.gen.ts a partir de docs/api/openapi.json
```

A URL da API vem de `VITE_API_URL` (arquivo `.env.local` ou variável de build). Padrão: `http://localhost:5080`.

## Estrutura

```
src/
  app/            App (rotas e guardas), Layout (menu lateral, topo, notificações), telas de autenticação, registro de módulos
  components/     ui.tsx (design system), table.tsx (DataTable + usePaged), shared.tsx (Anexos, Histórico, MemberPicker)
  lib/            api.ts (cliente HTTP + sessão), auth.tsx, format.ts, labels.ts (enums → rótulos), realtime.ts (SignalR)
  features/<modulo>/index.tsx   cada módulo exporta um Feature (itens de menu + rotas); é descoberto automaticamente
```

## Convenções obrigatórias

1. **Módulo = pasta.** Cada módulo fica em `src/features/<nome>/` e exporta por padrão um `Feature` em `index.tsx`
   (`nav` + `routes`). O menu lateral e o roteador são montados por `import.meta.glob` — não existe arquivo central para editar.
   Veja `features/associados` (lista, formulário em modal, detalhe com abas) e `features/dashboard` como referência.
2. **Tipos vêm do contrato.** Use `Schema<'MemberDto'>` (de `@/lib/api`), gerado do OpenAPI. Não declare tipos de DTO à mão.
   Campos não nulos são obrigatórios; anuláveis são `campo?: T | null`.
3. **Chamadas à API** somente por `api.get/post/put/delete/upload/download/blob` e TanStack Query
   (`useQuery`/`useMutation`). Listas paginadas usam `usePaged(path, filtros)` + `<DataTable>`.
   Após uma mutação, invalide as consultas afetadas (`queryClient.invalidateQueries({ queryKey: ['/api/rota'] })` —
   a chave de `usePaged` começa pelo caminho).
4. **Nunca mostre sucesso antes da resposta do servidor.** Botões usam `loading={mutation.isPending}`; o toast de
   sucesso vai em `onSuccess`; erros em `onError: toast.error` (a mensagem em português vem da API).
5. **Permissões.** `const { can } = useAuth()`; esconda ações que o usuário não pode executar (`can('members.manage')`)
   e declare `permissions` nas rotas e itens de menu. A API valida tudo novamente — a interface é só conveniência.
   Códigos de permissão: `GET /api/funcoes/permissoes` ou `backend/Clubes.Application/Common/Permissions.cs`.
6. **Design system único** (`@/components/ui`): `Button`, `Field` + `Input`/`Select`/`Textarea`/`MaskedInput`/`MoneyInput`,
   `Switch`, `TriState`, `Card`/`CardTitle`, `Badge`/`StatusBadge`, `Alert`, `PageHeader`, `StatCard`, `Tabs`, `Info`,
   `Modal`, `useConfirm` (com motivo opcional/obrigatório), `useToast`, `Menu`, `Avatar`/`FileImage`/`FileUpload`,
   `LoadingState`/`EmptyState`/`ErrorState`/`QueryBoundary`. Não crie variações locais de botão, campo, card ou tabela;
   não use cores fora dos tokens (`brand-*`, `ink`, `muted`, `line`, `canvas`, `surface`, `success`, `warning`, `danger`, `info`).
7. **Enums → rótulos** por `labels` (`@/lib/labels`) e `<StatusBadge dict={labels.x} value={...} />`. Opções de `<Select>`
   por `options(labels.x)`.
8. **Formatação** por `fmt` (`money`, `date`, `dateTime`, `day` para `DateOnly`, `competence`, `minutes`, `cpf`, `phone`)
   e `dates` (conversões de `<input type="date|datetime-local">` ↔ ISO UTC). A API trabalha em UTC.
9. **Formulários**: React Hook Form + Zod, campos agrupados, máscaras, erro junto ao campo, confirmação (`useConfirm`)
   em operações críticas e motivo obrigatório onde a API exige (`reason`).
10. **Estados**: toda tela trata carregando, vazio e erro (com "Tentar novamente").
11. **Operações financeiras** enviam `idempotencyKey` (`newIdempotencyKey()`), gerada uma vez por tentativa e reutilizada
    se o usuário repetir o envio da mesma tentativa.
12. **Arquivos protegidos** (`/api/arquivos/{id}`) exigem token: use `Avatar`, `FileImage`, `useFileUrl` ou `api.download`.
13. **Tempo real**: `useRealtime('availabilityChanged' | 'occupancyChanged' | 'notification', handler)`.

## Segurança

- Token de acesso apenas em memória; token de renovação em `sessionStorage` (descartado ao fechar a aba, com rotação a
  cada uso). Sem cookies → sem CSRF. React escapa todo conteúdo renderizado (não usamos `dangerouslySetInnerHTML`).
- O clube do usuário vem do token. Somente o superadministrador escolhe o clube (cabeçalho `X-Club-Id`, validado no servidor).
