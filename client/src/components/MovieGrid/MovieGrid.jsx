import React from 'react';
import MovieCard from '../MovieCard/MovieCard';
import { EmptyState } from '../Common/CommonStates';
import './MovieGrid.css';

const MovieGrid = ({
  movies = [],
  emptyMessage = 'Không tìm thấy bộ phim nào phù hợp.',
  emptyTitle = 'Không có kết quả',
  onEmptyAction,
  emptyActionText,
  renderItem,
}) => {
  if (!movies || movies.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        message={emptyMessage}
        actionText={emptyActionText}
        onAction={onEmptyAction}
      />
    );
  }

  return (
    <div className="movie-grid">
      {movies.map((movie, index) => (
        <div key={movie.id || index} className="movie-grid-item">
          {renderItem ? renderItem(movie) : <MovieCard movie={movie} />}
        </div>
      ))}
    </div>
  );
};

export default MovieGrid;
