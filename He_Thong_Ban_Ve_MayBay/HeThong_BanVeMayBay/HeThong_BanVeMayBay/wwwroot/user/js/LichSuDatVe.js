let bookingData = [];

document.addEventListener('DOMContentLoaded', function() {
    loadLichSu();

    document.getElementById('btnFilter').addEventListener('click', function() {
        loadLichSu();
    });

    const btnDownload = document.getElementById('btnDownloadPdf');
    if (btnDownload) {
        btnDownload.addEventListener('click', function() {
            const maVe = document.getElementById('modal-pnr').innerText;
            if (maVe) {
                window.open(`/User/Booking/XuatHoaDonByMaVe?maVe=${maVe}`, '_blank');
            }
        });
    }
});

async function loadLichSu() {
    const pnr = document.getElementById('txtPnr').value;
    const status = document.getElementById('selStatus').value;
    const date = document.getElementById('txtDate').value;

    const container = document.getElementById('ticketList');
    if (!container) return;

    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2 text-muted">Đang tìm kiếm...</p>
        </div>
    `;

    try {
        const response = await fetch(`/User/Booking/GetLichSuDatVeAPI?pnr=${pnr}&status=${status}&date=${date}`);
        const result = await response.json();

        if (result.success) {
            bookingData = result.data;
            renderTickets(bookingData);
        } else {
            container.innerHTML = `<div class="alert alert-info text-center">${result.message}</div>`;
        }
    } catch (error) {
        console.error('Error loading history:', error);
        container.innerHTML = `<div class="alert alert-danger text-center">Lỗi khi tải dữ liệu. Vui lòng thử lại sau.</div>`;
    }
}

function getAirlineLogo(airlineName) {
    let url = '';
    if (airlineName.includes('Vietnam Airlines')) url = 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/ad/Vietnam_Airlines_logo_2015.svg/1200px-Vietnam_Airlines_logo_2015.svg.png';
    else if (airlineName.includes('VietJet Air')) url = 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/VietJet_Air_logo.svg/2560px-VietJet_Air_logo.svg.png';
    else if (airlineName.includes('Bamboo Airways')) url = 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f5/Bamboo_Airways_logo.svg/2560px-Bamboo_Airways_logo.svg.png';
    else if (airlineName.includes('Vietravel Airlines')) url = 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a2/Vietravel_Airlines_logo.svg/2560px-Vietravel_Airlines_logo.svg.png';
    else url = 'https://via.placeholder.com/100x40?text=' + encodeURIComponent(airlineName);

    return `<div class="airline-logo-container mb-2">
                <img src="${url}" class="img-fluid rounded" style="max-height: 40px; width: auto; object-fit: contain;" 
                    alt="${airlineName}" 
                    onerror="this.style.display='none'; this.nextElementSibling.style.display='block';">
                <div class="airline-icon-fallback" style="display:none;">
                    <i class="bi bi-airplane-engines-fill fs-2 text-primary"></i>
                </div>
            </div>`;
}

function renderTickets(tickets) {
    const container = document.getElementById('ticketList');
    if (tickets.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5 bg-light rounded-4">
                <i class="bi bi-ticket-perforated text-muted display-1"></i>
                <p class="mt-3 text-muted">Bạn chưa có giao dịch nào phù hợp.</p>
            </div>
        `;
        return;
    }

    container.innerHTML = tickets.map(ticket => {
        const statusClass = ticket.trangThai === 'Đã thanh toán' ? 'status-paid' : 
                           (ticket.trangThai === 'Đã hủy' ? 'status-cancelled' : 'status-pending');
        const badgeClass = ticket.trangThai === 'Đã thanh toán' ? 'bg-success-subtle text-success' : 
                          (ticket.trangThai === 'Đã hủy' ? 'bg-danger-subtle text-danger' : 'bg-warning-subtle text-warning');

        return `
            <div class="card ticket-card ${statusClass} mb-3">
                <div class="card-body p-4">
                    <div class="row align-items-center">
                        <div class="col-lg-2 border-end text-center">
                            ${getAirlineLogo(ticket.hangBay)}
                            <div class="small fw-bold text-muted mb-2">${ticket.hangBay}</div>
                            <div class="pnr-box">
                                <span class="pnr-label">MÃ ĐẶT CHỖ</span>
                                <span class="pnr-value">${ticket.maVe}</span>
                            </div>
                        </div>
                        <div class="col-lg-5 px-lg-5 py-3 py-lg-0">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <div class="text-center">
                                    <div class="airport-code">${ticket.maSbDi}</div>
                                    <div class="airport-name text-truncate" style="max-width: 100px;">${ticket.tenSbDi}</div>
                                </div>
                                <div class="text-center flex-grow-1 px-3">
                                    <div class="small text-muted mb-1">${ticket.maCb} • ${ticket.hangGhe}</div>
                                    <div class="position-relative">
                                        <hr class="my-2">
                                        <i class="bi bi-airplane-fill position-absolute top-50 start-50 translate-middle bg-white px-2 text-primary"></i>
                                    </div>
                                    <div class="small text-muted mt-1">${ticket.thoiGianBay}</div>
                                </div>
                                <div class="text-center">
                                    <div class="airport-code">${ticket.maSbDen}</div>
                                    <div class="airport-name text-truncate" style="max-width: 100px;">${ticket.tenSbDen}</div>
                                </div>
                            </div>
                            <div class="d-flex gap-3 mt-3">
                                <span class="small text-muted"><i class="bi bi-calendar3 me-1"></i> ${ticket.ngayBay}</span>
                                <span class="small text-muted"><i class="bi bi-clock me-1"></i> ${ticket.gioBay}</span>
                            </div>
                        </div>
                        <div class="col-lg-2 text-center text-lg-start border-start px-lg-4">
                            <div class="small text-muted mb-1">Tổng cộng</div>
                            <div class="price-tag mb-2">${ticket.tongTien.toLocaleString('vi-VN')}đ</div>
                            <span class="badge ${badgeClass} px-3 py-2 rounded-pill small">${ticket.trangThai}</span>
                        </div>
                        <div class="col-lg-3 text-center text-lg-end mt-3 mt-lg-0">
                            <button class="btn btn-outline-primary btn-detail w-100 mb-2" onclick="viewDetail('${ticket.maVe}')">Xem chi tiết</button>
                            <a href="/User/Booking/XuatHoaDonByMaVe?maVe=${ticket.maVe}" target="_blank" class="btn btn-primary btn-detail w-100 mb-2"><i class="bi bi-cloud-download me-2"></i>Tải vé điện tử</a>
                            ${ticket.trangThai !== 'Đã hủy' ? 
                                `<button class="btn btn-outline-danger btn-detail w-100" onclick="confirmHuyVe('${ticket.maVe}')"><i class="bi bi-x-circle me-2"></i>Hủy vé</button>` : 
                                ''
                            }
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function viewDetail(maVe) {
    const ticket = bookingData.find(t => t.maVe === maVe);
    if (!ticket) return;

    const modal = new bootstrap.Modal(document.getElementById('ticketDetailModal'));
    const modalBody = document.querySelector('#ticketDetailModal .modal-body');
    
    // Điền thông tin Chuyến bay
    document.getElementById('modal-flight-code').innerText = ticket.maCb + ' - ' + ticket.hangBay;
    document.getElementById('modal-route').innerHTML = `${ticket.tenSbDi} (${ticket.maSbDi}) <i class="bi bi-arrow-right mx-1"></i> ${ticket.tenSbDen} (${ticket.maSbDen})`;
    document.getElementById('modal-departure-time').innerText = ticket.ngayBay + ' ' + ticket.gioBay;
    document.getElementById('modal-arrival-time').innerText = ticket.ngayDen + ' ' + ticket.gioDen;
    document.getElementById('modal-duration').innerText = ticket.thoiGianBay;

    // Điền thông tin Hành khách
    document.getElementById('modal-passenger-name').innerText = ticket.tenHanhKhach;
    document.getElementById('modal-passenger-cccd').innerText = ticket.cccd || 'N/A';
    document.getElementById('modal-passenger-phone').innerText = ticket.sdt || 'N/A';
    document.getElementById('modal-seat-class').innerText = ticket.hangGhe;
    document.getElementById('modal-seat-number').innerText = ticket.soGhe;
    document.getElementById('modal-booking-date').innerText = ticket.ngayDat || ticket.NgayDat || 'Không xác định';

    // Dịch vụ & Tiện ích
    document.getElementById('modal-baggage-info').innerHTML = `
        <div class="mb-1"><i class="bi bi-check2-circle text-success me-2"></i>Hành lý xách tay: <b>${ticket.xachTay}</b></div>
        <div><i class="bi bi-check2-circle text-success me-2"></i>Hành lý kí gửi: <b>${ticket.kyGui}</b></div>
    `;
    
    const amenitiesContainer = document.getElementById('modal-amenities');
    if (ticket.tienIches && ticket.tienIches.length > 0) {
        amenitiesContainer.innerHTML = ticket.tienIches.map(t => 
            `<div class="mb-1"><i class="bi bi-check2 text-primary me-2"></i>${t}</div>`
        ).join('');
    } else {
        amenitiesContainer.innerHTML = '<div class="text-muted">Theo tiêu chuẩn hạng vé</div>';
    }

    // Thanh toán & Trạng thái
    document.getElementById('modal-total-price').innerText = ticket.tongTien.toLocaleString('vi-VN') + ' VND';
    document.getElementById('modal-status-label').innerText = 'Trạng thái: ' + ticket.trangThai;
    
    const qrImg = document.querySelector('#modal-qr-container img');
    qrImg.src = `https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=${ticket.maVe}`;
    
    // Nút in vé gọi hàm xuất hóa đơn PDF
    document.getElementById('btnPrintTicket').onclick = function() {
        window.location.href = `/User/Booking/XuatHoaDonByMaVe?maVe=${ticket.maVe}`;
    };
    
    modal.show();
}

async function confirmHuyVe(maVe) {
    const result = await Swal.fire({
        title: 'Xác nhận hủy vé',
        text: "Bạn sẽ không thể hoàn tác sau khi hủy vé!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Đã hiểu, Hủy vé',
        cancelButtonText: 'Quay lại'
    });
    
    if (!result.isConfirmed) return;

    try {
        const response = await fetch(`/User/Booking/HuyVeAPI?maVe=${maVe}`, {
            method: 'POST'
        });
        const result = await response.json();

        if (result.success) {
            await Swal.fire('Thành công', result.message, 'success');
            loadLichSu(); // Tải lại danh sách
        } else {
            Swal.fire('Lỗi', result.message, 'error');
        }
    } catch (error) {
        console.error('Error cancelling ticket:', error);
        Swal.fire('Lỗi', 'Lỗi kết nối máy chủ', 'error');
    }
}
