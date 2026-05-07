using System;
using System.Collections.Generic;

namespace HeThong_BanVeMayBay.Models.Entities;

public partial class ChuyenBay
{
    public string MaCb { get; set; } = null!;

    public string? MaTuyen { get; set; }

    public string? MaLoaiMb { get; set; }

    public string? MaHang { get; set; }

    public DateTime NgayGioBay { get; set; }

    public decimal GiaVeCoBan { get; set; }

    public string? TrangThai { get; set; }

    public bool? DaXoa { get; set; }

    public virtual HangHangKhong? MaHangNavigation { get; set; }

    public virtual LoaiMayBay? MaLoaiMbNavigation { get; set; }

    public virtual TuyenBay? MaTuyenNavigation { get; set; }

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
