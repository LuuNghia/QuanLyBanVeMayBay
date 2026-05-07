using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class TienIch
{
    public int MaTienIch { get; set; }

    public string TenTienIch { get; set; } = null!;

    public string? Icon { get; set; }

    public bool? DaXoa { get; set; }

    public virtual ICollection<DanhMucHangGhe> MaHangGhes { get; set; } = new List<DanhMucHangGhe>();
}
