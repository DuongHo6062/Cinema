import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Shield,
  Film,
  Users,
  MessageSquare,
  Star,
  Download,
  Trash2,
  AlertCircle,
  CheckCircle,
  Layers,
  Search,
  RefreshCw,
  Globe,
  Sliders,
} from 'lucide-react';
import { adminService } from '../../services/api/adminService';
import { useAuth } from '../../context/AuthContext';
import { ErrorState } from '../../components/Common/CommonStates';
import './AdminPage.css';

const AdminPage = () => {
  const { isAdmin, loading: authLoading } = useAuth();
  const navigate = useNavigate();

  const [activeTab, setActiveTab] = useState('overview'); // 'overview' | 'import' | 'movies' | 'users' | 'comments'
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Import State
  const [importSlug, setImportSlug] = useState('');
  const [importLoading, setImportLoading] = useState(false);
  const [importResult, setImportResult] = useState(null);
  const [importError, setImportError] = useState(null);
  const [importVietsubOnly, setImportVietsubOnly] = useState(true);
  const [importEnglishOnly, setImportEnglishOnly] = useState(true);

  // Bulk Sync State
  const [syncSource, setSyncSource] = useState('au-my');
  const [syncFromPage, setSyncFromPage] = useState(1);
  const [syncToPage, setSyncToPage] = useState(2);
  const [syncEnglishOnly, setSyncEnglishOnly] = useState(true);
  const [syncVietsubOnly, setSyncVietsubOnly] = useState(true);
  const [syncLoading, setSyncLoading] = useState(false);
  const [syncResult, setSyncResult] = useState(null);
  const [syncError, setSyncError] = useState(null);

  // KKPhim Live Search State
  const [searchKeyword, setSearchKeyword] = useState('');
  const [searchLoading, setSearchLoading] = useState(false);
  const [searchResults, setSearchResults] = useState([]);
  const [searchError, setSearchError] = useState(null);
  const [importingSlugMap, setImportingSlugMap] = useState({});

  // Data lists
  const [moviesList, setMoviesList] = useState([]);
  const [usersList, setUsersList] = useState([]);
  const [commentsList, setCommentsList] = useState([]);

  // Check authorization
  useEffect(() => {
    if (!authLoading && !isAdmin) {
      navigate('/');
    }
  }, [isAdmin, authLoading]);

  const fetchDashboardData = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await adminService.getDashboard();
      setStats(data);
    } catch (err) {
      console.error('Failed to load admin dashboard', err);
      setError('Không thể tải thông tin bảng điều khiển quản trị.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isAdmin) {
      fetchDashboardData();
    }
  }, [isAdmin]);

  // Load tab-specific data
  useEffect(() => {
    if (!isAdmin) return;

    if (activeTab === 'movies' && moviesList.length === 0) {
      adminService
        .getMovies({ pageSize: 40 })
        .then((res) => setMoviesList(res?.items || []))
        .catch((e) => console.warn(e));
    } else if (activeTab === 'users' && usersList.length === 0) {
      adminService
        .getUsers({ pageSize: 40 })
        .then((res) => setUsersList(res?.items || []))
        .catch((e) => console.warn(e));
    } else if (activeTab === 'comments' && commentsList.length === 0) {
      adminService
        .getComments({ pageSize: 40 })
        .then((res) => setCommentsList(res?.items || []))
        .catch((e) => console.warn(e));
    }
  }, [activeTab, isAdmin]);

  const handleImportKKPhim = async (e) => {
    e.preventDefault();
    if (!importSlug.trim() || importLoading) return;

    setImportLoading(true);
    setImportResult(null);
    setImportError(null);

    try {
      const res = await adminService.importKKPhim(importSlug.trim(), {
        vietsubOnly: importVietsubOnly,
        englishOnly: importEnglishOnly,
      });
      setImportResult(res);
      setImportSlug('');
      fetchDashboardData();
    } catch (err) {
      setImportError(err.response?.data?.message || 'Không thể nhập phim từ KKPhim.');
    } finally {
      setImportLoading(false);
    }
  };

  const handleSyncKKPhim = async (e) => {
    e.preventDefault();
    if (syncLoading) return;

    setSyncLoading(true);
    setSyncResult(null);
    setSyncError(null);

    try {
      const payload = {
        listType: syncSource,
        fromPage: Number(syncFromPage) || 1,
        toPage: Number(syncToPage) || 2,
        englishOnly: syncEnglishOnly,
        vietsubOnly: syncVietsubOnly,
      };
      const res = await adminService.syncKKPhim(payload);
      setSyncResult(res);
      fetchDashboardData();
    } catch (err) {
      console.error('Sync error', err);
      setSyncError(err.response?.data?.message || 'Đồng bộ danh mục KKPhim thất bại.');
    } finally {
      setSyncLoading(false);
    }
  };

  const handleSearchKKPhim = async (e) => {
    e.preventDefault();
    if (!searchKeyword.trim() || searchLoading) return;

    setSearchLoading(true);
    setSearchError(null);
    setSearchResults([]);

    try {
      const res = await adminService.searchKKPhim(searchKeyword.trim());
      setSearchResults(res?.items || []);
      if (!res?.items || res.items.length === 0) {
        setSearchError('Không tìm thấy phim nào khớp với từ khóa trên KKPhim.');
      }
    } catch (err) {
      console.error('Search error', err);
      setSearchError('Không thể tìm kiếm trên KKPhim. Vui lòng thử lại sau.');
    } finally {
      setSearchLoading(false);
    }
  };

  const handleQuickImport = async (slug) => {
    if (!slug || importingSlugMap[slug]) return;

    setImportingSlugMap((prev) => ({ ...prev, [slug]: true }));
    try {
      const res = await adminService.importKKPhim(slug, {
        vietsubOnly: syncVietsubOnly,
        englishOnly: syncEnglishOnly,
      });
      alert(`Đã nhập thành công phim: ${res.title}`);
      fetchDashboardData();
    } catch (err) {
      alert(err.response?.data?.message || 'Không thể nhập phim này.');
    } finally {
      setImportingSlugMap((prev) => ({ ...prev, [slug]: false }));
    }
  };

  const handleDeleteComment = async (commentId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bình luận này?')) return;
    try {
      await adminService.deleteComment(commentId);
      setCommentsList((prev) => prev.filter((c) => c.id !== commentId));
    } catch (err) {
      console.warn('Failed to delete comment:', err);
      alert('Không thể xóa bình luận.');
    }
  };

  if (authLoading || loading) {
    return (
      <div className="admin-page container">
        <h1 className="admin-title">Bảng Quản Trị Hệ Thống</h1>
        <div className="skeleton line-title" style={{ marginTop: 20 }}></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="admin-page container">
        <ErrorState message={error} onRetry={fetchDashboardData} />
      </div>
    );
  }

  return (
    <div className="admin-page container">
      {/* Header */}
      <div className="admin-header">
        <div className="admin-header-title-box">
          <h1 className="admin-title">
            <Shield size={28} color="#f59e0b" /> Quản Trị SHIN movie
          </h1>
          <p className="admin-subtitle">
            Quản lý phim, người dùng, bình luận và tích hợp dữ liệu
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="admin-tabs">
        <button
          className={`admin-tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          <Layers size={16} /> Tổng Quan
        </button>
        <button
          className={`admin-tab-btn ${activeTab === 'import' ? 'active' : ''}`}
          onClick={() => setActiveTab('import')}
        >
          <Download size={16} /> Nhập Phim KKPhim
        </button>
        <button
          className={`admin-tab-btn ${activeTab === 'movies' ? 'active' : ''}`}
          onClick={() => setActiveTab('movies')}
        >
          <Film size={16} /> Danh Sách Phim ({stats?.totalMovies || 0})
        </button>
        <button
          className={`admin-tab-btn ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => setActiveTab('users')}
        >
          <Users size={16} /> Người Dùng ({stats?.totalUsers || 0})
        </button>
        <button
          className={`admin-tab-btn ${activeTab === 'comments' ? 'active' : ''}`}
          onClick={() => setActiveTab('comments')}
        >
          <MessageSquare size={16} /> Bình Luận ({stats?.totalComments || 0})
        </button>
      </div>

      {/* Tab: Overview */}
      {activeTab === 'overview' && (
        <div className="admin-tab-content">
          <div className="admin-stats-grid">
            <div className="admin-stat-card glass">
              <div className="stat-icon-wrap movies">
                <Film size={24} />
              </div>
              <div className="stat-details">
                <span className="stat-value">{stats?.totalMovies || 0}</span>
                <span className="stat-label">Tổng số phim</span>
              </div>
            </div>

            <div className="admin-stat-card glass">
              <div className="stat-icon-wrap episodes">
                <Layers size={24} />
              </div>
              <div className="stat-details">
                <span className="stat-value">{stats?.totalEpisodes || 0}</span>
                <span className="stat-label">Tổng số tập phim</span>
              </div>
            </div>

            <div className="admin-stat-card glass">
              <div className="stat-icon-wrap users">
                <Users size={24} />
              </div>
              <div className="stat-details">
                <span className="stat-value">{stats?.totalUsers || 0}</span>
                <span className="stat-label">Người dùng đã đăng ký</span>
              </div>
            </div>

            <div className="admin-stat-card glass">
              <div className="stat-icon-wrap comments">
                <MessageSquare size={24} />
              </div>
              <div className="stat-details">
                <span className="stat-value">{stats?.totalComments || 0}</span>
                <span className="stat-label">Bình luận</span>
              </div>
            </div>

            <div className="admin-stat-card glass">
              <div className="stat-icon-wrap ratings">
                <Star size={24} />
              </div>
              <div className="stat-details">
                <span className="stat-value">{stats?.totalRatings || 0}</span>
                <span className="stat-label">Lượt đánh giá</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab: KKPhim Import & Sync */}
      {activeTab === 'import' && (
        <div className="admin-tab-content">
          <div className="admin-import-grid">
            {/* 1. Bulk Sync Section */}
            <div className="admin-card glass sync-card">
              <div className="admin-card-header-icon">
                <RefreshCw size={22} className={syncLoading ? 'spin-icon' : ''} color="#38bdf8" />
                <div>
                  <h3 className="admin-card-title">Đồng Bộ Hàng Loạt (Bulk Sync)</h3>
                  <p className="admin-card-subtitle">
                    Tự động quét các trang danh mục KKPhim, lọc phim tiếng Anh và chỉ lưu bản Vietsub vào SQL Server.
                  </p>
                </div>
              </div>

              <form onSubmit={handleSyncKKPhim} className="sync-form">
                <div className="sync-form-row">
                  <div className="form-group flex-2">
                    <label className="form-label">Danh mục nguồn:</label>
                    <select
                      className="form-select"
                      value={syncSource}
                      onChange={(e) => setSyncSource(e.target.value)}
                      disabled={syncLoading}
                    >
                      <option value="au-my">Âu Mỹ (Phim Tiếng Anh / US-UK-CA-AU) ★ Khuyến nghị</option>
                      <option value="danh-sach-moi">Phim Mới Cập Nhật (Toàn bộ)</option>
                      <option value="phim-le">Phim Lẻ Điện Ảnh</option>
                      <option value="phim-bo">Phim Bộ Series</option>
                    </select>
                  </div>

                  <div className="form-group flex-1">
                    <label className="form-label">Từ trang:</label>
                    <input
                      type="number"
                      min="1"
                      max="1000"
                      className="form-input"
                      value={syncFromPage}
                      onChange={(e) => setSyncFromPage(Math.max(1, parseInt(e.target.value) || 1))}
                      disabled={syncLoading}
                    />
                  </div>

                  <div className="form-group flex-1">
                    <label className="form-label">Đến trang:</label>
                    <input
                      type="number"
                      min="1"
                      max="1000"
                      className="form-input"
                      value={syncToPage}
                      onChange={(e) => setSyncToPage(Math.max(1, parseInt(e.target.value) || 1))}
                      disabled={syncLoading}
                    />
                  </div>
                </div>

                <div className="sync-checkboxes">
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={syncEnglishOnly}
                      onChange={(e) => setSyncEnglishOnly(e.target.checked)}
                      disabled={syncLoading}
                    />
                    <span>Chỉ phim Tiếng Anh (Hoa Kỳ, Anh, Canada, Úc, New Zealand...)</span>
                  </label>
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={syncVietsubOnly}
                      onChange={(e) => setSyncVietsubOnly(e.target.checked)}
                      disabled={syncLoading}
                    />
                    <span>Chỉ bản Vietsub (Loại bỏ Thuyết Minh & Lồng Tiếng)</span>
                  </label>
                </div>

                <div className="sync-action-row">
                  <button
                    type="submit"
                    className="btn-primary sync-submit-btn"
                    disabled={syncLoading}
                  >
                    <RefreshCw size={16} className={syncLoading ? 'spin-icon' : ''} />
                    <span>{syncLoading ? 'Đang đồng bộ từ KKPhim...' : 'Bắt Đầu Đồng Bộ'}</span>
                  </button>
                  <span className="sync-note">
                    * Không tải video về máy. Chỉ lưu siêu dữ liệu và URL luồng phát Vietsub HLS (.m3u8).
                  </span>
                </div>
              </form>

              {syncError && (
                <div className="import-alert error">
                  <AlertCircle size={18} />
                  <span>{syncError}</span>
                </div>
              )}

              {syncResult && (
                <div className="sync-results-box glass">
                  <div className="sync-results-header">
                    <CheckCircle size={20} color="#34d399" />
                    <h4>Kết Quả Đồng Bộ Danh Mục</h4>
                    <span className="sync-duration">Thời gian: {syncResult.executionDuration}</span>
                  </div>

                  <div className="sync-stats-grid">
                    <div className="sync-stat-item">
                      <span className="stat-num">{syncResult.pagesProcessed}</span>
                      <span className="stat-text">Trang đã quét</span>
                    </div>
                    <div className="sync-stat-item">
                      <span className="stat-num">{syncResult.moviesDiscovered}</span>
                      <span className="stat-text">Phim tìm thấy</span>
                    </div>
                    <div className="sync-stat-item highlight">
                      <span className="stat-num">{syncResult.englishMovies}</span>
                      <span className="stat-text">Phim Tiếng Anh</span>
                    </div>
                    <div className="sync-stat-item highlight">
                      <span className="stat-num">{syncResult.vietsubAvailable}</span>
                      <span className="stat-text">Có Vietsub</span>
                    </div>
                    <div className="sync-stat-item success">
                      <span className="stat-num">{syncResult.imported}</span>
                      <span className="stat-text">Thêm mới</span>
                    </div>
                    <div className="sync-stat-item info">
                      <span className="stat-num">{syncResult.updated}</span>
                      <span className="stat-text">Cập nhật</span>
                    </div>
                    <div className="sync-stat-item muted">
                      <span className="stat-num">{syncResult.skipped}</span>
                      <span className="stat-text">Bỏ qua</span>
                    </div>
                    <div className="sync-stat-item error">
                      <span className="stat-num">{syncResult.errors}</span>
                      <span className="stat-text">Lỗi</span>
                    </div>
                  </div>

                  {syncResult.items && syncResult.items.length > 0 && (
                    <div className="sync-log-container">
                      <h5 className="sync-log-title">Nhật ký xử lý chi tiết:</h5>
                      <div className="sync-log-list no-scrollbar">
                        {syncResult.items.map((it, idx) => (
                          <div key={idx} className={`sync-log-row ${it.status}`}>
                            <span className={`status-pill ${it.status}`}>
                              {it.status === 'imported' ? 'MỚI' : it.status === 'updated' ? 'CẬP NHẬT' : 'BỎ QUA'}
                            </span>
                            <span className="log-title">{it.title}</span>
                            <span className="log-slug">({it.slug})</span>
                            <span className="log-reason">{it.reason}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* 2. KKPhim Search & Quick Import */}
            <div className="admin-card glass search-import-card">
              <div className="admin-card-header-icon">
                <Search size={22} color="#f59e0b" />
                <div>
                  <h3 className="admin-card-title">Tìm Kiếm & Khám Phá KKPhim</h3>
                  <p className="admin-card-subtitle">
                    Tìm kiếm tên phim trên kho KKPhim để kiểm tra thông tin và nhập nhanh.
                  </p>
                </div>
              </div>

              <form onSubmit={handleSearchKKPhim} className="import-form">
                <input
                  type="text"
                  placeholder="Nhập tên phim tiếng Anh hoặc tiếng Việt (vd: Oppenheimer, Batman...)"
                  value={searchKeyword}
                  onChange={(e) => setSearchKeyword(e.target.value)}
                  className="import-input"
                />
                <button
                  type="submit"
                  className="btn-secondary"
                  disabled={searchLoading || !searchKeyword.trim()}
                >
                  <Search size={16} />
                  <span>{searchLoading ? 'Đang tìm...' : 'Tìm Kiếm'}</span>
                </button>
              </form>

              {searchError && (
                <div className="import-alert error">
                  <AlertCircle size={16} />
                  <span>{searchError}</span>
                </div>
              )}

              {searchResults.length > 0 && (
                <div className="search-results-list no-scrollbar">
                  {searchResults.map((item) => (
                    <div key={item.slug} className="search-result-row">
                      <img
                        src={item.poster_url || item.thumb_url}
                        alt=""
                        referrerPolicy="no-referrer"
                        className="search-result-thumb"
                        onError={(e) => { e.target.style.display = 'none'; }}
                      />
                      <div className="search-result-info">
                        <div className="search-result-name">{item.name}</div>
                        <div className="search-result-origin">{item.origin_name} ({item.year || 'N/A'})</div>
                        <div className="search-result-meta">
                          <span className="badge-tag">{item.type || 'phim'}</span>
                          <span className="badge-quality">{item.episode_current || 'Full'}</span>
                        </div>
                      </div>
                      <button
                        className="btn-primary btn-sm"
                        onClick={() => handleQuickImport(item.slug)}
                        disabled={importingSlugMap[item.slug]}
                      >
                        <Download size={14} />
                        <span>{importingSlugMap[item.slug] ? 'Đang nhập...' : 'Nhập Phim'}</span>
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* 3. Single Movie Import by Slug */}
            <div className="admin-card glass single-import-card">
              <div className="admin-card-header-icon">
                <Download size={22} color="#a855f7" />
                <div>
                  <h3 className="admin-card-title">Nhập Đơn Lẻ Theo Slug</h3>
                  <p className="admin-card-subtitle">
                    Nhập chính xác slug KKPhim (ví dụ: <code>oppenheimer</code>, <code>the-dark-knight</code>).
                  </p>
                </div>
              </div>

              <form onSubmit={handleImportKKPhim} className="single-import-form">
                <div className="single-import-input-row">
                  <input
                    type="text"
                    placeholder="Nhập slug phim từ KKPhim..."
                    value={importSlug}
                    onChange={(e) => setImportSlug(e.target.value)}
                    className="import-input"
                    required
                  />
                  <button
                    type="submit"
                    className="btn-primary import-submit-btn"
                    disabled={importLoading || !importSlug.trim()}
                  >
                    <Download size={16} />
                    <span>{importLoading ? 'Đang nhập...' : 'Tiến Hành Nhập'}</span>
                  </button>
                </div>

                <div className="sync-checkboxes single">
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={importEnglishOnly}
                      onChange={(e) => setImportEnglishOnly(e.target.checked)}
                      disabled={importLoading}
                    />
                    <span>Lọc phim Tiếng Anh</span>
                  </label>
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={importVietsubOnly}
                      onChange={(e) => setImportVietsubOnly(e.target.checked)}
                      disabled={importLoading}
                    />
                    <span>Chỉ lấy nguồn Vietsub</span>
                  </label>
                </div>
              </form>

              {importError && (
                <div className="import-alert error">
                  <AlertCircle size={16} />
                  <span>{importError}</span>
                </div>
              )}

              {importResult && (
                <div className="import-alert success">
                  <CheckCircle size={16} />
                  <div>
                    <strong>Nhập phim thành công!</strong>
                    <p>Phim: {importResult.title} ({importResult.slug})</p>
                    <p>Quốc gia: {importResult.country || 'N/A'} - Ngôn ngữ: {importResult.language || 'English'}</p>
                    <p>Số tập: {importResult.episodesCount || 0} - Bản Vietsub: {importResult.isVietsub ? 'Có' : 'Không'}</p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tab: Movies */}
      {activeTab === 'movies' && (
        <div className="admin-tab-content">
          <div className="admin-table-card glass">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Ảnh</th>
                  <th>Tên phim</th>
                  <th>Năm</th>
                  <th>Loại</th>
                  <th>Chất lượng</th>
                  <th>Điểm</th>
                </tr>
              </thead>
              <tbody>
                {moviesList.map((m) => (
                  <tr key={m.id}>
                    <td>#{m.id}</td>
                    <td>
                      <img
                        src={m.posterUrl || m.thumbnailUrl}
                        alt=""
                        referrerPolicy="no-referrer"
                        className="table-poster-thumb"
                      />
                    </td>
                    <td>
                      <strong>{m.title}</strong>
                      <div className="table-subtext">{m.slug}</div>
                    </td>
                    <td>{m.releaseYear || '-'}</td>
                    <td>
                      <span className="badge-tag">{m.movieType}</span>
                    </td>
                    <td>
                      <span className="badge-quality">{m.quality || 'HD'}</span>
                    </td>
                    <td>
                      <span className="table-rating-text">
                        ★ {m.rating ? Number(m.rating).toFixed(1) : '-'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab: Users */}
      {activeTab === 'users' && (
        <div className="admin-tab-content">
          <div className="admin-table-card glass">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Tài khoản</th>
                  <th>Email</th>
                  <th>Vai trò</th>
                  <th>Ngày tạo</th>
                </tr>
              </thead>
              <tbody>
                {usersList.map((u) => (
                  <tr key={u.id}>
                    <td>#{u.id}</td>
                    <td>
                      <strong>{u.username}</strong>
                    </td>
                    <td>{u.email}</td>
                    <td>
                      <span className={`role-badge ${u.role === 'Admin' ? 'admin' : 'user'}`}>
                        {u.role}
                      </span>
                    </td>
                    <td>{new Date(u.createdAt).toLocaleDateString('vi-VN')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab: Comments Moderation */}
      {activeTab === 'comments' && (
        <div className="admin-tab-content">
          <div className="admin-table-card glass">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Người dùng</th>
                  <th>Nội dung</th>
                  <th>Phim ID</th>
                  <th>Ngày gửi</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {commentsList.map((c) => (
                  <tr key={c.id}>
                    <td>#{c.id}</td>
                    <td>
                      <strong>{c.username}</strong>
                    </td>
                    <td className="table-comment-content">{c.content}</td>
                    <td>#{c.movieId}</td>
                    <td>{new Date(c.createdAt).toLocaleDateString('vi-VN')}</td>
                    <td>
                      <button
                        className="table-action-delete"
                        onClick={() => handleDeleteComment(c.id)}
                        title="Xóa bình luận"
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminPage;
