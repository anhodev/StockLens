---
name: angular-enterprise
description: Generate enterprise-grade Angular 20 applications using modern Angular patterns, Standalone Components, Signals, Signal Store, Angular Material, and feature-first architecture.
---

# Angular Enterprise

You are an Angular 20 expert.

Always generate modern Angular.

Never generate legacy Angular.

Every solution must be production-ready.

---

# Angular Version

Always target

- Angular 20
- TypeScript latest stable

Never generate deprecated syntax.

---

# Tech Stack

Always use

- Standalone Components
- Angular Signals
- computed()
- effect()
- linkedSignal()
- resource() when appropriate
- inject()
- Angular Material
- Signal Store
- Reactive Forms
- SCSS

Use RxJS only where it provides clear value.

---

# Project Structure

Organize by feature.

```
src/

app/

core/

shared/

layout/

features/

```

Example

```
features/

inventory/

dashboard/

vehicle/

pricing/

alerts/

analytics/

settings/

```

Never organize by

```
components/

pages/

services/

models/

```

at the application root.

---

# Feature Structure

Each feature contains

```
inventory/

pages/

components/

dialogs/

services/

store/

models/

pipes/

routes.ts

```

Example

```
inventory/

pages/

inventory-page/

components/

inventory-grid/

inventory-filter/

inventory-summary/

vehicle-card/

dialogs/

edit-price-dialog/

services/

inventory-api.service.ts

store/

inventory.store.ts

models/

vehicle.ts

inventory-summary.ts

routes.ts

```

---

# Standalone Components

Every component must be standalone.

Example

```typescript
@Component({
standalone: true,
imports: [],
changeDetection: ChangeDetectionStrategy.OnPush
})
```

Never create NgModules.

---

# Dependency Injection

Always use

```typescript
private readonly api = inject(InventoryApiService);
```

Never use constructor injection unless explicitly required.

---

# Signals

Signals are the default state mechanism.

Example

```typescript
readonly vehicles = signal<Vehicle[]>([]);
```

Derived state

```typescript
readonly availableVehicles = computed(() =>
this.vehicles().filter(x => x.status === 'Available'));
```

Side effects

```typescript
effect(() => {

});
```

Avoid Subjects.

Avoid BehaviorSubject.

---

# Signal Store

Global feature state belongs in Signal Store.

Each feature should have exactly one primary store.

Store responsibilities

- state
- computed values
- methods
- loading
- error handling

Avoid duplicated state.

---

# HTTP

All HTTP belongs in API services.

Never inject HttpClient into components.

Good

```
InventoryApiService
```

Bad

```
InventoryComponent
↓
HttpClient
```

---

# Components

Keep components small.

Preferred hierarchy

```
InventoryPage

↓

InventoryToolbar

↓

InventorySummary

↓

InventoryGrid

↓

VehicleRow
```

Avoid massive page components.

---

# Smart Components

Responsibilities

- routing
- store interaction
- dialogs
- loading data

No rendering logic.

---

# Presentational Components

Responsibilities

- display
- input
- output

No business logic.

---

# Routing

Always lazy load features.

Example

```typescript
export default [
{
path: '',
loadComponent: () =>
import('./pages/inventory-page.component')
}
];
```

---

# Material Design

Use Angular Material 3.

Preferred components

Toolbar

Card

Button

Menu

Table

Paginator

Sort

SnackBar

Dialog

Tabs

Drawer

Expansion Panel

Select

Autocomplete

Chip

Progress Spinner

Progress Bar

Icon

Tooltip

---

# Dashboard Layout

Preferred order

```
Header

↓

Filters

↓

KPI Cards

↓

Charts

↓

Inventory Grid

↓

Alerts

↓

Timeline
```

Avoid clutter.

---

# KPI Cards

Reusable component

Inputs

Title

Value

Trend

Icon

Color

Loading

Support responsive layouts.

---

# Data Grid

Inventory tables should support

Sorting

Filtering

Pagination

Selection

Loading

Empty State

Sticky Headers

Responsive columns

Virtual scrolling when needed.

---

# Charts

Preferred library

Chart.js

Choose chart type based on data.

Examples

Inventory Trend

↓

Line

Vehicle Distribution

↓

Pie

Stock by Brand

↓

Bar

Pricing Trend

↓

Area

---

# Forms

Always use

Reactive Forms

Strong typing

Validators

Error messages

Never use Template Forms.

---

# Validation

Show validation immediately.

Never wait until submit.

---

# Styling

Use

SCSS

CSS Grid

Flexbox

Material spacing

Responsive design

Avoid inline styles.

---

# Responsiveness

Support

Desktop

Tablet

Mobile

Dashboard cards should wrap automatically.

---

# Accessibility

Support

Keyboard

ARIA

Focus management

Contrast

Screen readers

---

# Loading States

Every page must support

Loading

Error

Empty

Success

Avoid blank pages.

---

# Error Handling

Use Snackbar for recoverable errors.

Display meaningful messages.

Never expose backend exceptions.

---

# SignalR

Create a reusable SignalR service.

Responsibilities

Connection

Reconnect

Subscriptions

Connection state

Stores receive updates from SignalR.

Components never interact with HubConnection directly.

---

# Performance

Always consider

OnPush

Signals

trackBy

Lazy Loading

Virtual Scroll

Deferrable Views

Avoid unnecessary change detection.

---

# Control Flow

Always use

@if

@for

@switch

Never use

*ngIf

*ngFor

unless maintaining legacy code.

---

# Templates

Keep templates clean.

Avoid

Large expressions

Business logic

Complex calculations

Move logic into computed().

---

# Naming

Pages

InventoryPageComponent

DashboardPageComponent

Stores

InventoryStore

DashboardStore

Services

InventoryApiService

SignalRService

Models

Vehicle

InventorySummary

InventoryAlert

Components

VehicleCardComponent

InventoryGridComponent

---

# Inventory Dashboard

Dashboard should include

Inventory Count

Average Days On Lot

Vehicles Aging

Inventory Value

Low Stock

Overstock

Top Brands

Recent Activity

Alerts

AI Recommendations

Make dashboard actionable.

---

# AI Components

Reusable

Recommendation Card

Insight Card

Forecast Chart

Risk Indicator

Confidence Badge

Never hardcode AI provider details.

---

# Testing

Generate

Component tests

Store tests

API service tests

Dialog tests

Use Angular Testing Library.

---

# Anti Patterns

Never generate

NgModules

BehaviorSubject stores

Massive Components

Nested subscriptions

Manual DOM manipulation

Business logic in templates

HttpClient in Components

State duplication

Unsubscribe boilerplate

Any deprecated Angular APIs

---

# Before Returning

Verify

✅ Standalone Components

✅ Signals

✅ Signal Store

✅ inject()

✅ Angular Material

✅ OnPush

✅ Lazy Loading

✅ @if/@for

✅ Reactive Forms

✅ Responsive

✅ Accessible

✅ Production Ready

If any item fails,

regenerate before returning.