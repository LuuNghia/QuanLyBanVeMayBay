using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeThong_BanVeMayBay.Models.EF;
using HeThong_BanVeMayBay.Models.Entities;
using HeThong_BanVeMayBay.Attributes;

namespace HeThong_BanVeMayBay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class FlightController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public FlightController(QlBanVeMayBayContext context)
        {
            _context = context;
        }


        [HttpGet]
        [AdminAuthorize("AIRCRAFT_MGMT")]
        public async Task<IActionResult> QuanLyMayBay(string searchHhk = "", string searchMb = "", int pageHhk = 1, int pageMb = 1)
        {
            int pageSize = 5;

            var queryHhk = _context.HangHangKhongs.Where(h => h.DaXoa == false || h.DaXoa == null).AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchHhk))
            {
                searchHhk = searchHhk.Trim().ToLower();
                queryHhk = queryHhk.Where(h => h.MaHang.ToLower().Contains(searchHhk) || h.TenHang.ToLower().Contains(searchHhk));
                ViewBag.SearchHhk = searchHhk;
            }
            int totalHhk = await queryHhk.CountAsync();
            int totalPagesHhk = (int)System.Math.Ceiling((double)totalHhk / pageSize);
            if (totalPagesHhk == 0) totalPagesHhk = 1;
            if (pageHhk < 1) pageHhk = 1;
            if (pageHhk > totalPagesHhk) pageHhk = totalPagesHhk;
            var listHhk = await queryHhk.OrderBy(h => h.MaHang).Skip((pageHhk - 1) * pageSize).Take(pageSize).ToListAsync();

            var queryMb = _context.LoaiMayBays.Where(m => m.DaXoa == false || m.DaXoa == null).AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchMb))
            {
                searchMb = searchMb.Trim().ToLower();
                queryMb = queryMb.Where(m => m.MaLoaiMb.ToLower().Contains(searchMb)
                                          || m.TenLoaiMb.ToLower().Contains(searchMb)
                                          || m.MaHang.ToLower().Contains(searchMb));
                ViewBag.SearchMb = searchMb;
            }
            int totalMb = await queryMb.CountAsync();
            int totalPagesMb = (int)System.Math.Ceiling((double)totalMb / pageSize);
            if (totalPagesMb == 0) totalPagesMb = 1;
            if (pageMb < 1) pageMb = 1;
            if (pageMb > totalPagesMb) pageMb = totalPagesMb;
            var listMb = await queryMb.Include(m => m.MaHangNavigation).OrderBy(m => m.MaLoaiMb).Skip((pageMb - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalHhk = totalHhk; ViewBag.CurrentPageHhk = pageHhk; ViewBag.TotalPagesHhk = totalPagesHhk;
            ViewBag.TotalMb = totalMb; ViewBag.CurrentPageMb = pageMb; ViewBag.TotalPagesMb = totalPagesMb;

            ViewBag.HangHangKhongs = listHhk;
            ViewBag.LoaiMayBays = listMb;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ThemHangHangKhong([FromBody] HangHangKhong hhk)
        {
            if (hhk == null || string.IsNullOrWhiteSpace(hhk.MaHang)) return BadRequest("Dữ liệu không hợp lệ.");
            if (await _context.HangHangKhongs.AnyAsync(h => h.MaHang == hhk.MaHang)) return BadRequest("Mã Hãng đã tồn tại.");

            hhk.DaXoa = false;
            _context.HangHangKhongs.Add(hhk);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-hhk");
            }

            return Ok(new { success = true, message = "Thêm Hãng hàng không thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> SuaHangHangKhong([FromBody] HangHangKhong hhk) 
        {
            if (string.IsNullOrWhiteSpace(hhk.MaHang)) return BadRequest("Dữ liệu không hợp lệ.");
            var existing = await _context.HangHangKhongs.FirstOrDefaultAsync(h => h.MaHang == hhk.MaHang);
            if (existing == null) return NotFound("Không tìm thấy Hãng hàng không.");

            existing.TenHang = hhk.TenHang;
            _context.HangHangKhongs.Update(existing);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-hhk");
            }

            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        [HttpDelete]
        public async Task<IActionResult> XoaHangHangKhong(string id)
        {
            var existing = await _context.HangHangKhongs.FirstOrDefaultAsync(h => h.MaHang == id);
            if (existing == null) return NotFound("Không tìm thấy Hãng hàng không.");

            var isUsed = await _context.ChuyenBays.AnyAsync(c => c.MaHang == id);
            if (isUsed)
            {
                existing.DaXoa = true;
                _context.HangHangKhongs.Update(existing);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-hhk");
                }

                return Ok(new { success = true, message = "Xóa mềm thành công do đã có chuyến bay thuộc hãng này." });
            }
            _context.HangHangKhongs.Remove(existing);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-hhk");
            }

            return Ok(new { success = true, message = "Xóa thành công!" });
        }

 

        [HttpPost]
        public async Task<IActionResult> ThemMayBay([FromBody] LoaiMayBay mb)
        {
            if (mb == null || string.IsNullOrWhiteSpace(mb.MaLoaiMb)) return BadRequest("Dữ liệu không hợp lệ.");
            if (await _context.LoaiMayBays.AnyAsync(m => m.MaLoaiMb == mb.MaLoaiMb)) return BadRequest("Mã Máy bay đã tồn tại.");

            mb.DaXoa = false;
            _context.LoaiMayBays.Add(mb);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-mb");
            }

            return Ok(new { success = true, message = "Thêm Loại máy bay thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> SuaMayBay([FromBody] SuaMayBayRequest data)
        {
            if (string.IsNullOrWhiteSpace(data.MaLoaiMb)) return BadRequest("Dữ liệu không hợp lệ.");
            var existing = await _context.LoaiMayBays.FirstOrDefaultAsync(m => m.MaLoaiMb == data.MaLoaiMb);
            if (existing == null) return NotFound("Không tìm thấy Loại máy bay.");

            existing.TenLoaiMb = data.TenLoaiMb;
            existing.MaHang = data.MaHang;
            existing.SoLuongGhe = data.SoLuongGhe;
            _context.LoaiMayBays.Update(existing);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-mb");
            }

            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        [HttpDelete]
        public async Task<IActionResult> XoaMayBay(string id)
        {
            var existing = await _context.LoaiMayBays.FirstOrDefaultAsync(m => m.MaLoaiMb == id);
            if (existing == null) return NotFound("Không tìm thấy Loại máy bay.");

            var isUsed = await _context.ChuyenBays.AnyAsync(c => c.MaLoaiMb == id) || await _context.Ghes.AnyAsync(g => g.MaLoaiMb == id);
            if (isUsed)
            {
                existing.DaXoa = true;
                _context.LoaiMayBays.Update(existing);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-mb");
                }

                return Ok(new { success = true, message = "Xóa mềm thành công do máy bay đang được sử dụng." });
            }
            _context.LoaiMayBays.Remove(existing);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(QuanLyMayBay)) + "#tab-mb");
            }

            return Ok(new { success = true, message = "Xóa thành công!" });
        }

        [AdminAuthorize("SEAT_MAP")]
        public async Task<IActionResult> SoDoGhe()
        {
            var DanhMucHangGhes = await _context.DanhMucHangGhes
                .Where(h => h.DaXoa == false || h.DaXoa == null)
                .ToListAsync();

            // Ép buộc làm nổi bật màu sắc theo yêu cầu
            bool isUpdated = false;
            foreach (var g in DanhMucHangGhes)
            {
                if (g.TenHangGhe.Contains("Nhất", StringComparison.OrdinalIgnoreCase))
                {
                    if (g.MauSac != "#dc3545") { g.MauSac = "#dc3545"; isUpdated = true; } // Đỏ
                }
                else if (g.TenHangGhe.Contains("Thương gia", StringComparison.OrdinalIgnoreCase))
                {
                    if (g.MauSac != "#fd7e14") { g.MauSac = "#fd7e14"; isUpdated = true; } // Cam
                }
                else if (g.TenHangGhe.Contains("Đặc biệt", StringComparison.OrdinalIgnoreCase))
                {
                    if (g.MauSac != "#20c997") { g.MauSac = "#20c997"; isUpdated = true; } // Xanh ngọc
                }
                else if (g.TenHangGhe.Contains("Phổ thông", StringComparison.OrdinalIgnoreCase))
                {
                    if (g.MauSac != "#0d6efd") { g.MauSac = "#0d6efd"; isUpdated = true; } // Xanh dương
                }
            }

            if (isUpdated)
            {
                _context.UpdateRange(DanhMucHangGhes);
                await _context.SaveChangesAsync();
            }

            // Sắp xếp: Ưu tiên Hạng Nhất -> Thương Gia -> Đặc Biệt -> Phổ Thông
            ViewBag.DanhSachHangGhe = DanhMucHangGhes
                .OrderByDescending(g => g.TenHangGhe.Contains("Nhất", StringComparison.OrdinalIgnoreCase) ? 4 : g.TenHangGhe.Contains("Thương gia", StringComparison.OrdinalIgnoreCase) ? 3 : g.TenHangGhe.Contains("Đặc biệt", StringComparison.OrdinalIgnoreCase) ? 2 : 1)
                .ThenByDescending(g => g.HeSoGia)
                .ToList();

            ViewBag.DanhSachMayBay = await _context.LoaiMayBays
                .Where(m => m.DaXoa == false || m.DaXoa == null)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LaySoDoGhe(string maLoaiMb)
        {
            if (string.IsNullOrWhiteSpace(maLoaiMb)) return BadRequest("Mã loại máy bay không hợp lệ.");
            
            var danhSachGhe = await _context.Ghes
                .Include(g => g.MaHangGheNavigation)
                .Where(g => g.MaLoaiMb == maLoaiMb && (g.DaXoa == false || g.DaXoa == null))
                .Select(g => new {
                    maGhe = g.MaGhe,
                    soGhe = g.SoGhe,
                    maHangGhe = g.MaHangGhe,
                    tenHangGhe = g.MaHangGheNavigation.TenHangGhe,
                    mauSac = g.MaHangGheNavigation.MauSac,
                    phuThu = g.PhuThu,
                    trangThai = g.TrangThai,
                    viTriHang = g.ViTriHang,
                    viTriCot = g.ViTriCot
                })
                .ToListAsync();

            return Ok(new { success = true, data = danhSachGhe });
        }

     

        [HttpPost]
        public async Task<IActionResult> LuuSoDoGhe([FromBody] LuuSoDoGheRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.MaLoaiMb))
                return BadRequest("Dữ liệu không hợp lệ.");

            // Kiểm tra xem đã có vé nào được đặt cho loại máy bay này chưa
            bool hasTickets = await _context.Ves.AnyAsync(v => v.MaCbNavigation.MaLoaiMb == req.MaLoaiMb);
            if (hasTickets)
            {
                return Ok(new { success = false, message = "Không thể thay đổi sơ đồ ghế vì đã có vé được bán cho loại máy bay này." });
            }

            // Xoá cứng tất cả ghế cũ để ghi đè sơ đồ mới
            var gheCu = await _context.Ghes.Where(g => g.MaLoaiMb == req.MaLoaiMb).ToListAsync();
            _context.Ghes.RemoveRange(gheCu);

            if (req.DanhSachGhe != null && req.DanhSachGhe.Any())
            {
                var listGheMoi = req.DanhSachGhe.Select(g => new Ghe
                {
                    MaLoaiMb = req.MaLoaiMb,
                    SoGhe = g.SoGhe,
                    MaHangGhe = g.MaHangGhe,
                    PhuThu = g.PhuThu,
                    TrangThai = g.TrangThai,
                    ViTriHang = g.ViTriHang,
                    ViTriCot = g.ViTriCot,
                    DaXoa = false
                }).ToList();

                _context.Ghes.AddRange(listGheMoi);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Lưu cấu hình sơ đồ ghế thành công!" });
        }

        [HttpGet]
        [AdminAuthorize("FLIGHT_MGMT")]
        public async Task<IActionResult> QuanLyChuyenBay(int page = 1)
        {
            int pageSize = 5; 

            int totalFlights = await _context.ChuyenBays.Where(c => c.DaXoa == false || c.DaXoa == null).CountAsync();

            int totalPages = (int)System.Math.Ceiling((double)totalFlights / pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var flights = await _context.ChuyenBays
                .Include(c => c.MaLoaiMbNavigation)
                .Include(c => c.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdenNavigation)
                .Where(c => c.DaXoa == false || c.DaXoa == null)
                .OrderByDescending(c => c.NgayGioBay)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.DanhSachMayBay = await _context.LoaiMayBays.ToListAsync();
            ViewBag.DanhSachTuyenBay = await _context.TuyenBays.Where(t => (t.DaXoa == false || t.DaXoa == null) && t.TrangThai == true).ToListAsync();

            return View(flights);
        }
        [HttpGet]
        public async Task<IActionResult> ChiTietChuyenBay(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Mã chuyến bay không hợp lệ.");

            var flight = await _context.ChuyenBays
                .Include(c => c.MaLoaiMbNavigation)
                .Include(c => c.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation)
                    .ThenInclude(t => t.MaSbdenNavigation)
                .FirstOrDefaultAsync(c => c.MaCb == id && (c.DaXoa == false || c.DaXoa == null));

            if (flight == null) return NotFound("Chuyến bay không tồn tại.");

            return Ok(BuildChuyenBayPayload(flight));
        }

        [HttpPost]
        public async Task<IActionResult> ThemChuyenBay([FromBody] ChuyenBay chuyenBay)
        {
            if (chuyenBay == null || string.IsNullOrWhiteSpace(chuyenBay.MaCb))
                return BadRequest("Dữ liệu không hợp lệ.");

            var exists = await _context.ChuyenBays.AnyAsync(c => c.MaCb == chuyenBay.MaCb);
            if (exists) return BadRequest("Mã chuyến bay đã tồn tại trong hệ thống.");

            chuyenBay.DaXoa = false;

            if (string.IsNullOrWhiteSpace(chuyenBay.TrangThai))
            {
                chuyenBay.TrangThai = "Sắp bay";
            }

            if (chuyenBay.GiaVeCoBan == null || chuyenBay.GiaVeCoBan < 0)
            {
                chuyenBay.GiaVeCoBan = 0;
            }

            _context.ChuyenBays.Add(chuyenBay);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Thêm chuyến bay thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> SuaChuyenBay([FromBody] SuaChuyenBayRequest data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.MaCb))
                    return BadRequest("Dữ liệu không hợp lệ.");

                var existingCb = await _context.ChuyenBays.FirstOrDefaultAsync(c => c.MaCb == data.MaCb);
                if (existingCb == null)
                    return NotFound("Chuyến bay không tồn tại.");

                existingCb.MaLoaiMb = data.MaLoaiMb;
                existingCb.MaTuyen = data.MaTuyen;
                existingCb.NgayGioBay = data.NgayGioBay;
                existingCb.TrangThai = data.TrangThai;

                existingCb.GiaVeCoBan = data.GiaVeCoBan;

                _context.ChuyenBays.Update(existingCb);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Cập nhật chuyến bay thành công" });
            }
            catch (System.Exception ex)
            {
                return BadRequest("Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> XoaChuyenBay(string id)
        {
            var existingCb = await _context.ChuyenBays.FirstOrDefaultAsync(c => c.MaCb == id);
            if (existingCb == null) return NotFound("Chuyến bay không tồn tại.");

            var isUsedInVe = await _context.Ves.AnyAsync(v => v.MaCb == id);

            if (isUsedInVe)
            {
                existingCb.DaXoa = true;

                _context.ChuyenBays.Update(existingCb);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Xóa mềm chuyến bay thành công do đã có vé được đặt." });
            }

            _context.ChuyenBays.Remove(existingCb);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa chuyến bay thành công." });
        }
        private object BuildChuyenBayPayload(ChuyenBay flight)
        {
            var tuyenBay = flight.MaTuyenNavigation;
            var sbDi = tuyenBay?.MaSbdiNavigation;
            var sbDen = tuyenBay?.MaSbdenNavigation;

            return new
            {
                success = true,
                data = new
                {
                    maCb = flight.MaCb,
                    maLoaiMb = flight.MaLoaiMb,
                    maTuyen = flight.MaTuyen,

                    ngayGioBay = flight.NgayGioBay.ToString("yyyy-MM-ddTHH:mm"),

                    ngayGioBayDisplay = flight.NgayGioBay.ToString("HH:mm - dd/MM/yyyy"),

                    trangThai = flight.TrangThai ?? "Sắp bay",

                    giaVeGoc = flight.GiaVeCoBan,

                    giaVeCoBanDisplay = flight.GiaVeCoBan != null ? string.Format("{0:N0} VNĐ", flight.GiaVeCoBan) : "Chưa cập nhật",

                    tenMayBay = flight.MaLoaiMbNavigation?.TenLoaiMb ?? "N/A",
                    hangSanXuat = flight.MaLoaiMbNavigation?.MaHang ?? "N/A",

                    maSbDi = sbDi?.MaSb ?? "N/A",
                    thanhPhoDi = sbDi?.ThanhPho ?? "N/A",
                    tenSbDi = sbDi?.TenSb ?? "N/A",

                    maSbDen = sbDen?.MaSb ?? "N/A",
                    thanhPhoDen = sbDen?.ThanhPho ?? "N/A",
                    tenSbDen = sbDen?.TenSb ?? "N/A"
                }
            };
        }
        [AdminAuthorize("AIRPORT_ROUTE")]
        public async Task<IActionResult> SanBayVaTuyenBay(int pageSb = 1, int pageTb = 1)
        {
            int pageSize = 5;

            int totalAirports = await _context.SanBays.Where(sb => sb.DaXoa == false || sb.DaXoa == null).CountAsync();

            int activeRoutes = await _context.TuyenBays.Where(t => (t.DaXoa == false || t.DaXoa == null) && t.TrangThai == true).CountAsync();

            int maintenanceRoutes = await _context.TuyenBays.Where(t => (t.DaXoa == false || t.DaXoa == null) && t.TrangThai == false).CountAsync();

            ViewBag.TotalAirports = totalAirports;
            ViewBag.ActiveRoutes = activeRoutes;
            ViewBag.MaintenanceRoutes = maintenanceRoutes;

            int totalPagesSb = (int)System.Math.Ceiling((double)totalAirports / pageSize);
            if (totalPagesSb == 0) totalPagesSb = 1;
            if (pageSb < 1) pageSb = 1;
            if (pageSb > totalPagesSb) pageSb = totalPagesSb;

            var airports = await _context.SanBays
                .Where(sb => sb.DaXoa == false || sb.DaXoa == null)
                .OrderBy(sb => sb.MaSb)
                .Skip((pageSb - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allAirports = await _context.SanBays
                .Where(sb => sb.DaXoa == false || sb.DaXoa == null)
                .OrderBy(sb => sb.MaSb)
                .ToListAsync();

            int totalRoutes = activeRoutes + maintenanceRoutes;
            int totalPagesTb = (int)System.Math.Ceiling((double)totalRoutes / pageSize);

            if (totalPagesTb == 0) totalPagesTb = 1;
            if (pageTb < 1) pageTb = 1;
            if (pageTb > totalPagesTb) pageTb = totalPagesTb;

            var routes = await _context.TuyenBays
                .Include(t => t.MaSbdiNavigation)
                .Include(t => t.MaSbdenNavigation)
                .Where(t => t.DaXoa == false || t.DaXoa == null)
                .OrderBy(t => t.MaTuyen)
                .Skip((pageTb - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Airports = airports;
            ViewBag.AllAirports = allAirports;
            ViewBag.Routes = routes;

            ViewBag.CurrentPageSb = pageSb;
            ViewBag.TotalPagesSb = totalPagesSb;

            ViewBag.CurrentPageTb = pageTb;
            ViewBag.TotalPagesTb = totalPagesTb;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ThemSanBay([FromBody] HeThong_BanVeMayBay.Models.Entities.SanBay sanBay)
        {
            if (sanBay == null || string.IsNullOrWhiteSpace(sanBay.MaSb)) return BadRequest("Dữ liệu không hợp lệ.");

            var exists = await _context.SanBays.AnyAsync(s => s.MaSb == sanBay.MaSb);
            if (exists) return BadRequest("Sân bay đã tồn tại trong hệ thống.");

            sanBay.DaXoa = false;
            _context.SanBays.Add(sanBay);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#airport-content");
            }

            return Ok(new { success = true, message = "Thêm sân bay thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> SuaSanBay([FromBody] SuaSanBayRequest data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.OldMaSb))
                    return BadRequest("Dữ liệu không hợp lệ.");

                var existingSb = await _context.SanBays.FirstOrDefaultAsync(s => s.MaSb == data.OldMaSb);
                if (existingSb == null)
                    return NotFound("Sân bay không tồn tại.");
                if (data.OldMaSb != data.MaSb)
                {
                    return BadRequest("Hệ thống không cho phép thay đổi Mã sân bay vì có liên kết với Tuyến bay. Vui lòng chỉ sửa Tên và Thành phố.");
                }

                existingSb.TenSb = data.TenSb;
                existingSb.ThanhPho = data.ThanhPho;

                _context.SanBays.Update(existingSb);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#airport-content");
                }

                return Ok(new { success = true, message = "Cập nhật sân bay thành công" });
            }
            catch (System.Exception ex)
            {
                return BadRequest("Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> XoaSanBay(string id)
        {
            var existingSb = await _context.SanBays.FirstOrDefaultAsync(s => s.MaSb == id);
            if (existingSb == null) return NotFound("Sân bay không tồn tại.");

            var isUsedInRoute = await _context.TuyenBays.AnyAsync(t => t.MaSbdi == id || t.MaSbden == id);
            if (isUsedInRoute)
            {
                existingSb.DaXoa = true; 
                _context.SanBays.Update(existingSb);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#airport-content");
                }

                return Ok(new { success = true, message = "Xóa mềm sân bay thành công do có dữ liệu liên kết." });
            }

            _context.SanBays.Remove(existingSb);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#airport-content");
            }

            return Ok(new { success = true, message = "Xóa sân bay thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> ThemTuyenBay([FromBody] HeThong_BanVeMayBay.Models.Entities.TuyenBay tuyenBay)
        {
            if (tuyenBay == null || string.IsNullOrWhiteSpace(tuyenBay.MaSbdi) || string.IsNullOrWhiteSpace(tuyenBay.MaSbden)) return BadRequest("Dữ liệu không hợp lệ.");

            if (tuyenBay.MaSbdi == tuyenBay.MaSbden) return BadRequest("Điểm đi và điểm đến không được trùng nhau.");

            var maTuyen = $"{tuyenBay.MaSbdi}-{tuyenBay.MaSbden}";
            var exists = await _context.TuyenBays.AnyAsync(t => t.MaTuyen == maTuyen);

            if (exists) return BadRequest("Tuyến bay này đã tồn tại.");

            tuyenBay.MaTuyen = maTuyen;
            tuyenBay.TrangThai = true;
            tuyenBay.DaXoa = false;

            _context.TuyenBays.Add(tuyenBay);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#route-content");
            }

            return Ok(new { success = true, message = "Thêm tuyến bay thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> SuaTuyenBay([FromBody] HeThong_BanVeMayBay.Models.Entities.TuyenBay tuyenBay)
        {
            if (tuyenBay == null || string.IsNullOrWhiteSpace(tuyenBay.MaTuyen)) return BadRequest("Dữ liệu không hợp lệ.");

            var existingTb = await _context.TuyenBays.FirstOrDefaultAsync(t => t.MaTuyen == tuyenBay.MaTuyen);
            if (existingTb == null) return NotFound("Tuyến bay không tồn tại.");

            existingTb.KhoangCach = tuyenBay.KhoangCach;

            _context.TuyenBays.Update(existingTb);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#route-content");
            }

            return Ok(new { success = true, message = "Cập nhật tuyến bay thành công" });
        }

        [HttpDelete]
        public async Task<IActionResult> XoaTuyenBay(string id)
        {
            var existingTb = await _context.TuyenBays.FirstOrDefaultAsync(t => t.MaTuyen == id);
            if (existingTb == null) return NotFound("Tuyến bay không tồn tại.");

            var isUsedInFlight = await _context.ChuyenBays.AnyAsync(c => c.MaTuyen == id);

            if (isUsedInFlight)
            {
                existingTb.DaXoa = true;      
                existingTb.TrangThai = false; 

                _context.TuyenBays.Update(existingTb);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#route-content");
                }

                return Ok(new { success = true, message = "Xóa mềm tuyến bay thành công do có dữ liệu liên kết." });
            }

            _context.TuyenBays.Remove(existingTb);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(SanBayVaTuyenBay)) + "#route-content");
            }

            return Ok(new { success = true, message = "Xóa tuyến bay thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThaiTuyenBay(string id, bool status)
        {
            var existingTb = await _context.TuyenBays.FirstOrDefaultAsync(t => t.MaTuyen == id);
            if (existingTb == null) return NotFound("Tuyến bay không tồn tại.");

            existingTb.TrangThai = status;

            _context.TuyenBays.Update(existingTb);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công" });
        }

        [AdminAuthorize("CABIN_CLASS")]
        public async Task<IActionResult> DanhMucHangGhe(int pageTi = 1)
        {
            var danhMucHangGhe = await _context.DanhMucHangGhes
                .Include(g => g.MaTienIches)
                .OrderByDescending(hg => hg.HeSoGia)
                .ToListAsync();

            int pageSize = 5;
            var queryTienIch = _context.TienIches
                .Where(t => t.DaXoa == false || t.DaXoa == null)
                .AsQueryable();

            int totalTi = await queryTienIch.CountAsync();
            int totalPagesTi = (int)System.Math.Ceiling((double)totalTi / pageSize);
            if (totalPagesTi == 0) totalPagesTi = 1;
            if (pageTi < 1) pageTi = 1;
            if (pageTi > totalPagesTi) pageTi = totalPagesTi;

            ViewBag.CurrentPageTi = pageTi;
            ViewBag.TotalPagesTi = totalPagesTi;

            ViewBag.DsTienIch = await queryTienIch
                .OrderBy(t => t.MaTienIch)
                .Skip((pageTi - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(danhMucHangGhe);
        }

        [HttpGet]
        public async Task<IActionResult> LayThongTinHangGhe(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Mã không hợp lệ");

            var hg = await _context.DanhMucHangGhes
                .Include(g => g.MaTienIches)
                .FirstOrDefaultAsync(h => h.MaHangGhe == id && (h.DaXoa == false || h.DaXoa == null));

            if (hg == null) return NotFound("Không tìm thấy hạng ghế.");

            return Ok(new
            {
                maHangGhe = hg.MaHangGhe,
                tenHangGhe = hg.TenHangGhe,
                heSoGia = hg.HeSoGia,
                hanhLyXachTay = hg.HanhLyXachTay,
                hanhLyKyGui = hg.HanhLyKyGui,
                hoanVe = hg.HoanVe,
                doiLich = hg.DoiLich,
                mauSac = hg.MauSac,
                maTienIchIds = hg.MaTienIches.Select(t => t.MaTienIch).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ThemHangGhe([FromBody] LuuHangGheRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.TenHangGhe))
                return BadRequest("Tên hạng ghế không được để trống.");

            string newId = "HG" + System.DateTime.Now.Ticks.ToString().Substring(10);
            var hg = new DanhMucHangGhe { MaHangGhe = newId, DaXoa = false };

            await MappingVaLuu(hg, req);

            return Ok(new { success = true, message = "Thêm mới hạng ghế thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatHangGhe([FromBody] LuuHangGheRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.MaHangGhe))
                return BadRequest("Dữ liệu không hợp lệ.");

            var hg = await _context.DanhMucHangGhes
                .Include(hg => hg.MaTienIches)
                .FirstOrDefaultAsync(h => h.MaHangGhe == req.MaHangGhe);

            if (hg == null) return NotFound("Hạng ghế không tồn tại.");

            await MappingVaLuu(hg, req);

            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        private async Task MappingVaLuu(DanhMucHangGhe hg, LuuHangGheRequest req)
        {
            hg.TenHangGhe = req.TenHangGhe;
            hg.HeSoGia = req.HeSoGia;
            hg.HanhLyXachTay = req.HanhLyXachTay;
            hg.HanhLyKyGui = req.HanhLyKyGui;
            hg.HoanVe = req.HoanVe;
            hg.DoiLich = req.DoiLich;
            hg.MauSac = req.MauSac;

            hg.MaTienIches.Clear();
            if (req.MaTienIchIds != null && req.MaTienIchIds.Any())
            {
                var tienIchList = await _context.TienIches
                    .Where(t => req.MaTienIchIds.Contains(t.MaTienIch))
                    .ToListAsync();

                foreach (var tienIch in tienIchList)
                {
                    hg.MaTienIches.Add(tienIch);
                }
            }
            else if (!string.IsNullOrWhiteSpace(req.Amenities))
            {
                var listTen = req.Amenities.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct();
                foreach (var ten in listTen)
                {
                    var tienIch = await _context.TienIches.FirstOrDefaultAsync(t => t.TenTienIch.ToLower() == ten.ToLower())
                                  ?? new TienIch { TenTienIch = ten, DaXoa = false };
                    hg.MaTienIches.Add(tienIch);
                }
            }

            if (_context.Entry(hg).State == EntityState.Detached) _context.DanhMucHangGhes.Add(hg);
            else _context.DanhMucHangGhes.Update(hg);

            await _context.SaveChangesAsync();
        }

        [HttpDelete]
        public async Task<IActionResult> XoaHangGhe(string id)
        {
            var hg = await _context.DanhMucHangGhes.FirstOrDefaultAsync(h => h.MaHangGhe == id);
            if (hg == null) return NotFound("Không tìm thấy hạng ghế.");

            var isUsed = await _context.Ves.AnyAsync(v => v.MaGheNavigation.MaHangGhe == id);

            if (isUsed)
            {
                hg.DaXoa = true;    
                _context.DanhMucHangGhes.Update(hg);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Đã xóa mềm do hạng ghế đang có vé sử dụng." });
            }

            _context.DanhMucHangGhes.Remove(hg);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Xóa hạng ghế thành công." });
        }
        
        [HttpPost]
        public async Task<IActionResult> LuuTienIch([FromBody] TienIch req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.TenTienIch))
                return BadRequest("Dữ liệu không hợp lệ.");

            if (req.MaTienIch == 0)
            {
                req.DaXoa = false;
                _context.TienIches.Add(req);
            }
            else
            {
                var ti = await _context.TienIches.FindAsync(req.MaTienIch);
                if (ti == null) return NotFound();

                ti.TenTienIch = req.TenTienIch;
                ti.Icon = req.Icon;
                _context.TienIches.Update(ti);
            }

            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(DanhMucHangGhe)) + "#pills-tienich");
            }

            return Ok(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> XoaTienIch(int id)
        {
            var ti = await _context.TienIches.FindAsync(id);
            if (ti == null) return NotFound();

            _context.TienIches.Remove(ti);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                return Redirect(Url.Action(nameof(DanhMucHangGhe)) + "#pills-tienich");
            }

            return Ok(new { success = true });
        }
        public class SuaSanBayRequest
        {
            public string OldMaSb { get; set; }
            public string MaSb { get; set; }
            public string TenSb { get; set; }
            public string ThanhPho { get; set; }
        }

        public class SuaChuyenBayRequest
        {
            public string MaCb { get; set; }
            public string MaLoaiMb { get; set; }
            public string MaTuyen { get; set; }
            public DateTime NgayGioBay { get; set; }
            public string TrangThai { get; set; }
            public decimal GiaVeCoBan { get; set; }
        }
        public class SuaMayBayRequest
        {
            public string MaLoaiMb { get; set; }
            public string TenLoaiMb { get; set; }
            public string MaHang { get; set; }
            public int SoLuongGhe { get; set; }
        }
        public class LuuHangGheRequest
        {
            public string MaHangGhe { get; set; }
            public string TenHangGhe { get; set; }
            public double HeSoGia { get; set; }
            public string HanhLyXachTay { get; set; }
            public string HanhLyKyGui { get; set; }
            public string HoanVe { get; set; }
            public string DoiLich { get; set; }
            public string MauSac { get; set; }
            public string Amenities { get; set; }
            public List<int> MaTienIchIds { get; set; }
        }
        public class GheDto
        {
            public string SoGhe { get; set; }
            public string MaHangGhe { get; set; }
            public decimal? PhuThu { get; set; }
            public bool? TrangThai { get; set; }
            public int? ViTriHang { get; set; }
            public int? ViTriCot { get; set; }
        }

        public class LuuSoDoGheRequest
        {
            public string MaLoaiMb { get; set; }
            public List<GheDto> DanhSachGhe { get; set; }
        }
    }
}