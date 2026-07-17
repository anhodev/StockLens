import { CurrencyPipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import { Vehicle, VehicleFilter, VehicleStatus } from '../core/models';
import { VehicleDetailComponent } from './vehicle-detail';
import { CarImageComponent } from './car-image';

type ViewMode = 'table' | 'grid';

@Component({
  selector: 'app-inventory',
  imports: [CurrencyPipe, DecimalPipe, NgClass, FormsModule, VehicleDetailComponent, CarImageComponent],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private realtime = inject(RealtimeService);
  private unsubs: Array<() => void> = [];

  readonly vehicles = signal<Vehicle[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly selected = signal<Vehicle | null>(null);

  readonly statuses: VehicleStatus[] = ['InStock', 'Reserved', 'Sold'];
  readonly makes = signal<string[]>([]);
  readonly view = signal<ViewMode>('table');

  // Filter form state.
  search = '';
  make = '';
  status: VehicleStatus | '' = '';
  agingOnly = false;
  sortBy: 'age' | 'price' | 'make' = 'age';
  desc = true;
  page = 1;
  readonly pageSize = 25;

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  ngOnInit(): void {
    this.load();
    this.loadMakes();
    // A vehicle or action change anywhere refreshes the current list view
    // (e.g. open-action counts) — live across all connected dashboards.
    this.unsubs.push(this.realtime.onVehicle(() => this.load()));
    this.unsubs.push(this.realtime.onAction(() => this.load()));
  }

  ngOnDestroy(): void {
    this.unsubs.forEach((u) => u());
  }

  private currentFilter(): VehicleFilter {
    return {
      search: this.search.trim() || undefined,
      make: this.make || undefined,
      status: this.status || undefined,
      agingOnly: this.agingOnly || undefined,
      sortBy: this.sortBy,
      desc: this.desc,
      page: this.page,
      pageSize: this.pageSize,
    };
  }

  load(): void {
    this.loading.set(true);
    this.api.getVehicles(this.currentFilter()).subscribe({
      next: (res) => {
        this.vehicles.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadMakes(): void {
    // Derive the make dropdown from the full inventory once.
    this.api.getVehicles({ pageSize: 200 }).subscribe((res) => {
      this.makes.set([...new Set(res.items.map((v) => v.make))].sort());
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.search = '';
    this.make = '';
    this.status = '';
    this.agingOnly = false;
    this.sortBy = 'age';
    this.desc = true;
    this.applyFilters();
  }

  changePage(delta: number): void {
    const next = this.page + delta;
    if (next < 1 || next > this.totalPages()) return;
    this.page = next;
    this.load();
  }

  open(v: Vehicle): void {
    this.selected.set(v);
  }

  setView(mode: ViewMode): void {
    this.view.set(mode);
  }

  onDetailChanged(): void {
    this.load();
  }

  statusBadge(status: VehicleStatus): string {
    switch (status) {
      case 'InStock': return 'badge-instock';
      case 'Reserved': return 'badge-reserved';
      default: return 'badge-sold';
    }
  }
}
