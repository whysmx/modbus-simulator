# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

This is a Modbus simulator project with both backend and frontend components:

- **Backend**: ASP.NET Core 8.0 API (`modbus-simulator/`) - Modbus TCP simulator with SQLite database
- **Frontend**: Next.js React application (`modbus-simulator-web/`) - Web interface for managing connections and slaves

## Architecture Overview

### Backend Architecture
```
Controller → Service → Repository → Database (SQLite)
```

The backend follows a layered architecture with strict separation of concerns:
- **Controllers**: HTTP API endpoints for connections, slaves, and registers
- **Services**: Business logic and validation (ConnectionService, SlaveService, RegisterService)
- **Repositories**: Data access layer using Dapper ORM
- **TCP Services**: Modbus TCP protocol handlers and server management

Key components:
- `ModbusTcpService`: Handles Modbus TCP frame parsing and protocol logic
- `TcpServer`: Manages TCP connections and port listeners
- Data validation occurs at multiple layers with hex format validation for register data

### Frontend Architecture
- Next.js with TypeScript and Tailwind CSS
- Uses modern React patterns with hooks and context
- Component-based architecture with shadcn/ui components
- State management via Zustand

## Development Commands

### Backend (.NET)
```bash
# Navigate to backend directory
cd modbus-simulator

# Build the solution
dotnet build

# Run the application
dotnet run --project ModbusSimulator

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test ModbusSimulator.Tests

# Watch mode for continuous testing
dotnet watch test --project ModbusSimulator.Tests
```

### Frontend (Next.js)
```bash
# Navigate to frontend directory
cd modbus-simulator-web

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Start production server
npm run start

# Run linting
npm run lint
```

## Testing Strategy

The project follows strict **Test-Driven Development (TDD)** practices:

### Test Structure
- **Unit Tests**: Repository, Service, and Controller layers
- **Integration Tests**: End-to-end business workflows
- **TCP Protocol Tests**: Modbus frame parsing and protocol handling
- **Data Validation Tests**: Input validation and error handling

### Test Categories
- Repository tests: CRUD operations, constraints, database interactions
- Service tests: Business logic, validation rules, error handling
- Controller tests: HTTP endpoints, status codes, request/response formats
- TCP service tests: Protocol parsing, frame handling, connection management
- Integration tests: Complete workflows from API to database

### Running Tests
All tests use xUnit framework with Moq for mocking. Tests use in-memory SQLite databases for isolation.

## Database

- **Type**: SQLite (file-based for production, in-memory for tests)
- **ORM**: Dapper for lightweight data access
- **Migrations**: Manual SQL scripts (check `Repository` classes for schema)

Key entities:
- `Connections`: TCP connection configurations with ports and settings  
- `Slaves`: Modbus slave devices with addresses (1-247)
- `Registers`: Modbus registers with hex data validation

## Modbus Protocol Support

- **Modbus TCP**: Primary protocol implementation
- **Function Codes**: Read operations only (01-04)
  - 01: Read Coils
  - 02: Read Discrete Inputs  
  - 03: Read Holding Registers
  - 04: Read Input Registers
- **Address Ranges**: Validated per register type
- **Data Format**: Hexadecimal strings with strict validation

## Key Validation Rules

- Connection ports: 1-65535
- Slave addresses: 1-247  
- Register addresses: Type-specific ranges
- Hex data format: Must be valid hexadecimal
- Name constraints: Max 100 characters, auto-trimmed
- Uniqueness: Port per connection, address per slave, register addresses per slave

## Development Environment

- **.NET SDK**: 8.0+
- **Node.js**: 24.4.1
- **Package Manager**: npm 11.4.2
- **Database**: SQLite (cross-platform)

## Important Notes

- Tests must pass before any code changes are committed
- Follow TDD red-green-refactor cycle for new features
- All API endpoints return consistent error response formats
- TCP server manages multiple simultaneous connections
- Register data validation happens at controller and service levels
- Use dependency injection for all service dependencies