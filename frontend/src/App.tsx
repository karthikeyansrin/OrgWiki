import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { OverviewPage } from './pages/OverviewPage'
import { SystemPage } from './pages/SystemPage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="system" element={<SystemPage />} />
      </Route>
    </Routes>
  )
}

export default App
