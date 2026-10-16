import React, { useState, useEffect } from 'react';
import { Filter, RotateCcw } from 'lucide-react';
import { genreService } from '../../services/api/genreService';
import './FilterBar.css';

const FilterBar = ({ filters, onFilterChange, onReset }) => {
  const [genres, setGenres] = useState([]);

  useEffect(() => {
    genreService
      .getGenres()
      .then((data) => setGenres(data || []))
      .catch((e) => console.warn('Could not load genres in filter bar', e));
  }, []);

  const years = [
    2026, 2025, 2024, 2023, 2022, 2021, 2020, 2019, 2018, 2017, 2016, 2015, 2010,
  ];

  return (
    <div className="filter-bar glass">
      <div className="filter-bar-header">
        <span className="filter-bar-title">
          <Filter size={16} /> Bộ lọc tìm kiếm
        </span>
        <button className="filter-reset-btn" onClick={onReset}>
          <RotateCcw size={13} /> Đặt lại
        </button>
      </div>

      <div className="filter-controls-grid">
        {/* Genre Filter */}
        <div className="filter-group">
          <label className="filter-label">Thể loại</label>
          <select
            value={filters.genre || ''}
            onChange={(e) => onFilterChange('genre', e.target.value || null)}
            className="filter-select"
          >
            <option value="">Tất cả thể loại</option>
            {genres.map((g) => (
              <option key={g.id} value={g.name}>
                {g.name}
              </option>
            ))}
          </select>
        </div>

        {/* Movie Type */}
        <div className="filter-group">
          <label className="filter-label">Hình thức</label>
          <select
            value={filters.type || ''}
            onChange={(e) => onFilterChange('type', e.target.value || null)}
            className="filter-select"
          >
            <option value="">Tất cả hình thức</option>
            <option value="single">Phim lẻ</option>
            <option value="series">Phim bộ</option>
          </select>
        </div>

        {/* Release Year */}
        <div className="filter-group">
          <label className="filter-label">Năm phát hành</label>
          <select
            value={filters.year || ''}
            onChange={(e) => onFilterChange('year', e.target.value ? Number(e.target.value) : null)}
            className="filter-select"
          >
            <option value="">Tất cả các năm</option>
            {years.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>
        </div>

        {/* Sort By */}
        <div className="filter-group">
          <label className="filter-label">Sắp xếp theo</label>
          <select
            value={filters.sortBy || 'newest'}
            onChange={(e) => onFilterChange('sortBy', e.target.value)}
            className="filter-select"
          >
            <option value="newest">Mới cập nhật</option>
            <option value="rating">Điểm đánh giá cao</option>
            <option value="year">Năm sản xuất</option>
            <option value="title">Tên phim (A-Z)</option>
          </select>
        </div>
      </div>
    </div>
  );
};

export default FilterBar;
