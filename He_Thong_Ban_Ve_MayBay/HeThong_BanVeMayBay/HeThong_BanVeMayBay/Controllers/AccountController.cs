using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using HeThong_BanVeMayBay.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HeThong_BanVeMayBay.Controllers
{
    public class AccountController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public AccountController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(u => u.TenDangNhap == username && u.MatKhau == password && u.DaXoa != true);

            if (user != null)
            {
                // Lưu session chung
                HttpContext.Session.SetString("UserEmail", user.TenDangNhap);
                HttpContext.Session.SetString("UserRole", user.LoaiTaiKhoan ?? "User");
                HttpContext.Session.SetInt32("UserId", user.MaTk);

                // Kiểm tra nếu là Admin hoặc Nhân viên
                if (user.NhanVien != null || user.LoaiTaiKhoan == "Admin" || user.LoaiTaiKhoan == "Staff" || user.LoaiTaiKhoan == "Nhân viên")
                {
                    HttpContext.Session.SetString("UserName", user.NhanVien?.HoTen ?? user.TenDangNhap);
                    HttpContext.Session.SetString("UserAvatar", user.NhanVien?.AnhDaiDien ?? "");
                    
                    
                    string permissions = user.LoaiTaiKhoan == "Admin" ? "ALL" : (user.NhanVien?.Quyen ?? "");
                    HttpContext.Session.SetString("UserPermissions", permissions);
                    
                    return Json(new { success = true, redirectUrl = "/Admin/Home/Index" });
                }
                else // Khách hàng
                {
                    HttpContext.Session.SetString("UserName", user.KhachHang?.HoTen ?? user.TenDangNhap);
                    HttpContext.Session.SetString("UserAvatar", user.KhachHang?.AnhDaiDien ?? "");
                    return Json(new { success = true, redirectUrl = "/User/Home/Index" });
                }
            }

            return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string username, string password, string email, string phone, string cccd)
        {
            try
            {
                // Ràng buộc dữ liệu thực tế
                if (string.IsNullOrEmpty(fullName) || fullName.Trim().Split(' ').Length < 2)
                    return Json(new { success = false, message = "Họ tên phải bao gồm ít nhất 2 từ." });

                if (string.IsNullOrEmpty(email) || !email.EndsWith("@gmail.com"))
                    return Json(new { success = false, message = "Email phải có định dạng @gmail.com (VD: abc@gmail.com)." });

                if (string.IsNullOrEmpty(phone) || phone.Length != 10 || !phone.StartsWith("0") || !phone.All(char.IsDigit))
                    return Json(new { success = false, message = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0." });

                if (string.IsNullOrEmpty(cccd) || (cccd.Length != 12 && cccd.Length != 9) || !cccd.All(char.IsDigit))
                    return Json(new { success = false, message = "CCCD/Passport phải gồm 9 hoặc 12 chữ số." });

                if (string.IsNullOrEmpty(username) || username.Length < 5)
                    return Json(new { success = false, message = "Tên đăng nhập phải có ít nhất 5 ký tự." });

                if (string.IsNullOrEmpty(password) || password.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

                if (await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username))
                {
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });
                }

                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = username,
                    MatKhau = password,
                    LoaiTaiKhoan = "User",
                    TrangThai = true,
                    DaXoa = false
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                var lastKH = await _context.KhachHangs
                    .Where(k => k.MaKh.StartsWith("KH") && k.MaKh.Length == 5)
                    .OrderByDescending(k => k.MaKh)
                    .FirstOrDefaultAsync();
                
                int nextId = 1;
                if (lastKH != null)
                {
                    if (int.TryParse(lastKH.MaKh.Substring(2), out int currentId))
                    {
                        nextId = currentId + 1;
                    }
                }
                string newMaKh = "KH" + nextId.ToString("D3");

                var khachHang = new KhachHang
                {
                    MaKh = newMaKh,
                    MaTk = taiKhoan.MaTk,
                    HoTen = fullName,
                    Email = email,
                    SoDienThoai = phone,
                    Cccd = cccd,
                    LoaiThanhVien = "Mới",
                    DiemTichLuy = 0,
                    DaXoa = false
                };

                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account", new { area = "" });
        }
    }
}
