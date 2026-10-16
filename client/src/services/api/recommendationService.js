import api from './client';

export const recommendationService = {
  async getRecommendations(params = {}, isAuthenticated = false) {
    const endpoint = isAuthenticated ? '/recommendations' : '/recommendations/public';
    try {
      const res = await api.get(endpoint, { params });
      return res.data;
    } catch (e) {
      if (isAuthenticated) {
        // Fallback to public if token expired or invalid
        const res = await api.get('/recommendations/public', { params });
        return res.data;
      }
      throw e;
    }
  },
};
