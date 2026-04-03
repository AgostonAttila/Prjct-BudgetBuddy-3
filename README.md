# 🏦 Personal Finance App - Multi-Client Architecture

A simple personal finance application demonstrating modern web and mobile development with **multiple client implementations** connecting to ASP.NET Core WebAPI backends. This project showcases the latest features in .NET, Angular  and other popular frameworks.

> ⚠️ **WARNING**: These projects are for **testing and learning purposes only**. They are **NOT production-ready** and lack proper security implementations. Do not use with real financial data.

## 🎯 Project Goals

- Explore multiple client frameworks connecting to the same backend
- Keep implementations simple and focused on framework-specific features
- Create reusable authentication patterns across different platforms

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Backend Servers                       │
├─────────────────────────────────────────────────────────┤
│  • ASP.NET Core WebAPI (.NET 10) - Identity Endpoints   │
└─────────────────────────────────────────────────────────┘
                            ▲
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────▼───────┐  ┌────────▼────────┐  ┌──────▼──────┐
│   Web Clients │  │  Desktop Apps   │  │ Mobile Apps │
├───────────────┤  ├─────────────────┤  ├─────────────┤
│ • Blazor      │  │ • MAUI Hybrid   │  │ • MAUI      │
│               │  │ • Electron/Tauri│  │ • Ionic     │
│   Server      │  │                 │  │ • React     │
│ • Blazor      │  └─────────────────┘  │   Native    │
│   WebAssembly │                       │             │
│ • Angular     │                       └─────────────┘
│ • React       │
│   (Vite)      │
│ • Next.js     │
│   (CSR)       │
│ • Vue.js*     │
│               │
└───────────────┘

* Planned for future implementation
```

## 📦 Project Structure

### Backend Projects

#### 1. **PersonalFinance.API.Identity** (.NET 10)
- **Features**: 
  - Uses new `AddIdentityApiEndpoints()` method (introduced in .NET 8)
  - Bearer token authentication
  - Simplified identity setup
  - Minimal API endpoints for user registration and login
- **Tech Stack**: ASP.NET Core 10.0, Entity Framework Core, SQLite/SQL Server/Postgres
- **Endpoints**:
  - `POST /register` - User registration
  - `POST /login` - User login (returns access token)
  - `GET /api/transactions` - Get user transactions
  - `POST /api/transactions` - Create transaction
  - `GET /api/accounts` - Get user accounts
  - `POST /api/accounts` - Create account

### Web Client Projects

#### 3. **PersonalFinance.Blazor.Server** (.NET 10)
- **Features**:
  - Server-side rendering
  - Uses new .NET 8 Identity Razor pages
  - Built-in authentication UI
  - Real-time updates via SignalR
- **Tech Stack**: Blazor Server, ASP.NET Core Identity

#### 4. **PersonalFinance.Blazor.Wasm** (.NET 10)
- **Features**:
  - Client-side Blazor (WebAssembly)
  - Standalone application
  - Token-based authentication
  - Local storage for auth state
- **Tech Stack**: Blazor WebAssembly, .NET 8

#### 5. **PersonalFinance.Angular** (Angular 21)
- **Features**:
  - Standalone components (new in Angular 15+)
  - Signals API (new in Angular 17)
  - Built-in control flow (`@if`, `@for`)
  - HTTP interceptors for auth
- **Tech Stack**: Angular 17, TypeScript, RxJS
- **Highlights**:
  - Standalone bootstrap (no `app.module.ts`)
  - Modern reactive patterns with Signals

#### 6. **PersonalFinance.React** (React 19)
- **Features**:
  - Vite for fast development
  - React 18 features (concurrent rendering)
  - Custom hooks for API calls
  - Context API for auth state
- **Tech Stack**: React 18, TypeScript, Vite, Axios

#### 7. **PersonalFinance.NextJs** (Next.js 16)
- **Features**:
  - Client-side rendering only (no server components)
  - App Router (new in Next.js 13+)
  - API route handlers for client-side fetching
  - Static export capability
- **Tech Stack**: Next.js 14, React, TypeScript
- **Note**: Not using server-side features to maintain similarity with other SPAs

#### 8. **PersonalFinance.Vue** (Vue 3)* - **Planned**
- **Features**:
  - Composition API
  - `<script setup>` syntax
  - Pinia for state management
- **Tech Stack**: Vue 3, TypeScript, Vite

#### 9. **PersonalFinance.Ionic** (Ionic + Angular/React)* - **Planned**
- Cross-platform mobile/web application
- Capacitor for native features

### Desktop Client Projects

#### 10. **PersonalFinance.MAUI** (.NET 10)
- **Features**:
  - Native mobile app (iOS, Android)
  - Cross-platform UI
  - Separate from Blazor (no code sharing)
  - Platform-specific features
- **Tech Stack**: .NET MAUI 8.0, MVVM pattern

#### 11. **PersonalFinance.MAUI.Hybrid** (.NET 10)
- **Features**:
  - Blazor components in MAUI
  - Shared UI logic with Blazor projects
  - Native device features
- **Tech Stack**: .NET MAUI 8.0, Blazor Hybrid

#### 12. **PersonalFinance.ReactNative** (Expo)
- **Features**:
  - Expo for simplified development
  - Cross-platform (iOS, Android)
  - React Native navigation
  - Async storage for tokens
- **Tech Stack**: React Native, Expo, TypeScript

#### 13. **PersonalFinance.Electron*** - **Planned**
- Desktop application (Windows, macOS, Linux)
- Chromium-based with Node.js backend access

### Future Implementations

- **Next.js with Server Components** - Exploring server-side rendering and data fetching
- Additional frameworks as they evolve

## 🔑 Authentication Flow

### Option 1: Identity API Endpoints (.NET 8)
```
Client → POST /register → API (Identity Endpoints)
Client → POST /login → API returns { accessToken, refreshToken }
Client → GET /api/transactions (Header: Authorization: Bearer {token})
```


## 📊 Core Features

All clients implement these basic features:

1. **User Authentication**
   - Registration
   - Login/Logout
   - Token management

2. **Account Management**
   - View all accounts
   - Create new account
   - View account balance

3. **Transaction Tracking**
   - List transactions
   - Add new transaction (income/expense)
   - Filter by date/category
   - Simple statistics (total income, expenses)

4. **Basic Dashboard**
   - Overview of accounts
   - Recent transactions
   - Simple charts (optional)

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 18+ (for frontend projects)
- Visual Studio 2022 / VS Code / Rider
- SQL Server / SQLite / PostgreSQL

### Running the Backend

```bash
cd PersonalFinance.API.Identity
dotnet restore
dotnet ef database update
dotnet run
```

### Running Web Clients

**Angular:**
```bash
cd PersonalFinance.Angular
npm install
ng serve
```

**React (Vite):**
```bash
cd PersonalFinance.React
npm install
npm run dev
```

**Next.js:**
```bash
cd PersonalFinance.NextJs
npm install
npm run dev
```

**Blazor WebAssembly:**
```bash
cd PersonalFinance.Blazor.Wasm
dotnet run
```

### Running Mobile/Desktop

**MAUI:**
```bash
cd PersonalFinance.MAUI
dotnet build
dotnet run -f net8.0-android
# or net8.0-ios, net8.0-windows, etc.
```

**React Native:**
```bash
cd PersonalFinance.ReactNative
npm install
npx expo start
```

## 🛠️ Technology Highlights

### .NET 10 New Features Used
- ✅ `MapIdentityApi<TUser>()` - Simplified identity endpoints
- ✅ Improved minimal API performance
- ✅ Native AOT support considerations
- ✅ Enhanced authentication middleware

### Angular 17 New Features
- ✅ Standalone components as default
- ✅ New control flow syntax (`@if`, `@for`, `@switch`)
- ✅ Signals for reactive state management
- ✅ Improved SSR and hydration

### Next.js 14 Features
- ✅ App Router (stable)
- ✅ Server Actions (used minimally, client-side focused)
- ✅ Improved TypeScript support
- ✅ Turbopack in development

### React 18 Features
- ✅ Concurrent rendering
- ✅ Automatic batching
- ✅ Transitions API
- ✅ Modern hooks patterns

## ⚠️ Security Disclaimers

**This project is NOT secure and should NOT be used in production:**

- ❌ No HTTPS enforcement on local dev
- ❌ Simplified password requirements
- ❌ No rate limiting
- ❌ No CSRF protection on some clients
- ❌ No input sanitization
- ❌ Tokens stored in localStorage (vulnerable to XSS)
- ❌ No refresh token rotation
- ❌ Simplified CORS configuration
- ❌ No audit logging
- ❌ No data encryption at rest

**For production, you would need:**
- ✅ HTTPS everywhere
- ✅ Secure token storage (httpOnly cookies)
- ✅ CSRF tokens
- ✅ Input validation and sanitization
- ✅ Rate limiting and throttling
- ✅ Proper error handling (no sensitive data leaks)
- ✅ Security headers (CSP, HSTS, etc.)
- ✅ Data encryption
- ✅ Comprehensive logging and monitoring
- ✅ Regular security audits

## 📚 Learning Resources

- [.NET 10 Identity API Endpoints](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization)
- [Angular 21 Documentation](https://angular.io/docs)
- [Next.js 16 Documentation](https://nextjs.org/docs)
- [React 19 Documentation](https://react.dev/)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

## 🤝 Contributing

This is a personal learning project, but suggestions and improvements are welcome!

## 📝 License

MIT License - Feel free to use this for learning purposes.

## 🗺️ Roadmap

- [ ] Backend with Identity API Endpoints
- [ ] Backend with Custom JWT
- [ ] Blazor Server
- [ ] Blazor WebAssembly
- [ ] MAUI Native
- [ ] MAUI Hybrid
- [ ] Angular 17
- [ ] React 18 (Vite)
- [ ] Next.js 14 (CSR)
- [ ] React Native (Expo)
- [ ] Vue 3
- [ ] Ionic
- [ ] Electron
- [ ] Next.js with Server Components

---

**Remember**: This is a skeleton application for learning modern web/mobile development patterns. Always implement proper security measures for real-world applications! 🔒
