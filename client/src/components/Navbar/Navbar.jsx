import React, { useState, useEffect, useRef } from 'react';
import { Link, NavLink, useNavigate, useLocation } from 'react-router-dom';
import {
  Search,
  User,
  Heart,
  Clock,
  Shield,
  LogOut,
  Menu,
  X,
  ChevronDown,
  Film,
  Sparkles,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { genreService } from '../../services/api/genreService';
import './Navbar.css';

const Navbar = () => {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const [isScrolled, setIsScrolled] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isUserDropdownOpen, setIsUserDropdownOpen] = useState(false);
  const [isGenreDropdownOpen, setIsGenreDropdownOpen] = useState(false);
  const [genres, setGenres] = useState([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearchActive, setIsSearchActive] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();
  const searchInputRef = useRef(null);
  const userMenuRef = useRef(null);
  const genreMenuRef = useRef(null);

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 40);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    genreService
      .getGenres()
      .then((data) => setGenres(data || []))
      .catch((err) => console.warn('Could not fetch genres for navbar', err));
  }, []);

  // Close menus on route change
  useEffect(() => {
    setIsMobileMenuOpen(false);
    setIsUserDropdownOpen(false);
    setIsGenreDropdownOpen(false);
  }, [location.pathname]);

  // Click outside to close dropdowns
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target)) {
        setIsUserDropdownOpen(false);
      }
      if (genreMenuRef.current && !genreMenuRef.current.contains(e.target)) {
        setIsGenreDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
      setIsSearchActive(false);
      setSearchQuery('');
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  return (
    <header className={`navbar-header ${isScrolled ? 'scrolled glass-header' : ''}`}>
      <div className="navbar-container container">
        {/* Left: Brand Logo */}
        <div className="navbar-left">
          <Link to="/" className="navbar-brand" aria-label="SHIN movie Trang chủ">
            <span className="brand-logo-icon">
              <Film size={20} />
            </span>
            <span className="brand-title">
              SHIN <span className="brand-title-accent">movie</span>
            </span>
          </Link>

          {/* Desktop Nav items */}
          <nav className="navbar-nav desktop-only">
            <NavLink
              to="/"
              className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
            >
              Trang Chủ
            </NavLink>
            <NavLink
              to="/movies"
              className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
            >
              Phim
            </NavLink>
            <NavLink
              to="/movies?type=series"
              className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
            >
              Phim Bộ
            </NavLink>
            <NavLink
              to="/movies?type=single"
              className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
            >
              Phim Lẻ
            </NavLink>

            {/* Genre Dropdown */}
            <div className="nav-dropdown-wrapper" ref={genreMenuRef}>
              <button
                className={`nav-link dropdown-toggle ${isGenreDropdownOpen ? 'open' : ''}`}
                onClick={() => setIsGenreDropdownOpen(!isGenreDropdownOpen)}
              >
                <span>Thể Loại</span>
                <ChevronDown size={14} />
              </button>

              {isGenreDropdownOpen && (
                <div className="nav-dropdown-menu genre-menu glass">
                  <div className="genre-grid">
                    {genres.slice(0, 16).map((g) => (
                      <Link
                        key={g.id}
                        to={`/movies?genre=${encodeURIComponent(g.name)}`}
                        className="genre-item"
                        onClick={() => setIsGenreDropdownOpen(false)}
                      >
                        {g.name}
                      </Link>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </nav>
        </div>

        {/* Right: Search, Auth / Profile */}
        <div className="navbar-right">
          {/* Search Bar */}
          <form
            onSubmit={handleSearchSubmit}
            className={`navbar-search-form ${isSearchActive ? 'active' : ''}`}
          >
            <input
              ref={searchInputRef}
              type="text"
              placeholder="Tìm kiếm phim, diễn viên..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="navbar-search-input"
            />
            <button
              type="button"
              className="navbar-search-btn"
              onClick={() => {
                if (!isSearchActive) {
                  setIsSearchActive(true);
                  setTimeout(() => searchInputRef.current?.focus(), 100);
                } else if (searchQuery.trim()) {
                  handleSearchSubmit({ preventDefault: () => {} });
                } else {
                  setIsSearchActive(false);
                }
              }}
              aria-label="Tìm kiếm"
            >
              <Search size={18} />
            </button>
          </form>

          {/* User Account / Auth */}
          {isAuthenticated ? (
            <div className="navbar-user-menu" ref={userMenuRef}>
              <button
                className="navbar-user-btn"
                onClick={() => setIsUserDropdownOpen(!isUserDropdownOpen)}
                aria-label="Tài khoản"
              >
                <div className="user-avatar">
                  {user?.username ? user.username.charAt(0).toUpperCase() : 'U'}
                </div>
                <span className="user-name desktop-only">{user?.username}</span>
                <ChevronDown size={14} className="desktop-only" />
              </button>

              {isUserDropdownOpen && (
                <div className="user-dropdown-menu glass">
                  <div className="user-dropdown-header">
                    <span className="user-dropdown-name">{user?.username}</span>
                    <span className="user-dropdown-email">{user?.email}</span>
                  </div>
                  <div className="user-dropdown-divider"></div>

                  <Link
                    to="/favorites"
                    className="user-dropdown-item"
                    onClick={() => setIsUserDropdownOpen(false)}
                  >
                    <Heart size={16} />
                    <span>Phim Yêu Thích</span>
                  </Link>
                  <Link
                    to="/history"
                    className="user-dropdown-item"
                    onClick={() => setIsUserDropdownOpen(false)}
                  >
                    <Clock size={16} />
                    <span>Lịch Sử Xem</span>
                  </Link>

                  {isAdmin && (
                    <Link
                      to="/admin"
                      className="user-dropdown-item admin-link"
                      onClick={() => setIsUserDropdownOpen(false)}
                    >
                      <Shield size={16} />
                      <span>Quản Trị Viên</span>
                    </Link>
                  )}

                  <div className="user-dropdown-divider"></div>
                  <button
                    className="user-dropdown-item logout"
                    onClick={handleLogout}
                  >
                    <LogOut size={16} />
                    <span>Đăng Xuất</span>
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="navbar-auth-actions desktop-only">
              <Link to="/login" className="btn-secondary nav-btn-login">
                Đăng Nhập
              </Link>
              <Link to="/register" className="btn-primary nav-btn-register">
                Đăng Ký
              </Link>
            </div>
          )}

          {/* Mobile Menu Button */}
          <button
            className="navbar-mobile-toggle mobile-only"
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            aria-label="Menu"
          >
            {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </div>
      </div>

      {/* Mobile Drawer */}
      {isMobileMenuOpen && (
        <div className="navbar-mobile-drawer glass">
          <nav className="mobile-nav-links">
            <NavLink to="/" className="mobile-nav-link">
              Trang Chủ
            </NavLink>
            <NavLink to="/movies" className="mobile-nav-link">
              Tất Cả Phim
            </NavLink>
            <NavLink to="/movies?type=series" className="mobile-nav-link">
              Phim Bộ
            </NavLink>
            <NavLink to="/movies?type=single" className="mobile-nav-link">
              Phim Lẻ
            </NavLink>
            <NavLink to="/search" className="mobile-nav-link">
              Tìm Kiếm
            </NavLink>

            {isAuthenticated ? (
              <>
                <div className="mobile-divider"></div>
                <NavLink to="/favorites" className="mobile-nav-link">
                  <Heart size={16} /> Phim Yêu Thích
                </NavLink>
                <NavLink to="/history" className="mobile-nav-link">
                  <Clock size={16} /> Lịch Sử Xem
                </NavLink>
                {isAdmin && (
                  <NavLink to="/admin" className="mobile-nav-link admin-link">
                    <Shield size={16} /> Quản Trị Hệ Thống
                  </NavLink>
                )}
                <button
                  className="mobile-nav-link logout"
                  onClick={handleLogout}
                >
                  <LogOut size={16} /> Đăng Xuất ({user?.username})
                </button>
              </>
            ) : (
              <div className="mobile-auth-buttons">
                <Link to="/login" className="btn-secondary">
                  Đăng Nhập
                </Link>
                <Link to="/register" className="btn-primary">
                  Đăng Ký
                </Link>
              </div>
            )}
          </nav>
        </div>
      )}
    </header>
  );
};

export default Navbar;
