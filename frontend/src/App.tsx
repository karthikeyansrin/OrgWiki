import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { OverviewPage } from './pages/OverviewPage'
import { SystemPage } from './pages/SystemPage'
import { ImportPage } from './pages/ImportPage'
import { UploadDetailsPage } from './pages/UploadDetailsPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { GeneratedArticlesPage } from './pages/GeneratedArticlesPage'
import { ReviewDashboardPage } from './pages/ReviewDashboardPage'
import { ReviewArticlePage } from './pages/ReviewArticlePage'
import { KnowledgeBasePage } from './pages/KnowledgeBasePage'
import { KnowledgeArticlePage } from './pages/KnowledgeArticlePage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="system" element={<SystemPage />} />
        <Route path="import" element={<ImportPage />} />
        <Route path="uploads/:uploadId" element={<UploadDetailsPage />} />
        <Route path="analyses/:analysisId" element={<AnalysisPage />} />
        <Route path="generations/:generationId" element={<GeneratedArticlesPage />} />
        <Route path="review" element={<ReviewDashboardPage />} />
        <Route path="review/articles/:articleId" element={<ReviewArticlePage />} />
        <Route path="knowledge" element={<KnowledgeBasePage />} />
        <Route path="knowledge/articles/:articleKey" element={<KnowledgeArticlePage />} />
      </Route>
    </Routes>
  )
}

export default App
