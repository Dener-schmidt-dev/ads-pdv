import { Pipe, PipeTransform } from '@angular/core';
import { formatBrl } from '../format/number-format';

@Pipe({
  name: 'brl',
  standalone: true
})
export class BrlPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return formatBrl(value);
  }
}
