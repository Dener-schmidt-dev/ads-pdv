const { app, BrowserWindow, dialog } = require('electron');
const { spawn } = require('child_process');
const path = require('path');
const http = require('http');

const isDev = !app.isPackaged;
const API_PORT = process.env.MEUPDV_API_PORT || '5050';
const API_URL = process.env.MEUPDV_API_URL || `http://localhost:${API_PORT}`;
const WEB_URL = isDev
  ? (process.env.MEUPDV_WEB_URL || 'http://localhost:4200')
  : API_URL;
const FULLSCREEN = isDev
  ? process.env.MEUPDV_FULLSCREEN === '1'
  : process.env.MEUPDV_FULLSCREEN !== '0';

const API_PROJECT = path.join(__dirname, '..', 'MeuPdv.Api', 'MeuPdv.Api.csproj');

let mainWindow = null;
let apiProcess = null;

const gotLock = app.requestSingleInstanceLock();
if (!gotLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
    }
  });
}

function checkUrl(url) {
  return new Promise((resolve) => {
    const req = http.get(url, (res) => {
      resolve(res.statusCode >= 200 && res.statusCode < 500);
      res.resume();
    });
    req.on('error', () => resolve(false));
    req.setTimeout(2000, () => {
      req.destroy();
      resolve(false);
    });
  });
}

function startApiDev() {
  apiProcess = spawn('dotnet', ['run', '--project', API_PROJECT], {
    cwd: path.join(__dirname, '..', 'MeuPdv.Api'),
    stdio: 'ignore',
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: API_URL
    }
  });
  apiProcess.on('error', (err) => console.error('Falha ao iniciar API:', err.message));
}

function startApiProduction() {
  const apiDir = path.join(process.resourcesPath, 'api');
  const exe = path.join(apiDir, 'MeuPdv.Api.exe');

  apiProcess = spawn(exe, [], {
    cwd: apiDir,
    stdio: 'ignore',
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: API_URL
    }
  });

  apiProcess.on('error', (err) => {
    dialog.showErrorBox('MeuPDV — Erro', `Não foi possível iniciar a API:\n${err.message}`);
  });
}

function startApi() {
  if (process.env.MEUPDV_SKIP_API === '1') return;

  if (isDev) {
    startApiDev();
  } else {
    startApiProduction();
  }
}

function stopApi() {
  if (!apiProcess) return;
  try {
    if (process.platform === 'win32') {
      spawn('taskkill', ['/pid', String(apiProcess.pid), '/f', '/t'], { stdio: 'ignore', windowsHide: true });
    } else {
      apiProcess.kill('SIGTERM');
    }
  } catch {
    apiProcess.kill();
  }
  apiProcess = null;
}

async function waitForApi(maxAttempts = 45) {
  for (let i = 0; i < maxAttempts; i++) {
    if (await checkUrl(`${API_URL}/api/health`)) return true;
    await new Promise((r) => setTimeout(r, 1000));
  }
  return false;
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1366,
    height: 768,
    minWidth: 1024,
    minHeight: 600,
    fullscreen: FULLSCREEN,
    title: 'MeuPDV',
    autoHideMenuBar: !isDev,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.loadURL(WEB_URL);

  if (isDev && process.env.MEUPDV_DEVTOOLS === '1') {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }

  mainWindow.on('closed', () => { mainWindow = null; });
}

app.whenReady().then(async () => {
  const apiRunning = await checkUrl(`${API_URL}/api/health`);

  if (!apiRunning) {
    startApi();
    const started = await waitForApi();
    if (!started) {
      dialog.showErrorBox(
        'MeuPDV — Erro',
        'Não foi possível conectar à API local.\n\n' +
        'Verifique se o .NET Runtime e o MongoDB estão instalados e em execução.'
      );
      app.quit();
      return;
    }
  }

  if (isDev) {
    const webRunning = await checkUrl(WEB_URL);
    if (!webRunning) {
      dialog.showMessageBox({
        type: 'warning',
        title: 'MeuPDV',
        message: 'Frontend Angular não detectado.',
        detail: 'Execute "npm start" em src/MeuPdv.Web ou defina MEUPDV_WEB_URL.'
      });
    }
  }

  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  stopApi();
  if (process.platform !== 'darwin') app.quit();
});

app.on('before-quit', () => stopApi());
