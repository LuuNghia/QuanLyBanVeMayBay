using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class NhanVien
{
    public string MaNv { get; set; } = null!;

    public int? MaTk { get; set; }

    public string HoTen { get; set; } = null!;

    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string Cccd { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string ChucVu { get; set; } = null!;

    public DateOnly? NgayVaoLam { get; set; }

    public string? AnhDaiDien { get; set; }

    public string? Quyen { get; set; }
    
    public bool? DaXoa { get; set; }

    public virtual TaiKhoan? MaTkNavigation { get; set; }
}
