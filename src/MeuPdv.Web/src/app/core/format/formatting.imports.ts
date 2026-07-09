import { CurrencyInputDirective } from '../directives/currency-input.directive';
import { PercentInputDirective } from '../directives/percent-input.directive';
import { BrlPipe } from '../pipes/brl.pipe';
import { PercentBrPipe } from '../pipes/percent-br.pipe';

export const FORMATTING_IMPORTS = [
  BrlPipe,
  PercentBrPipe,
  CurrencyInputDirective,
  PercentInputDirective
] as const;
