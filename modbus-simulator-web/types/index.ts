export interface Connection {
  id: string
  name: string
  port: number
  protocolType: number // 0: ModbusRtuOverTcp, 1: ModbusTcp
}

export interface ConnectionTree extends Connection {
  slaves: Slave[]
}

export interface ProtocolType {
  value: number
  name: string
  displayName: string
  description: string
}

export interface Slave {
  id: string
  connId: string
  name: string
  slaveid: number
}

export interface Register {
  id: string
  slaveid: string
  startaddr: number
  hexdata: string
  names: string
  coefficients: string
}

export interface RegisterDefinition {
  address: number
  registerType: "Holding" | "Input" | "Coil" | "Discrete"
  dataType: "UInt16" | "Int16" | "Bool" | "Float32"
  defaultValue: number | boolean
  description?: string
}

export interface ApiError {
  error: string
  code: number
}

export interface CommunicationLog {
  id: string
  timestamp: string
  direction: "RX" | "TX"
  data: string
  connectionId: string
  slaveId: number
}
