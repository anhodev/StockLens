import { CurrencyPipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, filter, map, skip } from 'rxjs';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import { Vehicle, VehicleFilter, VehicleStatus } from '../core/models';
import { VehicleDetailComponent } from './vehicle-detail';
import { CarImageComponent } from './car-image';

type ViewMode = 'table' | 'grid';
type SortKey = 'age' | 'price' | 'make';

/** Minimum term length before a search auto-runs; a cleared box always re-runs. */
const MIN_SEARCH_LENGTH = 3;
const SEARCH_DEBOUNCE_MS = 300;

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
  readonly agingCount = signal(0);

  readonly statuses: VehicleStatus[] = ['Open', 'Deposited', 'Hold', 'Sold'];
  readonly makes = signal<string[]>([]);
  readonly view = signal<ViewMode>('grid');

  // Filter form state.
  readonly search = signal('');
  make = '';
  status: VehicleStatus | '' = 'Open';
  agingOnly = false;
  sortBy: SortKey = 'age';
  desc = true;
  page = 1;
  readonly pageSize = 50;

  /** The term the currently-displayed results were fetched with; guards against duplicate requests. */
  private lastAppliedSearch = '';

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  /** Nudge shown while a term is too short to trigger a search. */
  readonly searchHint = computed(() => {
    const term = this.search().trim();
    return term.length > 0 && term.length < MIN_SEARCH_LENGTH
      ? `Type ${MIN_SEARCH_LENGTH}+ characters to search`
      : '';
  });

  constructor() {
    // Auto-run the search once the term reaches MIN_SEARCH_LENGTH, or when it is
    // cleared entirely. Debounced so typing doesn't fire a request per keystroke.
    toObservable(this.search)
      .pipe(
        skip(1), // ignore the initial value emitted on subscribe
        map((term) => term.trim()),
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        filter((term) => term.length === 0 || term.length >= MIN_SEARCH_LENGTH),
        // Skip terms already reflected on screen (e.g. after Enter or Reset all).
        filter((term) => term !== this.lastAppliedSearch),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.applyFilters());
  }

  ngOnInit(): void {
    this.load();
    this.loadMakes();
    this.loadAgingCount();
    // A vehicle or action change anywhere refreshes the current list view
    // (e.g. open-action counts) — live across all connected dashboards.
    this.unsubs.push(this.realtime.onVehicle((v) => {
      if (this.selected()?.id === v.id) this.selected.set(v);
      this.load();
      this.loadAgingCount();
    }));
    this.unsubs.push(this.realtime.onAction(() => this.load()));
  }

  ngOnDestroy(): void {
    this.unsubs.forEach((u) => u());
  }

  private currentFilter(): VehicleFilter {
    return {
      search: this.search().trim() || undefined,
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
    // Derive the brand dropdown from the full inventory once.
    this.api.getVehicles({ pageSize: 200 }).subscribe((res) => {
      this.makes.set([...new Set(res.items.map((v) => v.make))].sort());
    });
  }

  private loadAgingCount(): void {
    this.api.getAgingStock().subscribe((list) => this.agingCount.set(list.length));
  }

  applyFilters(): void {
    this.lastAppliedSearch = this.search().trim();
    this.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.search.set('');
    this.make = '';
    this.status = '';
    this.agingOnly = false;
    this.sortBy = 'age';
    this.desc = true;
    this.applyFilters();
  }

  setStatus(status: VehicleStatus | ''): void {
    this.status = status;
    this.applyFilters();
  }

  toggleAging(): void {
    this.agingOnly = !this.agingOnly;
    this.applyFilters();
  }

  setSort(key: SortKey): void {
    this.sortBy = key;
    this.applyFilters();
  }

  toggleDesc(): void {
    this.desc = !this.desc;
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

  /** The drawer changed the vehicle (e.g. a status move): refresh both it and the list. */
  onDetailChanged(updated: Vehicle): void {
    this.selected.set(updated);
    this.load();
    this.loadAgingCount();
  }

  /** True when the vehicle has a real trim worth showing (blanks and dash placeholders don't count). */
  hasTrim(v: Vehicle): boolean {
    const t = (v.trim ?? '').trim();
    return t !== '' && t !== '—' && t !== '-' && t !== '--';
  }

  statusBadge(status: VehicleStatus): string {
    switch (status) {
      case 'Open': return 'badge-instock';
      case 'Deposited': return 'badge-open';
      case 'Hold': return 'badge-reserved';
      default: return 'badge-sold';
    }
  }

  statusLabel(status: VehicleStatus | ''): string {
    return status === '' ? 'Any' : status;
  }

  /** Label for the active sort when descending (the API treats age-desc as oldest-first). */
  sortDescLabel(): string {
    switch (this.sortBy) {
      case 'price': return 'Highest price';
      case 'make': return 'Brand Z–A';
      default: return 'Oldest first';
    }
  }

  sortAscLabel(): string {
    switch (this.sortBy) {
      case 'price': return 'Lowest price';
      case 'make': return 'Brand A–Z';
      default: return 'Newest first';
    }
  }
}
