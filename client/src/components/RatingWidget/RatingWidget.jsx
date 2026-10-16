import React, { useState, useEffect } from 'react';
import { Star, Trash2 } from 'lucide-react';
import { ratingService } from '../../services/api/ratingService';
import { useAuth } from '../../context/AuthContext';
import './RatingWidget.css';

const RatingWidget = ({ movieId, onRatingUpdated }) => {
  const { isAuthenticated } = useAuth();
  const [summary, setSummary] = useState(null);
  const [myRating, setMyRating] = useState(null);
  const [hoverRating, setHoverRating] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [feedback, setFeedback] = useState(null);

  const fetchRatingData = async () => {
    if (!movieId) return;
    try {
      const sum = await ratingService.getRatingSummary(movieId);
      setSummary(sum);

      if (isAuthenticated) {
        const my = await ratingService.getMyRating(movieId);
        setMyRating(my?.rating || null);
      } else {
        setMyRating(null);
      }
    } catch (err) {
      console.warn('Could not load ratings', err);
    }
  };

  useEffect(() => {
    fetchRatingData();
  }, [movieId, isAuthenticated]);

  const handleRate = async (score) => {
    if (!isAuthenticated) {
      setFeedback('Vui lòng đăng nhập để đánh giá phim.');
      return;
    }
    if (isSubmitting) return;

    setIsSubmitting(true);
    setFeedback(null);
    try {
      await ratingService.rateMovie(movieId, score);
      setMyRating(score);
      setFeedback(`Đã lưu đánh giá ${score}/10 của bạn!`);
      await fetchRatingData();
      if (onRatingUpdated) onRatingUpdated();
    } catch (err) {
      setFeedback('Không thể lưu đánh giá. Thử lại sau.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteRating = async () => {
    if (!myRating || isSubmitting) return;
    setIsSubmitting(true);
    try {
      await ratingService.deleteRating(movieId);
      setMyRating(null);
      setFeedback('Đã gỡ đánh giá của bạn.');
      await fetchRatingData();
      if (onRatingUpdated) onRatingUpdated();
    } catch (err) {
      setFeedback('Lỗi khi gỡ đánh giá.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const avg = summary?.averageRating ? Number(summary.averageRating).toFixed(1) : 'Chưa có';
  const total = summary?.totalRatings || 0;

  return (
    <div className="rating-widget glass">
      <div className="rating-widget-overview">
        <div className="rating-score-box">
          <div className="rating-score-number">
            <Star size={28} fill="#f59e0b" color="#f59e0b" />
            <span>{avg}</span>
          </div>
          <span className="rating-score-total">/10 ({total} lượt đánh giá)</span>
        </div>

        {/* Interactive Star Picker */}
        <div className="rating-interactive-section">
          <span className="rating-prompt">
            {myRating ? `Đánh giá của bạn: ${myRating}/10` : 'Đánh giá bộ phim này (1-10):'}
          </span>
          <div className="rating-stars-row">
            {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((star) => {
              const isFilled = (hoverRating || myRating || 0) >= star;
              return (
                <button
                  key={star}
                  type="button"
                  className="rating-star-btn"
                  onMouseEnter={() => setHoverRating(star)}
                  onMouseLeave={() => setHoverRating(0)}
                  onClick={() => handleRate(star)}
                  title={`${star}/10 sao`}
                  aria-label={`${star} sao`}
                >
                  <Star
                    size={22}
                    fill={isFilled ? '#f59e0b' : 'none'}
                    color={isFilled ? '#f59e0b' : '#4b5563'}
                  />
                </button>
              );
            })}
          </div>

          {myRating && (
            <button
              className="rating-remove-btn"
              onClick={handleDeleteRating}
              title="Xóa đánh giá của bạn"
            >
              <Trash2 size={13} /> Gỡ đánh giá
            </button>
          )}

          {feedback && <div className="rating-feedback-msg">{feedback}</div>}
        </div>
      </div>

      {/* Distribution Bars */}
      {summary?.distribution && total > 0 && (
        <div className="rating-distribution-section">
          <span className="distribution-title">Phổ điểm đánh giá:</span>
          <div className="distribution-bars">
            {[10, 9, 8, 7, 6, 5, 4, 3, 2, 1].map((score) => {
              const count = summary.distribution[String(score)] || 0;
              const percent = total > 0 ? (count / total) * 100 : 0;
              return (
                <div key={score} className="distribution-row">
                  <span className="dist-score-label">{score}★</span>
                  <div className="dist-track">
                    <div className="dist-fill" style={{ width: `${percent}%` }}></div>
                  </div>
                  <span className="dist-count">{count}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};

export default RatingWidget;
