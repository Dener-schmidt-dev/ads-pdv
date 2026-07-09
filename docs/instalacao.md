# Instalação e execução — MeuPDV

## Desenvolvimento

### Atalho — script `dev.ps1`

Na raiz do repositório:

```powershell
.\dev.ps1
```

Instala automaticamente via **winget** o que faltar (.NET SDK 10, Node.js LTS, MongoDB), instala dependências do projeto, sobe API e Angular em janelas separadas e abre o navegador. Pode solicitar **UAC (admin)** na primeira vez.

```powershell
.\dev.ps1 -Setup           # só instalar/verificar
.\dev.ps1 -Desktop         # incluir Electron
.\dev.ps1 -Stop            # encerrar tudo
.\dev.ps1 -NoAutoInstall   # não instalar ferramentas do sistema
```

Se acabou de instalar Node ou .NET, feche e abra um **novo terminal** antes de rodar de novo (atualização do PATH).

### Passo 1 — MongoDB

Instale o MongoDB Community Edition e inicie o serviço:

```powershell
# Verificar se o serviço está rodando (Windows)
Get-Service MongoDB
```

Connection string padrão: `mongodb://localhost:27017`  
Banco: `meupdv`

### Passo 2 — API

```powershell
cd src/MeuPdv.Api
dotnet run
```

Na primeira execução em Development:
- Índices são criados automaticamente
- Usuário `admin` / senha `admin123` é criado
- 5 produtos de exemplo são inseridos

### Passo 3 — Frontend

Requer **Node.js 20+**.

```powershell
cd src/MeuPdv.Web
npm install
npm start
```

### Passo 4 — Electron (opcional em dev)

```powershell
cd src/MeuPdv.Desktop
npm install
npm start
```

## Produção / Instalador

### Gerar instalador

```powershell
.\installer\build.ps1
```

Instalador gerado em `dist/desktop/MeuPDV Setup 0.1.0.exe`.

Detalhes em [installer/README.md](../installer/README.md).

### Pré-requisitos no PC do cliente

- **MongoDB Community** em execução (`localhost:27017`)
- **MongoDB Database Tools** (para backup/restauração)

O .NET Runtime está incluído no instalador (publish self-contained).

### Primeiro acesso (produção)

| Campo | Valor |
|-------|-------|
| Login | `admin` |
| Senha | `admin123` |

Altere a senha após instalar.

### Modo produção local (sem instalador)

```powershell
.\installer\build.ps1 -SkipInstaller
cd src\MeuPdv.Desktop
npm run dist:dir
# Executar dist\desktop\win-unpacked\MeuPDV.exe
```

## Solução de problemas

| Problema | Solução |
|----------|---------|
| API retorna `mongodb: false` no health | Inicie o serviço MongoDB |
| `npm install` falha no Angular | Atualize Node para 20 LTS |
| Logs | `%ProgramData%\MeuPDV\logs\meupdv-*.log` |
