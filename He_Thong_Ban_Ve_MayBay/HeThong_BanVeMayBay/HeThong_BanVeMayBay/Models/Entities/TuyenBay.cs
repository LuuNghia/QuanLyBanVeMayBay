using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class TuyenBay
{
    public string MaTuyen { get; set; } = null!;

    public string? MaSbdi { get; set; }

    public string? MaSbden { get; set; }

    public double? KhoangCach { get; set; }

    public int? ThoiGianBayDuKien { get; set; }

    public bool? TrangThai { get; set; }

    public bool? DaXoa { get; set; }

    public virtual ICollection<ChuyenBay> ChuyenBays { get; set; } = new List<ChuyenBay>();

    public virtual SanBay? MaSbdenNavigation { get; set; }

    public virtual SanBay? MaSbdiNavigation { get; set; }
}
