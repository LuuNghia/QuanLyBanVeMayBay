using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using HeThong_BanVeMayBay.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

using HeThong_BanVeMayBay.Attributes;

namespace HeThong_BanVeMayBay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ProfileController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public ProfileController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        public IActionResult ThongTinCaNhan()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfileAPI()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var user = await _context.TaiKhoans
                    .Include(t => t.NhanVien)
                    .FirstOrDefaultAsync(t => t.MaTk == userId);

                if (user == null || user.NhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                return Json(new {
                    success = true,
                    data = new
                    {
                        hoTen = user.NhanVien.HoTen,
                        email = user.NhanVien.Email,
                        sdt = user.NhanVien.SoDienThoai,
                        cccd = user.NhanVien.Cccd,
                        gioiTinh = user.NhanVien.GioiTinh,
                        ngaySinh = user.NhanVien.NgaySinh?.ToString("dd-MM-yyyy"),
                        diaChi = user.NhanVien.DiaChi,
                        chucVu = user.NhanVien.ChucVu,
                        ngayVaoLam = user.NhanVien.NgayVaoLam?.ToString("dd-MM-yyyy"),
                        anhDaiDien = !string.IsNullOrEmpty(user.NhanVien.AnhDaiDien) ? user.NhanVien.AnhDaiDien : $"https://ui-avatars.com/api/?name={user.NhanVien.HoTen}&background=003580&color=ffffff&size=128"
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileAPI([FromBody] ProfileUpdateRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

                var nhanVien = await _context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.MaTk == userId);
                
                if (nhanVien == null) return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });

                // Kiểm tra trùng lặp (Cả trong bảng Nhân viên và Khách hàng)
                if (!string.IsNullOrEmpty(request.Email))
                {
                    bool existsInNhanVien = await _context.NhanViens.AnyAsync(nv => nv.Email == request.Email && nv.MaTk != userId);
                    bool existsInKhachHang = await _context.KhachHangs.AnyAsync(kh => kh.Email == request.Email);
                    if (existsInNhanVien || existsInKhachHang) 
                        return Json(new { success = false, message = "Email này đã được sử dụng trong hệ thống" });
                }

                if (!string.IsNullOrEmpty(request.Sdt))
                {
                    bool existsInNhanVien = await _context.NhanViens.AnyAsync(nv => nv.SoDienThoai == request.Sdt && nv.MaTk != userId);
                    bool existsInKhachHang = await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == request.Sdt);
                    if (existsInNhanVien || existsInKhachHang) 
                        return Json(new { success = false, message = "Số điện thoại này đã được sử dụng trong hệ thống" });
                }

                if (!string.IsNullOrEmpty(request.Cccd))
                {
                    bool existsInNhanVien = await _context.NhanViens.AnyAsync(nv => nv.Cccd == request.Cccd && nv.MaTk != userId);
                    bool existsInKhachHang = await _context.KhachHangs.AnyAsync(kh => kh.Cccd == request.Cccd);
                    if (existsInNhanVien || existsInKhachHang) 
                        return Json(new { success = false, message = "Số CCCD này đã được sử dụng trong hệ thống" });
                }

                nhanVien.HoTen = request.HoTen;
                nhanVien.GioiTinh = request.GioiTinh;
                nhanVien.NgaySinh = !string.IsNullOrEmpty(request.NgaySinh) ? DateOnly.Parse(request.NgaySinh) : null;
                nhanVien.Cccd = request.Cccd;
                nhanVien.SoDienThoai = request.Sdt;
                nhanVien.Email = request.Email;
                nhanVien.DiaChi = request.DiaChi;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePasswordAPI([FromBody] ChangePasswordRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

                var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.MaTk == userId);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản" });

                if (user.MatKhau != request.OldPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu cũ không chính xác" });
                }

                user.MatKhau = request.NewPassword;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvatarAPI(IFormFile avatar)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

                if (avatar == null || avatar.Length == 0)
                    return Json(new { success = false, message = "Vui lòng chọn ảnh" });

                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaTk == userId);
                if (nhanVien == null) return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });

                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatar.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                nhanVien.AnhDaiDien = "/uploads/avatars/" + fileName;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", url = nhanVien.AnhDaiDien });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public class ProfileUpdateRequest
        {
            public string HoTen { get; set; }
            public string GioiTinh { get; set; }
            public string NgaySinh { get; set; }
            public string Cccd { get; set; }
            public string Sdt { get; set; }
            public string DiaChi { get; set; }
            public string Email { get; set; }
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }
    }
}