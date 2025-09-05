import { create } from "zustand"
import type { CommunicationLog } from "@/types"

interface CommunicationLogStore {
  logs: CommunicationLog[]
  isLogging: boolean
  filter: "All" | "RX" | "TX"
  format: "Hex" | "ASCII"
  autoScroll: boolean
  addLog: (log: Omit<CommunicationLog, "id" | "timestamp">) => void
  clearLogs: () => void
  startLogging: () => void
  stopLogging: () => void
  setFilter: (filter: "All" | "RX" | "TX") => void
  setFormat: (format: "Hex" | "ASCII") => void
  toggleAutoScroll: () => void
}

export const useCommunicationLog = create<CommunicationLogStore>((set) => ({
  logs: [],
  isLogging: false,
  filter: "All",
  format: "Hex",
  autoScroll: true,

  addLog: (logData) => {
    const log: CommunicationLog = {
      ...logData,
      id: `log_${Date.now()}_${Math.random()}`,
      timestamp: new Date().toISOString(),
    }

    set((state) => ({
      logs: [...state.logs, log],
    }))
  },

  clearLogs: () => {
    set({ logs: [] })
  },

  startLogging: () => {
    set({ isLogging: true })
  },

  stopLogging: () => {
    set({ isLogging: false })
  },

  setFilter: (filter) => {
    set({ filter })
  },

  setFormat: (format) => {
    set({ format })
  },

  toggleAutoScroll: () => {
    set((state) => ({ autoScroll: !state.autoScroll }))
  },
}))
