"use client"

import { createContext, useContext, useReducer, useCallback, type ReactNode } from "react"
import { apiClient } from "@/lib/api"
import type { Connection, Slave, Register, ConnectionTree } from "@/types"

interface ConnectionState {
  connections: ConnectionTree[]
  loading: boolean
  error: string | null
  selectedConnectionId: string | null
  selectedSlaveId: string | null
  selectedRegisterAddress: number | null
  selectedRegisterType: string | null
  openTabs: Array<{
    connectionId: string
    slaveId: string
    slaveName: string
    connectionName: string
    registerType: string
  }>
  activeTabIndex: number
  unsavedChanges: Map<string, any>
  hasUnsavedChanges: boolean
  loadingSlaves: Set<string>
  loadedSlaves: Set<string>
  slaveErrors: Map<string, string>
}

type ConnectionAction =
  | { type: "SET_LOADING"; payload: boolean }
  | { type: "SET_ERROR"; payload: string | null }
  | { type: "SET_CONNECTIONS"; payload: ConnectionTree[] }
  | { type: "SET_SELECTED_CONNECTION"; payload: string | null }
  | { type: "SET_SELECTED_SLAVE"; payload: string | null }
  | { type: "SET_SELECTED_REGISTER"; payload: number | null }
  | { type: "SET_SELECTED_REGISTER_TYPE"; payload: string | null }
  | {
      type: "OPEN_TAB"
      payload: {
        connectionId: string
        slaveId: string
        slaveName: string
        connectionName: string
        registerType: string
      }
    }
  | { type: "CLOSE_TAB"; payload: number }
  | { type: "SET_ACTIVE_TAB"; payload: number }
  | { type: "MARK_AS_CHANGED"; payload: { key: string; data: any } }
  | { type: "CLEAR_UNSAVED_CHANGES" }
  | { type: "SET_SLAVE_LOADING"; payload: { slaveId: string; loading: boolean } }
  | { type: "SET_SLAVE_ERROR"; payload: { slaveId: string; error: string | null } }
  | { type: "SET_SLAVE_LOADED"; payload: { slaveId: string; loaded: boolean } }
  | {
      type: "SET_SLAVE_REGISTERS"
      payload: { connectionId: string; slaveId: string; registers: Register[] }
    }

const initialState: ConnectionState = {
  connections: [],
  loading: false,
  error: null,
  selectedConnectionId: null,
  selectedSlaveId: null,
  selectedRegisterAddress: null,
  selectedRegisterType: null,
  openTabs: [],
  activeTabIndex: -1,
  unsavedChanges: new Map(),
  hasUnsavedChanges: false,
  loadingSlaves: new Set<string>(),
  loadedSlaves: new Set<string>(),
  slaveErrors: new Map<string, string>(),
}

function connectionReducer(state: ConnectionState, action: ConnectionAction): ConnectionState {
  console.log("[v0] Reducer action:", action.type, action.payload)
  switch (action.type) {
    case "SET_LOADING":
      console.log("[v0] Setting loading to:", action.payload)
      return { ...state, loading: action.payload }
    case "SET_ERROR":
      console.log("[v0] Setting error to:", action.payload)
      return { ...state, error: action.payload }
    case "SET_CONNECTIONS":
      console.log("[v0] Setting connections in reducer:", action.payload)
      console.log("[v0] Previous state connections:", state.connections)
      const newState = { ...state, connections: action.payload, loading: false }
      console.log("[v0] New state connections:", newState.connections)
      console.log("[v0] State update complete, connections length:", newState.connections.length)
      return newState
    case "SET_SELECTED_CONNECTION":
      return { ...state, selectedConnectionId: action.payload }
    case "SET_SELECTED_SLAVE":
      return { ...state, selectedSlaveId: action.payload }
    case "SET_SELECTED_REGISTER":
      return { ...state, selectedRegisterAddress: action.payload }
    case "SET_SELECTED_REGISTER_TYPE":
      return { ...state, selectedRegisterType: action.payload }
    case "OPEN_TAB": {
      const existingIndex = state.openTabs.findIndex(
        (tab) =>
          tab.connectionId === action.payload.connectionId &&
          tab.slaveId === action.payload.slaveId &&
          tab.registerType === action.payload.registerType,
      )

      if (existingIndex >= 0) {
        return {
          ...state,
          activeTabIndex: existingIndex,
          selectedRegisterType: action.payload.registerType,
          selectedConnectionId: action.payload.connectionId,
          selectedSlaveId: action.payload.slaveId,
        }
      }

      return {
        ...state,
        openTabs: [...state.openTabs, action.payload],
        activeTabIndex: state.openTabs.length,
        selectedRegisterType: action.payload.registerType,
        selectedConnectionId: action.payload.connectionId,
        selectedSlaveId: action.payload.slaveId,
      }
    }
    case "CLOSE_TAB": {
      const newTabs = state.openTabs.filter((_, i) => i !== action.payload)
      let newActiveIndex = state.activeTabIndex

      if (action.payload === state.activeTabIndex) {
        newActiveIndex = newTabs.length > 0 ? Math.max(0, action.payload - 1) : -1
      } else if (action.payload < state.activeTabIndex) {
        newActiveIndex = state.activeTabIndex - 1
      }

      const activeTab = newActiveIndex >= 0 ? newTabs[newActiveIndex] : null
      return {
        ...state,
        openTabs: newTabs,
        activeTabIndex: newActiveIndex,
        selectedRegisterType: activeTab?.registerType || null,
        selectedConnectionId: activeTab?.connectionId || null,
        selectedSlaveId: activeTab?.slaveId || null,
      }
    }
    case "SET_ACTIVE_TAB": {
      const activeTab = state.openTabs[action.payload]
      return {
        ...state,
        activeTabIndex: action.payload,
        selectedRegisterType: activeTab?.registerType || null,
        selectedConnectionId: activeTab?.connectionId || null,
        selectedSlaveId: activeTab?.slaveId || null,
      }
    }
    case "MARK_AS_CHANGED": {
      const newUnsavedChanges = new Map(state.unsavedChanges)
      newUnsavedChanges.set(action.payload.key, action.payload.data)
      return {
        ...state,
        unsavedChanges: newUnsavedChanges,
        hasUnsavedChanges: true,
      }
    }
    case "CLEAR_UNSAVED_CHANGES":
      return { ...state, unsavedChanges: new Map(), hasUnsavedChanges: false }
    case "SET_SLAVE_LOADING": {
      const next = new Set(state.loadingSlaves)
      if (action.payload.loading) next.add(action.payload.slaveId)
      else next.delete(action.payload.slaveId)
      return { ...state, loadingSlaves: next }
    }
    case "SET_SLAVE_ERROR": {
      const next = new Map(state.slaveErrors)
      if (action.payload.error) next.set(action.payload.slaveId, action.payload.error)
      else next.delete(action.payload.slaveId)
      return { ...state, slaveErrors: next }
    }
    case "SET_SLAVE_LOADED": {
      const next = new Set(state.loadedSlaves)
      if (action.payload.loaded) next.add(action.payload.slaveId)
      else next.delete(action.payload.slaveId)
      return { ...state, loadedSlaves: next }
    }
    case "SET_SLAVE_REGISTERS": {
      const { connectionId, slaveId, registers } = action.payload
      const connections = state.connections.map((conn) => {
        if (conn.id !== connectionId) return conn
        return {
          ...conn,
          slaves: conn.slaves.map((s: any) =>
            s.id === slaveId ? { ...s, registers } : s,
          ),
        }
      })
      return { ...state, connections }
    }
    default:
      return state
  }
}

interface ConnectionContextType extends ConnectionState {
  // API actions
  loadConnections: () => Promise<void>

  // Connection actions
  addConnection: (connection: Omit<Connection, "id">) => Promise<void>
  updateConnection: (id: string, updates: Partial<Connection>) => Promise<void>
  deleteConnection: (id: string) => Promise<void>

  // Slave actions
  addSlave: (connectionId: string, slave: Omit<Slave, "id" | "connid">) => Promise<void>
  updateSlave: (connectionId: string, slaveId: string, updates: Partial<Slave>) => Promise<void>
  deleteSlave: (connectionId: string, slaveId: string) => Promise<void>

  // Register actions
  loadRegisters: (connectionId: string, slaveId: string) => Promise<Register[]>
  loadRegistersForSlave: (connectionId: string, slaveId: string, force?: boolean) => Promise<Register[]>
  addRegister: (connectionId: string, slaveId: string, register: Omit<Register, "id" | "slaveid">) => Promise<void>
  updateRegister: (
    connectionId: string,
    slaveId: string,
    registerId: string,
    updates: Partial<Register>,
  ) => Promise<void>
  deleteRegister: (connectionId: string, slaveId: string, registerId: string) => Promise<void>

  // Selection actions
  setSelectedConnection: (id: string | null) => void
  setSelectedSlave: (id: string | null) => void
  setSelectedRegister: (address: number | null) => void
  setSelectedRegisterType: (type: string | null) => void

  // Aliases for better API naming
  selectConnection: (id: string | null) => void
  selectSlave: (id: string | null) => void
  selectRegister: (address: number | null) => void

  // Utility functions
  getConnectionById: (id: string) => ConnectionTree | undefined
  getSlaveById: (connectionId: string, slaveId: string) => Slave | undefined

  // Tab management functions
  openRegisterTab: (
    connectionId: string,
    slaveId: string,
    slaveName: string,
    connectionName: string,
    registerType: string,
  ) => void
  closeTab: (index: number) => void
  setActiveTab: (index: number) => void

  saveChanges: () => Promise<void>
  markAsChanged: (key: string, data: any) => void
  clearUnsavedChanges: () => void

  getRegistersByType: (connectionId: string, slaveId: string, registerType: string) => Register[]
  classifyRegisterType: (startaddr: number) => string
  getRegisterTypeFromRegtype: (regtype: string) => string
  // Slave register cache helpers
  isSlaveLoading: (slaveId: string) => boolean
  isSlaveLoaded: (slaveId: string) => boolean
  getSlaveError: (slaveId: string) => string | undefined
}

const ConnectionContext = createContext<ConnectionContextType | undefined>(undefined)

export function ConnectionProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(connectionReducer, initialState)

  const loadConnections = useCallback(async () => {
    console.log("[v0] Loading connections...")
    dispatch({ type: "SET_LOADING", payload: true })
    dispatch({ type: "SET_ERROR", payload: null })
    try {
      console.log("[v0] Calling apiClient.getConnectionTree()...")
      const connections = await apiClient.getConnectionTree()
      console.log("[v0] API response received:", connections)
      console.log("[v0] Number of connections:", connections.length)
      console.log("[v0] Raw connections data:", JSON.stringify(connections, null, 2))
      console.log("[v0] About to dispatch SET_CONNECTIONS")
      dispatch({ type: "SET_CONNECTIONS", payload: connections })
      console.log("[v0] Dispatch SET_CONNECTIONS completed")
    } catch (error) {
      console.error("[v0] Error loading connections:", error)
      console.error("[v0] Error details:", error instanceof Error ? error.message : error)
      console.error("[v0] Error stack:", error instanceof Error ? error.stack : 'No stack trace')
      dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to load connections" })
      dispatch({ type: "SET_LOADING", payload: false })
    }
  }, [])

  const addConnection = useCallback(
    async (connectionData: Omit<Connection, "id">) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.createConnection(connectionData)
        await loadConnections()
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to create connection" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections],
  )

  const updateConnection = useCallback(
    async (id: string, updates: Partial<Connection>) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.updateConnection(id, updates)
        await loadConnections()
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to update connection" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections],
  )

  const deleteConnection = useCallback(
    async (id: string) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.deleteConnection(id)
        await loadConnections()
        if (state.selectedConnectionId === id) {
          dispatch({ type: "SET_SELECTED_CONNECTION", payload: null })
        }
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to delete connection" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections, state.selectedConnectionId],
  )

  const addSlave = useCallback(
    async (connectionId: string, slaveData: Omit<Slave, "id" | "connid">) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        console.log("[v0] addSlave called with:", { connectionId, slaveData })
        const result = await apiClient.createSlave(connectionId, slaveData)
        console.log("[v0] apiClient.createSlave result:", result)
        console.log("[v0] Slave created successfully, reloading connections...")
        await loadConnections()
        console.log("[v0] Connections reloaded after slave creation")
      } catch (error) {
        console.error("[v0] Error creating slave:", error)
        console.error("[v0] Error details:", error instanceof Error ? error.message : error)
        console.error("[v0] Error stack:", error instanceof Error ? error.stack : 'No stack trace')
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to create slave" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections],
  )

  const updateSlave = useCallback(
    async (connectionId: string, slaveId: string, updates: Partial<Slave>) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.updateSlave(connectionId, slaveId, updates)
        await loadConnections()
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to update slave" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections],
  )

  const deleteSlave = useCallback(
    async (connectionId: string, slaveId: string) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.deleteSlave(connectionId, slaveId)
        await loadConnections()
        if (state.selectedSlaveId === slaveId) {
          dispatch({ type: "SET_SELECTED_SLAVE", payload: null })
        }
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to delete slave" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadConnections, state.selectedSlaveId],
  )

  const loadRegisters = useCallback(async (connectionId: string, slaveId: string): Promise<Register[]> => {
    try {
      return await apiClient.getRegisters(connectionId, slaveId)
    } catch (error) {
      dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to load registers" })
      return []
    }
  }, [])



  const loadRegistersForSlave = useCallback(
    async (connectionId: string, slaveId: string, force = false): Promise<Register[]> => {
      console.log("[v0] loadRegistersForSlave called:", { connectionId, slaveId, force })
      console.log("[v0] loadedSlaves state:", state.loadedSlaves)
      console.log("[v0] Is slave already loaded?", state.loadedSlaves.has(slaveId))
      
      if (!force && state.loadedSlaves.has(slaveId)) {
        console.log("[v0] Returning cached registers for slave:", slaveId)
        // Already loaded - find slave directly in state without using getSlaveById
        const connection = state.connections.find((conn) => conn.id === connectionId)
        const slave = connection?.slaves.find((s) => s.id === slaveId) as any
        const registers = slave?.registers || []
        console.log("[v0] Cached registers:", registers)
        return registers
      }

      console.log("[v0] Loading registers for slave from API:", slaveId)
      dispatch({ type: "SET_SLAVE_ERROR", payload: { slaveId, error: null } })
      dispatch({ type: "SET_SLAVE_LOADING", payload: { slaveId, loading: true } })
      console.log("[v0] Set loading state to true for slave:", slaveId)
      
      try {
        const regs = await apiClient.getRegisters(connectionId, slaveId)
        console.log("[v0] API returned registers:", regs)
        dispatch({ type: "SET_SLAVE_REGISTERS", payload: { connectionId, slaveId, registers: regs } })
        dispatch({ type: "SET_SLAVE_LOADED", payload: { slaveId, loaded: true } })
        console.log("[v0] Updated state with registers and marked as loaded")
        return regs
      } catch (e) {
        const msg = e instanceof Error ? e.message : "Failed to load registers"
        console.error("[v0] Error loading registers:", e)
        dispatch({ type: "SET_SLAVE_ERROR", payload: { slaveId, error: msg } })
        return []
      } finally {
        dispatch({ type: "SET_SLAVE_LOADING", payload: { slaveId, loading: false } })
        console.log("[v0] Set loading state to false for slave:", slaveId)
      }
    },
    [state.loadedSlaves, state.connections],
  )

  const addRegister = useCallback(
    async (connectionId: string, slaveId: string, registerData: Omit<Register, "id" | "slaveid">) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.createRegister(connectionId, slaveId, registerData)
        await loadRegistersForSlave(connectionId, slaveId, true)
        dispatch({ type: "SET_LOADING", payload: false })
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to create register" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadRegistersForSlave],
  )

  const updateRegister = useCallback(
    async (connectionId: string, slaveId: string, registerId: string, updates: Partial<Register>) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.updateRegister(connectionId, slaveId, registerId, updates)
        await loadRegistersForSlave(connectionId, slaveId, true)
        dispatch({ type: "SET_LOADING", payload: false })
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to update register" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadRegistersForSlave],
  )

  const deleteRegister = useCallback(
    async (connectionId: string, slaveId: string, registerId: string) => {
      dispatch({ type: "SET_LOADING", payload: true })
      dispatch({ type: "SET_ERROR", payload: null })
      try {
        await apiClient.deleteRegister(connectionId, slaveId, registerId)
        await loadRegistersForSlave(connectionId, slaveId, true)
        dispatch({ type: "SET_LOADING", payload: false })
      } catch (error) {
        dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to delete register" })
        dispatch({ type: "SET_LOADING", payload: false })
      }
    },
    [loadRegistersForSlave],
  )

  const setSelectedConnection = useCallback((id: string | null) => {
    dispatch({ type: "SET_SELECTED_CONNECTION", payload: id })
  }, [])

  const setSelectedSlave = useCallback((id: string | null) => {
    dispatch({ type: "SET_SELECTED_SLAVE", payload: id })
  }, [])

  const setSelectedRegister = useCallback((address: number | null) => {
    dispatch({ type: "SET_SELECTED_REGISTER", payload: address })
  }, [])

  const setSelectedRegisterType = useCallback((type: string | null) => {
    dispatch({ type: "SET_SELECTED_REGISTER_TYPE", payload: type })
  }, [])

  const getConnectionById = useCallback(
    (id: string) => {
      return state.connections.find((conn) => conn.id === id)
    },
    [state.connections],
  )

  const getSlaveById = useCallback(
    (connectionId: string, slaveId: string) => {
      const connection = getConnectionById(connectionId)
      return connection?.slaves.find((slave) => slave.id === slaveId)
    },
    [getConnectionById],
  )

  const openRegisterTab = useCallback(
    (connectionId: string, slaveId: string, slaveName: string, connectionName: string, registerType: string) => {
      dispatch({ type: "OPEN_TAB", payload: { connectionId, slaveId, slaveName, connectionName, registerType } })
    },
    [],
  )

  const closeTab = useCallback((index: number) => {
    dispatch({ type: "CLOSE_TAB", payload: index })
  }, [])

  const setActiveTab = useCallback((index: number) => {
    dispatch({ type: "SET_ACTIVE_TAB", payload: index })
  }, [])

  const saveChanges = useCallback(async () => {
    dispatch({ type: "SET_LOADING", payload: true })
    dispatch({ type: "SET_ERROR", payload: null })

    try {
      for (const [key, data] of state.unsavedChanges.entries()) {
        const [connectionId, slaveId, registerId] = key.split(":")
        await apiClient.updateRegister(connectionId, slaveId, registerId, data)
      }

      dispatch({ type: "CLEAR_UNSAVED_CHANGES" })
      await loadConnections()
    } catch (error) {
      dispatch({ type: "SET_ERROR", payload: error instanceof Error ? error.message : "Failed to save changes" })
      dispatch({ type: "SET_LOADING", payload: false })
    }
  }, [state.unsavedChanges, loadConnections])

  const markAsChanged = useCallback((key: string, data: any) => {
    dispatch({ type: "MARK_AS_CHANGED", payload: { key, data } })
  }, [])

  const clearUnsavedChanges = useCallback(() => {
    dispatch({ type: "CLEAR_UNSAVED_CHANGES" })
  }, [])

  const classifyRegisterType = useCallback((startaddr: number) => {
    if (startaddr >= 1 && startaddr <= 9999) return "线圈"
    if (startaddr >= 10001 && startaddr <= 19999) return "离散输入"
    if (startaddr >= 30001 && startaddr <= 39999) return "输入寄存器"
    if (startaddr >= 40001 && startaddr <= 49999) return "保持寄存器"
    return "未知类型"
  }, [])

  const getRegisterTypeFromRegtype = useCallback((regtype: string) => {
    switch (regtype) {
      case "01":
        return "线圈"
      case "02":
        return "离散输入"
      case "03":
        return "保持寄存器"
      case "04":
        return "输入寄存器"
      default:
        return "未知类型"
    }
  }, [])

  const getRegistersByType = useCallback(
    (connectionId: string, slaveId: string, registerType: string) => {
      const connection = getConnectionById(connectionId)
      const slave = connection?.slaves.find((s) => s.id === slaveId)
      if (!slave?.registers) return []

      return slave.registers.filter((register) => {
        const type = classifyRegisterType(register.startaddr)
        return type === registerType
      })
    },
    [getConnectionById, classifyRegisterType],
  )



  const contextValue: ConnectionContextType = {
    ...state,
    loadConnections,
    addConnection,
    updateConnection,
    deleteConnection,
    addSlave,
    updateSlave,
    deleteSlave,
    loadRegisters,
    loadRegistersForSlave,
    addRegister,
    updateRegister,
    deleteRegister,
    setSelectedConnection,
    setSelectedSlave,
    setSelectedRegister,
    setSelectedRegisterType,
    selectConnection: setSelectedConnection,
    selectSlave: setSelectedSlave,
    selectRegister: setSelectedRegister,
    getConnectionById,
    getSlaveById,
    openRegisterTab,
    closeTab,
    setActiveTab,
    saveChanges,
    markAsChanged,
    clearUnsavedChanges,
    getRegistersByType,
    classifyRegisterType,
    getRegisterTypeFromRegtype,
    isSlaveLoading: (slaveId: string) => state.loadingSlaves.has(slaveId),
    isSlaveLoaded: (slaveId: string) => state.loadedSlaves.has(slaveId),
    getSlaveError: (slaveId: string) => state.slaveErrors.get(slaveId),
  }

  return <ConnectionContext.Provider value={contextValue}>{children}</ConnectionContext.Provider>
}

export function useConnections() {
  const context = useContext(ConnectionContext)
  if (context === undefined) {
    throw new Error("useConnections must be used within a ConnectionProvider")
  }
  return context
}
