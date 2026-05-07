using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HeThong_BanVeMayBay.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public HomeController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? diemDi, string? diemDen, DateTime? ngayDi, string? hangVe)
        {
            ViewBag.SanBays = await _context.SanBays.Where(s => s.DaXoa != true).ToListAsync();
            ViewBag.HangGhes = await _context.DanhMucHangGhes.Where(h => h.DaXoa != true).ToListAsync();
            
            var query = _context.ChuyenBays
                .Include(c => c.MaHangNavigation)
                .Include(c => c.MaLoaiMbNavigation)
                .Include(c => c.MaTuyenNavigation)
                .ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation)
                .ThenInclude(t => t.MaSbdenNavigation)
                .AsQueryable();

            query = query.Where(c => c.DaXoa != true && 
                                    (c.TrangThai == "Sắp bay" || c.TrangThai == "Đúng giờ") && 
                                    c.NgayGioBay >= DateTime.Now);

            if (!string.IsNullOrEmpty(diemDi))
            {
                query = query.Where(c => c.MaTuyenNavigation != null && c.MaTuyenNavigation.MaSbdi == diemDi);
            }

            if (!string.IsNullOrEmpty(diemDen))
            {
                query = query.Where(c => c.MaTuyenNavigation != null && c.MaTuyenNavigation.MaSbden == diemDen);
            }

            if (ngayDi.HasValue)
            {
                query = query.Where(c => c.NgayGioBay.Date == ngayDi.Value.Date);
            }

            if (!string.IsNullOrEmpty(hangVe))
            {
                // Gi? ??nh ChuyenBay c� MaHang (Ho?c li�n k?t qua c�c kh�a kh�c, n?u mu?n l?c ??n gi?n th� c� th? b? qua n?u c.MaHang kh�ng ?�ng m?c ?�ch)
                // Ph? thu?c v�o ki?n tr�c, ?? t?m l?c theo MaHang
            }

            var activeFlights = await query
                .OrderBy(c => c.NgayGioBay)
                .Take(20)
                .ToListAsync();
                
            ViewBag.ChuyenBays = activeFlights;

            ViewBag.DiemDi = diemDi;
            ViewBag.DiemDen = diemDen;
            ViewBag.NgayDi = ngayDi?.ToString("yyyy-MM-dd");
            ViewBag.HangVe = hangVe;
            
            return View();
        }
    }
}