"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { useConnections } from "@/hooks/useConnections"
import type { Register } from "@/types"
import { Trash2, X, Check } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"

interface RegisterConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  connectionId?: string | null
  slaveId?: string | null
  editMode?: boolean
  existingGroup?: Register | null
}

export function RegisterConfigDialog({
  open,
  onOpenChange,
  connectionId,
  slaveId,
  editMode = false,
  existingGroup,
}: RegisterConfigDialogProps) {
  const { addRegister, updateRegister, deleteRegister, getAllRegisters } = useConnections()
  const [selectedType, setSelectedType] = useState<string>("")
  const [offsetAddress, setOffsetAddress] = useState<number>(1)
  const [offsetHex, setOffsetHex] = useState<string>("1")
  const [hexData, setHexData] = useState<string>("0000")
  const [names, setNames] = useState<string>("")
  const [coefficients, setCoefficients] = useState<string>("")
  const [hexError, setHexError] = useState<string | null>(null)
  const [offsetError, setOffsetError] = useState<string | null>(null)
  const [addressDuplicateError, setAddressDuplicateError] = useState<string | null>(null)

  // 寄存器类型定义
  const registerTypes = [
    {
      id: "holding-registers", 
      name: "保持寄存器",
      englishName: "Holding Registers",
      baseAddress: 40000,
      addressRange: "40001-49999",
      readCodes: ["03"],
      writeCodes: ["06", "16"]
    },
    {
      id: "input-registers",
      name: "输入寄存器",
      englishName: "Input Registers",
      baseAddress: 30000,
      addressRange: "30001-39999", 
      readCodes: ["04"],
      writeCodes: []
    },
    {
      id: "coils",
      name: "线圈",
      englishName: "Coils",
      baseAddress: 0,
      addressRange: "1-9999",
      readCodes: ["01"],
      writeCodes: ["05", "15"]
    },
    {
      id: "discrete-inputs",
      name: "离散输入",
      englishName: "Discrete Inputs", 
      baseAddress: 10000,
      addressRange: "10001-19999",
      readCodes: ["02"],
      writeCodes: []
    }
  ]

  // 16进制验证函数
  const validateHexData = (hexData: string): string | null => {
    if (!hexData.trim()) {
      return "16进制数据不能为空"
    }
    
    // 移除空格和0x前缀
    const cleaned = hexData.trim().replace(/\s/g, "").replace(/^0x/i, "")
    
    // 检查是否只包含有效的16进制字符
    const hexPattern = /^[0-9A-Fa-f]*$/
    if (!hexPattern.test(cleaned)) {
      return "只能包含有效的16进制字符 (0-9, A-F)"
    }
    
    // 检查长度是否为偶数
    if (cleaned.length % 2 !== 0) {
      return "16进制数据长度必须为偶数"
    }
    
    return null
  }

  // 验证地址是否重复
  const validateAddressDuplicate = (finalAddress: number): string | null => {
    if (!connectionId || !slaveId) return null
    
    // 获取当前从机的所有寄存器
    const existingRegisters = getAllRegisters(connectionId, slaveId)
    
    // 检查是否有重复的起始地址
    const duplicateRegister = existingRegisters.find(register => {
      // 如果是编辑模式，跳过当前正在编辑的寄存器
      if (editMode && existingGroup && register.id === existingGroup.id) {
        return false
      }
      return register.startaddr === finalAddress
    })
    
    if (duplicateRegister) {
      return `起始地址 ${finalAddress} 已被其他寄存器组使用`
    }
    
    return null
  }

  // 偏移地址验证函数
  const validateOffsetAddress = (offset: number): string | null => {
    if (offset < 0 || offset > 9998) {
      return "起始地址必须在0-9998范围内"
    }
    return null
  }

  // 16进制地址验证函数
  const validateHexAddress = (hexStr: string): string | null => {
    if (!hexStr.trim()) {
      return "16进制地址不能为空"
    }
    
    // 移除空格，不区分大小写
    const cleaned = hexStr.trim().replace(/\s/g, "").toLowerCase()
    
    // 检查是否只包含有效的16进制字符
    const hexPattern = /^[0-9a-f]+$/
    if (!hexPattern.test(cleaned)) {
      return "只能包含有效的16进制字符 (0-9, A-F)"
    }
    
    // 转换为10进制检查范围
    const decValue = parseInt(cleaned, 16)
    if (decValue < 0 || decValue > 9998) {
      return "地址值必须在0-9998范围内"
    }
    
    return null
  }

  // 10进制转16进制
  const decToHex = (dec: number): string => {
    return dec.toString(16).toUpperCase()
  }

  // 16进制转10进制（支持空格，不区分大小写）
  const hexToDec = (hex: string): number => {
    const cleaned = hex.trim().replace(/\s/g, "").toLowerCase()
    return parseInt(cleaned, 16) || 0
  }

  // 根据地址获取寄存器类型
  const getRegisterTypeByAddress = (address: number): string => {
    for (const type of registerTypes) {
      const minAddr = type.baseAddress + 1
      const maxAddr = type.baseAddress + 9999
      if (address >= minAddr && address <= maxAddr) {
        return type.id
      }
    }
    return "holding-registers" // 默认类型
  }

  // 计算最终地址
  const calculateFinalAddress = (typeId: string, offset: number): number => {
    const type = registerTypes.find(t => t.id === typeId)
    return type ? type.baseAddress + offset + 1 : offset + 1
  }

  // 计算偏移地址
  const calculateOffsetAddress = (address: number): number => {
    for (const type of registerTypes) {
      const minAddr = type.baseAddress + 1
      const maxAddr = type.baseAddress + 9999
      if (address >= minAddr && address <= maxAddr) {
        return address - type.baseAddress - 1
      }
    }
    return 0 // 默认偏移
  }

  // 处理16进制数据输入
  const handleHexDataChange = (value: string) => {
    setHexData(value)
    const error = validateHexData(value)
    setHexError(error)
  }

  // 处理10进制地址输入
  const handleDecAddressChange = (value: number) => {
    setOffsetAddress(value)
    setOffsetHex(decToHex(value))
    const error = validateOffsetAddress(value)
    setOffsetError(error)
    
    // 检查地址重复
    if (!error && selectedType) {
      const finalAddress = calculateFinalAddress(selectedType, value)
      const duplicateError = validateAddressDuplicate(finalAddress)
      setAddressDuplicateError(duplicateError)
    } else {
      setAddressDuplicateError(null)
    }
  }

  // 处理16进制地址输入
  const handleHexAddressChange = (value: string) => {
    setOffsetHex(value)
    const hexError = validateHexAddress(value)
    
    if (!hexError) {
      const decValue = hexToDec(value)
      setOffsetAddress(decValue)
      const decError = validateOffsetAddress(decValue)
      setOffsetError(decError)
      
      // 检查地址重复
      if (!decError && selectedType) {
        const finalAddress = calculateFinalAddress(selectedType, decValue)
        const duplicateError = validateAddressDuplicate(finalAddress)
        setAddressDuplicateError(duplicateError)
      } else {
        setAddressDuplicateError(null)
      }
    } else {
      setOffsetError(hexError)
      setAddressDuplicateError(null)
    }
  }

  useEffect(() => {
    if (open) {
      if (editMode && existingGroup) {
        // 编辑模式：根据现有数据计算类型和偏移
        const typeId = getRegisterTypeByAddress(existingGroup.startaddr)
        const offset = calculateOffsetAddress(existingGroup.startaddr)
        
        setSelectedType(typeId)
        setOffsetAddress(offset)
        setOffsetHex(decToHex(offset))
        setHexData(existingGroup.hexdata || "0000")
        setNames(existingGroup.names || "")
        setCoefficients(existingGroup.coefficients || "")
      } else {
        // 新建模式：设置默认值
        setSelectedType("holding-registers")
        setOffsetAddress(0)
        setOffsetHex("0")
        setHexData("0000")
        setNames("")
        setCoefficients("")
      }
      // 清除所有错误状态
      setHexError(null)
      setOffsetError(null)
      setAddressDuplicateError(null)
    }
  }, [open, editMode, existingGroup])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!connectionId || !slaveId) return

    // 验证必填字段
    if (!selectedType) {
      alert("请选择寄存器类型")
      return
    }

    // 验证16进制数据
    const hexValidationError = validateHexData(hexData)
    if (hexValidationError) {
      setHexError(hexValidationError)
      return
    }

    // 验证偏移地址
    const offsetValidationError = validateOffsetAddress(offsetAddress)
    if (offsetValidationError) {
      setOffsetError(offsetValidationError)
      return
    }

    // 计算最终地址
    const finalAddress = calculateFinalAddress(selectedType, offsetAddress)

    // 验证地址重复
    const duplicateValidationError = validateAddressDuplicate(finalAddress)
    if (duplicateValidationError) {
      setAddressDuplicateError(duplicateValidationError)
      return
    }

    if (editMode && existingGroup) {
      updateRegister(connectionId, slaveId, existingGroup.id, {
        startaddr: finalAddress,
        hexdata: hexData,
        names: names,
        coefficients: coefficients,
      })
    } else {
      addRegister(connectionId, slaveId, {
        startaddr: finalAddress,
        hexdata: hexData,
        names: names,
        coefficients: coefficients,
      })
    }

    onOpenChange(false)
  }

  const handleDelete = () => {
    if (connectionId && slaveId && existingGroup) {
      if (confirm('确定要删除这个寄存器组吗？此操作无法撤销。')) {
        deleteRegister(connectionId, slaveId, existingGroup.id)
        onOpenChange(false)
      }
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{editMode ? "编辑寄存器" : "新增寄存器"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* 寄存器类型选择 */}
          <div className="space-y-3">
            <div className="grid grid-cols-4 gap-2 max-w-2xl">
              {registerTypes.map((type) => (
                <Card
                  key={type.id}
                  className={`cursor-pointer transition-all duration-200 border-2 min-h-16 ${
                    selectedType === type.id
                      ? "border-blue-500 bg-blue-50 shadow-md"
                      : "border-gray-200 hover:border-gray-300 hover:shadow-sm"
                  }`}
                  onClick={() => {
                    setSelectedType(type.id)
                    // 寄存器类型变化时重新检查地址重复
                    const finalAddress = calculateFinalAddress(type.id, offsetAddress)
                    const duplicateError = validateAddressDuplicate(finalAddress)
                    setAddressDuplicateError(duplicateError)
                  }}
                >
                  <CardContent className="p-2 h-full">
                    <div className="flex flex-col justify-between h-full">
                      <div className="text-center">
                        <div className="font-bold text-sm">
                          {type.name}
                        </div>
                        <div className="text-xs text-gray-500">
                          {type.englishName}
                        </div>
                      </div>
                      <div className="flex flex-wrap gap-0.5 justify-center mt-2">
                        {type.readCodes.map((code) => (
                          <Badge
                            key={code}
                            variant="secondary"
                            className="text-xs px-1 py-0 bg-blue-100 text-blue-700 border-blue-200 h-4"
                          >
                            读:{code}
                          </Badge>
                        ))}
                        {type.writeCodes.map((code) => (
                          <Badge
                            key={code}
                            variant="secondary"
                            className="text-xs px-1 py-0 bg-red-100 text-red-700 border-red-200 h-4"
                          >
                            写:{code}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>

          {/* 起始地址 */}
          <div className="space-y-2">
            <div className="text-sm font-medium text-gray-700">
              起始地址(从0开始)
            </div>
            <div className="flex gap-2">
              <div className="flex-1">
                <div className="relative">
                  <span className="absolute left-3 top-1/2 transform -translate-y-1/2 text-sm text-gray-500 pointer-events-none">
                    10进制:
                  </span>
                  <Input
                    type="number"
                    value={offsetAddress}
                    onChange={(e) => handleDecAddressChange(Number.parseInt(e.target.value) || 0)}
                    min={0}
                    max={9998}
                    className={`pl-36 ${offsetError || addressDuplicateError ? "border-red-500" : ""}`}
                    required
                  />
                </div>
              </div>
              <div className="flex-1">
                <div className="relative">
                  <span className="absolute left-3 top-1/2 transform -translate-y-1/2 text-sm text-gray-500 pointer-events-none">
                    16进制:
                  </span>
                  <Input
                    type="text"
                    value={offsetHex}
                    onChange={(e) => handleHexAddressChange(e.target.value)}
                    className={`pl-36 ${offsetError || addressDuplicateError ? "border-red-500" : ""}`}
                    required
                  />
                </div>
              </div>
            </div>
            {offsetError && (
              <p className="text-sm text-red-500 flex items-center gap-1">
                <X className="w-4 h-4" />
                {offsetError}
              </p>
            )}
            {addressDuplicateError && (
              <p className="text-sm text-red-500 flex items-center gap-1">
                <X className="w-4 h-4" />
                {addressDuplicateError}
              </p>
            )}
          </div>

          {/* 16进制数据 */}
          <div className="space-y-2">
            <div className="relative">
              <span className="absolute left-3 top-3 text-sm text-gray-500 pointer-events-none z-10">
                16进制数据:
              </span>
              <Textarea
                id="hexdata"
                value={hexData}
                onChange={(e) => handleHexDataChange(e.target.value)}
                className={`min-h-24 pl-24 pt-3 ${hexError ? "border-red-500" : ""}`}
                rows={3}
                required
              />
            </div>
            {hexError && (
              <p className="text-sm text-red-500 flex items-center gap-1">
                <X className="w-4 h-4" />
                {hexError}
              </p>
            )}
          </div>

          {/* 寄存器名称 */}
          <div className="space-y-2">
            <div className="relative">
              <span className="absolute left-3 top-3 text-sm text-gray-500 pointer-events-none z-10">
                寄存器名称:
              </span>
              <Textarea
                id="names"
                value={names}
                onChange={(e) => setNames(e.target.value)}
                className="min-h-20 pl-24 pt-3"
                rows={2}
                placeholder="逗号分隔，如：温度,湿度,压力"
              />
            </div>
          </div>

          {/* 显示系数 */}
          <div className="space-y-2">
            <div className="relative">
              <span className="absolute left-3 top-1/2 transform -translate-y-1/2 text-sm text-gray-500 pointer-events-none">
                显示系数:
              </span>
              <Input
                id="coefficients"
                value={coefficients}
                onChange={(e) => setCoefficients(e.target.value)}
                className="pl-20"
                placeholder="逗号分隔，如：0.1,1.0,2.0"
              />
            </div>
          </div>

          <DialogFooter className="flex justify-between">
            <div className="flex-1">
              {editMode && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button type="button" variant="destructive" size="icon" onClick={handleDelete}>
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>删除</p>
                  </TooltipContent>
                </Tooltip>
              )}
            </div>
            <div className="flex gap-2">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button type="button" variant="outline" size="icon" onClick={() => onOpenChange(false)}>
                    <X className="w-4 h-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>取消</p>
                </TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button type="submit" size="icon" disabled={!selectedType || !!hexError || !!offsetError || !!addressDuplicateError}>
                    <Check className="w-4 h-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{editMode ? "保存" : "新增"}</p>
                </TooltipContent>
              </Tooltip>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
