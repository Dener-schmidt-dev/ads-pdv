import {
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  signal,
  computed
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import {
  DiaCalendario,
  diasSemana,
  formatarDataBr,
  hojeIso,
  labelMesAno,
  montarGradeCalendario
} from './date-input.utils';

@Component({
  selector: 'app-date-input',
  standalone: true,
  template: `
    <div class="date-input">
      <div
        class="date-input__field"
        [class.date-input__field--open]="aberto()"
        [class.date-input__field--disabled]="disabled"
      >
        <input
          class="date-input__text"
          type="text"
          readonly
          [value]="textoExibicao()"
          [placeholder]="placeholder"
          [disabled]="disabled"
          (click)="toggle()"
          (keydown.enter)="toggle()"
          (keydown.space)="toggle(); $event.preventDefault()"
          aria-haspopup="dialog"
          [attr.aria-expanded]="aberto()"
        />
        <button
          type="button"
          class="date-input__btn"
          [disabled]="disabled"
          (click)="toggle()"
          aria-label="Abrir calendário"
        >
          <span class="mi mi--sm" aria-hidden="true">calendar_month</span>
        </button>
      </div>

      @if (aberto()) {
        <div class="date-input__popup" role="dialog" aria-label="Selecionar data">
          <header class="date-input__head">
            <p class="date-input__mes">{{ labelMes() }}</p>
            <div class="date-input__nav">
              <button type="button" class="date-input__nav-btn" (click)="mesAnterior()" aria-label="Mês anterior">
                <span class="mi mi--sm" aria-hidden="true">chevron_left</span>
              </button>
              <button type="button" class="date-input__nav-btn" (click)="proximoMes()" aria-label="Próximo mês">
                <span class="mi mi--sm" aria-hidden="true">chevron_right</span>
              </button>
            </div>
          </header>

          <div class="date-input__weekdays" aria-hidden="true">
            @for (d of diasSemana; track d) {
              <span>{{ d }}</span>
            }
          </div>

          <div class="date-input__grid">
            @for (dia of grade(); track dia.data) {
              <button
                type="button"
                class="date-input__day"
                [class.date-input__day--out]="!dia.mesAtual"
                [class.date-input__day--today]="dia.hoje"
                [class.date-input__day--selected]="dia.selecionado"
                (click)="selecionar(dia)"
              >
                {{ dia.dia }}
              </button>
            }
          </div>

          <footer class="date-input__footer">
            <button type="button" class="date-input__action" (click)="limpar()">Limpar</button>
            <button type="button" class="date-input__action" (click)="selecionarHoje()">Hoje</button>
          </footer>
        </div>
      }
    </div>
  `,
  styleUrl: './date-input.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateInputComponent),
      multi: true
    }
  ]
})
export class DateInputComponent implements ControlValueAccessor {
  @Input() placeholder = 'dd/mm/aaaa';

  readonly aberto = signal(false);
  readonly valor = signal('');
  readonly mesVisivel = signal(this.parseMes(hojeIso()));

  readonly diasSemana = diasSemana();

  readonly textoExibicao = computed(() => formatarDataBr(this.valor()));

  readonly labelMes = computed(() =>
    labelMesAno(this.mesVisivel().ano, this.mesVisivel().mes)
  );

  readonly grade = computed((): DiaCalendario[] => {
    const { ano, mes } = this.mesVisivel();
    return montarGradeCalendario(ano, mes, this.valor() || undefined);
  });

  disabled = false;

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor(private readonly host: ElementRef<HTMLElement>) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.aberto()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.fechar();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.fechar();
  }

  writeValue(value: string | null): void {
    const iso = value ?? '';
    this.valor.set(iso);
    if (iso) {
      this.mesVisivel.set(this.parseMes(iso));
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (isDisabled) this.aberto.set(false);
  }

  toggle(): void {
    if (this.disabled) return;
    if (!this.aberto() && this.valor()) {
      this.mesVisivel.set(this.parseMes(this.valor()));
    }
    this.aberto.update((v) => !v);
    if (this.aberto()) this.onTouched();
  }

  fechar(): void {
    this.aberto.set(false);
  }

  mesAnterior(): void {
    this.mesVisivel.update(({ ano, mes }) => {
      const d = new Date(ano, mes - 1, 1);
      return { ano: d.getFullYear(), mes: d.getMonth() };
    });
  }

  proximoMes(): void {
    this.mesVisivel.update(({ ano, mes }) => {
      const d = new Date(ano, mes + 1, 1);
      return { ano: d.getFullYear(), mes: d.getMonth() };
    });
  }

  selecionar(dia: DiaCalendario): void {
    this.valor.set(dia.data);
    this.onChange(dia.data);
    this.fechar();
  }

  selecionarHoje(): void {
    const hoje = hojeIso();
    this.valor.set(hoje);
    this.mesVisivel.set(this.parseMes(hoje));
    this.onChange(hoje);
    this.fechar();
  }

  limpar(): void {
    this.valor.set('');
    this.onChange('');
    this.fechar();
  }

  private parseMes(iso: string): { ano: number; mes: number } {
    const [ano, mes] = iso.split('-').map(Number);
    if (!ano || Number.isNaN(mes)) {
      const hoje = new Date();
      return { ano: hoje.getFullYear(), mes: hoje.getMonth() };
    }
    return { ano, mes: mes - 1 };
  }
}
