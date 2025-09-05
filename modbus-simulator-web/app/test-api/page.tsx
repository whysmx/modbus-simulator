"use client"

import { useState } from "react"

export default function TestAPI() {
  const [result, setResult] = useState("")
  const [loading, setLoading] = useState(false)

  const testDirectAPI = async () => {
    setLoading(true)
    try {
      console.log("Testing direct fetch to backend...")
      const response = await fetch("http://localhost:5000/api/connections/tree")
      const data = await response.json()
      setResult("Direct API Success: " + JSON.stringify(data, null, 2))
      console.log("Direct API result:", data)
    } catch (error) {
      console.error("Direct API error:", error)
      setResult("Direct API Error: " + String(error))
    } finally {
      setLoading(false)
    }
  }

  const testProxyAPI = async () => {
    setLoading(true)
    try {
      console.log("Testing proxy API...")
      const response = await fetch("/api/connections/tree")
      const data = await response.json()
      setResult("Proxy API Success: " + JSON.stringify(data, null, 2))
      console.log("Proxy API result:", data)
    } catch (error) {
      console.error("Proxy API error:", error)
      setResult("Proxy API Error: " + String(error))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold mb-4">API Test Page</h1>
      
      <div className="space-y-4">
        <button 
          onClick={testDirectAPI} 
          disabled={loading}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:bg-gray-400"
        >
          {loading ? "Testing..." : "Test Direct API (localhost:5000)"}
        </button>
        
        <button 
          onClick={testProxyAPI} 
          disabled={loading}
          className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:bg-gray-400"
        >
          {loading ? "Testing..." : "Test Proxy API (/api)"}
        </button>
      </div>

      {result && (
        <div className="mt-6">
          <h2 className="text-lg font-semibold mb-2">Result:</h2>
          <pre className="bg-gray-100 p-4 rounded overflow-auto max-h-96">
            {result}
          </pre>
        </div>
      )}
    </div>
  )
}