import { create } from "zustand"
import type { Device, Register } from "@/types"

interface DeviceStore {
  devices: Device[]
  selectedDeviceId: string | null
  addDevice: (device: Omit<Device, "id" | "createdAt" | "updatedAt">) => void
  updateDevice: (id: string, updates: Partial<Device>) => void
  deleteDevice: (id: string) => void
  selectDevice: (id: string | null) => void
  addRegister: (deviceId: string, register: Omit<Register, "group">) => void
  updateRegister: (deviceId: string, address: number, updates: Partial<Register>) => void
  deleteRegister: (deviceId: string, address: number) => void
  startDevice: (id: string) => void
  stopDevice: (id: string) => void
}

const generateRegisterGroup = (type: Register["type"]): string => {
  switch (type) {
    case "Holding":
      return "4x 保持寄存器"
    case "Input":
      return "3x 输入寄存器"
    case "Coil":
      return "0x 线圈"
    case "Discrete":
      return "1x 离散输入"
    default:
      return "未知类型"
  }
}

const sampleDevices: Device[] = [
  {
    id: "device1",
    name: "主控PLC",
    protocol: "ModbusTcp",
    port: 502,
    slaveId: 1,
    status: "Running",
    byteOrder: "BigEndian",
    timeoutMs: 5000,
    whitelistPolicy: "Exception",
    createdAt: "2024-08-19T10:00:00Z",
    updatedAt: "2024-08-19T10:00:00Z",
    registers: [
      {
        address: 0,
        type: "Holding",
        group: "4x 保持寄存器",
        hexValue: "0x7EEE",
        int16Value: 32494,
        uint16Value: 32494,
        floatABCD: 1.58e38,
        floatCDAB: 1.58e38,
        floatBADC: -1.57e-35,
        floatDCBA: -1.57e-35,
        stringValue: "1",
        description: "系统状态寄存器",
        dataType: "UInt16",
        defaultValue: 32494,
      },
      {
        address: 1,
        type: "Input",
        group: "3x 输入寄存器",
        hexValue: "0x85A6",
        int16Value: -31322,
        uint16Value: 34214,
        floatABCD: -9.28e-16,
        floatCDAB: -8.45e-12,
        floatBADC: 1.75e-26,
        floatDCBA: -1.12e-12,
        stringValue: "2",
        description: "温度传感器1",
        dataType: "Int16",
        defaultValue: -31322,
      },
    ],
  },
  {
    id: "device2",
    name: "温度传感器",
    protocol: "ModbusTcp",
    port: 503,
    slaveId: 3,
    status: "Stopped",
    byteOrder: "BigEndian",
    timeoutMs: 5000,
    whitelistPolicy: "Exception",
    createdAt: "2024-08-19T10:00:00Z",
    updatedAt: "2024-08-19T10:00:00Z",
    registers: [
      {
        address: 0,
        type: "Coil",
        group: "0x 线圈",
        hexValue: "0x0001",
        int16Value: 1,
        uint16Value: 1,
        floatABCD: 1.4e-45,
        floatCDAB: 1.4e-45,
        floatBADC: 1.4e-45,
        floatDCBA: 1.4e-45,
        stringValue: "3",
        description: "报警状态",
        dataType: "Bool",
        defaultValue: true,
      },
    ],
  },
]

export const useDevices = create<DeviceStore>((set, get) => ({
  devices: sampleDevices,
  selectedDeviceId: null,

  addDevice: (deviceData) => {
    const newDevice: Device = {
      ...deviceData,
      id: `device_${Date.now()}`,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    set((state) => ({
      devices: [...state.devices, newDevice],
    }))
  },

  updateDevice: (id, updates) => {
    set((state) => ({
      devices: state.devices.map((device) =>
        device.id === id ? { ...device, ...updates, updatedAt: new Date().toISOString() } : device,
      ),
    }))
  },

  deleteDevice: (id) => {
    set((state) => ({
      devices: state.devices.filter((device) => device.id !== id),
      selectedDeviceId: state.selectedDeviceId === id ? null : state.selectedDeviceId,
    }))
  },

  selectDevice: (id) => {
    set({ selectedDeviceId: id })
  },

  addRegister: (deviceId, registerData) => {
    const register: Register = {
      ...registerData,
      group: generateRegisterGroup(registerData.type),
    }

    set((state) => ({
      devices: state.devices.map((device) =>
        device.id === deviceId
          ? {
              ...device,
              registers: [...device.registers, register],
              updatedAt: new Date().toISOString(),
            }
          : device,
      ),
    }))
  },

  updateRegister: (deviceId, address, updates) => {
    set((state) => ({
      devices: state.devices.map((device) =>
        device.id === deviceId
          ? {
              ...device,
              registers: device.registers.map((register) =>
                register.address === address ? { ...register, ...updates } : register,
              ),
              updatedAt: new Date().toISOString(),
            }
          : device,
      ),
    }))
  },

  deleteRegister: (deviceId, address) => {
    set((state) => ({
      devices: state.devices.map((device) =>
        device.id === deviceId
          ? {
              ...device,
              registers: device.registers.filter((register) => register.address !== address),
              updatedAt: new Date().toISOString(),
            }
          : device,
      ),
    }))
  },

  startDevice: (id) => {
    get().updateDevice(id, { status: "Running" })
  },

  stopDevice: (id) => {
    get().updateDevice(id, { status: "Stopped" })
  },
}))
