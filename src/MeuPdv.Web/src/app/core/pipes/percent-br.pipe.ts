import { Pipe, PipeTransform } from '@angular/core';
import { formatPercent } from '../format/number-format';

@Pipe({
  name: 'percentBr',
  standalone: true
})
export class PercentBrPipe implements PipeTransform {
  transform(value: number | null | undefined, decimals = 2): string {
    return formatPercent(value, decimals);
  }
}
