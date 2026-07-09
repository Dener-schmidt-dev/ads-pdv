# Arquitetura — MeuPDV

## Visão geral

O MeuPDV é um sistema **desktop offline** composto por três processos principais:

1. **Electron** — janela do aplicativo, inicialização da API, mensagens de erro amigáveis
2. **Angular** — interface do operador (telas, atalhos, modais)
3. **ASP.NET Core API** — regras de negócio, persistência, logs

O MongoDB roda localmente como banco único (`meupdv`).

## Camadas .NET

### MeuPdv.Domain
- Entidades (`Produto`, `Venda`, `SessaoCaixa`, etc.)
- Enums e regras puras (`VendaCalculator`, `PagamentoValidator`)
- Sem dependência de frameworks

### MeuPdv.Application
- Interfaces de repositório e serviços
- Opções de configuração (`MongoDbOptions`, `MeuPdvPathsOptions`)
- Casos de uso (a expandir nas próximas etapas)

### MeuPdv.Infrastructure.Mongo
- `MongoDbContext` e repositórios
- Índices (`MongoIndexInitializer`)
- Seed de desenvolvimento
- Implementações de `ILicenseService` e `IFiscalService`

### MeuPdv.Api
- Controllers REST
- Middleware de exceções
- Serilog (console + arquivo)
- Swagger apenas em Development

## Fluxo de dados

```
Angular Component → HttpClient → API Controller → Application Service → Repository → MongoDB
```

O Angular **nunca** acessa o MongoDB diretamente.

## Preparação para o futuro

| Interface | Implementação atual | Evolução |
|-----------|---------------------|----------|
| `ILicenseService` | Validação local + modo dev | API remota de licenciamento |
| `IFiscalService` | `FiscalServiceDisabled` | NFC-e real (etapa isolada) |
| Repositórios | MongoDB local | Sincronização com API remota |

## Índices MongoDB

- `usuarios.login` (único)
- `produtos.codigoBarras`, `codigoInterno` (único), `descricaoNormalizada`
- `vendas.createdAt`, `sessaoCaixaId`, `operadorId`
- `sessoes_caixa.caixaIdentificador + status`
- `movimentacoes_estoque.createdAt`, `produtoId`, `idempotencyKey` (único)
