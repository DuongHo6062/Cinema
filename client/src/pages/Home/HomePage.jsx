import React, { useState, useEffect } from 'react';
import HeroBanner from '../../components/HeroBanner/HeroBanner';
import MovieRow from '../../components/MovieRow/MovieRow';
import { HeroSkeleton, MovieRowSkeleton } from '../../components/Loading/Skeletons';
import { ErrorState } from '../../components/Common/CommonStates';
import { movieService } from '../../services/api/movieService';
import { recommendationService } from '../../services/api/recommendationService';
import { watchHistoryService } from '../../services/api/watchHistoryService';
import { useAuth } from '../../context/AuthContext';
import MovieCard from '../../components/MovieCard/MovieCard';
import './HomePage.css';

const HomePage = () => {
  const { isAuthenticated } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [heroMovies, setHeroMovies] = useState([]);
  const [continueWatching, setContinueWatching] = useState([]);
  const [trendingMovies, setTrendingMovies] = useState([]);
  const [recommendedMovies, setRecommendedMovies] = useState([]);
  const [recentMovies, setRecentMovies] = useState([]);
  const [englishMovies, setEnglishMovies] = useState([]);
  const [seriesMovies, setSeriesMovies] = useState([]);
  const [singleMovies, setSingleMovies] = useState([]);

  const loadHomeData = async () => {
    setLoading(true);
    setError(null);

    try {
      // 1. Fetch recent movies (for hero banner & recent row)
      const recentRes = await movieService.getMovies({
        sortBy: 'newest',
        pageSize: 12,
      });
      const recentList = recentRes?.items || [];
      setRecentMovies(recentList);
      setHeroMovies(recentList.slice(0, 5));

      // 2. Fetch top rated / trending
      const ratedRes = await movieService.getMovies({
        sortBy: 'rating',
        pageSize: 10,
      });
      setTrendingMovies(ratedRes?.items || []);

      // 3. Fetch Recommendations (Personalized or Public)
      try {
        const recRes = await recommendationService.getRecommendations(
          { pageSize: 10 },
          isAuthenticated
        );
        setRecommendedMovies(recRes?.items || []);
      } catch (recErr) {
        console.warn('Could not fetch recommendations', recErr);
      }

      // 4. Fetch English / Âu Mỹ Vietsub movies
      try {
        const englishRes = await movieService.getMovies({
          country: 'Âu Mỹ',
          isVietsub: true,
          pageSize: 14,
        });
        setEnglishMovies(englishRes?.items || []);
      } catch (eErr) {
        console.warn('Could not fetch English movies', eErr);
      }

      // 5. Fetch Series
      const seriesRes = await movieService.getMovies({
        type: 'series',
        pageSize: 12,
      });
      setSeriesMovies(seriesRes?.items || []);

      // 6. Fetch Single Movies
      const singleRes = await movieService.getMovies({
        type: 'single',
        pageSize: 12,
      });
      setSingleMovies(singleRes?.items || []);

      // 6. Continue watching if logged in
      if (isAuthenticated) {
        try {
          const continueRes = await watchHistoryService.getContinueWatching(8);
          setContinueWatching(continueRes || []);
        } catch (cwErr) {
          console.warn('Could not fetch continue watching', cwErr);
        }
      } else {
        setContinueWatching([]);
      }
    } catch (err) {
      console.error('Failed to load homepage data', err);
      setError('Không thể tải danh sách phim từ máy chủ. Vui lòng kiểm tra kết nối API.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHomeData();
  }, [isAuthenticated]);

  if (loading) {
    return (
      <div className="home-page-loading">
        <HeroSkeleton />
        <div className="container">
          <MovieRowSkeleton count={6} />
          <MovieRowSkeleton count={6} />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="home-page-error container">
        <ErrorState message={error} onRetry={loadHomeData} />
      </div>
    );
  }

  return (
    <div className="home-page">
      {/* Cinematic Hero */}
      <HeroBanner movies={heroMovies} />

      <div className="home-content">
        {/* Continue Watching (Auth only) */}
        {continueWatching.length > 0 && (
          <MovieRow
            title="Tiếp Tục Xem"
            subtitle="Xem tiếp tập phim bạn đang theo dõi"
            movies={continueWatching.map((item) => ({
              id: item.movieId,
              title: item.movieTitle,
              slug: item.movieSlug || item.movieId,
              posterUrl: item.posterUrl,
              quality: 'HD',
              duration: item.durationSeconds ? Math.floor(item.durationSeconds / 60) : null,
              progressPercentage: item.progressPercentage,
              progressSeconds: item.progressSeconds,
              showProgress: true,
            }))}
            viewAllLink="/history"
            renderItem={(m) => (
              <MovieCard
                movie={m}
                showProgress={true}
                progressPercentage={m.progressPercentage}
                progressSeconds={m.progressSeconds}
              />
            )}
          />
        )}

        {/* Recommended For You */}
        {recommendedMovies.length > 0 && (
          <MovieRow
            title={isAuthenticated ? 'Gợi Ý Dành Riêng Cho Bạn' : 'Khám Phá Thịnh Hành'}
            subtitle={isAuthenticated ? 'Dựa trên sở thích và lịch sử xem của bạn' : 'Những bộ phim được yêu thích nhất'}
            movies={recommendedMovies}
            viewAllLink="/movies"
          />
        )}

        {/* Trending / High Rated */}
        {trendingMovies.length > 0 && (
          <MovieRow
            title="Được Đánh Giá Cao Nhất"
            subtitle="Top phim có số điểm đánh giá cao từ cộng đồng"
            movies={trendingMovies}
            viewAllLink="/movies?sortBy=rating"
          />
        )}

        {/* Recently Added */}
        {recentMovies.length > 0 && (
          <MovieRow
            title="Phim Mới Cập Nhật"
            subtitle="Các bộ phim mới nhất vừa được bổ sung vào hệ thống"
            movies={recentMovies}
            viewAllLink="/movies?sortBy=newest"
          />
        )}

        {/* English Movies with Vietsub */}
        {englishMovies.length > 0 && (
          <MovieRow
            title="Phim Âu Mỹ Vietsub"
            subtitle="Tuyển chọn phim điện ảnh Âu Mỹ chất lượng cao với phụ đề tiếng Việt"
            movies={englishMovies}
            viewAllLink="/movies?country=Âu Mỹ"
          />
        )}

        {/* TV Series */}
        {seriesMovies.length > 0 && (
          <MovieRow
            title="Phim Bộ Đặc Sắc"
            subtitle="Tuyển tập các series phim bộ dài tập hấp dẫn"
            movies={seriesMovies}
            viewAllLink="/movies?type=series"
          />
        )}

        {/* Single Movies */}
        {singleMovies.length > 0 && (
          <MovieRow
            title="Phim Lẻ Chiếu Rạp"
            subtitle="Phim điện ảnh lẻ chọn lọc với nội dung trọn vẹn"
            movies={singleMovies}
            viewAllLink="/movies?type=single"
          />
        )}
      </div>
    </div>
  );
};

export default HomePage;
