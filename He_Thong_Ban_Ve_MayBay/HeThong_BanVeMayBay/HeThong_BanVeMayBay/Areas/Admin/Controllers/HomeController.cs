using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using Microsoft.EntityFrameworkCore;

using HeThong_BanVeMayBay.Attributes;

namespace HeThong_BanVeMayBay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize("DASHBOARD")]
    public class HomeController : Controller
    {
        private readonly QlBanVeMayBayContext _context;

        public HomeController(QlBanVeMayBayContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalRevenue = await _context.HoaDons
                .Where(h => h.TrangThaiThanhToan == "Đã thanh toán" && (h.DaXoa == false || h.DaXoa == null))
                .SumAsync(h => h.TongTien ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            var now = DateTime.Now;
            var currentMonthRevenue = await _context.HoaDons
                .Where(h => h.NgayLap.Value.Month == now.Month && h.NgayLap.Value.Year == now.Year && h.TrangThaiThanhToan == "Đã thanh toán")
                .SumAsync(h => h.TongTien ?? 0);
            
            var lastMonthDate = now.AddMonths(-1);
            var lastMonthRevenue = await _context.HoaDons
                .Where(h => h.NgayLap.Value.Month == lastMonthDate.Month && h.NgayLap.Value.Year == lastMonthDate.Year && h.TrangThaiThanhToan == "Đã thanh toán")
                .SumAsync(h => h.TongTien ?? 0);

            double growth = 0;
            if (lastMonthRevenue > 0)
            {
                growth = (double)((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
            }
            else if (currentMonthRevenue > 0)
            {
                growth = 100;
            }
            ViewBag.RevenueGrowth = growth;

            // 3. Vé đã bán
            ViewBag.TotalTickets = await _context.Ves.CountAsync(v => v.DaXoa == false || v.DaXoa == null);

            // 4. Tổng chuyến bay
            ViewBag.TotalFlights = await _context.ChuyenBays.CountAsync(c => c.DaXoa == false || c.DaXoa == null);

            // 5. Tổng số sân bay
            ViewBag.TotalAirports = await _context.SanBays.CountAsync(s => s.DaXoa == false || s.DaXoa == null);

            // 6. Các chuyến bay gần đây
            ViewBag.RecentFlights = await _context.ChuyenBays
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .Include(c => c.MaLoaiMbNavigation)
                .Where(c => c.DaXoa == false || c.DaXoa == null)
                .OrderByDescending(c => c.NgayGioBay)
                .Take(5)
                .ToListAsync();

            // 7. Dữ liệu biểu đồ (6 tháng gần nhất)
            var chartData = new List<decimal>();
            var chartLabels = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var revenue = await _context.HoaDons
                    .Where(h => h.NgayLap.Value.Month == month.Month && h.NgayLap.Value.Year == month.Year && h.TrangThaiThanhToan == "Đã thanh toán")
                    .SumAsync(h => h.TongTien ?? 0);
                chartData.Add(revenue);
                chartLabels.Add($"T{month.Month}/{month.Year}");
            }
            ViewBag.ChartData = chartData;
            ViewBag.ChartLabels = chartLabels;

            // 8. Tình trạng đội bay (Đếm thực tế từ bảng ChuyenBay hoặc LoaiMayBay)
            ViewBag.FleetStatus = await _context.LoaiMayBays
                .Where(m => m.DaXoa == false || m.DaXoa == null)
                .Select(m => new { 
                    Ten = m.TenLoaiMb, 
                    Tong = 5, // Số lượng máy bay giả định hoặc đếm từ bảng Máy Bay nếu có
                    DangBay = _context.ChuyenBays.Count(c => c.MaLoaiMb == m.MaLoaiMb && c.TrangThai == "Khởi hành")
                })
                .Take(3)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(int year)
        {
            var data = new List<decimal>();
            for (int month = 1; month <= 12; month++)
            {
                var revenue = await _context.HoaDons
                    .Where(h => h.NgayLap.Value.Year == year && h.NgayLap.Value.Month == month && h.TrangThaiThanhToan == "Đã thanh toán" && (h.DaXoa == false || h.DaXoa == null))
                    .SumAsync(h => h.TongTien ?? 0);
                data.Add(revenue);
            }
            return Json(new { success = true, data = data });
        }
    }
}