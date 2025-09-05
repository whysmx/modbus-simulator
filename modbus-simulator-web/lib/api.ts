import type { Connection, Slave, Register, ConnectionTree, ApiError, ProtocolType } from "@/types"

const API_BASE = typeof window !== "undefined" ? "/api" : "http://localhost:5000/api"

class ApiClient {
  private async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE}${endpoint}`, {
      headers: {
        "Content-Type": "application/json",
        ...options?.headers,
      },
      ...options,
    })

    if (!response.ok) {
      const error: ApiError = await response.json()
      throw new Error(error.error || `HTTP ${response.status}`)
    }

    if (response.status === 204) {
      return {} as T
    }

    return response.json()
  }

  // 前端显示：编辑框中每两个字符加空格
  private convertHexDataForFrontend(apiRegisters: Register[]): Register[] {
    return apiRegisters.map(reg => ({
      ...reg,
      hexdata: this.formatHexForDisplay(reg.hexdata || "")
    }))
  }

  // 保存时处理：去掉空格，转大写
  private convertHexDataForAPI(hexdata: string): string {
    return hexdata.replace(/\s/g, '').toUpperCase()
  }

  // 格式化十六进制字符串用于显示：每两个字符加空格
  private formatHexForDisplay(hex: string): string {
    const cleaned = hex.replace(/\s/g, '').toUpperCase()
    return cleaned.replace(/(.{2})/g, '$1 ').trim()
  }

  // Connection endpoints
  async getProtocolTypes(): Promise<ProtocolType[]> {
    return this.request<ProtocolType[]>("/connections/protocol-types")
  }

  async getConnectionTree(): Promise<ConnectionTree[]> {
    return this.request<ConnectionTree[]>("/connections/tree")
  }

  async createConnection(connection: Omit<Connection, "id">): Promise<Connection> {
    const apiConnection = {
      name: connection.name,
      protocolType: connection.protocolType
    }
    return this.request<Connection>("/connections", {
      method: "POST",
      body: JSON.stringify(apiConnection),
    })
  }

  async updateConnection(id: string, updates: Partial<Connection>): Promise<Connection> {
    const apiUpdates = {
      name: updates.name,
      port: updates.port,
      protocolType: updates.protocolType
    }
    return this.request<Connection>(`/connections/${id}`, {
      method: "PUT",
      body: JSON.stringify(apiUpdates),
    })
  }

  async deleteConnection(id: string): Promise<void> {
    return this.request<void>(`/connections/${id}`, {
      method: "DELETE",
    })
  }

  // Slave endpoints
  async createSlave(connectionId: string, slave: Omit<Slave, "id" | "connId">): Promise<Slave> {
    console.log("[v0] ApiClient.createSlave called with:", { connectionId, slave })
    const apiSlave = {
      name: slave.name,
      slaveid: slave.slaveid
    }
    console.log("[v0] Making API request to:", `/connections/${connectionId}/slaves`)
    console.log("[v0] Request body:", JSON.stringify(apiSlave))
    
    try {
      const result = await this.request<Slave>(`/connections/${connectionId}/slaves`, {
        method: "POST",
        body: JSON.stringify(apiSlave),
      })
      console.log("[v0] API response:", result)
      return result
    } catch (error) {
      console.error("[v0] API error in createSlave:", error)
      throw error
    }
  }

  async updateSlave(connectionId: string, slaveId: string, updates: Partial<Slave>): Promise<Slave> {
    const apiUpdates = {
      name: updates.name,
      slaveid: updates.slaveid
    }
    return this.request<Slave>(`/connections/${connectionId}/slaves/${slaveId}`, {
      method: "PUT",
      body: JSON.stringify(apiUpdates),
    })
  }

  async deleteSlave(connectionId: string, slaveId: string): Promise<void> {
    return this.request<void>(`/connections/${connectionId}/slaves/${slaveId}`, {
      method: "DELETE",
    })
  }

  // Register endpoints
  async getRegisters(connectionId: string, slaveId: string): Promise<Register[]> {
    const registers = await this.request<Register[]>(`/connections/${connectionId}/slaves/${slaveId}/registers`)
    return this.convertHexDataForFrontend(registers)
  }

  async createRegister(
    connectionId: string,
    slaveId: string,
    register: Omit<Register, "id" | "slaveid">,
  ): Promise<Register> {
    const apiRegister = {
      ...register,
      hexdata: this.convertHexDataForAPI(register.hexdata)
    }
    const result = await this.request<Register>(`/connections/${connectionId}/slaves/${slaveId}/registers`, {
      method: "POST",
      body: JSON.stringify(apiRegister),
    })
    return this.convertHexDataForFrontend([result])[0]
  }

  async updateRegister(
    connectionId: string,
    slaveId: string,
    registerId: string,
    updates: Partial<Register>,
  ): Promise<Register> {
    let apiUpdates = updates
    if (updates.hexdata !== undefined) {
      apiUpdates = {
        ...updates,
        hexdata: this.convertHexDataForAPI(updates.hexdata)
      }
    }
    const result = await this.request<Register>(`/connections/${connectionId}/slaves/${slaveId}/registers/${registerId}`, {
      method: "PUT",
      body: JSON.stringify(apiUpdates),
    })
    return this.convertHexDataForFrontend([result])[0]
  }

  async deleteRegister(connectionId: string, slaveId: string, registerId: string): Promise<void> {
    return this.request<void>(`/connections/${connectionId}/slaves/${slaveId}/registers/${registerId}`, {
      method: "DELETE",
    })
  }
}

export const apiClient = new ApiClient()
