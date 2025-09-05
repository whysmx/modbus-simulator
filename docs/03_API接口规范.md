   
  -- 组合查询
  GET /api/connections/tree            -- 获取连接+从机树形结构
   
   -- 连接
  POST /api/connections                -- 创建连接
  PUT /api/connections/{id}            -- 更新连接
  DELETE /api/connections/{id}         -- 删除连接

  -- 从机
  POST /api/connections/{id}/slaves                    -- 创建从机
  PUT /api/connections/{id}/slaves/{slaveId}           -- 更新从机
  DELETE /api/connections/{id}/slaves/{slaveId}        -- 删除从机

  -- 寄存器
  GET /api/connections/{id}/slaves/{slaveId}/registers    -- 获取寄存器列表
  POST /api/connections/{id}/slaves/{slaveId}/registers   -- 创建寄存器组
  PUT /api/connections/{id}/slaves/{slaveId}/registers/{regId}   -- 更新寄存器组
  DELETE /api/connections/{id}/slaves/{slaveId}/registers/{regId} -- 删除寄存器组




接口返回数据结构

-- 通用对象定义
  Connection {
    id: string,        -- 连接ID，32位无横杠UUID
    name: string,      -- 连接名称
    port: number,      -- 端口号（新增连接时自动分配，从502开始递增）
    protocolType: number  -- 协议类型：0=Modbus RTU Over TCP（默认），1=Modbus TCP
  }

  Slave {
    id: string,         -- 从机ID，32位无横杠UUID
    connid: string,    -- 所属连接ID
    name: string,       -- 从机名称
    slaveid: number     -- 从机地址（1-247）
  }

  Register {
    id: string,         -- 寄存器块ID，32位无横杠UUID
    slaveid: string,   -- 所属从机ID
    startaddr: number,  -- 起始逻辑地址
    hexdata: string     -- 连续数据的16进制字符串（大写、无0x前缀；依据地址区间确定规则）
                        -- 当 startaddr ∈ [30001–39999] 或 [40001–49999]（功能码04/03，寄存器型）时：
                        --   长度为4的倍数；寄存器数量 = hexdata.Length / 4（每4个hex=1个16位寄存器）
                        -- 当 startaddr ∈ [00001–09999] 或 [10001–19999]（功能码01/02，位型）时：
                        --   长度为2的倍数；覆盖位数 = (hexdata.Length / 2) * 8（每2个hex=1字节=8位）
  }


-- 响应约定
  列表接口：200 OK，Body=资源数组（如 Connection[] / Slave[] / Register[]）
  创建接口：201 Created，Body=新建实体
  更新接口：200 OK，Body=更新后实体（或错误时400/404）
  删除接口：204 No Content（或错误时404）
  树查询接口：200 OK，Body=ConnectionTree[]


-- 组合查询
  GET /api/connections/tree            返回：ConnectionTree[]
  ConnectionTree = Connection & { slaves: Slave[] }

-- 树与寄存器加载策略（约定）
  1) 设备树默认仅返回两层数据：连接(Connection) 与 从机(Slave)
  2) 前端在“展开某个从机”时，调用下列接口拉取该从机下的全部寄存器组：
     GET /api/connections/{id}/slaves/{slaveId}/registers
  3) 前端根据返回数据的 startaddr 字段，将寄存器按地址区间分类为四种类型节点（线圈/离散输入/输入寄存器/保持寄存器），以补齐第三层，仅用于树展示与右侧联动
  4) 树接口不返回寄存器明细，减少首屏数据量；寄存器列表接口用于按需懒加载


-- 示例
  GET /api/connections/{id}/slaves/{slaveId}/registers 响应示例：
  [
    { "id": "0a1b2c3d4e5f60718293a4b5c6d7e8f9", "slaveid": "00112233445566778899aabbccddeeff", "startaddr": 0, "hexdata": "00112233AABB" },
    { "id": "9f8e7d6c5b4a3928171605f4e3d2c1b0", "slaveid": "00112233445566778899aabbccddeeff", "startaddr": 100, "hexdata": "DEADBEEF" }
  ]

  GET /api/connections/tree 响应示例：
  [
    {
      "id": "0123456789abcdef0123456789abcdef",
      "name": "主连接",
      "port": 502,
      "protocolType": 0,
      "slaves": [
        { "id": "00112233445566778899aabbccddeeff", "connid": "0123456789abcdef0123456789abcdef", "name": "从机1", "slaveid": 1 },
        { "id": "ffeeddccbbaa99887766554433221100", "connid": "0123456789abcdef0123456789abcdef", "name": "从机2", "slaveid": 2 }
      ]
    }
  ]


-- 地址区间与类型映射（前端分类用）
  - 线圈（Coil，FC01）：00001–09999（位型）
  - 离散输入（Discrete Input，FC02）：10001–19999（位型）
  - 输入寄存器（Input Register，FC04）：30001–39999（寄存器型）
  - 保持寄存器（Holding Register，FC03）：40001–49999（寄存器型）


-- 错误返回规范
  后端在方法内部进行业务验证，有错误时返回相应HTTP状态码和错误信息：
  
  常见错误类型：
  - 400 Bad Request：业务验证失败（如端口已被使用、参数错误等）
  - 404 Not Found：资源不存在（如连接ID不存在）
  - 201 Created：创建成功
  - 200 OK：更新成功
  - 204 No Content：删除成功
  
  错误返回格式：
  {
    "error": "具体错误描述",     -- 用于前端显示的错误信息
    "code": 400              -- HTTP状态码
  }
  
  示例：
  PUT /api/connections/abc123 时端口冲突 → 400 { "error": "端口已被使用", "code": 400 }
  PUT /api/connections/notfound 连接不存在 → 404 { "error": "连接不存在", "code": 404 }
  POST /api/connections/{id}/slaves/{slaveId}/registers 地址范围与已有记录重叠 → 400 { "error": "地址范围与已有记录重叠", "code": 400 }
