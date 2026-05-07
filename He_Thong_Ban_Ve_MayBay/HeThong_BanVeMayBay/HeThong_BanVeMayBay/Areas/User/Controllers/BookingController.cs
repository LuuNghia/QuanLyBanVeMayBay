using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using HeThong_BanVeMayBay.Models.Entities;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HeThong_BanVeMayBay.Areas.User.Controllers
{
    [Area("User")]
    public class BookingController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public BookingController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        public IActionResult TraCuuVe()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TraCuuVeAPI(string maVe, string contact)
        {
            try
            {
                if (string.IsNullOrEmpty(maVe) && string.IsNullOrEmpty(contact))
                {
                    return Json(new { success = false, message = "Vui lòng nhập Mã vé hoặc Thông tin liên hệ." });
                }

                var query = _context.Ves
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaHangNavigation)
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdiNavigation)
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdenNavigation)
                    .Include(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaHangGheNavigation)
                    .Include(v => v.MaHoaDonNavigation)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(maVe))
                {
                    query = query.Where(v => v.MaVe.Contains(maVe));
                }
                if (!string.IsNullOrEmpty(contact))
                {
                    query = query.Where(v => v.SoDienThoai == contact || v.CccdPassport == contact);
                }

                var results = await query.Select(v => new {
                    MaVe = v.MaVe,
                    TenHanhKhach = v.TenHanhKhach,
                    Cccd = v.CccdPassport ?? "N/A",
                    Sdt = v.SoDienThoai ?? "N/A",
                    MaCb = v.MaCb,
                    MaSbDi = v.MaCbNavigation.MaTuyenNavigation.MaSbdi ?? "N/A",
                    TenSbDi = v.MaCbNavigation.MaTuyenNavigation.MaSbdiNavigation.TenSb ?? "N/A",
                    MaSbDen = v.MaCbNavigation.MaTuyenNavigation.MaSbden ?? "N/A",
                    TenSbDen = v.MaCbNavigation.MaTuyenNavigation.MaSbdenNavigation.TenSb ?? "N/A",
                    NgayBay = v.MaCbNavigation.NgayGioBay,
                    TgBay = v.MaCbNavigation.MaTuyenNavigation.ThoiGianBayDuKien ?? 0,
                    SoGhe = v.MaGheNavigation.SoGhe ?? "N/A",
                    HangGhe = v.MaGheNavigation.MaHangGheNavigation.TenHangGhe ?? "N/A",
                    TongTien = v.MaHoaDonNavigation.TongTien ?? 0,
                    TrangThai = v.MaHoaDonNavigation.TrangThaiThanhToan ?? "Chưa rõ",
                    NgayDat = v.MaHoaDonNavigation.NgayLap,
                    XachTay = v.MaGheNavigation.MaHangGheNavigation.HanhLyXachTay ?? "7kg",
                    KyGui = v.MaGheNavigation.MaHangGheNavigation.HanhLyKyGui ?? "0kg"
                }).ToListAsync();

                var data = results.Select(v => new {
                    maVe = v.MaVe,
                    tenHanhKhach = v.TenHanhKhach,
                    cccd = v.Cccd,
                    sdt = v.Sdt,
                    maCb = v.MaCb,
                    maSbDi = v.MaSbDi,
                    tenSbDi = v.TenSbDi,
                    maSbDen = v.MaSbDen,
                    tenSbDen = v.TenSbDen,
                    ngayBay = v.NgayBay.ToString("dd/MM/yyyy HH:mm"),
                    soGhe = v.SoGhe,
                    hangGhe = v.HangGhe,
                    tongTien = v.TongTien,
                    trangThai = v.TrangThai,
                    ngayDat = v.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định",
                    xachTay = v.XachTay,
                    kyGui = v.KyGui
                }).ToList();

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public async Task<IActionResult> ChonCho(string maCb)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrEmpty(maCb)) return RedirectToAction("Index", "Home");

            var chuyenBay = await _context.ChuyenBays
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .Include(c => c.MaLoaiMbNavigation)
                .FirstOrDefaultAsync(c => c.MaCb == maCb);

            if (chuyenBay == null) return RedirectToAction("Index", "Home");

            var danhSachGhe = await _context.Ghes
                .Include(g => g.MaHangGheNavigation).ThenInclude(h => h.MaTienIches)
                .Where(g => g.MaLoaiMb == chuyenBay.MaLoaiMb && g.DaXoa != true)
                .OrderBy(g => g.ViTriHang)
                .ThenBy(g => g.ViTriCot)
                .ToListAsync();

            var gheDaDat = await _context.Ves
                .Where(v => v.MaCb == maCb && v.DaXoa != true)
                .Select(v => v.MaGhe)
                .ToListAsync();

            ViewBag.ChuyenBay = chuyenBay;
            ViewBag.DanhSachGhe = danhSachGhe;
            ViewBag.GheDaDat = gheDaDat;

            return View();
        }

        public async Task<IActionResult> ThongTinKHDatVe(string maCb, string ids, string seats)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrEmpty(maCb) || string.IsNullOrEmpty(ids)) return RedirectToAction("Index", "Home");

            var chuyenBay = await _context.ChuyenBays
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .FirstOrDefaultAsync(c => c.MaCb == maCb);

            var seatIdList = ids.Split(',').Select(id => int.Parse(id)).ToList();
            var danhSachGhe = await _context.Ghes
                .Include(g => g.MaHangGheNavigation).ThenInclude(h => h.MaTienIches)
                .Where(g => seatIdList.Contains(g.MaGhe))
                .ToListAsync();

            ViewBag.ChuyenBay = chuyenBay;
            ViewBag.SelectedSeats = danhSachGhe;
            ViewBag.SeatNos = seats;
            ViewBag.SeatIds = ids;

            // Lấy thông tin người dùng đang đăng nhập để tự động điền
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.MaTk == userId);
                ViewBag.CurrentUser = khachHang;
            }

            return View();
        }
        
        [HttpPost]
        public IActionResult TiepTucThanhToan(BookingRequest request)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện" });

            if (request == null || string.IsNullOrEmpty(request.SeatIds)) 
                return Json(new { success = false, message = "Dữ liệu đặt vé không hợp lệ" });

            // Lưu thông tin vào session để bước sau thanh toán mới lưu DB
            HttpContext.Session.SetString("PendingBooking", JsonSerializer.Serialize(request));
            
            return Json(new { success = true, redirectUrl = "/User/Booking/ThanhToan" });
        }

        [HttpGet]
        public async Task<IActionResult> ThanhToan()
        {
            var json = HttpContext.Session.GetString("PendingBooking");
            if (string.IsNullOrEmpty(json)) return RedirectToAction("Index", "Home");

            var request = JsonSerializer.Deserialize<BookingRequest>(json);
            
            var chuyenBay = await _context.ChuyenBays
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .FirstOrDefaultAsync(c => c.MaCb == request.MaCb);

            ViewBag.BookingRequest = request;
            ViewBag.ChuyenBay = chuyenBay;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> XacNhanThanhToan()
        {
            var json = HttpContext.Session.GetString("PendingBooking");
            if (string.IsNullOrEmpty(json)) return Json(new { success = false, message = "Hết hạn phiên làm việc" });

            var request = JsonSerializer.Deserialize<BookingRequest>(json);

            try
            {
                // 1. Tạo hóa đơn
                var hoaDon = new HoaDon
                {
                    MaTk = HttpContext.Session.GetInt32("UserId"), // Lưu MaTk vào hóa đơn
                    NgayLap = DateTime.Now,
                    TongTien = request.TongTien,
                    TrangThaiThanhToan = "Đã thanh toán",
                    DaXoa = false
                };
                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                // 2. Tạo vé cho từng ghế
                var seatIdList = request.SeatIds.Split(',').Select(int.Parse).ToList();
                var allSeats = await _context.Ghes.Where(s => seatIdList.Contains(s.MaGhe)).ToListAsync();
                
                var random = new Random();
                foreach (var sId in seatIdList)
                {
                    var ve = new Ve
                    {
                        MaVe = "VE" + DateTime.Now.ToString("HHmmss") + random.Next(1000, 9999).ToString(),
                        MaHoaDon = hoaDon.MaHoaDon,
                        MaCb = request.MaCb,
                        MaGhe = sId,
                        MaKh = request.MaKh, // Lưu MaKH
                        TenHanhKhach = request.HoTen,
                        CccdPassport = request.Cccd,
                        SoDienThoai = request.ContactPhone,
                        LoaiHanhKhach = request.LoaiHanhKhach, // Lưu loại hành khách
                        NgaySinh = !string.IsNullOrEmpty(request.NgaySinh) ? DateOnly.ParseExact(request.NgaySinh, "d/M/yyyy") : null, // Lưu ngày sinh
                        GiaVeThucTe = request.GiaTungGhe,
                        DaXoa = false
                    };
                    _context.Ves.Add(ve);

                    var seat = allSeats.FirstOrDefault(s => s.MaGhe == sId);
                    if (seat != null) seat.TrangThai = true; 
                }

                await _context.SaveChangesAsync();
                
                // Xóa session sau khi xong
                HttpContext.Session.Remove("PendingBooking");

                return Json(new { success = true, message = "Thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> XuatHoaDonByMaVe(string maVe)
        {
            var ve = await _context.Ves
                .Include(v => v.MaCbNavigation).ThenInclude(cb => cb.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(v => v.MaCbNavigation).ThenInclude(cb => cb.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .Include(v => v.MaGheNavigation).ThenInclude(g => g.MaHangGheNavigation).ThenInclude(hg => hg.MaTienIches)
                .Include(v => v.MaHoaDonNavigation)
                .FirstOrDefaultAsync(v => v.MaVe == maVe);

            if (ve == null) return NotFound("Không tìm thấy thông tin vé");

            var hoaDon = ve.MaHoaDonNavigation;
            if (hoaDon == null) return NotFound("Vé chưa có thông tin hóa đơn");

            var route = ve.MaCbNavigation?.MaTuyenNavigation;
            var hangGheObj = ve.MaGheNavigation?.MaHangGheNavigation;

            string maSbDi = route?.MaSbdiNavigation?.MaSb ?? "N/A";
            string tenSbDi = route?.MaSbdiNavigation?.TenSb ?? "N/A";
            string maSbDen = route?.MaSbdenNavigation?.MaSb ?? "N/A";
            string tenSbDen = route?.MaSbdenNavigation?.TenSb ?? "N/A";
            
            var tgBay = route?.ThoiGianBayDuKien ?? 0;
            var ngayBayDt = ve.MaCbNavigation?.NgayGioBay ?? DateTime.Now;
            var ngayDenDt = ngayBayDt.AddMinutes(tgBay);

            string thoiGianDi = ngayBayDt.ToString("dd/MM/yyyy HH:mm");
            string thoiGianDen = ngayDenDt.ToString("dd/MM/yyyy HH:mm");
            string hangBay = ve.MaCbNavigation?.MaHang ?? "N/A";
            string soGhe = ve.MaGheNavigation?.SoGhe ?? "Chưa xếp";
            string tenHangGhe = hangGheObj?.TenHangGhe ?? "Phổ thông";
            string hanhLyXachTay = hangGheObj?.HanhLyXachTay ?? "7 kg";
            string hanhLyKyGui = hangGheObj?.HanhLyKyGui ?? "20 kg";
            string tongTien = hoaDon.TongTien?.ToString("N0") + " VND";
            string trangThai = hoaDon.TrangThaiThanhToan ?? "Chờ thanh toán";
            string ngayLap = hoaDon.NgayLap?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
            string pnr = "#" + ve.MaVe;

            var tienIches = hangGheObj?.MaTienIches?.Where(t => t.DaXoa != true).ToList() ?? new List<TienIch>();

            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Item().Background(Color.FromHex("#1a2b5a")).Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("✈ SkyViet Airlines").FontSize(20).Bold().FontColor(Colors.White);
                                c.Item().Text("Hóa đơn điện tử / E-Invoice").FontSize(10).FontColor(Color.FromHex("#a0b8d8"));
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(pnr).FontSize(14).Bold().FontColor(Color.FromHex("#f0c040")).AlignRight();
                                c.Item().PaddingTop(2).Text("Ngày lập: " + ngayLap).FontSize(9).FontColor(Colors.White).AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(5);

                        col.Item().Background(Color.FromHex("#f0f4ff")).Padding(15).Column(flightCol =>
                        {
                            flightCol.Item().Text("THÔNG TIN CHUYẾN BAY").FontSize(10).Bold().FontColor(Color.FromHex("#1a2b5a")).LetterSpacing(1);
                            flightCol.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(maSbDi).FontSize(32).Bold().FontColor(Color.FromHex("#1a2b5a"));
                                    c.Item().Text(tenSbDi).FontSize(10).FontColor(Colors.Grey.Medium);
                                    c.Item().PaddingTop(5).Text("Khởi hành: " + thoiGianDi).FontSize(10).Bold();
                                });
                                row.ConstantItem(80).AlignMiddle().Column(c =>
                                {
                                    c.Item().Text("✈").FontSize(24).AlignCenter().FontColor(Color.FromHex("#1a2b5a"));
                                    c.Item().Text(hangBay).FontSize(10).AlignCenter().FontColor(Colors.Grey.Medium);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(maSbDen).FontSize(32).Bold().FontColor(Color.FromHex("#1a2b5a")).AlignRight();
                                    c.Item().Text(tenSbDen).FontSize(10).FontColor(Colors.Grey.Medium).AlignRight();
                                    c.Item().PaddingTop(5).Text("Hạ cánh: " + thoiGianDen).FontSize(10).Bold().AlignRight();
                                });
                            });
                        });

                        col.Item().PaddingVertical(5);

                        col.Item().Border(1).BorderColor(Color.FromHex("#e2e8f0")).Padding(15).Column(pasCol =>
                        {
                            pasCol.Item().Text("THÔNG TIN HÀNH KHÁCH").FontSize(10).Bold().FontColor(Color.FromHex("#1a2b5a")).LetterSpacing(1);
                            pasCol.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Họ và tên").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(ve.TenHanhKhach ?? "N/A").FontSize(13).Bold();
                                    c.Item().PaddingTop(8).Text("CCCD / Passport").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(ve.CccdPassport ?? "N/A").FontSize(11).Bold();
                                    c.Item().PaddingTop(8).Text("Số điện thoại").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(ve.SoDienThoai ?? "N/A").FontSize(11).Bold();
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Hạng ghế").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(tenHangGhe.ToUpper()).FontSize(11).Bold().FontColor(Color.FromHex("#d97706"));
                                    c.Item().PaddingTop(8).Text("Số ghế").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(soGhe).FontSize(24).Bold().FontColor(Color.FromHex("#1a2b5a"));
                                });
                            });
                        });

                        col.Item().PaddingVertical(5);

                        col.Item().Background(Color.FromHex("#f8fafc")).Border(1).BorderColor(Color.FromHex("#e2e8f0")).Padding(15).Column(bagCol =>
                        {
                            bagCol.Item().Text("HÀNH LÝ & QUYỀN LỢI").FontSize(10).Bold().FontColor(Color.FromHex("#1a2b5a")).LetterSpacing(1);
                            bagCol.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Text("✓ Hành lý xách tay: " + hanhLyXachTay).FontSize(11);
                                row.RelativeItem().Text("✓ Hành lý ký gửi: " + hanhLyKyGui).FontSize(11);
                            });
                            if (tienIches.Count > 0)
                            {
                                bagCol.Item().PaddingTop(8).LineHorizontal(1).LineColor(Color.FromHex("#e2e8f0"));
                                bagCol.Item().PaddingTop(8).Text("TIỆN ICH KÈM THEO").FontSize(9).Bold().FontColor(Colors.Grey.Medium).LetterSpacing(1);
                                int half = (int)Math.Ceiling(tienIches.Count / 2.0);
                                bagCol.Item().PaddingTop(6).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        for (int i = 0; i < half && i < tienIches.Count; i++)
                                            c.Item().Text("✓ " + tienIches[i].TenTienIch).FontSize(10);
                                    });
                                    row.RelativeItem().Column(c =>
                                    {
                                        for (int i = half; i < tienIches.Count; i++)
                                            c.Item().Text("✓ " + tienIches[i].TenTienIch).FontSize(10);
                                    });
                                });
                            }
                        });

                        col.Item().PaddingVertical(5);

                        // --- TỔNG THANH TOÁN ---
                        col.Item().Background(Color.FromHex("#1a2b5a")).Padding(12).Row(row =>
                        {
                            row.RelativeItem(7).Column(c =>
                            {
                                c.Item().Text("TỔNG TIỀN THANH TOÁN").FontSize(9).FontColor(Color.FromHex("#a0b8d8"));
                                c.Item().Text(tongTien).FontSize(18).Bold().FontColor(Colors.White);
                            });

                            row.RelativeItem(3).AlignRight().Column(c =>
                            {
                                c.Item().Text("TRẠNG THÁI").FontSize(8).FontColor(Color.FromHex("#a0b8d8")).AlignRight();
                                var statusColor = (trangThai.Contains("thanh toán") || trangThai.Contains("Thành công")) ? Color.FromHex("#34d399") 
                                                : (trangThai.Contains("hủy") || trangThai.Contains("Hủy")) ? Color.FromHex("#f87171") 
                                                : Color.FromHex("#fbbf24");
                                c.Item().PaddingTop(2).AlignRight()
                                    .Border(0.5f).BorderColor(statusColor).Background(statusColor.WithAlpha(30))
                                    .PaddingHorizontal(6).PaddingVertical(3)
                                    .Text(trangThai.ToUpper()).FontSize(7).Bold().FontColor(statusColor);
                            });
                        });

                        col.Item().PaddingVertical(10);
                        col.Item().AlignCenter().Text("Cảm ơn Quý khách đã sử dụng dịch vụ SkyViet Airlines!").FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"HoaDon_{ve.MaVe}.pdf");
        }

        public IActionResult LichSuDatVe()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLichSuDatVeAPI(string pnr, string status, string date)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để xem lịch sử." });
                }

                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.MaTk == userId);
                string cccd = khachHang?.Cccd;
                string sdt = khachHang?.SoDienThoai;

                var query = _context.Ves
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaHangNavigation)
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdiNavigation)
                    .Include(v => v.MaCbNavigation)
                        .ThenInclude(c => c.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdenNavigation)
                    .Include(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaHangGheNavigation)
                    .Include(v => v.MaHoaDonNavigation)
                    .AsQueryable();

                query = query.Where(v => 
                    v.MaHoaDonNavigation.MaTk == userId || 
                    (v.CccdPassport == cccd && !string.IsNullOrEmpty(cccd)) || 
                    (v.SoDienThoai == sdt && !string.IsNullOrEmpty(sdt))
                );

                // Áp dụng các bộ lọc từ giao diện
                if (!string.IsNullOrEmpty(pnr))
                {
                    query = query.Where(v => v.MaVe.Contains(pnr));
                }
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(v => v.MaHoaDonNavigation.TrangThaiThanhToan == status);
                }
                if (!string.IsNullOrEmpty(date))
                {
                    if (DateTime.TryParse(date, out DateTime filterDate))
                    {
                        // Lọc theo ngày lập hóa đơn (ngày đặt vé)
                        query = query.Where(v => v.MaHoaDonNavigation.NgayLap.HasValue && 
                                               v.MaHoaDonNavigation.NgayLap.Value.Date == filterDate.Date);
                    }
                }

                var results = await query.Select(v => new {
                    MaVe = v.MaVe,
                    TenHanhKhach = v.TenHanhKhach,
                    Cccd = v.CccdPassport ?? "N/A",
                    Sdt = v.SoDienThoai ?? "N/A",
                    MaCb = v.MaCb,
                    HangBay = v.MaCbNavigation.MaHangNavigation.TenHang ?? "N/A",
                    MaSbDi = v.MaCbNavigation.MaTuyenNavigation.MaSbdi ?? "N/A",
                    TenSbDi = v.MaCbNavigation.MaTuyenNavigation.MaSbdiNavigation.TenSb ?? "N/A",
                    MaSbDen = v.MaCbNavigation.MaTuyenNavigation.MaSbden ?? "N/A",
                    TenSbDen = v.MaCbNavigation.MaTuyenNavigation.MaSbdenNavigation.TenSb ?? "N/A",
                    NgayBay = v.MaCbNavigation.NgayGioBay,
                    TgBay = v.MaCbNavigation.MaTuyenNavigation.ThoiGianBayDuKien ?? 0,
                    HangGhe = v.MaGheNavigation.MaHangGheNavigation.TenHangGhe ?? "N/A",
                    SoGhe = v.MaGheNavigation.SoGhe ?? "N/A",
                    TongTien = v.MaHoaDonNavigation.TongTien ?? 0,
                    TrangThai = (v.DaXoa == true) ? "Đã hủy" : (v.MaHoaDonNavigation.TrangThaiThanhToan ?? "Chờ thanh toán"),
                    NgayDat = v.MaHoaDonNavigation.NgayLap,
                    XachTay = v.MaGheNavigation.MaHangGheNavigation.HanhLyXachTay ?? "7kg",
                    KyGui = v.MaGheNavigation.MaHangGheNavigation.HanhLyKyGui ?? "0kg",
                    TienIches = v.MaGheNavigation.MaHangGheNavigation.MaTienIches.Select(t => t.TenTienIch).ToList()
                }).OrderByDescending(v => v.NgayDat).ToListAsync();

                var data = results.Select(v => {
                    var arrivalDate = v.NgayBay.AddMinutes(v.TgBay);
                    return new {
                        maVe = v.MaVe,
                        tenHanhKhach = v.TenHanhKhach,
                        cccd = v.Cccd,
                        sdt = v.Sdt,
                        maCb = v.MaCb,
                        hangBay = v.HangBay,
                        maSbDi = v.MaSbDi,
                        tenSbDi = v.TenSbDi,
                        maSbDen = v.MaSbDen,
                        tenSbDen = v.TenSbDen,
                        ngayBay = v.NgayBay.ToString("dd/MM/yyyy"),
                        gioBay = v.NgayBay.ToString("HH:mm"),
                        ngayDen = arrivalDate.ToString("dd/MM/yyyy"),
                        gioDen = arrivalDate.ToString("HH:mm"),
                        thoiGianBay = (v.TgBay / 60) + "h " + (v.TgBay % 60) + "m",
                        hangGhe = v.HangGhe,
                        soGhe = v.SoGhe,
                        tongTien = v.TongTien,
                        trangThai = v.TrangThai,
                        NgayDat = v.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định",
                        xachTay = v.XachTay,
                        kyGui = v.KyGui,
                        tienIches = v.TienIches
                    };
                }).ToList();

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> HuyVeAPI(string maVe)
        {
            try
            {
                var ve = await _context.Ves
                    .Include(v => v.MaHoaDonNavigation)
                    .Include(v => v.MaGheNavigation)
                    .FirstOrDefaultAsync(v => v.MaVe == maVe);

                if (ve == null) return Json(new { success = false, message = "Không tìm thấy thông tin vé." });
                if (ve.DaXoa == true) return Json(new { success = false, message = "Vé này đã được hủy trước đó." });

                // 1. Đánh dấu vé đã hủy (Dùng DaXoa làm trạng thái Hủy)
                ve.DaXoa = true;

                // 2. Giải phóng ghế
                if (ve.MaGheNavigation != null)
                {
                    ve.MaGheNavigation.TrangThai = true;
                }

                // 3. Kiểm tra xem tất cả các vé trong cùng hóa đơn đã bị hủy chưa
                if (ve.MaHoaDonNavigation != null)
                {
                    var otherTickets = await _context.Ves
                        .Where(v => v.MaHoaDon == ve.MaHoaDon && v.MaVe != maVe && v.DaXoa != true)
                        .CountAsync();

                    if (otherTickets == 0)
                    {
                        ve.MaHoaDonNavigation.TrangThaiThanhToan = "Đã hủy";
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Hủy vé thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy vé: " + ex.Message });
            }
        }

        public class BookingRequest
        {
            public string MaCb { get; set; }
            public string SeatIds { get; set; }
            public string MaKh { get; set; }
            public string HoTen { get; set; }
            public string Cccd { get; set; }
            public string ContactPhone { get; set; }
            public string NgaySinh { get; set; }
            public string LoaiHanhKhach { get; set; }
            public decimal TongTien { get; set; }
            public decimal GiaTungGhe { get; set; }
        }
    }
}
