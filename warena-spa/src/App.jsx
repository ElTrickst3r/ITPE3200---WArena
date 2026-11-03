import { useEffect, useRef, useState } from 'react'
import { NavLink, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import DashboardPage from './pages/DashboardPage'
import QuestionsPage from './pages/QuestionsPage'
import ChallengeSetsPage from './pages/ChallengeSetsPage'
import PlaySetPage from './pages/PlaySetPage'
import HistoryPage from './pages/HistoryPage'
import AchievementsPage from './pages/AchievementsPage'
import ManagePage from './pages/ManagePage'
import LiveArenaPage from './pages/LiveArenaPage'
import UsersPage from './pages/UsersPage'
import ProfilePage from './pages/ProfilePage'

function MenuLink({ to, children, onNavigate }) {
  return <NavLink to={to} onClick={onNavigate}>{children}</NavLink>
}

function Layout() {
  const { user, logout, canManage, isAdmin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const navRef = useRef(null)
  const [activeMenu, setActiveMenu] = useState(null)

  const closeMenus = () => setActiveMenu(null)
  const toggleMenu = menu => setActiveMenu(current => current === menu ? null : menu)

  useEffect(() => {
    closeMenus()
  }, [location.pathname])

  useEffect(() => {
    const onPointerDown = event => {
      if (navRef.current && !navRef.current.contains(event.target)) closeMenus()
    }
    const onKeyDown = event => {
      if (event.key === 'Escape') closeMenus()
    }

    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [])

  const signOut = () => {
    closeMenus()
    logout()
    navigate('/login')
  }

  return <div className="app-shell">
    <header className="topbar" ref={navRef}>
      <div className="nav-wrap">
        <NavLink to="/" className="brand" onClick={closeMenus}>WArena</NavLink>
        {user && <nav className="nav-links" aria-label="Main navigation">
          <NavLink to="/" onClick={closeMenus}>Dashboard</NavLink>

          <div className={`nav-menu ${activeMenu === 'questions' ? 'open' : ''}`}>
            <button
              type="button"
              className="nav-menu-trigger"
              aria-expanded={activeMenu === 'questions'}
              aria-haspopup="menu"
              onClick={() => toggleMenu('questions')}
            >
              Questions <span aria-hidden="true">▾</span>
            </button>
            {activeMenu === 'questions' && <div className="nav-menu-panel" role="menu">
              <MenuLink to="/questions" onNavigate={closeMenus}>Singles</MenuLink>
              <MenuLink to="/sets" onNavigate={closeMenus}>Sets</MenuLink>
            </div>}
          </div>

          <NavLink to="/live" onClick={closeMenus}>Live Arena</NavLink>

          {canManage && <div className={`nav-menu ${activeMenu === 'manage' ? 'open' : ''}`}>
            <button
              type="button"
              className="nav-menu-trigger"
              aria-expanded={activeMenu === 'manage'}
              aria-haspopup="menu"
              onClick={() => toggleMenu('manage')}
            >
              Manage <span aria-hidden="true">▾</span>
            </button>
            {activeMenu === 'manage' && <div className="nav-menu-panel" role="menu">
              <MenuLink to="/manage" onNavigate={closeMenus}>Content</MenuLink>
              {isAdmin && <MenuLink to="/users" onNavigate={closeMenus}>Users</MenuLink>}
            </div>}
          </div>}
        </nav>}

        <div className="nav-user">
          {user ? <div className={`nav-menu user-menu ${activeMenu === 'profile' ? 'open' : ''}`}>
            <button
              type="button"
              className="nav-menu-trigger user-summary"
              aria-expanded={activeMenu === 'profile'}
              aria-haspopup="menu"
              onClick={() => toggleMenu('profile')}
            >
              <span>{user.username}<small>{user.role}</small></span>
              <span aria-hidden="true">▾</span>
            </button>
            {activeMenu === 'profile' && <div className="nav-menu-panel user-menu-panel" role="menu">
              <MenuLink to="/history" onNavigate={closeMenus}>History</MenuLink>
              <MenuLink to="/badges" onNavigate={closeMenus}>Badges</MenuLink>
              <MenuLink to="/profile" onNavigate={closeMenus}>Profile</MenuLink>
              <button type="button" className="nav-menu-button" onClick={signOut}>Log out</button>
            </div>}
          </div> : <>
            <NavLink to="/login" className="ghost-button" onClick={closeMenus}>Log in</NavLink>
            <NavLink to="/register" className="light-button" onClick={closeMenus}>Register</NavLink>
          </>}
        </div>
      </div>
    </header>

    <main className="page"><Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<ProtectedRoute />}>
        <Route index element={<DashboardPage />} />
        <Route path="/questions" element={<QuestionsPage />} />
        <Route path="/sets" element={<ChallengeSetsPage />} />
        <Route path="/sets/:id/play" element={<PlaySetPage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route path="/badges" element={<AchievementsPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/live" element={<LiveArenaPage />} />
      </Route>
      <Route element={<ProtectedRoute roles={['Admin','Teacher']} />}><Route path="/manage" element={<ManagePage />} /></Route>
      <Route element={<ProtectedRoute roles={['Admin']} />}><Route path="/users" element={<UsersPage />} /></Route>
    </Routes></main>

    <footer className="footer">WArena — Web Applications, Gamified · API + React SPA</footer>
  </div>
}

export default function App(){ return <Layout /> }
