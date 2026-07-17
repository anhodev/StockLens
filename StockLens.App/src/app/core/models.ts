// TypeScript mirrors of the API DTOs.

export type VehicleStatus = 'InStock' | 'Reserved' | 'Sold';

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
  listPrice: number;
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
}

export interface MakeBreakdown {
  make: string;
  count: number;
  stockValue: number;
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

export const ACTION_TYPES: ActionType[] = [
  'PriceReductionPlanned', 'MoveToAuction', 'Promote', 'TransferToBranch', 'Recondition', 'Other',
];
export const ACTION_STATUSES: ActionStatus[] = ['Open', 'InProgress', 'Done', 'Cancelled'];
export const STRATEGY_SCOPES: StrategyScope[] = ['Factory', 'VehicleType', 'Vehicle'];
