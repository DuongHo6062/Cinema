import React, { useRef, useState, useEffect } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import MovieCard from '../MovieCard/MovieCard';
import './MovieRow.css';

const MovieRow = ({
  title,
  subtitle,
  movies = [],
  viewAllLink,
  renderItem,
}) => {
  const rowRef = useRef(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const checkScroll = () => {
    if (rowRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = rowRef.current;
      setCanScrollLeft(scrollLeft > 20);
      setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 20);
    }
  };

  useEffect(() => {
    checkScroll();
    window.addEventListener('resize', checkScroll);
    return () => window.removeEventListener('resize', checkScroll);
  }, [movies]);

  const handleScroll = (direction) => {
    if (rowRef.current) {
      const { clientWidth } = rowRef.current;
      const scrollAmount = direction === 'left' ? -clientWidth * 0.75 : clientWidth * 0.75;
      rowRef.current.scrollBy({ left: scrollAmount, behavior: 'smooth' });
      setTimeout(checkScroll, 350);
    }
  };

  if (!movies || movies.length === 0) return null;

  return (
    <section className="movie-row-section">
      <div className="container movie-row-wrapper">
        <div className="movie-row-header">
          <div className="movie-row-title-group">
            <div className="movie-row-accent-bar"></div>
            <div>
              <h2 className="movie-row-title">{title}</h2>
              {subtitle && <p className="movie-row-subtitle">{subtitle}</p>}
            </div>
          </div>

          {viewAllLink && (
            <Link to={viewAllLink} className="movie-row-view-all">
              Xem tất cả
              <ChevronRight size={16} />
            </Link>
          )}
        </div>

        <div className="movie-row-container">
          {/* Left Arrow */}
          {canScrollLeft && (
            <button
              className="movie-row-arrow left"
              onClick={() => handleScroll('left')}
              aria-label="Cuộn sang trái"
            >
              <ChevronLeft size={24} />
            </button>
          )}

          {/* Scrollable Track */}
          <div
            className="movie-row-track no-scrollbar"
            ref={rowRef}
            onScroll={checkScroll}
          >
            {movies.map((item, idx) => (
              <div key={item.id || idx} className="movie-row-item">
                {renderItem ? renderItem(item) : <MovieCard movie={item} />}
              </div>
            ))}
          </div>

          {/* Right Arrow */}
          {canScrollRight && (
            <button
              className="movie-row-arrow right"
              onClick={() => handleScroll('right')}
              aria-label="Cuộn sang phải"
            >
              <ChevronRight size={24} />
            </button>
          )}
        </div>
      </div>
    </section>
  );
};

export default MovieRow;
