import { app, BrowserWindow, ipcMain } from 'electron';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { spawn } from 'child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const isDev = process.env.NODE_ENV === 'development';

let mainWindow;
let serverProcess;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1600,
    height: 1000,
    minWidth: 1200,
    minHeight: 800,
    webPreferences: {
      preload: join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  if (isDev) {
    mainWindow.loadURL('http://localhost:5173');
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(join(__dirname, 'build', 'index.html'));
  }

  mainWindow.webContents.session.setPermissionRequestHandler((_webContents, permission, cb) => {
    cb(permission === 'media');
  });
}

function startServer() {
  const serverDir = join(__dirname, 'server');
  serverProcess = spawn('node', ['index.js'], {
    cwd: serverDir,
    env: { ...process.env, NODE_ENV: isDev ? 'development' : 'production' },
    stdio: isDev ? 'inherit' : 'ignore',
  });
  serverProcess.on('error', err => console.error('[server]', err.message));
}

app.whenReady().then(() => {
  startServer();
  createWindow();
  app.on('activate', () => { if (BrowserWindow.getAllWindows().length === 0) createWindow(); });
});

app.on('window-all-closed', () => {
  serverProcess?.kill();
  if (process.platform !== 'darwin') app.quit();
});

ipcMain.handle('get-app-version', () => app.getVersion());
ipcMain.handle('app-close', () => app.quit());
