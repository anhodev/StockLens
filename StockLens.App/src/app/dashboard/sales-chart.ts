import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  Input, OnChanges, OnDestroy, ViewChild,
} from '@angular/core';
import {
  BarController, BarElement, CategoryScale, Chart, Filler, Legend,
  LineController, LineElement, LinearScale, PointElement, Tooltip,
} from 'chart.js';
import { SalesTrendPoint } from '../core/models';

// Register only what this chart uses, so tree-shaking can drop the rest of Chart.js.
Chart.register(
  BarController, BarElement, LineController, LineElement, PointElement,
  CategoryScale, LinearScale, Tooltip, Legend, Filler,
);

const money = (n: number) =>
  new Intl.NumberFormat(undefined, {
    style: 'currency', currency: 'USD', maximumFractionDigits: 0,
  }).format(n);

/** Sales performance over the trailing months: units sold as bars, revenue as a trend line. */
@Component({
  selector: 'app-sales-chart',
  template: `<div class="chart-host"><canvas #canvas></canvas></div>`,
  styles: [`
    .chart-host { position: relative; height: 248px; padding: 14px 16px 16px; }
    canvas { width: 100% !important; height: 100% !important; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SalesChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') private canvasRef?: ElementRef<HTMLCanvasElement>;

  @Input({ required: true }) points: SalesTrendPoint[] = [];

  private chart?: Chart;

  ngAfterViewInit(): void {
    this.render();
  }

  // Inputs can change before the canvas exists, and again on every live update.
  ngOnChanges(): void {
    this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return; // view not initialised yet; ngAfterViewInit will call back

    const labels = this.points.map((p) => p.label);
    const units = this.points.map((p) => p.units);
    const revenue = this.points.map((p) => p.revenue);

    // Update in place rather than rebuilding, so live SignalR pushes animate smoothly.
    if (this.chart) {
      this.chart.data.labels = labels;
      this.chart.data.datasets[0].data = units;
      this.chart.data.datasets[1].data = revenue;
      this.chart.update();
      return;
    }

    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            type: 'bar',
            label: 'Units sold',
            data: units,
            yAxisID: 'yUnits',
            backgroundColor: '#cfdcfb',
            hoverBackgroundColor: '#2563eb',
            borderRadius: 4,
            barPercentage: 0.6,
            categoryPercentage: 0.7,
            order: 2,
          },
          {
            type: 'line',
            label: 'Revenue',
            data: revenue,
            yAxisID: 'yRevenue',
            borderColor: '#16a34a',
            backgroundColor: 'rgba(22, 163, 74, 0.08)',
            borderWidth: 2,
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: '#16a34a',
            pointBorderColor: '#fff',
            pointBorderWidth: 1.5,
            order: 1,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              usePointStyle: true, boxWidth: 8, boxHeight: 8,
              padding: 14, color: '#5c5f66', font: { size: 11 },
            },
          },
          tooltip: {
            backgroundColor: '#16171a',
            padding: 10,
            cornerRadius: 6,
            titleFont: { size: 12 },
            bodyFont: { size: 12 },
            callbacks: {
              label: (ctx) => {
                const value = ctx.parsed.y ?? 0;
                return ctx.dataset.yAxisID === 'yRevenue'
                  ? ` Revenue: ${money(value)}`
                  : ` Units sold: ${value}`;
              },
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            border: { color: '#e6e6e8' },
            ticks: { color: '#9b9ea5', font: { size: 11 } },
          },
          yUnits: {
            type: 'linear',
            position: 'left',
            beginAtZero: true,
            grid: { color: '#f0f0f2' },
            border: { display: false },
            ticks: { precision: 0, color: '#9b9ea5', font: { size: 11 } },
            title: { display: true, text: 'Units', color: '#9b9ea5', font: { size: 10 } },
          },
          yRevenue: {
            type: 'linear',
            position: 'right',
            beginAtZero: true,
            grid: { display: false },
            border: { display: false },
            ticks: {
              color: '#9b9ea5',
              font: { size: 11 },
              callback: (value) => `$${Math.round(Number(value) / 1000)}k`,
            },
            title: { display: true, text: 'Revenue', color: '#9b9ea5', font: { size: 10 } },
          },
        },
      },
    });
  }
}
