import api from './client';

export const movieService = {
  async getMovies(params = {}) {
    const res = await api.get('/movies', { params });
    return res.data;
  },

  async getMovieById(id) {
    const res = await api.get(`/movies/${id}`);
    return res.data;
  },

  async getMovieBySlug(slug) {
    const res = await api.get(`/movies/slug/${slug}`);
    return res.data;
  },

  async getMovieEpisodes(id) {
    const res = await api.get(`/movies/${id}/episodes`);
    return res.data;
  },

  async getSimilarMovies(id, params = {}) {
    const res = await api.get(`/movies/${id}/similar`, { params });
    return res.data;
  },
};
