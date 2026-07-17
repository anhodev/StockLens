import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DashboardComponent } from './dashboard/dashboard';
import { InventoryComponent } from './inventory/inventory';
import { StrategiesComponent } from './strategies/strategies';
import { RealtimeService } from './core/realtime.service';

type Tab = 'dashboard' | 'inventory' | 'strategies';

@Component({
  selector: 'app-root',
  imports: [DashboardComponent, InventoryComponent, StrategiesComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App implements OnInit {
  protected readonly realtime = inject(RealtimeService);
  protected readonly tab = signal<Tab>('dashboard');

  ngOnInit(): void {
    this.realtime.start();
  }

  select(tab: Tab): void {
    this.tab.set(tab);
  }
}
