import React from 'react';
import { Link } from 'react-router-dom';
import { Film, Heart } from 'lucide-react';
import './Footer.css';

const Footer = () => {
  return (
    <footer className="footer-section">
      <div className="footer-container container">
        <div className="footer-top">
          <div className="footer-brand-col">
            <Link to="/" className="footer-brand">
              <span className="brand-logo-icon">
                <Film size={20} />
              </span>
              <span className="brand-title">
                SHIN <span className="brand-title-accent">movie</span>
              </span>
            </Link>
            <p className="footer-desc">
              SHIN movie — Nền tảng xem phim trực tuyến hiện đại với giao diện dark cinematic, 
              cập nhật liên tục những tác phẩm điện ảnh bom tấn và phim bộ đặc sắc chất lượng cao.
            </p>
          </div>

          <div className="footer-links-grid">
            <div className="footer-col">
              <h4 className="footer-col-title">Khám Phá</h4>
              <ul className="footer-links">
                <li><Link to="/movies?type=series">Phim Bộ Mới</Link></li>
                <li><Link to="/movies?type=single">Phim Lẻ Chiếu Rạp</Link></li>
                <li><Link to="/movies?sortBy=rating">Bảng Xếp Hạng</Link></li>
                <li><Link to="/search">Tìm Kiếm Nhanh</Link></li>
              </ul>
            </div>

            <div className="footer-col">
              <h4 className="footer-col-title">Cá Nhân</h4>
              <ul className="footer-links">
                <li><Link to="/favorites">Phim Đã Lưu</Link></li>
                <li><Link to="/history">Lịch Sử Xem</Link></li>
                <li><Link to="/login">Đăng Nhập</Link></li>
                <li><Link to="/register">Tạo Tài Khoản</Link></li>
              </ul>
            </div>

            <div className="footer-col">
              <h4 className="footer-col-title">Hỗ Trợ & Bản Quyền</h4>
              <ul className="footer-links">
                <li><span className="footer-muted-text">Điều khoản sử dụng</span></li>
                <li><span className="footer-muted-text">Chính sách bảo mật</span></li>
                <li><span className="footer-muted-text">Khiếu nại bản quyền</span></li>
                <li><span className="footer-muted-text">Liên hệ hợp tác</span></li>
              </ul>
            </div>
          </div>
        </div>

        <div className="footer-bottom">
          <p className="footer-copyright">
            © {new Date().getFullYear()} <strong className="brand-title-accent">SHIN movie</strong>. Mọi dữ liệu và hình ảnh phục vụ mục đích phi thương mại.
          </p>
          <div className="footer-badge">
            Trải nghiệm chuẩn Streaming Cinema
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
