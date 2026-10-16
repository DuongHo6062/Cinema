import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Heart, Compass } from 'lucide-react';
import MovieGrid from '../../components/MovieGrid/MovieGrid';
import { MovieGridSkeleton } from '../../components/Loading/Skeletons';
import { EmptyState, ErrorState } from '../../components/Common/CommonStates';
import { favoriteService } from '../../services/api/favoriteService';
import { useAuth } from '../../context/AuthContext';
import './FavoritesPage.css';

const FavoritesPage = () => {
  const { isAuthenticated, loading: authLoading } = useAuth();
  const navigate = useNavigate();

  const [favorites, setFavorites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchFavorites = async () => {
    if (!isAuthenticated) return;
    setLoading(true);
    setError(null);
    try {
      const res = await favoriteService.getFavorites(1, 60);
      setFavorites(res?.items || []);
    } catch (err) {
      console.error('Error fetching favorites', err);
      setError('Không thể tải danh sách phim yêu thích.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate('/login?redirect=/favorites');
      return;
    }
    if (isAuthenticated) {
      fetchFavorites();
    }
  }, [isAuthenticated, authLoading]);

  const handleFavoriteChange = (movieId, isFav) => {
    if (!isFav) {
      setFavorites((prev) => prev.filter((m) => m.id !== movieId));
    }
  };

  if (authLoading || loading) {
    return (
      <div className="favorites-page container">
        <div className="favorites-header">
          <h1 className="favorites-title">Phim Yêu Thích</h1>
        </div>
        <MovieGridSkeleton count={12} />
      </div>
    );
  }

  if (error) {
    return (
      <div className="favorites-page container">
        <ErrorState message={error} onRetry={fetchFavorites} />
      </div>
    );
  }

  return (
    <div className="favorites-page container">
      <div className="favorites-header">
        <div>
          <h1 className="favorites-title">
            <Heart size={26} fill="#ff2a54" color="#ff2a54" /> Phim Yêu Thích
          </h1>
          <p className="favorites-subtitle">
            Danh sách những bộ phim bạn đã đánh dấu để theo dõi
          </p>
        </div>
        <span className="favorites-count">
          {favorites.length} bộ phim
        </span>
      </div>

      <MovieGrid
        movies={favorites}
        emptyTitle="Chưa có phim yêu thích"
        emptyMessage="Bạn chưa lưu bộ phim nào. Hãy khám phá và bấm biểu tượng trái tim để lưu phim bạn yêu thích!"
        emptyActionText="Khám phá ngay"
        onEmptyAction={() => navigate('/movies')}
        renderItem={(movie) => (
          <div key={movie.id} className="fav-item-wrap">
            <MovieCard
              movie={movie}
              initialFavorite={true}
              onFavoriteChange={handleFavoriteChange}
            />
          </div>
        )}
      />
    </div>
  );
};

export default FavoritesPage;
