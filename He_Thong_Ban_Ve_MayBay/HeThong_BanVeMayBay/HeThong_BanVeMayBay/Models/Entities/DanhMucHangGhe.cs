using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class DanhMucHangGhe
{
    public string MaHangGhe { get; set; } = null!;

    public string TenHangGhe { get; set; } = null!;

    public double? HeSoGia { get; set; }

    public string? HanhLyXachTay { get; set; }

    public string? HanhLyKyGui { get; set; }

    public string? HoanVe { get; set; }

    public string? DoiLich { get; set; }

    public string? MauSac { get; set; }

    public bool? DaXoa { get; set; }

    public virtual ICollection<Ghe> Ghes { get; set; } = new List<Ghe>();

    public virtual ICollection<TienIch> MaTienIches { get; set; } = new List<TienIch>();
}
