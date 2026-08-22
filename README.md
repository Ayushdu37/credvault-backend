# 💳 CredVault — Microservices Backend for Credit Card & Bill Management

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture%20%2F%20Microservices-blue)](#architecture)
[![API Gateway](https://img.shields.io/badge/Gateway-Ocelot-orange)](#api-gateway)
[![Database](https://img.shields.io/badge/Database-SQL%20Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Testing](https://img.shields.io/badge/Tests-xUnit%20%2B%20Moq-green)](#testing)

**CredVault** is an enterprise-grade, CRED-inspired credit card management platform backend built with **.NET 10**, **Clean Architecture**, and **Microservices**. It empowers users to securely store and manage credit cards, generate monthly billing statements, and execute idempotent bill payments with automated balance reconciliation.

---

## 🏗️ Architecture Overview

The solution is decoupled into independent microservices fronted by an **Ocelot API Gateway**, with each service strictly adhering to **Clean Architecture** (Domain, Application, Infrastructure, API layers) and the **Database-per-Service** pattern.

```
                                  ┌────────────────────────┐
                                  │      Client App /      │
                                  │  Swagger UI / Postman  │
                                  └───────────┬────────────┘
                                              │
                                              ▼
                                 ┌──────────────────────────┐
                                 │    Ocelot API Gateway    │
                                 │       (Port 5050)        │
                                 │   [JWT Bearer Validation]│
                                 └──────┬────────────┬──────┘
                                        │            │
             ┌──────────────────────────┘            └─────────────────────────┐
             │                                                                 │
             ▼ (Port 5001)                                                     ▼ (Port 5002)
┌───────────────────────────┐                                     ┌───────────────────────────┐
│     Identity Service      │                                     │ Credit Management Service │
├───────────────────────────┤                                     ├───────────────────────────┤
│ • JWT Auth & Registration │                                     │ • Card Management (Luhn)  │
│ • BCrypt Password Hashing │                                     │ • Billing Statements      │
│ • Role-based Permissions  │                                     │ • Idempotent Payments     │
│ • User Profile Management │                                     │ • Balance Reconciliation  │
├───────────────────────────┤                                     ├───────────────────────────┤
│ IdentityDb (SQL Server)   │                                     │ CreditManagementDb (SQL)  │
└───────────────────────────┘                                     └───────────────────────────┘
```

---

## ✨ Key Features & Capabilities

### 🔐 1. Identity & Access Management
* **JWT Bearer Authentication:** Stateless token-based security shared across the gateway and services.
* **BCrypt Hashing:** Secure, salted password storage.
* **Deterministic Role Seeding:** Initial roles (`User`, `Admin`) seeded using fixed GUIDs for reproducible database migrations.

### 💳 2. Credit Card Management
* **Luhn Algorithm Validation (Mod 10):** Mathematical checksum validation on incoming card numbers.
* **BIN Issuer Detection:** Automatic identification of card networks (**Visa**, **Mastercard**, **RuPay**, **Amex**) based on IIN/BIN prefixes.
* **PCI-DSS Compliant Masking:** Displays numbers as `**** **** **** 1234`.
* **SHA-256 Duplicate Guard:** Cryptographic hashing prevents registering the same card twice without storing raw card data.

### 🧾 3. Bill & Statement Management
* **Statement Lifecycle:** Tracks status (`1 = Unpaid`, `2 = Paid`, `3 = Overdue`).
* **Automated Calculations:** Automatically computes the standard 5% minimum due and calculates default 18-day due dates.
* **Running Card Balance:** Automatically syncs and updates the card's `OutstandingAmount`.

### 💰 4. Idempotent Payment Processing
* **Idempotency Guard:** Rejects duplicate payment submissions with **`409 Conflict`** using client/gateway `TransactionReference` tokens.
* **Overpayment Protection:** Rejects amounts exceeding the remaining unpaid balance (`CustomValidationException` $\rightarrow$ `400 Bad Request`).
* **Multi-Entity Balance Reconciliation:** Simultaneously creates the payment receipt, updates bill settlement status, and decrements card outstanding balances.

### 🛡️ 5. Cross-Cutting Concerns
* **Global Exception Handling Middleware:** Centralized error translation eliminating repetitive `try-catch` blocks, outputting standardized **RFC 7807 `ProblemDetails`** JSON.
* **Serilog Structured Logging:** Dual-sink logging to Console and daily rolling files (`Logs/`).

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Framework** | **ASP.NET Core (.NET 10.0)** |
| **API Gateway** | **Ocelot 25.0.0** |
| **Architecture** | **Clean Architecture** (Domain, Application, Infrastructure, API) |
| **Database & ORM** | **Microsoft SQL Server**, **Entity Framework Core 10** |
| **Security** | **JWT Bearer**, **BCrypt.Net-Next** |
| **Logging** | **Serilog.AspNetCore**, **Serilog.Sinks.File** |
| **API Documentation**| **Swagger / OpenAPI (Swashbuckle 10.2)** |
| **Unit Testing** | **xUnit**, **Moq** |

---

## 📋 API Endpoints

All endpoints can be accessed through the **API Gateway** (`http://localhost:5050`) or directly through service ports:

### 👤 Identity Service (`http://localhost:5001`)
| Method | Endpoint | Auth Required | Description |
| :--- | :--- | :---: | :--- |
| `POST` | `/api/auth/register` | No | Create new user account |
| `POST` | `/api/auth/login` | No | Authenticate & get JWT token |
| `GET` | `/api/users/profile` | **Yes (Bearer)** | Get authenticated user profile |
| `PUT` | `/api/users/profile` | **Yes (Bearer)** | Update profile details |
| `GET` | `/api/users` | **Yes (Bearer)** | Get all users |

### 💳 Credit Management Service (`http://localhost:5002`)
| Method | Endpoint | Auth Required | Description |
| :--- | :--- | :---: | :--- |
| `POST` | `/api/cards` | **Yes (Bearer)** | Add new credit card (Luhn validated) |
| `GET` | `/api/cards` | **Yes (Bearer)** | Get cards for authenticated user |
| `GET` | `/api/cards/{id}` | **Yes (Bearer)** | Get card details with recent bills |
| `PUT` | `/api/cards/{id}` | **Yes (Bearer)** | Update credit limit / card details |
| `DELETE` | `/api/cards/{id}` | **Yes (Bearer)** | Delete card |
| `POST` | `/api/bills/generate/{cardId}` | **Yes (Bearer)** | Generate monthly billing statement |
| `GET` | `/api/bills` | **Yes (Bearer)** | Get all bills for authenticated user |
| `GET` | `/api/bills/{id}` | **Yes (Bearer)** | Get bill details with payment breakdown |
| `POST` | `/api/payments` | **Yes (Bearer)** | Idempotent bill payment / settlement |
| `GET` | `/api/payments` | **Yes (Bearer)** | Get payment history |
| `GET` | `/api/payments/{id}` | **Yes (Bearer)** | Get payment details |

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [SQL Server](https://www.microsoft.com/sql-server) (LocalDB / SQLEXPRESS)

### 1. Clone the Repository
```bash
git clone https://github.com/Ayushdu37/credvault-backend.git
cd credvault-backend
```

### 2. Configure Database Connections
Update `ConnectionStrings` in `IdentityService/Identity.API/appsettings.json` and `CreditManagementService/CreditManagement.API/appsettings.json` to point to your SQL Server instance.

### 3. Apply EF Core Migrations
```bash
# Identity Service Database
dotnet ef database update --project IdentityService/Identity.Infrastructure --startup-project IdentityService/Identity.API

# Credit Management Service Database
dotnet ef database update --project CreditManagementService/CreditManagement.Infrastructure --startup-project CreditManagementService/CreditManagement.API
```

### 4. Run All Services
Open 3 separate terminals:

```powershell
# Terminal 1 — Identity Service (Port 5001)
dotnet run --project IdentityService/Identity.API

# Terminal 2 — Credit Management Service (Port 5002)
dotnet run --project CreditManagementService/CreditManagement.API

# Terminal 3 — Ocelot API Gateway (Port 5050)
dotnet run --project ApiGateway
```

> **Swagger UI will automatically launch in your browser:**
> - Identity Service: [http://localhost:5001/swagger](http://localhost:5001/swagger)
> - Credit Management Service: [http://localhost:5002/swagger](http://localhost:5002/swagger)
> - API Gateway: [http://localhost:5050](http://localhost:5050)

---

## 🧪 Running Unit Tests

The test suite covers happy paths, edge cases, domain validations, and exception scenarios with **100% test isolation** using **Moq**:

```bash
# Run all unit tests across the entire solution
dotnet test CredVault.slnx
```

---

## 📄 License
This project is developed for educational and portfolio demonstration purposes.
