export type PerfilUsuario = 'Administrador' | 'OperadorCaixa' | 'Fiscal';

export type Permissao =
  | 'CadastrarProduto'
  | 'AlterarPreco'
  | 'ConcederDesconto'
  | 'CancelarItem'
  | 'CancelarVenda'
  | 'MovimentarEstoque'
  | 'VisualizarRelatorios'
  | 'AbrirCaixa'
  | 'FecharCaixa'
  | 'AcessarConfiguracoes';

export interface UsuarioLogado {
  id: string;
  login: string;
  nome: string;
  perfil: PerfilUsuario;
  permissoes: Permissao[];
}

export interface LoginResponse {
  token: string;
  expiraEm: string;
  usuario: UsuarioLogado;
}

export interface UsuarioDto {
  id: string;
  login: string;
  nome: string;
  perfil: PerfilUsuario;
  permissoesExtras: Permissao[];
  permissoesEfetivas: Permissao[];
  ativo: boolean;
  ultimoAcesso?: string;
  createdAt: string;
}

export interface PerfilInfo {
  valor: number;
  nome: PerfilUsuario;
  permissoes: Permissao[];
}
