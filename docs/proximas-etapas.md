# Próximas etapas

O MVP (etapas 1–9 + polish) está concluído. Veja o status detalhado em [`PLANO_IMPLEMENTACAO.md`](../PLANO_IMPLEMENTACAO.md).

## Etapa 10 — Pós-MVP (não implementado)

- Sincronização com nuvem (repositórios já com `CreatedAt`/`UpdatedAt` e idempotência em estoque)
- Emissão NFC-e (`IFiscalService` + feature flag `FiscalEnabled`)
- Servidor de licenças dedicado (`License:ValidationUrl` já suporta validação online na ativação)

## Melhorias opcionais

- `PUT /api/usuarios/{id}` — editar usuário existente
- UI para criar categorias de produto
- Uso de `LogAuditoria` nas operações sensíveis
- Repositório dedicado para `LicencaLocal`
- Mais testes de integração na API

## Módulo fiscal (quando implementar NFC-e)

Preparação existente:

- `ConfiguracaoSistema.FiscalEnabled`
- `IFiscalService` + `FiscalServiceDisabled`
- Campos em `Venda`: `StatusFiscal`, `XmlFiscal`, `ChaveFiscal`, `ProtocoloFiscal`

Regras:

1. Nova implementação de `IFiscalService`, sem simular autorização SEFAZ
2. Não misturar regras fiscais com o fluxo de venda
3. Etapa independente do MVP offline

## Licenciamento

- Ativação offline com chave `MPDV-AAAAMMDD-XXXXXXXX`
- Validação online opcional na ativação
- Bloqueio de finalizar/cancelar venda sem licença válida
