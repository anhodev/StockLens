import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_BASE } from './config';
import { DashboardSummary, Vehicle, VehicleAction } from './models';

/**
 * Wraps the SignalR connection to /hubs/inventory and surfaces server-pushed
 * events as Angular signals plus lightweight subscription callbacks.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private connection?: HubConnection;

  readonly connected = signal(false);
  readonly lastVehicle = signal<Vehicle | null>(null);
  readonly lastAction = signal<VehicleAction | null>(null);
  readonly lastDashboard = signal<DashboardSummary | null>(null);

  private vehicleHandlers = new Set<(v: Vehicle) => void>();
  private actionHandlers = new Set<(a: VehicleAction) => void>();
  private dashboardHandlers = new Set<(d: DashboardSummary) => void>();

  start(): void {
    if (this.connection) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/inventory`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('VehicleChanged', (v: Vehicle) => {
      this.lastVehicle.set(v);
      this.vehicleHandlers.forEach((h) => h(v));
    });
    this.connection.on('ActionChanged', (a: VehicleAction) => {
      this.lastAction.set(a);
      this.actionHandlers.forEach((h) => h(a));
    });
    this.connection.on('DashboardChanged', (d: DashboardSummary) => {
      this.lastDashboard.set(d);
      this.dashboardHandlers.forEach((h) => h(d));
    });

    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onclose(() => this.connected.set(false));

    this.connection
      .start()
      .then(() => this.connected.set(this.connection?.state === HubConnectionState.Connected))
      .catch((err) => console.error('SignalR connection failed', err));
  }

  onVehicle(handler: (v: Vehicle) => void): () => void {
    this.vehicleHandlers.add(handler);
    return () => this.vehicleHandlers.delete(handler);
  }

  onAction(handler: (a: VehicleAction) => void): () => void {
    this.actionHandlers.add(handler);
    return () => this.actionHandlers.delete(handler);
  }

  onDashboard(handler: (d: DashboardSummary) => void): () => void {
    this.dashboardHandlers.add(handler);
    return () => this.dashboardHandlers.delete(handler);
  }
}
