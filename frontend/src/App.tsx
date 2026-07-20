import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { ImportPage } from './pages/ImportPage'
import { UploadDetailsPage } from './pages/UploadDetailsPage'
import { UploadsPage } from './pages/UploadsPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { GeneratedArticlesPage } from './pages/GeneratedArticlesPage'
import { ReviewDashboardPage } from './pages/ReviewDashboardPage'
import { ReviewArticlePage } from './pages/ReviewArticlePage'
import { KnowledgeBasePage } from './pages/KnowledgeBasePage'
import { KnowledgeArticlePage } from './pages/KnowledgeArticlePage'
import { LandingPage } from './pages/LandingPage'
import { AuthPage } from './pages/AuthPage'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { TeamSpacesPage } from './pages/TeamSpacesPage'
import { TeamSpacePage } from './pages/TeamSpacePage'
import { TeamSpaceArticlePage } from './pages/TeamSpaceArticlePage'
import { AppFooter } from './components/AppFooter'

function App() {
  return <>
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/auth" element={<AuthPage />} />
      <Route path="/spaces" element={<TeamSpacesPage />} />
      <Route path="/spaces/:slug" element={<TeamSpacePage />} />
      <Route path="/spaces/:slug/:articleSlug" element={<TeamSpaceArticlePage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="dashboard" element={<Navigate to="/import" replace />} />
          {/* <Route path="system" element={<SystemPage />} /> */}
          <Route path="import" element={<ImportPage />} />
          <Route path="uploads" element={<UploadsPage />} />
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
    <AppFooter />
  </>
}

export default App
