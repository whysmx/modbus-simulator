# 代码风格和约定

## C# (.NET) 约定
- **命名约定**: PascalCase用于类、方法、属性；camelCase用于局部变量和参数
- **文件结构**: 每个类一个文件，文件名与类名相同
- **命名空间**: 遵循项目结构 (如 `ModbusSimulator.Controllers`)
- **架构模式**: 严格的分层架构，Controllers → Services → Repositories
- **依赖注入**: 使用ASP.NET Core内置DI容器
- **测试**: 严格遵循TDD (测试驱动开发) 实践
- **数据访问**: 使用Dapper进行轻量级ORM操作
- **验证**: 多层验证 (Controller层和Service层)

## TypeScript/React 约定
- **组件**: 使用函数式组件和React Hooks
- **命名**: PascalCase用于组件，camelCase用于变量和函数
- **状态管理**: 使用Zustand进行全局状态管理
- **样式**: 使用Tailwind CSS类名，遵循utility-first原则
- **UI组件**: 基于shadcn/ui组件系统
- **类型安全**: 严格的TypeScript配置，避免`any`类型
- **文件结构**: 组件在`components/`，页面在`app/`，工具函数在`lib/`

## 项目特定约定
- **数据验证**: 十六进制数据必须严格验证
- **端口范围**: 1-65535
- **从设备地址**: 1-247
- **寄存器地址**: 根据类型验证不同范围
- **错误处理**: 统一的错误响应格式
- **TCP连接**: 支持多个同时连接
- **协议支持**: 仅支持Modbus TCP读操作 (功能码01-04)

## 测试约定
- **TDD**: 红-绿-重构循环
- **隔离**: 每个测试使用独立的内存数据库
- **覆盖率**: 追求高测试覆盖率
- **分类**: 单元测试、集成测试、E2E测试分离
- **模拟**: 使用Moq进行依赖模拟 (.NET)，Playwright进行E2E测试 (Web)