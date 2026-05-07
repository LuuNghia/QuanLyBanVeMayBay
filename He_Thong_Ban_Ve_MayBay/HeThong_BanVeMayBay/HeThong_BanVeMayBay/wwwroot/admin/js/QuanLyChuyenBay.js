const toastEl = document.getElementById('liveToast');
const bsToast = new bootstrap.Toast(toastEl, { delay: 2500 });
let currentFlightId = '';
let isEditMode = false;

// 1. HÀM HIỂN THỊ THÔNG BÁO (Chuẩn hóa giống template cũ)
function toastMessage(msg, colorClass = 'bg-success') {
    document.getElementById('toastMessage').innerText = msg;
    toastEl.className = `toast align-items-center text-white border-0 shadow-lg rounded-pill px-4 py-3 ${colorClass}`;

    const toastContainer = toastEl.closest('.toast-container');
    if (toastContainer) {
        toastContainer.className = 'toast-container position-fixed top-0 start-50 translate-middle-x p-3';
        toastContainer.style.zIndex = '9999';
        toastContainer.style.marginTop = '20px';
    }
    bsToast.show();
}

// 2. MỞ MODAL THÊM MỚI
function openAddModal() {
    isEditMode = false;
    document.getElementById('modalFlightTitle').innerHTML = '<i class="bi bi-plus-circle me-2"></i>Thêm Chuyến bay mới';
    document.getElementById('flightForm').reset();

    // Mở khóa nhập liệu cho Mã Chuyến Bay
    const fCodeInput = document.getElementById('fCode');
    fCodeInput.readOnly = false;
    fCodeInput.style.cursor = 'text';
    fCodeInput.classList.remove('text-muted');

    // Reset trạng thái về "Sắp bay" khi thêm mới
    const fStatus = document.getElementById('fStatus');
    if (fStatus) {
        fStatus.value = "Sắp bay";
    }

    // Reset giá vé về rỗng
    const fPrice = document.getElementById('fPrice');
    if (fPrice) fPrice.value = '';

    // Ẩn thông báo lưu ý
    const noticeEl = document.getElementById('editNotice');
    if (noticeEl) noticeEl.style.display = 'none';

    new bootstrap.Modal(document.getElementById('modalFlight')).show();
}

// 3. MỞ MODAL SỬA (Lấy data từ API)
async function openEditModal(flightCode) {
    isEditMode = true;
    document.getElementById('modalFlightTitle').innerHTML = '<i class="bi bi-pencil-square me-2"></i>Cập nhật Chuyến bay';

    try {
        const res = await fetch(`/Admin/Flight/ChiTietChuyenBay?id=${flightCode}`);
        if (!res.ok) {
            toastMessage('Không thể lấy thông tin chuyến bay', 'bg-danger');
            return;
        }

        const json = await res.json();
        const data = json.data;

        // Đổ dữ liệu lên form
        document.getElementById('fCode').value = data.maCb;
        document.getElementById('fPlane').value = data.maLoaiMb;
        document.getElementById('fRoute').value = data.maTuyen;
        document.getElementById('fDate').value = data.ngayGioBay;

        // Cập nhật trạng thái
        const fStatus = document.getElementById('fStatus');
        if (fStatus) {
            fStatus.value = data.trangThai;
        }

        // Cập nhật giá vé (Lấy số gốc để gán vào input type="number")
        const fPrice = document.getElementById('fPrice');
        if (fPrice) {
            fPrice.value = data.giaVeGoc;
        }

        // Xử lý UI: Cho hiện mã nhưng không cho nhập
        const fCodeInput = document.getElementById('fCode');
        fCodeInput.readOnly = true;
        fCodeInput.style.cursor = 'not-allowed';
        fCodeInput.classList.add('text-muted');

        // Hiện thông báo lưu ý phía trên
        const noticeEl = document.getElementById('editNotice');
        if (noticeEl) {
            noticeEl.style.display = 'block';
            noticeEl.innerText = 'Lưu ý: Không thể thay đổi Mã chuyến bay sau khi đã tạo.';
        }

        new bootstrap.Modal(document.getElementById('modalFlight')).show();
    } catch (e) {
        toastMessage('Lỗi kết nối Server!', 'bg-danger');
    }
}

// 4. SUBMIT FORM THÊM / SỬA (Có ràng buộc dữ liệu)
async function submitFlightForm(e) {
    e.preventDefault();

    const maCb = document.getElementById('fCode').value.trim().toUpperCase();
    const maLoaiMb = document.getElementById('fPlane').value;
    const maTuyen = document.getElementById('fRoute').value;
    const ngayGioBay = document.getElementById('fDate').value;
    const trangThai = document.getElementById('fStatus') ? document.getElementById('fStatus').value : 'Sắp bay';

    // Lấy giá vé
    const giaVeCoBanInput = document.getElementById('fPrice') ? document.getElementById('fPrice').value : '0';
    const giaVeCoBan = giaVeCoBanInput ? parseFloat(giaVeCoBanInput) : 0;

    // Bẫy lỗi: Kiểm tra rỗng
    if (!maCb || !maLoaiMb || !maTuyen || !ngayGioBay) {
        toastMessage('Vui lòng nhập và chọn đầy đủ thông tin chuyến bay!', 'bg-warning text-dark');
        return;
    }
    if (giaVeCoBan < 0) {
        toastMessage('Giá vé cơ bản không được là số âm!', 'bg-warning text-dark');
        // Nếu muốn, bạn có thể tự động focus lại vào ô nhập giá vé
        const fPriceEl = document.getElementById('fPrice');
        if (fPriceEl) fPriceEl.focus();
        return;
    }

    // Bẫy lỗi: Kiểm tra ngày giờ bay (không được ở trong quá khứ so với thời điểm nhập - Tạm tắt nếu bạn cho phép sửa chuyến trong quá khứ)
     const selectedDate = new Date(ngayGioBay);
     if (selectedDate < new Date() && !isEditMode) {
         toastMessage('Ngày giờ bay không hợp lệ !', 'bg-warning text-dark');
         return;
     }

    const payload = {
        MaCb: maCb,
        MaLoaiMb: maLoaiMb,
        MaTuyen: maTuyen,
        NgayGioBay: ngayGioBay,
        TrangThai: trangThai,
        GiaVeCoBan: giaVeCoBan
    };

    const url = isEditMode ? '/Admin/Flight/SuaChuyenBay' : '/Admin/Flight/ThemChuyenBay';

    try {
        const req = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (req.ok) {
            bootstrap.Modal.getInstance(document.getElementById('modalFlight')).hide();
            toastMessage(isEditMode ? 'Cập nhật chuyến bay thành công!' : 'Thêm chuyến bay thành công!', 'bg-success');
            setTimeout(() => window.location.reload(), 1000);
        } else {
            const err = await req.text();
            toastMessage(err || "Có lỗi xảy ra phía máy chủ.", 'bg-danger');
        }
    } catch (err) {
        toastMessage("Lỗi kết nối đến máy chủ.", 'bg-danger');
    }
}

// 5. MỞ MODAL XEM CHI TIẾT
async function openViewModal(flightCode) {
    try {
        const res = await fetch(`/Admin/Flight/ChiTietChuyenBay?id=${flightCode}`);
        if (!res.ok) {
            toastMessage('Không tìm thấy chuyến bay', 'bg-warning text-dark');
            return;
        }

        const json = await res.json();
        const data = json.data;

        // Header
        document.getElementById('viewCode').innerText = data.maCb;
        document.getElementById('viewDate').innerHTML = `<i class="bi bi-clock-history me-2"></i>${data.ngayGioBayDisplay}`;

        // Xử lý màu sắc cho Trạng thái
        const statusEl = document.getElementById('viewStatus');
        statusEl.innerText = data.trangThai;
        if (data.trangThai === "Delay" || data.trangThai === "Hủy chuyến") {
            statusEl.className = "badge bg-danger text-white px-3 py-2 rounded-pill fs-6 fw-bold shadow-sm";
        } else {
            statusEl.className = "badge bg-white text-primary px-3 py-2 rounded-pill fs-6 fw-bold shadow-sm";
        }

        // Lộ trình Đi
        document.getElementById('viewSbDi').innerText = data.maSbDi;
        document.getElementById('viewThanhPhoDi').innerText = data.thanhPhoDi;
        document.getElementById('viewTenSbDi').innerText = data.tenSbDi;

        // Lộ trình Đến
        document.getElementById('viewSbDen').innerText = data.maSbDen;
        document.getElementById('viewThanhPhoDen').innerText = data.thanhPhoDen;
        document.getElementById('viewTenSbDen').innerText = data.tenSbDen;

        // Thông tin phụ
        document.getElementById('viewPlane').innerText = data.tenMayBay;
        document.getElementById('viewHangSX').innerText = data.hangSanXuat;

        // Hiển thị giá vé cơ bản (có chữ VNĐ) trên Modal Xem Chi Tiết
        const viewPrice = document.getElementById('viewPrice');
        if (viewPrice) {
            viewPrice.innerText = data.giaVeCoBanDisplay;
        }

        new bootstrap.Modal(document.getElementById('modalView')).show();
    } catch (e) {
        toastMessage('Lỗi lấy dữ liệu!', 'bg-danger');
    }
}

// 6. XÓA CHUYẾN BAY
function openDeleteModal(flightCode) {
    currentFlightId = flightCode;
    document.getElementById('delFlightCode').innerText = flightCode;
    new bootstrap.Modal(document.getElementById('modalDelete')).show();
}

async function confirmDelete() {
    try {
        const res = await fetch(`/Admin/Flight/XoaChuyenBay?id=${currentFlightId}`, { method: 'DELETE' });

        bootstrap.Modal.getInstance(document.getElementById('modalDelete')).hide();

        if (res.ok) {
            const row = document.getElementById('row-' + currentFlightId);
            if (row) {
                row.style.opacity = '0';
                setTimeout(() => row.remove(), 300);
            }
            toastMessage(`Đã xóa chuyến bay ${currentFlightId} thành công!`, 'bg-success');
        } else {
            const err = await res.text();
            toastMessage(err || 'Không thể xóa chuyến bay này!', 'bg-danger');
        }
    } catch (e) {
        toastMessage('Lỗi hệ thống!', 'bg-danger');
    }
}