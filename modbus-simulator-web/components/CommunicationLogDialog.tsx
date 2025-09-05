"use client"

import { useEffect, useRef } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Checkbox } from "@/components/ui/checkbox"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Play, Square, Trash2, Download } from "lucide-react"
import { useCommunicationLog } from "@/hooks/useCommunicationLog"
import { useDevices } from "@/hooks/useDevices"

interface CommunicationLogDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CommunicationLogDialog({ open, onOpenChange }: CommunicationLogDialogProps) {
  const {
    logs,
    isLogging,
    filter,
    format,
    autoScroll,
    addLog,
    clearLogs,
    startLogging,
    stopLogging,
    setFilter,
    setFormat,
    toggleAutoScroll,
  } = useCommunicationLog()

  const { devices } = useDevices()
  const logContainerRef = useRef<HTMLDivElement>(null)

  // 模拟日志生成
  useEffect(() => {
    if (!isLogging) return

    const interval = setInterval(
      () => {
        const runningDevices = devices.filter((d) => d.status === "Running")
        if (runningDevices.length === 0) return

        const device = runningDevices[Math.floor(Math.random() * runningDevices.length)]
        const isRX = Math.random() > 0.5

        addLog({
          direction: isRX ? "RX" : "TX",
          data: isRX
            ? `01 03 00 00 00 0A C5 CD`
            : `01 03 14 ${Array.from({ length: 10 }, () =>
                Math.floor(Math.random() * 256)
                  .toString(16)
                  .padStart(2, "0")
                  .toUpperCase(),
              ).join(" ")} XX XX`,
          deviceId: device.id,
        })
      },
      1000 + Math.random() * 2000,
    )

    return () => clearInterval(interval)
  }, [isLogging, devices, addLog])

  // 自动滚动
  useEffect(() => {
    if (autoScroll && logContainerRef.current) {
      logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight
    }
  }, [logs, autoScroll])

  const filteredLogs = logs.filter((log) => {
    if (filter === "All") return true
    return log.direction === filter
  })

  const formatData = (data: string) => {
    if (format === "ASCII") {
      return data
        .split(" ")
        .map((hex) => {
          const num = Number.parseInt(hex, 16)
          return isNaN(num) ? "." : num >= 32 && num <= 126 ? String.fromCharCode(num) : "."
        })
        .join("")
    }
    return data
  }

  const handleSaveLogs = () => {
    const logText = filteredLogs
      .map((log) => `${new Date(log.timestamp).toLocaleString()} [${log.direction}] ${formatData(log.data)}`)
      .join("\n")

    const blob = new Blob([logText], { type: "text/plain" })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = `modbus-log-${new Date().toISOString().slice(0, 19).replace(/:/g, "-")}.txt`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl h-[600px] flex flex-col">
        <DialogHeader>
          <DialogTitle>通信日志</DialogTitle>
        </DialogHeader>

        <div className="flex items-center gap-4 p-4 border-b">
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant={isLogging ? "destructive" : "default"}
              onClick={isLogging ? stopLogging : startLogging}
            >
              {isLogging ? (
                <>
                  <Square className="w-4 h-4 mr-2" />
                  停止
                </>
              ) : (
                <>
                  <Play className="w-4 h-4 mr-2" />
                  开始
                </>
              )}
            </Button>

            <Button size="sm" variant="outline" onClick={clearLogs}>
              <Trash2 className="w-4 h-4 mr-2" />
              清除
            </Button>

            <Button size="sm" variant="outline" onClick={handleSaveLogs}>
              <Download className="w-4 h-4 mr-2" />
              保存
            </Button>
          </div>

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <label className="text-sm">过滤:</label>
              <Select value={filter} onValueChange={setFilter}>
                <SelectTrigger className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="All">全部</SelectItem>
                  <SelectItem value="RX">RX</SelectItem>
                  <SelectItem value="TX">TX</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <label className="text-sm">格式:</label>
              <Select value={format} onValueChange={setFormat}>
                <SelectTrigger className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Hex">Hex</SelectItem>
                  <SelectItem value="ASCII">ASCII</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox id="autoScroll" checked={autoScroll} onCheckedChange={toggleAutoScroll} />
              <label htmlFor="autoScroll" className="text-sm">
                自动滚动
              </label>
            </div>
          </div>
        </div>

        <div className="flex-1 overflow-hidden">
          <div ref={logContainerRef} className="h-full overflow-auto font-mono text-xs">
            <table className="w-full">
              <thead className="bg-muted/50 sticky top-0">
                <tr>
                  <th className="text-left p-2 w-32">时间</th>
                  <th className="text-left p-2 w-16">方向</th>
                  <th className="text-left p-2">数据</th>
                </tr>
              </thead>
              <tbody>
                {filteredLogs.map((log) => (
                  <tr key={log.id} className="border-b hover:bg-muted/25">
                    <td className="p-2 text-muted-foreground">{new Date(log.timestamp).toLocaleTimeString()}</td>
                    <td className="p-2">
                      <Badge variant={log.direction === "RX" ? "default" : "secondary"} className="text-xs">
                        {log.direction}
                      </Badge>
                    </td>
                    <td className="p-2 font-mono">{formatData(log.data)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {filteredLogs.length === 0 && (
              <div className="p-8 text-center text-muted-foreground">
                <div className="text-sm">暂无通信日志</div>
                <div className="text-xs mt-1">{isLogging ? "等待通信数据..." : '点击"开始"按钮开始记录日志'}</div>
              </div>
            )}
          </div>
        </div>

        <div className="p-4 border-t bg-muted/25">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <div>状态: {isLogging ? <span className="text-green-600">正在记录</span> : <span>已停止</span>}</div>
            <div>
              总计: {logs.length} 条记录 | 显示: {filteredLogs.length} 条
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
