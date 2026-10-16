import React, { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Film, Lock, User, AlertCircle, ArrowRight } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import './Auth.css';

const LoginPage = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const redirect = searchParams.get('redirect') || '/';

  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!usernameOrEmail.trim() || !password) return;

    setLoading(true);
    setError(null);
    try {
      await login({ usernameOrEmail: usernameOrEmail.trim(), password });
      navigate(redirect, { replace: true });
    } catch (err) {
      setError(
        err.response?.data?.message ||
        'Tên đăng nhập hoặc mật khẩu không chính xác.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card glass">
        {/* Brand */}
        <div className="auth-brand">
          <div className="auth-logo-icon">
            <Film size={24} />
          </div>
          <h2 className="auth-brand-title">
            SHIN <span className="auth-accent">movie</span>
          </h2>
          <p className="auth-subtitle">Đăng nhập để tiếp tục trải nghiệm điện ảnh</p>
        </div>

        {error && (
          <div className="auth-error-alert">
            <AlertCircle size={16} />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="auth-input-group">
            <label className="auth-label">Tài khoản hoặc Email</label>
            <div className="auth-input-wrapper">
              <User size={16} className="auth-input-icon" />
              <input
                type="text"
                placeholder="Nhập tên người dùng hoặc email"
                value={usernameOrEmail}
                onChange={(e) => setUsernameOrEmail(e.target.value)}
                required
                className="auth-input"
              />
            </div>
          </div>

          <div className="auth-input-group">
            <label className="auth-label">Mật khẩu</label>
            <div className="auth-input-wrapper">
              <Lock size={16} className="auth-input-icon" />
              <input
                type="password"
                placeholder="Nhập mật khẩu"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="auth-input"
              />
            </div>
          </div>

          <button
            type="submit"
            className="btn-primary auth-submit-btn"
            disabled={loading}
          >
            {loading ? 'Đang xác thực...' : 'Đăng Nhập'}
            <ArrowRight size={16} />
          </button>
        </form>

        <div className="auth-footer">
          <span>Chưa có tài khoản?</span>
          <Link to={`/register?redirect=${encodeURIComponent(redirect)}`} className="auth-link">
            Đăng ký ngay
          </Link>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
