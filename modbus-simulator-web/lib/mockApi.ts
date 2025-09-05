import type { Connection, Slave, Register, ConnectionTree } from "@/types"

const mockConnections: Connection[] = [
  {
    id: "0123456789abcdef0123456789abcdef",
    name: "主连接",
    port: 502,
  },
  {
    id: "ffeeddccbbaa99887766554433221100",
    name: "备用连接",
    port: 503,
  },
  {
    id: "11223344556677889900aabbccddeeff",
    name: "测试连接",
    port: 504,
  },
]

const mockSlaves: Slave[] = [
  {
    id: "00112233445566778899aabbccddeeff",
    connid: "0123456789abcdef0123456789abcdef",
    name: "温度传感器",
    slaveid: 1,
  },
  {
    id: "aabbccddeeff00112233445566778899",
    connid: "0123456789abcdef0123456789abcdef",
    name: "压力传感器",
    slaveid: 2,
  },
  {
    id: "99887766554433221100ffeeddccbbaa",
    connid: "ffeeddccbbaa99887766554433221100",
    name: "电机控制器",
    slaveid: 1,
  },
  {
    id: "ddeeff001122334455667788990abbcc",
    connid: "11223344556677889900aabbccddeeff",
    name: "开关控制",
    slaveid: 1,
  },
  {
    id: "5566778899aabbccddeeff0011223344",
    connid: "11223344556677889900aabbccddeeff",
    name: "数据采集",
    slaveid: 2,
  },
]

const mockRegisters: Register[] = [
  // 保持寄存器 (40001-49999) - 温度传感器
  {
    id: "reg001",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 40001,
    hexdata: "0064,00C8,012C,0190,01F4,0258,02BC,0320,0384,03E8",
  },
  {
    id: "reg002",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 40100,
    hexdata: "0001,0002,03E8,07D0,1388,1F40,2710,30D4",
  },

  // 线圈 (1-9999) - 温度传感器
  {
    id: "reg003",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 1,
    hexdata: "FF,AA,55",
  },
  {
    id: "reg004",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 100,
    hexdata: "0F,F0",
  },

  // 离散输入 (10001-19999) - 温度传感器
  {
    id: "reg005",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 10001,
    hexdata: "A5,F0,33,CC",
  },

  // 输入寄存器 (30001-39999) - 温度传感器
  {
    id: "reg006",
    slaveid: "00112233445566778899aabbccddeeff",
    startaddr: 30001,
    hexdata: "1388,1770,1B58,1F40,2328,2710,2AF8,2EE0",
  },

  // 保持寄存器 - 压力传感器
  {
    id: "reg007",
    slaveid: "aabbccddeeff00112233445566778899",
    startaddr: 40001,
    hexdata: "00FA,012C,015E,0190,01C2,01F4,0226,0258,028A,02BC,02EE,0320",
  },
  {
    id: "reg008",
    slaveid: "aabbccddeeff00112233445566778899",
    startaddr: 40200,
    hexdata: "4120,0000,4140,0000,4160,0000,4180,0000",
  },

  // 线圈 - 压力传感器
  {
    id: "reg009",
    slaveid: "aabbccddeeff00112233445566778899",
    startaddr: 50,
    hexdata: "3C,C3",
  },

  // 离散输入 - 压力传感器
  {
    id: "reg010",
    slaveid: "aabbccddeeff00112233445566778899",
    startaddr: 10050,
    hexdata: "5A,A5",
  },

  // 输入寄存器 - 压力传感器
  {
    id: "reg011",
    slaveid: "aabbccddeeff00112233445566778899",
    startaddr: 30100,
    hexdata: "2710,30D4,3A98,445C,4E20,57E4,61A8,6B6C",
  },

  // 保持寄存器 - 电机控制器
  {
    id: "reg012",
    slaveid: "99887766554433221100ffeeddccbbaa",
    startaddr: 40001,
    hexdata: "0000,0001,07D0,1388,0000,FFFF,8000,7FFF",
  },

  // 线圈 - 电机控制器
  {
    id: "reg013",
    slaveid: "99887766554433221100ffeeddccbbaa",
    startaddr: 1,
    hexdata: "01,02,04,08,10,20,40,80",
  },

  // 离散输入 - 电机控制器
  {
    id: "reg014",
    slaveid: "99887766554433221100ffeeddccbbaa",
    startaddr: 10001,
    hexdata: "FF,00,AA,55",
  },

  // 输入寄存器 - 电机控制器
  {
    id: "reg015",
    slaveid: "99887766554433221100ffeeddccbbaa",
    startaddr: 30001,
    hexdata: "4000,0000,4080,0000,40A0,0000,40C0,0000",
  },

  // 开关控制 - 所有类型
  {
    id: "reg016",
    slaveid: "ddeeff001122334455667788990abbcc",
    startaddr: 40001,
    hexdata: "0001,0000,0001,0000",
  },
  {
    id: "reg017",
    slaveid: "ddeeff001122334455667788990abbcc",
    startaddr: 1,
    hexdata: "0F,F0,AA,55",
  },
  {
    id: "reg018",
    slaveid: "ddeeff001122334455667788990abbcc",
    startaddr: 10001,
    hexdata: "C3,3C",
  },
  {
    id: "reg019",
    slaveid: "ddeeff001122334455667788990abbcc",
    startaddr: 30001,
    hexdata: "1000,2000,3000,4000,5000,6000",
  },

  // 数据采集 - 大量数据
  {
    id: "reg020",
    slaveid: "5566778899aabbccddeeff0011223344",
    startaddr: 40001,
    hexdata: "0001,0002,0003,0004,0005,0006,0007,0008,0009,000A,000B,000C,000D,000E,000F,0010",
  },
  {
    id: "reg021",
    slaveid: "5566778899aabbccddeeff0011223344",
    startaddr: 1,
    hexdata: "FF,FF,00,00,AA,AA,55,55",
  },
  {
    id: "reg022",
    slaveid: "5566778899aabbccddeeff0011223344",
    startaddr: 10001,
    hexdata: "01,02,04,08,10,20,40,80,FF,FE,FC,F8,F0,E0,C0,80",
  },
  {
    id: "reg023",
    slaveid: "5566778899aabbccddeeff0011223344",
    startaddr: 30001,
    hexdata: "4200,0000,4220,0000,4240,0000,4260,0000,4280,0000,42A0,0000,42C0,0000,42E0,0000",
  },
]

// Mock API responses with delay simulation
export class MockApiClient {
  private delay(ms = 300) {
    return new Promise((resolve) => setTimeout(resolve, ms))
  }

  async getConnectionTree(): Promise<ConnectionTree[]> {
    await this.delay()

    return mockConnections.map((conn) => ({
      ...conn,
      slaves: mockSlaves
        .filter((slave) => slave.connid === conn.id)
        .map((slave) => ({
          ...slave,
          registers: mockRegisters.filter((reg) => reg.slaveid === slave.id),
        })),
    }))
  }

  async createConnection(connection: Omit<Connection, "id">): Promise<Connection> {
    await this.delay()
    const maxPort = Math.max(...mockConnections.map((c) => c.port), 501)
    const newConnection: Connection = {
      ...connection,
      id: `conn-${Date.now()}`,
      port: maxPort + 1,
    }
    mockConnections.push(newConnection)
    return newConnection
  }

  async updateConnection(id: string, updates: Partial<Connection>): Promise<Connection> {
    await this.delay()
    const index = mockConnections.findIndex((c) => c.id === id)
    if (index === -1) throw new Error("Connection not found")

    mockConnections[index] = { ...mockConnections[index], ...updates }
    return mockConnections[index]
  }

  async deleteConnection(id: string): Promise<void> {
    await this.delay()
    const index = mockConnections.findIndex((c) => c.id === id)
    if (index === -1) throw new Error("Connection not found")

    mockConnections.splice(index, 1)
    // Also remove related slaves and registers
    const slavesToRemove = mockSlaves.filter((s) => s.connid === id)
    slavesToRemove.forEach((slave) => {
      const slaveIndex = mockSlaves.findIndex((s) => s.id === slave.id)
      if (slaveIndex !== -1) mockSlaves.splice(slaveIndex, 1)

      const registerIndexes = mockRegisters
        .map((r, i) => (r.slaveid === slave.id ? i : -1))
        .filter((i) => i !== -1)
        .reverse()
      registerIndexes.forEach((i) => mockRegisters.splice(i, 1))
    })
  }

  async createSlave(connectionId: string, slave: Omit<Slave, "id" | "connid">): Promise<Slave> {
    await this.delay()
    const newSlave: Slave = {
      ...slave,
      id: `slave-${Date.now()}`,
      connid: connectionId,
    }
    mockSlaves.push(newSlave)
    return newSlave
  }

  async updateSlave(connectionId: string, slaveId: string, updates: Partial<Slave>): Promise<Slave> {
    await this.delay()
    const index = mockSlaves.findIndex((s) => s.id === slaveId && s.connid === connectionId)
    if (index === -1) throw new Error("Slave not found")

    mockSlaves[index] = { ...mockSlaves[index], ...updates }
    return mockSlaves[index]
  }

  async deleteSlave(connectionId: string, slaveId: string): Promise<void> {
    await this.delay()
    const index = mockSlaves.findIndex((s) => s.id === slaveId && s.connid === connectionId)
    if (index === -1) throw new Error("Slave not found")

    mockSlaves.splice(index, 1)
    // Also remove related registers
    const registerIndexes = mockRegisters
      .map((r, i) => (r.slaveid === slaveId ? i : -1))
      .filter((i) => i !== -1)
      .reverse()
    registerIndexes.forEach((i) => mockRegisters.splice(i, 1))
  }

  async getRegisters(connectionId: string, slaveId: string): Promise<Register[]> {
    await this.delay()
    return mockRegisters.filter((r) => r.slaveid === slaveId)
  }

  async createRegister(
    connectionId: string,
    slaveId: string,
    register: Omit<Register, "id" | "slaveid">,
  ): Promise<Register> {
    await this.delay()
    const newRegister: Register = {
      ...register,
      id: `reg-${Date.now()}`,
      slaveid: slaveId,
    }
    mockRegisters.push(newRegister)
    return newRegister
  }

  async updateRegister(
    connectionId: string,
    slaveId: string,
    registerId: string,
    updates: Partial<Register>,
  ): Promise<Register> {
    await this.delay()
    const index = mockRegisters.findIndex((r) => r.id === registerId && r.slaveid === slaveId)
    if (index === -1) throw new Error("Register not found")

    mockRegisters[index] = { ...mockRegisters[index], ...updates }
    return mockRegisters[index]
  }

  async deleteRegister(connectionId: string, slaveId: string, registerId: string): Promise<void> {
    await this.delay()
    const index = mockRegisters.findIndex((r) => r.id === registerId && r.slaveid === slaveId)
    if (index === -1) throw new Error("Register not found")

    mockRegisters.splice(index, 1)
  }
}

export const mockApiClient = new MockApiClient()
