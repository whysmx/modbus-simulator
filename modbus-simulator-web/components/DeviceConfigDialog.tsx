"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { useConnections } from "@/hooks/useConnections"
import { apiClient } from "@/lib/api"
import { Trash2 } from "lucide-react"
import type { ProtocolType } from "@/types"

interface DeviceConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  connectionId?: string | null
}

export function DeviceConfigDialog({ open, onOpenChange, connectionId }: DeviceConfigDialogProps) {
  const { connections, addConnection, updateConnection, deleteConnection } = useConnections()
  const [protocolTypes, setProtocolTypes] = useState<ProtocolType[]>([])
  const [formData, setFormData] = useState({
    name: "",
    port: 502,
    protocolType: 0, // 默认为 ModbusRtuOverTcp
  })

  const isEditing = !!connectionId && connectionId !== "new-connection"
  const connection = connections.find((c) => c.id === connectionId)

  // 加载协议类型
  useEffect(() => {
    const loadProtocolTypes = async () => {
      try {
        const types = await apiClient.getProtocolTypes()
        setProtocolTypes(types)
      } catch (error) {
        console.error("Failed to load protocol types:", error)
      }
    }
    loadProtocolTypes()
  }, [])

  useEffect(() => {
    if (isEditing && connection) {
      setFormData({
        name: connection.name,
        port: connection.port || 502,
        protocolType: connection.protocolType || 0,
      })
    } else {
      setFormData({
        name: "",
        port: 502,
        protocolType: 0,
      })
    }
  }, [isEditing, connection, open])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (isEditing && connectionId) {
      updateConnection(connectionId, formData)
    } else {
      addConnection({
        ...formData,
        port: formData.port,
        slaves: [],
      })
    }

    onOpenChange(false)
  }

  const handleDelete = () => {
    if (isEditing && connectionId) {
      deleteConnection(connectionId)
      onOpenChange(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEditing ? "编辑连接" : "新建连接"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">连接名称</Label>
            <Input
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="连接1"
              required
            />
          </div>

          <div className="space-y-3">
            <Label className="text-sm font-medium">协议类型</Label>
            <RadioGroup
              value={formData.protocolType.toString()}
              onValueChange={(value) => setFormData({ ...formData, protocolType: Number.parseInt(value) })}
              className="grid grid-cols-1 gap-3"
            >
              {protocolTypes.map((type) => (
                <div key={type.value} className="flex items-center space-x-3 rounded-lg border p-3 hover:bg-gray-50/80 transition-colors">
                  <RadioGroupItem value={type.value.toString()} id={`protocol-${type.value}`} className="mt-0.5" />
                  <Label 
                    htmlFor={`protocol-${type.value}`} 
                    className="flex-1 cursor-pointer font-mono text-sm leading-relaxed"
                  >
                    {type.value === 0 ? "Modbus RTU Over TCP" : "Modbus TCP"}
                  </Label>
                </div>
              ))}
            </RadioGroup>
          </div>

          {isEditing && (
            <div className="space-y-2">
              <Label htmlFor="port">端口号</Label>
              <Input
                id="port"
                type="number"
                value={formData.port}
                onChange={(e) => setFormData({ ...formData, port: Number.parseInt(e.target.value) || 502 })}
                placeholder="502"
                min="1"
                max="65535"
                required
              />
            </div>
          )}

          <DialogFooter className="flex justify-between items-center">
            <div className="flex-1">
              {isEditing && (
                <Button type="button" variant="destructive" onClick={handleDelete}>
                  <Trash2 className="w-4 h-4 mr-2" />
                  删除
                </Button>
              )}
            </div>
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                取消
              </Button>
              <Button type="submit">{isEditing ? "保存" : "创建"}</Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
