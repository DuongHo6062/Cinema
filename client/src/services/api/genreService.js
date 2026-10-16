import api from './client';

export const genreService = {
  async getGenres() {
    const res = await api.get('/genres');
    return res.data;
  },

  async getGenreById(id) {
    const res = await api.get(`/genres/${id}`);
    return res.data;
  },
};
