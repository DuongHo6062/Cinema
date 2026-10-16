import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Play, Info, Star, Calendar, Clock, ChevronLeft, ChevronRight, Bookmark } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { favoriteService } from '../../services/api/favoriteService';
import './HeroBanner.css';

const HeroBanner = ({ movies = [] }) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFavorited, setIsFavorited] = useState(false);
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const currentMovie = movies[currentIndex] || movies[0];

  useEffect(() => {
    if (!movies || movies.length <= 1) return;
    const interval = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % movies.length);
    }, 8000);
    return () => clearInterval(interval);
  }, [movies]);

  useEffect(() => {
    if (currentMovie && isAuthenticated) {
      favoriteService
        .checkFavoriteStatus(currentMovie.id)
        .then((res) => setIsFavorited(res?.isFavorite || false))
        .catch(() => setIsFavorited(false));
    } else {
      setIsFavorited(false);
    }
  }, [currentMovie, isAuthenticated]);

  if (!currentMovie) return null;

  const backdrop =
    currentMovie.thumbnailUrl ||
    currentMovie.posterUrl ||
    'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=1600&auto=format&fit=crop&q=80';

  const movieYear =
    currentMovie.releaseYear ||
    (currentMovie.createdAt ? new Date(currentMovie.createdAt).getFullYear() : null);
  const ratingValue =
    currentMovie.rating != null ? Number(currentMovie.rating).toFixed(1) : null;
  const movieSlug = currentMovie.slug || currentMovie.id;

  const handleToggleFavorite = async () => {
    if (!isAuthenticated) {
      navigate('/login?redirect=' + encodeURIComponent(window.location.pathname));
      return;
    }
    try {
      if (isFavorited) {
        await favoriteService.removeFavorite(currentMovie.id);
        setIsFavorited(false);
      } else {
        await favoriteService.addFavorite(currentMovie.id);
        setIsFavorited(true);
      }
    } catch (err) {
      console.error('Failed to toggle hero favorite', err);
    }
  };

  const handlePrev = () => {
    setCurrentIndex((prev) => (prev - 1 + movies.length) % movies.length);
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev + 1) % movies.length);
  };

  return (
    <div className="hero-banner">
      {/* Background Image Container */}
      <div className="hero-banner-bg">
        <img
          src={backdrop}
          alt={currentMovie.title}
          referrerPolicy="no-referrer"
          className="hero-banner-bg-img"
          onError={(e) => {
            e.target.onerror = null;
            e.target.src = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=1600&auto=format&fit=crop&q=80';
          }}
        />
        <div className="hero-banner-overlay-dark"></div>
        <div className="hero-banner-overlay-bottom"></div>
        <div className="hero-banner-overlay-radial"></div>
      </div>

      <div className="hero-banner-content container">
        <div className="hero-banner-info">
          {/* Badges / Meta */}
          <div className="hero-banner-badges">
            <span className="badge-quality">
              {currentMovie.quality || 'FHD'}
            </span>
            {currentMovie.movieType && (
              <span className="badge-tag">
                {currentMovie.movieType === 'series'
                  ? 'Phim bộ'
                  : currentMovie.movieType === 'single'
                  ? 'Phim lẻ'
                  : currentMovie.movieType}
              </span>
            )}
            {ratingValue && ratingValue > 0 && (
              <span className="badge-rating">
                <Star size={13} fill="currentColor" />
                {ratingValue}
              </span>
            )}
            {movieYear && (
              <span className="hero-banner-meta-item">
                <Calendar size={13} />
                {movieYear}
              </span>
            )}
            {currentMovie.duration && (
              <span className="hero-banner-meta-item">
                <Clock size={13} />
                {currentMovie.duration} phút
              </span>
            )}
          </div>

          {/* Title */}
          <h1 className="hero-banner-title">{currentMovie.title}</h1>
          {currentMovie.originalTitle && (
            <h3 className="hero-banner-original-title">
              {currentMovie.originalTitle}
            </h3>
          )}

          {/* Genres */}
          {currentMovie.genres && currentMovie.genres.length > 0 && (
            <div className="hero-banner-genres">
              {currentMovie.genres.map((g) => (
                <span key={g.id || g.name} className="hero-genre-pill">
                  {g.name}
                </span>
              ))}
            </div>
          )}

          {/* Description */}
          {currentMovie.description && (
            <p className="hero-banner-description">
              {currentMovie.description.replace(/<[^>]*>?/gm, '')}
            </p>
          )}

          {/* CTA Buttons */}
          <div className="hero-banner-actions">
            <Link to={`/watch/${movieSlug}`} className="btn-primary hero-btn-watch">
              <Play size={18} fill="currentColor" />
              <span>Xem Ngay</span>
            </Link>

            <Link to={`/movie/${movieSlug}`} className="btn-secondary hero-btn-detail">
              <Info size={18} />
              <span>Chi Tiết</span>
            </Link>

            <button
              className={`btn-icon hero-btn-fav ${isFavorited ? 'favorited' : ''}`}
              onClick={handleToggleFavorite}
              title={isFavorited ? 'Bỏ lưu' : 'Lưu vào danh sách'}
              aria-label="Yêu thích"
            >
              <Bookmark
                size={18}
                fill={isFavorited ? '#ff2a54' : 'none'}
                color={isFavorited ? '#ff2a54' : '#fff'}
              />
            </button>
          </div>
        </div>

        {/* Carousel indicators if multiple */}
        {movies.length > 1 && (
          <div className="hero-carousel-controls">
            <button
              className="hero-arrow-btn prev"
              onClick={handlePrev}
              aria-label="Phim trước"
            >
              <ChevronLeft size={20} />
            </button>
            <div className="hero-dots">
              {movies.map((_, idx) => (
                <button
                  key={idx}
                  className={`hero-dot ${idx === currentIndex ? 'active' : ''}`}
                  onClick={() => setCurrentIndex(idx)}
                  aria-label={`Chuyển tới phim ${idx + 1}`}
                />
              ))}
            </div>
            <button
              className="hero-arrow-btn next"
              onClick={handleNext}
              aria-label="Phim kế tiếp"
            >
              <ChevronRight size={20} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default HeroBanner;
