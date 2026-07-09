import { Directive, ElementRef, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { formatPercent, formatPercentInput, parsePercent } from '../format/number-format';

@Directive({
  selector: 'input[appPercentInput]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PercentInputDirective),
      multi: true
    }
  ],
  host: {
    class: 'input-percent',
    type: 'text',
    inputmode: 'decimal',
    autocomplete: 'off',
    '(focus)': 'onFocus()',
    '(blur)': 'onBlur()'
  }
})
export class PercentInputDirective implements ControlValueAccessor {
  private readonly el = inject(ElementRef<HTMLInputElement>);

  private value = 0;
  private editing = false;
  private onChange: (value: number) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: number | null): void {
    this.value = value ?? 0;
    if (!this.editing) {
      this.el.nativeElement.value = formatPercent(this.value);
    }
  }

  registerOnChange(fn: (value: number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.el.nativeElement.disabled = isDisabled;
  }

  onFocus(): void {
    this.editing = true;
    this.el.nativeElement.value = formatPercentInput(this.value);
    this.el.nativeElement.select();
  }

  onBlur(): void {
    this.editing = false;
    this.value = parsePercent(this.el.nativeElement.value);
    this.el.nativeElement.value = formatPercent(this.value);
    this.onChange(this.value);
    this.onTouched();
  }
}
