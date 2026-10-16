import api from './client';

export const authService = {
  async register(data) {
    const res = await api.post('/auth/register', data);
    return res.data;
  },

  async login(credentials) {
    const res = await api.post('/auth/login', credentials);
    if (res.data?.accessToken) {
      localStorage.setItem('shin_access_token', res.data.accessToken);
      localStorage.setItem('shin_refresh_token', res.data.refreshToken);
      localStorage.setItem('shin_user', JSON.stringify(res.data.user));
      window.dispatchEvent(new Event('shin_auth_change'));
    }
    return res.data;
  },

  async logout() {
    try {
      const refreshToken = localStorage.getItem('shin_refresh_token');
      if (refreshToken) {
        await api.post('/auth/logout', { refreshToken });
      }
    } catch (e) {
      console.warn('Logout API notification failed', e);
    } finally {
      localStorage.removeItem('shin_access_token');
      localStorage.removeItem('shin_refresh_token');
      localStorage.removeItem('shin_user');
      window.dispatchEvent(new Event('shin_auth_change'));
    }
  },

  async getCurrentUser() {
    const res = await api.get('/users/me');
    return res.data;
  },

  async changePassword(data) {
    const res = await api.post('/users/change-password', data);
    return res.data;
  },

  getStoredUser() {
    try {
      const u = localStorage.getItem('shin_user');
      return u ? JSON.parse(u) : null;
    } catch {
      return null;
    }
  },

  isAuthenticated() {
    return !!localStorage.getItem('shin_access_token');
  },
};
