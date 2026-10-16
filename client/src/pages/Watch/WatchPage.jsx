import React, { useState, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate, Link } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  Info,
  Calendar,
  Star,
  Film,
  List,
} from 'lucide-react';
import VideoPlayer from '../../components/VideoPlayer/VideoPlayer';
import EpisodeList from '../../components/EpisodeList/EpisodeList';
import CommentSection from '../../components/CommentSection/CommentSection';
import RatingWidget from '../../components/RatingWidget/RatingWidget';
import MovieRow from '../../components/MovieRow/MovieRow';
import { ErrorState } from '../../components/Common/CommonStates';
import { movieService } from '../../services/api/movieService';
import { episodeService } from '../../services/api/episodeService';
import { watchHistoryService } from '../../services/api/watchHistoryService';
import { useAuth } from '../../context/AuthContext';
import './WatchPage.css';

const WatchPage = () => {
  const { slug } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  const [movie, setMovie] = useState(null);
  const [episodes, setEpisodes] = useState([]);
  const [currentEpisode, setCurrentEpisode] = useState(null);
  const [sources, setSources] = useState([]);
  const [similarMovies, setSimilarMovies] = useState([]);
  const [resumeTime, setResumeTime] = useState(0);

  const [loading, setLoading] = useState(true);
  const [loadingSources, setLoadingSources] = useState(false);
  const [error, setError] = useState(null);

  const epParam = searchParams.get('ep');

  // Load movie and episodes
  useEffect(() => {
    const loadMovieAndEpisodes = async () => {
      setLoading(true);
      setError(null);
      try {
        const movieData = await movieService.getMovieBySlug(slug);
        setMovie(movieData);

        const eps = await movieService.getMovieEpisodes(movieData.id);
        setEpisodes(eps || []);

        // Pick episode
        let selectedEp = null;
        if (epParam) {
          selectedEp = eps.find((e) => String(e.id) === epParam);
        }
        if (!selectedEp && eps.length > 0) {
          selectedEp = eps[0];
        }
        setCurrentEpisode(selectedEp);

        // Fetch similar
        try {
          const sim = await movieService.getSimilarMovies(movieData.id, { pageSize: 8 });
          setSimilarMovies(sim?.items || []);
        } catch (simErr) {
          console.warn('Could not load similar movies in watch page', simErr);
        }
      } catch (err) {
        console.error('Failed to load movie for player', err);
        setError('Không thể tải thông tin bộ phim.');
      } finally {
        setLoading(false);
      }
    };

    loadMovieAndEpisodes();
  }, [slug]);

  // Handle changing episode via epParam
  useEffect(() => {
    if (episodes.length > 0 && epParam) {
      const found = episodes.find((e) => String(e.id) === epParam);
      if (found && found.id !== currentEpisode?.id) {
        setCurrentEpisode(found);
      }
    }
  }, [epParam, episodes]);

  // Load sources for selected episode
  useEffect(() => {
    if (!currentEpisode) return;

    const loadSources = async () => {
      setLoadingSources(true);
      try {
        const sourceRes = await episodeService.getEpisodeSources(currentEpisode.id);
        const sourceList = sourceRes?.sources || [];
        setSources(sourceList);

        // Check resume progress from watch history if authenticated
        if (isAuthenticated) {
          try {
            const hist = await watchHistoryService.getHistory(1, 30);
            const foundHist = hist?.items?.find((h) => h.episodeId === currentEpisode.id);
            if (foundHist && foundHist.progressSeconds > 10) {
              setResumeTime(foundHist.progressSeconds);
            } else {
              setResumeTime(0);
            }
          } catch {
            setResumeTime(0);
          }
        }
      } catch (err) {
        console.error('Failed to load sources for episode', err);
      } finally {
        setLoadingSources(false);
      }
    };

    loadSources();
  }, [currentEpisode, isAuthenticated]);

  const handleSelectEpisode = (ep) => {
    setCurrentEpisode(ep);
    const newParams = new URLSearchParams(searchParams);
    newParams.set('ep', String(ep.id));
    setSearchParams(newParams);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Next / Previous Episode helpers
  const currentEpIndex = episodes.findIndex((e) => e.id === currentEpisode?.id);
  const prevEp = currentEpIndex > 0 ? episodes[currentEpIndex - 1] : null;
  const nextEp =
    currentEpIndex >= 0 && currentEpIndex < episodes.length - 1
      ? episodes[currentEpIndex + 1]
      : null;

  if (loading) {
    return (
      <div className="watch-page loading-state container">
        <div className="skeleton watch-player-skeleton"></div>
      </div>
    );
  }

  if (error || !movie) {
    return (
      <div className="watch-page error-state-wrap container">
        <ErrorState message={error} onRetry={() => window.location.reload()} />
      </div>
    );
  }

  const movieYear =
    movie.releaseYear ||
    (movie.createdAt ? new Date(movie.createdAt).getFullYear() : null);

  return (
    <div className="watch-page">
      <div className="watch-cinema-container container">
        {/* Breadcrumb / Top Bar */}
        <div className="watch-breadcrumb">
          <Link to="/" className="breadcrumb-link">
            Trang Chủ
          </Link>
          <span className="breadcrumb-sep">/</span>
          <Link to={`/movie/${movie.slug || movie.id}`} className="breadcrumb-link">
            {movie.title}
          </Link>
          <span className="breadcrumb-sep">/</span>
          <span className="breadcrumb-current">
            {currentEpisode?.name || 'Đang xem'}
          </span>
        </div>

        {/* Video Player */}
        <div className="watch-player-box">
          {loadingSources ? (
            <div className="video-player-wrapper player-loading-overlay">
              <div className="player-spinner"></div>
              <span>Đang lấy nguồn phát tập phim...</span>
            </div>
          ) : (
            <VideoPlayer
              sources={sources}
              episodeId={currentEpisode?.id}
              initialTime={resumeTime}
              movieTitle={movie.title}
              episodeName={currentEpisode?.name}
              onEpisodeEnded={() => {
                if (nextEp) {
                  handleSelectEpisode(nextEp);
                }
              }}
            />
          )}
        </div>

        {/* Episode Navigation Buttons (Prev / Next) */}
        <div className="watch-ep-nav-row glass">
          <div className="watch-ep-nav-left">
            <button
              className="btn-secondary watch-nav-btn"
              disabled={!prevEp}
              onClick={() => prevEp && handleSelectEpisode(prevEp)}
            >
              <ChevronLeft size={16} />
              <span>Tập Trước</span>
            </button>
            <button
              className="btn-primary watch-nav-btn"
              disabled={!nextEp}
              onClick={() => nextEp && handleSelectEpisode(nextEp)}
            >
              <span>Tập Kế Tiếp</span>
              <ChevronRight size={16} />
            </button>
          </div>

          <div className="watch-ep-nav-right">
            <Link
              to={`/movie/${movie.slug || movie.id}`}
              className="btn-secondary watch-info-btn"
            >
              <Info size={16} />
              <span>Thông Tin Phim</span>
            </Link>
          </div>
        </div>

        {/* Movie Info & Episode Selector Area */}
        <div className="watch-meta-grid">
          {/* Main Info */}
          <div className="watch-main-info glass">
            <h1 className="watch-movie-title">
              {movie.title} - {currentEpisode?.name || 'Full'}
            </h1>
            {movie.originalTitle && (
              <h3 className="watch-movie-original">{movie.originalTitle}</h3>
            )}

            <div className="watch-movie-tags">
              {movie.quality && <span className="badge-quality">{movie.quality}</span>}
              {movie.rating && (
                <span className="badge-rating">
                  <Star size={12} fill="currentColor" />
                  {Number(movie.rating).toFixed(1)}
                </span>
              )}
              {movieYear && <span className="badge-tag">{movieYear}</span>}
              {movie.duration && <span className="badge-tag">{movie.duration} phút</span>}
            </div>

            {movie.description && (
              <p className="watch-movie-desc">
                {movie.description.replace(/<[^>]*>?/gm, '')}
              </p>
            )}

            {/* Interactive Rating */}
            <RatingWidget movieId={movie.id} />
          </div>

          {/* Episode Selector Sidebar */}
          <div className="watch-episodes-sidebar glass">
            <div className="sidebar-header">
              <List size={18} />
              <span>Chọn Tập Phim</span>
            </div>
            <EpisodeList
              episodes={episodes}
              currentEpisodeId={currentEpisode?.id}
              onSelectEpisode={handleSelectEpisode}
            />
          </div>
        </div>

        {/* Similar Movies */}
        {similarMovies.length > 0 && (
          <div className="watch-similar-wrap">
            <MovieRow
              title="Có Thể Bạn Muốn Xem"
              subtitle="Các bộ phim tương tự có thể bạn quan tâm"
              movies={similarMovies}
            />
          </div>
        )}

        {/* Comments Section */}
        <div className="watch-comments-wrap">
          <CommentSection movieId={movie.id} />
        </div>
      </div>
    </div>
  );
};

export default WatchPage;
