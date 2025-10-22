# MeuPDV

PDV offline para minimercados, mercearias e conveniências. Produto simples, rápido e de baixo custo.

## Pré-requisitos

| Ferramenta | Versão mínima | Observação |
|------------|---------------|------------|
| .NET SDK | 10.0 | ✅ Já detectado no ambiente |
| Node.js | **20 LTS ou superior** | ⚠️ Atualizar se estiver em v14/v16 |
| MongoDB Community | 7.x ou 8.x | Instalar e iniciar o serviço |
| npm | 10+ | Vem com Node 20+ |

### Instalar MongoDB (Windows)

1. Baixe em [mongodb.com/try/download/community](https://www.mongodb.com/try/download/community)
2. Instale como serviço Windows
3. Confirme: `mongosh` ou verifique o serviço **MongoDB Server**

### Atualizar Node.js (se necessário)

Baixe Node 20 LTS em [nodejs.org](https://nodejs.org/) e reinicie o terminal.

## Estrutura

```text
src/
├── MeuPdv.Domain/           Entidades e regras puras
├── MeuPdv.Application/    Casos de uso e interfaces
├── MeuPdv.Infrastructure.Mongo/  Repositórios MongoDB
├── MeuPdv.Api/              API local ASP.NET Core
├── MeuPdv.Web/              Frontend Angular 21
└── MeuPdv.Desktop/          Shell Electron 42
```

## Executar em desenvolvimento

### Atalho (recomendado)

Na raiz do projeto, com MongoDB rodando:

```powershell
.\dev.ps1
```

O script **instala automaticamente** o que faltar (.NET SDK 10, Node.js LTS e MongoDB via `winget`), instala dependências do projeto, abre janelas da API e do Angular e lança o navegador em `http://localhost:4200`.

Na primeira execução pode pedir **permissão de administrador** (UAC) para instalar ferramentas do sistema.

| Comando | Ação |
|---------|------|
| `.\dev.ps1` | Instala o necessário + sobe API + Angular |
| `.\dev.ps1 -Desktop` | Inclui Electron |
| `.\dev.ps1 -Setup` | Só instala/verifica (não sobe serviços) |
| `.\dev.ps1 -Stop` | Encerra a sessão dev |
| `.\dev.ps1 -NoAutoInstall` | Não instala .NET/Node/MongoDB (só verifica) |

Login dev: `admin` / `admin123`

### Manual (3 terminais)

**1. API** — `cd src/MeuPdv.Api` → `dotnet run` → `http://localhost:5050`

**2. Frontend** — `cd src/MeuPdv.Web` → `npm install` → `npm start` → `http://localhost:4200`

**3. Desktop (opcional)** — `cd src/MeuPdv.Desktop` → `npm install` → `npm start`

### Testes

```powershell
dotnet test
```

## Dados de desenvolvimento

Criados automaticamente ao iniciar a API em modo Development:

| Campo | Valor |
|-------|-------|
| Usuário | `admin` |
| Senha | `admin123` |
| Perfil | Administrador |

Produtos de exemplo com códigos de barras reais para teste com bipador.

## Diretórios do sistema (produção)

| Caminho | Uso |
|---------|-----|
| `%ProgramData%\MeuPDV\data` | Dados MongoDB |
| `%ProgramData%\MeuPDV\logs` | Logs da aplicação |
| `%ProgramData%\MeuPDV\backups` | Backups |

## Gerar instalador Windows

```powershell
.\installer\build.ps1
```

Instalador em `dist/desktop/`. Ver [installer/README.md](installer/README.md).

## Documentação

- [Plano de implementação](PLANO_IMPLEMENTACAO.md)
- [Arquitetura](docs/arquitetura.md)
- [Instalação](docs/instalacao.md)
- [Atalhos do PDV](docs/atalhos-pdv.md)
- [Próximas etapas](docs/proximas-etapas.md)
<!-- bot-contribuicoes: 21 -->
