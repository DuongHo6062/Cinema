import React, { useState } from 'react';
import { Play, CheckCircle } from 'lucide-react';
import './EpisodeList.css';

const EpisodeList = ({
  episodes = [],
  currentEpisodeId,
  onSelectEpisode,
}) => {
  const [searchTerm, setSearchTerm] = useState('');

  if (!episodes || episodes.length === 0) {
    return (
      <div className="episodes-empty">
        <p>Chưa có danh sách tập phim cho nội dung này.</p>
      </div>
    );
  }

  const filteredEpisodes = episodes.filter((ep) => {
    if (!searchTerm.trim()) return true;
    const nameMatch = (ep.name || '').toLowerCase().includes(searchTerm.toLowerCase());
    const numMatch = String(ep.episodeNumber || '').includes(searchTerm);
    return nameMatch || numMatch;
  });

  return (
    <div className="episodes-section">
      <div className="episodes-header">
        <h3 className="episodes-title">
          Danh Sách Tập Phim <span className="episodes-count">({episodes.length} tập)</span>
        </h3>
        {episodes.length > 15 && (
          <input
            type="text"
            placeholder="Tìm tập..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="episodes-search-input"
          />
        )}
      </div>

      <div className="episodes-grid">
        {filteredEpisodes.map((ep) => {
          const isActive = currentEpisodeId && Number(currentEpisodeId) === ep.id;
          const displayLabel = ep.name || `Tập ${ep.episodeNumber || ep.id}`;

          return (
            <button
              key={ep.id}
              className={`episode-btn ${isActive ? 'active' : ''}`}
              onClick={() => onSelectEpisode && onSelectEpisode(ep)}
              title={ep.name || `Tập ${ep.episodeNumber}`}
            >
              {isActive && <Play size={12} fill="currentColor" className="episode-active-icon" />}
              <span>{displayLabel}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
};

export default EpisodeList;
