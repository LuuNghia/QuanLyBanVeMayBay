USE QL_BanVeMayBay;
GO

-- ==============================================
-- 1. HÃNG HÀNG KHÔNG
-- ==============================================
INSERT INTO HangHangKhong (MaHang, TenHang) VALUES 
('VN', N'Vietnam Airlines'),
('VJ', N'VietJet Air'),
('QH', N'Bamboo Airways');
GO

-- ==============================================
-- 2. LOẠI MÁY BAY
-- ==============================================
INSERT INTO LoaiMayBay (MaLoaiMB, TenLoaiMB, MaHang, SoLuongGhe) VALUES 
('VN-A321', N'Airbus A321', 'VN', 200),
('VN-B787', N'Boeing 787 Dreamliner', 'VN', 300),
('VJ-A320', N'Airbus A320', 'VJ', 180),
('QH-B787', N'Boeing 787-9', 'QH', 290);
GO

-- ==============================================
-- 3. SÂN BAY
-- ==============================================
INSERT INTO SanBay (MaSB, TenSB, ThanhPho) VALUES 
('SGN', N'Tân Sơn Nhất', N'TP. Hồ Chí Minh'),
('HAN', N'Nội Bài', N'Hà Nội'),
('DAD', N'Đà Nẵng', N'Đà Nẵng'),
('PQC', N'Phú Quốc', N'Kiên Giang'),
('CXR', N'Cam Ranh', N'Khánh Hòa');
GO

-- ==============================================
-- 4. TUYẾN BAY
-- ==============================================
INSERT INTO TuyenBay (MaTuyen, MaSBDi, MaSBDen, KhoangCach, ThoiGianBayDuKien) VALUES 
('SGN-HAN', 'SGN', 'HAN', 1190, 125),
('HAN-SGN', 'HAN', 'SGN', 1190, 125),
('SGN-DAD', 'SGN', 'DAD', 600, 85),
('DAD-SGN', 'DAD', 'SGN', 600, 85),
('HAN-PQC', 'HAN', 'PQC', 1200, 130);
GO

-- ==============================================
-- 5. HẠNG GHẾ
-- ==============================================
INSERT INTO DanhMucHangGhe (MaHangGhe, TenHangGhe, HeSoGia, HanhLyXachTay, HanhLyKyGui, HoanVe, DoiLich, MauSac) VALUES 
('ECO', N'Phổ thông ', 1.0, N'7kg', N'Không bao gồm', N'Không hỗ trợ', N'Thu phí 300k', '#10b981'),
('DELUXE', N'Phổ thông đặc biệt', 1.5, N'10kg', N'20kg', N'Thu phí 500k', N'Miễn phí', '#3b82f6'),
('FIRST', N'Hạng nhất', 5.0, N'18kg', N'60kg', N'Miễn phí', N'Miễn phí', '#ef4444'),
('BIZ', N'Thương gia ', 3.0, N'14kg', N'40kg', N'Miễn phí', N'Miễn phí', '#f59e0b');
GO

-- ==============================================
-- 6. TIỆN ÍCH
-- ==============================================
SET IDENTITY_INSERT TienIch ON;
INSERT INTO TienIch (MaTienIch, TenTienIch, Icon) VALUES 
(1, N'Suất ăn nóng', 'bi-cup-hot'),
(2, N'Phòng chờ thương gia', 'bi-star'),
(3, N'Wifi trên máy bay', 'bi-wifi'),
(4, N'Hệ thống giải trí', 'bi-display');
SET IDENTITY_INSERT TienIch OFF;
GO

-- ==============================================
-- 7. CHI TIẾT TIỆN ÍCH (MAPPING)
-- ==============================================
INSERT INTO ChiTietTienIch (MaHangGhe, MaTienIch) VALUES 
('ECO', 4),
('DELUXE', 1), ('DELUXE', 4),
('BIZ', 1), ('BIZ', 2), ('BIZ', 3), ('BIZ', 4);
GO

-- ==============================================
-- 8. DANH MỤC GHẾ
-- ==============================================
SET IDENTITY_INSERT DanhMucGhe ON;
INSERT INTO DanhMucGhe (MaGhe, MaLoaiMB, SoGhe, MaHangGhe, ViTriHang, ViTriCot, PhuThu) VALUES 
-- Máy bay VN-A321
(1, 'VN-A321', '1A', 'BIZ', 1, 1, 0),
(2, 'VN-A321', '1C', 'BIZ', 1, 3, 0),
(3, 'VN-A321', '15A', 'ECO', 15, 1, 100000), -- Ghế cạnh cửa sổ phụ thu 100k
(4, 'VN-A321', '15B', 'ECO', 15, 2, 0),
-- Máy bay VN-B787
(5, 'VN-B787', '2A', 'BIZ', 2, 1, 0),
(6, 'VN-B787', '10A', 'DELUXE', 10, 1, 50000),
(7, 'VN-B787', '10B', 'DELUXE', 10, 2, 0);
SET IDENTITY_INSERT DanhMucGhe OFF;
GO

-- ==============================================
-- 9. CHUYẾN BAY
-- ==============================================
INSERT INTO ChuyenBay (MaCB, MaTuyen, MaLoaiMB, MaHang, NgayGioBay, GiaVeCoBan, TrangThai) VALUES 
('VN210', 'SGN-HAN', 'VN-B787', 'VN', '2026-05-10 08:00:00', 1500000, N'Sắp khởi hành'),
('VJ123', 'SGN-DAD', 'VJ-A320', 'VJ', '2026-05-10 14:30:00', 900000, N'Sắp khởi hành'),
('QH305', 'HAN-PQC', 'QH-B787', 'QH', '2026-05-12 09:15:00', 2000000, N'Sắp khởi hành');
GO

-- ==============================================
-- 10. TÀI KHOẢN
-- ==============================================
SET IDENTITY_INSERT TaiKhoan ON;
INSERT INTO TaiKhoan (MaTK, TenDangNhap, MatKhau, LoaiTaiKhoan) VALUES 
(1, 'admin', '123456', 'Admin'),
(2, 'nhanvien1', '123456', 'NhanVien'),
(3, 'khachhang1', '123456', 'KhachHang');
SET IDENTITY_INSERT TaiKhoan OFF;
GO

-- ==============================================
-- 11. KHÁCH HÀNG
-- ==============================================
INSERT INTO KhachHang (MaKH, MaTK, HoTen, GioiTinh, NgaySinh, CCCD, SoDienThoai, Email, LoaiThanhVien, DiemTichLuy) VALUES 
('KH001', 3, N'Nguyễn Văn An', N'Nam', '1990-05-20', '079190001234', '0901234567', 'an.nguyen@email.com', N'Vàng', 1500);
GO

-- ==============================================
-- 12. NHÂN VIÊN
-- ==============================================
INSERT INTO NhanVien (MaNV, MaTK, HoTen, GioiTinh, NgaySinh, CCCD, SoDienThoai, Email, ChucVu) VALUES 
('NV001', 2, N'Trần Thị Hoa', N'Nữ', '1995-10-15', '079195009876', '0987654321', 'hoa.tran@skybooking.vn', N'Nhân viên bán vé');
GO

-- ==============================================
-- 13. ĐẶT VÉ
-- ==============================================
SET IDENTITY_INSERT DatVe ON;
INSERT INTO DatVe (MaDatVe, MaTK, NgayDat, TongTien, TrangThaiThanhToan) VALUES 
(1, 3, '2026-05-01 10:00:00', 4500000, N'Đã thanh toán'),
(2, 3, '2026-05-01 11:30:00', 1600000, N'Chờ thanh toán');
SET IDENTITY_INSERT DatVe OFF;
GO

-- ==============================================
-- 14. VÉ
-- ==============================================
INSERT INTO Ve (MaVe, MaDatVe, MaCB, MaGhe, TenHanhKhach, LoaiHanhKhach, NgaySinh, CCCD_Passport, GiaVeThucTe) VALUES 
-- Vé 1: Chuyến SGN-HAN, Ghế BIZ (Hệ số 3.0), Giá cơ bản 1.500.000 -> 4.500.000
('TK-VN210-001', 1, 'VN210', 5, N'Nguyễn Văn An', N'Người lớn', '1990-05-20', '079190001234', 4500000),

-- Vé 2: Chuyến SGN-HAN, Ghế ECO (Hệ số 1.0) + Phụ thu ghế 100k -> 1.600.000
('TK-VN210-002', 2, 'VN210', 3, N'Lê Hoàng Anh', N'Trẻ em', '2015-08-10', '079215001111', 1600000);
GO