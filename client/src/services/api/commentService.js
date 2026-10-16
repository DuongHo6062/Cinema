import api from './client';

export const commentService = {
  async getMovieComments(movieId, page = 1, pageSize = 20) {
    const res = await api.get(`/movies/${movieId}/comments`, {
      params: { page, pageSize },
    });
    return res.data;
  },

  async addComment(movieId, content) {
    const res = await api.post(`/movies/${movieId}/comments`, { content });
    return res.data;
  },

  async editComment(commentId, content) {
    const res = await api.put(`/comments/${commentId}`, { content });
    return res.data;
  },

  async deleteComment(commentId) {
    const res = await api.delete(`/comments/${commentId}`);
    return res.data;
  },
};
