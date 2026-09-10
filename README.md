# Hotel Booking API

A Ready production-oriented Hotel Booking RESTful API built with **ASP.NET Core .NET 9**, designed to handle hotel room management, bookings, payments, authentication, authorization, and other core hotel operations.

The project focuses on building a maintainable backend with clear separation of responsibilities, secure authentication, payment integration, caching, logging, exception handling, and concurrency control.

---

## Features

### Authentication & Authorization

* JWT-based authentication
* ASP.NET Core Identity
* Role-based authorization
* Google OAuth authentication
* User profile management
* Email confirmation
* Password recovery
* Role management

### User Roles

The system supports different roles based on the user's responsibilities:

* Admin
* Receptionist
* Guest

Each role has access only to the operations required for its responsibilities.

---

## Hotel & Room Management

* Create, update, and delete rooms
* Upload room images
* Manage room status
* Filter rooms by type and availability
* Pagination
* Room availability checking

### Room Types

* Single
* Double
* Suite

### Room Statuses

* Available
* Reserved
* Occupied
* Cleaning
* Maintenance
* Out Of Service

---

## Booking Management

The booking system handles the complete booking lifecycle.

### Main Operations

* Create booking
* Check room availability
* Calculate booking price
* Manage booking rooms
* Manage booking services
* Cancel booking
* Check-in
* Check-out
* Booking status management

The booking flow also takes payment status into consideration before completing sensitive booking operations.

---

## Payment Integration

The project integrates **Paymob** for card payment processing.

### Payment Flow

```text
Customer
   |
   v
Create Booking
   |
   v
Create Payment
   |
   v
Generate Paymob Checkout
   |
   v
Customer Completes Payment
   |
   v
Paymob Webhook
   |
   v
Validate HMAC
   |
   v
Process Payment Callback
   |
   v
Update Payment Status
```

### Payment Statuses

* Pending
* Processing
* Paid
* Failed
* Expired
* Partially Refunded
* Refunded

### Webhook Handling

The system uses the **Paymob callback/webhook** to confirm payment results.

The callback:

1. Validates the received payload.
2. Checks the transaction type.
3. Validates the Paymob HMAC.
4. Finds the related payment.
5. Prevents duplicate processing.
6. Updates the payment status.
7. Stores the provider event information.

This approach avoids relying on the browser redirect as the source of truth for payment confirmation.

---

## Refunds & Cancellation

The booking cancellation flow handles paid payments and refunds.

The system supports:

* Cancellation validation
* Processing paid payments
* Refund handling
* Payment status updates
* Refund records
* Payment transaction tracking
* Notifications after payment/refund operations

---

## Redis Caching

Redis is used to reduce unnecessary database queries for frequently requested data.

One of the main cached operations is:

```text
Available Rooms
       |
       v
Redis Cache
       |
       +-- Cache Hit  -> Return cached data
       |
       +-- Cache Miss -> Query SQL Server
                            |
                            v
                       Store in Redis
```

The available-room cache uses short expiration times because room availability is highly dynamic.

---

## Logging

The project uses **Serilog** for structured application logging.

Logging is used for important application events such as:

* Payment processing
* Payment callbacks
* Exceptions
* Booking operations
* Important business operations

Sensitive information such as passwords, secrets, and payment credentials should not be logged.

---

## Global Exception Handling

Instead of handling exceptions independently inside every controller, the project uses a global exception handling mechanism.

```text
Exception
    |
    v
Global Exception Handler
    |
    +-- Validation Error
    +-- Not Found
    +-- Conflict
    +-- Forbidden
    +-- Unexpected Error
            |
            v
    Consistent API Response
```

Custom application exceptions are used to represent expected business errors.

Examples include:

* `NotFoundException`
* `ConflictException`
* `ForbiddenException`
* `ValidationAppException`

---

## Transactions & Concurrency

The project considers data consistency during critical booking operations.

### Transactions

Transactions are used when multiple related database operations must succeed or fail together.

For example:

```text
Booking Cancellation
       |
       +-- Update Booking
       +-- Process Payment
       +-- Create Refund
       +-- Create Notification
```

### Optimistic Concurrency

Rooms use a `RowVersion` property to help detect concurrent modifications.

This is important in a hotel booking system because multiple users may try to interact with the same room at nearly the same time.

---

## Idempotency

Payment-related operations need to be safe when the same provider event is received more than once.

The project tracks provider transaction/event information to avoid processing the same payment event repeatedly.

This is especially important for webhook-based payment integrations because providers may retry callbacks.

---

## Database

The project uses:

* SQL Server
* Entity Framework Core
* ASP.NET Core Identity

The database contains the main entities required for the hotel booking workflow, including:

* Users
* Rooms
* Bookings
* Booking Rooms
* Hotel Services
* Payments
* Payment Transactions
* Refunds
* Reviews
* Coupons
* Notifications

---

## Architecture

The application follows a layered approach that separates HTTP concerns from business logic and data access.

```text
                    Client
                      |
                      v
                Controllers
                      |
                      v
                   Services
                      |
                      v
                 Repositories
                      |
                      v
            Entity Framework Core
                      |
                      v
                  SQL Server
```

Supporting infrastructure:

```text
              +---------------+
              |     Redis     |
              +---------------+
                      ^
                      |
Controllers -> Services -> Repositories
                      |
                      v
              External Services
                      |
                 +----+----+
                 |         |
              Paymob   Google OAuth
```

The controllers are kept focused on HTTP concerns while business rules are handled inside application services.

---

## Technologies

| Technology            | Purpose                   |
| --------------------- | ------------------------- |
| .NET 9                | Backend platform          |
| ASP.NET Core Web API  | REST API                  |
| Entity Framework Core | ORM                       |
| SQL Server            | Database                  |
| ASP.NET Core Identity | User management           |
| JWT                   | Authentication            |
| Google OAuth          | External authentication   |
| Redis                 | Caching                   |
| StackExchange.Redis   | Redis integration         |
| Paymob                | Payment processing        |
| Serilog               | Structured logging        |
| Scalar                | API documentation/testing |

---

## Project Structure

```text
Hotel Booking
|
+-- Areas
|   +-- Admin
|   +-- ...
|
+-- Controllers
|
+-- Services
|   +-- Interfaces
|   +-- Implementations
|
+-- Repositories
|
+-- Models
|
+-- DTOs
|   +-- Requests
|   +-- Responses
|
+-- Exceptions
|
+-- Data
|
+-- Helpers
|
+-- Program.cs
```

---

## API Authentication

Protected endpoints require a valid JWT token.

Example:

```http
Authorization: Bearer <access_token>
```

Role-based endpoints use ASP.NET Core authorization:

```csharp
[Authorize(Roles = SD.ADMIN_ROLE)]
```

---

## API Documentation

The project uses **Scalar** for API documentation and testing.

When running the application locally, the API documentation can be accessed through the configured Scalar endpoint.

---

## Getting Started

### Prerequisites

Make sure you have:

* .NET 9 SDK
* SQL Server
* Redis
* A Paymob test account if payment functionality is required
* Google OAuth credentials if Google authentication is required

### Clone the Repository

```bash
git clone <YOUR_REPOSITORY_URL>
cd "Hotel Booking"
```

### Configure Application Settings

Create your local configuration based on the project's configuration structure.

Required configuration includes:

```text
SQL Server Connection String
JWT Settings
Google OAuth Settings
Redis Configuration
Paymob Settings
```

Do not commit secrets or credentials to GitHub.

---

## Database Setup

Run Entity Framework migrations:

```bash
dotnet ef database update
```

Then run the application:

```bash
dotnet run
```

---

## Security Notes

Sensitive configuration should be stored outside source control.

Never commit:

```text
JWT Secret Keys
Paymob API Keys
Paymob HMAC Secrets
Google Client Secrets
Database Passwords
```

For local development, use:

* User Secrets
* Environment Variables
* Local configuration files excluded from Git

---

## Testing Payment Integration Locally

For local Paymob webhook testing, the application can be exposed through a tunneling service such as ngrok.

Example flow:

```text
Paymob
   |
   v
Public HTTPS Endpoint
   |
   v
Local ASP.NET Core API
   |
   v
/api/Payments/paymob/callback
```

The webhook endpoint is designed to accept Paymob's callback and process the transaction independently from the browser redirect.

---

## Project Goals

This project was built to practice and demonstrate real backend concepts beyond basic CRUD APIs.

The main goals were:

* Building RESTful APIs with ASP.NET Core
* Applying layered architecture
* Implementing authentication and authorization
* Integrating third-party payment services
* Handling payment webhooks
* Maintaining data consistency
* Handling concurrency
* Implementing Redis caching
* Implementing structured logging
* Designing centralized exception handling
* Working with Entity Framework Core and SQL Server

---

# Future Improvements

Potential future improvements include:

* Automated integration tests
* More advanced monitoring
* Background processing for non-critical operations
* Containerization
* CI/CD pipeline
* Cloud deployment
* More advanced reporting and analytics

---

## Author

**Abdelrahman Osama**

Backend Developer — .NET

* GitHub: https://github.com/Abdo-Ossama


---



The focus is not only on making the API work, but also on understanding the backend engineering decisions behind authentication, payments, caching, concurrency, error handling, and data consistency.
