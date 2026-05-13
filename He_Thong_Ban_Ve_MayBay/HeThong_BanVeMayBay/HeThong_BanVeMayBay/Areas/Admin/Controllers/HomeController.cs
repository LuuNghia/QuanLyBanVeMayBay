using Microsoft.AspNetCore.Mvc;
using HeThong_BanVeMayBay.Models.EF;
using Microsoft.EntityFrameworkCore;

using HeThong_BanVeMayBay.Attributes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int year)
        {
            ExcelPackage.License.SetNonCommercialPersonal("HeThongBanVeMayBay");

            // Danh sách thống kê 12 tháng
            var dtThongKe = new List<dynamic>();
            decimal tongDoanhThu = 0;
            int tongChuyenBay = 0;
            int tongVeBan = 0;

            for (int m = 1; m <= 12; m++)
            {
                var doanhThuThang = await _context.HoaDons
                    .Where(h => h.NgayLap.Value.Year == year && h.NgayLap.Value.Month == m && h.TrangThaiThanhToan == "Đã thanh toán" && (h.DaXoa == false || h.DaXoa == null))
                    .SumAsync(h => h.TongTien ?? 0);

                var soChuyenBayThang = await _context.ChuyenBays
                    .Where(c => c.NgayGioBay.Year == year && c.NgayGioBay.Month == m && (c.DaXoa == false || c.DaXoa == null))
                    .CountAsync();

                var soVeBanThang = await _context.Ves
                    .Where(v => v.MaHoaDonNavigation.NgayLap.Value.Year == year && v.MaHoaDonNavigation.NgayLap.Value.Month == m && v.MaHoaDonNavigation.TrangThaiThanhToan == "Đã thanh toán" && (v.DaXoa == false || v.DaXoa == null))
                    .CountAsync();

                dtThongKe.Add(new {
                    Thang = m,
                    SoChuyenBay = soChuyenBayThang,
                    SoVeBan = soVeBanThang,
                    DoanhThu = doanhThuThang
                });

                tongDoanhThu += doanhThuThang;
                tongChuyenBay += soChuyenBayThang;
                tongVeBan += soVeBanThang;
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add($"DoanhThu_{year}");

            // Tiêu đề
            ws.Cells["A1:E1"].Merge = true;
            ws.Cells["A1"].Value = $"BÁO CÁO DOANH THU NĂM {year}";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["A2:E2"].Merge = true;
            ws.Cells["A2"].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A2"].Style.Font.Italic = true;

            // Header bảng
            var headerRow = 4;
            ws.Cells[$"A{headerRow}"].Value = "STT";
            ws.Cells[$"B{headerRow}"].Value = "Tháng";
            ws.Cells[$"C{headerRow}"].Value = "Số Chuyến Bay";
            ws.Cells[$"D{headerRow}"].Value = "Số Vé Đã Bán";
            ws.Cells[$"E{headerRow}"].Value = "Doanh Thu (VNĐ)";

            var headerRange = ws.Cells[$"A{headerRow}:E{headerRow}"];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Data
            int row = headerRow + 1;
            int stt = 1;
            foreach (var item in dtThongKe)
            {
                ws.Cells[$"A{row}"].Value = stt++;
                ws.Cells[$"A{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                
                ws.Cells[$"B{row}"].Value = $"Tháng {item.Thang}";
                ws.Cells[$"B{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                
                ws.Cells[$"C{row}"].Value = item.SoChuyenBay;
                ws.Cells[$"C{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[$"D{row}"].Value = item.SoVeBan;
                ws.Cells[$"D{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                
                ws.Cells[$"E{row}"].Value = item.DoanhThu;
                ws.Cells[$"E{row}"].Style.Numberformat.Format = "#,##0";
                
                row++;
            }

            // Tổng cộng
            ws.Cells[$"B{row}"].Value = "TỔNG CỘNG:";
            ws.Cells[$"B{row}"].Style.Font.Bold = true;
            ws.Cells[$"B{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[$"C{row}"].Value = tongChuyenBay;
            ws.Cells[$"C{row}"].Style.Font.Bold = true;
            ws.Cells[$"C{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[$"C{row}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

            ws.Cells[$"D{row}"].Value = tongVeBan;
            ws.Cells[$"D{row}"].Style.Font.Bold = true;
            ws.Cells[$"D{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[$"D{row}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            
            ws.Cells[$"E{row}"].Value = tongDoanhThu;
            ws.Cells[$"E{row}"].Style.Font.Bold = true;
            ws.Cells[$"E{row}"].Style.Numberformat.Format = "#,##0";
            ws.Cells[$"E{row}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

            // Căn chỉnh viền
            var dataRange = ws.Cells[$"A{headerRow}:E{row}"];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // AutoFit Columns
            if (ws.Dimension != null) ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_DoanhThu_{year}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            var now = DateTime.Now;
            
            // 1. Tổng quan
            var totalRevenue = await _context.HoaDons
                .Where(h => h.TrangThaiThanhToan == "Đã thanh toán" && (h.DaXoa == false || h.DaXoa == null))
                .SumAsync(h => h.TongTien ?? 0);
            
            var totalTickets = await _context.Ves.CountAsync(v => v.DaXoa == false || v.DaXoa == null);
            var totalFlights = await _context.ChuyenBays.CountAsync(c => c.DaXoa == false || c.DaXoa == null);
            var totalAirports = await _context.SanBays.CountAsync(s => s.DaXoa == false || s.DaXoa == null);

            // 2. Chuyến bay gần đây
            var recentFlights = await _context.ChuyenBays
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .Include(c => c.MaLoaiMbNavigation)
                .Where(c => c.DaXoa == false || c.DaXoa == null)
                .OrderByDescending(c => c.NgayGioBay)
                .Take(8)
                .ToListAsync();

            // 3. Doanh thu 6 tháng gần nhất
            var revenue6Months = new List<dynamic>();
            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var rev = await _context.HoaDons
                    .Where(h => h.NgayLap.Value.Month == month.Month && h.NgayLap.Value.Year == month.Year && h.TrangThaiThanhToan == "Đã thanh toán" && (h.DaXoa == false || h.DaXoa == null))
                    .SumAsync(h => h.TongTien ?? 0);
                revenue6Months.Add(new { Month = $"Tháng {month.Month}/{month.Year}", Revenue = rev });
            }

            // 4. Vé bán gần đây
            var recentTickets = await _context.Ves
                .Include(v => v.MaHoaDonNavigation)
                .Include(v => v.MaCbNavigation).ThenInclude(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdiNavigation)
                .Include(v => v.MaCbNavigation).ThenInclude(c => c.MaTuyenNavigation).ThenInclude(t => t.MaSbdenNavigation)
                .Where(v => v.DaXoa == false || v.DaXoa == null)
                .OrderByDescending(v => v.MaHoaDonNavigation.NgayLap)
                .Take(8)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, totalRevenue, totalTickets, totalFlights, totalAirports, recentFlights, revenue6Months, recentTickets));
                    page.Footer().Element(ComposeFooter);
                });
            });

            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            return File(pdfStream, "application/pdf", $"BaoCao_TongQuan_HeThong_{DateTime.Now:ddMMyyyy_HHmm}.pdf");
        }

        private void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("SKY FLIGHT SYSTEM").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("BÁO CÁO VẬN HÀNH & DOANH THU").FontSize(16).FontColor(Colors.Grey.Darken2);
                    column.Item().PaddingTop(5).Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(100).AlignRight().Column(column => 
                {
                    column.Item().Text("Admin").SemiBold();
                    column.Item().Text("Phòng Quản Trị");
                });
            });
        }

        private void ComposeContent(IContainer container, decimal totalRevenue, int totalTickets, int totalFlights, int totalAirports, dynamic recentFlights, dynamic revenue6Months, dynamic recentTickets)
        {
            container.Column(column =>
            {
                column.Spacing(20);

                // Section 1: Tổng quan (Dạng thẻ)
                column.Item().Text("1. TỔNG QUAN CHUNG").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Row(row =>
                {
                    row.RelativeItem().BorderLeft(4).BorderColor(Colors.Blue.Darken1).Background(Colors.Blue.Lighten5).Padding(15).Column(c => {
                        c.Item().Text("Tổng Doanh Thu").FontSize(10).FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"{totalRevenue:N0} VNĐ").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                    });
                    row.ConstantItem(15);
                    row.RelativeItem().BorderLeft(4).BorderColor(Colors.Teal.Darken1).Background(Colors.Teal.Lighten5).Padding(15).Column(c => {
                        c.Item().Text("Vé Đã Bán").FontSize(10).FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"{totalTickets:N0} Vé").FontSize(14).SemiBold().FontColor(Colors.Teal.Darken2);
                    });
                    row.ConstantItem(15);
                    row.RelativeItem().BorderLeft(4).BorderColor(Colors.Orange.Darken1).Background(Colors.Orange.Lighten5).Padding(15).Column(c => {
                        c.Item().Text("Tổng Chuyến Bay").FontSize(10).FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"{totalFlights:N0} Chuyến").FontSize(14).SemiBold().FontColor(Colors.Orange.Darken2);
                    });
                });

                // Section 2: Doanh thu 6 tháng
                column.Item().Text("2. DOANH THU 6 THÁNG GẦN NHẤT").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tháng").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Doanh Thu (VNĐ)").FontColor(Colors.White).SemiBold();
                    });

                    bool isAlternate = false;
                    foreach (var rm in revenue6Months)
                    {
                        string m = Convert.ToString(rm.Month);
                        string r = Convert.ToDecimal(rm.Revenue).ToString("N0");

                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(m);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r);
                        isAlternate = !isAlternate;
                    }
                });

                // Section 3: Chuyến bay gần đây
                column.Item().Text("3. LỊCH TRÌNH CHUYẾN BAY GẦN ĐÂY").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.RelativeColumn();
                        columns.ConstantColumn(110);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Mã CB").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Hành Trình").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Khởi Hành").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Trạng Thái").FontColor(Colors.White).SemiBold();
                    });

                    bool isAlternate = false;
                    foreach (var flight in recentFlights)
                    {
                        string from = Convert.ToString(flight.MaTuyenNavigation?.MaSbdiNavigation?.TenSb) ?? "N/A";
                        string to = Convert.ToString(flight.MaTuyenNavigation?.MaSbdenNavigation?.TenSb) ?? "N/A";
                        string maCb = Convert.ToString(flight.MaCb) ?? "N/A";
                        string time = Convert.ToString(flight.NgayGioBay?.ToString("HH:mm dd/MM/yyyy")) ?? "N/A";
                        string status = Convert.ToString(flight.TrangThai) ?? "Đang chờ";

                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(maCb).SemiBold();
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{from} -> {to}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(time);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(status).FontColor(status == "Đã hủy" ? Colors.Red.Medium : Colors.Green.Medium);
                        isAlternate = !isAlternate;
                    }
                });

                // Section 4: Vé bán gần đây
                column.Item().Text("4. GIAO DỊCH VÉ MỚI NHẤT").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(60); // Mã Vé
                        columns.RelativeColumn();   // Khách hàng
                        columns.RelativeColumn();   // Chặng bay
                        columns.ConstantColumn(80); // Giá vé
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Mã Vé").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Hành Khách").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Chặng Bay").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Giá (VNĐ)").FontColor(Colors.White).SemiBold();
                    });

                    bool isAlternate = false;
                    foreach (var ve in recentTickets)
                    {
                        string maVe = Convert.ToString(ve.MaVe) ?? "N/A";
                        string hanhKhach = Convert.ToString(ve.TenHanhKhach) ?? "N/A";
                        string from = Convert.ToString(ve.MaCbNavigation?.MaTuyenNavigation?.MaSbdiNavigation?.TenSb) ?? "N/A";
                        string to = Convert.ToString(ve.MaCbNavigation?.MaTuyenNavigation?.MaSbdenNavigation?.TenSb) ?? "N/A";
                        string giaVe = Convert.ToDecimal(ve.GiaVeThucTe ?? 0).ToString("N0");

                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(maVe);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(hanhKhach);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{from} -> {to}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(giaVe).AlignRight();
                        isAlternate = !isAlternate;
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.RelativeItem().Text("Hệ thống Quản lý Bán vé Máy bay - Đồ án Tốt nghiệp").FontSize(9).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Trang ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    x.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }

    }
}