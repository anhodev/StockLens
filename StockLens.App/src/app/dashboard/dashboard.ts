import { CurrencyPipe, DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import { DashboardSummary } from '../core/models';

/** A single headline metric rendered as a KPI card. */
interface Kpi {
  label: string;
  value: string;
  sub: string | null;
  icon: string;
  /** Accent name; drives the card's tint via a `kpi-{accent}` class. */
  accent: 'blue' | 'aging' | 'green' | 'violet' | 'teal' | 'slate';
}

const money = (n: number) =>
  new Intl.NumberFormat(undefined, {
    style: 'currency', currency: 'USD', maximumFractionDigits: 0,
  }).format(n);

const decimal = (n: number) =>
  new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 }).format(n);

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, DatePipe, NgClass],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private realtime = inject(RealtimeService);
  private unsub?: () => void;

  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(true);

  /** Headline metrics, pre-formatted so the template stays free of display logic. */
  readonly kpis = computed<Kpi[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      {
        label: 'Vehicles in stock', value: `${s.totalInStock}`,
        sub: 'on the lot now', icon: '▤', accent: 'blue',
      },
      {
        label: 'Aging stock (>90 days)', value: `${s.agingStockCount}`,
        sub: 'needs attention', icon: '▲', accent: 'aging',
      },
      {
        label: 'Total stock value', value: money(s.totalStockValue),
        sub: 'list price on hand', icon: '◈', accent: 'green',
      },
      {
        label: 'Avg days in inventory', value: decimal(s.averageDaysInInventory),
        sub: 'across in-stock units', icon: '◷', accent: 'violet',
      },
      {
        label: 'Sold (30 days)', value: `${s.soldLast30Days}`,
        sub: `${money(s.revenueLast30Days)} revenue`, icon: '✓', accent: 'teal',
      },
      {
        label: 'Avg days to sell', value: decimal(s.averageDaysToSell ?? 0),
        sub: 'acquisition to sale', icon: '⇄', accent: 'slate',
      },
    ];
  });

  ngOnInit(): void {
    this.load();
    // Live-refresh whenever the API broadcasts a dashboard change.
    this.unsub = this.realtime.onDashboard((d) => this.summary.set(d));
  }

  ngOnDestroy(): void {
    this.unsub?.();
  }

  load(): void {
    this.loading.set(true);
    this.api.getDashboard().subscribe({
      next: (d) => { this.summary.set(d); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  maxMakeCount(s: DashboardSummary): number {
    return Math.max(1, ...s.stockByMake.map((m) => m.count));
  }
}
