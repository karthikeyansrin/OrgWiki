import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { OverviewPage } from './pages/OverviewPage'
import { SystemPage } from './pages/SystemPage'
import { ImportPage } from './pages/ImportPage'
import { UploadDetailsPage } from './pages/UploadDetailsPage'
import { AnalysisPage } from './pages/AnalysisPage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="system" element={<SystemPage />} />
        <Route path="import" element={<ImportPage />} />
        <Route path="uploads/:uploadId" element={<UploadDetailsPage />} />
        <Route path="analyses/:analysisId" element={<AnalysisPage />} />
      </Route>
    </Routes>
  )
}

export default App
