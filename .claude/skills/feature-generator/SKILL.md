---
name: feature-generator
description: Generate complete full-stack features for the Intelligent Inventory Dashboard, including backend, frontend, SignalR integration, tests, and documentation.
---

# Full Stack Feature Generator

Generate complete production-ready features.

Always follow

- project-standards
- dotnet-clean-api
- angular-enterprise

Never generate partial implementations unless explicitly requested.

---

# Goal

When asked to create a feature,

Generate everything required.

A feature should compile immediately after generation.

No TODO comments.

No placeholders.

No fake implementations.

---

# Supported Feature Types

Examples

Inventory

Vehicle

Dashboard

Pricing

Alerts

Analytics

Authentication

Dealership

Reports

AI

Administration

---

# Backend

Generate

Domain

Application

Infrastructure

Presentation

Tests

---

## Domain

Create

Entity (if needed)

Value Objects

Enums

Business Rules

Domain Events (when appropriate)

Never place business logic in Infrastructure.

---

## Application

Generate

Request

Response

Handler

Validator

Interfaces

Mapping

Business logic belongs here.

---

## Infrastructure

Generate

EF Core configuration

Database queries

SignalR notifications

Persistence

Never expose Infrastructure classes outside.

---

## Presentation

Generate

Minimal API endpoint

OpenAPI metadata

Authorization

Validation

Typed Results

Example

GET

POST

PUT

DELETE

PATCH

depending on feature requirements.

---

# Frontend

Generate

Page

Components

Store

Service

Models

Routing

Material UI

SignalR integration

Tests

---

## Angular Page

Responsibilities

Load data

Connect store

Handle filters

Handle routing

Display dashboard

Avoid business logic.

---

## Store

Generate Signal Store.

State

Loading

Error

Data

Filters

Sorting

Pagination

Selection

Computed values

Methods

Never use BehaviorSubject.

---

## API Service

Generate

HTTP methods

Typed models

Error handling

No business logic.

---

## Components

Split into reusable components.

Example

Inventory

InventoryToolbar

InventoryFilters

InventoryTable

InventorySummary

InventoryChart

InventoryRow

EmptyState

LoadingOverlay

Avoid one giant page component.

---

# SignalR

If feature changes dashboard data

Automatically generate

Hub event

Angular listener

Store update

Reconnect handling

Only transmit minimal payloads.

---

# Database

If feature stores data

Generate

Migration

Entity Configuration

Indexes

Relationships

Constraints

Always optimize indexes.

---

# Validation

Generate

Backend validation

Frontend validation

Use identical validation rules where appropriate.

---

# Authorization

If feature requires authentication

Generate

Authorization policy

Endpoint protection

Frontend route guard

Permission checks

Never rely solely on frontend authorization.

---

# Logging

Generate structured logging.

Important operations

Create

Update

Delete

Failures

Warnings

Never log sensitive information.

---

# Testing

Always generate

Backend

Unit Tests

Integration Tests

Frontend

Component Tests

Store Tests

Service Tests

E2E scenarios (when appropriate)

---

# Documentation

Generate XML documentation for public backend APIs.

Generate comments only when explaining WHY.

Avoid obvious comments.

---

# Feature Folder Example

Backend

Features/

Inventory/

CreateVehicle/

Endpoint.cs

Request.cs

Response.cs

Validator.cs

Handler.cs

Mapping.cs

Tests.cs

Frontend

features/

inventory/

pages/

vehicle-list/

components/

vehicle-table/

vehicle-toolbar/

vehicle-filter/

vehicle-summary/

vehicle-dialog/

store/

inventory.store.ts

services/

inventory-api.service.ts

models/

vehicle.ts

routes.ts

---

# CRUD Rules

When creating CRUD features

Automatically generate

List

Details

Create

Update

Delete

Validation

Search

Filtering

Sorting

Pagination

Loading state

Error state

Success notifications

Confirmation dialogs

Audit logging

---

# Dashboard Rules

Dashboard features should automatically include

KPI Cards

Charts

Tables

Filters

Summary cards

Alerts

Recent activity

Responsive layout

Do not generate a plain table unless explicitly requested.

---

# Inventory Rules

Inventory features should consider

VIN

Stock Number

Make

Model

Trim

Year

Mileage

Color

Price

MSRP

Dealer Cost

Location

Status

Acquisition Date

Days on Lot

Vehicle Age

Inventory Status

Reservation Status

Images

AI Insights

Only include fields relevant to the request.

---

# Pricing Rules

Pricing features should consider

Current Price

Suggested Price

Price History

Market Average

Competitor Price

Markdown %

Profit Margin

AI Recommendation

---

# AI Features

When asked for AI functionality

Generate

Application abstraction

Provider interface

Prompt models

Response models

Telemetry

Caching

Never tightly couple business logic to an LLM provider.

Example

IInventoryInsightService

IPricingRecommendationService

IDemandForecastService

---

# Performance

Always consider

Pagination

Projection

AsNoTracking

Indexes

Virtual scrolling

SignalR payload size

Lazy loading

Avoid loading unnecessary data.

---

# UX

Every feature should support

Loading

Empty State

Error State

Retry

Notifications

Responsive Design

Accessibility

---

# Naming

Generate meaningful names.

Avoid

Manager

Helper

Utils

CommonService

RepositoryBase

Generic names.

---

# Anti Patterns

Never generate

Controllers

Generic Repository

God Services

Huge Components

Business logic in UI

Business logic in Endpoints

Duplicate DTOs

Duplicate Models

BehaviorSubject stores

NgModules

---

# Completion Checklist

Before returning

Verify

✅ Backend compiles

✅ Frontend compiles

✅ Minimal API

✅ Angular Standalone

✅ Signals

✅ Signal Store

✅ Validation

✅ Logging

✅ Tests

✅ SignalR considered

✅ Documentation

✅ Responsive UI

✅ Production Ready

If anything is missing,

generate it automatically.