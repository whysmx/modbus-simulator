"use client"

import { Button } from "@/components/ui/button"
import { Save } from "lucide-react"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"

interface ToolbarProps {
  onSaveChanges?: () => void
  hasUnsavedChanges?: boolean
}

export function Toolbar({ onSaveChanges, hasUnsavedChanges = false }: ToolbarProps) {
  return (
    <TooltipProvider>
      <div className="flex items-center justify-end gap-2 p-4 border-b bg-card">
        <div className="flex items-center gap-2">
          {hasUnsavedChanges && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="default" size="sm" onClick={onSaveChanges} className="bg-primary hover:bg-primary/90">
                  <Save className="w-4 h-4 mr-2" />
                  保存修改
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>保存所有修改的寄存器数据</p>
              </TooltipContent>
            </Tooltip>
          )}
        </div>
      </div>
    </TooltipProvider>
  )
}
