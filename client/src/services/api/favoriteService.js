import api from './client';

export const favoriteService = {
  async getFavorites(page = 1, pageSize = 20) {
    const res = await api.get('/favorites', {
      params: { page, pageSize },
    });
    return res.data;
  },

  async addFavorite(movieId) {
    const res = await api.post(`/favorites/${movieId}`);
    return res.data;
  },

  async removeFavorite(movieId) {
    const res = await api.delete(`/favorites/${movieId}`);
    return res.data;
  },

  async checkFavoriteStatus(movieId) {
    const res = await api.get(`/favorites/${movieId}/status`);
    return res.data;
  },
};
