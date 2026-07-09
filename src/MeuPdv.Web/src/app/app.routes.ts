import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { ShellComponent } from './layout/shell/shell.component';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { UsuariosComponent } from './pages/usuarios/usuarios.component';
import { UsuarioFormComponent } from './pages/usuarios/usuario-form.component';
import { ProdutosComponent } from './pages/produtos/produtos.component';
import { ProdutoFormComponent } from './pages/produtos/produto-form.component';
import { EstoqueComponent } from './pages/estoque/estoque.component';
import { CaixaComponent } from './pages/caixa/caixa.component';
import { VendaComponent } from './pages/venda/venda.component';
import { ConfiguracoesComponent } from './pages/configuracoes/configuracoes.component';
import { RelatoriosComponent } from './pages/relatorios/relatorios.component';
import { BackupComponent } from './pages/backup/backup.component';
import { ClientesComponent } from './pages/clientes/clientes.component';
import { ClienteFormComponent } from './pages/clientes/cliente-form.component';
import { VendasHistoricoComponent } from './pages/vendas-historico/vendas-historico.component';

const produtoGuard = permissionGuard('CadastrarProduto');
const adminGuard = permissionGuard('AcessarConfiguracoes');

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      { path: 'inicio', component: HomeComponent },
      {
        path: 'venda',
        component: VendaComponent
      },
      {
        path: 'vendas',
        component: VendasHistoricoComponent,
        canActivate: [permissionGuard('CancelarVenda')]
      },
      {
        path: 'produtos/novo',
        component: ProdutoFormComponent,
        canActivate: [produtoGuard]
      },
      {
        path: 'produtos/:id',
        component: ProdutoFormComponent,
        canActivate: [produtoGuard]
      },
      {
        path: 'produtos',
        component: ProdutosComponent,
        canActivate: [produtoGuard]
      },
      {
        path: 'estoque',
        component: EstoqueComponent
      },
      {
        path: 'caixa',
        component: CaixaComponent
      },
      {
        path: 'clientes/novo',
        component: ClienteFormComponent
      },
      {
        path: 'clientes/:id',
        component: ClienteFormComponent
      },
      {
        path: 'clientes',
        component: ClientesComponent
      },
      {
        path: 'usuarios/novo',
        component: UsuarioFormComponent,
        canActivate: [adminGuard]
      },
      {
        path: 'usuarios',
        component: UsuariosComponent,
        canActivate: [adminGuard]
      },
      {
        path: 'configuracoes',
        component: ConfiguracoesComponent,
        canActivate: [adminGuard]
      },
      {
        path: 'relatorios',
        component: RelatoriosComponent,
        canActivate: [permissionGuard('VisualizarRelatorios')]
      },
      {
        path: 'backup',
        component: BackupComponent,
        canActivate: [adminGuard]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
