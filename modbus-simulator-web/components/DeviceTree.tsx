"use client"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Cable, Cpu, MemoryStick, ChevronDown, ChevronRight, MoreVertical, Loader2 } from "lucide-react"
import { useConnections } from "@/hooks/useConnections"
import { cn } from "@/lib/utils"
import { useState } from "react"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"

interface DeviceTreeProps {
  editingDevice: string | null
  setEditingDevice: (value: string | null) => void
  editingSlave: { connectionId: string; slaveId: string | null } | null
  setEditingSlave: (value: { connectionId: string; slaveId: string | null } | null) => void
  showRegisterDialog: { connectionId: string; slaveId: string } | null
  setShowRegisterDialog: (value: { connectionId: string; slaveId: string } | null) => void
  editingRegister: { connectionId: string; slaveId: string; registerName: string; registerData: any } | null
  setEditingRegister: (
    value: { connectionId: string; slaveId: string; registerName: string; registerData: any } | null,
  ) => void
  collapsed?: boolean
}

export function DeviceTree({
  editingDevice,
  setEditingDevice,
  editingSlave,
  setEditingSlave,
  showRegisterDialog,
  setShowRegisterDialog,
  editingRegister,
  setEditingRegister,
  collapsed = false,
}: DeviceTreeProps) {
  const {
    connections,
    selectedConnectionId,
    selectedSlaveId,
    selectedRegisterType,
    selectConnection,
    selectSlave,
    selectRegister,
    deleteConnection,
    deleteSlave,
    deleteRegister,
    openTab,
    startSlave,
    stopSlave,
    toggleConnection,
    openRegisterTab,
    classifyRegisterType,
    getRegistersByType,
    loadRegistersForSlave,
    isSlaveLoading,
    isSlaveLoaded,
    getSlaveError,
  } = useConnections()

  console.log("[v0] DeviceTree connections:", connections)
  console.log("[v0] DeviceTree connections length:", connections.length)
  console.log("[v0] DeviceTree connections detailed:", JSON.stringify(connections, null, 2))

  const [expandedConnections, setExpandedConnections] = useState<Set<string>>(new Set())
  const [expandedSlaves, setExpandedSlaves] = useState<Set<string>>(new Set())

  const toggleConnectionExpansion = (connectionId: string) => {
    const newExpanded = new Set(expandedConnections)
    if (newExpanded.has(connectionId)) {
      newExpanded.delete(connectionId)
    } else {
      newExpanded.add(connectionId)
    }
    setExpandedConnections(newExpanded)
  }

  const toggleSlave = async (slaveKey: string, connectionId?: string, slaveId?: string) => {
    console.log("[v0] toggleSlave called with:", { slaveKey, connectionId, slaveId })
    console.log("[v0] Current expandedSlaves:", expandedSlaves)
    const newExpanded = new Set(expandedSlaves)
    const wasExpanded = newExpanded.has(slaveKey)
    console.log("[v0] wasExpanded:", wasExpanded)
    
    if (wasExpanded) {
      newExpanded.delete(slaveKey)
      console.log("[v0] Collapsing slave:", slaveKey)
    } else {
      newExpanded.add(slaveKey)
      console.log("[v0] Expanding slave:", slaveKey)
    }
    setExpandedSlaves(newExpanded)
    console.log("[v0] New expandedSlaves:", newExpanded)

    // Load registers when expanding (not when was already expanded)
    if (!wasExpanded && connectionId && slaveId) {
      console.log("[v0] Loading registers for slave:", slaveId)
      await loadRegistersForSlave(connectionId, slaveId)
    }
  }

  if (collapsed) {
    return (
      <TooltipProvider>
        <Card className="h-full">
          <div className="p-2 space-y-2">
            {connections.map((connection) => (
              <Tooltip key={connection.id}>
                <TooltipTrigger asChild>
                  <div className="flex flex-col items-center gap-1 p-2 rounded-md hover:bg-accent transition-colors cursor-pointer">
                    <Cable className="w-5 h-5 text-blue-600" />
                  </div>
                </TooltipTrigger>
                <TooltipContent side="right">
                  <div>
                    <p className="font-medium">{connection.name}</p>
                    <p className="text-xs text-muted-foreground">端口: {connection.port}</p>
                  </div>
                </TooltipContent>
              </Tooltip>
            ))}
          </div>
        </Card>
      </TooltipProvider>
    )
  }

  return (
    <TooltipProvider>
      <Card className="h-full">
        <div className="p-2 space-y-1">
          {connections.length === 0 && (
            <div className="p-4 text-center text-muted-foreground">
              <p>正在加载连接数据...</p>
              <p className="text-xs mt-1">如果持续显示此消息，请检查控制台日志</p>
            </div>
          )}
          {connections.map((connection) => (
            <div key={connection.id}>
              {/* Level 1: Connection */}
              <div
                className={cn(
                  "flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
                  selectedConnectionId === connection.id && "bg-accent",
                )}
                onClick={() => {
                  selectConnection(connection.id)
                  toggleConnectionExpansion(connection.id)
                }}
              >
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-4 w-4 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    toggleConnectionExpansion(connection.id)
                  }}
                >
                  {expandedConnections.has(connection.id) ? (
                    <ChevronDown className="w-3 h-3" />
                  ) : (
                    <ChevronRight className="w-3 h-3" />
                  )}
                </Button>
                <Cable className="w-4 h-4 text-blue-600 flex-shrink-0" />
                <div className="min-w-0 flex-1">
                  <div className="text-sm font-medium truncate">{connection.name}</div>
                  <div className="text-xs text-muted-foreground">端口: {connection.port}</div>
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-6 w-6 p-0 flex-shrink-0"
                      aria-haspopup="menu"
                      aria-label="连接操作"
                      data-testid={`connection-actions-${connection.id}`}
                      onClick={(e) => e.stopPropagation()}
                    >
                      <MoreVertical className="w-3 h-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => setEditingSlave({ connectionId: connection.id, slaveId: null })}>
                      新增从机
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => setEditingDevice(connection.id)}>编辑连接</DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (confirm("确定要删除此连接吗？")) {
                          deleteConnection(connection.id)
                        }
                      }}
                      className="text-red-600"
                    >
                      删除连接
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>

              {/* Level 2: Slaves (only show if connection is expanded) */}
              {expandedConnections.has(connection.id) &&
                connection.slaves.map((slave) => {
                  const slaveKey = `${connection.id}-${slave.id}`
                  return (
                    <div key={slaveKey} className="ml-6">
                      <div
                        className={cn(
                          "flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
                          selectedSlaveId === slave.id && "bg-accent",
                        )}
                        onClick={async () => {
                          selectSlave(slave.id)
                          await toggleSlave(slaveKey, connection.id, slave.id)
                        }}
                      >
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-4 w-4 p-0"
                          onClick={async (e) => {
                            e.stopPropagation()
                            await toggleSlave(slaveKey, connection.id, slave.id)
                          }}
                        >
                          {expandedSlaves.has(slaveKey) ? (
                            <ChevronDown className="w-3 h-3" />
                          ) : (
                            <ChevronRight className="w-3 h-3" />
                          )}
                        </Button>
                        <Cpu className="w-4 h-4 text-purple-600 flex-shrink-0" />
                        <div className="min-w-0 flex-1">
                          <div className="text-sm font-medium truncate">{slave.name}</div>
                          <div className="text-xs text-muted-foreground">从机地址: {slave.slaveid}</div>
                        </div>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-6 w-6 p-0 flex-shrink-0"
                              aria-haspopup="menu"
                              aria-label="从机操作"
                              data-testid={`slave-actions-${slave.id}`}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <MoreVertical className="w-3 h-3" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              onClick={() => setShowRegisterDialog({ connectionId: connection.id, slaveId: slave.id })}
                            >
                              新增寄存器
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              onClick={() => setEditingSlave({ connectionId: connection.id, slaveId: slave.id })}
                            >
                              编辑从机
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              onClick={() => {
                                if (confirm("确定要删除此从机吗？")) {
                                  deleteSlave(connection.id, slave.id)
                                }
                              }}
                              className="text-red-600"
                            >
                              删除从机
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>

                      {expandedSlaves.has(slaveKey) && (
                        <div className="ml-6 space-y-1">
                          {isSlaveLoading(slave.id) && (
                            <div className="flex items-center gap-2 text-xs text-muted-foreground px-2 py-1">
                              <Loader2 className="w-3 h-3 animate-spin" /> 正在加载寄存器...
                            </div>
                          )}
                          {getSlaveError(slave.id) && (
                            <div className="text-xs text-red-600 px-2 py-1">加载失败：{getSlaveError(slave.id)}</div>
                          )}
                          {/* Show register types when not loading */}
                          {(!isSlaveLoading(slave.id)) && ["线圈", "离散输入", "输入寄存器", "保持寄存器"].map((registerType) => {
                            const registersOfType = getRegistersByType(connection.id, slave.id, registerType)
                            const isSelectedRegisterType =
                              selectedConnectionId === connection.id &&
                              selectedSlaveId === slave.id &&
                              selectedRegisterType === registerType

                            return (
                              <div
                                key={registerType}
                                className={cn(
                                  "flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
                                  registersOfType.length === 0 && "opacity-80",
                                  isSelectedRegisterType && "bg-primary/10 border border-primary/20",
                                )}
                                onClick={() => {
                                  openRegisterTab(connection.id, slave.id, slave.name, connection.name, registerType)
                                }}
                              >
                                <div className="w-4" />
                                <MemoryStick
                                  className={cn(
                                    "w-4 h-4 flex-shrink-0",
                                    isSelectedRegisterType ? "text-primary" : "text-emerald-600",
                                  )}
                                />
                                <div className="min-w-0 flex-1">
                                  <div
                                    className={cn(
                                      "text-sm font-medium truncate",
                                      isSelectedRegisterType && "text-primary font-semibold",
                                    )}
                                  >
                                    {registerType}
                                  </div>
                                  <div className="text-xs text-muted-foreground">{registersOfType.length} 个寄存器组</div>
                                </div>
                                <div
                                  className={cn(
                                    "px-2 py-0.5 text-[10px] rounded-full border",
                                    registersOfType.length > 0
                                      ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                                      : "bg-slate-50 text-slate-500 border-slate-200",
                                  )}
                                >
                                  {registersOfType.length}
                                </div>
                                {registersOfType.length > 0 && (
                                  <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                      <Button
                                        variant="ghost"
                                        size="sm"
                                        className="h-6 w-6 p-0 flex-shrink-0"
                                        aria-haspopup="menu"
                                        aria-label="寄存器操作"
                                        data-testid={`register-actions-${registerType}`}
                                        onClick={(e) => e.stopPropagation()}
                                      >
                                        <MoreVertical className="w-3 h-3" />
                                      </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end">
                                      <DropdownMenuItem
                                        onClick={() => {
                                          // Find first register of this type for editing
                                          const firstRegister = registersOfType[0]
                                          if (firstRegister) {
                                            setEditingRegister({
                                              connectionId: connection.id,
                                              slaveId: slave.id,
                                              registerName: registerType,
                                              registerData: firstRegister,
                                            })
                                          }
                                        }}
                                      >
                                        编辑寄存器
                                      </DropdownMenuItem>
                                      <DropdownMenuItem
                                        onClick={() => {
                                          if (confirm(`确定要删除所有${registerType}吗？`)) {
                                            registersOfType.forEach((register) => {
                                              deleteRegister(connection.id, slave.id, register.id)
                                            })
                                          }
                                        }}
                                        className="text-red-600"
                                      >
                                        删除寄存器
                                      </DropdownMenuItem>
                                    </DropdownMenuContent>
                                  </DropdownMenu>
                                )}
                              </div>
                            )
                          })}
                        </div>
                      )}
                    </div>
                  )
                })}
            </div>
          ))}
        </div>
      </Card>
    </TooltipProvider>
  )
}
