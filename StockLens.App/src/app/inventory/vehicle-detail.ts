import { CurrencyPipe, DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges,
  OnDestroy, OnInit, Output, computed, inject, signal,
} from '@angular/core';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import {
  ACTION_STATUSES, ACTION_TYPES, ActionStatus, ActionType, ChangeStatusRequest,
  EffectiveStrategy, Salesperson, Vehicle, VehicleAction, VehicleStatus,
  VehicleStatusChange, VEHICLE_STATUSES,
} from '../core/models';

@Component({
  selector: 'app-vehicle-detail',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, NgClass, FormsModule],
  templateUrl: './vehicle-detail.html',
  styleUrl: './vehicle-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailComponent implements OnChanges, OnInit, OnDestroy {
  @Input({ required: true }) vehicle!: Vehicle;
  @Output() closed = new EventEmitter<void>();
  @Output() changed = new EventEmitter<Vehicle>();

  private api = inject(ApiService);
  private realtime = inject(RealtimeService);
  private unsub?: () => void;

  readonly actionTypes = ACTION_TYPES;
  readonly actionStatuses = ACTION_STATUSES;
  readonly allStatuses = VEHICLE_STATUSES;

  readonly actions = signal<VehicleAction[]>([]);
  readonly effective = signal<EffectiveStrategy | null>(null);
  readonly saving = signal(false);

  // --- Status workflow ---------------------------------------------------
  readonly salespeople = signal<Salesperson[]>([]);
  readonly history = signal<VehicleStatusChange[]>([]);
  readonly pendingStatus = signal<VehicleStatus | null>(null);
  readonly statusSaving = signal(false);
  readonly statusError = signal<string | null>(null);

  /** Fields the pending transition requires — drives which inputs the form shows. */
  readonly needsReason = computed(() =>
    this.pendingStatus() === 'Hold' || this.pendingStatus() === 'Open');
  readonly needsDeposit = computed(() => this.pendingStatus() === 'Deposited');
  readonly needsSaleDetails = computed(() => this.pendingStatus() === 'Sold');
  readonly needsSalesperson = computed(() =>
    this.pendingStatus() === 'Deposited' || this.pendingStatus() === 'Sold');

  /**
   * Every required field present? Mirrors the API's rules so the button can't submit junk.
   *
   * Deliberately a method, not a computed(): it reads `statusForm`, which is plain
   * ngModel-bound state rather than a signal. A computed would never see those edits and
   * would latch at its first value, leaving the form permanently unsubmittable.
   */
  statusFormValid(): boolean {
    if (!this.pendingStatus()) return false;
    if (this.needsReason() && !this.statusForm.reason.trim()) return false;
    if (this.needsDeposit() && !(this.statusForm.depositAmount! > 0)) return false;
    if (this.needsSaleDetails() && (!(this.statusForm.salePrice! > 0) || !this.statusForm.soldDate)) return false;
    if (this.needsSalesperson() && !this.statusForm.salespersonId) return false;
    return true;
  }

  statusForm = this.blankStatusForm();

  // New-action form state.
  newType: ActionType = 'PriceReductionPlanned';
  newStatus: ActionStatus = 'Open';
  newNote = '';
  newBy = 'manager';

  ngOnInit(): void {
    this.api.getSalespeople(true).subscribe((team) => this.salespeople.set(team));
    // Refresh actions live when the API broadcasts a change for this vehicle.
    this.unsub = this.realtime.onAction((a) => {
      if (a.vehicleId === this.vehicle.id) this.loadActions();
    });
  }

  ngOnChanges(): void {
    this.loadActions();
    this.loadStrategy();
    this.loadHistory();
    this.cancelStatusChange();
  }

  ngOnDestroy(): void {
    this.unsub?.();
  }

  private blankStatusForm() {
    return {
      reason: '',
      depositAmount: null as number | null,
      salePrice: null as number | null,
      soldDate: '' as string,
      salespersonId: '' as string,
      changedBy: 'manager',
    };
  }

  private loadActions(): void {
    this.api.getActions(this.vehicle.id).subscribe((a) => this.actions.set(a));
  }

  private loadHistory(): void {
    this.api.getStatusHistory(this.vehicle.id).subscribe((h) => this.history.set(h));
  }

  private loadStrategy(): void {
    this.effective.set(null);
    this.api.getEffectiveStrategy(this.vehicle.id).subscribe({
      next: (e) => this.effective.set(e),
      error: () => this.effective.set(null), // 204 = no applicable strategy
    });
  }

  /** Opens the change form for a target status, pre-filling sensible defaults. */
  startStatusChange(status: VehicleStatus): void {
    if (status === this.vehicle.status) return;
    this.statusForm = this.blankStatusForm();
    // Selling defaults to today at the current list price — the common case.
    this.statusForm.soldDate = new Date().toISOString().slice(0, 10);
    this.statusForm.salePrice = this.vehicle.listPrice;
    this.statusError.set(null);
    this.pendingStatus.set(status);
  }

  cancelStatusChange(): void {
    this.pendingStatus.set(null);
    this.statusError.set(null);
  }

  submitStatusChange(): void {
    const toStatus = this.pendingStatus();
    if (!toStatus || !this.statusFormValid()) return;

    const body: ChangeStatusRequest = {
      toStatus,
      changedBy: this.statusForm.changedBy?.trim() || 'manager',
      reason: this.needsReason() ? this.statusForm.reason.trim() : null,
      depositAmount: this.needsDeposit() ? this.statusForm.depositAmount : null,
      salePrice: this.needsSaleDetails() ? this.statusForm.salePrice : null,
      soldDate: this.needsSaleDetails() ? this.statusForm.soldDate : null,
      salespersonId: this.needsSalesperson() ? this.statusForm.salespersonId : null,
    };

    this.statusSaving.set(true);
    this.api.changeStatus(this.vehicle.id, body).subscribe({
      next: (updated) => {
        this.statusSaving.set(false);
        this.pendingStatus.set(null);
        this.loadHistory();
        this.changed.emit(updated); // parent refreshes the list and this drawer
      },
      error: (e) => {
        this.statusSaving.set(false);
        this.statusError.set(this.describeError(e));
      },
    });
  }

  /** Surfaces the API's validation messages rather than a generic failure. */
  private describeError(e: unknown): string {
    const err = e as { error?: { errors?: Record<string, string[]>; detail?: string; title?: string } };
    const errors = err?.error?.errors;
    if (errors) return Object.values(errors).flat().join(' ');
    return err?.error?.detail || err?.error?.title || 'Could not update the status. Please try again.';
  }

  addAction(): void {
    this.saving.set(true);
    this.api.createAction(this.vehicle.id, {
      actionType: this.newType,
      status: this.newStatus,
      note: this.newNote?.trim() || null,
      createdBy: this.newBy?.trim() || 'manager',
    }).subscribe({
      next: () => {
        this.newNote = '';
        this.loadActions();
        this.saving.set(false);
      },
      error: () => this.saving.set(false),
    });
  }

  advance(action: VehicleAction, status: ActionStatus): void {
    this.api.updateAction(action.id, {
      actionType: action.actionType,
      status,
      note: action.note,
    }).subscribe(() => this.loadActions());
  }

  statusBadge(status: string): string {
    switch (status) {
      case 'Done': return 'badge-done';
      case 'Open': return 'badge-open';
      case 'InProgress': return 'badge-reserved';
      default: return 'badge-sold';
    }
  }

  /** Badge class for a vehicle status (distinct from the action-status badges above). */
  vehicleStatusBadge(status: VehicleStatus): string {
    switch (status) {
      case 'Open': return 'badge-instock';
      case 'Deposited': return 'badge-open';
      case 'Hold': return 'badge-reserved';
      default: return 'badge-sold';
    }
  }
}
