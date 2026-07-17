import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { BusinessStrategy, STRATEGY_SCOPES, StrategyScope } from '../core/models';

type StrategyForm = {
  id: string | null;
  scope: StrategyScope;
  scopeKey: string;
  name: string;
  description: string;
  targetDaysToSell: number | null;
  discountPercent: number | null;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
};

@Component({
  selector: 'app-strategies',
  imports: [NgClass, FormsModule],
  templateUrl: './strategies.html',
  styleUrl: './strategies.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategiesComponent implements OnInit {
  private api = inject(ApiService);

  readonly scopes = STRATEGY_SCOPES;
  readonly strategies = signal<BusinessStrategy[]>([]);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  form: StrategyForm = this.blank();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getStrategies().subscribe((s) => this.strategies.set(s));
  }

  private blank(): StrategyForm {
    return {
      id: null,
      scope: 'Factory',
      scopeKey: '',
      name: '',
      description: '',
      targetDaysToSell: 60,
      discountPercent: 3,
      isActive: true,
      effectiveFrom: new Date().toISOString().slice(0, 10),
      effectiveTo: null,
    };
  }

  startCreate(): void {
    this.form = this.blank();
    this.error.set(null);
    this.editing.set(true);
  }

  startEdit(s: BusinessStrategy): void {
    this.form = {
      id: s.id,
      scope: s.scope,
      scopeKey: s.scopeKey,
      name: s.name,
      description: s.description ?? '',
      targetDaysToSell: s.targetDaysToSell ?? null,
      discountPercent: s.discountPercent ?? null,
      isActive: s.isActive,
      effectiveFrom: s.effectiveFrom,
      effectiveTo: s.effectiveTo ?? null,
    };
    this.error.set(null);
    this.editing.set(true);
  }

  cancel(): void {
    this.editing.set(false);
  }

  scopeHint(scope: StrategyScope): string {
    switch (scope) {
      case 'Factory': return 'Make, e.g. "Toyota"';
      case 'VehicleType': return 'Make|Model, e.g. "Ford|F-150"';
      case 'Vehicle': return 'A specific VehicleId';
    }
  }

  save(): void {
    this.error.set(null);
    this.saving.set(true);
    const body: Partial<BusinessStrategy> = {
      scope: this.form.scope,
      scopeKey: this.form.scopeKey.trim(),
      name: this.form.name.trim(),
      description: this.form.description.trim() || null,
      targetDaysToSell: this.form.targetDaysToSell,
      discountPercent: this.form.discountPercent,
      isActive: this.form.isActive,
      effectiveFrom: this.form.effectiveFrom,
      effectiveTo: this.form.effectiveTo || null,
    };

    const req = this.form.id
      ? this.api.updateStrategy(this.form.id, body)
      : this.api.createStrategy(body);

    req.subscribe({
      next: () => { this.saving.set(false); this.editing.set(false); this.load(); },
      error: (e) => { this.saving.set(false); this.error.set(this.describe(e)); },
    });
  }

  remove(s: BusinessStrategy): void {
    this.api.deleteStrategy(s.id).subscribe(() => this.load());
  }

  private describe(e: unknown): string {
    const err = e as { error?: { errors?: Record<string, string[]> } };
    const errors = err?.error?.errors;
    if (errors) return Object.values(errors).flat().join(' ');
    return 'Could not save strategy. Check the fields and try again.';
  }

  grouped(scope: StrategyScope): BusinessStrategy[] {
    return this.strategies().filter((s) => s.scope === scope);
  }
}
