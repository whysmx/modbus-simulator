"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Toolbar } from "@/components/Toolbar"
import { DeviceTree } from "@/components/DeviceTree"
import { RegisterTable } from "@/components/RegisterTable"
import { DeviceConfigDialog } from "@/components/DeviceConfigDialog"
import { SlaveConfigDialog } from "@/components/SlaveConfigDialog"
import { RegisterConfigDialog } from "@/components/RegisterConfigDialog"
import { useConnections, ConnectionProvider } from "@/hooks/useConnections"

function HomeContent() {
  const { loadConnections, connections, saveChanges, hasUnsavedChanges } = useConnections()
  const [editingDevice, setEditingDevice] = useState<string | null>(null)
  const [editingSlave, setEditingSlave] = useState<{ connectionId: string; slaveId: string | null } | null>(null)
  const [showRegisterDialog, setShowRegisterDialog] = useState<{ connectionId: string; slaveId: string } | null>(null)
  const [editingRegister, setEditingRegister] = useState<{
    connectionId: string
    slaveId: string
    registerName: string
    registerData: any
  } | null>(null)

  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  useEffect(() => {
    loadConnections()
  }, [])

  useEffect(() => {
    console.log("[v0] Page useEffect: Connections updated:", connections)
    console.log("[v0] Page useEffect: Connections length:", connections.length)
  }, [connections])

  return (
    <div className="h-screen flex flex-col bg-background">
      <Toolbar
        onSaveChanges={saveChanges}
        hasUnsavedChanges={hasUnsavedChanges}
      />

      <div className="flex-1 flex gap-4 p-4 overflow-hidden">
        <div className={`${sidebarCollapsed ? "w-12" : "w-1/5 min-w-[300px]"} transition-all duration-300 relative`}>
          <Button
            variant="ghost"
            size="sm"
            className="absolute -right-3 top-4 z-10 h-6 w-6 rounded-full border bg-background shadow-md hover:bg-accent"
            onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          >
            {sidebarCollapsed ? <ChevronRight className="h-3 w-3" /> : <ChevronLeft className="h-3 w-3" />}
          </Button>

          <div className={`h-full ${sidebarCollapsed ? "overflow-hidden" : ""}`}>
            <DeviceTree
              editingDevice={editingDevice}
              setEditingDevice={setEditingDevice}
              editingSlave={editingSlave}
              setEditingSlave={setEditingSlave}
              showRegisterDialog={showRegisterDialog}
              setShowRegisterDialog={setShowRegisterDialog}
              editingRegister={editingRegister}
              setEditingRegister={setEditingRegister}
              collapsed={sidebarCollapsed}
            />
          </div>
        </div>

        <div className="flex-1">
          <RegisterTable />
        </div>
      </div>

      <DeviceConfigDialog
        open={editingDevice !== null && !editingDevice?.startsWith("register-")}
        onOpenChange={(open) => !open && setEditingDevice(null)}
        connectionId={editingDevice === "new-connection" ? null : editingDevice}
      />

      <SlaveConfigDialog
        open={editingSlave !== null}
        onOpenChange={(open) => !open && setEditingSlave(null)}
        connectionId={editingSlave?.connectionId || ""}
        slaveId={editingSlave?.slaveId}
      />

      <RegisterConfigDialog
        open={showRegisterDialog !== null}
        onOpenChange={(open) => !open && setShowRegisterDialog(null)}
        connectionId={showRegisterDialog?.connectionId || ""}
        slaveId={showRegisterDialog?.slaveId || ""}
        editMode={false}
      />

      <RegisterConfigDialog
        open={editingRegister !== null}
        onOpenChange={(open) => !open && setEditingRegister(null)}
        connectionId={editingRegister?.connectionId || ""}
        slaveId={editingRegister?.slaveId || ""}
        editMode={true}
        existingGroup={editingRegister?.registerData}
      />
    </div>
  )
}

export default function Home() {
  return (
    <ConnectionProvider>
      <HomeContent />
    </ConnectionProvider>
  )
}
