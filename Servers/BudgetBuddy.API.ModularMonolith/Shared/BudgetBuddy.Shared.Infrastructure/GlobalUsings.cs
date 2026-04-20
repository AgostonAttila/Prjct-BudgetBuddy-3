global using BudgetBuddy.Shared.Kernel.Entities; // Only User, AuditLog, SecurityEvent, RefreshToken remain here
global using BudgetBuddy.Shared.Kernel.Enums;
global using BudgetBuddy.Shared.Kernel.Contracts;
global using BudgetBuddy.Shared.Kernel.Logging;
global using BudgetBuddy.Shared.Kernel.Constants;
global using BudgetBuddy.Shared.Kernel.Exceptions;
global using BudgetBuddy.Shared.Infrastructure;
global using BudgetBuddy.Shared.Infrastructure.Persistence;
global using BudgetBuddy.Shared.Contracts;
global using BudgetBuddy.Shared.Contracts.Accounts;
global using BudgetBuddy.Shared.Contracts.Investments;
global using BudgetBuddy.Shared.Contracts.Financial;
global using NodaTime;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
