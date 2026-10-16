import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Search, X, SlidersHorizontal } from 'lucide-react';
import MovieGrid from '../../components/MovieGrid/MovieGrid';
import FilterBar from '../../components/FilterBar/FilterBar';
import { MovieGridSkeleton } from '../../components/Loading/Skeletons';
import { movieService } from '../../services/api/movieService';
import './SearchPage.css';

const SearchPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const qParam = searchParams.get('q') || '';

  const [searchTerm, setSearchTerm] = useState(qParam);
  const [movies, setMovies] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [showFilters, setShowFilters] = useState(false);

  // Filter states
  const [filters, setFilters] = useState({
    genre: null,
    type: null,
    year: null,
    sortBy: 'newest',
  });

  const performSearch = async (query, currentFilters) => {
    setLoading(true);
    try {
      const res = await movieService.getMovies({
        search: query.trim() || undefined,
        genre: currentFilters.genre || undefined,
        type: currentFilters.type || undefined,
        year: currentFilters.year || undefined,
        sortBy: currentFilters.sortBy || undefined,
        pageSize: 24,
      });

      setMovies(res?.items || []);
      setTotalCount(res?.totalItems || res?.totalCount || 0);
    } catch (err) {
      console.error('Search error', err);
      setMovies([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setSearchTerm(qParam);
    performSearch(qParam, filters);
  }, [qParam]);

  const handleSubmit = (e) => {
    e.preventDefault();
    const newParams = new URLSearchParams(searchParams);
    if (searchTerm.trim()) {
      newParams.set('q', searchTerm.trim());
    } else {
      newParams.delete('q');
    }
    setSearchParams(newParams);
    performSearch(searchTerm, filters);
  };

  const handleFilterChange = (key, val) => {
    const updated = { ...filters, [key]: val };
    setFilters(updated);
    performSearch(searchTerm, updated);
  };

  const handleResetFilters = () => {
    const reset = { genre: null, type: null, year: null, sortBy: 'newest' };
    setFilters(reset);
    performSearch(searchTerm, reset);
  };

  return (
    <div className="search-page container">
      {/* Big Search Input Hero */}
      <div className="search-hero">
        <h1 className="search-page-title">Tìm Kiếm Phim</h1>
        <p className="search-page-subtitle">
          Tìm kiếm trong hàng ngàn bộ phim bom tấn, phim bộ và anime
        </p>

        <form onSubmit={handleSubmit} className="search-input-form glass">
          <Search size={22} className="search-input-icon" />
          <input
            type="text"
            placeholder="Nhập tên phim, đạo diễn, diễn viên..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-main-input"
            autoFocus
          />
          {searchTerm && (
            <button
              type="button"
              className="search-clear-btn"
              onClick={() => {
                setSearchTerm('');
                setSearchParams({});
                performSearch('', filters);
              }}
            >
              <X size={18} />
            </button>
          )}
          <button type="submit" className="btn-primary search-submit-btn">
            Tìm Kiếm
          </button>
        </form>

        <div className="search-toggle-filters">
          <button
            className={`btn-secondary filter-toggle-btn ${showFilters ? 'active' : ''}`}
            onClick={() => setShowFilters(!showFilters)}
          >
            <SlidersHorizontal size={15} />
            <span>{showFilters ? 'Ẩn bộ lọc nâng cao' : 'Hiện bộ lọc nâng cao'}</span>
          </button>
        </div>
      </div>

      {/* Optional Filter Bar */}
      {showFilters && (
        <FilterBar
          filters={filters}
          onFilterChange={handleFilterChange}
          onReset={handleResetFilters}
        />
      )}

      {/* Results Header */}
      <div className="search-results-header">
        <h2 className="search-results-title">
          {searchTerm ? (
            <>
              Kết quả tìm kiếm cho: <span>"{searchTerm}"</span>
            </>
          ) : (
            'Tất cả các phim'
          )}
        </h2>
        <span className="search-results-count">({totalCount} kết quả)</span>
      </div>

      {/* Results Grid */}
      {loading ? (
        <MovieGridSkeleton count={12} />
      ) : (
        <MovieGrid
          movies={movies}
          emptyTitle="Không tìm thấy phim phù hợp"
          emptyMessage={`Không có kết quả nào cho "${searchTerm}". Vui lòng thử từ khóa khác hoặc xóa bộ lọc.`}
          emptyActionText="Xem tất cả phim"
          onEmptyAction={() => {
            setSearchTerm('');
            setSearchParams({});
            performSearch('', filters);
          }}
        />
      )}
    </div>
  );
};

export default SearchPage;
