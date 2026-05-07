
const toastEl = document.getElementById('liveToast');
const bsToast = new bootstrap.Toast(toastEl, { delay: 3000 });
let processingId = 0;
let currentGiaVe = 0; // Giá của hạng ghế đang chọn
let selectedSeats = []; // [{maGhe, soGhe}]

function showToast(msg, colorClass = 'bg-success') {
    document.getElementById('toastMessage').innerText = msg;
    toastEl.className = 'toast align-items-center text-white border-0 shadow-lg rounded-pill px-4 py-3 ' + colorClass;
    bsToast.show();
}

// ─────────────────────────────────────────
// VALIDATION HELPERS
// ─────────────────────────────────────────
function setFieldError(inputId, errorId, show) {
    const input = document.getElementById(inputId);
    const errEl = document.getElementById(errorId);
    if (show) {
        input.classList.add('is-invalid-custom');
        errEl.classList.add('show');
    } else {
        input.classList.remove('is-invalid-custom');
        errEl.classList.remove('show');
    }
    return !show;
}

function validateForm() {
    let valid = true;
    const tenKh = document.getElementById('create-tenKh').value.trim();
    const cccd = document.getElementById('create-cccd').value.trim();
    const sdt = document.getElementById('create-sdt').value.trim();
    const email = document.getElementById('create-email').value.trim();

    valid &= setFieldError('create-tenKh', 'err-tenKh', tenKh === '');
    valid &= setFieldError('create-cccd', 'err-cccd', !/^\d{9}$|^\d{12}$/.test(cccd));
    valid &= setFieldError('create-sdt', 'err-sdt', !/^0\d{9}$/.test(sdt));
    valid &= setFieldError('create-email', 'err-email', email !== '' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email));
    valid &= setFieldError('create-maGhe', 'err-ghe', selectedSeats.length === 0);

    return !!valid;
}

// Xóa lỗi khi người dùng gõ vào
['create-tenKh', 'create-cccd', 'create-sdt', 'create-email'].forEach(id => {
    document.addEventListener('DOMContentLoaded', () => {
        const el = document.getElementById(id);
        if (el) el.addEventListener('input', () => {
            el.classList.remove('is-invalid-custom');
            const errMap = {
                'create-tenKh': 'err-tenKh',
                'create-cccd': 'err-cccd',
                'create-sdt': 'err-sdt',
                'create-email': 'err-email'
            };
            document.getElementById(errMap[id])?.classList.remove('show');
        });
    });
});

// ─────────────────────────────────────────
// LOAD HANG GHE KHI MO MODAL
// ─────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    const btnOpen = document.querySelector('[data-bs-target="#modalCreateBooking"]');
    if (btnOpen) {
        btnOpen.addEventListener('click', () => {
            resetCreateForm();
            const maCb = document.getElementById('create-maCb')?.value;
            loadHangGhe(maCb);
            setTimeout(updateFlightPreview, 50);
        });
    }

    // Status filter buttons
    document.querySelectorAll('.status-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.status-btn').forEach(b => {
                b.classList.remove('btn-dark');
                b.classList.add('btn-outline-secondary');
            });
            this.classList.add('btn-dark');
            this.classList.remove('btn-outline-secondary');
            document.getElementById('filterStatus').value = this.dataset.status;
            document.getElementById('filterForm').submit();
        });
    });
});

function resetCreateForm() {
    ['create-tenKh', 'create-cccd', 'create-sdt', 'create-email'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
    selectedSeats = [];
    document.getElementById('create-maGhe').value = '';
    document.getElementById('selectedSeatInfo').classList.remove('show');
    document.getElementById('priceDisplay').innerText = '-- Đ';
    document.getElementById('seatMapContainer').innerHTML = `
        <div class="text-center text-muted py-5">
            <i class="bi bi-grid-3x3-gap fs-1 d-block mb-2 opacity-25"></i>
            <span class="small">Chọn chuyến bay và hạng ghế để xem sơ đồ</span>
        </div>`;
    ['err-tenKh', 'err-cccd', 'err-sdt', 'err-email', 'err-ghe'].forEach(id => {
        document.getElementById(id)?.classList.remove('show');
    });
    ['create-tenKh', 'create-cccd', 'create-sdt', 'create-email'].forEach(id => {
        document.getElementById(id)?.classList.remove('is-invalid-custom');
    });
    currentGiaVe = 0;
}

function loadHangGhe(maCb) {
    const url = maCb
        ? `/Admin/Booking/GetHangGheByChuyenBay?maCb=${encodeURIComponent(maCb)}`
        : '/Admin/Booking/GetHangGhe';

    const sel = document.getElementById('create-hangGhe');
    sel.innerHTML = '<option value="">-- Đang tải... --</option>';

    fetch(url)
        .then(r => r.json())
        .then(data => {
            sel.innerHTML = '';
            if (!data || data.length === 0) {
                sel.innerHTML = '<option value="">-- Không có hạng ghế --</option>';
                return;
            }
            data.forEach(hg => {
                const opt = document.createElement('option');
                opt.value = hg.maHangGhe;
                opt.text = hg.tenHangGhe;
                sel.appendChild(opt);
            });
            onHangGheChange();
        })
        .catch(() => {
            sel.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
        });
}

function onChuyenBayChange() {
    selectedSeats = [];
    document.getElementById('create-maGhe').value = '';
    document.getElementById('selectedSeatInfo').classList.remove('show');
    updateFlightPreview();
    // Load lại hạng ghế theo chuyến bay mới chọn
    const maCb = document.getElementById('create-maCb')?.value;
    loadHangGhe(maCb);
    // loadSoDo() sẽ được gọi bởi onHangGheChange() sau khi hạng ghế được set
}

// Cập nhật card preview thông tin chuyến bay
function updateFlightPreview() {
    const sel = document.getElementById('create-maCb');
    if (!sel || !sel.options[sel.selectedIndex]) return;
    const opt = sel.options[sel.selectedIndex];

    const set = (id, val) => { const el = document.getElementById(id); if (el) el.innerText = val || ''; };
    set('fp-sbDi', opt.dataset.sbdi || '---');
    set('fp-sbDen', opt.dataset.sden || '---');
    set('fp-tpDi', opt.dataset.tpdi || '');
    set('fp-tpDen', opt.dataset.tpden || '');
    set('fp-tenDi', opt.dataset.tendi || '');
    set('fp-tenDen', opt.dataset.tenden || '');
    set('fp-gioKh', opt.dataset.giokh || '--:--');
    set('fp-ngayKh', opt.dataset.ngaykh || '');
    set('fp-hang', opt.dataset.hang || '');
    set('fp-tenmb', opt.dataset.tenmb || '');
    set('fp-tgian', opt.dataset.tgian ? 'Bay: ' + opt.dataset.tgian : '');
}

function onHangGheChange() {
    selectedSeats = [];
    document.getElementById('create-maGhe').value = '';
    document.getElementById('selectedSeatInfo').classList.remove('show');
    loadSoDo();
}

// ─────────────────────────────────────────
// LOAD SO DO GHE
// ─────────────────────────────────────────
function loadSoDo() {
    const maCb = document.getElementById('create-maCb')?.value;
    const maHangGhe = document.getElementById('create-hangGhe')?.value;
    if (!maCb || !maHangGhe) return;

    const container = document.getElementById('seatMapContainer');
    container.innerHTML = `<div class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Đang tải sơ đồ...</div>`;

    fetch(`/Admin/Booking/GetGheByHangVaChuyen?maCb=${encodeURIComponent(maCb)}&maHangGhe=${encodeURIComponent(maHangGhe)}`)
        .then(r => r.json())
        .then(data => {
            if (!data.success) {
                container.innerHTML = `<div class="text-center text-danger py-3"><i class="bi bi-exclamation-circle me-2"></i>${data.message}</div>`;
                return;
            }
            currentGiaVe = data.giaVe;
            document.getElementById('priceDisplay').innerText = Number(data.giaVe).toLocaleString('vi-VN') + ' d';
            renderSeatMap(data.ghes, container);
        })
        .catch(() => {
            container.innerHTML = `<div class="text-center text-danger py-3">Lỗi tải sơ đồ ghế.</div>`;
        });
}

function renderSeatMap(ghes, container) {
    if (!ghes || ghes.length === 0) {
        container.innerHTML = `<div class="text-center text-muted py-5">Không có ghế trong hạng này.</div>`;
        return;
    }

    // Nhóm theo hàng và tìm danh sách cột
    const rows = {};
    const colSet = new Set();
    ghes.forEach(g => {
        const r = g.viTriHang ?? 0;
        if (!rows[r]) rows[r] = {};
        rows[r][g.viTriCot ?? 1] = g;
        colSet.add(g.viTriCot ?? 1);
    });

    const sortedCols = [...colSet].sort((a, b) => a - b);
    const maxCols = sortedCols.length;

    // Cột nào là lối đi? Chia đôi nếu >= 6 cột (chia sau cột thứ 3)
    // Nếu 4 cột: chia giữa (sau cột 2); nếu 3 cột: không chia
    let aisleAfterIdx = -1;
    if (maxCols >= 6) aisleAfterIdx = 2;      // A B C | D E F
    else if (maxCols === 4) aisleAfterIdx = 1; // A B | C D
    else if (maxCols === 5) aisleAfterIdx = 1; // A B | C D E

    // Ten cot: lay tu soGhế (vi du 12A => lay 'A'), fallback la A B C D E F...
    const colLabels = {};
    sortedCols.forEach(colNum => {
        // Tìm bất kỳ ghế nào trong cột này để lấy chữ cột
        let label = String.fromCharCode(64 + colNum); // A=1, B=2...
        for (const rKey of Object.keys(rows)) {
            const g = rows[rKey][colNum];
            if (g && g.soGhe) {
                const match = g.soGhe.match(/([A-Z]+)$/);
                if (match) { label = match[1]; break; }
            }
        }
        colLabels[colNum] = label;
    });

    // Render header cột
    let colHeaderHtml = `<div class="seat-row mb-1">`;
    colHeaderHtml += `<div class="seat-row-label"></div>`; // placeholder cho nhãn hàng
    sortedCols.forEach((colNum, idx) => {
        if (idx === aisleAfterIdx + 1) colHeaderHtml += `<div class="seat-aisle"></div>`;
        colHeaderHtml += `<div class="seat-col-header">${colLabels[colNum]}</div>`;
    });
    colHeaderHtml += `</div>`;

    // Render từng hàng
    let rowsHtml = '';
    Object.keys(rows).sort((a, b) => +a - +b).forEach(rowKey => {
        const rowData = rows[rowKey];
        rowsHtml += `<div class="seat-row">`;
        rowsHtml += `<div class="seat-row-label">${rowKey}</div>`;

        sortedCols.forEach((colNum, idx) => {
            if (idx === aisleAfterIdx + 1) rowsHtml += `<div class="seat-aisle"></div>`;
            const g = rowData[colNum];
            if (!g) {
                rowsHtml += `<div class="seat" style="visibility:hidden"></div>`;
            } else {
                const cls = g.daDat ? 'seat-booked' : 'seat-available';
                const clickFn = g.daDat ? '' : `onclick="selectGhe(${g.maGhe}, '${g.soGhe}', ${currentGiaVe})"`;
                // Chỉ hiện chữ cột trong ô, tooltip hiện đầy đủ mã ghế
                rowsHtml += `<div class="seat ${cls}" id="seat-${g.maGhe}" ${clickFn} title="${g.soGhe}${g.daDat ? ' (Đã đặt)' : ''}">${colLabels[colNum]}</div>`;
            }
        });

        rowsHtml += `</div>`;
    });

    const legend = `
        <div class="seat-legend">
            <div class="d-flex align-items-center gap-1"><div class="legend-dot" style="background:#d1fae5;border:2px solid #6ee7b7"></div><small>Trống</small></div>
            <div class="d-flex align-items-center gap-1"><div class="legend-dot" style="background:#fee2e2;border:2px solid #fca5a5"></div><small>Đã đặt</small></div>
            <div class="d-flex align-items-center gap-1"><div class="legend-dot" style="background:#1a2b5a;border:2px solid #f0c040"></div><small>Đang chọn</small></div>
        </div>
        <div class="text-center mb-2">
            <div class="d-inline-block bg-white border rounded-3 px-4 py-1 small fw-bold text-muted" style="font-size:10px;letter-spacing:1px">MŨI MÁY BAY ▲</div>
        </div>`;

    container.innerHTML = legend + colHeaderHtml + rowsHtml;
}

function selectGhe(maGhe, soGhe, giaVe) {
    const index = selectedSeats.findIndex(s => s.maGhe === maGhe);
    const seatEl = document.getElementById('seat-' + maGhe);

    if (index > -1) {
        // Deselect
        selectedSeats.splice(index, 1);
        if (seatEl) {
            seatEl.classList.remove('seat-selected');
            seatEl.classList.add('seat-available');
        }
    } else {
        // Select
        selectedSeats.push({ maGhe, soGhe });
        if (seatEl) {
            seatEl.classList.remove('seat-available');
            seatEl.classList.add('seat-selected');
        }
    }

    updateSelectedSeatsUI();
}

function updateSelectedSeatsUI() {
    const infoEl = document.getElementById('selectedSeatInfo');
    const textEl = document.getElementById('selectedSeatText');
    const priceEl = document.getElementById('priceDisplay');

    if (selectedSeats.length === 0) {
        infoEl.classList.remove('show');
        priceEl.innerText = '-- Đ';
        document.getElementById('create-maGhe').value = '';
        return;
    }

    const labels = selectedSeats.map(s => s.soGhe).join(', ');
    const total = selectedSeats.length * currentGiaVe;

    textEl.innerText = 'Ghế: ' + labels + ' (' + selectedSeats.length + ')';
    priceEl.innerText = total.toLocaleString('vi-VN') + ' Đ';
    infoEl.classList.add('show');

    // Lưu tạm 1 cái maGhe vào input ẩn nếu cần validation cũ, 
    // nhưng tốt nhất là validation dùng selectedSeats.length
    document.getElementById('create-maGhe').value = selectedSeats[0].maGhe;
    document.getElementById('err-ghe').classList.remove('show');
}

// ─────────────────────────────────────────
// TAO DAT VE
// ─────────────────────────────────────────
function submitBooking(mode = 'save') {
    if (!validateForm()) return;

    // Hiện spinner trên đúng nút được click
    const btnId = mode === 'export' ? 'btnXuatVe' : 'btnLuu';
    const btnEl = document.getElementById(btnId);
    const origHtml = btnEl ? btnEl.innerHTML : '';
    if (btnEl) { btnEl.disabled = true; btnEl.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...'; }

    const payload = {
        maCb: document.getElementById('create-maCb').value,
        tenHanhKhach: document.getElementById('create-tenKh').value.trim().toUpperCase(),
        cccd: document.getElementById('create-cccd').value.trim(),
        sdt: document.getElementById('create-sdt').value.trim(),
        email: document.getElementById('create-email').value.trim(),
        maGhes: selectedSeats.map(s => s.maGhe),
        trangThaiThanhToan: document.querySelector('input[name="trangThaiThanhToan"]:checked').value,
        phuongThucThanhToan: document.getElementById('create-phuongThuc')?.value ?? ''
    };

    fetch('/Admin/Booking/TaoDatVe', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                if (mode === 'export' && data.maVe) {
                    window.open(`/Admin/Booking/XuatHoaDonPdf?maVe=${data.maVe}`, '_blank');
                }

                showToast(mode === 'export' ? 'Tạo vé thành công! Đang xuất PDF...' : 'Lưu đặt chỗ thành công!', 'bg-success');

                const modalEl = document.getElementById('modalCreateBooking');
                const modal = bootstrap.Modal.getInstance(modalEl);

                // Sau khi modal đóng hoàn toàn thì reload để hiện vé mới
                modalEl.addEventListener('hidden.bs.modal', () => {
                    location.reload();
                }, { once: true });

                modal.hide();
            } else {
                showToast(data.message, 'bg-danger');
                if (btnEl) { btnEl.disabled = false; btnEl.innerHTML = origHtml; }
            }
        })
        .catch(() => {
            showToast('Lỗi khi tạo đặt chỗ!', 'bg-danger');
            if (btnEl) { btnEl.disabled = false; btnEl.innerHTML = origHtml; }
        });
}



// ─────────────────────────────────────────
// XEM CHI TIET VE
// ─────────────────────────────────────────
function viewDetail(id) {
    fetch(`/Admin/Booking/ChiTietVe?id=${id}`)
        .then(r => r.json())
        .then(data => {
            if (!data.success) { showToast(data.message, 'bg-danger'); return; }

            document.getElementById('detail-pnr').innerText = data.maVe;
            document.getElementById('detail-MaHoaDonGoc').value = data.maHoaDonGoc;
            document.getElementById('detail-MaVe').value = data.maVe;
            document.getElementById('detail-sbDiCode').innerText = data.maSbDi;
            document.getElementById('detail-sbDiName').innerText = data.sbDi;
            document.getElementById('detail-thoiGianDi').innerText = data.thoiGianDi;
            document.getElementById('detail-maCb').innerText = data.hangBay;
            document.getElementById('detail-sbDenCode').innerText = data.maSbDen;
            document.getElementById('detail-sbDenName').innerText = data.sbDen;
            document.getElementById('detail-thoiGianDen').innerText = data.thoiGianDen;
            document.getElementById('detail-tenKh').innerText = data.tenHanhKhach;
            document.getElementById('detail-cccd').innerText = data.cccd || 'N/A';
            document.getElementById('detail-sdt').innerText = data.sdt || 'N/A';
            document.getElementById('detail-soGhe').innerText = data.soGhe;
            document.getElementById('detail-ngayDat').innerText = data.ngayDat;
            document.getElementById('detail-price').innerText = data.tongTien;
            document.getElementById('detail-xluat').innerText = data.hanhLyXachTay || '7 kg';
            document.getElementById('detail-xgui').innerText = data.hanhLyKyGui || '20 kg';

            // Hang ghe badge
            const badge = document.getElementById('detail-hangGhe-badge');
            const isBusiness = data.hangGhe?.toLowerCase().includes('thuong');
            badge.className = 'badge ' + (isBusiness ? 'bg-warning text-dark' : 'bg-primary');
            badge.innerText = data.hangGhe?.toUpperCase() || 'N/A';

            // Trang thai
            const statusEl = document.getElementById('detail-status');
            const trangThai = data.trangThai || "Chờ thanh toán";
            statusEl.innerText = trangThai.toUpperCase();

            const isPaid = trangThai === 'Đã thanh toán' || trangThai === 'Thành công';
            const isCancelled = trangThai === 'Đã hủy';

            statusEl.className = 'bp-status-badge ' + (isPaid ? 'bg-success-subtle text-success' : isCancelled ? 'bg-danger-subtle text-danger' : 'bg-warning-subtle text-warning');

            // Ẩn nút hủy nếu đã hủy
            document.getElementById('btnCancelInDetail').style.display = isCancelled ? 'none' : 'inline-flex';

            // Ẩn nút in hóa đơn nếu đã hủy
            document.getElementById('btnPrintInDetail').style.display = isCancelled ? 'none' : 'inline-flex';

            new bootstrap.Modal(document.getElementById('modalDetail')).show();
        })
        .catch(() => showToast('Không thể tải thông tin vé!', 'bg-danger'));
}

// ─────────────────────────────────────────
// IN HOA DON PDF
// ─────────────────────────────────────────
function printInvoice(maVe) {
    if (!maVe) return;
    showToast('Đang xuất PDF...', 'bg-primary');
    window.open(`/Admin/Booking/XuatHoaDonPdf?maVe=${maVe}`, '_blank');
}

function printInvoiceFromDetail() {
    const maVe = document.getElementById('detail-MaVe').value;
    if (maVe) printInvoice(maVe);
}

// ─────────────────────────────────────────
// THANH TOAN
// ─────────────────────────────────────────
function openPaymentModal(id, amount) {
    processingId = id;
    document.getElementById('payPnrCode').innerText = '#PNR-' + String(id).padStart(4, '0');
    document.getElementById('payAmount').innerText = amount;
    new bootstrap.Modal(document.getElementById('modalPayment')).show();
}

function confirmPayment() {
    const method = document.getElementById('payMethod').value;
    fetch('/Admin/Booking/XacNhanThanhToan', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            id: parseInt(processingId),
            phuongThuc: method
        })
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                bootstrap.Modal.getInstance(document.getElementById('modalPayment')).hide();

                // Cập nhật Badge trạng thái
                const statusBadge = document.getElementById('status-' + processingId);
                if (statusBadge) {
                    statusBadge.className = 'status-badge bg-success-subtle text-success mt-1 d-inline-block';
                    statusBadge.innerText = 'ĐÃ THANH TOÁN';
                }

                // Làm mờ và vô hiệu hóa nút thanh toán
                const btnPay = document.getElementById('btnPay-' + processingId);
                if (btnPay) {
                    btnPay.disabled = true;
                    btnPay.classList.add('opacity-25');
                    btnPay.classList.remove('bg-success-subtle', 'text-success', 'border-success');
                    btnPay.title = 'Đã thanh toán';
                }

                showToast(data.message, 'bg-success');
            } else { showToast(data.message, 'bg-danger'); }
        })
        .catch(() => showToast('Lỗi khi thanh toán!', 'bg-danger'));
}

// ─────────────────────────────────────────
// HUY VE
// ─────────────────────────────────────────
// BIẾN TOÀN CỤC CHO MODAL XÁC NHẬN
let confirmActionType = ''; // 'cancel' hoặc 'delete'
let confirmBookingId = null;

async function cancelTicket() {
    const id = document.getElementById('detail-MaHoaDonGoc').value;
    if (!id) return;

    confirmActionType = 'cancel';
    confirmBookingId = id;

    document.getElementById('confirmText').innerText = "Bạn có chắc chắn muốn hủy đơn đặt vé này?";
    const btn = document.getElementById('btnConfirmAction');
    btn.innerText = 'Đồng ý hủy';
    btn.className = 'btn btn-danger w-50 fw-bold rounded-pill';
    btn.onclick = executeConfirmAction;

    const modal = new bootstrap.Modal(document.getElementById('modalConfirm'));
    modal.show();
}

async function confirmDelete(id) {
    if (!id) return;

    confirmActionType = 'delete';
    confirmBookingId = id;

    document.getElementById('confirmText').innerText = "Dữ liệu xóa sẽ không thể khôi phục.";
    const btn = document.getElementById('btnConfirmAction');
    btn.innerText = 'Xóa bỏ';
    btn.className = 'btn btn-danger w-50 fw-bold rounded-pill';
    btn.onclick = executeConfirmAction;

    const modal = new bootstrap.Modal(document.getElementById('modalConfirm'));
    modal.show();
}

function executeConfirmAction() {
    if (!confirmBookingId) return;

    const endpoint = confirmActionType === 'cancel' ? '/Admin/Booking/HuyDatVe' : '/Admin/Booking/XoaDatVe';
    const successMsg = confirmActionType === 'cancel' ? 'Đã hủy đơn thành công!' : 'Đã xóa vĩnh viễn đơn đặt vé!';

    fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: parseInt(confirmBookingId) })
    })
        .then(r => r.json())
        .then(data => {
            // Đóng modal
            const modalEl = document.getElementById('modalConfirm');
            const modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            if (data.success) {
                toastMessage(successMsg, 'bg-success');
                setTimeout(() => location.reload(), 1200);
            } else {
                toastMessage(data.message, 'bg-danger');
            }
        })
        .catch(() => toastMessage('Lỗi kết nối máy chủ!', 'bg-danger'));
}

function toastMessage(message, bgClass = 'bg-success') {
    const toastEl = document.getElementById('liveToast');
    const msgEl = document.getElementById('toastMessage');
    toastEl.className = `toast align-items-center text-white ${bgClass} border-0 shadow-lg rounded-pill px-4 py-3`;
    msgEl.innerText = message;
    const toast = new bootstrap.Toast(toastEl);
    toast.show();
}
