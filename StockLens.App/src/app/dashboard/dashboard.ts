import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import { DashboardSummary } from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, DatePipe],
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
