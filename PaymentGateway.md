# 📄 Payment Gateway Challenge Documentation

## 📌 Overview

This project is a **.NET 8 Web API** solution simulating a Payment Gateway system that:

- Accepts credit card payments
- Forwards them to a simulated bank
- Returns a response (Authorized/Declined)
- Stores and retrieves payment information securely

---

## 🧱 Key Components

### 1. **API Layer**
- **`PaymentsController`**
  - Handles `POST /api/payments`: Processes a new payment
  - Handles `GET /api/payments/{id}`: Retrieves a payment by ID
  - Validates model and card expiry before delegating to the service

### 2. **Services**
- **`IPaymentsService` / `PaymentsService`**
  - Business logic layer
  - Interacts with:
    - `IBankClient`: for bank interaction
    - `IPaymentRepository`: for persistence

- **`IBankClient` / `BankClient`**
  - Sends payment requests to a simulated bank (via HTTP)
  - Handles success, empty, or `503 Service Unavailable` responses
  - Returns `BankResponse` to the service

### 3. **Persistence**
- **`IPaymentRepository` / `PaymentRepository`**
  - Uses an in-memory thread-safe `ConcurrentDictionary<Guid, PaymentResponse>`
  - Stores and retrieves payment records

---

## 🔄 Class Interaction Flow

```
[Client/API Request]
      |
      v
PaymentsController
    |
    v
IPaymentsService (PaymentsService)
    |
    +--> Validates input
    |
    +--> IBankClient (BankClient)
    |       |
    |       +--> POST request to bank
    |
    +--> IPaymentRepository (PaymentRepository)
            |
            +--> Save or retrieve PaymentResponse
```

---

## ✅ Testing

- Unit tests for services, repository, and controller logic
- Integration tests using `WebApplicationFactory<Program>`
- Mocking with `Moq` for `IPaymentsService` and `IBankClient`
- Validation tests for bad models, invalid expiry dates, etc.

---