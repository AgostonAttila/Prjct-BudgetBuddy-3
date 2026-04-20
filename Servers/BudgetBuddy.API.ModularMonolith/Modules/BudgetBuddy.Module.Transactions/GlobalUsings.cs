global using MediatR;
global using Carter;
global using MapsterMapper;
global using NodaTime;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Builder;
global using BudgetBuddy.Shared.Kernel.Entities; // User, AuditLog, SecurityEvent, RefreshToken
global using BudgetBuddy.Shared.Kernel.Enums;
global using BudgetBuddy.Shared.Kernel.Contracts;
global using BudgetBuddy.Shared.Kernel.Constants;
global using BudgetBuddy.Shared.Kernel.Exceptions;
global using BudgetBuddy.Shared.Infrastructure.Persistence; // AppDbContext (auth/identity only)
global using BudgetBuddy.Shared.Infrastructure;
global using BudgetBuddy.Module.Transactions.Domain;
global using BudgetBuddy.Module.Transactions.Persistence;
global using BudgetBuddy.Shared.Contracts.Transactions;
global using BudgetBuddy.Shared.Infrastructure.Extensions;
global using BudgetBuddy.Shared.Contracts.Financial;
global using BudgetBuddy.Shared.Contracts;
global using BudgetBuddy.Shared.Contracts.Accounts;
global using BudgetBuddy.Shared.Contracts.ReferenceData;
global using BudgetBuddy.Shared.Contracts.Investments;
global using Tags = BudgetBuddy.Shared.Kernel.Constants.CacheTags;

