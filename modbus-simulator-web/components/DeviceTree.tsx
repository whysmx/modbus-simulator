"use client"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Globe, Monitor, HardDrive, ChevronDown, ChevronRight, Loader2, Search, X, Plus, Settings } from "lucide-react"
import { useConnections } from "@/hooks/useConnections"
import { cn } from "@/lib/utils"
import { useState, useMemo, useCallback } from "react"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import { Input } from "@/components/ui/input"
import { SearchHighlight } from "@/components/SearchHighlight"

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
    getAllRegisters,
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
  const [searchTerm, setSearchTerm] = useState<string>("")

  // 搜索匹配函数
  const matchesSearch = useCallback((text: string, searchTerm: string): boolean => {
    if (!searchTerm.trim()) return true
    return text.toLowerCase().includes(searchTerm.toLowerCase().trim())
  }, [])

  // 过滤连接和从机
  const filteredConnections = useMemo(() => {
    if (!searchTerm.trim()) return connections
    
    return connections.filter(connection => {
      // 检查连接名称和端口号是否匹配
      const connectionMatches = 
        matchesSearch(connection.name, searchTerm) ||
        matchesSearch(connection.port.toString(), searchTerm)
      
      // 检查从机是否匹配
      const slaveMatches = connection.slaves.some(slave => 
        matchesSearch(slave.name, searchTerm) ||
        matchesSearch(slave.slaveid.toString(), searchTerm)
      )
      
      return connectionMatches || slaveMatches
    })
  }, [connections, searchTerm, matchesSearch])

  // 为有匹配的连接过滤从机
  const getFilteredSlaves = useCallback((connection: any) => {
    if (!searchTerm.trim()) return connection.slaves
    
    return connection.slaves.filter(slave => 
      matchesSearch(slave.name, searchTerm) ||
      matchesSearch(slave.slaveid.toString(), searchTerm) ||
      matchesSearch(connection.name, searchTerm) ||
      matchesSearch(connection.port.toString(), searchTerm)
    )
  }, [searchTerm, matchesSearch])

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
                    <Globe className="w-5 h-5 text-blue-600" />
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
        {/* 搜索输入框 */}
        <div className="p-3 border-b">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="搜索连接、从机、端口..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 pr-10"
            />
            {searchTerm && (
              <Button
                variant="ghost"
                size="sm"
                className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0"
                onClick={() => setSearchTerm("")}
              >
                <X className="w-3 h-3" />
              </Button>
            )}
          </div>
        </div>
        <div className="p-2 space-y-1">
          {filteredConnections.length === 0 && searchTerm && (
            <div className="p-4 text-center text-muted-foreground">
              <p>未找到匹配的设备</p>
              <p className="text-xs mt-1">请尝试修改搜索关键词</p>
            </div>
          )}
          {connections.length === 0 && !searchTerm ? (
            /* 无连接时显示新增按钮 */
            <div className="p-2 flex justify-end">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-gray-500 hover:text-blue-600 hover:bg-blue-50"
                    onClick={() => setEditingDevice("new-connection")}
                  >
                    <Plus className="w-4 h-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>新建连接</p>
                </TooltipContent>
              </Tooltip>
            </div>
          ) : (
            /* 有连接时显示连接列表 */
            filteredConnections.map((connection) => {
            const filteredSlaves = getFilteredSlaves(connection)
            
            return (
              <div key={connection.id}>
                {/* Level 1: Connection */}
                <div
                  className={cn(
                    "group flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
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
                    className="h-4 w-4 p-0 hover:bg-accent/50 transition-all duration-150"
                    onClick={(e) => {
                      e.stopPropagation()
                      toggleConnectionExpansion(connection.id)
                    }}
                  >
                    {expandedConnections.has(connection.id) ? (
                      <ChevronDown className="w-3 h-3 transition-transform duration-200" />
                    ) : (
                      <ChevronRight className="w-3 h-3 transition-transform duration-200" />
                    )}
                  </Button>
                  <Globe className="w-4 h-4 text-blue-600 flex-shrink-0 transition-colors duration-150 group-hover:text-blue-700" />
                  <div className="min-w-0 flex-1">
                    <div className="text-sm font-medium truncate">
                      <SearchHighlight text={connection.name} searchTerm={searchTerm} />
                    </div>
                    <div className="text-xs text-muted-foreground">
                      端口: <SearchHighlight text={connection.port.toString()} searchTerm={searchTerm} />
                    </div>
                  </div>
                  <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all duration-200 ease-in-out">
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                          onClick={(e) => {
                            e.stopPropagation()
                            setEditingDevice(connection.id)
                          }}
                        >
                          <Settings className="w-3 h-3" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>
                        <p>编辑连接</p>
                      </TooltipContent>
                    </Tooltip>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                          onClick={(e) => {
                            e.stopPropagation()
                            setEditingDevice("new-connection")
                          }}
                        >
                          <Plus className="w-3 h-3" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>
                        <p>新建连接</p>
                      </TooltipContent>
                    </Tooltip>
                  </div>
                </div>

                {/* Level 2: Slaves (only show if connection is expanded) */}
                {expandedConnections.has(connection.id) && (
                  <div className="ml-6">
                    {filteredSlaves.length === 0 ? (
                      /* 无从机时显示新增按钮 */
                      <div className="p-2 flex justify-end">
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-gray-500 hover:text-blue-600 hover:bg-blue-50"
                              onClick={() => setEditingSlave({ connectionId: connection.id, slaveId: null })}
                            >
                              <Plus className="w-4 h-4" />
                            </Button>
                          </TooltipTrigger>
                          <TooltipContent>
                            <p>添加从机</p>
                          </TooltipContent>
                        </Tooltip>
                      </div>
                    ) : (
                      /* 有从机时显示从机列表 */
                      filteredSlaves.map((slave) => {
                    const slaveKey = `${connection.id}-${slave.id}`
                    return (
                      <div key={slaveKey}>
                        <div
                          className={cn(
                            "group flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
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
                            className="h-4 w-4 p-0 hover:bg-accent/50 transition-all duration-150"
                            onClick={async (e) => {
                              e.stopPropagation()
                              await toggleSlave(slaveKey, connection.id, slave.id)
                            }}
                          >
                            {expandedSlaves.has(slaveKey) ? (
                              <ChevronDown className="w-3 h-3 transition-transform duration-200" />
                            ) : (
                              <ChevronRight className="w-3 h-3 transition-transform duration-200" />
                            )}
                          </Button>
                          <Monitor className="w-4 h-4 text-purple-600 flex-shrink-0 transition-colors duration-150 group-hover:text-purple-700" />
                          <div className="min-w-0 flex-1">
                            <div className="text-sm font-medium truncate">
                              <SearchHighlight text={slave.name} searchTerm={searchTerm} />
                            </div>
                            <div className="text-xs text-muted-foreground">
                              从机地址: <SearchHighlight text={slave.slaveid.toString()} searchTerm={searchTerm} />
                            </div>
                          </div>
                          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all duration-200 ease-in-out">
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    setEditingSlave({ connectionId: connection.id, slaveId: slave.id })
                                  }}
                                >
                                  <Settings className="w-3 h-3" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>
                                <p>编辑从机</p>
                              </TooltipContent>
                            </Tooltip>
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    setEditingSlave({ connectionId: connection.id, slaveId: null })
                                  }}
                                >
                                  <Plus className="w-3 h-3" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>
                                <p>新增从机</p>
                              </TooltipContent>
                            </Tooltip>
                          </div>
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
                            
                            {/* Register groups */}
                            {(!isSlaveLoading(slave.id)) && (() => {
                              const allRegisters = getAllRegisters(connection.id, slave.id)
                              
                              if (allRegisters.length === 0) {
                                // 无寄存器时显示新增按钮
                                return (
                                  <div className="p-2 flex justify-end">
                                    <Tooltip>
                                      <TooltipTrigger asChild>
                                        <Button
                                          variant="ghost"
                                          size="sm"
                                          className="text-gray-500 hover:text-emerald-600 hover:bg-emerald-50"
                                          onClick={() => setShowRegisterDialog({ connectionId: connection.id, slaveId: slave.id })}
                                        >
                                          <Plus className="w-4 h-4" />
                                        </Button>
                                      </TooltipTrigger>
                                      <TooltipContent>
                                        <p>添加寄存器</p>
                                      </TooltipContent>
                                    </Tooltip>
                                  </div>
                                )
                              }
                              
                              // 有寄存器时显示寄存器组列表
                              return allRegisters.map((register) => {
                                const registerType = classifyRegisterType(register.startaddr)
                                const isSelected = 
                                  selectedConnectionId === connection.id &&
                                  selectedSlaveId === slave.id &&
                                  editingRegister?.registerData?.id === register.id

                                return (
                                  <div
                                    key={register.id}
                                    className={cn(
                                      "group flex items-center gap-2 p-2 rounded-md cursor-pointer hover:bg-accent transition-colors",
                                      isSelected && "bg-primary/10 border border-primary/20",
                                    )}
                                    onClick={() => {
                                      openRegisterTab(connection.id, slave.id, slave.name, connection.name, registerType)
                                    }}
                                  >
                                    <div className="w-4" />
                                    <HardDrive
                                      className={cn(
                                        "w-4 h-4 flex-shrink-0 transition-all duration-150",
                                        isSelected 
                                          ? "text-primary group-hover:text-primary/80" 
                                          : "text-emerald-600 group-hover:text-emerald-700",
                                      )}
                                    />
                                    <div className="min-w-0 flex-1">
                                      <div
                                        className={cn(
                                          "text-sm font-medium truncate",
                                          isSelected && "text-primary font-semibold",
                                        )}
                                      >
                                        {registerType} (地址{register.startaddr})
                                      </div>
                                      <div className="flex flex-wrap gap-1 mt-1">
                                        {(() => {
                                          // 根据寄存器类型显示功能码标签
                                          const typeMap: Record<string, { read: string[], write: string[] }> = {
                                            "线圈": { read: ["01"], write: ["05", "15"] },
                                            "离散输入": { read: ["02"], write: [] },
                                            "输入寄存器": { read: ["04"], write: [] },
                                            "保持寄存器": { read: ["03"], write: ["06", "16"] }
                                          }
                                          const codes = typeMap[registerType] || { read: [], write: [] }
                                          return (
                                            <>
                                              {codes.read.map(code => (
                                                <span key={`read-${code}`} className="inline-flex items-center px-1.5 py-0.5 text-[10px] font-medium rounded-full bg-blue-100 text-blue-700 border border-blue-200">
                                                  {code}
                                                </span>
                                              ))}
                                              {codes.write.map(code => (
                                                <span key={`write-${code}`} className="inline-flex items-center px-1.5 py-0.5 text-[10px] font-medium rounded-full bg-red-100 text-red-700 border border-red-200">
                                                  {code}
                                                </span>
                                              ))}
                                            </>
                                          )
                                        })()}
                                      </div>
                                    </div>
                                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all duration-200 ease-in-out">
                                      <Tooltip>
                                        <TooltipTrigger asChild>
                                          <Button
                                            variant="ghost"
                                            size="sm"
                                            className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                                            onClick={(e) => {
                                              e.stopPropagation()
                                              setEditingRegister({
                                                connectionId: connection.id,
                                                slaveId: slave.id,
                                                registerName: registerType,
                                                registerData: register,
                                              })
                                            }}
                                          >
                                            <Settings className="w-3 h-3" />
                                          </Button>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                          <p>编辑寄存器</p>
                                        </TooltipContent>
                                      </Tooltip>
                                      <Tooltip>
                                        <TooltipTrigger asChild>
                                          <Button
                                            variant="ghost"
                                            size="sm"
                                            className="h-6 w-6 p-0 hover:scale-110 transition-all duration-150"
                                            onClick={(e) => {
                                              e.stopPropagation()
                                              setShowRegisterDialog({ connectionId: connection.id, slaveId: slave.id })
                                            }}
                                          >
                                            <Plus className="w-3 h-3" />
                                          </Button>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                          <p>新增寄存器</p>
                                        </TooltipContent>
                                      </Tooltip>
                                    </div>
                                  </div>
                                )
                              })
                            })()}
                          </div>
                        )}
                      </div>
                    )
                  })
                    )}
                  </div>
                )}
              </div>
            )
          })
          )}
        </div>
      </Card>
    </TooltipProvider>
  )
}