import React, { useState, useEffect } from 'react';
import { MessageSquare, Send, Edit2, Trash2, Check, X } from 'lucide-react';
import { commentService } from '../../services/api/commentService';
import { useAuth } from '../../context/AuthContext';
import './CommentSection.css';

const CommentSection = ({ movieId }) => {
  const { user, isAuthenticated } = useAuth();
  const [comments, setComments] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [newComment, setNewComment] = useState('');
  const [loading, setLoading] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [editContent, setEditContent] = useState('');
  const [errorMsg, setErrorMsg] = useState(null);

  const fetchComments = async () => {
    if (!movieId) return;
    try {
      const res = await commentService.getMovieComments(movieId, 1, 50);
      setComments(res?.items || []);
      setTotalCount(res?.totalCount || res?.totalItems || 0);
    } catch (err) {
      console.warn('Could not load comments', err);
    }
  };

  useEffect(() => {
    fetchComments();
  }, [movieId]);

  const handleAddComment = async (e) => {
    e.preventDefault();
    if (!newComment.trim() || loading) return;
    if (!isAuthenticated) {
      setErrorMsg('Vui lòng đăng nhập để bình luận.');
      return;
    }

    setLoading(true);
    setErrorMsg(null);
    try {
      await commentService.addComment(movieId, newComment.trim());
      setNewComment('');
      await fetchComments();
    } catch (err) {
      setErrorMsg(err.response?.data?.message || 'Không thể gửi bình luận.');
    } finally {
      setLoading(false);
    }
  };

  const handleStartEdit = (comment) => {
    setEditingId(comment.id);
    setEditContent(comment.content);
  };

  const handleSaveEdit = async (commentId) => {
    if (!editContent.trim()) return;
    try {
      await commentService.editComment(commentId, editContent.trim());
      setEditingId(null);
      await fetchComments();
    } catch (err) {
      alert('Không thể sửa bình luận.');
    }
  };

  const handleDeleteComment = async (commentId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bình luận này?')) return;
    try {
      await commentService.deleteComment(commentId);
      await fetchComments();
    } catch (err) {
      alert('Không thể xóa bình luận.');
    }
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="comment-section">
      <div className="comment-section-header">
        <h3 className="comment-section-title">
          <MessageSquare size={20} />
          Bình Luận <span className="comment-count">({totalCount})</span>
        </h3>
      </div>

      {/* Add comment input */}
      <form onSubmit={handleAddComment} className="comment-form glass">
        <div className="comment-form-avatar">
          {user?.username ? user.username.charAt(0).toUpperCase() : '?'}
        </div>
        <div className="comment-form-input-wrapper">
          <textarea
            placeholder={
              isAuthenticated
                ? 'Chia sẻ cảm nghĩ của bạn về bộ phim này...'
                : 'Đăng nhập để tham gia thảo luận cùng cộng đồng...'
            }
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            disabled={!isAuthenticated || loading}
            rows={2}
            className="comment-textarea"
          />
          {errorMsg && <span className="comment-error-msg">{errorMsg}</span>}
        </div>
        <button
          type="submit"
          className="btn-primary comment-submit-btn"
          disabled={!isAuthenticated || !newComment.trim() || loading}
        >
          <Send size={15} />
          <span>Gửi</span>
        </button>
      </form>

      {/* Comments List */}
      <div className="comment-list">
        {comments.length === 0 ? (
          <div className="comment-empty">
            <p>Chưa có bình luận nào. Hãy là người đầu tiên để lại nhận xét!</p>
          </div>
        ) : (
          comments.map((comment) => {
            const isOwner = comment.isOwner || (user && user.id === comment.userId);
            const isEditing = editingId === comment.id;

            return (
              <div key={comment.id} className="comment-item glass">
                <div className="comment-item-avatar">
                  {comment.username ? comment.username.charAt(0).toUpperCase() : 'U'}
                </div>

                <div className="comment-item-body">
                  <div className="comment-item-header">
                    <span className="comment-author">{comment.username}</span>
                    <span className="comment-date">
                      {formatDate(comment.createdAt)}
                      {comment.isEdited && ' (đã chỉnh sửa)'}
                    </span>

                    {isOwner && !isEditing && (
                      <div className="comment-actions">
                        <button
                          className="comment-action-btn"
                          onClick={() => handleStartEdit(comment)}
                          title="Sửa"
                          aria-label="Sửa bình luận"
                        >
                          <Edit2 size={13} />
                        </button>
                        <button
                          className="comment-action-btn delete"
                          onClick={() => handleDeleteComment(comment.id)}
                          title="Xóa"
                          aria-label="Xóa bình luận"
                        >
                          <Trash2 size={13} />
                        </button>
                      </div>
                    )}
                  </div>

                  {isEditing ? (
                    <div className="comment-edit-form">
                      <textarea
                        value={editContent}
                        onChange={(e) => setEditContent(e.target.value)}
                        className="comment-edit-textarea"
                        rows={2}
                      />
                      <div className="comment-edit-buttons">
                        <button
                          className="btn-primary edit-save-btn"
                          onClick={() => handleSaveEdit(comment.id)}
                        >
                          <Check size={14} /> Lưu
                        </button>
                        <button
                          className="btn-secondary edit-cancel-btn"
                          onClick={() => setEditingId(null)}
                        >
                          <X size={14} /> Hủy
                        </button>
                      </div>
                    </div>
                  ) : (
                    <p className="comment-content">{comment.content}</p>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
};

export default CommentSection;
