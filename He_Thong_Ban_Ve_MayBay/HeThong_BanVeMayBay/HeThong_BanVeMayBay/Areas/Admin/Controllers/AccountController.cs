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
    public class AccountController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public AccountController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        [AdminAuthorize("USER_MGMT")]
        public async Task<IActionResult> NhanVienVaKhachHang(int pageNv = 1, int pageKh = 1)
        {
            int pageSize = 5;

            var nhanVienQuery = _context.NhanViens
                .Include(nv => nv.MaTkNavigation)
                .Where(nv => (nv.DaXoa == false || nv.DaXoa == null)
                             && (nv.MaTkNavigation == null || nv.MaTkNavigation.DaXoa == false || nv.MaTkNavigation.DaXoa == null))
                .OrderBy(nv => nv.MaNv)
                .AsQueryable();

            var khachHangQuery = _context.KhachHangs
                .Include(kh => kh.MaTkNavigation)
                .Where(kh => (kh.DaXoa == false || kh.DaXoa == null)
                             && (kh.MaTkNavigation == null || kh.MaTkNavigation.DaXoa == false || kh.MaTkNavigation.DaXoa == null))
                .OrderByDescending(kh => kh.DiemTichLuy)
                .AsQueryable();

            int totalNhanVien = await nhanVienQuery.CountAsync();
            int totalKhachHang = await khachHangQuery.CountAsync();

            int totalPagesNv = (int)System.Math.Ceiling((double)totalNhanVien / pageSize);
            if (totalPagesNv == 0) totalPagesNv = 1;
            if (pageNv < 1) pageNv = 1;
            if (pageNv > totalPagesNv) pageNv = totalPagesNv;

            int totalPagesKh = (int)System.Math.Ceiling((double)totalKhachHang / pageSize);
            if (totalPagesKh == 0) totalPagesKh = 1;
            if (pageKh < 1) pageKh = 1;
            if (pageKh > totalPagesKh) pageKh = totalPagesKh;

            var nhanViens = await nhanVienQuery
                .Skip((pageNv - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var khachHangs = await khachHangQuery
                .Skip((pageKh - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalAccounts = await _context.TaiKhoans
                .CountAsync(tk => tk.DaXoa == false || tk.DaXoa == null);
            var activeStaffs = await nhanVienQuery.CountAsync(nv => nv.MaTkNavigation != null && nv.MaTkNavigation.TrangThai == true);
            var loaiTaiKhoanList = new List<string> { "Admin", "Nhân viên", "Khách hàng" };

            ViewBag.NhanViens = nhanViens;
            ViewBag.KhachHangs = khachHangs;
            ViewBag.TotalAccounts = totalAccounts;
            ViewBag.ActiveStaffs = activeStaffs;
            ViewBag.TotalCustomers = totalKhachHang;
            ViewBag.CurrentPageNv = pageNv;
            ViewBag.TotalPagesNv = totalPagesNv;
            ViewBag.CurrentPageKh = pageKh;
            ViewBag.TotalPagesKh = totalPagesKh;
            ViewBag.LoaiTaiKhoanList = loaiTaiKhoanList;

            return View("NhanVienVaKhachHang");
        }

        [HttpGet]
        public async Task<IActionResult> LayThongTinNhanVien(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Dữ liệu không hợp lệ.");

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.MaTkNavigation)
                .FirstOrDefaultAsync(nv => nv.MaNv == id && (nv.DaXoa == false || nv.DaXoa == null));

            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");

            return Ok(new
            {
                maNv = nhanVien.MaNv,
                maTk = nhanVien.MaTk,
                hoTen = nhanVien.HoTen,
                soDienThoai = nhanVien.SoDienThoai,
                email = nhanVien.Email,
                cccd = nhanVien.Cccd,
                loaiTaiKhoan = nhanVien.MaTkNavigation?.LoaiTaiKhoan,
                gioiTinh = nhanVien.GioiTinh,
                ngaySinh = nhanVien.NgaySinh?.ToString("dd/MM/yyyy"),
                diaChi = nhanVien.DiaChi,
                chucVu = nhanVien.ChucVu,
                ngayThamGia = nhanVien.NgayVaoLam?.ToString("dd/MM/yyyy"),
                anhDaiDien = nhanVien.AnhDaiDien,
                quyen = nhanVien.Quyen
            });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatQuyenNhanVien(string maNv, string quyen)
        {
            if (string.IsNullOrWhiteSpace(maNv)) return BadRequest("Mã nhân viên không hợp lệ.");

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == maNv);
            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");

            nhanVien.Quyen = quyen;
            _context.NhanViens.Update(nhanVien);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật quyền truy cập thành công." });
        }

        [HttpGet]
        public async Task<IActionResult> LayThongTinKhachHang(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Dữ liệu không hợp lệ.");

            var khachHang = await _context.KhachHangs
                .Include(kh => kh.MaTkNavigation)
                .FirstOrDefaultAsync(kh => kh.MaKh == id && (kh.DaXoa == false || kh.DaXoa == null));

            if (khachHang == null) return NotFound("Không tìm thấy khách hàng.");

            DateTime? ngayThamGia = null;
            if (khachHang.MaTk.HasValue)
            {
                ngayThamGia = await _context.Set<HoaDon>()
                    .Where(hd => hd.MaTk == khachHang.MaTk)
                    .OrderBy(hd => hd.NgayLap)
                    .Select(hd => hd.NgayLap)
                    .FirstOrDefaultAsync();
            }

            return Ok(new
            {
                maKh = khachHang.MaKh,
                maTk = khachHang.MaTk,
                hoTen = khachHang.HoTen,
                soDienThoai = khachHang.SoDienThoai,
                email = khachHang.Email,
                cccd = khachHang.Cccd,
                loaiTaiKhoan = khachHang.MaTkNavigation?.LoaiTaiKhoan,
                gioiTinh = khachHang.GioiTinh,
                ngaySinh = khachHang.NgaySinh?.ToString("dd/MM/yyyy"),
                diaChi = khachHang.DiaChi,
                loaiThanhVien = khachHang.LoaiThanhVien,
                diemTichLuy = khachHang.DiemTichLuy,
                ngayThamGia = ngayThamGia?.ToString("dd/MM/yyyy")
            });
        }

        [HttpGet]
        public async Task<IActionResult> LichSuKhachHang(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Dữ liệu không hợp lệ.");

            var khachHang = await _context.KhachHangs
                .Include(kh => kh.MaTkNavigation)
                .FirstOrDefaultAsync(kh => kh.MaKh == id && (kh.DaXoa == false || kh.DaXoa == null));

            if (khachHang == null) return NotFound("Không tìm thấy khách hàng.");

            var maTk = khachHang.MaTk;
            if (!maTk.HasValue)
            {
                return Ok(new
                {
                    hoTen = khachHang.HoTen,
                    loaiThanhVien = khachHang.LoaiThanhVien ?? "Thường",
                    total = 0,
                    tickets = new List<object>()
                });
            }

            var tickets = await _context.Ves
                .Include(v => v.MaHoaDonNavigation)
                .Include(v => v.MaCbNavigation)
                    .ThenInclude(cb => cb.MaTuyenNavigation)
                        .ThenInclude(t => t.MaSbdiNavigation)
                .Include(v => v.MaCbNavigation)
                    .ThenInclude(cb => cb.MaTuyenNavigation)
                        .ThenInclude(t => t.MaSbdenNavigation)
                .Where(v => v.MaHoaDon != null && v.MaHoaDonNavigation!.MaTk == maTk
                            && v.MaCbNavigation != null
                            && v.MaCbNavigation.MaTuyenNavigation != null
                            && (v.DaXoa == false || v.DaXoa == null))
                .OrderByDescending(v => v.MaCbNavigation.NgayGioBay)
                .Select(v => new
                {
                    maVe = v.MaVe,
                    sanBayDi = v.MaCbNavigation.MaTuyenNavigation.MaSbdiNavigation != null
                        ? v.MaCbNavigation.MaTuyenNavigation.MaSbdiNavigation.MaSb
                        : v.MaCbNavigation.MaTuyenNavigation.MaSbdi,
                    sanBayDen = v.MaCbNavigation.MaTuyenNavigation.MaSbdenNavigation != null
                        ? v.MaCbNavigation.MaTuyenNavigation.MaSbdenNavigation.MaSb
                        : v.MaCbNavigation.MaTuyenNavigation.MaSbden,
                    ngayBay = v.MaCbNavigation.NgayGioBay,
                    trangThai = v.MaCbNavigation.TrangThai
                })
                .ToListAsync();

            var now = System.DateTime.Now;
            var resultTickets = tickets.Select(t => new
            {
                maVe = t.maVe,
                sanBayDi = t.sanBayDi ?? "N/A",
                sanBayDen = t.sanBayDen ?? "N/A",
                ngayBay = t.ngayBay.ToString("dd/MM/yyyy"),
                trangThai = !string.IsNullOrWhiteSpace(t.trangThai)
                    ? t.trangThai
                    : (t.ngayBay < now ? "Đã hoàn thành" : "Sắp bay")
            }).ToList();

            return Ok(new
            {
                hoTen = khachHang.HoTen,
                loaiThanhVien = khachHang.LoaiThanhVien ?? "Thường",
                total = resultTickets.Count,
                tickets = resultTickets
            });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatNhanVien([FromBody] CapNhatNhanVienRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.MaNv)) return BadRequest("Dữ liệu không hợp lệ.");

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == req.MaNv);
            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");

            if (string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.SoDienThoai)
                || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Cccd))
            {
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");
            }

            if (req.NgaySinh == null) return BadRequest("Vui lòng chọn ngày sinh.");

            if (!IsValidEmail(req.Email)) return BadRequest("Email không hợp lệ.");
            if (!IsValidPhone(req.SoDienThoai)) return BadRequest("Số điện thoại không hợp lệ.");
            if (!IsValidCccd(req.Cccd)) return BadRequest("CCCD/PASSPORT phải gồm 9 hoặc 12 chữ số.");

            if (await _context.NhanViens.AnyAsync(nv => nv.Email == req.Email && nv.MaNv != req.MaNv) ||
                await _context.KhachHangs.AnyAsync(kh => kh.Email == req.Email))
            {
                return BadRequest("Email đã được sử dụng bởi người dùng khác.");
            }

            if (await _context.NhanViens.AnyAsync(nv => nv.SoDienThoai == req.SoDienThoai && nv.MaNv != req.MaNv) ||
                await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == req.SoDienThoai))
            {
                return BadRequest("Số điện thoại đã được sử dụng.");
            }

            if (await _context.NhanViens.AnyAsync(nv => nv.Cccd == req.Cccd && nv.MaNv != req.MaNv) ||
                await _context.KhachHangs.AnyAsync(kh => kh.Cccd == req.Cccd))
            {
                return BadRequest("Số CCCD/PASSPORT đã được sử dụng.");
            }

            nhanVien.HoTen = req.HoTen;
            nhanVien.GioiTinh = req.GioiTinh;
            nhanVien.NgaySinh = req.NgaySinh;
            nhanVien.SoDienThoai = req.SoDienThoai;
            nhanVien.Email = req.Email;
            nhanVien.Cccd = req.Cccd;
            nhanVien.DiaChi = req.DiaChi;
            nhanVien.ChucVu = req.ChucVu;

            _context.NhanViens.Update(nhanVien);

            if (req.MaTk.HasValue)
            {
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.MaTk == req.MaTk.Value);
                if (taiKhoan != null)
                {
                    taiKhoan.LoaiTaiKhoan = req.LoaiTaiKhoan == "Nhân viên" ? "Staff" : (req.LoaiTaiKhoan == "Khách hàng" ? "User" : req.LoaiTaiKhoan);
                    if (!string.IsNullOrWhiteSpace(req.MatKhau)) taiKhoan.MatKhau = req.MatKhau;
                    _context.TaiKhoans.Update(taiKhoan);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật nhân viên thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatKhachHang([FromBody] CapNhatKhachHangRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.MaKh)) return BadRequest("Dữ liệu không hợp lệ.");

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.MaKh == req.MaKh);
            if (khachHang == null) return NotFound("Không tìm thấy khách hàng.");

            if (string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.SoDienThoai)
                || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Cccd))
            {
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");
            }

            if (req.NgaySinh == null) return BadRequest("Vui lòng chọn ngày sinh.");

            if (!IsValidEmail(req.Email)) return BadRequest("Email không hợp lệ.");
            if (!IsValidPhone(req.SoDienThoai)) return BadRequest("Số điện thoại không hợp lệ.");
            if (!IsValidCccd(req.Cccd)) return BadRequest("CCCD/PASSPORT phải gồm 9 hoặc 12 chữ số.");

            // Kiểm tra trùng lặp (Email, SĐT, CCCD)
            if (await _context.KhachHangs.AnyAsync(kh => kh.Email == req.Email && kh.MaKh != req.MaKh) ||
                await _context.NhanViens.AnyAsync(nv => nv.Email == req.Email))
            {
                return BadRequest("Email đã được sử dụng bởi người dùng khác.");
            }

            if (await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == req.SoDienThoai && kh.MaKh != req.MaKh) ||
                await _context.NhanViens.AnyAsync(nv => nv.SoDienThoai == req.SoDienThoai))
            {
                return BadRequest("Số điện thoại đã được sử dụng.");
            }

            if (await _context.KhachHangs.AnyAsync(kh => kh.Cccd == req.Cccd && kh.MaKh != req.MaKh) ||
                await _context.NhanViens.AnyAsync(nv => nv.Cccd == req.Cccd))
            {
                return BadRequest("Số CCCD/PASSPORT đã được sử dụng.");
            }

            khachHang.HoTen = req.HoTen;
            khachHang.GioiTinh = req.GioiTinh;
            khachHang.NgaySinh = req.NgaySinh;
            khachHang.SoDienThoai = req.SoDienThoai;
            khachHang.Email = req.Email;
            khachHang.Cccd = req.Cccd;
            khachHang.DiaChi = req.DiaChi;

            _context.KhachHangs.Update(khachHang);

            if (req.MaTk.HasValue)
            {
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.MaTk == req.MaTk.Value);
                if (taiKhoan != null)
                {
                    taiKhoan.LoaiTaiKhoan = req.LoaiTaiKhoan == "Nhân viên" ? "Staff" : (req.LoaiTaiKhoan == "Khách hàng" ? "User" : req.LoaiTaiKhoan);
                    if (!string.IsNullOrWhiteSpace(req.MatKhau)) taiKhoan.MatKhau = req.MatKhau;
                    _context.TaiKhoans.Update(taiKhoan);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật khách hàng thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> ThemNhanVien([FromBody] TaoTaiKhoanRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");

            if (string.IsNullOrWhiteSpace(req.MatKhau)) return BadRequest("Vui lòng nhập mật khẩu.");
            if (req.NgaySinh == null) return BadRequest("Vui lòng chọn ngày sinh.");

            if (!IsValidEmail(req.Email)) return BadRequest("Email không hợp lệ.");
            if (!IsValidPhone(req.SoDienThoai)) return BadRequest("Số điện thoại không hợp lệ.");
            if (!IsValidCccd(req.Cccd)) return BadRequest("CCCD/PASSPORT phải gồm 9 hoặc 12 chữ số.");

            if (string.IsNullOrWhiteSpace(req.ChucVu)) return BadRequest("Vui lòng chọn chức vụ.");

            // Kiểm tra trùng lặp trên toàn hệ thống
            if (await _context.TaiKhoans.AnyAsync(tk => tk.TenDangNhap == req.Email) ||
                await _context.NhanViens.AnyAsync(nv => nv.Email == req.Email) ||
                await _context.KhachHangs.AnyAsync(kh => kh.Email == req.Email))
            {
                return BadRequest("Email đã tồn tại trong hệ thống.");
            }

            if (await _context.NhanViens.AnyAsync(nv => nv.SoDienThoai == req.SoDienThoai) ||
                await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == req.SoDienThoai))
            {
                return BadRequest("Số điện thoại này đã được sử dụng.");
            }

            if (await _context.NhanViens.AnyAsync(nv => nv.Cccd == req.Cccd) ||
                await _context.KhachHangs.AnyAsync(kh => kh.Cccd == req.Cccd))
            {
                return BadRequest("Số CCCD/PASSPORT này đã được sử dụng.");
            }

            var taiKhoan = new Models.Entities.TaiKhoan
            {
                TenDangNhap = req.Email,
                MatKhau = req.MatKhau,
                LoaiTaiKhoan = req.LoaiTaiKhoan == "Nhân viên" ? "Staff" : (req.LoaiTaiKhoan == "Khách hàng" ? "User" : req.LoaiTaiKhoan),
                TrangThai = true,
                DaXoa = false
            };

            var maNv = await GenerateNhanVienId();
            var nhanVien = new Models.Entities.NhanVien
            {
                MaNv = maNv,
                HoTen = req.HoTen,
                GioiTinh = req.GioiTinh,
                NgaySinh = req.NgaySinh,
                SoDienThoai = req.SoDienThoai,
                Email = req.Email,
                Cccd = req.Cccd,
                DiaChi = req.DiaChi,
                ChucVu = req.ChucVu,
                NgayVaoLam = System.DateOnly.FromDateTime(System.DateTime.Now),
                DaXoa = false,
                MaTkNavigation = taiKhoan
            };

            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thêm nhân viên thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> ThemKhachHang([FromBody] TaoTaiKhoanRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");

            if (string.IsNullOrWhiteSpace(req.MatKhau)) return BadRequest("Vui lòng nhập mật khẩu.");
            if (req.NgaySinh == null) return BadRequest("Vui lòng chọn ngày sinh.");

            if (!IsValidEmail(req.Email)) return BadRequest("Email không hợp lệ.");
            if (!IsValidPhone(req.SoDienThoai)) return BadRequest("Số điện thoại không hợp lệ.");
            if (!IsValidCccd(req.Cccd)) return BadRequest("CCCD/PASSPORT phải gồm 9 hoặc 12 chữ số.");

            // Kiểm tra trùng lặp trên toàn hệ thống
            if (await _context.TaiKhoans.AnyAsync(tk => tk.TenDangNhap == req.Email) ||
                await _context.KhachHangs.AnyAsync(kh => kh.Email == req.Email) ||
                await _context.NhanViens.AnyAsync(nv => nv.Email == req.Email))
            {
                return BadRequest("Email đã tồn tại trong hệ thống.");
            }

            if (await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == req.SoDienThoai) ||
                await _context.NhanViens.AnyAsync(nv => nv.SoDienThoai == req.SoDienThoai))
            {
                return BadRequest("Số điện thoại này đã được sử dụng.");
            }

            if (await _context.KhachHangs.AnyAsync(kh => kh.Cccd == req.Cccd) ||
                await _context.NhanViens.AnyAsync(nv => nv.Cccd == req.Cccd))
            {
                return BadRequest("Số CCCD/PASSPORT này đã được sử dụng.");
            }

            var taiKhoan = new Models.Entities.TaiKhoan
            {
                TenDangNhap = req.Email,
                MatKhau = req.MatKhau,
                LoaiTaiKhoan = req.LoaiTaiKhoan == "Nhân viên" ? "Staff" : (req.LoaiTaiKhoan == "Khách hàng" ? "User" : req.LoaiTaiKhoan),
                TrangThai = true,
                DaXoa = false
            };

            var maKh = await GenerateKhachHangId();
            var khachHang = new Models.Entities.KhachHang
            {
                MaKh = maKh,
                HoTen = req.HoTen,
                GioiTinh = req.GioiTinh,
                NgaySinh = req.NgaySinh,
                SoDienThoai = req.SoDienThoai,
                Email = req.Email,
                Cccd = req.Cccd,
                DiaChi = req.DiaChi,
                LoaiThanhVien = "Thường",
                DiemTichLuy = 0,
                DaXoa = false,
                MaTkNavigation = taiKhoan
            };

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thêm khách hàng thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThaiTaiKhoan(int maTk, bool status, string tab = "staff")
        {
            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.MaTk == maTk);
            if (taiKhoan == null) return NotFound("Tài khoản không tồn tại.");

            taiKhoan.TrangThai = status;
            _context.TaiKhoans.Update(taiKhoan);
            await _context.SaveChangesAsync();

            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                var hash = tab == "customer" ? "#customer-content" : "#staff-content";
                return Redirect(Url.Action(nameof(NhanVienVaKhachHang)) + hash);
            }

            return Ok(new { success = true, message = "Cập nhật trạng thái tài khoản thành công." });
        }

        [HttpDelete]
        public async Task<IActionResult> XoaTaiKhoan(int maTk, string tab = "staff")
        {
            var taiKhoan = await _context.TaiKhoans
                .Include(tk => tk.KhachHang)
                .Include(tk => tk.NhanVien)
                .FirstOrDefaultAsync(tk => tk.MaTk == maTk);

            if (taiKhoan == null) return NotFound("Tài khoản không tồn tại.");

            var hasDatVe = await _context.Set<HoaDon>().AnyAsync(hd => hd.MaTk == maTk);

            if (hasDatVe)
            {
                taiKhoan.DaXoa = true;
                taiKhoan.TrangThai = false;

                if (taiKhoan.KhachHang != null) taiKhoan.KhachHang.DaXoa = true;
                if (taiKhoan.NhanVien != null) taiKhoan.NhanVien.DaXoa = true;

                _context.TaiKhoans.Update(taiKhoan);
                await _context.SaveChangesAsync();
                if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
                {
                    var hash = tab == "customer" ? "#customer-content" : "#staff-content";
                    return Redirect(Url.Action(nameof(NhanVienVaKhachHang)) + hash);
                }

                return Ok(new { success = true, message = "Đã xóa mềm tài khoản do có dữ liệu liên kết." });
            }

            if (taiKhoan.KhachHang != null)
            {
                _context.KhachHangs.Remove(taiKhoan.KhachHang);
            }

            if (taiKhoan.NhanVien != null)
            {
                _context.NhanViens.Remove(taiKhoan.NhanVien);
            }

            _context.TaiKhoans.Remove(taiKhoan);
            await _context.SaveChangesAsync();
            if (Request.Headers["Accept"].Any(h => h.Contains("text/html")))
            {
                var hash = tab == "customer" ? "#customer-content" : "#staff-content";
                return Redirect(Url.Action(nameof(NhanVienVaKhachHang)) + hash);
            }

            return Ok(new { success = true, message = "Xóa tài khoản thành công." });
        }

        public class CapNhatNhanVienRequest
        {
            public string MaNv { get; set; }
            public int? MaTk { get; set; }
            public string HoTen { get; set; }
            public string GioiTinh { get; set; }
            public DateOnly? NgaySinh { get; set; }
            public string SoDienThoai { get; set; }
            public string Email { get; set; }
            public string Cccd { get; set; }
            public string DiaChi { get; set; }
            public string ChucVu { get; set; }
            public string LoaiTaiKhoan { get; set; }
            public string MatKhau { get; set; }
        }

        public class CapNhatKhachHangRequest
        {
            public string MaKh { get; set; }
            public int? MaTk { get; set; }
            public string HoTen { get; set; }
            public string GioiTinh { get; set; }
            public DateOnly? NgaySinh { get; set; }
            public string SoDienThoai { get; set; }
            public string Email { get; set; }
            public string Cccd { get; set; }
            public string DiaChi { get; set; }
            public string LoaiTaiKhoan { get; set; }
            public string MatKhau { get; set; }
        }

        public class TaoTaiKhoanRequest
        {
            public string HoTen { get; set; }
            public string GioiTinh { get; set; }
            public DateOnly? NgaySinh { get; set; }
            public string SoDienThoai { get; set; }
            public string Email { get; set; }
            public string Cccd { get; set; }
            public string DiaChi { get; set; }
            public string ChucVu { get; set; }
            public string LoaiTaiKhoan { get; set; }
            public string MatKhau { get; set; }
        }

        private static bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email ?? string.Empty, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$");
        }

        private static bool IsValidPhone(string phone)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phone ?? string.Empty, "^(03|05|07|08|09)\\d{8}$");
        }

        private static bool IsValidCccd(string cccd)
        {
            if (string.IsNullOrEmpty(cccd)) return false;
            return (cccd.Length == 9 || cccd.Length == 12) && cccd.All(char.IsDigit);
        }

        private async Task<string> GenerateNhanVienId()
        {
            var lastId = await _context.NhanViens
                .OrderByDescending(nv => nv.MaNv)
                .Select(nv => nv.MaNv)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrWhiteSpace(lastId) && lastId.Length > 2
                && int.TryParse(lastId.Substring(2), out var parsed))
            {
                nextNumber = parsed + 1;
            }

            return $"NV{nextNumber:D3}";
        }

        private async Task<string> GenerateKhachHangId()
        {
            var lastId = await _context.KhachHangs
                .OrderByDescending(kh => kh.MaKh)
                .Select(kh => kh.MaKh)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrWhiteSpace(lastId) && lastId.Length > 2
                && int.TryParse(lastId.Substring(2), out var parsed))
            {
                nextNumber = parsed + 1;
            }

            return $"KH{nextNumber:D3}";
        }
    }
}
