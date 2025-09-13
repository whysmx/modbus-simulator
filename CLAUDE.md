# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

- **Backend**: ASP.NET Core 8.0 API (`modbus-simulator/`) - Modbus TCP simulator
- **Frontend**: Next.js React app (`modbus-simulator-web/`) - Web interface
- **Architecture**: Controller → Service → Repository → SQLite Database
- **Key Components**: ModbusTcpService (protocol), TcpServer (connections)

## Development Commands

### Backend
```bash
cd modbus-simulator
dotnet build
dotnet test
dotnet watch test --project ModbusSimulator.Tests  # TDD development
```

### Frontend
```bash
cd modbus-simulator-web
npm run dev
npm run lint
npm run test:e2e
```

## Testing

- **TDD required**: Red-green-refactor cycle
- **xUnit + Moq** for .NET, **Playwright** for E2E
- Tests use isolated in-memory SQLite databases

## Key Constraints

- **Ports**: 1-65535, **Slave addresses**: 1-247
- **Hex data validation** at multiple layers
- **Modbus TCP only**: Function codes 01-04 (read operations)
- **Database**: SQLite with Dapper ORM

## Before Commits

- `dotnet test` (backend) and `npm run lint && npm run test:e2e` (frontend)
- All tests must pass, follow TDD cycle