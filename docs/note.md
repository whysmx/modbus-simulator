# TODO: 双层树 + 展开加载第三层 调整清单

## 前端（modbus-simulator-web）
- [x] 实现从机节点展开时调用 `GET /api/connections/{id}/slaves/{slaveId}/registers` 并缓存结果（按 `slaveId`）
- [x] 依据 `startaddr` 将寄存器分类为四类并在树内物化四个类型节点（显示计数徽标）
- [x] 点击类型在右侧打开/切换标签页，使用缓存数据渲染；本地增删改后更新缓存或标记失效
- [x] 错误与空态：加载中 spinner、失败提示、四类节点恒显（为0）
- [ ] 可选优化（后续）：支持仅拉某类型或摘要（保留向后兼容）

## 后端（ModbusSimulator）
- [x] 无需修改现有路由；确认 `GET /api/connections/tree` 不包含寄存器明细
- [x] 确认 `GET /api/connections/{id}/slaves/{slaveId}/registers` 返回指定从机全部寄存器组
- [x] 日志/错误信息遵循 `{"error":"...","code":HTTP_CODE}` 规范

## 自动化测试（优先级：高→中）
### 前端 E2E（Playwright）
- [x] 展开从机时发起一次寄存器请求，请求成功后出现四类类型节点及计数
- [x] 点击某类型后右侧打开对应标签并显示正确数据（与分类一致）
- [x] 增删改寄存器后，树计数与右侧列表同步更新；再次展开不重复拉取（有缓存）或在失效后重拉
- [x] 错误用例：寄存器接口 400/404/500 时的提示与回退

### 后端单元/集成测试
- [x] `ConnectionsController.GetConnectionsTree` 不返回寄存器明细
- [x] `RegistersController.GetRegisters` 指定从机返回全部寄存器组
- [x] 边界：空从机返回空数组；不同地址区间的数据正确覆盖
- [x] 错误：不存在的 connectionId/slaveId 返回 404；业务验证错误返回 400


dotnet test "/Users/wen/Desktop/code/10Modbus/modbus-simulator/modbus-simulator/ModbusSimulator.Tests/ModbusSimulator.Tests.csproj" --collect:"XPlat Code Coverage" --results-directory "/Users/wen/Desktop/code/10Modbus/modbus-simulator/modbus-simulator/ModbusSimulator.Tests/TestResults"



/Users/wen/.dotnet/tools/reportgenerator -reports:"/Users/wen/Desktop/code/10Modbus/modbus-simulator/modbus-simulator/ModbusSimulator.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"/Users/wen/Desktop/code/10Modbus/modbus-simulator/coveragereport"
open "/Users/wen/Desktop/code/10Modbus/modbus-simulator/coveragereport/index.html"