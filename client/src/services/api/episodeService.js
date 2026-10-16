import api from './client';

export const episodeService = {
  async getEpisodeById(id) {
    const res = await api.get(`/episodes/${id}`);
    return res.data;
  },

  async getEpisodeSources(id) {
    const res = await api.get(`/episodes/${id}/sources`);
    return res.data;
  },
};
