import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import {
  BusinessStrategy, ChangeStatusRequest, DashboardSummary, EffectiveStrategy, PagedResult,
  Salesperson, StrategyScopeOptions, Vehicle, VehicleAction, VehicleFilter, VehicleStatusChange,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = API_BASE;

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.base}/api/dashboard/summary`);
  }

  getSalespeople(activeOnly = true): Observable<Salesperson[]> {
    return this.http.get<Salesperson[]>(`${this.base}/api/salespeople`, {
      params: { activeOnly },
    });
  }

  getVehicles(filter: VehicleFilter): Observable<PagedResult<Vehicle>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<PagedResult<Vehicle>>(`${this.base}/api/vehicles`, { params });
  }

  getAgingStock(): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>(`${this.base}/api/vehicles/aging`);
  }

  getVehicle(id: string): Observable<Vehicle> {
    return this.http.get<Vehicle>(`${this.base}/api/vehicles/${id}`);
  }

  createVehicle(body: Partial<Vehicle>): Observable<Vehicle> {
    return this.http.post<Vehicle>(`${this.base}/api/vehicles`, body);
  }

  updateVehicle(id: string, body: Partial<Vehicle>): Observable<Vehicle> {
    return this.http.put<Vehicle>(`${this.base}/api/vehicles/${id}`, body);
  }

  changeStatus(vehicleId: string, body: ChangeStatusRequest): Observable<Vehicle> {
    return this.http.post<Vehicle>(`${this.base}/api/vehicles/${vehicleId}/status`, body);
  }

  getStatusHistory(vehicleId: string): Observable<VehicleStatusChange[]> {
    return this.http.get<VehicleStatusChange[]>(`${this.base}/api/vehicles/${vehicleId}/status-history`);
  }

  getActions(vehicleId: string): Observable<VehicleAction[]> {
    return this.http.get<VehicleAction[]>(`${this.base}/api/vehicles/${vehicleId}/actions`);
  }

  createAction(vehicleId: string, body: Partial<VehicleAction>): Observable<VehicleAction> {
    return this.http.post<VehicleAction>(`${this.base}/api/vehicles/${vehicleId}/actions`, body);
  }

  updateAction(id: string, body: Partial<VehicleAction>): Observable<VehicleAction> {
    return this.http.put<VehicleAction>(`${this.base}/api/actions/${id}`, body);
  }

  getEffectiveStrategy(vehicleId: string): Observable<EffectiveStrategy> {
    return this.http.get<EffectiveStrategy>(`${this.base}/api/vehicles/${vehicleId}/effective-strategy`);
  }

  getStrategies(): Observable<BusinessStrategy[]> {
    return this.http.get<BusinessStrategy[]>(`${this.base}/api/strategies`);
  }

  getStrategyScopeOptions(): Observable<StrategyScopeOptions> {
    return this.http.get<StrategyScopeOptions>(`${this.base}/api/strategies/scope-options`);
  }

  createStrategy(body: Partial<BusinessStrategy>): Observable<BusinessStrategy> {
    return this.http.post<BusinessStrategy>(`${this.base}/api/strategies`, body);
  }

  updateStrategy(id: string, body: Partial<BusinessStrategy>): Observable<BusinessStrategy> {
    return this.http.put<BusinessStrategy>(`${this.base}/api/strategies/${id}`, body);
  }

  deleteStrategy(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/strategies/${id}`);
  }
}
