import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  ViewChild
} from '@angular/core';
import { Chart, ChartConfiguration } from 'chart.js';
import './chart-setup';

@Component({
  selector: 'app-relatorio-chart',
  standalone: true,
  template: `
    <div class="relatorio-chart" [class.relatorio-chart--empty]="empty">
      @if (empty) {
        <div class="relatorio-chart__empty">
          <span class="mi" aria-hidden="true">bar_chart</span>
          <p>Sem dados para exibir no período.</p>
        </div>
      } @else {
        <canvas #canvas role="img" [attr.aria-label]="titulo"></canvas>
      }
    </div>
  `,
  styleUrl: './relatorio-chart.component.scss'
})
export class RelatorioChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() config: ChartConfiguration | null = null;
  @Input() titulo = 'Gráfico';
  @Input() empty = false;

  @ViewChild('canvas') private canvasRef?: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;
  private viewReady = false;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.render();
  }

  ngOnChanges(): void {
    this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(): void {
    if (!this.viewReady) return;

    this.chart?.destroy();
    this.chart = undefined;

    if (this.empty || !this.config) return;

    const el = this.canvasRef?.nativeElement;
    if (!el) return;

    this.chart = new Chart(el, this.config);
  }
}
