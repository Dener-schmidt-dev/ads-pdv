import {
  Component,
  ElementRef,
  EventEmitter,
  forwardRef,
  inject,
  Input,
  Output,
  signal,
  viewChild
} from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ClienteDto } from '../../core/models/cliente.models';
import { ClienteLookupValue } from './lookup.models';

@Component({
  selector: 'app-cliente-lookup',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './cliente-lookup.component.html',
  styleUrl: './cliente-lookup.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ClienteLookupComponent),
      multi: true
    }
  ]
})
export class ClienteLookupComponent implements ControlValueAccessor {
  private readonly http = inject(HttpClient);

  readonly inputRef = viewChild<ElementRef<HTMLInputElement>>('inputRef');

  @Input() disabled = false;
  @Input() dropUp = false;
  @Input() placeholder = 'Buscar cliente por nome, CPF ou telefone…';
  @Output() readonly solicitarCriar = new EventEmitter<string>();

  readonly sugestoes = signal<ClienteDto[]>([]);
  readonly aberto = signal(false);
  readonly indice = signal(0);
  readonly buscando = signal(false);

  termo = '';

  private valor: ClienteLookupValue | null = null;
  private buscaTimer?: ReturnType<typeof setTimeout>;
  private onChange: (v: ClienteLookupValue | null) => void = () => {};
  private onTouched: () => void = () => {};

  get selecionado(): ClienteLookupValue | null {
    return this.valor;
  }

  writeValue(value: ClienteLookupValue | null): void {
    this.valor = value;
    this.termo = value?.nome ?? '';
  }

  registerOnChange(fn: (v: ClienteLookupValue | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  focar(): void {
    this.inputRef()?.nativeElement?.focus();
  }

  onInput(): void {
    if (this.disabled) return;

    if (this.valor && this.termo !== this.valor.nome) {
      this.valor = null;
      this.onChange(null);
    }

    clearTimeout(this.buscaTimer);
    const termo = this.termo.trim();

    if (termo.length < 2) {
      this.sugestoes.set([]);
      this.aberto.set(false);
      return;
    }

    this.buscando.set(true);
    this.buscaTimer = setTimeout(() => {
      this.http.get<ClienteDto[]>(`/api/clientes/busca?termo=${encodeURIComponent(termo)}`).subscribe({
        next: (itens) => {
          this.sugestoes.set(itens);
          this.indice.set(0);
          this.aberto.set(true);
          this.buscando.set(false);
        },
        error: () => {
          this.sugestoes.set([]);
          this.aberto.set(false);
          this.buscando.set(false);
        }
      });
    }, 200);
  }

  onKeydown(event: KeyboardEvent): void {
    if (!this.aberto()) {
      if (event.key === 'ArrowDown' && this.termo.trim().length >= 2) {
        this.onInput();
      }
      return;
    }

    const total = this.totalOpcoes();

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.indice.update((i) => Math.min(i + 1, total - 1));
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.indice.update((i) => Math.max(i - 1, 0));
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      this.selecionarIndice(this.indice());
      return;
    }

    if (event.key === 'Escape') {
      this.fecharLista();
    }
  }

  totalOpcoes(): number {
    return this.sugestoes().length + (this.mostrarCriar() ? 1 : 0);
  }

  mostrarCriar(): boolean {
    return this.termo.trim().length >= 2 && !this.buscando();
  }

  indiceCriar(): number {
    return this.sugestoes().length;
  }

  selecionarIndice(index: number): void {
    if (this.mostrarCriar() && index === this.indiceCriar()) {
      this.abrirCriar();
      return;
    }

    const cliente = this.sugestoes()[index];
    if (cliente) this.selecionar(cliente);
  }

  selecionar(cliente: ClienteDto): void {
    const valor: ClienteLookupValue = { id: cliente.id, nome: cliente.nome };
    this.valor = valor;
    this.termo = cliente.nome;
    this.onChange(valor);
    this.onTouched();
    this.fecharLista();
  }

  limpar(): void {
    this.valor = null;
    this.termo = '';
    this.onChange(null);
    this.onTouched();
    this.fecharLista();
    this.focar();
  }

  abrirCriar(): void {
    this.solicitarCriar.emit(this.termo.trim());
    this.fecharLista();
  }

  fecharLista(): void {
    this.aberto.set(false);
    this.sugestoes.set([]);
  }

  onBlur(): void {
    this.onTouched();
    setTimeout(() => this.fecharLista(), 180);
  }
}
