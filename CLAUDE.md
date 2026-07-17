# CLAUDE.md

# StockLens

StockLens is an AI-powered Intelligent Inventory Dashboard for automotive dealerships.

The application provides real-time inventory visibility, inventory aging analysis, pricing insights, dealership KPIs, live dashboard updates, and AI-powered recommendations.

---

# Primary Goal

Generate production-ready code that is:

- Maintainable
- Simple
- Testable
- Secure
- Performant
- Enterprise-ready

Favor readability over clever implementations.

---

# Tech Stack

## Backend

- .NET 10
- ASP.NET Core Minimal API
- Clean Architecture
- Vertical Slice Architecture
- Entity Framework Core
- SQL Server
- SignalR
- FluentValidation
- OpenTelemetry
- Serilog

## Frontend

- Angular 20
- Standalone Components
- Angular Signals
- Signal Store
- Angular Material
- SCSS

## Cloud

- Microsoft Azure
- Azure SQL
- Azure SignalR Service
- Azure App Insights
- Azure Key Vault

---

# Architecture

This project follows

- Clean Architecture
- Vertical Slice Architecture
- Feature-first organization

Do not introduce new architectural patterns unless explicitly requested.

Always preserve dependency direction.

Domain must remain independent.

---

# Coding Principles

Always prefer

- SOLID
- KISS
- DRY
- Composition over inheritance
- Dependency Injection
- Immutable models where appropriate
- Async programming
- Strong typing

Avoid unnecessary abstraction.

Avoid over-engineering.

---

# Backend Rules

Always use

- Minimal APIs
- Typed Results
- FluentValidation
- EF Core
- CancellationToken
- Structured Logging

Never generate

- MVC Controllers
- Generic Repository
- Unit of Work wrapper
- Static helper classes
- Service Locator

---

# Frontend Rules

Always use

- Standalone Components
- Angular Signals
- Signal Store
- OnPush Change Detection
- Angular Material

Avoid

- NgModules
- BehaviorSubject state management
- Business logic inside components

---

# SignalR

SignalR is the preferred mechanism for real-time updates.

Do not implement polling unless explicitly requested.

Send minimal update payloads.

---

# Database

Use SQL Server.

Optimize queries.

Prefer projection over loading entire entities.

Always consider indexing.

Avoid N+1 queries.

---

# Testing

Every feature should include appropriate tests.

Backend

- Unit Tests
- Integration Tests

Frontend

- Component Tests
- Store Tests

Generate Playwright tests when appropriate.

---

# Documentation

Public APIs should include XML documentation.

Explain WHY instead of WHAT when comments are needed.

Avoid unnecessary comments.

---

# Performance

Always consider

- Database round trips
- Memory allocations
- Serialization size
- SignalR payload size
- Frontend rendering performance

Avoid premature optimization.

---

# Security

Never trust client input.

Always validate.

Never expose secrets.

Never log sensitive information.

Never hardcode credentials.

---

# UI Philosophy

The dashboard should be clean, modern and data-driven.

Prioritize

- KPIs
- Trends
- Alerts
- Actionable insights

Avoid visual clutter.

---

# AI Features

Design AI functionality behind abstractions.

Never couple business logic directly to an AI provider.

Potential AI capabilities include

- Inventory insights
- Pricing recommendations
- Demand forecasting
- Natural language summaries
- Vehicle recommendations

---

# Project Structure

backend/
frontend/
database/
docs/

Keep code organized by feature.

Avoid organizing by technical layer.

---

# Documentation

Consult the following documents before making architectural decisions.

docs/architecture/

docs/adr/

If an Architecture Decision Record (ADR) exists, follow it unless instructed otherwise.

---

# Claude Skills

When applicable, automatically apply the following skills.

- project-standards
- dotnet-clean-api
- angular-enterprise
- feature-generator
- inventory-domain-expert
- code-reviewer

---

# Expectations

Unless explicitly requested otherwise,

always generate

- production-ready code
- complete implementations
- strongly typed models
- validation
- logging
- tests
- documentation
- responsive UI
- clean architecture

Never generate placeholder code or TODO implementations.

Assume this project is intended for long-term enterprise maintenance.