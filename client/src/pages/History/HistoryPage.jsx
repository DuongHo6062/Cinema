import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Clock, Play, Trash2, Calendar } from 'lucide-react';
import { watchHistoryService } from '../../services/api/watchHistoryService';
import { useAuth } from '../../context/AuthContext';
import { EmptyState, ErrorState } from '../../components/Common/CommonStates';
import './HistoryPage.css';

const HistoryPage = () => {
  const { isAuthenticated, loading: authLoading } = useAuth();
  const navigate = useNavigate();

  const [historyItems, setHistoryItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchHistory = async () => {
    if (!isAuthenticated) return;
    setLoading(true);
    setError(null);
    try {
      const res = await watchHistoryService.getHistory(1, 50);
      setHistoryItems(res?.items || []);
    } catch (err) {
      console.error('Failed to fetch watch history', err);
      setError('Không thể tải lịch sử xem phim.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate('/login?redirect=/history');
      return;
    }
    if (isAuthenticated) {
      fetchHistory();
    }
  }, [isAuthenticated, authLoading]);

  const handleDeleteItem = async (episodeId, e) => {
    e.preventDefault();
    e.stopPropagation();
    try {
      await watchHistoryService.deleteHistory(episodeId);
      setHistoryItems((prev) => prev.filter((item) => item.episodeId !== episodeId));
    } catch (err) {
      alert('Không thể xóa mục này khỏi lịch sử.');
    }
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  const formatSeconds = (sec) => {
    if (!sec) return '00:00';
    const m = Math.floor(sec / 60);
    const s = Math.floor(sec % 60);
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  };

  if (authLoading || loading) {
    return (
      <div className="history-page container">
        <div className="history-header">
          <h1 className="history-title">Lịch Sử Xem</h1>
        </div>
        <div className="skeleton line-title" style={{ marginBottom: 20 }}></div>
        <div className="skeleton line-title"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="history-page container">
        <ErrorState message={error} onRetry={fetchHistory} />
      </div>
    );
  }

  return (
    <div className="history-page container">
      <div className="history-header">
        <div>
          <h1 className="history-title">
            <Clock size={26} color="#ff2a54" /> Lịch Sử Xem Phim
          </h1>
          <p className="history-subtitle">
            Tiếp tục xem các bộ phim và tập bạn đã xem dở
          </p>
        </div>
        <span className="history-count">{historyItems.length} mục</span>
      </div>

      {historyItems.length === 0 ? (
        <EmptyState
          icon={Clock}
          title="Lịch sử xem đang trống"
          message="Bạn chưa xem bộ phim nào gần đây. Hãy chọn một bộ phim yêu thích và thưởng thức ngay!"
          actionText="Xem phim ngay"
          onAction={() => navigate('/movies')}
        />
      ) : (
        <div className="history-list">
          {historyItems.map((item) => {
            const watchUrl = `/watch/${item.movieSlug || item.movieId}?ep=${item.episodeId}`;
            const percent = Math.min(100, Math.max(0, item.progressPercentage || 0));

            return (
              <div key={item.episodeId} className="history-card glass">
                {/* Poster Thumbnail */}
                <Link to={watchUrl} className="history-thumb-link">
                  <div className="history-thumb-wrap">
                    <img
                      src={
                        item.posterUrl ||
                        'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=400&auto=format&fit=crop&q=80'
                      }
                      alt={item.movieTitle}
                      referrerPolicy="no-referrer"
                      className="history-thumb-img"
                    />
                    <div className="history-thumb-overlay">
                      <Play size={24} fill="currentColor" />
                    </div>
                    {/* Progress Bar on Thumbnail */}
                    <div className="history-thumb-progress">
                      <div
                        className="history-thumb-fill"
                        style={{ width: `${percent}%` }}
                      ></div>
                    </div>
                  </div>
                </Link>

                {/* Details */}
                <div className="history-card-details">
                  <div className="history-card-top">
                    <Link to={watchUrl} className="history-movie-title">
                      {item.movieTitle}
                    </Link>
                    <button
                      className="history-delete-btn"
                      onClick={(e) => handleDeleteItem(item.episodeId, e)}
                      title="Xóa khỏi lịch sử"
                      aria-label="Xóa khỏi lịch sử"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>

                  <div className="history-card-ep-badge">
                    {item.episodeName || `Tập ${item.episodeNumber || item.episodeId}`}
                  </div>

                  <div className="history-card-meta">
                    <span className="history-progress-text">
                      Đã xem {formatSeconds(item.progressSeconds)} / {formatSeconds(item.durationSeconds)} ({percent.toFixed(0)}%)
                    </span>
                    <span className="history-date">
                      <Calendar size={12} /> {formatDate(item.lastWatchedAt)}
                    </span>
                  </div>

                  <div className="history-actions">
                    <Link to={watchUrl} className="btn-primary history-play-btn">
                      <Play size={14} fill="currentColor" />
                      <span>Xem tiếp</span>
                    </Link>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default HistoryPage;
