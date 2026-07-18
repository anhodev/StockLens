// TypeScript mirrors of the API DTOs.

export type VehicleStatus = 'Open' | 'Deposited' | 'Hold' | 'Sold';

export type BodyType = 'Sedan' | 'Suv' | 'Truck' | 'Hatchback' | 'Coupe' | 'Van' | 'Wagon';

export type ActionType =
  | 'PriceReductionPlanned' | 'MoveToAuction' | 'Promote'
  | 'TransferToBranch' | 'Recondition' | 'Other';

export type ActionStatus = 'Open' | 'InProgress' | 'Done' | 'Cancelled';

export type StrategyScope = 'Factory' | 'VehicleType' | 'Vehicle';

export interface Vehicle {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  trim?: string | null;
  color?: string | null;
  mileage: number;
  bodyType: BodyType;
  depositAmount?: number | null;
  salespersonName?: string | null;
  listPrice: number;
  /** Effective discount % from the applied business strategy, if any. */
  discountPercent?: number | null;
  /** List price after the effective strategy discount; equals listPrice when none applies. */
  netPrice: number;
  cost: number;
  status: VehicleStatus;
  acquiredDate: string;
  soldDate?: string | null;
  daysInInventory: number;
  isAgingStock: boolean;
  openActionCount: number;
  imageUrl?: string | null;
}

export interface VehicleAction {
  id: string;
  vehicleId: string;
  actionType: ActionType;
  status: ActionStatus;
  note?: string | null;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface BusinessStrategy {
  id: string;
  scope: StrategyScope;
  scopeKey: string;
  name: string;
  description?: string | null;
  targetDaysToSell?: number | null;
  discountPercent?: number | null;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
}

export interface EffectiveStrategy {
  vehicleId: string;
  strategy: BusinessStrategy;
  matchedScope: StrategyScope;
}

export interface TopSale {
  vehicleId: string;
  make: string;
  model: string;
  year: number;
  salePrice: number;
  soldDate: string;
  daysToSell: number;
  soldBy: string;
}

export interface MakeBreakdown {
  make: string;
  count: number;
  stockValue: number;
}

/** One month of sales performance, oldest first. */
export interface SalesTrendPoint {
  month: string;
  label: string;
  units: number;
  revenue: number;
}

export interface Salesperson {
  id: string;
  fullName: string;
  email?: string | null;
  team?: string | null;
  hireDate: string;
  isActive: boolean;
  salesCount: number;
  revenue: number;
}

export interface DashboardSummary {
  totalInStock: number;
  agingStockCount: number;
  totalStockValue: number;
  averageDaysInInventory: number;
  averageDaysToSell?: number | null;
  soldLast30Days: number;
  revenueLast30Days: number;
  topSales: TopSale[];
  stockByMake: MakeBreakdown[];
  salesTrend: SalesTrendPoint[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface VehicleFilter {
  make?: string;
  model?: string;
  status?: VehicleStatus;
  agingOnly?: boolean;
  search?: string;
  sortBy?: 'age' | 'price' | 'make';
  desc?: boolean;
  page?: number;
  pageSize?: number;
}

/** An audited move between vehicle statuses, newest first. */
export interface VehicleStatusChange {
  id: string;
  vehicleId: string;
  fromStatus: VehicleStatus;
  toStatus: VehicleStatus;
  reason?: string | null;
  depositAmount?: number | null;
  salePrice?: number | null;
  salespersonName?: string | null;
  effectiveDate: string;
  changedBy: string;
  createdAt: string;
}

/** Payload for a status move; which fields are required depends on `toStatus`. */
export interface ChangeStatusRequest {
  toStatus: VehicleStatus;
  reason?: string | null;
  depositAmount?: number | null;
  salePrice?: number | null;
  soldDate?: string | null;
  salespersonId?: string | null;
  changedBy?: string | null;
}

export const VEHICLE_STATUSES: VehicleStatus[] = ['Open', 'Deposited', 'Hold', 'Sold'];

export const ACTION_TYPES: ActionType[] = [
  'PriceReductionPlanned', 'MoveToAuction', 'Promote', 'TransferToBranch', 'Recondition', 'Other',
];
export const ACTION_STATUSES: ActionStatus[] = ['Open', 'InProgress', 'Done', 'Cancelled'];
export const STRATEGY_SCOPES: StrategyScope[] = ['Factory', 'VehicleType', 'Vehicle'];
