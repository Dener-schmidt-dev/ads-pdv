import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { NotificationService } from '../../core/notification/notification.service';
import { LicencaInfo } from '../../core/models/licenca.models';
import { ConfiguracaoPdv } from '../../core/models/pdv-config.models';
import { PdvThemeService } from '../../core/pdv/pdv-theme.service';

type ConfigSecao = 'empresa' | 'sistema' | 'personalizacao' | 'licenca' | 'senha';

interface ConfigNavItem {
  id: ConfigSecao;
  label: string;
  descricao: string;
  icon: string;
}

interface ConfigEmpresa {
  nomeEstabelecimento: string;
  cnpj?: string;
  endereco?: string;
  chavePix?: string;
  tipoChavePix?: string;
}

interface ConfigSistema {
  impressaoAutomatica: boolean;
  nomeImpressora?: string;
  estoqueNegativoPermitido: boolean;
  limiteDescontoOperadorPercentual: number;
  diretorioBackup: string;
  intervaloBackupMinutos: number;
  diasRetencaoBackup: number;
  temaVisual: string;
}

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [FormsModule, DatePipe, ...FORMATTING_IMPORTS],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss'
})
export class ConfiguracoesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  private readonly pdvTheme = inject(PdvThemeService);

  readonly secaoAtiva = signal<ConfigSecao>('empresa');

  readonly nav: ConfigNavItem[] = [
    { id: 'empresa', label: 'Empresa', descricao: 'Dados e Pix', icon: 'store' },
    { id: 'sistema', label: 'Sistema', descricao: 'PDV e backup', icon: 'tune' },
    { id: 'personalizacao', label: 'Personalização', descricao: 'Visual do PDV', icon: 'palette' },
    { id: 'licenca', label: 'Licença', descricao: 'Ativação', icon: 'verified_user' },
    { id: 'senha', label: 'Segurança', descricao: 'Alterar senha', icon: 'lock' }
  ];

  readonly salvando = signal(false);

  empresa: ConfigEmpresa = {
    nomeEstabelecimento: '',
    cnpj: '',
    endereco: '',
    chavePix: '',
    tipoChavePix: 'Email'
  };

  sistema: ConfigSistema = {
    impressaoAutomatica: false,
    nomeImpressora: '',
    estoqueNegativoPermitido: false,
    limiteDescontoOperadorPercentual: 5,
    diretorioBackup: '',
    intervaloBackupMinutos: 1440,
    diasRetencaoBackup: 30,
    temaVisual: 'claro'
  };

  pdv: ConfiguracaoPdv = {
    corPrimaria: '#2563eb',
    exibirNomeEmpresa: true,
    temImagemFundo: false
  };

  removerImagemFundoPdv = false;
  previewImagemFundo: string | null = null;
  private imagemFundoArquivo: File | null = null;

  tiposChavePix = ['Cpf', 'Cnpj', 'Email', 'Telefone', 'Aleatoria'];

  readonly licenca = signal<LicencaInfo | null>(null);
  chaveLicenca = '';
  senhaAtual = '';
  senhaNova = '';
  senhaNovaConfirmacao = '';

  ngOnInit(): void {
    this.carregarLicenca();
    this.http.get<ConfigEmpresa>('/api/configuracoes/empresa').subscribe({
      next: (e) => {
        this.empresa = {
          ...e,
          tipoChavePix: e.tipoChavePix ?? 'Email'
        };
      }
    });
    this.http.get<ConfigSistema>('/api/configuracoes/sistema').subscribe({
      next: (s) => this.sistema = s
    });
    this.http.get<ConfiguracaoPdv>('/api/configuracoes/pdv').subscribe({
      next: (p) => {
        this.pdv = p;
        this.previewImagemFundo = p.temImagemFundo ? this.urlImagemFundoPreview(p) : null;
      }
    });
  }

  urlImagemFundoPreview(p: ConfiguracaoPdv): string {
    const v = p.imagemFundoVersao ? `?v=${p.imagemFundoVersao}` : '';
    return `/api/pdv/imagem-fundo${v}`;
  }

  onImagemFundoSelecionada(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      this.notify.error('A imagem deve ter no máximo 2 MB.');
      input.value = '';
      return;
    }

    this.imagemFundoArquivo = file;
    this.removerImagemFundoPdv = false;
    this.previewImagemFundo = URL.createObjectURL(file);
  }

  marcarRemoverImagemFundo(): void {
    this.imagemFundoArquivo = null;
    this.removerImagemFundoPdv = true;
    this.previewImagemFundo = null;
  }

  salvarPersonalizacao(): void {
    this.salvando.set(true);

    this.http.put<ConfiguracaoPdv>('/api/configuracoes/pdv', {
      corPrimaria: this.pdv.corPrimaria,
      exibirNomeEmpresa: this.pdv.exibirNomeEmpresa,
      removerImagemFundo: this.removerImagemFundoPdv
    }).subscribe({
      next: (p) => {
        const upload$ = this.imagemFundoArquivo
          ? this.uploadImagemFundo(this.imagemFundoArquivo)
          : null;

        if (upload$) {
          upload$.subscribe({
            next: (atualizado) => this.finalizarPersonalizacao(atualizado),
            error: (e) => {
              this.salvando.set(false);
              this.notify.apiError(e, 'Erro ao enviar imagem de fundo.');
            }
          });
        } else {
          this.finalizarPersonalizacao(p);
        }
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao salvar personalização.');
      }
    });
  }

  private uploadImagemFundo(arquivo: File) {
    const form = new FormData();
    form.append('arquivo', arquivo);
    return this.http.post<ConfiguracaoPdv>('/api/configuracoes/pdv/imagem-fundo', form);
  }

  private finalizarPersonalizacao(p: ConfiguracaoPdv): void {
    this.pdv = p;
    this.imagemFundoArquivo = null;
    this.removerImagemFundoPdv = false;
    this.previewImagemFundo = p.temImagemFundo ? this.urlImagemFundoPreview(p) : null;
    this.pdvTheme.config.set(p);
    this.pdvTheme.aplicarCorPrimaria(p.corPrimaria);
    this.salvando.set(false);
    this.notify.success('Personalização salva para todos os usuários.');
  }

  selecionarSecao(id: ConfigSecao): void {
    this.secaoAtiva.set(id);
  }

  tituloSecao(): string {
    return this.nav.find((n) => n.id === this.secaoAtiva())?.label ?? 'Configurações';
  }

  salvarEmpresa(): void {
    this.salvar('/api/configuracoes/empresa', this.empresa);
  }

  salvarSistema(): void {
    this.salvar('/api/configuracoes/sistema', {
      impressaoAutomatica: this.sistema.impressaoAutomatica,
      nomeImpressora: this.sistema.nomeImpressora,
      estoqueNegativoPermitido: this.sistema.estoqueNegativoPermitido,
      limiteDescontoOperadorPercentual: this.sistema.limiteDescontoOperadorPercentual,
      diretorioBackup: this.sistema.diretorioBackup,
      intervaloBackupMinutos: this.sistema.intervaloBackupMinutos,
      diasRetencaoBackup: this.sistema.diasRetencaoBackup
    });
  }

  carregarLicenca(): void {
    this.http.get<LicencaInfo>('/api/licenca').subscribe({
      next: (l) => this.licenca.set(l)
    });
  }

  ativarLicenca(): void {
    this.salvando.set(true);
    this.http.post<LicencaInfo>('/api/licenca/ativar', { chave: this.chaveLicenca }).subscribe({
      next: (l) => {
        this.licenca.set(l);
        this.chaveLicenca = '';
        this.salvando.set(false);
        this.notify.success('Licença ativada.');
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao ativar licença.');
      }
    });
  }

  alterarSenha(): void {
    if (this.senhaNova !== this.senhaNovaConfirmacao) {
      this.notify.error('A confirmação da nova senha não confere.');
      return;
    }

    this.salvando.set(true);
    this.http.post('/api/auth/alterar-senha', {
      senhaAtual: this.senhaAtual,
      senhaNova: this.senhaNova
    }).subscribe({
      next: () => {
        this.salvando.set(false);
        this.senhaAtual = '';
        this.senhaNova = '';
        this.senhaNovaConfirmacao = '';
        this.notify.success('Senha alterada.');
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao alterar senha.');
      }
    });
  }

  private salvar(url: string, body: unknown): void {
    this.salvando.set(true);

    this.http.put(url, body).subscribe({
      next: () => {
        this.salvando.set(false);
        this.notify.success('Configurações salvas.');
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao salvar.');
      }
    });
  }
}
