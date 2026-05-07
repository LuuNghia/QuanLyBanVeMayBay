using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using HeThong_BanVeMayBay.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

namespace HeThong_BanVeMayBay.Areas.User.Controllers
{
    [Area("User")]
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
                    .Include(t => t.KhachHang)
                    .FirstOrDefaultAsync(t => t.MaTk == userId);

                if (user == null || user.KhachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                }

                return Json(new {
                    success = true,
                    data = new
                    {
                        hoTen = user.KhachHang.HoTen,
                        email = user.KhachHang.Email,
                        sdt = user.KhachHang.SoDienThoai,
                        cccd = user.KhachHang.Cccd,
                        gioiTinh = user.KhachHang.GioiTinh,
                        ngaySinh = user.KhachHang.NgaySinh?.ToString("dd-MM-yyyy"),
                        diaChi = user.KhachHang.DiaChi,
                        anhDaiDien = !string.IsNullOrEmpty(user.KhachHang.AnhDaiDien) ? user.KhachHang.AnhDaiDien : $"https://ui-avatars.com/api/?name={user.KhachHang.HoTen}&background=feba02&color=003580&size=128",
                        diemTichLuy = user.KhachHang.DiemTichLuy ?? 0,
                        loaiThanhVien = user.KhachHang.LoaiThanhVien ?? "Mới"
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

                var khachHang = await _context.KhachHangs
                    .Include(kh => kh.MaTkNavigation)
                    .FirstOrDefaultAsync(kh => kh.MaTk == userId);
                
                if (khachHang == null) return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });

                if (!string.IsNullOrEmpty(request.Email))
                {
                    string newEmail = request.Email.Trim().ToLower();
                    string oldEmail = (khachHang.Email ?? "").Trim().ToLower();

                    if (newEmail != oldEmail)
                    {
                        var existingEmail = await _context.KhachHangs.AnyAsync(kh => kh.Email.ToLower() == newEmail && kh.MaTk != userId);
                        if (existingEmail) return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác" });
                    }
                    khachHang.Email = request.Email.Trim();
                }

                if (!string.IsNullOrEmpty(request.Sdt))
                {
                    string newSdt = request.Sdt.Trim();
                    string oldSdt = (khachHang.SoDienThoai ?? "").Trim();

                    if (newSdt != oldSdt)
                    {
                        var existingPhone = await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == newSdt && kh.MaTk != userId);
                        if (existingPhone) return Json(new { success = false, message = "Số điện thoại này đã được sử dụng bởi tài khoản khác" });
                    }
                    khachHang.SoDienThoai = newSdt;
                }

                khachHang.HoTen = request.HoTen;
                khachHang.GioiTinh = request.GioiTinh;
                khachHang.NgaySinh = !string.IsNullOrEmpty(request.NgaySinh) ? DateOnly.Parse(request.NgaySinh) : null;
                khachHang.Cccd = request.Cccd;
                khachHang.DiaChi = request.DiaChi;

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

                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.MaTk == userId);
                if (khachHang == null) return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });

                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatar.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                khachHang.AnhDaiDien = "/uploads/avatars/" + fileName;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", url = khachHang.AnhDaiDien });
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