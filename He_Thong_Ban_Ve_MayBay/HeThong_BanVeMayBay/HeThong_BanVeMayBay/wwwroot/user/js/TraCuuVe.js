function executeTicketLookup() {
    const btnSearch = document.getElementById('btnSearch');
    const resultBox = document.getElementById('search-result');
    const resultContainer = document.getElementById('result-container');
    const maVeInput = document.getElementById('lookupMaVe');
    const contactInput = document.getElementById('lookupContact');

    if (!btnSearch || !maVeInput || !contactInput) return;

    const maVe = maVeInput.value.trim();
    const contact = contactInput.value.trim();

    if (!maVe && !contact) {
        Swal.fire('Thông báo', 'Vui lòng nhập ít nhất Mã vé hoặc SĐT/CCCD.', 'warning');
        return;
    }

    const originalHtml = btnSearch.innerHTML;
    btnSearch.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
    btnSearch.disabled = true;
    if (resultBox) resultBox.style.display = 'none';

    const apiUrl = window.lookupConfig ? window.lookupConfig.apiUrl : '/User/Booking/TraCuuVeAPI';
    const url = `${apiUrl}?maVe=${encodeURIComponent(maVe)}&contact=${encodeURIComponent(contact)}`;

    fetch(url)
        .then(response => response.json())
        .then(data => {
            btnSearch.innerHTML = originalHtml;
            btnSearch.disabled = false;

            if (data.success) {
                renderTicketResults(data.data, resultContainer);
                if (resultBox) {
                    resultBox.style.display = 'block';
                    setTimeout(() => {
                        resultBox.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }, 100);
                }
            } else {
                Swal.fire('Không tìm thấy', data.message, 'info');
            }
        })
        .catch(error => {
            btnSearch.innerHTML = originalHtml;
            btnSearch.disabled = false;
            Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ.', 'error');
        });
}

function renderTicketResults(tickets, container) {
    let html = `<h4 class="fw-bold mb-4 text-navy"><i class="bi bi-card-checklist me-2"></i>Kết quả tìm thấy (${tickets.length})</h4>`;

    tickets.forEach((ticket, index) => {
        html += `
            <div class="mb-4 shadow-sm border bg-white" style="border-radius: 15px; border-left: 6px solid #003580 !important; overflow: hidden;">
                <div class="p-4">
                    <div class="d-flex justify-content-between align-items-center border-bottom pb-3 mb-3">
                        <div>
                            <span class="badge bg-primary px-3 py-2 me-2" style="font-size: 0.9rem;">MÃ VÉ: ${ticket.maVe}</span>
                            <span class="badge ${ticket.trangThai === 'Đã thanh toán' ? 'bg-success' : 'bg-warning text-dark'} px-3 py-2">${ticket.trangThai}</span>
                        </div>
                        <div class="fw-bold text-navy fs-5">${ticket.tongTien.toLocaleString('vi-VN')} đ</div>
                    </div>
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Hành khách:</div>
                            <div class="fw-bold text-uppercase">${ticket.tenHanhKhach}</div>
                        </div>
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Chuyến bay:</div>
                            <div class="fw-bold text-primary">${ticket.maCb}</div>
                        </div>
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Lộ trình:</div>
                            <div class="fw-bold">${ticket.tenSbDi} <i class="bi bi-arrow-right mx-1 small"></i> ${ticket.tenSbDen}</div>
                        </div>
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Khởi hành:</div>
                            <div class="fw-bold">${ticket.ngayBay}</div>
                        </div>
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Ngày giao dịch:</div>
                            <div class="fw-bold text-success">${ticket.ngayDat || ticket.NgayDat || 'Không xác định'}</div>
                        </div>
                        <div class="col-md-6 mb-3">
                            <div class="small text-muted mb-1">Vị trí ngồi:</div>
                            <div class="fw-bold text-danger">Ghế ${ticket.soGhe} <span class="text-muted fw-normal">(${ticket.hangGhe})</span></div>
                        </div>
                    </div>
                   
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

// Gắn sự kiện khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', function () {
    const btnSearch = document.getElementById('btnSearch');
    if (btnSearch) {
        btnSearch.onclick = executeTicketLookup;
    }
});
