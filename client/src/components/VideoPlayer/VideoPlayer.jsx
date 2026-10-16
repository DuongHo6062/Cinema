import React, { useRef, useState, useEffect, useCallback } from 'react';
import Hls from 'hls.js';
import {
  Play,
  Pause,
  Volume2,
  VolumeX,
  Maximize,
  Minimize,
  RotateCcw,
  Server,
  AlertCircle,
  RefreshCw,
  ShieldCheck,
  Zap,
} from 'lucide-react';
import { watchHistoryService } from '../../services/api/watchHistoryService';
import { useAuth } from '../../context/AuthContext';
import './VideoPlayer.css';

const VideoPlayer = ({
  sources = [],
  episodeId,
  initialTime = 0,
  onEpisodeEnded,
  movieTitle,
  episodeName,
}) => {
  const { isAuthenticated } = useAuth();
  const videoRef = useRef(null);
  const playerContainerRef = useRef(null);
  const hlsRef = useRef(null);
  const progressSaveTimerRef = useRef(null);
  const retryCountRef = useRef(0);
  const loadingTimerRef = useRef(null);

  const [activeSourceIndex, setActiveSourceIndex] = useState(0);
  const [playerMode, setPlayerMode] = useState('hls'); // 'hls' | 'embed'
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState(1);
  const [isMuted, setIsMuted] = useState(false);
  const [wasAutoplayMuted, setWasAutoplayMuted] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [showControls, setShowControls] = useState(true);
  const [controlsTimeout, setControlsTimeout] = useState(null);
  const [playbackError, setPlaybackError] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // 1. Mặc định ưu tiên server Vietsub khi danh sách sources hoặc episodeId thay đổi
  useEffect(() => {
    if (sources && sources.length > 0) {
      const vietsubIdx = sources.findIndex((s) => {
        const sName = (s.serverName || '').toLowerCase();
        const sLang = (s.language || '').toLowerCase();
        return sName.includes('vietsub') || sLang.includes('vietsub');
      });
      setActiveSourceIndex(vietsubIdx >= 0 ? vietsubIdx : 0);
    } else {
      setActiveSourceIndex(0);
    }
  }, [episodeId, sources]);

  const currentSource = sources[activeSourceIndex] || sources[0];

  // Helper to determine available playback types
  const hasEmbed = !!currentSource?.embedUrl;
  const hasHls = !!currentSource?.videoUrl;

  // Decide initial player mode when source changes
  useEffect(() => {
    retryCountRef.current = 0;
    setPlaybackError(null);

    // If source only has embed, force embed mode
    if (!hasHls && hasEmbed) {
      setPlayerMode('embed');
    } else {
      setPlayerMode('hls');
    }
  }, [currentSource?.videoUrl, currentSource?.embedUrl, hasHls, hasEmbed]);

  const switchToEmbed = useCallback(() => {
    if (hasEmbed) {
      console.warn('Switching to embed player fallback');
      if (hlsRef.current) {
        hlsRef.current.destroy();
        hlsRef.current = null;
      }
      setPlayerMode('embed');
      setIsLoading(false);
      setPlaybackError(null);
    } else {
      setIsLoading(false);
      setPlaybackError('Không thể phát luồng video từ máy chủ này và không có nguồn dự phòng.');
    }
  }, [hasEmbed]);

  // Helper attempt autoplay on media ready
  const attemptAutoplay = useCallback(() => {
    const vid = videoRef.current;
    if (!vid) return;

    const playPromise = vid.play();
    if (playPromise !== undefined) {
      playPromise
        .then(() => {
          setIsPlaying(true);
          setWasAutoplayMuted(false);
        })
        .catch((err) => {
          console.warn('Unmuted autoplay prevented by browser policy, attempting muted autoplay:', err);
          // Try muted autoplay if unmuted was blocked by browser
          vid.muted = true;
          setIsMuted(true);
          setWasAutoplayMuted(true);
          vid
            .play()
            .then(() => {
              setIsPlaying(true);
            })
            .catch(() => {
              setIsPlaying(false);
            });
        });
    }
  }, []);

  // Initialize Video / HLS Stream
  useEffect(() => {
    if (loadingTimerRef.current) {
      clearTimeout(loadingTimerRef.current);
    }

    if (hlsRef.current) {
      hlsRef.current.destroy();
      hlsRef.current = null;
    }

    if (!currentSource) {
      setIsLoading(false);
      return;
    }

    // When running in embed mode, iframe handles playback
    if (playerMode === 'embed') {
      setIsLoading(false);
      setPlaybackError(null);
      return;
    }

    // HLS / Direct mode
    const streamUrl = currentSource.videoUrl;
    if (!streamUrl) {
      switchToEmbed();
      return;
    }

    const video = videoRef.current;
    if (!video) return;

    setPlaybackError(null);
    setIsLoading(true);

    // Safety timeout: If HLS manifest doesn't parse within 6 seconds, auto fallback to embed
    loadingTimerRef.current = setTimeout(() => {
      if (hasEmbed) {
        console.warn('HLS stream loading timed out (6s). Auto fallback to Embed player.');
        switchToEmbed();
      } else {
        setIsLoading(false);
        setPlaybackError('Quá thời gian tải luồng phát. Vui lòng bấm thử lại hoặc chọn máy chủ khác.');
      }
    }, 6000);

    const isHls = streamUrl.includes('.m3u8');

    if (isHls && Hls.isSupported()) {
      const hls = new Hls({
        enableWorker: true,
        lowLatencyMode: true,
        manifestLoadingTimeOut: 5000,
        manifestLoadingMaxRetry: 2,
        levelLoadingTimeOut: 5000,
      });
      hlsRef.current = hls;

      hls.loadSource(streamUrl);
      hls.attachMedia(video);

      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
        setIsLoading(false);
        setPlaybackError(null);
        if (initialTime > 0) {
          video.currentTime = initialTime;
        }
        attemptAutoplay();
      });

      hls.on(Hls.Events.ERROR, (_, data) => {
        if (data.fatal) {
          console.warn('HLS fatal error:', data.type, data.details);
          retryCountRef.current += 1;

          if (retryCountRef.current > 2) {
            if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
            if (hasEmbed) {
              switchToEmbed();
            } else {
              hls.destroy();
              setIsLoading(false);
              setPlaybackError('Không thể tải luồng video từ CDN này.');
            }
            return;
          }

          switch (data.type) {
            case Hls.ErrorTypes.NETWORK_ERROR:
              hls.startLoad();
              break;
            case Hls.ErrorTypes.MEDIA_ERROR:
              hls.recoverMediaError();
              break;
            default:
              if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
              if (hasEmbed) {
                switchToEmbed();
              } else {
                hls.destroy();
                setIsLoading(false);
                setPlaybackError('Định dạng video không được hỗ trợ.');
              }
              break;
          }
        }
      });
    } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
      // Native Safari HLS
      video.src = streamUrl;
      const handleMetadata = () => {
        if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
        setIsLoading(false);
        if (initialTime > 0) video.currentTime = initialTime;
        attemptAutoplay();
      };
      const handleError = () => {
        if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
        if (hasEmbed) {
          switchToEmbed();
        } else {
          setIsLoading(false);
          setPlaybackError('Không thể phát luồng video trên trình duyệt này.');
        }
      };

      video.addEventListener('loadedmetadata', handleMetadata, { once: true });
      video.addEventListener('error', handleError, { once: true });
    } else {
      // Standard video format
      video.src = streamUrl;
      const handleMetadata = () => {
        if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
        setIsLoading(false);
        if (initialTime > 0) video.currentTime = initialTime;
        attemptAutoplay();
      };
      const handleError = () => {
        if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
        if (hasEmbed) {
          switchToEmbed();
        } else {
          setIsLoading(false);
          setPlaybackError('Định dạng video không được hỗ trợ hoặc link đã hết hạn.');
        }
      };

      video.addEventListener('loadedmetadata', handleMetadata, { once: true });
      video.addEventListener('error', handleError, { once: true });
    }

    return () => {
      if (loadingTimerRef.current) clearTimeout(loadingTimerRef.current);
      if (hlsRef.current) {
        hlsRef.current.destroy();
      }
    };
  }, [currentSource, playerMode, hasEmbed, hasHls, initialTime, switchToEmbed, attemptAutoplay]);

  // Save watch progress periodically
  useEffect(() => {
    if (!isAuthenticated || !episodeId || playerMode === 'embed') return;

    progressSaveTimerRef.current = setInterval(() => {
      if (videoRef.current && isPlaying && videoRef.current.currentTime > 5) {
        watchHistoryService
          .saveProgress({
            episodeId,
            progressSeconds: videoRef.current.currentTime,
            durationSeconds: videoRef.current.duration || 0,
          })
          .catch(() => {});
      }
    }, 12000);

    return () => {
      if (progressSaveTimerRef.current) {
        clearInterval(progressSaveTimerRef.current);
      }
      const videoEl = videoRef.current;
      if (videoEl && videoEl.currentTime > 5) {
        watchHistoryService
          .saveProgress({
            episodeId,
            progressSeconds: videoEl.currentTime,
            durationSeconds: videoEl.duration || 0,
          })
          .catch(() => {});
      }
    };
  }, [episodeId, isPlaying, isAuthenticated, playerMode]);

  const togglePlay = () => {
    if (!videoRef.current) return;
    if (isPlaying) {
      videoRef.current.pause();
    } else {
      videoRef.current.play().then(() => setIsPlaying(true)).catch(() => {});
    }
  };

  const handleTimeUpdate = () => {
    if (videoRef.current) {
      setCurrentTime(videoRef.current.currentTime);
      setDuration(videoRef.current.duration || 0);
    }
  };

  const handleSeek = (e) => {
    const seekTime = Number(e.target.value);
    if (videoRef.current) {
      videoRef.current.currentTime = seekTime;
      setCurrentTime(seekTime);
    }
  };

  const toggleMute = () => {
    if (!videoRef.current) return;
    videoRef.current.muted = !isMuted;
    setIsMuted(!isMuted);
    if (wasAutoplayMuted) {
      setWasAutoplayMuted(false);
    }
  };

  const handleVolumeChange = (e) => {
    const val = Number(e.target.value);
    if (videoRef.current) {
      videoRef.current.volume = val;
      setVolume(val);
      setIsMuted(val === 0);
      if (wasAutoplayMuted && val > 0) {
        videoRef.current.muted = false;
        setWasAutoplayMuted(false);
      }
    }
  };

  const toggleFullscreen = () => {
    if (!playerContainerRef.current) return;
    if (!document.fullscreenElement) {
      playerContainerRef.current.requestFullscreen().catch(() => {});
      setIsFullscreen(true);
    } else {
      document.exitFullscreen().catch(() => {});
      setIsFullscreen(false);
    }
  };

  const handleMouseMove = () => {
    setShowControls(true);
    if (controlsTimeout) clearTimeout(controlsTimeout);
    const timeout = setTimeout(() => {
      if (isPlaying) setShowControls(false);
    }, 3500);
    setControlsTimeout(timeout);
  };

  const formatSeconds = (sec) => {
    if (!sec || isNaN(sec)) return '00:00';
    const m = Math.floor(sec / 60);
    const s = Math.floor(sec % 60);
    return `${m < 10 ? '0' : ''}${m}:${s < 10 ? '0' : ''}${s}`;
  };

  const handleRetryHls = () => {
    retryCountRef.current = 0;
    setPlaybackError(null);
    setIsLoading(true);
    setPlayerMode('hls');
  };

  if (!currentSource) {
    return (
      <div className="video-player-wrapper player-empty-state">
        <AlertCircle size={40} />
        <p>Tập phim này hiện chưa có nguồn phát video.</p>
      </div>
    );
  }

  return (
    <div className="video-player-container-block">
      <div
        className={`video-player-wrapper ${isFullscreen ? 'fullscreen' : ''}`}
        ref={playerContainerRef}
        onMouseMove={handleMouseMove}
        onMouseLeave={() => isPlaying && setShowControls(false)}
      >
        {/* Embed Iframe fallback if selected or active */}
        {playerMode === 'embed' && currentSource.embedUrl ? (
          <iframe
            src={currentSource.embedUrl}
            title={movieTitle || 'Video player'}
            allowFullScreen
            className="video-player-iframe"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; fullscreen"
          />
        ) : (
          <>
            <video
              ref={videoRef}
              className="video-player-element"
              playsInline
              onClick={togglePlay}
              onPlay={() => setIsPlaying(true)}
              onPause={() => setIsPlaying(false)}
              onPlaying={() => {
                setIsLoading(false);
                setIsPlaying(true);
              }}
              onWaiting={() => setIsLoading(true)}
              onCanPlay={() => setIsLoading(false)}
              onTimeUpdate={handleTimeUpdate}
              onEnded={() => {
                setIsPlaying(false);
                if (onEpisodeEnded) onEpisodeEnded();
              }}
            />

            {/* Unmute floating badge if muted by browser autoplay policy */}
            {wasAutoplayMuted && isPlaying && (
              <button
                type="button"
                className="player-unmute-floating-btn"
                onClick={toggleMute}
                title="Bấm để bật âm thanh"
              >
                <VolumeX size={16} />
                <span>Bấm để bật tiếng</span>
              </button>
            )}

            {/* Loading Overlay */}
            {isLoading && (
              <div className="player-loading-overlay">
                <div className="player-spinner"></div>
                <span>Đang kết nối luồng phát...</span>
                {hasEmbed && (
                  <button
                    type="button"
                    className="player-switch-embed-btn"
                    onClick={switchToEmbed}
                    title="Chuyển ngay sang player dự phòng"
                  >
                    <ShieldCheck size={14} /> Chuyển sang Player Dự Phòng
                  </button>
                )}
              </div>
            )}

            {/* Error Overlay */}
            {playbackError && (
              <div className="player-error-overlay">
                <AlertCircle size={40} />
                <p>{playbackError}</p>
                <div className="player-error-actions">
                  {hasEmbed && (
                    <button
                      type="button"
                      className="btn-primary player-error-btn"
                      onClick={switchToEmbed}
                    >
                      <ShieldCheck size={16} /> Dùng Player Dự Phòng (Iframe)
                    </button>
                  )}
                  <button
                    type="button"
                    className="btn-secondary player-error-btn"
                    onClick={handleRetryHls}
                  >
                    <RefreshCw size={16} /> Thử Lại
                  </button>
                </div>
              </div>
            )}

            {/* Custom Overlay Controls */}
            <div className={`player-controls ${showControls || !isPlaying ? 'visible' : ''}`}>
              {/* Top Bar: Title info */}
              <div className="player-top-bar">
                <span className="player-title">{movieTitle}</span>
                {episodeName && <span className="player-episode-badge">{episodeName}</span>}
              </div>

              {/* Middle Big Play Trigger (when paused) */}
              {!isPlaying && !isLoading && !playbackError && (
                <button
                  type="button"
                  className="player-big-play"
                  onClick={togglePlay}
                  aria-label="Phát video"
                >
                  <Play size={36} fill="currentColor" />
                </button>
              )}

              {/* Bottom Controls Bar */}
              <div className="player-bottom-bar">
                {/* Scrubber / Progress slider */}
                <div className="player-progress-container">
                  <input
                    type="range"
                    min="0"
                    max={duration || 100}
                    value={currentTime}
                    onChange={handleSeek}
                    className="player-progress-slider"
                    aria-label="Tiến trình video"
                  />
                  <div
                    className="player-progress-fill"
                    style={{ width: `${(currentTime / (duration || 1)) * 100}%` }}
                  ></div>
                </div>

                <div className="player-actions-row">
                  <div className="player-actions-left">
                    <button
                      type="button"
                      className="player-btn"
                      onClick={togglePlay}
                      aria-label="Phát/Dừng"
                    >
                      {isPlaying ? <Pause size={20} /> : <Play size={20} fill="currentColor" />}
                    </button>

                    <button
                      type="button"
                      className="player-btn"
                      onClick={() => {
                        if (videoRef.current) {
                          videoRef.current.currentTime = Math.max(0, videoRef.current.currentTime - 10);
                        }
                      }}
                      title="Lùi 10 giây"
                      aria-label="Lùi 10 giây"
                    >
                      <RotateCcw size={18} />
                    </button>

                    {/* Volume */}
                    <div className="player-volume-group">
                      <button
                        type="button"
                        className="player-btn"
                        onClick={toggleMute}
                        aria-label="Âm lượng"
                      >
                        {isMuted || volume === 0 ? <VolumeX size={20} /> : <Volume2 size={20} />}
                      </button>
                      <input
                        type="range"
                        min="0"
                        max="1"
                        step="0.05"
                        value={isMuted ? 0 : volume}
                        onChange={handleVolumeChange}
                        className="player-volume-slider"
                        aria-label="Thanh âm lượng"
                      />
                    </div>

                    {/* Time */}
                    <div className="player-time-display">
                      <span>{formatSeconds(currentTime)}</span>
                      <span className="player-time-separator">/</span>
                      <span>{formatSeconds(duration)}</span>
                    </div>
                  </div>

                  <div className="player-actions-right">
                    <button
                      type="button"
                      className="player-btn"
                      onClick={toggleFullscreen}
                      title="Toàn màn hình"
                      aria-label="Toàn màn hình"
                    >
                      {isFullscreen ? <Minimize size={20} /> : <Maximize size={20} />}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Control Toolbar: Player Mode Switcher & Server Switcher */}
      <div className="player-subbar glass">
        {/* Player Mode Switcher */}
        {hasEmbed && (
          <div className="player-mode-toggle-group">
            <span className="subbar-label">Chế độ Player:</span>
            <div className="mode-toggle-buttons">
              {hasHls && (
                <button
                  type="button"
                  className={`mode-btn ${playerMode === 'hls' ? 'active' : ''}`}
                  onClick={() => setPlayerMode('hls')}
                >
                  <Zap size={14} />
                  <span>Trực Tiếp (HLS)</span>
                </button>
              )}
              <button
                type="button"
                className={`mode-btn ${playerMode === 'embed' ? 'active' : ''}`}
                onClick={() => setPlayerMode('embed')}
              >
                <ShieldCheck size={14} />
                <span>Dự Phòng (Iframe)</span>
              </button>
            </div>
          </div>
        )}

        {/* Server Switcher */}
        {sources.length > 1 && (
          <div className="player-server-group">
            <span className="subbar-label">
              <Server size={14} /> Server nguồn:
            </span>
            <div className="server-pills">
              {sources.map((src, idx) => (
                <button
                  key={src.id || idx}
                  type="button"
                  className={`server-pill ${idx === activeSourceIndex ? 'active' : ''}`}
                  onClick={() => setActiveSourceIndex(idx)}
                >
                  {src.serverName || `Server #${idx + 1}`}
                  {src.language && <span className="server-lang">({src.language})</span>}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default VideoPlayer;
