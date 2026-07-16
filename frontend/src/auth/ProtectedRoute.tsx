import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function ProtectedRoute() {
  const { user, isLoading } = useAuth()
  const location = useLocation()
  if (isLoading) return <div className="grid min-h-screen place-items-center text-slate-600">Restoring your session...</div>
  return user ? <Outlet /> : <Navigate to="/auth" replace state={{ from: location.pathname }} />
}
