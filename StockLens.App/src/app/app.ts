import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DashboardComponent } from './dashboard/dashboard';
import { InventoryComponent } from './inventory/inventory';
import { StrategiesComponent } from './strategies/strategies';
import { RealtimeService } from './core/realtime.service';
import { ToastComponent } from './core/toast.component';
import { ToastService } from './core/toast.service';

type Tab = 'dashboard' | 'inventory' | 'strategies';

interface NavItem {
  tab: Tab;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-root',
  imports: [DashboardComponent, InventoryComponent, StrategiesComponent, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App implements OnInit, OnDestroy {
  protected readonly realtime = inject(RealtimeService);
  protected readonly tab = signal<Tab>('dashboard');
  private readonly toasts = inject(ToastService);
  private readonly unsubs: Array<() => void> = [];

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
    this.unsubs.push(
      this.realtime.onVehicle((v) => {
        const price = new Intl.NumberFormat(undefined, {
          style: 'currency', currency: 'USD', maximumFractionDigits: 0,
        }).format(v.netPrice);
        const detail = [
          v.status,
          `${v.daysInInventory}d on lot`,
          price,
          ...(v.discountPercent ? [`−${v.discountPercent}% off`] : []),
        ].join(' · ');
        this.toasts.show(`${v.year} ${v.make} ${v.model}`, detail, 'success');
      }),
      this.realtime.onAction((a) => {
        const parts = [`${a.status} · by ${a.createdBy}`];
        if (a.note) parts.push(a.note);
        this.toasts.show(a.actionType, parts.join(' — '), 'info');
      }),
      this.realtime.onStrategy((s) => {
        const parts: string[] = [`${s.scope} · ${s.scopeKey}`];
        if (s.discountPercent != null) parts.push(`−${s.discountPercent}%`);
        if (s.targetDaysToSell != null) parts.push(`${s.targetDaysToSell}d target`);
        this.toasts.show(s.name, parts.join(' · '), 'info');
      }),
    );
  }

  ngOnDestroy(): void {
    this.unsubs.forEach((u) => u());
  }

  select(tab: Tab): void {
    this.tab.set(tab);
  }
}
