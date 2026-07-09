export interface BackupStatus {
  diretorioBackup: string;
  intervaloBackupMinutos: number;
  diasRetencaoBackup: number;
  ultimoBackupEm?: string;
  ultimoBackupSucesso: boolean;
  ferramentaDisponivel?: string;
}

export interface BackupHistorico {
  id: string;
  nomeArquivo: string;
  caminhoArquivo: string;
  tamanhoBytes: number;
  sucesso: boolean;
  mensagemErro?: string;
  automatico: boolean;
  createdAt: string;
}

export interface ExecutarBackupResultado {
  id: string;
  sucesso: boolean;
  mensagem?: string;
  nomeArquivo?: string;
  tamanhoBytes: number;
}
