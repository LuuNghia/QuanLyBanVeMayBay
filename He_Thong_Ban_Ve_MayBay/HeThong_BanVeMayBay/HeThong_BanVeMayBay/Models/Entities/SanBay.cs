using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class SanBay
{
    public string MaSb { get; set; } = null!;

    public string TenSb { get; set; } = null!;

    public string ThanhPho { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<TuyenBay> TuyenBayMaSbdenNavigations { get; set; } = new List<TuyenBay>();

    public virtual ICollection<TuyenBay> TuyenBayMaSbdiNavigations { get; set; } = new List<TuyenBay>();
}
