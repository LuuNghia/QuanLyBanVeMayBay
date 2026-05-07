using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class HangHangKhong
{
    public string MaHang { get; set; } = null!;

    public string TenHang { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<ChuyenBay> ChuyenBays { get; set; } = new List<ChuyenBay>();

    public virtual ICollection<LoaiMayBay> LoaiMayBays { get; set; } = new List<LoaiMayBay>();
}
