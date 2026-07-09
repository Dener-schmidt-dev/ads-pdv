const { contextBridge } = require('electron');

contextBridge.exposeInMainWorld('meupdv', {
  offline: true,
  versao: '0.1.0',
  desktop: true
});
