USE master;
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'QL_BanVeMayBay')
BEGIN
    ALTER DATABASE QL_BanVeMayBay SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QL_BanVeMayBay;
END
GO

CREATE DATABASE QL_BanVeMayBay;
GO

USE QL_BanVeMayBay;
GO

-- =============================
-- 1. HÃNG HÀNG KHÔNG
-- =============================
CREATE TABLE HangHangKhong (
    MaHang VARCHAR(10) PRIMARY KEY,
    TenHang NVARCHAR(100) NOT NULL,
    DaXoa BIT DEFAULT 0
);

-- =============================
-- 2. LOẠI MÁY BAY
-- =============================
CREATE TABLE LoaiMayBay (
    MaLoaiMB VARCHAR(20) PRIMARY KEY,
    TenLoaiMB NVARCHAR(100),
    MaHang VARCHAR(10),
    SoLuongGhe INT NOT NULL,
    DaXoa BIT DEFAULT 0,
    CONSTRAINT FK_LoaiMayBay_HangHangKhong FOREIGN KEY (MaHang) REFERENCES HangHangKhong(MaHang)
);

-- =============================
-- 3. SÂN BAY
-- =============================
CREATE TABLE SanBay (
    MaSB VARCHAR(5) PRIMARY KEY,
    TenSB NVARCHAR(100) NOT NULL,
    ThanhPho NVARCHAR(100) NOT NULL,
    DaXoa BIT DEFAULT 0
);

-- =============================
-- 4. TUYẾN BAY
-- =============================
CREATE TABLE TuyenBay (
    MaTuyen VARCHAR(20) PRIMARY KEY,
    MaSBDi VARCHAR(5),
    MaSBDen VARCHAR(5),
    KhoangCach FLOAT,
    ThoiGianBayDuKien INT,
    TrangThai BIT DEFAULT 1,
    DaXoa BIT DEFAULT 0,
    CONSTRAINT FK_TuyenBay_SBDi FOREIGN KEY (MaSBDi) REFERENCES SanBay(MaSB),
    CONSTRAINT FK_TuyenBay_SBDen FOREIGN KEY (MaSBDen) REFERENCES SanBay(MaSB),
    CONSTRAINT CHK_TuyenBay CHECK (MaSBDi <> MaSBDen)
);

-- =============================
-- 5. DANH MỤC HẠNG GHẾ (Đổi tên từ HangGhe)
-- =============================
CREATE TABLE DanhMucHangGhe (
    MaHangGhe VARCHAR(20) PRIMARY KEY,
    TenHangGhe NVARCHAR(50) NOT NULL,
    HeSoGia FLOAT DEFAULT 1.0,
    HanhLyXachTay NVARCHAR(50),
    HanhLyKyGui NVARCHAR(50),
    HoanVe NVARCHAR(100),
    DoiLich NVARCHAR(100),
    MauSac VARCHAR(20),
    DaXoa BIT DEFAULT 0
);


-- =============================
-- 6. GHẾ (Đổi tên từ DanhMucGhe)
-- =============================
CREATE TABLE Ghe (
    MaGhe INT IDENTITY PRIMARY KEY,
    MaLoaiMB VARCHAR(20),
    SoGhe VARCHAR(10) NOT NULL,
    MaHangGhe VARCHAR(20),
    ViTriHang INT,
    ViTriCot INT,
    PhuThu DECIMAL(18,2) DEFAULT 0,
    TrangThai BIT DEFAULT 1,
    DaXoa BIT DEFAULT 0,
    FOREIGN KEY (MaLoaiMB) REFERENCES LoaiMayBay(MaLoaiMB),
    FOREIGN KEY (MaHangGhe) REFERENCES DanhMucHangGhe(MaHangGhe)
);
-- =============================
-- 7. TIỆN ÍCH
-- =============================
CREATE TABLE TienIch (
    MaTienIch INT IDENTITY PRIMARY KEY,
    TenTienIch NVARCHAR(100) NOT NULL,
    Icon VARCHAR(50),
    DaXoa BIT DEFAULT 0
);

-- =============================
-- 8. CHI TIẾT TIỆN ÍCH
-- =============================
CREATE TABLE ChiTietTienIch (
    MaHangGhe VARCHAR(20),
    MaTienIch INT,
    PRIMARY KEY (MaHangGhe, MaTienIch),
    FOREIGN KEY (MaHangGhe) REFERENCES DanhMucHangGhe(MaHangGhe),
    FOREIGN KEY (MaTienIch) REFERENCES TienIch(MaTienIch)
);

-- =============================
-- 9. CHUYẾN BAY
-- =============================
CREATE TABLE ChuyenBay (
    MaCB VARCHAR(20) PRIMARY KEY,
    MaTuyen VARCHAR(20),
    MaLoaiMB VARCHAR(20),
    MaHang VARCHAR(10),
    NgayGioBay DATETIME NOT NULL,
    GiaVeCoBan DECIMAL(18,2) NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Sắp khởi hành',
    DaXoa BIT DEFAULT 0,
    FOREIGN KEY (MaTuyen) REFERENCES TuyenBay(MaTuyen),
    FOREIGN KEY (MaLoaiMB) REFERENCES LoaiMayBay(MaLoaiMB),
    FOREIGN KEY (MaHang) REFERENCES HangHangKhong(MaHang)
);

-- =============================
-- 10. TÀI KHOẢN
-- =============================
CREATE TABLE TaiKhoan (
    MaTK INT IDENTITY PRIMARY KEY,
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL,
    LoaiTaiKhoan NVARCHAR(20),
    TrangThai BIT DEFAULT 1,
    DaXoa BIT DEFAULT 0
);

-- =============================
-- 11. KHÁCH HÀNG
-- =============================
CREATE TABLE KhachHang (
    MaKH VARCHAR(10) PRIMARY KEY,
    MaTK INT UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    CCCD VARCHAR(20) UNIQUE NOT NULL,
    SoDienThoai VARCHAR(15),
    Email VARCHAR(100),
    DiaChi NVARCHAR(200),
    AnhDaiDien NVARCHAR(255),
    LoaiThanhVien NVARCHAR(20) DEFAULT N'Thường',
    DiemTichLuy INT DEFAULT 0,
    DaXoa BIT DEFAULT 0,
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);

-- =============================
-- 12. NHÂN VIÊN
-- =============================
CREATE TABLE NhanVien (
    MaNV VARCHAR(10) PRIMARY KEY,
    MaTK INT UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    CCCD VARCHAR(20) UNIQUE NOT NULL,
    SoDienThoai VARCHAR(15),
    Email VARCHAR(100),
    DiaChi NVARCHAR(200),
    ChucVu NVARCHAR(50) NOT NULL,
    NgayVaoLam DATE DEFAULT GETDATE(),
    AnhDaiDien NVARCHAR(255),
    DaXoa BIT DEFAULT 0,
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);

-- =============================
-- 13. HÓA ĐƠN (Đổi tên từ DatVe)
-- =============================
CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY PRIMARY KEY,
    MaTK INT, -- Người thực hiện thanh toán
    NgayLap DATETIME DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) DEFAULT 0,
    TrangThaiThanhToan NVARCHAR(50) DEFAULT N'Chờ thanh toán',
    DaXoa BIT DEFAULT 0,
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);

-- =============================
-- 14. VÉ (Chi tiết của Hóa đơn)
-- =============================
CREATE TABLE Ve (
    MaVe VARCHAR(20) PRIMARY KEY,
    MaHoaDon INT,
    MaCB VARCHAR(20),
    MaGhe INT,
    MaKH VARCHAR(10), -- Kết nối với khách hàng để tích điểm/lịch sử

    -- Thông tin hành khách (Lưu Snapshot để đặt vé hộ người không có tài khoản)
    TenHanhKhach NVARCHAR(100) NOT NULL, 
    LoaiHanhKhach NVARCHAR(20), -- Người lớn, Trẻ em, Em bé
    NgaySinh DATE,
    CCCD_Passport VARCHAR(50),
    SoDienThoai VARCHAR(15),

    GiaVeThucTe DECIMAL(18,2), -- Giá sau khi tính hệ số hạng ghế và khuyến mãi
    DaXoa BIT DEFAULT 0,

    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    FOREIGN KEY (MaCB) REFERENCES ChuyenBay(MaCB),
    FOREIGN KEY (MaGhe) REFERENCES Ghe(MaGhe),
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),

    CONSTRAINT UQ_GheChuyenBay UNIQUE (MaCB, MaGhe)
);
GO

-- =============================
-- 14. VÉ (Chi tiết của Hóa đơn)
-- =============================
CREATE TABLE Ve (
    MaVe VARCHAR(20) PRIMARY KEY,
    MaHoaDon INT,
    MaCB VARCHAR(20),
    MaGhe INT,
    MaKH VARCHAR(10), -- Kết nối với khách hàng để tích điểm/lịch sử

    -- Thông tin hành khách (Lưu Snapshot để đặt vé hộ người không có tài khoản)
    TenHanhKhach NVARCHAR(100) NOT NULL, 
    LoaiHanhKhach NVARCHAR(20), -- Người lớn, Trẻ em, Em bé
    NgaySinh DATE,
    CCCD_Passport VARCHAR(50),
    SoDienThoai VARCHAR(15),

    GiaVeThucTe DECIMAL(18,2), -- Giá sau khi tính hệ số hạng ghế và khuyến mãi
    DaXoa BIT DEFAULT 0,

    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    FOREIGN KEY (MaCB) REFERENCES ChuyenBay(MaCB),
    FOREIGN KEY (MaGhe) REFERENCES Ghe(MaGhe),
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),

    CONSTRAINT UQ_GheChuyenBay UNIQUE (MaCB, MaGhe)
);
GO