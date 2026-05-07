using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class LoaiMayBay
{
    public string MaLoaiMb { get; set; } = null!;

    public string? TenLoaiMb { get; set; }

    public string? MaHang { get; set; }

    public int SoLuongGhe { get; set; }

    public bool? DaXoa { get; set; }

    public virtual ICollection<ChuyenBay> ChuyenBays { get; set; } = new List<ChuyenBay>();

    public virtual ICollection<Ghe> Ghes { get; set; } = new List<Ghe>();

    public virtual HangHangKhong? MaHangNavigation { get; set; }
}
