using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using HeThong_BanVeMayBay.Models.EF;
using HeThong_BanVeMayBay.Models.Entities;
using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using HeThong_BanVeMayBay.Attributes;

namespace HeThong_BanVeMayBay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize("BOOKING_MGMT")]
    public class BookingController : Controller
    {
        private readonly QlBanVeMayBayContext _context;
        
        public BookingController(QlBanVeMayBayContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<IActionResult> QuanLyDatVe(string filterStatus = "", string filterMonth = "", string filterDate = "", string filterMaCb = "", int pageHhk = 1)
        {
            int pageSize = 5;
            var query = _context.HoaDons
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaCbNavigation)
                        .ThenInclude(cb => cb.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdiNavigation)
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaCbNavigation)
                        .ThenInclude(cb => cb.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdenNavigation)
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaHangGheNavigation)
                .Where(hd => (hd.DaXoa == false || hd.DaXoa == null) && hd.Ves.Any())
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "Tất cả")
            {
                query = query.Where(hd => hd.TrangThaiThanhToan == filterStatus);
            }

            if (!string.IsNullOrEmpty(filterDate))
            {
                if (DateTime.TryParse(filterDate, out DateTime parsedDate))
                {
                    query = query.Where(hd => hd.NgayLap.HasValue && hd.NgayLap.Value.Date == parsedDate.Date);
                }
            }
            else if (!string.IsNullOrEmpty(filterMonth))
            {
                if (DateTime.TryParse(filterMonth + "-01", out DateTime parsedMonth))
                {
                    query = query.Where(hd => hd.NgayLap.HasValue && hd.NgayLap.Value.Month == parsedMonth.Month && hd.NgayLap.Value.Year == parsedMonth.Year);
                }
            }

            if (!string.IsNullOrEmpty(filterMaCb))
            {
                query = query.Where(hd => hd.Ves.Any(v => v.MaCb == filterMaCb));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var danhSachDatVe = await query
                .OrderByDescending(hd => hd.NgayLap)
                .Skip((pageHhk - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var currentDate = DateTime.Now;
            var currentMonthRevenue = await _context.HoaDons
                .Where(hd => hd.NgayLap.HasValue && hd.NgayLap.Value.Month == currentDate.Month && hd.NgayLap.Value.Year == currentDate.Year &&
                            (hd.TrangThaiThanhToan == "Đã thanh toán" || hd.TrangThaiThanhToan == "Thành công"))
                .SumAsync(hd => hd.TongTien ?? 0);

            var tongSoDatVe = totalItems;

            // Danh sách chuyến bay để lọc (lấy thêm tên hãng)
            ViewBag.ChuyenBays = await _context.ChuyenBays
                .Include(cb => cb.MaHangNavigation)
                .Include(cb => cb.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdiNavigation)
                .Include(cb => cb.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdenNavigation)
                .Where(cb => cb.NgayGioBay >= DateTime.Now.AddMonths(-3))
                .OrderByDescending(cb => cb.NgayGioBay)
                .ToListAsync();

            ViewBag.DoanhThuThangNay = currentMonthRevenue;
            ViewBag.TongSoDatVe = tongSoDatVe;
            ViewBag.TotalItems = totalItems;

            ViewBag.CurrentPageHhk = pageHhk;
            ViewBag.TotalPagesHhk = totalPages;
            ViewBag.FilterStatus = filterStatus;
            ViewBag.FilterMonth = filterMonth;
            ViewBag.FilterDate = filterDate;
            ViewBag.FilterMaCb = filterMaCb;

            return View(danhSachDatVe);
        }

        // ====================================================
        // API: Lấy danh sách hạng ghế từ DB (tất cả)
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> GetHangGhe()
        {
            var hangGhes = await _context.DanhMucHangGhes
                .Where(hg => hg.DaXoa != true)
                .OrderBy(hg => hg.MaHangGhe)
                .Select(hg => new {
                    hg.MaHangGhe,
                    hg.TenHangGhe,
                    hg.HeSoGia,
                    hg.MauSac
                })
                .ToListAsync();
            return Json(hangGhes);
        }

        // ====================================================
        // API: Lấy hạng ghế CÓ TRONG chuyến bay (theo loại máy bay)
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> GetHangGheByChuyenBay(string maCb)
        {
            if (string.IsNullOrEmpty(maCb))
                return Json(new List<object>());

            // Bước 1: Lấy loại máy bay của chuyến này
            var chuyenBay = await _context.ChuyenBays
                .FirstOrDefaultAsync(cb => cb.MaCb == maCb);

            if (chuyenBay == null || string.IsNullOrEmpty(chuyenBay.MaLoaiMb))
                return Json(new List<object>());

            // Bước 2: Lấy danh sách MaHangGhe thực sự có trong máy bay này (ra List trước)
            var maHangGheList = await _context.Ghes
                .Where(g => g.MaLoaiMb == chuyenBay.MaLoaiMb
                         && g.MaHangGhe != null)
                .Select(g => g.MaHangGhe!)
                .Distinct()
                .ToListAsync();

            IQueryable<DanhMucHangGhe> query = _context.DanhMucHangGhes.Where(hg => hg.DaXoa != true);
            
            // Nếu tìm thấy ghế theo máy bay thì lọc, nếu không thì lấy tất cả (fallback)
            if (maHangGheList.Any())
            {
                query = query.Where(hg => maHangGheList.Contains(hg.MaHangGhe));
            }

            // Bước 3: Lấy thông tin hạng ghế
            var hangGhes = await query
                .OrderBy(hg => hg.MaHangGhe)
                .Select(hg => new {
                    hg.MaHangGhe,
                    hg.TenHangGhe,
                    hg.HeSoGia,
                    hg.MauSac
                })
                .ToListAsync();

            return Json(hangGhes);
        }


        // ====================================================
        // API: Lấy sơ đồ ghế theo chuyến bay + hạng ghế
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> GetGheByHangVaChuyen(string maCb, string maHangGhe)
        {
            if (string.IsNullOrEmpty(maCb)) return Json(new { success = false, message = "Thiếu mã chuyến bay" });

            // Lấy loại máy bay của chuyến bay
            var chuyenBay = await _context.ChuyenBays
                .FirstOrDefaultAsync(cb => cb.MaCb == maCb);

            if (chuyenBay == null) return Json(new { success = false, message = "Không tìm thấy chuyến bay" });

            // Lấy danh sách ghế theo loại máy bay + hạng ghế
            var gheQuery = _context.Ghes
                .Include(g => g.MaHangGheNavigation)
                .Where(g => g.MaLoaiMb == chuyenBay.MaLoaiMb && g.DaXoa != true);

            if (!string.IsNullOrEmpty(maHangGhe))
                gheQuery = gheQuery.Where(g => g.MaHangGhe == maHangGhe);

            var tatCaGhe = await gheQuery
                .OrderBy(g => g.ViTriHang).ThenBy(g => g.ViTriCot)
                .ToListAsync();

            // Lấy danh sách ghế đã đặt trên chuyến bay này
            var gheDaDat = await _context.Ves
                .Where(v => v.MaCb == maCb && v.DaXoa != true && v.MaGhe.HasValue)
                .Select(v => v.MaGhe.Value)
                .ToListAsync();

            // Lấy giá vé chuyến bay + hệ số hạng ghế
            decimal giaVeCoBan = chuyenBay.GiaVeCoBan;
            double heSoGia = 1.0;
            if (!string.IsNullOrEmpty(maHangGhe))
            {
                var hangGhe = await _context.DanhMucHangGhes.FindAsync(maHangGhe);
                if (hangGhe?.HeSoGia != null) heSoGia = hangGhe.HeSoGia.Value;
            }

            var result = tatCaGhe.Select(g => new {
                g.MaGhe,
                g.SoGhe,
                g.ViTriHang,
                g.ViTriCot,
                g.MaHangGhe,
                TenHangGhe = g.MaHangGheNavigation?.TenHangGhe ?? "",
                DaDat = gheDaDat.Contains(g.MaGhe),
                GiaVe = (long)(giaVeCoBan * (decimal)heSoGia)
            });

            return Json(new {
                success = true,
                ghes = result,
                giaVe = (long)(giaVeCoBan * (decimal)heSoGia)
            });
        }

        // ====================================================
        // API: Xem chi tiết vé
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> ChiTietVe(int id)
        {
            var hoaDon = await _context.HoaDons
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaCbNavigation)
                        .ThenInclude(cb => cb.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdiNavigation)
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaCbNavigation)
                        .ThenInclude(cb => cb.MaTuyenNavigation)
                            .ThenInclude(t => t.MaSbdenNavigation)
                .Include(hd => hd.Ves)
                    .ThenInclude(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaHangGheNavigation)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == id);

            if (hoaDon == null) return Json(new { success = false, message = "Không tìm thấy hóa đơn" });

            var mainTicket = hoaDon.Ves.FirstOrDefault();
            if (mainTicket == null) return Json(new { success = false, message = "Hóa đơn chưa có vé cụ thể" });

            var route = mainTicket.MaCbNavigation?.MaTuyenNavigation;
            var hangGheObj = mainTicket.MaGheNavigation?.MaHangGheNavigation;

            var tgBay = route?.ThoiGianBayDuKien ?? 0;
            var ngayDenDt = mainTicket.MaCbNavigation?.NgayGioBay.AddMinutes(tgBay) ?? DateTime.Now;

            var result = new {
                success = true,
                maVe = mainTicket.MaVe,
                maHoaDon = hoaDon.MaHoaDon.ToString("D4"),
                maHoaDonGoc = hoaDon.MaHoaDon,
                tenHanhKhach = mainTicket.TenHanhKhach,
                cccd = mainTicket.CccdPassport,
                sdt = mainTicket.SoDienThoai,
                hangGhe = hangGheObj?.TenHangGhe ?? "PHỔ THÔNG",
                maHangGhe = mainTicket.MaGheNavigation?.MaHangGhe ?? "",
                soGhe = mainTicket.MaGheNavigation?.SoGhe ?? "Chưa xếp",
                sbDi = route?.MaSbdiNavigation?.TenSb ?? "N/A",
                maSbDi = route?.MaSbdiNavigation?.MaSb ?? "N/A",
                sbDen = route?.MaSbdenNavigation?.TenSb ?? "N/A",
                maSbDen = route?.MaSbdenNavigation?.MaSb ?? "N/A",
                thoiGianDi = mainTicket.MaCbNavigation?.NgayGioBay.ToString("dd/MM/yyyy - HH:mm") ?? "N/A",
                thoiGianDen = ngayDenDt.ToString("dd/MM/yyyy - HH:mm"),
                hangBay = mainTicket.MaCbNavigation?.MaHang ?? "N/A",
                tongTien = hoaDon.TongTien?.ToString("N0") + " đ",
                ngayDat = hoaDon.NgayLap?.ToString("dd/MM/yyyy") ?? "N/A",
                trangThai = hoaDon.TrangThaiThanhToan ?? "Chờ thanh toán",
                hanhLyXachTay = hangGheObj?.HanhLyXachTay ?? "7 kg",
                hanhLyKyGui = hangGheObj?.HanhLyKyGui ?? "20 kg"
            };

            return Json(result);
        }

        public class IdInput { 
            public int Id { get; set; } 
            public string PhuongThuc { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> XacNhanThanhToan([FromBody] IdInput model)
        {
            if (model == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            var hoaDon = await _context.HoaDons.FindAsync(model.Id);
            if (hoaDon == null) return Json(new { success = false, message = "Không tìm thấy mã hóa đơn!" });

            hoaDon.TrangThaiThanhToan = "Đã thanh toán";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xác nhận thu tiền thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> HuyDatVe([FromBody] IdInput model)
        {
            if (model == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            var hoaDon = await _context.HoaDons.FindAsync(model.Id);
            if (hoaDon == null) return Json(new { success = false, message = "Không tìm thấy mã hóa đơn!" });

            hoaDon.TrangThaiThanhToan = "Đã hủy";

            // Giải phóng tất cả các vé và ghế liên quan
            var relatedTickets = await _context.Ves
                .Include(v => v.MaGheNavigation)
                .Where(v => v.MaHoaDon == hoaDon.MaHoaDon)
                .ToListAsync();

            foreach (var v in relatedTickets)
            {
                v.DaXoa = true;
                if (v.MaGheNavigation != null)
                {
                    v.MaGheNavigation.TrangThai = true;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã hủy hóa đơn đặt vé!" });
        }

        [HttpPost]
        public async Task<IActionResult> XoaDatVe([FromBody] IdInput model)
        {
            if (model == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            var hoaDon = await _context.HoaDons.FindAsync(model.Id);
            if (hoaDon == null) return Json(new { success = false, message = "Không tìm thấy mã hóa đơn!" });

            // Xóa cứng: Giải phóng ghế trước, sau đó xóa vé và hóa đơn
            var relatedTickets = await _context.Ves
                .Include(v => v.MaGheNavigation)
                .Where(v => v.MaHoaDon == hoaDon.MaHoaDon)
                .ToListAsync();

            foreach (var v in relatedTickets)
            {
                if (v.MaGheNavigation != null)
                {
                    v.MaGheNavigation.TrangThai = true;
                }
                _context.Ves.Remove(v);
            }

            _context.HoaDons.Remove(hoaDon);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa vĩnh viễn đơn đặt vé và giải phóng ghế!" });
        }

      

        [HttpPost]
        public async Task<IActionResult> TaoDatVe([FromBody] BookingInput model)
        {
            if (model == null || model.MaGhes == null || !model.MaGhes.Any())
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một chỗ ngồi!" });

            try
            {
                // Lấy thông tin chuyến bay
                var chuyenBay = await _context.ChuyenBays.FindAsync(model.MaCb);
                if (chuyenBay == null) return Json(new { success = false, message = "Không tìm thấy chuyến bay!" });

                decimal tongTienHoaDon = 0;
                var danhSachVe = new List<Ve>();

                var random = new Random();
                foreach (var maGhe in model.MaGhes)
                {
                    decimal giaVeThucTe = chuyenBay.GiaVeCoBan;
                    
                    var ghe = await _context.Ghes
                        .Include(g => g.MaHangGheNavigation)
                        .FirstOrDefaultAsync(g => g.MaGhe == maGhe);

                    if (ghe?.MaHangGheNavigation?.HeSoGia != null)
                        giaVeThucTe = giaVeThucTe * (decimal)ghe.MaHangGheNavigation.HeSoGia.Value;

                    tongTienHoaDon += giaVeThucTe;

                    danhSachVe.Add(new Ve
                    {
                        MaVe = "VE" + DateTime.Now.ToString("HHmmss") + random.Next(1000, 9999).ToString(),
                        MaCb = model.MaCb,
                        MaGhe = maGhe,
                        TenHanhKhach = model.TenHanhKhach,
                        CccdPassport = model.Cccd,
                        SoDienThoai = model.Sdt,
                        GiaVeThucTe = giaVeThucTe,
                        DaXoa = false
                    });

                    if (ghe != null) ghe.TrangThai = true;
                }

                // Tạo hóa đơn tổng
                var hoaDon = new HoaDon
                {
                    NgayLap = DateTime.Now,
                    TrangThaiThanhToan = model.TrangThaiThanhToan,
                    TongTien = tongTienHoaDon,
                    DaXoa = false
                };
                
                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                // Gán MaHoaDon cho các vé và lưu
                foreach (var ve in danhSachVe)
                {
                    ve.MaHoaDon = hoaDon.MaHoaDon;
                    _context.Ves.Add(ve);
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã đặt thành công {model.MaGhes.Count} vé!", maHoaDon = hoaDon.MaHoaDon, maVe = danhSachVe.FirstOrDefault()?.MaVe });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ====================================================
        // API: Xuất hóa đơn PDF bằng QuestPDF
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> XuatHoaDonPdf(string maVe)
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
                        // --- HEADER ---
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

                        // --- THÔNG TIN CHUYẾN BAY ---
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

                        // --- THÔNG TIN HÀNH KHÁCH ---
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

                        // --- HÀNH LÝ & TIỆN ÍCH ---
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
          public class BookingInput
        {
            public string MaCb { get; set; }
            public string TenHanhKhach { get; set; }
            public string Cccd { get; set; }
            public string Sdt { get; set; }
            public string Email { get; set; }
            public List<int> MaGhes { get; set; }
            public string TrangThaiThanhToan { get; set; }
            public string PhuongThucThanhToan { get; set; }
        }
    }
}
