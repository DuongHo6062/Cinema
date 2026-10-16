import api from './client';

export const adminService = {
  async getDashboard() {
    const res = await api.get('/admin/dashboard');
    return res.data;
  },

  async getRecentActivity() {
    const res = await api.get('/admin/dashboard/recent');
    return res.data;
  },

  async getMovies(params = {}) {
    const res = await api.get('/admin/movies', { params });
    return res.data;
  },

  async getUsers(params = {}) {
    const res = await api.get('/admin/users', { params });
    return res.data;
  },

  async getComments(params = {}) {
    const res = await api.get('/admin/comments', { params });
    return res.data;
  },

  async deleteComment(commentId) {
    const res = await api.delete(`/admin/comments/${commentId}`);
    return res.data;
  },

  async importKKPhim(slug, options = { vietsubOnly: true, englishOnly: true }) {
    const res = await api.post(`/admin/import/kkphim/movie/${slug}`, null, {
      params: options,
    });
    return res.data;
  },

  async syncKKPhim(payload) {
    const res = await api.post('/admin/import/kkphim/sync', payload);
    return res.data;
  },

  async searchKKPhim(keyword, page = 1) {
    const res = await api.get('/import/kkphim/search', {
      params: { keyword, page },
    });
    return res.data;
  },
};
