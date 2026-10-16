import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Play, Star, Heart, Clock, Film } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { favoriteService } from '../../services/api/favoriteService';
import './MovieCard.css';

const MovieCard = ({
  movie,
  initialFavorite = false,
  onFavoriteChange,
  showProgress = false,
  progressPercentage = 0,
  progressSeconds = 0,
}) => {
  const { isAuthenticated } = useAuth();
  const [isFav, setIsFav] = useState(initialFavorite);
  const [favLoading, setFavLoading] = useState(false);
  const [fallbackTried, setFallbackTried] = useState(false);
  const [imageFailed, setImageFailed] = useState(false);

  if (!movie) return null;

  const posterImage =
    movie.posterUrl ||
    movie.thumbnailUrl ||
    'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop&q=80';

  const movieYear = movie.releaseYear || (movie.createdAt ? new Date(movie.createdAt).getFullYear() : null);
  const ratingValue = movie.rating != null ? Number(movie.rating).toFixed(1) : null;
  const movieSlug = movie.slug || movie.id;

  const handleToggleFavorite = async (e) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      window.location.href = '/login?redirect=' + encodeURIComponent(window.location.pathname);
      return;
    }

    if (favLoading) return;
    setFavLoading(true);

    try {
      if (isFav) {
        await favoriteService.removeFavorite(movie.id);
        setIsFav(false);
        if (onFavoriteChange) onFavoriteChange(movie.id, false);
      } else {
        await favoriteService.addFavorite(movie.id);
        setIsFav(true);
        if (onFavoriteChange) onFavoriteChange(movie.id, true);
      }
    } catch (err) {
      console.error('Failed to toggle favorite', err);
    } finally {
      setFavLoading(false);
    }
  };

  const handleImageError = (e) => {
    if (!fallbackTried) {
      setFallbackTried(true);
      e.target.onerror = () => {
        setImageFailed(true);
      };
      e.target.src = 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500&auto=format&fit=crop&q=80';
    } else {
      setImageFailed(true);
    }
  };

  const formatTime = (secs) => {
    const mins = Math.floor(secs / 60);
    return `${mins}p`;
  };

  return (
    <div className="movie-card">
      <Link to={`/movie/${movieSlug}`} className="movie-card-link" aria-label={movie.title}>
        <div className="movie-card-poster-wrapper">
          {imageFailed ? (
            <div className="movie-card-placeholder">
              <div className="movie-card-placeholder-icon">
                <Film size={34} />
              </div>
              <span className="movie-card-placeholder-title">{movie.title}</span>
            </div>
          ) : (
            <img
              src={posterImage}
              alt={movie.title}
              loading="lazy"
              referrerPolicy="no-referrer"
              className="movie-card-poster"
              onError={handleImageError}
            />
          )}

          {/* Badges */}
          <div className="movie-card-badges">
            {movie.quality && <span className="badge-quality">{movie.quality}</span>}
            {ratingValue && ratingValue > 0 && (
              <span className="badge-rating">
                <Star size={11} fill="currentColor" />
                {ratingValue}
              </span>
            )}
          </div>

          {/* Favorite Quick Action */}
          <button
            className={`movie-card-fav-btn ${isFav ? 'active' : ''}`}
            onClick={handleToggleFavorite}
            title={isFav ? 'Bỏ lưu' : 'Lưu vào danh sách yêu thích'}
            aria-label="Yêu thích"
          >
            <Heart size={16} fill={isFav ? '#ff2a54' : 'none'} color={isFav ? '#ff2a54' : '#ffffff'} />
          </button>

          {/* Hover overlay with Play button */}
          <div className="movie-card-overlay">
            <div className="movie-card-play-icon">
              <Play size={22} fill="currentColor" />
            </div>
            {movie.duration && (
              <span className="movie-card-duration">
                <Clock size={12} /> {movie.duration} phút
              </span>
            )}
          </div>

          {/* Watch Progress Bar if applicable */}
          {showProgress && progressPercentage > 0 && (
            <div className="movie-card-progress-bar">
              <div
                className="movie-card-progress-fill"
                style={{ width: `${Math.min(100, Math.max(0, progressPercentage))}%` }}
              ></div>
            </div>
          )}
        </div>

        {/* Card info below poster */}
        <div className="movie-card-info">
          <h4 className="movie-card-title" title={movie.title}>
            {movie.title}
          </h4>
          <div className="movie-card-meta">
            {movieYear && <span className="movie-card-year">{movieYear}</span>}
            {movie.movieType && (
              <span className="movie-card-type">
                {movie.movieType === 'series' ? 'Phim bộ' : movie.movieType === 'single' ? 'Phim lẻ' : movie.movieType}
              </span>
            )}
            {showProgress && progressSeconds > 0 && (
              <span className="movie-card-progress-text">Đã xem {formatTime(progressSeconds)}</span>
            )}
          </div>
        </div>
      </Link>
    </div>
  );
};

export default MovieCard;
