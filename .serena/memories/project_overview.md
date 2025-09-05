# Modbus TCP 模拟器项目概述

## 项目目的
这是一个Modbus TCP协议模拟器项目，提供完整的Web界面来管理和配置Modbus设备。包含后端API服务和前端Web应用。

## 技术栈

### 后端 (.NET)
- **框架**: ASP.NET Core 8.0
- **数据库**: SQLite (生产环境使用文件数据库，测试使用内存数据库)
- **ORM**: Dapper (轻量级数据访问)
- **测试框架**: xUnit + Moq
- **架构模式**: 分层架构 (Controllers → Services → Repositories)

### 前端 (Next.js)
- **框架**: Next.js 15.2.4 with App Router
- **语言**: TypeScript 5
- **样式**: Tailwind CSS 4.1.9
- **UI组件**: shadcn/ui (基于Radix UI)
- **状态管理**: Zustand
- **测试**: Playwright E2E测试
- **包管理**: 支持npm和pnpm

## 项目结构
```
modbus-simulator/           # 根目录
├── modbus-simulator/       # 后端 .NET 项目
│   ├── modbus-simulator.sln     # Visual Studio解决方案文件
│   ├── ModbusSimulator/         # 主项目
│   └── ModbusSimulator.Tests/   # 测试项目
├── modbus-simulator-web/   # 前端 Next.js 项目
├── docs/                   # 项目文档
└── CLAUDE.md              # Claude Code配置
```

## 核心功能
- Modbus TCP协议支持 (功能码01-04)
- 连接管理 (TCP端口配置)
- 从设备管理 (地址1-247)
- 寄存器管理 (十六进制数据验证)
- 实时通信日志
- Web界面管理