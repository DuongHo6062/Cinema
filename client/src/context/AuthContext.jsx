import React, { createContext, useContext, useState, useEffect } from 'react';
import { authService } from '../services/api/authService';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(authService.getStoredUser());
  const [isAuthenticated, setIsAuthenticated] = useState(authService.isAuthenticated());
  const [loading, setLoading] = useState(true);

  const fetchProfile = async () => {
    if (authService.isAuthenticated()) {
      try {
        const profile = await authService.getCurrentUser();
        setUser(profile);
        localStorage.setItem('shin_user', JSON.stringify(profile));
        setIsAuthenticated(true);
      } catch (err) {
        console.warn('Failed to load user profile', err);
      }
    } else {
      setUser(null);
      setIsAuthenticated(false);
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchProfile();

    const handleAuthChange = () => {
      const stored = authService.getStoredUser();
      setUser(stored);
      setIsAuthenticated(authService.isAuthenticated());
    };

    window.addEventListener('shin_auth_change', handleAuthChange);
    return () => window.removeEventListener('shin_auth_change', handleAuthChange);
  }, []);

  const login = async (credentials) => {
    const data = await authService.login(credentials);
    setUser(data.user);
    setIsAuthenticated(true);
    return data;
  };

  const register = async (userData) => {
    return await authService.register(userData);
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
    setIsAuthenticated(false);
  };

  const isAdmin = user?.role === 'Admin' || user?.role?.toLowerCase() === 'admin';

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isAdmin,
        loading,
        login,
        register,
        logout,
        refreshProfile: fetchProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
