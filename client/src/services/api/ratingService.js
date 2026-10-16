import api from './client';

export const ratingService = {
  async rateMovie(movieId, score) {
    const res = await api.post(`/movies/${movieId}/rating`, { score });
    return res.data;
  },

  async getMyRating(movieId) {
    try {
      const res = await api.get(`/movies/${movieId}/rating/me`);
      return res.data;
    } catch (e) {
      return null;
    }
  },

  async deleteRating(movieId) {
    const res = await api.delete(`/movies/${movieId}/rating`);
    return res.data;
  },

  async getRatingSummary(movieId) {
    const res = await api.get(`/movies/${movieId}/rating-summary`);
    return res.data;
  },
};
