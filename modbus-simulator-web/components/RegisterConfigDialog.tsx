"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useConnections } from "@/hooks/useConnections"
import type { Register } from "@/types"
import { Trash2, Info } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

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
  const { addRegister, updateRegister, deleteRegister } = useConnections()
  const [formData, setFormData] = useState({
    startaddr: 0,
    hexdata: "0000",
  })

  // 寄存器类型定义
  const registerTypes = [
    {
      name: "输入寄存器",
      englishName: "Input Registers",
      addressRange: "30001-39999",
      readCodes: ["04"],
      writeCodes: [],
      borderColor: "border-slate-200"
    },
    {
      name: "保持寄存器",
      englishName: "Holding Registers",
      addressRange: "40001-49999",
      readCodes: ["03"],
      writeCodes: ["06", "16"],
      borderColor: "border-slate-200"
    },
    {
      name: "线圈",
      englishName: "Coils",
      addressRange: "1-9999",
      readCodes: ["01"],
      writeCodes: ["05", "15"],
      borderColor: "border-slate-200"
    },
    {
      name: "离散输入",
      englishName: "Discrete Inputs",
      addressRange: "10001-19999",
      readCodes: ["02"],
      writeCodes: [],
      borderColor: "border-slate-200"
    }
  ]

  useEffect(() => {
    if (open) {
      if (editMode && existingGroup) {
        setFormData({
          startaddr: existingGroup.startaddr,
          hexdata: existingGroup.hexdata || "0000",
        })
      } else {
        setFormData({
          startaddr: 0,
          hexdata: "0000",
        })
      }
    }
  }, [open, editMode, existingGroup])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!connectionId || !slaveId) return

    if (editMode && existingGroup) {
      updateRegister(connectionId, slaveId, existingGroup.id, {
        startaddr: formData.startaddr,
        hexdata: formData.hexdata,
      })
    } else {
      addRegister(connectionId, slaveId, {
        startaddr: formData.startaddr,
        hexdata: formData.hexdata,
      })
    }

    onOpenChange(false)
  }

  const handleDelete = () => {
    if (connectionId && slaveId && existingGroup) {
      deleteRegister(connectionId, slaveId, existingGroup.id)
      onOpenChange(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{editMode ? "编辑寄存器组" : "配置寄存器组"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="startaddr">起始逻辑地址</Label>
              <Input
                id="startaddr"
                type="number"
                value={formData.startaddr}
                onChange={(e) => setFormData({ ...formData, startaddr: Number.parseInt(e.target.value) })}
                min={0}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="hexdata">16进制字符串</Label>
              <Input
                id="hexdata"
                value={formData.hexdata}
                onChange={(e) => setFormData({ ...formData, hexdata: e.target.value })}
                placeholder="0000"
                required
              />
            </div>
          </div>

          {/* 寄存器类型参考信息 */}
          <div className="border-t pt-4">
            <div className="grid grid-cols-2 gap-4">
              {registerTypes.map((type) => (
                <Card key={type.name} className={`${type.borderColor} bg-slate-50/30 relative shadow-sm`}>
                  <div className="absolute -top-2 left-3 px-2 py-0.5 bg-white border border-slate-200 rounded text-sm font-medium text-slate-700 shadow-sm">
                    {type.name}
                  </div>
                  <CardContent className="p-4 pt-5">
                    <div className="space-y-2 text-xs text-muted-foreground">
                      <div className="flex items-center gap-2">
                        <span className="w-10 text-right">功能码:</span>
                        <div className="flex flex-wrap gap-1">
                          {type.readCodes.map((code) => (
                            <Badge key={code} variant="secondary" className="text-xs px-2 py-0.5 bg-blue-50 text-blue-700 border-blue-200 font-mono">
                              {code}
                            </Badge>
                          ))}
                          {type.writeCodes.map((code) => (
                            <Badge key={code} variant="secondary" className="text-xs px-2 py-0.5 bg-red-50 text-red-700 border-red-200 font-mono">
                              {code}
                            </Badge>
                          ))}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="w-10 text-right">地址:</span>
                        <span className="font-mono bg-slate-200 px-1 py-0.5 rounded text-slate-700">{type.addressRange}</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>

          <DialogFooter className="flex justify-between">
            {editMode && (
              <Button type="button" variant="destructive" onClick={handleDelete}>
                <Trash2 className="w-4 h-4 mr-2" />
                删除
              </Button>
            )}
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                取消
              </Button>
              <Button type="submit">确定</Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
