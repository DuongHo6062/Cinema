import React from 'react';
import { Film, AlertCircle, RefreshCw } from 'lucide-react';
import './Common.css';

export const EmptyState = ({
  icon: Icon = Film,
  title = 'Không có dữ liệu',
  message = 'Không tìm thấy nội dung phù hợp với tiêu chí của bạn.',
  actionText,
  onAction,
}) => {
  return (
    <div className="empty-state">
      <div className="empty-state-icon">
        <Icon size={44} />
      </div>
      <h3 className="empty-state-title">{title}</h3>
      <p className="empty-state-message">{message}</p>
      {actionText && onAction && (
        <button className="btn-secondary empty-state-btn" onClick={onAction}>
          {actionText}
        </button>
      )}
    </div>
  );
};

export const ErrorState = ({
  message = 'Không thể tải dữ liệu từ máy chủ.',
  onRetry,
}) => {
  return (
    <div className="error-state">
      <div className="error-state-icon">
        <AlertCircle size={44} />
      </div>
      <h3 className="error-state-title">Đã xảy ra lỗi</h3>
      <p className="error-state-message">{message}</p>
      {onRetry && (
        <button className="btn-primary error-state-btn" onClick={onRetry}>
          <RefreshCw size={16} />
          Thử lại
        </button>
      )}
    </div>
  );
};
