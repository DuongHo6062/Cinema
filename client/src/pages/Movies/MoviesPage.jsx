import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import MovieGrid from '../../components/MovieGrid/MovieGrid';
import FilterBar from '../../components/FilterBar/FilterBar';
import { MovieGridSkeleton } from '../../components/Loading/Skeletons';
import { ErrorState } from '../../components/Common/CommonStates';
import { movieService } from '../../services/api/movieService';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import './MoviesPage.css';

const MoviesPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  const [movies, setMovies] = useState([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Extract query filters
  const page = Number(searchParams.get('page')) || 1;
  const genre = searchParams.get('genre') || null;
  const type = searchParams.get('type') || null;
  const year = searchParams.get('year') ? Number(searchParams.get('year')) : null;
  const sortBy = searchParams.get('sortBy') || 'newest';
  const search = searchParams.get('search') || null;

  const currentFilters = { genre, type, year, sortBy, search, page };

  const fetchMovies = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await movieService.getMovies({
        page,
        pageSize: 18,
        genre: genre || undefined,
        type: type || undefined,
        year: year || undefined,
        sortBy: sortBy || undefined,
        search: search || undefined,
      });

      setMovies(res?.items || []);
      setTotalItems(res?.totalItems || res?.totalCount || 0);
      setTotalPages(res?.totalPages || Math.ceil((res?.totalItems || 0) / 18) || 1);
    } catch (err) {
      console.error('Error fetching movies', err);
      setError('Không thể tải danh sách phim từ máy chủ.');
    } finally {
      setLoading(false);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  useEffect(() => {
    fetchMovies();
  }, [page, genre, type, year, sortBy, search]);

  const handleFilterChange = (key, value) => {
    const newParams = new URLSearchParams(searchParams);
    if (value) {
      newParams.set(key, value);
    } else {
      newParams.delete(key);
    }
    newParams.set('page', '1'); // Reset to page 1 on filter change
    setSearchParams(newParams);
  };

  const handleResetFilters = () => {
    setSearchParams({});
  };

  const handlePageChange = (newPage) => {
    if (newPage < 1 || newPage > totalPages) return;
    const newParams = new URLSearchParams(searchParams);
    newParams.set('page', String(newPage));
    setSearchParams(newParams);
  };

  // Determine Title
  let pageTitle = 'Tất Cả Phim';
  if (type === 'series') pageTitle = 'Phim Bộ Đặc Sắc';
  else if (type === 'single') pageTitle = 'Phim Lẻ Chiếu Rạp';
  else if (genre) pageTitle = `Phim Thể Loại: ${genre}`;
  else if (search) pageTitle = `Kết Quả Tìm Kiếm Cho: "${search}"`;

  return (
    <div className="movies-page container">
      {/* Header */}
      <div className="movies-page-header">
        <h1 className="movies-page-title">{pageTitle}</h1>
        <p className="movies-page-count">
          Tìm thấy <strong>{totalItems}</strong> bộ phim phù hợp
        </p>
      </div>

      {/* Filter Bar */}
      <FilterBar
        filters={currentFilters}
        onFilterChange={handleFilterChange}
        onReset={handleResetFilters}
      />

      {/* Content */}
      {loading ? (
        <MovieGridSkeleton count={18} />
      ) : error ? (
        <ErrorState message={error} onRetry={fetchMovies} />
      ) : (
        <>
          <MovieGrid
            movies={movies}
            emptyTitle="Không tìm thấy phim"
            emptyMessage="Hãy thử chọn thể loại khác hoặc xóa bộ lọc để xem nhiều phim hơn."
            emptyActionText="Xóa bộ lọc"
            onEmptyAction={handleResetFilters}
          />

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="pagination-container">
              <button
                className="pagination-btn arrow"
                disabled={page <= 1}
                onClick={() => handlePageChange(page - 1)}
                aria-label="Trang trước"
              >
                <ChevronLeft size={18} />
                <span>Trước</span>
              </button>

              <div className="pagination-pages">
                {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
                  let pageNum;
                  if (totalPages <= 7) {
                    pageNum = i + 1;
                  } else if (page <= 4) {
                    pageNum = i + 1;
                  } else if (page >= totalPages - 3) {
                    pageNum = totalPages - 6 + i;
                  } else {
                    pageNum = page - 3 + i;
                  }

                  return (
                    <button
                      key={pageNum}
                      className={`pagination-number ${pageNum === page ? 'active' : ''}`}
                      onClick={() => handlePageChange(pageNum)}
                    >
                      {pageNum}
                    </button>
                  );
                })}
              </div>

              <button
                className="pagination-btn arrow"
                disabled={page >= totalPages}
                onClick={() => handlePageChange(page + 1)}
                aria-label="Trang sau"
              >
                <span>Sau</span>
                <ChevronRight size={18} />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default MoviesPage;
