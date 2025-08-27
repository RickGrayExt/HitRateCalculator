
import React, { useState } from 'react'

function App() {
  const [datasetPath, setDatasetPath] = useState('/data/sample_sales.csv')
  const [mode, setMode] = useState('PTO')
  const [runId, setRunId] = useState(null)
  const [status, setStatus] = useState(null)
  const [result, setResult] = useState(null)

  const startRun = async () => {
    const res = await fetch('/runs', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ datasetPath, mode })
    })
    const data = await res.json()
    setRunId(data.runId)
    setStatus(data.status)
    setResult(null)
    pollStatus(data.runId)
  }

  const pollStatus = (id) => {
    const interval = setInterval(async () => {
      const res = await fetch(`/runs/${id}`)
      if (res.ok) {
        const data = await res.json()
        setStatus(data.status)
        if (data.status === 'Completed') {
          clearInterval(interval)
          fetchResult(id)
        }
      }
    }, 2000)
  }

  const fetchResult = async (id) => {
    const res = await fetch(`/results/${id}`)
    if (res.ok) {
      const data = await res.json()
      setResult(data)
    }
  }

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-4">HitRate Calculator Dashboard</h1>
      <div className="mb-4">
        <label className="block">Dataset Path:</label>
        <input className="border p-2 w-full" value={datasetPath} onChange={e=>setDatasetPath(e.target.value)} />
      </div>
      <div className="mb-4">
        <label className="block">Mode:</label>
        <select className="border p-2 w-full" value={mode} onChange={e=>setMode(e.target.value)}>
          <option value="PTO">Pick-to-Order</option>
          <option value="PTL">Pick-to-Line</option>
        </select>
      </div>
      <button className="bg-blue-500 text-white px-4 py-2 rounded" onClick={startRun}>Start Run</button>

      {runId && (
        <div className="mt-4">
          <p>Run ID: {runId}</p>
          <p>Status: {status}</p>
        </div>
      )}

      {result && (
        <div className="mt-6 bg-white p-4 rounded shadow">
          <h2 className="text-xl font-semibold mb-2">Results ({result.mode})</h2>
          <p>Hit Rate: {(result.hitRate*100).toFixed(2)}%</p>
          <p>Total Items Picked: {result.totalItemsPicked}</p>
          <p>Total Rack Presentations: {result.totalRackPresentations}</p>
          <h3 className="mt-2 font-semibold">By Rack:</h3>
          <ul>
            {Object.entries(result.byRack).map(([rack,val])=>(
              <li key={rack}>{rack}: {val.toFixed(2)}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

export default App
