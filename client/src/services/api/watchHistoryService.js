import api from './client';

export const watchHistoryService = {
  async saveProgress({ episodeId, progressSeconds, durationSeconds }) {
    const res = await api.post('/watch-history', {
      episodeId,
      progressSeconds: Math.floor(progressSeconds || 0),
      durationSeconds: Math.floor(durationSeconds || 0),
    });
    return res.data;
  },

  async getHistory(page = 1, pageSize = 20) {
    const res = await api.get('/watch-history', {
      params: { page, pageSize },
    });
    return res.data;
  },

  async getContinueWatching(limit = 10) {
    const res = await api.get('/watch-history/continue', {
      params: { limit },
    });
    return res.data;
  },

  async deleteHistory(episodeId) {
    const res = await api.delete(`/watch-history/${episodeId}`);
    return res.data;
  },
};
