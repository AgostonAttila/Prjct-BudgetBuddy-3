// ============================================================================
// ASP.NET Core
// ============================================================================
global using Microsoft.AspNetCore.Http.HttpResults;

// ============================================================================
// Entity Framework Core
// ============================================================================
global using Microsoft.EntityFrameworkCore;

// ============================================================================
// CQRS
// ============================================================================
global using MediatR;

// ============================================================================
// Minimal API & Mapping
// ============================================================================
global using Carter;
global using MapsterMapper;

// ============================================================================
// Time Handling
// ============================================================================
global using NodaTime;

// ============================================================================
// Project - Common Domain
// ============================================================================
global using BudgetBuddy.API.VSA.Common.Domain.Entities;
global using BudgetBuddy.API.VSA.Common.Domain.Enums;
global using BudgetBuddy.API.VSA.Common.Domain.Exceptions;

// ============================================================================
// Project - Common Infrastructure
// ============================================================================
global using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
global using BudgetBuddy.API.VSA.Common.Infrastructure.Services;

// ============================================================================
// Project - Common Shared
// ============================================================================
global using BudgetBuddy.API.VSA.Common.Shared.Contracts;
global using BudgetBuddy.API.VSA.Common.Shared;

// ============================================================================
// Project - Common Extensions
// ============================================================================
global using BudgetBuddy.API.VSA.Common.Extensions;

global using BudgetBuddy.API.VSA.Common.Domain.Constants;
global using Tags = BudgetBuddy.API.VSA.Common.Domain.Constants.CacheTags;