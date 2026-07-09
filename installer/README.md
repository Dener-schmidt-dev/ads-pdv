# Instalador Windows — MeuPDV

## Pré-requisitos na máquina de build

| Ferramenta | Versão |
|------------|--------|
| .NET SDK | 10.0 |
| Node.js | 20 LTS+ |
| npm | 10+ |

## Pré-requisitos na máquina do cliente

| Ferramenta | Observação |
|------------|------------|
| .NET Runtime | Incluído no instalador (publish self-contained win-x64) |
| MongoDB Community | Serviço local em `localhost:27017` |
| MongoDB Database Tools | Para backup/restauração (Etapa 8) |

## Gerar instalador

Na raiz do repositório:

```powershell
.\installer\build.ps1
```

Saídas:

| Caminho | Conteúdo |
|---------|----------|
| `dist/api/` | API publicada com frontend em `wwwroot` |
| `dist/desktop/MeuPDV Setup 0.1.0.exe` | Instalador NSIS (electron-builder) |

### Opções

```powershell
# Apenas API + wwwroot (sem instalador)
.\installer\build.ps1 -SkipInstaller

# Build Debug
.\installer\build.ps1 -Configuration Debug
```

## O que o instalador faz

1. Instala o MeuPDV em `C:\Users\<user>\AppData\Local\Programs\meupdv-desktop` (padrão electron-builder)
2. Cria atalho na área de trabalho e menu Iniciar
3. Ao abrir: inicia a API local + janela Electron em tela cheia
4. Desinstalação **não** remove `%ProgramData%\MeuPDV\backups` por padrão

## Diretórios de dados (criados pela API na primeira execução)

```text
%ProgramData%\MeuPDV\
├── data\
├── logs\
└── backups\
```

## Primeiro acesso

Na primeira execução em produção, se não houver usuários:

| Campo | Valor |
|-------|-------|
| Login | `admin` |
| Senha | `admin123` |

Altere a senha após o primeiro login.

## Desenvolvimento (sem instalador)

```powershell
# Terminal 1 — API
cd src\MeuPdv.Api
dotnet run

# Terminal 2 — Angular
cd src\MeuPdv.Web
npm start

# Terminal 3 — Electron
cd src\MeuPdv.Desktop
npm install
npm start
```

Variáveis úteis:

| Variável | Efeito |
|----------|--------|
| `MEUPDV_SKIP_API=1` | Não inicia a API (já rodando) |
| `MEUPDV_FULLSCREEN=1` | Tela cheia em dev |
| `MEUPDV_DEVTOOLS=1` | Abre DevTools em dev |

## Testar build de produção localmente (sem NSIS)

```powershell
.\installer\build.ps1 -SkipInstaller
cd src\MeuPdv.Desktop
npm run dist:dir
# Executar dist\desktop\win-unpacked\MeuPDV.exe
```
