import { CurrencyPipe, DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { RealtimeService } from '../core/realtime.service';
import {
  ACTION_STATUSES, ACTION_TYPES, ActionStatus, ActionType,
  EffectiveStrategy, Vehicle, VehicleAction,
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

  readonly actions = signal<VehicleAction[]>([]);
  readonly effective = signal<EffectiveStrategy | null>(null);
  readonly saving = signal(false);

  // New-action form state.
  newType: ActionType = 'PriceReductionPlanned';
  newStatus: ActionStatus = 'Open';
  newNote = '';
  newBy = 'manager';

  ngOnInit(): void {
    // Refresh actions live when the API broadcasts a change for this vehicle.
    this.unsub = this.realtime.onAction((a) => {
      if (a.vehicleId === this.vehicle.id) this.loadActions();
    });
  }

  ngOnChanges(): void {
    this.loadActions();
    this.loadStrategy();
  }

  ngOnDestroy(): void {
    this.unsub?.();
  }

  private loadActions(): void {
    this.api.getActions(this.vehicle.id).subscribe((a) => this.actions.set(a));
  }

  private loadStrategy(): void {
    this.effective.set(null);
    this.api.getEffectiveStrategy(this.vehicle.id).subscribe({
      next: (e) => this.effective.set(e),
      error: () => this.effective.set(null), // 204 = no applicable strategy
    });
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
}
