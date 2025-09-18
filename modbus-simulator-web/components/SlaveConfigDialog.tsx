"use client"

import type React from "react"
import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useConnections } from "@/hooks/useConnections"
import { Trash2, X, Check } from "lucide-react"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"

interface SlaveConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  connectionId?: string
  slaveId?: string | null
}

export function SlaveConfigDialog({ open, onOpenChange, connectionId, slaveId }: SlaveConfigDialogProps) {
  const { connections, addSlave, updateSlave, deleteSlave } = useConnections()
  const [formData, setFormData] = useState({
    name: "从机1",
    slaveid: 1,
  })

  const isEditing = !!slaveId && slaveId !== "new"
  const connection = connections.find((c) => c.id === connectionId)
  const slave = connection?.slaves.find((s) => s.id === slaveId)

  useEffect(() => {
    if (isEditing && slave) {
      setFormData({
        name: slave.name,
        slaveid: slave.slaveid,
      })
    } else {
      setFormData({
        name: "从机1",
        slaveid: 1,
      })
    }
  }, [isEditing, slave, open])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!connectionId) {
      console.error("[v0] SlaveConfigDialog: No connectionId provided!")
      return
    }

    console.log("[v0] SlaveConfigDialog handleSubmit called", { 
      connectionId, 
      slaveId, 
      formData, 
      isEditing 
    })

    try {
      if (isEditing && slaveId) {
        console.log("[v0] Updating slave with connectionId:", connectionId, "slaveId:", slaveId)
        await updateSlave(connectionId, slaveId, {
          name: formData.name,
          slaveid: formData.slaveid,
        })
      } else {
        console.log("[v0] Adding new slave with connectionId:", connectionId, "data:", {
          name: formData.name,
          slaveid: formData.slaveid,
        })
        const result = await addSlave(connectionId, {
          name: formData.name,
          slaveid: formData.slaveid,
        })
        console.log("[v0] addSlave returned result:", result)
      }

      console.log("[v0] Slave operation completed, closing dialog")
      onOpenChange(false)
    } catch (error) {
      console.error('[v0] Failed to save slave:', error)
      console.error('[v0] Error type:', typeof error, error)
      // Keep dialog open on error so user can retry
    }
  }

  const handleDelete = () => {
    if (connectionId && slaveId) {
      if (confirm('确定要删除这个从机吗？此操作无法撤销。')) {
        deleteSlave(connectionId, slaveId)
        onOpenChange(false)
      }
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEditing ? "编辑从机" : "新增从机"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">从机名称</Label>
            <Input
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="从机1"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="slaveid">从机地址</Label>
            <Input
              id="slaveid"
              type="number"
              value={formData.slaveid}
              onChange={(e) => setFormData({ ...formData, slaveid: Number.parseInt(e.target.value) })}
              min={1}
              max={255}
              required
            />
          </div>

          <DialogFooter className="flex justify-between">
            <div className="flex-1">
              {isEditing && (
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
                  <Button type="submit" size="icon">
                    <Check className="w-4 h-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{isEditing ? "保存" : "创建"}</p>
                </TooltipContent>
              </Tooltip>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
