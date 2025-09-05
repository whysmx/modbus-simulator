# 建议的开发命令

## 后端开发命令 (.NET)
```bash
# 切换到后端目录
cd modbus-simulator

# 构建解决方案
dotnet build

# 运行主应用
dotnet run --project ModbusSimulator

# 运行所有测试
dotnet test

# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 监视模式运行测试 (开发时推荐)
dotnet watch test --project ModbusSimulator.Tests

# 清理构建产物
dotnet clean
```

## 前端开发命令 (Next.js)
```bash
# 切换到前端目录
cd modbus-simulator-web

# 安装依赖 (支持npm或pnpm)
npm install
# 或
pnpm install

# 启动开发服务器
npm run dev

# 构建生产版本
npm run build

# 启动生产服务器
npm run start

# 运行ESLint检查
npm run lint

# 运行Playwright E2E测试
npm run test:e2e
# 或
pnpm test:e2e

# 以UI模式运行Playwright测试
npm run test:e2e:ui
# 或
pnpm test:e2e:ui
```

## Git和系统命令 (macOS/Darwin)
```bash
# Git操作
git status
git add .
git commit -m "message"
git push
git pull

# 文件系统操作
ls -la          # 列出文件详情
find . -name "*.cs"    # 查找C#文件
grep -r "pattern" .    # 搜索模式
cd directory    # 切换目录

# 进程管理
ps aux | grep dotnet   # 查找.NET进程
lsof -i :5000         # 查看端口5000使用情况
```

## 任务完成后必须执行的命令
1. **后端**: `dotnet test` (确保所有测试通过)
2. **前端**: `npm run lint` 和 `npm run test:e2e` (检查代码质量和E2E测试)
3. **构建验证**: `dotnet build` (后端) 和 `npm run build` (前端)