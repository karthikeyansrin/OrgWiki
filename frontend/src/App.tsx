import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { OverviewPage } from './pages/OverviewPage'
import { ImportPage } from './pages/ImportPage'
import { UploadDetailsPage } from './pages/UploadDetailsPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { GeneratedArticlesPage } from './pages/GeneratedArticlesPage'
import { ReviewDashboardPage } from './pages/ReviewDashboardPage'
import { ReviewArticlePage } from './pages/ReviewArticlePage'
import { KnowledgeBasePage } from './pages/KnowledgeBasePage'
import { KnowledgeArticlePage } from './pages/KnowledgeArticlePage'
import { LandingPage } from './pages/LandingPage'
import { AuthPage } from './pages/AuthPage'
import { ProtectedRoute } from './auth/ProtectedRoute'

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/auth" element={<AuthPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="dashboard" element={<OverviewPage />} />
          {/* <Route path="system" element={<SystemPage />} /> */}
          <Route path="import" element={<ImportPage />} />
          <Route path="uploads/:uploadId" element={<UploadDetailsPage />} />
          <Route path="analyses/:analysisId" element={<AnalysisPage />} />
          <Route path="generations/:generationId" element={<GeneratedArticlesPage />} />
          <Route path="review" element={<ReviewDashboardPage />} />
          <Route path="review/articles/:articleId" element={<ReviewArticlePage />} />
          <Route path="knowledge" element={<KnowledgeBasePage />} />
          <Route path="knowledge/articles/:articleKey" element={<KnowledgeArticlePage />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App
