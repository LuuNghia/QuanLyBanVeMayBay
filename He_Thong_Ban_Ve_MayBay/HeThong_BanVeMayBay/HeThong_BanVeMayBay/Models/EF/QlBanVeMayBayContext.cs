using System;
using System.Collections.Generic;
using HeThong_BanVeMayBay.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HeThong_BanVeMayBay.Models.EF;

public partial class QlBanVeMayBayContext : DbContext
{
    public QlBanVeMayBayContext()
    {
    }

    public QlBanVeMayBayContext(DbContextOptions<QlBanVeMayBayContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChuyenBay> ChuyenBays { get; set; }

    public virtual DbSet<DanhMucHangGhe> DanhMucHangGhes { get; set; }

    public virtual DbSet<Ghe> Ghes { get; set; }

    public virtual DbSet<HangHangKhong> HangHangKhongs { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LoaiMayBay> LoaiMayBays { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<SanBay> SanBays { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TienIch> TienIches { get; set; }

    public virtual DbSet<TuyenBay> TuyenBays { get; set; }

    public virtual DbSet<Ve> Ves { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-V9Q3E3QU\\SQLEXPRESS;Database=QL_BanVeMayBay;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChuyenBay>(entity =>
        {
            entity.HasKey(e => e.MaCb).HasName("PK__ChuyenBa__27258E1ACB84A72A");

            entity.ToTable("ChuyenBay");

            entity.Property(e => e.MaCb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaCB");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.GiaVeCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiMb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaLoaiMB");
            entity.Property(e => e.MaTuyen)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NgayGioBay).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Sắp khởi hành");

            entity.HasOne(d => d.MaHangNavigation).WithMany(p => p.ChuyenBays)
                .HasForeignKey(d => d.MaHang)
                .HasConstraintName("FK__ChuyenBay__MaHan__5CD6CB2B");

            entity.HasOne(d => d.MaLoaiMbNavigation).WithMany(p => p.ChuyenBays)
                .HasForeignKey(d => d.MaLoaiMb)
                .HasConstraintName("FK__ChuyenBay__MaLoa__5BE2A6F2");

            entity.HasOne(d => d.MaTuyenNavigation).WithMany(p => p.ChuyenBays)
                .HasForeignKey(d => d.MaTuyen)
                .HasConstraintName("FK__ChuyenBay__MaTuy__5AEE82B9");
        });

        modelBuilder.Entity<DanhMucHangGhe>(entity =>
        {
            entity.HasKey(e => e.MaHangGhe).HasName("PK__DanhMucH__9774DC5353B23F78");

            entity.ToTable("DanhMucHangGhe");

            entity.Property(e => e.MaHangGhe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DoiLich).HasMaxLength(100);
            entity.Property(e => e.HanhLyKyGui).HasMaxLength(50);
            entity.Property(e => e.HanhLyXachTay).HasMaxLength(50);
            entity.Property(e => e.HeSoGia).HasDefaultValue(10.0);
            entity.Property(e => e.HoanVe).HasMaxLength(100);
            entity.Property(e => e.MauSac)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TenHangGhe).HasMaxLength(50);

            entity.HasMany(d => d.MaTienIches).WithMany(p => p.MaHangGhes)
                .UsingEntity<Dictionary<string, object>>(
                    "ChiTietTienIch",
                    r => r.HasOne<TienIch>().WithMany()
                        .HasForeignKey("MaTienIch")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ChiTietTi__MaTie__4F7CD00D"),
                    l => l.HasOne<DanhMucHangGhe>().WithMany()
                        .HasForeignKey("MaHangGhe")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ChiTietTi__MaHan__4E88ABD4"),
                    j =>
                    {
                        j.HasKey("MaHangGhe", "MaTienIch").HasName("PK__ChiTietT__231DA1DD337B0005");
                        j.ToTable("ChiTietTienIch");
                        j.IndexerProperty<string>("MaHangGhe")
                            .HasMaxLength(20)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<Ghe>(entity =>
        {
            entity.HasKey(e => e.MaGhe).HasName("PK__Ghe__3CD3C67BB123310D");

            entity.ToTable("Ghe");

            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaHangGhe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiMb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaLoaiMB");
            entity.Property(e => e.PhuThu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoGhe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaHangGheNavigation).WithMany(p => p.Ghes)
                .HasForeignKey(d => d.MaHangGhe)
                .HasConstraintName("FK__Ghe__MaHangGhe__5629CD9C");

            entity.HasOne(d => d.MaLoaiMbNavigation).WithMany(p => p.Ghes)
                .HasForeignKey(d => d.MaLoaiMb)
                .HasConstraintName("FK__Ghe__MaLoaiMB__5535A963");
        });

        modelBuilder.Entity<HangHangKhong>(entity =>
        {
            entity.HasKey(e => e.MaHang).HasName("PK__HangHang__19C0DB1D805FE99E");

            entity.ToTable("HangHangKhong");

            entity.Property(e => e.MaHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.TenHang).HasMaxLength(100);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHoaDon).HasName("PK__HoaDon__835ED13BDAB7E2DE");

            entity.ToTable("HoaDon");

            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TongTien)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThaiThanhToan)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ thanh toán");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaTk)
                .HasConstraintName("FK__HoaDon__MaTK__76969D2E");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1E06D29782");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.MaTk, "UQ__KhachHan__272500713D143A23").IsUnique();

            entity.HasIndex(e => e.Cccd, "UQ__KhachHan__A955A0AA8C2A67E7").IsUnique();

            entity.Property(e => e.MaKh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaKH");
            entity.Property(e => e.AnhDaiDien).HasMaxLength(255);
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.DiemTichLuy).HasDefaultValue(0);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.LoaiThanhVien)
                .HasMaxLength(20)
                .HasDefaultValue("Thường");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.MaTkNavigation).WithOne(p => p.KhachHang)
                .HasForeignKey<KhachHang>(d => d.MaTk)
                .HasConstraintName("FK__KhachHang__MaTK__693CA210");
        });

        modelBuilder.Entity<LoaiMayBay>(entity =>
        {
            entity.HasKey(e => e.MaLoaiMb).HasName("PK__LoaiMayB__12253B3AD1B293DA");

            entity.ToTable("LoaiMayBay");

            entity.Property(e => e.MaLoaiMb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaLoaiMB");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenLoaiMb)
                .HasMaxLength(100)
                .HasColumnName("TenLoaiMB");

            entity.HasOne(d => d.MaHangNavigation).WithMany(p => p.LoaiMayBays)
                .HasForeignKey(d => d.MaHang)
                .HasConstraintName("FK_LoaiMayBay_HangHangKhong");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70ABF0F6906");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.MaTk, "UQ__NhanVien__27250071CAC93006").IsUnique();

            entity.HasIndex(e => e.Cccd, "UQ__NhanVien__A955A0AA2EDBE71B").IsUnique();

            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.AnhDaiDien).HasMaxLength(255);
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.NgayVaoLam).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.MaTkNavigation).WithOne(p => p.NhanVien)
                .HasForeignKey<NhanVien>(d => d.MaTk)
                .HasConstraintName("FK__NhanVien__MaTK__6FE99F9F");
        });

        modelBuilder.Entity<SanBay>(entity =>
        {
            entity.HasKey(e => e.MaSb).HasName("PK__SanBay__2725080E9CDC90B5");

            entity.ToTable("SanBay");

            entity.Property(e => e.MaSb)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("MaSB");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.TenSb)
                .HasMaxLength(100)
                .HasColumnName("TenSB");
            entity.Property(e => e.ThanhPho).HasMaxLength(100);
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.MaTk).HasName("PK__TaiKhoan__2725007078BFC0F9");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC086E3302F").IsUnique();

            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.LoaiTaiKhoan).HasMaxLength(20);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<TienIch>(entity =>
        {
            entity.HasKey(e => e.MaTienIch).HasName("PK__TienIch__4697D8EA7CFF8E28");

            entity.ToTable("TienIch");

            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TenTienIch).HasMaxLength(100);
        });

        modelBuilder.Entity<TuyenBay>(entity =>
        {
            entity.HasKey(e => e.MaTuyen).HasName("PK__TuyenBay__B457602076F9CC64");

            entity.ToTable("TuyenBay");

            entity.Property(e => e.MaTuyen)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaSbden)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("MaSBDen");
            entity.Property(e => e.MaSbdi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("MaSBDi");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaSbdenNavigation).WithMany(p => p.TuyenBayMaSbdenNavigations)
                .HasForeignKey(d => d.MaSbden)
                .HasConstraintName("FK_TuyenBay_SBDen");

            entity.HasOne(d => d.MaSbdiNavigation).WithMany(p => p.TuyenBayMaSbdiNavigations)
                .HasForeignKey(d => d.MaSbdi)
                .HasConstraintName("FK_TuyenBay_SBDi");
        });

        modelBuilder.Entity<Ve>(entity =>
        {
            entity.HasKey(e => e.MaVe).HasName("PK__Ve__2725100FD5508E63");

            entity.ToTable("Ve");

            entity.HasIndex(e => new { e.MaCb, e.MaGhe }, "UQ_GheChuyenBay")
                .IsUnique()
                .HasFilter("[DaXoa] = 0");

            entity.Property(e => e.MaVe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CccdPassport)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CCCD_Passport");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.GiaVeThucTe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoaiHanhKhach).HasMaxLength(20);
            entity.Property(e => e.MaCb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaCB");
            entity.Property(e => e.MaKh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaKH");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenHanhKhach).HasMaxLength(100);

            entity.HasOne(d => d.MaCbNavigation).WithMany(p => p.Ves)
                .HasForeignKey(d => d.MaCb)
                .HasConstraintName("FK__Ve__MaCB__7C4F7684");

            entity.HasOne(d => d.MaGheNavigation).WithMany(p => p.Ves)
                .HasForeignKey(d => d.MaGhe)
                .HasConstraintName("FK__Ve__MaGhe__7D439ABD");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.Ves)
                .HasForeignKey(d => d.MaHoaDon)
                .HasConstraintName("FK__Ve__MaHoaDon__7B5B524B");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Ves)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK__Ve__MaKH__7E37BEF6");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
