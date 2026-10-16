import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import {
  Play,
  Heart,
  Star,
  Calendar,
  Clock,
  Globe,
  Share2,
} from 'lucide-react';
import { movieService } from '../../services/api/movieService';
import { favoriteService } from '../../services/api/favoriteService';
import { useAuth } from '../../context/AuthContext';
import EpisodeList from '../../components/EpisodeList/EpisodeList';
import RatingWidget from '../../components/RatingWidget/RatingWidget';
import CommentSection from '../../components/CommentSection/CommentSection';
import MovieRow from '../../components/MovieRow/MovieRow';
import { ErrorState } from '../../components/Common/CommonStates';
import './MovieDetailPage.css';

const MovieDetailPage = () => {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  const [movie, setMovie] = useState(null);
  const [episodes, setEpisodes] = useState([]);
  const [similarMovies, setSimilarMovies] = useState([]);
  const [isFavorited, setIsFavorited] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchMovieDetail = async () => {
    setLoading(true);
    setError(null);
    try {
      // 1. Fetch movie by slug or ID
      const movieData = await movieService.getMovieBySlug(slug);
      setMovie(movieData);

      // 2. Fetch episodes
      try {
        const eps = await movieService.getMovieEpisodes(movieData.id);
        setEpisodes(eps || []);
      } catch (epErr) {
        console.warn('Could not fetch episodes', epErr);
      }

      // 3. Fetch similar movies
      try {
        const simRes = await movieService.getSimilarMovies(movieData.id, { pageSize: 10 });
        setSimilarMovies(simRes?.items || []);
      } catch (simErr) {
        console.warn('Could not fetch similar movies', simErr);
      }

      // 4. Check favorite status
      if (isAuthenticated) {
        favoriteService
          .checkFavoriteStatus(movieData.id)
          .then((res) => setIsFavorited(res?.isFavorite || false))
          .catch(() => setIsFavorited(false));
      }
    } catch (err) {
      console.error('Error loading movie detail', err);
      setError('Không tìm thấy bộ phim hoặc phim đã bị gỡ khỏi hệ thống.');
    } finally {
      setLoading(false);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  useEffect(() => {
    fetchMovieDetail();
  }, [slug, isAuthenticated]);

  const handleToggleFavorite = async () => {
    if (!isAuthenticated) {
      navigate('/login?redirect=' + encodeURIComponent(window.location.pathname));
      return;
    }
    if (!movie) return;

    try {
      if (isFavorited) {
        await favoriteService.removeFavorite(movie.id);
        setIsFavorited(false);
      } else {
        await favoriteService.addFavorite(movie.id);
        setIsFavorited(true);
      }
    } catch (err) {
      console.error('Failed to toggle favorite', err);
    }
  };

  const handleShare = () => {
    if (navigator.share) {
      navigator.share({
        title: movie?.title,
        url: window.location.href,
      }).catch(() => {});
    } else {
      navigator.clipboard.writeText(window.location.href);
      alert('Đã sao chép liên kết vào bộ nhớ tạm!');
    }
  };

  if (loading) {
    return (
      <div className="detail-loading-page container">
        <div className="skeleton detail-hero-skeleton"></div>
      </div>
    );
  }

  if (error || !movie) {
    return (
      <div className="detail-error-page container">
        <ErrorState message={error} onRetry={fetchMovieDetail} />
      </div>
    );
  }

  const backdrop =
    movie.thumbnailUrl ||
    movie.posterUrl ||
    'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=1600&auto=format&fit=crop&q=80';

  const poster =
    movie.posterUrl ||
    movie.thumbnailUrl ||
    'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=600&auto=format&fit=crop&q=80';

  const movieYear =
    movie.releaseYear ||
    (movie.createdAt ? new Date(movie.createdAt).getFullYear() : null);
  const ratingValue = movie.rating != null ? Number(movie.rating).toFixed(1) : null;
  const firstEpId = episodes.length > 0 ? episodes[0].id : null;
  const watchUrl = firstEpId
    ? `/watch/${movie.slug || movie.id}?ep=${firstEpId}`
    : `/watch/${movie.slug || movie.id}`;

  return (
    <div className="movie-detail-page">
      {/* Backdrop Header */}
      <div className="movie-detail-backdrop">
        <img
          src={backdrop}
          alt={movie.title}
          referrerPolicy="no-referrer"
          className="movie-detail-backdrop-img"
          onError={(e) => {
            e.target.onerror = null;
            e.target.src = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=1600&auto=format&fit=crop&q=80';
          }}
        />
        <div className="backdrop-overlay-dark"></div>
        <div className="backdrop-overlay-bottom"></div>
      </div>

      <div className="movie-detail-content container">
        {/* Main Info Block: Poster + Details */}
        <div className="movie-detail-main">
          {/* Poster Column */}
          <div className="movie-detail-poster-col">
            <div className="movie-detail-poster-wrapper">
              <img
                src={poster}
                alt={movie.title}
                referrerPolicy="no-referrer"
                className="movie-detail-poster"
                onError={(e) => {
                  e.target.onerror = null;
                  e.target.src = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=600&auto=format&fit=crop&q=80';
                }}
              />
              {movie.quality && (
                <div className="poster-badge-top">
                  <span className="badge-quality">{movie.quality}</span>
                </div>
              )}
            </div>

            {/* Quick CTAs under poster */}
            <div className="movie-detail-poster-actions">
              <Link to={watchUrl} className="btn-primary detail-watch-btn">
                <Play size={18} fill="currentColor" />
                <span>Xem Phim</span>
              </Link>

              <div className="poster-secondary-buttons">
                <button
                  className={`btn-secondary detail-action-btn ${isFavorited ? 'favorited' : ''}`}
                  onClick={handleToggleFavorite}
                  title={isFavorited ? 'Đã lưu vào danh sách' : 'Lưu phim'}
                >
                  <Heart
                    size={16}
                    fill={isFavorited ? '#ff2a54' : 'none'}
                    color={isFavorited ? '#ff2a54' : 'currentColor'}
                  />
                  <span>{isFavorited ? 'Đã Lưu' : 'Yêu Thích'}</span>
                </button>

                <button
                  className="btn-secondary detail-action-btn"
                  onClick={handleShare}
                  title="Chia sẻ phim"
                >
                  <Share2 size={16} />
                  <span>Chia Sẻ</span>
                </button>
              </div>
            </div>
          </div>

          {/* Details Column */}
          <div className="movie-detail-info-col">
            <h1 className="movie-detail-title">{movie.title}</h1>
            {movie.originalTitle && (
              <h2 className="movie-detail-original-title">{movie.originalTitle}</h2>
            )}

            {/* Meta Tags Bar */}
            <div className="movie-detail-meta-bar">
              {ratingValue && ratingValue > 0 && (
                <span className="badge-rating detail-meta-pill">
                  <Star size={13} fill="currentColor" />
                  {ratingValue}
                </span>
              )}
              {movieYear && (
                <span className="detail-meta-pill">
                  <Calendar size={13} />
                  {movieYear}
                </span>
              )}
              {movie.duration && (
                <span className="detail-meta-pill">
                  <Clock size={13} />
                  {movie.duration} phút
                </span>
              )}
              {movie.country && (
                <span className="detail-meta-pill">
                  <Globe size={13} />
                  {movie.country}
                </span>
              )}
              {movie.status && (
                <span className="detail-meta-pill status">
                  {movie.status}
                </span>
              )}
              {movie.movieType && (
                <span className="badge-tag">
                  {movie.movieType === 'series'
                    ? 'Phim bộ'
                    : movie.movieType === 'single'
                    ? 'Phim lẻ'
                    : movie.movieType}
                </span>
              )}
            </div>

            {/* Genres Pills */}
            {movie.genres && movie.genres.length > 0 && (
              <div className="movie-detail-genres">
                {movie.genres.map((g) => (
                  <Link
                    key={g.id || g.name}
                    to={`/movies?genre=${encodeURIComponent(g.name)}`}
                    className="genre-tag-link"
                  >
                    {g.name}
                  </Link>
                ))}
              </div>
            )}

            {/* Synopsis / Description */}
            <div className="movie-detail-synopsis">
              <h3 className="synopsis-heading">Nội dung phim</h3>
              <p className="synopsis-text">
                {movie.description
                  ? movie.description.replace(/<[^>]*>?/gm, '')
                  : 'Nội dung phim đang được cập nhật.'}
              </p>
            </div>

            {/* Interactive Rating Section */}
            <RatingWidget movieId={movie.id} onRatingUpdated={fetchMovieDetail} />

            {/* Episode List */}
            {episodes.length > 0 && (
              <EpisodeList
                episodes={episodes}
                onSelectEpisode={(ep) =>
                  navigate(`/watch/${movie.slug || movie.id}?ep=${ep.id}`)
                }
              />
            )}
          </div>
        </div>

        {/* Similar / Related Movies */}
        {similarMovies.length > 0 && (
          <div className="movie-detail-similar-section">
            <MovieRow
              title="Phim Tương Tự"
              subtitle="Những bộ phim có cùng thể loại hoặc phong cách"
              movies={similarMovies}
            />
          </div>
        )}

        {/* Comments Section */}
        <div className="movie-detail-comments-wrapper">
          <CommentSection movieId={movie.id} />
        </div>
      </div>
    </div>
  );
};

export default MovieDetailPage;
