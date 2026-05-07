using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class Ghe
{
    public int MaGhe { get; set; }

    public string? MaLoaiMb { get; set; }

    public string SoGhe { get; set; } = null!;

    public string? MaHangGhe { get; set; }

    public int? ViTriHang { get; set; }

    public int? ViTriCot { get; set; }

    public decimal? PhuThu { get; set; }

    public bool? TrangThai { get; set; }

    public bool? DaXoa { get; set; }

    public virtual DanhMucHangGhe? MaHangGheNavigation { get; set; }

    public virtual LoaiMayBay? MaLoaiMbNavigation { get; set; }

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
