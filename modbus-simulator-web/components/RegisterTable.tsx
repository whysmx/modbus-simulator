"use client"

import { Card } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { X } from "lucide-react"
import { useConnections } from "@/hooks/useConnections"
import { useState } from "react"
import { cn } from "@/lib/utils"

interface ParsedRegister {
  address: number
  hexValue: string
  binaryValue: string
  int16Value: number
  uint16Value: number
  floatABCD: number | string
  floatCDAB: number | string
  floatBADC: number | string
  floatDCBA: number | string
  stringValue: string
  name: string
  coefficient: string
  isModified: boolean
  originalHex: string
}

interface BitRegister {
  address: number
  byteHex: string
  bitValue: boolean
  binaryByte: string
  isModified: boolean
  originalBit: boolean
}

export function RegisterTable() {
  const { connections, openTabs, activeTabIndex, setActiveTab, closeTab, hasUnsavedChanges, markAsChanged } =
    useConnections()
  const [editingCell, setEditingCell] = useState<{ address: number; field: string } | null>(null)
  const [modifiedData, setModifiedData] = useState<Map<number, any>>(new Map())

  const activeTab = activeTabIndex >= 0 ? openTabs[activeTabIndex] : null
  const selectedConnection = activeTab ? connections.find((c) => c.id === activeTab.connectionId) : null
  const selectedSlave =
    activeTab && selectedConnection ? selectedConnection.slaves.find((s) => s.id === activeTab.slaveId) : null

  const registers = selectedSlave?.registers
    ? parseRegistersByType(selectedSlave.registers, activeTab.registerType)
    : []

  function parseRegistersByType(apiRegisters: any[], registerType: string): ParsedRegister[] | BitRegister[] {
    const relevantRegisters = apiRegisters.filter((reg) => {
      const startAddr = reg.startaddr || 0
      switch (registerType) {
        case "线圈":
          return startAddr >= 1 && startAddr <= 9999
        case "离散输入":
          return startAddr >= 10001 && startAddr <= 19999
        case "输入寄存器":
          return startAddr >= 30001 && startAddr <= 39999
        case "保持寄存器":
          return startAddr >= 40001 && startAddr <= 49999
        default:
          return false
      }
    })

    if (registerType === "线圈" || registerType === "离散输入") {
      return parseBitRegisters(relevantRegisters)
    } else {
      return parseWordRegisters(relevantRegisters)
    }
  }

  function parseBitRegisters(apiRegisters: any[]): BitRegister[] {
    const bitRegisters: BitRegister[] = []

    apiRegisters.forEach((apiReg) => {
      if (!apiReg.hexdata) return

      // 显示时去掉空格，转大写
      const hexData = apiReg.hexdata.toString().replace(/\s/g, '').toUpperCase()
      const startAddr = apiReg.startaddr || 0

      // 位寄存器：每个字节包含8个位，每字节用2个十六进制字符表示
      const charsPerByte = 2
      
      // 按字节拆分十六进制数据
      for (let i = 0; i < hexData.length; i += charsPerByte) {
        const byteHex = hexData.substring(i, i + charsPerByte)
        if (byteHex.length < charsPerByte) break // 不完整的字节数据跳过

        const byteValue = Number.parseInt(byteHex, 16) || 0
        const binaryByte = byteValue.toString(2).padStart(8, "0")
        
        // 为这个字节中的每一位创建一个位寄存器项
        for (let bitIndex = 0; bitIndex < 8; bitIndex++) {
          const address = startAddr + (Math.floor(i / charsPerByte) * 8) + bitIndex
          const bitValue = ((byteValue >> (7 - bitIndex)) & 1) === 1
          
          bitRegisters.push({
            address,
            byteHex: byteHex,
            bitValue,
            binaryByte,
            isModified: false,
            originalBit: bitValue,
          })
        }
      }
    })

    return bitRegisters.sort((a, b) => a.address - b.address)
  }

  function parseWordRegisters(apiRegisters: any[]): ParsedRegister[] {
    const wordRegisters: ParsedRegister[] = []

    apiRegisters.forEach((apiReg) => {
      if (!apiReg.hexdata) return

      // 显示时去掉空格，转大写
      const hexData = apiReg.hexdata.toString().replace(/\s/g, '').toUpperCase()
      const startAddr = apiReg.startaddr || 0

      // 根据寄存器类型确定每个寄存器的字长（字节数）
      // 字寄存器类型：每个寄存器占用2字节（4个十六进制字符）
      const bytesPerRegister = 2 
      const charsPerRegister = bytesPerRegister * 2 // 每字节2个十六进制字符

      // 按寄存器拆分十六进制数据
      for (let i = 0; i < hexData.length; i += charsPerRegister) {
        const registerHex = hexData.substring(i, i + charsPerRegister)
        if (registerHex.length < charsPerRegister) break // 不完整的寄存器数据跳过

        const address = startAddr + Math.floor(i / charsPerRegister)
        const uint16Value = Number.parseInt(registerHex, 16) || 0
        const int16Value = uint16Value > 32767 ? uint16Value - 65536 : uint16Value
        const binaryValue = uint16Value.toString(2).padStart(16, "0")

        // String conversion (2 bytes to 2 ASCII chars)
        const char1 = String.fromCharCode((uint16Value >> 8) & 0xff)
        const char2 = String.fromCharCode(uint16Value & 0xff)
        const stringValue = (char1 + char2).replace(/[\x00-\x1F\x7F]/g, ".")

        // 解析名称和系数
        const names = (apiReg.names || "").split(",")
        const coefficients = (apiReg.coefficients || "").split(",")
        const registerIndex = Math.floor(i / charsPerRegister)
        const name = names[registerIndex]?.trim() || ""
        const coefficient = coefficients[registerIndex]?.trim() || "1"

        wordRegisters.push({
          address,
          hexValue: registerHex, // 显示单个寄存器的16进制值
          binaryValue,
          int16Value,
          uint16Value,
          floatABCD: "NA", // Will be calculated for pairs
          floatCDAB: "NA",
          floatBADC: "NA",
          floatDCBA: "NA",
          stringValue,
          name,
          coefficient,
          isModified: false,
          originalHex: registerHex,
        })
      }
    })

    // Sort registers by address first
    const sortedRegisters = wordRegisters.sort((a, b) => a.address - b.address)

    // Calculate float values for consecutive register pairs
    for (let i = 0; i < sortedRegisters.length - 1; i++) {
      const currentReg = sortedRegisters[i]
      const nextReg = sortedRegisters[i + 1]
      
      // Check if registers are consecutive
      if (nextReg.address === currentReg.address + 1) {
        const highWord = currentReg.uint16Value
        const lowWord = nextReg.uint16Value
        
        // Calculate different float formats
        currentReg.floatABCD = convertToFloat(highWord, lowWord)
        currentReg.floatCDAB = convertToFloat(lowWord, highWord)
        currentReg.floatBADC = convertToFloat(
          ((highWord & 0xFF) << 8) | ((highWord >> 8) & 0xFF),
          ((lowWord & 0xFF) << 8) | ((lowWord >> 8) & 0xFF)
        )
        currentReg.floatDCBA = convertToFloat(
          ((lowWord & 0xFF) << 8) | ((lowWord >> 8) & 0xFF),
          ((highWord & 0xFF) << 8) | ((highWord >> 8) & 0xFF)
        )
      }
    }

    return sortedRegisters
  }

  function convertToFloat(highWord: number, lowWord: number): number | string {
    try {
      const buffer = new ArrayBuffer(4)
      const view = new DataView(buffer)
      view.setUint16(0, highWord, false) // big endian
      view.setUint16(2, lowWord, false)
      const floatValue = view.getFloat32(0, false)

      if (isNaN(floatValue) || !isFinite(floatValue)) return "NA"

      const decimalPlaces = floatValue.toString().split(".")[1]?.length || 0
      if (
        decimalPlaces > 6 ||
        Math.abs(floatValue) >= 1000000 ||
        (Math.abs(floatValue) < 0.000001 && floatValue !== 0)
      ) {
        return Number.parseFloat(floatValue.toExponential(3))
      } else {
        return Number.parseFloat(floatValue.toFixed(Math.min(decimalPlaces, 6)))
      }
    } catch {
      return "NA"
    }
  }

  function formatInteger(value: number): string {
    if (Math.abs(value) >= 1000000) {
      return value.toExponential(3)
    }
    return value.toString()
  }

  function formatNumber(value: number | string): string {
    if (typeof value === "string") return value
    if (Math.abs(value) >= 1000000) {
      return value.toExponential(3)
    }
    return value.toString()
  }

  function formatCoefficient(coefficient: string): string {
    if (!coefficient || coefficient === "") return "1"
    const num = parseFloat(coefficient)
    if (isNaN(num)) return coefficient
    // 如果是整数，不显示小数点
    return num % 1 === 0 ? num.toString() : coefficient
  }

  const handleCellEdit = (address: number, field: string, value: string) => {
    if (!activeTab) return

    const currentData = modifiedData.get(address) || registers.find((r) => r.address === address)
    if (!currentData) return

    let hasChanged = false
    const originalValue = currentData.originalHex || currentData.hexValue

    if (activeTab.registerType === "线圈" || activeTab.registerType === "离散输入") {
      // Bit register editing
      if (field === "bitValue") {
        const newBitValue = value === "true" || value === "1"
        hasChanged = newBitValue !== currentData.originalBit
      } else if (field === "binaryByte") {
        const binaryStr = value.padStart(8, "0").slice(-8)
        hasChanged = binaryStr !== currentData.binaryByte
      }
    } else {
      // Word register editing - check if hex value changed
      let newHexValue = ""

      switch (field) {
        case "hexValue":
          newHexValue = value.replace(/^0x/i, "").toUpperCase()
          break
        case "binaryValue":
          const binVal = value.padStart(16, "0").slice(-16)
          const uint16FromBin = Number.parseInt(binVal, 2) || 0
          newHexValue = uint16FromBin.toString(16).toUpperCase()
          break
        case "int16Value":
          const int16 = Number.parseInt(value) || 0
          const uint16FromInt16 = int16 < 0 ? int16 + 65536 : int16
          newHexValue = uint16FromInt16.toString(16).toUpperCase()
          break
        case "uint16Value":
          const uint16FromInput = Number.parseInt(value) || 0
          newHexValue = uint16FromInput.toString(16).toUpperCase()
          break
        case "floatABCD":
        case "floatCDAB":
        case "floatBADC":
        case "floatDCBA":
          const floatVal = Number.parseFloat(value) || 0
          const floatBuffer = new ArrayBuffer(4)
          const floatView = new DataView(floatBuffer)
          floatView.setFloat32(0, floatVal, false)

          let word1: number
          switch (field) {
            case "floatABCD":
              word1 = floatView.getUint16(0, false)
              break
            case "floatCDAB":
              word1 = floatView.getUint16(2, false)
              break
            case "floatBADC":
              word1 = floatView.getUint16(0, false)
              word1 = ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff)
              break
            case "floatDCBA":
              word1 = floatView.getUint16(2, false)
              word1 = ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff)
              break
            default:
              word1 = 0
          }
          newHexValue = word1.toString(16).toUpperCase().padStart(4, "0")
          break
        case "stringValue":
          if (value.length >= 2) {
            const char1 = value.charCodeAt(0) || 0
            const char2 = value.charCodeAt(1) || 0
            const combinedValue = (char1 << 8) | char2
            newHexValue = combinedValue.toString(16).toUpperCase().padStart(4, "0")
          } else {
            newHexValue = originalValue
          }
          break
        default:
          newHexValue = originalValue
      }

      hasChanged = newHexValue !== originalValue
    }

    if (!hasChanged) {
      setEditingCell(null)
      return
    }

    const updates: any = { ...currentData, isModified: true }

    if (activeTab.registerType === "线圈" || activeTab.registerType === "离散输入") {
      // Bit register editing
      if (field === "bitValue") {
        updates.bitValue = value === "true" || value === "1"
        updates.originalBit = currentData.originalBit
      } else if (field === "binaryByte") {
        // Update all 8 bits in the byte
        const binaryStr = value.padStart(8, "0").slice(-8)
        updates.binaryByte = binaryStr
        // This would need to update multiple bit registers in the same byte
      }
    } else {
      // Word register editing with format conversion
      switch (field) {
        case "hexValue":
          const hexVal = value.replace(/^0x/i, "")
          const uint16 = Number.parseInt(hexVal.substring(0, 4), 16) || 0
          updates.hexValue = hexVal // 保存完整的原始16进制字符串
          updates.uint16Value = uint16
          updates.int16Value = uint16 > 32767 ? uint16 - 65536 : uint16
          updates.binaryValue = uint16.toString(2).padStart(16, "0")
          break

        case "binaryValue":
          const binVal = value.padStart(16, "0").slice(-16)
          const uint16FromBin = Number.parseInt(binVal, 2) || 0
          updates.binaryValue = binVal
          updates.uint16Value = uint16FromBin
          updates.int16Value = uint16FromBin > 32767 ? uint16FromBin - 65536 : uint16FromBin
          updates.hexValue = uint16FromBin.toString(16).toUpperCase()
          break

        case "int16Value":
          const int16 = Number.parseInt(value) || 0
          const uint16FromInt16 = int16 < 0 ? int16 + 65536 : int16
          updates.int16Value = int16
          updates.uint16Value = uint16FromInt16
          updates.hexValue = uint16FromInt16.toString(16).toUpperCase()
          updates.binaryValue = uint16FromInt16.toString(2).padStart(16, "0")
          break

        case "uint16Value":
          const uint16FromInput = Number.parseInt(value) || 0
          updates.uint16Value = uint16FromInput
          updates.int16Value = uint16FromInput > 32767 ? uint16FromInput - 65536 : uint16FromInput
          updates.hexValue = uint16FromInput.toString(16).toUpperCase()
          updates.binaryValue = uint16FromInput.toString(2).padStart(16, "0")
          break

        case "floatABCD":
        case "floatCDAB":
        case "floatBADC":
        case "floatDCBA":
          const floatVal = Number.parseFloat(value) || 0
          const floatBuffer = new ArrayBuffer(4)
          const floatView = new DataView(floatBuffer)
          floatView.setFloat32(0, floatVal, false)

          let word1: number, word2: number
          switch (field) {
            case "floatABCD": // AB CD
              word1 = floatView.getUint16(0, false)
              word2 = floatView.getUint16(2, false)
              break
            case "floatCDAB": // CD AB
              word1 = floatView.getUint16(2, false)
              word2 = floatView.getUint16(0, false)
              break
            case "floatBADC": // BA DC
              word1 = floatView.getUint16(0, false)
              word2 = floatView.getUint16(2, false)
              word1 = ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff)
              word2 = ((word2 & 0xff) << 8) | ((word2 >> 8) & 0xff)
              break
            case "floatDCBA": // DC BA
              word1 = floatView.getUint16(2, false)
              word2 = floatView.getUint16(0, false)
              word1 = ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff)
              word2 = ((word2 & 0xff) << 8) | ((word2 >> 8) & 0xff)
              break
            default:
              word1 = word2 = 0
          }

          updates.uint16Value = word1
          updates.int16Value = word1 > 32767 ? word1 - 65536 : word1
          updates.hexValue = word1.toString(16).toUpperCase()
          updates.binaryValue = word1.toString(2).padStart(16, "0")
          break

        case "stringValue":
          updates.stringValue = value
          // Convert string back to hex (simplified)
          if (value.length >= 2) {
            const char1 = value.charCodeAt(0) || 0
            const char2 = value.charCodeAt(1) || 0
            const combinedValue = (char1 << 8) | char2
            updates.uint16Value = combinedValue
            updates.hexValue = combinedValue.toString(16).toUpperCase()
          }
          break
      }

      recalculateFloatValues(address, updates)
    }

    setModifiedData((prev) => new Map(prev.set(address, updates)))
    markAsChanged(`${activeTab.connectionId}:${activeTab.slaveId}:${address}`, updates)
    setEditingCell(null)
  }

  function recalculateFloatValues(address: number, updates: any) {
    const allRegisters = registers as ParsedRegister[]
    const currentIndex = allRegisters.findIndex((r) => r.address === address)

    if (currentIndex === -1) return

    // Find the pair register (either previous or next)
    let pairIndex = -1
    let isPrimaryRegister = false

    if (currentIndex > 0 && allRegisters[currentIndex - 1].address === address - 1) {
      // Current register is the second in pair
      pairIndex = currentIndex - 1
      isPrimaryRegister = false
    } else if (currentIndex < allRegisters.length - 1 && allRegisters[currentIndex + 1].address === address + 1) {
      // Current register is the first in pair
      pairIndex = currentIndex + 1
      isPrimaryRegister = true
    }

    if (pairIndex !== -1) {
      const pairRegister = allRegisters[pairIndex]
      const pairData = modifiedData.get(pairRegister.address) || pairRegister

      let word1: number, word2: number
      if (isPrimaryRegister) {
        word1 = updates.uint16Value
        word2 = pairData.uint16Value
      } else {
        word1 = pairData.uint16Value
        word2 = updates.uint16Value
      }

      // Calculate all float formats
      const floatABCD = convertToFloat(word1, word2)
      const floatCDAB = convertToFloat(word2, word1)
      const floatBADC = convertToFloat(
        ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff),
        ((word2 & 0xff) << 8) | ((word2 >> 8) & 0xff),
      )
      const floatDCBA = convertToFloat(
        ((word2 & 0xff) << 8) | ((word2 >> 8) & 0xff),
        ((word1 & 0xff) << 8) | ((word1 >> 8) & 0xff),
      )

      // Update both registers with new float values
      updates.floatABCD = floatABCD
      updates.floatCDAB = floatCDAB
      updates.floatBADC = floatBADC
      updates.floatDCBA = floatDCBA

      // Update pair register as well
      const pairUpdates = {
        ...pairData,
        floatABCD,
        floatCDAB,
        floatBADC,
        floatDCBA,
        isModified: true,
      }
      setModifiedData((prev) => new Map(prev.set(pairRegister.address, pairUpdates)))
    }
  }

  const EditableCell = ({
    value,
    address,
    field,
    className = "",
    isHexColumn = false, // Added parameter to identify hex column
  }: {
    value: string | number | boolean
    address: number
    field: string
    className?: string
    isHexColumn?: boolean
  }) => {
    const isEditing = editingCell?.address === address && editingCell?.field === field
    const [tempValue, setTempValue] = useState(value.toString())
    const modifiedReg = modifiedData.get(address)
    const isModified = modifiedReg?.isModified || false

    if (isEditing) {
      return (
        <Input
          value={tempValue}
          onChange={(e) => setTempValue(e.target.value)}
          onBlur={() => handleCellEdit(address, field, tempValue)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              handleCellEdit(address, field, tempValue)
            } else if (e.key === "Escape") {
              setEditingCell(null)
              setTempValue(value.toString())
            }
          }}
          className={`h-7 text-xs border-2 border-primary focus:border-primary ${className}`}
          autoFocus
        />
      )
    }

    return (
      <div
        className={cn(
          "h-7 px-3 py-1 text-xs cursor-pointer hover:bg-blue-50 rounded-md transition-all duration-200 border border-transparent",
          isModified && isHexColumn && "bg-amber-50 border-amber-300 text-amber-900 font-medium",
          className,
        )}
        onClick={() => {
          setEditingCell({ address, field })
          setTempValue(value.toString())
        }}
      >
        {value.toString()}
      </div>
    )
  }

  if (openTabs.length === 0) {
    return (
      <Card className="h-full flex items-center justify-center">
        <div className="text-center text-muted-foreground">
          <div className="text-lg mb-2">请选择寄存器类型</div>
          <div className="text-sm">从左侧设备树中点击寄存器类型来打开对应的寄存器表格</div>
        </div>
      </Card>
    )
  }

  const isBitType = activeTab?.registerType === "线圈" || activeTab?.registerType === "离散输入"

  return (
    <Card className="h-full flex flex-col shadow-sm">
      <div className="border-b bg-gradient-to-r from-slate-50 to-slate-100">
        <div className="flex items-center">
          {openTabs.map((tab, index) => (
            <div
              key={`${tab.connectionId}-${tab.slaveId}-${tab.registerType}`}
              className={cn(
                "flex items-center gap-3 px-6 py-3 border-r cursor-pointer transition-all duration-200 min-w-[120px] justify-between",
                "hover:bg-white hover:shadow-sm",
                index === activeTabIndex && "bg-white border-b-2 border-blue-500 shadow-sm",
              )}
              onClick={() => setActiveTab(index)}
            >
              <div className="flex items-center gap-2">
                <div
                  className={cn(
                    "w-2 h-2 rounded-full transition-colors",
                    index === activeTabIndex ? "bg-blue-500" : "bg-slate-400",
                  )}
                ></div>
                <span
                  className={cn("text-xs font-medium", index === activeTabIndex ? "text-blue-700" : "text-slate-600")}
                >
                  Tab {index + 1}
                </span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-5 w-5 p-0 hover:bg-red-100 hover:text-red-600 flex-shrink-0 rounded-full"
                onClick={(e) => {
                  e.stopPropagation()
                  closeTab(index)
                }}
              >
                <X className="w-3 h-3" />
              </Button>
            </div>
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-auto bg-slate-50/30">
        <div className="min-w-max">
          {isBitType ? (
            <table className="w-full text-xs bg-white">
              <thead className="bg-gradient-to-r from-slate-100 to-slate-200 sticky top-0 shadow-sm">
                <tr>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">地址</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">ByteHex</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">Bit</th>
                  <th className="text-center p-3 font-semibold text-slate-700">Binary(8)</th>
                </tr>
              </thead>
              <tbody>
                {(registers as BitRegister[]).map((register, index) => (
                  <tr
                    key={register.address}
                    className={cn(
                      "border-b border-slate-200 hover:bg-blue-50/50 transition-colors",
                      index % 2 === 0 ? "bg-white" : "bg-slate-50/50",
                    )}
                  >
                    <td className="p-3 font-mono text-slate-600 border-r border-slate-200 text-center">
                      {register.address.toString().padStart(5, "0")}
                    </td>
                    <td className="p-3 font-mono text-slate-800 border-r border-slate-200 font-medium text-center">
                      {register.byteHex}
                    </td>
                    <td className="p-3 border-r border-slate-200 text-center">
                      <EditableCell
                        value={register.bitValue ? "1" : "0"}
                        address={register.address}
                        field="bitValue"
                        className="font-mono font-bold text-center"
                      />
                    </td>
                    <td className="p-3 text-center">
                      <EditableCell
                        value={register.binaryByte}
                        address={register.address}
                        field="binaryByte"
                        className="font-mono text-center"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <table className="w-full text-xs bg-white">
              <thead className="bg-gradient-to-r from-slate-100 to-slate-200 sticky top-0 shadow-sm">
                <tr>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">地址</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">Hex</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">名称</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">系数</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">Int16</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">UInt16</th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">
                    Float AB CD
                  </th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">
                    Float CD AB
                  </th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">
                    Float BA DC
                  </th>
                  <th className="text-center p-3 font-semibold text-slate-700 border-r border-slate-300">
                    Float DC BA
                  </th>
                  <th className="text-center p-3 font-semibold text-slate-700">String</th>
                </tr>
              </thead>
              <tbody>
                {(registers as ParsedRegister[]).map((register, index) => {
                  const displayData = modifiedData.get(register.address) || register
                  const isFloatRow = index % 2 === 0 // Show floats on even rows (first of pair)

                  return (
                    <tr
                      key={register.address}
                      className={cn(
                        "border-b border-slate-200 hover:bg-blue-50/50 transition-colors",
                        index % 2 === 0 ? "bg-white" : "bg-slate-50/50",
                      )}
                    >
                      <td className="p-3 font-mono text-slate-600 border-r border-slate-200 font-medium text-center">
                        {register.address.toString().padStart(5, "0")}
                      </td>
                      <td className="p-3 border-r border-slate-200 text-center">
                        <EditableCell
                          value={displayData.hexValue}
                          address={register.address}
                          field="hexValue"
                          className="font-mono font-bold text-blue-700 text-center"
                          isHexColumn={true} // Mark hex column for special styling
                        />
                      </td>
                      <td className="p-3 border-r border-slate-200 text-center">
                        <div className="text-xs text-slate-700 font-medium">
                          {displayData.name || ""}
                        </div>
                      </td>
                      <td className="p-3 border-r border-slate-200 text-center">
                        <div className="text-xs text-slate-600 font-mono">
                          {formatCoefficient(displayData.coefficient)}
                        </div>
                      </td>
                      <td className="p-3 border-r border-slate-200 text-center">
                        <EditableCell
                          value={formatNumber(displayData.int16Value)}
                          address={register.address}
                          field="int16Value"
                          className="font-mono text-center"
                        />
                      </td>
                      <td className="p-3 border-r border-slate-200 text-center">
                        <EditableCell
                          value={formatNumber(displayData.uint16Value)}
                          address={register.address}
                          field="uint16Value"
                          className="font-mono text-center"
                        />
                      </td>
                      <td className={cn("p-3 border-r border-slate-200 text-center", !isFloatRow && "text-slate-400")}>
                        {isFloatRow ? (
                          <EditableCell
                            value={formatNumber(displayData.floatABCD)}
                            address={register.address}
                            field="floatABCD"
                            className="font-mono text-xs font-medium text-purple-700 text-center"
                          />
                        ) : (
                          <div className="font-mono text-xs text-center text-slate-400">
                            {formatNumber(displayData.floatABCD)}
                          </div>
                        )}
                      </td>
                      <td className={cn("p-3 border-r border-slate-200 text-center", !isFloatRow && "text-slate-400")}>
                        {isFloatRow ? (
                          <EditableCell
                            value={formatNumber(displayData.floatCDAB)}
                            address={register.address}
                            field="floatCDAB"
                            className="font-mono text-xs font-medium text-purple-700 text-center"
                          />
                        ) : (
                          <div className="font-mono text-xs text-center text-slate-400">
                            {formatNumber(displayData.floatCDAB)}
                          </div>
                        )}
                      </td>
                      <td className={cn("p-3 border-r border-slate-200 text-center", !isFloatRow && "text-slate-400")}>
                        {isFloatRow ? (
                          <EditableCell
                            value={formatNumber(displayData.floatBADC)}
                            address={register.address}
                            field="floatBADC"
                            className="font-mono text-xs font-medium text-purple-700 text-center"
                          />
                        ) : (
                          <div className="font-mono text-xs text-center text-slate-400">
                            {formatNumber(displayData.floatBADC)}
                          </div>
                        )}
                      </td>
                      <td className={cn("p-3 border-r border-slate-200 text-center", !isFloatRow && "text-slate-400")}>
                        {isFloatRow ? (
                          <EditableCell
                            value={formatNumber(displayData.floatDCBA)}
                            address={register.address}
                            field="floatDCBA"
                            className="font-mono text-xs font-medium text-purple-700 text-center"
                          />
                        ) : (
                          <div className="font-mono text-xs text-center text-slate-400">
                            {formatNumber(displayData.floatDCBA)}
                          </div>
                        )}
                      </td>
                      <td className="p-3 text-center">
                        <EditableCell
                          value={displayData.stringValue}
                          address={register.address}
                          field="stringValue"
                          className="font-mono text-indigo-600 text-center"
                        />
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}

          {registers.length === 0 && (
            <div className="p-12 text-center bg-white rounded-lg m-4 border border-slate-200">
              <div className="text-slate-500 text-sm font-medium">该寄存器类型暂无数据</div>
              <div className="text-slate-400 text-xs mt-2">点击左侧设备树的"新建寄存器"添加数据</div>
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}
