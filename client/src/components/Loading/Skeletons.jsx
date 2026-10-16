import React from 'react';
import './Skeletons.css';

export const MovieCardSkeleton = () => {
  return (
    <div className="movie-card-skeleton">
      <div className="skeleton poster-skeleton"></div>
      <div className="skeleton title-skeleton"></div>
      <div className="skeleton meta-skeleton"></div>
    </div>
  );
};

export const HeroSkeleton = () => {
  return (
    <div className="hero-skeleton skeleton">
      <div className="hero-skeleton-content container">
        <div className="skeleton line-title"></div>
        <div className="skeleton line-meta"></div>
        <div className="skeleton line-desc"></div>
        <div className="skeleton line-desc short"></div>
        <div className="hero-skeleton-buttons">
          <div className="skeleton line-btn"></div>
          <div className="skeleton line-btn"></div>
        </div>
      </div>
    </div>
  );
};

export const MovieGridSkeleton = ({ count = 12 }) => {
  return (
    <div className="movie-grid">
      {Array.from({ length: count }).map((_, i) => (
        <MovieCardSkeleton key={i} />
      ))}
    </div>
  );
};

export const MovieRowSkeleton = ({ count = 6 }) => {
  return (
    <div className="movie-row-skeleton">
      <div className="skeleton section-title-skeleton"></div>
      <div className="movie-row-skeleton-track">
        {Array.from({ length: count }).map((_, i) => (
          <MovieCardSkeleton key={i} />
        ))}
      </div>
    </div>
  );
};
