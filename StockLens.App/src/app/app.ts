import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DashboardComponent } from './dashboard/dashboard';
import { InventoryComponent } from './inventory/inventory';
import { StrategiesComponent } from './strategies/strategies';
import { RealtimeService } from './core/realtime.service';

type Tab = 'dashboard' | 'inventory' | 'strategies';

interface NavItem {
  tab: Tab;
  label: string;
  icon: string;
}

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

  protected readonly navItems: NavItem[] = [
    { tab: 'dashboard', label: 'Dashboard', icon: '◈' },
    { tab: 'inventory', label: 'Inventory', icon: '▤' },
    { tab: 'strategies', label: 'Strategies', icon: '◎' },
  ];

  protected readonly today = new Date().toLocaleDateString(undefined, {
    weekday: 'short', month: 'short', day: 'numeric',
  });

  protected readonly pageTitle = computed(
    () => this.navItems.find((i) => i.tab === this.tab())?.label ?? '');

  ngOnInit(): void {
    this.realtime.start();
  }

  select(tab: Tab): void {
    this.tab.set(tab);
  }
}
