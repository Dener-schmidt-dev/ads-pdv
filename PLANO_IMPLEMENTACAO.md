# Plano de Implementação — MeuPDV

Sistema desktop de PDV offline para minimercados. Produto comercial simples, rápido e de baixo custo de manutenção.

## Arquitetura proposta

```
┌─────────────────────────────────────────────────────────────┐
│                    MeuPdv.Desktop (Electron 42)              │
│  Inicia API local · Janela fullscreen · Tratamento de erros │
└──────────────────────────┬──────────────────────────────────┘
                           │ http://localhost:5050
┌──────────────────────────▼──────────────────────────────────┐
│                    MeuPdv.Web (Angular 21)                   │
│  Telas do PDV · Atalhos de teclado · Layout responsivo       │
└──────────────────────────┬──────────────────────────────────┘
                           │ REST
┌──────────────────────────▼──────────────────────────────────┐
│                    MeuPdv.Api (.NET 10)                      │
│  Controllers · Middleware · Swagger (dev) · Serilog          │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│              MeuPdv.Application (casos de uso)                │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│         MeuPdv.Infrastructure.Mongo (repositórios)              │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│              MongoDB Community (banco: meupdv)                │
└───────────────────────────────────────────────────────────────┘

MeuPdv.Domain — entidades e regras puras (sem dependências externas)
```

### Decisões técnicas

| Decisão | Motivo |
|---------|--------|
| Electron + Angular + API local | Offline total, UI moderna, API reutilizável para sync futuro |
| MongoDB local | Schema flexível, baixo custo, fácil backup com `mongodump` |
| `decimal` para valores | Precisão monetária obrigatória |
| Repositórios encapsulados | Angular nunca acessa banco diretamente |
| Serilog em arquivo | Logs em `%ProgramData%\MeuPDV\logs` |
| Seed de desenvolvimento | Produtos demo só em Development; admin/config no bootstrap |
| JWT em arquivo (produção) | Secret em `%ProgramData%\MeuPDV\data\jwt.secret` |
| Licença offline | Chave `MPDV-AAAAMMDD-XXXXXXXX`; URL online opcional |
| `IFiscalService` desabilitado | Preparado para NFC-e sem simular SEFAZ |

## Estrutura de diretórios

```text
MeuPDV/
├── README.md
├── PLANO_IMPLEMENTACAO.md
├── docs/
├── installer/          build.ps1 + README do instalador
├── src/
│   ├── MeuPdv.Domain/
│   ├── MeuPdv.Application/
│   ├── MeuPdv.Infrastructure.Mongo/
│   ├── MeuPdv.Api/
│   ├── MeuPdv.Web/
│   └── MeuPdv.Desktop/
└── tests/              20 testes (Domain + Api)
```

## Etapas de implementação

| # | Etapa | Status |
|---|-------|--------|
| 1 | Base — solução, MongoDB, logs, seed, docs | ✅ Concluída |
| 2 | Segurança — login, usuários, perfis, permissões | ✅ Concluída |
| 3 | Produtos e estoque | ✅ Concluída |
| 4 | Caixa | ✅ Concluída |
| 5 | Venda (tela principal) | ✅ Concluída |
| 6 | Pix (QR Code estático) | ✅ Concluída |
| 7 | Relatórios | ✅ Concluída |
| 8 | Backup | ✅ Concluída |
| 9 | Desktop + instalador Windows | ✅ Concluída |
| 10 | Licenciamento, sync, fiscal | 🔄 Parcial |
| — | Polish pós-MVP (estorno, senha, JWT, impressão) | ✅ Concluída |

## Checklist de funcionalidades (MVP)

### Infraestrutura
- [x] Solução .NET organizada em camadas
- [x] Entidades de domínio e regras críticas
- [x] Repositórios MongoDB, índices, Serilog, health check
- [x] Seed de desenvolvimento + bootstrap admin em produção
- [x] Interfaces `ILicenseService` e `IFiscalService`
- [x] Angular 21 + Electron 42 + instalador NSIS (`installer/build.ps1`)
- [x] JWT secret persistente em produção

### Segurança
- [x] Login JWT local (12h)
- [x] Perfis: administrador, operador, fiscal
- [x] Permissões por perfil + extras
- [x] CRUD de usuários (inativar)
- [x] Guards e interceptor Angular
- [x] Alterar senha do usuário logado (`POST /api/auth/alterar-senha`)

### Telas e módulos
- [x] Login
- [x] Venda (tela principal)
- [x] Estornos (`/vendas`, permissão `CancelarVenda`)
- [x] Produtos + CSV
- [x] Estoque
- [x] Clientes (CRUD + F2 na venda)
- [x] Caixa
- [x] Relatórios
- [x] Usuários
- [x] Configurações (empresa, sistema, licença, senha)
- [x] Backup

### Produtos e estoque
- [x] CRUD, categorias, CSV, movimentações, alertas, idempotência
- [x] Bloqueio de estoque negativo (configurável)

### Clientes
- [x] API CRUD + busca/cadastro rápido na venda (F2)
- [x] Tela dedicada: listagem, busca, criar, editar, inativar

### Venda
- [x] Bipador, autocomplete, atalhos F2–F6, modais
- [x] Pagamento misto, troco, Pix (QR + confirmação manual)
- [x] Baixa de estoque e movimentação de caixa
- [x] Comprovante conforme `impressaoAutomatica` na config
- [x] Estorno de venda concluída (caixa aberto, estoque + caixa)

### Licenciamento (parcial — Etapa 10)
- [x] Validação local com tolerância configurável
- [x] Ativação por chave offline (`MPDV-AAAAMMDD-XXXXXXXX`)
- [x] URL de validação online opcional (`License:ValidationUrl`)
- [x] Bloqueio de finalizar/cancelar venda sem licença válida
- [x] Tela de ativação em Configurações + status no health/início
- [ ] Servidor de licenças dedicado
- [ ] Sync nuvem
- [ ] NFC-e fiscal (`IFiscalService` real)

## Detalhamento por etapa

### Etapa 1 — Base
- [x] Solução em camadas, MongoDB, índices, Serilog, seed, health

### Etapa 2 — Segurança
- [x] JWT, usuários, perfis, permissões, telas login/usuários

### Etapa 3 — Produtos e estoque
- [x] CRUD, categorias, CSV, movimentações, alertas

### Etapa 4 — Caixa
- [x] Abertura, suprimento, sangria, fechamento, histórico
- [x] Estornos refletidos no fechamento (`CancelamentoVenda`)

### Etapa 5 — Venda
- [x] Tela principal, atalhos, modais, comprovante, estorno

### Etapa 6 — Pix
- [x] QR Code, config chave Pix, modal com confirmação manual

### Etapa 7 — Relatórios
- [x] Vendas por período, ranking, estoque baixo, CSV/PDF

### Etapa 8 — Backup
- [x] Manual/automático, `mongodump`/`mongorestore`, retenção, tela Backup

### Etapa 9 — Desktop + instalador
- [x] Electron prod/dev, `wwwroot`, fullscreen, `installer/build.ps1`, self-contained

### Polish pós-MVP
- [x] `POST /api/vendas/cancelar/{id}` — estorno com motivo
- [x] `GET /api/licenca`, `POST /api/licenca/ativar`
- [x] JWT em `%ProgramData%\MeuPDV\data\jwt.secret`
- [x] Alterar senha e impressão automática configurável
- [x] Permissões `ConcederDesconto`, `CancelarItem`, `AlterarPreco` (API + UI)
- [x] Interceptor 401 → logout; `refreshMe()` no boot do shell
- [x] Índices Mongo: clientes, movimentações de caixa, categorias
- [x] Caixa recarrega sessão após suprimento/sangria

## Próxima etapa

**Etapa 10 (restante):** sincronização com nuvem, emissão NFC-e e servidor de licenças dedicado.

## Como executar

```powershell
# Desenvolvimento (recomendado — instala o que faltar via winget e sobe API + Angular)
.\dev.ps1
.\dev.ps1 -Desktop       # inclui Electron
.\dev.ps1 -Stop          # encerra sessão
.\dev.ps1 -NoAutoInstall # só verifica ferramentas, não instala

# Manual
cd src/MeuPdv.Api && dotnet run
cd src/MeuPdv.Web && npm start

# Testes
dotnet test

# Instalador
.\installer\build.ps1
```

Credenciais dev: `admin` / `admin123` — alterar após instalar em produção.
