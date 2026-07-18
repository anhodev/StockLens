import { Injectable, signal } from '@angular/core';

export type ToastType = 'info' | 'success' | 'warning';

export interface Toast {
  id: number;
  message: string;
  detail: string | null;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts = signal<Toast[]>([]);

  show(message: string, detail: string | null = null, type: ToastType = 'info', durationMs = 4000): void {
    const id = ++this.nextId;
    this.toasts.update((list) => [...list, { id, message, detail, type }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
