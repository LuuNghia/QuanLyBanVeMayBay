
const toastEl = document.getElementById('liveToast');
const bsToast = new bootstrap.Toast(toastEl, { delay: 2500 });

document.addEventListener('DOMContentLoaded', () => {
    const hash = window.location.hash;
    if (hash) {
        const tabTrigger = document.querySelector(`button[data-bs-toggle="tab"][data-bs-target="${hash}"]`);
        if (tabTrigger) {
            const tab = new bootstrap.Tab(tabTrigger);
            tab.show();
        }
    }

    document.querySelectorAll('button[data-bs-toggle="tab"]').forEach(tabTrigger => {
        tabTrigger.addEventListener('shown.bs.tab', event => {
            const target = event.target.getAttribute('data-bs-target');
            if (target) {
                history.replaceState(null, document.title, `${window.location.pathname}${window.location.search}${target}`);
            }
        });
    });

    registerModalFocusFix('modalHistory');
    registerModalFocusFix('modalDetail');
    registerModalFocusFix('modalAddUser');
});

function registerModalFocusFix(modalId) {
    const modalEl = document.getElementById(modalId);
    if (!modalEl) return;
    modalEl.addEventListener('hidden.bs.modal', () => {
        if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
        }
    });
}

function showModal(modalId) {
    const modalEl = document.getElementById(modalId);
    if (!modalEl) return;

    const openModal = document.querySelector('.modal.show');
    if (openModal && openModal !== modalEl) {
        if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
        }
        bootstrap.Modal.getInstance(openModal)?.hide();
        openModal.addEventListener('hidden.bs.modal', () => {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }, { once: true });
        return;
    }

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

const accountTypeSelect = document.getElementById('accountTypeSelect');
if (accountTypeSelect) {
    accountTypeSelect.addEventListener('change', toggleRoleField);
}

function toggleRoleField() {
    const roleField = document.getElementById('roleField');
    if (!roleField || !accountTypeSelect) return;

    const normalize = accountTypeSelect.value.toLowerCase();
    if (normalize.includes('khách')) {
        roleField.classList.add('d-none');
    } else {
        roleField.classList.remove('d-none');
    }
}

function toastMessage(msg, colorClass = 'bg-success') {
    const toastMessageEl = document.getElementById('toastMessage');
    if (!toastMessageEl) {
        console.warn('Toast element not found:', msg);
        return;
    }
    toastMessageEl.innerText = msg;
    toastEl.className = `toast align-items-center text-white border-0 shadow-lg rounded-pill px-3 py-2 ${colorClass}`;
    bsToast.show();
}

let currentUserType = '';
let currentUserId = '';

function openAddUserModal() {
    document.getElementById('modalAddUserTitle').innerHTML = '<i class="bi bi-person-circle me-2"></i>Thêm Người dùng mới';
    document.getElementById('userForm').reset();
    document.getElementById('accountTypeSelect').disabled = false;
    document.getElementById('userType').value = '';
    document.getElementById('userId').value = '';
    document.getElementById('userAccountId').value = '';
    document.getElementById('roleField').classList.remove('d-none');
    currentUserType = '';
    currentUserId = '';
    showModal('modalAddUser');
}

async function openEditUserModal(type, id) {
    currentUserType = type;
    currentUserId = id;
    document.getElementById('modalAddUserTitle').innerHTML = '<i class="bi bi-pencil-square me-2"></i>Cập nhật tài khoản';
    document.getElementById('accountTypeSelect').disabled = false;
    document.getElementById('userType').value = type;
    document.getElementById('userId').value = id;

    try {
        const url = type === 'staff'
            ? `/Admin/Account/LayThongTinNhanVien?id=${id}`
            : `/Admin/Account/LayThongTinKhachHang?id=${id}`;
        const res = await fetch(url);
        if (!res.ok) {
            toastMessage('Không thể tải dữ liệu tài khoản!', 'bg-danger');
            return;
        }
        const data = await res.json();
        document.getElementById('userFullName').value = data.hoTen || '';
        document.getElementById('userPhone').value = data.soDienThoai || '';
        document.getElementById('userEmail').value = data.email || '';
        document.getElementById('userCccd').value = data.cccd || '';
        document.getElementById('userGender').value = data.gioiTinh || '';
        document.getElementById('userBirthDate').value = data.ngaySinh || '';
        document.getElementById('userAddress').value = data.diaChi || '';
        document.getElementById('userRole').value = data.chucVu || '';
        document.getElementById('userPassword').value = '';
        document.getElementById('userAccountId').value = data.maTk || '';
        if (data.loaiTaiKhoan) {
            document.getElementById('accountTypeSelect').value = data.loaiTaiKhoan;
        }

        toggleRoleField();

        showModal('modalAddUser');
    } catch (err) {
        toastMessage('Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

async function updateAccountStatus(maTk, status, tab = 'staff') {
    if (!maTk) {
        toastMessage('Không tìm thấy tài khoản để cập nhật.', 'bg-danger');
        return;
    }

    try {
        const res = await fetch(`/Admin/Account/CapNhatTrangThaiTaiKhoan?maTk=${maTk}&status=${status}&tab=${tab}`, {
            method: 'POST'
        });
        if (res.ok) {
            const data = await res.json();
            toastMessage(data.message || 'Đã cập nhật trạng thái tài khoản.');
        } else {
            const err = await res.text();
            toastMessage(err || 'Không thể cập nhật trạng thái!', 'bg-danger');
        }
    } catch (err) {
        toastMessage('Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

async function openRoleModal(maNv, userName) {
    document.getElementById('roleUserId').value = maNv;
    document.getElementById('roleUserName').innerText = userName;
    
    // Reset switches
    document.querySelectorAll('.role-switch').forEach(s => s.checked = false);
    
    try {
        const res = await fetch(`/Admin/Account/LayThongTinNhanVien?id=${maNv}`);
        if (res.ok) {
            const data = await res.json();
            if (data.quyen) {
                const roles = data.quyen.split(',');
                roles.forEach(r => {
                    const el = document.getElementById(`role_${r.trim()}`);
                    if (el) el.checked = true;
                });
            }
        }
    } catch (err) {
        console.error(err);
    }
    
    showModal('modalRole');
}

async function saveRoles() {
    const maNv = document.getElementById('roleUserId').value;
    const selectedRoles = [];
    document.querySelectorAll('.role-switch:checked').forEach(s => {
        selectedRoles.push(s.id.replace('role_', ''));
    });
    
    const quyen = selectedRoles.join(',');
    
    try {
        const res = await fetch(`/Admin/Account/CapNhatQuyenNhanVien?maNv=${maNv}&quyen=${quyen}`, {
            method: 'POST'
        });
        if (res.ok) {
            const data = await res.json();
            toastMessage(data.message || 'Cập nhật quyền thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalRole')).hide();
        } else {
            toastMessage('Không thể cập nhật quyền!', 'bg-danger');
        }
    } catch (err) {
        toastMessage('Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

async function openHistoryModal(customerId) {
    try {
        const res = await fetch(`/Admin/Account/LichSuKhachHang?id=${customerId}`);
        if (!res.ok) {
            const message = await res.text();
            toastMessage(message || 'Không thể tải lịch sử chuyến bay!', 'bg-danger');
            return;
        }

        const data = await readJsonResponse(res);
        const historyName = document.getElementById('historyName');
        const historyTotal = document.getElementById('historyTotal');
        if (!historyName || !historyTotal) {
            toastMessage('Không tìm thấy khung lịch sử chuyến bay.', 'bg-danger');
            return;
        }
        historyName.innerText = data.hoTen || '--';
        historyTotal.innerText = `Tổng số: ${data.total || 0} chuyến`;

        const badge = document.getElementById('historyBadge');
        const loai = (data.loaiThanhVien || 'Thường').toUpperCase();
        if (!badge) {
            toastMessage('Không tìm thấy badge lịch sử.', 'bg-danger');
            return;
        }
        badge.innerText = `Thành viên ${loai}`;
        badge.className = `badge me-3 ${getMembershipBadgeClass(loai)}`;

        const tbody = document.getElementById('historyTableBody');
        if (!tbody) {
            toastMessage('Không tìm thấy bảng lịch sử chuyến bay.', 'bg-danger');
            return;
        }
        if (!data.tickets || data.tickets.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-4">Chưa có dữ liệu lịch sử.</td></tr>';
        } else {
            tbody.innerHTML = data.tickets.map(ticket => `
                <tr>
                    <td class="fw-bold text-primary">${ticket.maVe}</td>
                    <td><span class="fw-bold">${ticket.sanBayDi}</span> <i class="bi bi-arrow-right opacity-50"></i> <span class="fw-bold">${ticket.sanBayDen}</span></td>
                    <td>${ticket.ngayBay}</td>
                    <td><span class="badge ${getStatusBadgeClass(ticket.trangThai)}">${ticket.trangThai}</span></td>
                </tr>
            `).join('');
        }

        showModal('modalHistory');
    } catch (err) {
        toastMessage(err.message || 'Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

let currentDetailType = '';
let currentDetailId = '';
let currentDetailEmail = '';

async function openDetailModal(type, id) {
    currentDetailType = type;
    currentDetailId = id;

    // Hiển thị trạng thái chờ
    const detailNameEl = document.getElementById('detailName');
    if (detailNameEl) detailNameEl.innerText = "Đang tải...";

    try {
        const url = type === 'staff'
            ? `/Admin/Account/LayThongTinNhanVien?id=${id}`
            : `/Admin/Account/LayThongTinKhachHang?id=${id}`;

        const res = await fetch(url);
        if (!res.ok) {
            toastMessage('Không thể tải thông tin chi tiết!', 'bg-danger');
            return;
        }

        const data = await readJsonResponse(res);

        // 1. Đổ dữ liệu chung (Sử dụng Optional Chaining ?. để tránh lỗi null)
        const setText = (id, value) => {
            const el = document.getElementById(id);
            if (el) el.innerText = value || '--';
        };

        setText('detailName', data.hoTen);
        setText('detailCode', type === 'staff' ? `Mã NV: ${data.maNv}` : `Mã KH: ${data.maKh}`);
        setText('detailEmail', data.email);
        setText('detailPhone', data.soDienThoai);
        setText('detailCccd', data.cccd);
        setText('detailGender', data.gioiTinh);
        setText('detailBirthDate', data.ngaySinh);
        setText('detailAddress', data.diaChi);

        currentDetailEmail = data.email || '';

        // 2. Xử lý Ảnh đại diện
        const avatarImg = document.getElementById('detailAvatar');
        const avatarPlaceholder = document.getElementById('detailAvatarPlaceholder');
        if (avatarImg) {
            if (data.anhDaiDien) {
                avatarImg.src = data.anhDaiDien;
                avatarImg.classList.remove('d-none');
                avatarPlaceholder?.classList.add('d-none');
            } else {
                avatarImg.classList.add('d-none');
                avatarPlaceholder?.classList.remove('d-none');
            }
        }

        // 3. Xử lý các trường đặc thù (Nhân viên/Khách hàng)
        // Ẩn/Hiện bằng class thay vì classList trực tiếp trên biến có thể null
        const staffElements = document.querySelectorAll('.staff-only');
        const customerElements = document.querySelectorAll('.customer-only');

        if (type === 'staff') {
            staffElements.forEach(el => el.classList.remove('d-none'));
            customerElements.forEach(el => el.classList.add('d-none'));
            setText('detailRole', data.chucVu);
            setText('detailJoined', data.ngayVaoLam || data.ngayThamGia);
        } else {
            staffElements.forEach(el => el.classList.add('d-none'));
            customerElements.forEach(el => el.classList.remove('d-none'));
            const pointsEl = document.getElementById('detailPoints');
            if (pointsEl) pointsEl.innerText = (data.diemTichLuy || 0).toLocaleString();
        }

        // 4. Xử lý Badge & Điểm tích lũy
        const badgeContainer = document.getElementById('detailBadgeContainer');
        const pointsEl = document.getElementById('detailPoints'); // Lấy phần tử hiển thị điểm

        if (type === 'staff') {
            // Xử lý Badge cho Nhân viên
            if (badgeContainer) {
                badgeContainer.innerHTML = `<span class="account-type-badge badge-custom-staff">Nhân viên</span>`;
            }
        } else {
            // 1. Xử lý Badge cho Khách hàng
            if (badgeContainer) {
                const loai = (data.loaiThanhVien || 'Thường').toUpperCase();
                let badgeClass = 'badge-custom-normal';

                if (loai.includes('VÀNG') || loai.includes('GOLD')) badgeClass = 'badge-custom-gold';
                else if (loai.includes('BẠC') || loai.includes('SILVER')) badgeClass = 'badge-custom-silver';
                else if (loai.includes('KIM CƯƠNG') || loai.includes('DIAMOND')) badgeClass = 'badge-custom-diamond';

                badgeContainer.innerHTML = `<span class="account-type-badge ${badgeClass}">HẠNG ${loai}</span>`;
            }

            // 2. QUAN TRỌNG: Đổ điểm tích lũy vào giao diện
            if (pointsEl) {
                // Kiểm tra tên thuộc tính từ database trả về (diemTichLuy hoặc DiemTichLuy)
                const diem = data.diemTichLuy || data.DiemTichLuy || 0;
                pointsEl.innerText = diem.toLocaleString() + " pts";
            }
        }
        showModal('modalDetail');

    } catch (err) {
        toastMessage('Lỗi hệ thống: ' + err.message, 'bg-danger');
    }
}
function copyDetailEmail() {
    if (!currentDetailEmail) {
        toastMessage('Không có email để sao chép.', 'bg-warning text-dark');
        return;
    }

    navigator.clipboard.writeText(currentDetailEmail)
        .then(() => toastMessage('Đã sao chép email.'))
        .catch(() => toastMessage('Không thể sao chép email.', 'bg-danger'));
}

function editFromDetail() {
    if (!currentDetailId || !currentDetailType) {
        toastMessage('Không xác định tài khoản để sửa.', 'bg-warning text-dark');
        return;
    }
    bootstrap.Modal.getInstance(document.getElementById('modalDetail')).hide();
    openEditUserModal(currentDetailType, currentDetailId);
}

function getInitials(name) {
    const parts = (name || '').trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return 'U';
    if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

function getMembershipBadgeClass(type) {
    const t = type.toUpperCase();
    if (t.includes('VÀNG') || t.includes('GOLD')) return 'badge-gold';
    if (t.includes('BẠC') || t.includes('SILVER')) return 'badge-silver';
    if (t.includes('KIM CƯƠNG') || t.includes('DIAMOND')) return 'badge-diamond';
    return 'bg-secondary text-white'; // Mặc định cho hạng Thường
}

function getStatusBadgeClass(status) {
    if (!status) return 'bg-secondary-subtle text-secondary';
    const normalized = status.toLowerCase();
    if (normalized.includes('hoàn') || normalized.includes('đã')) return 'bg-success-subtle text-success';
    if (normalized.includes('hủy') || normalized.includes('huỷ')) return 'bg-danger-subtle text-danger';
    return 'bg-warning-subtle text-warning';
}

function getAccountBadgeClass(type, text) {
    if (type === 'staff') {
        return text.toLowerCase().includes('admin') ? 'bg-danger-subtle text-danger' : 'bg-primary-subtle text-primary';
    }
    return getMembershipBadgeClass(text.toUpperCase());
}

let confirmActionType = '';
let confirmAccountId = 0;
let confirmTab = 'staff';

function confirmDeleteAccount(maTk, name, tab = 'staff') {
    if (!maTk) {
        toastMessage('Không tìm thấy tài khoản để xử lý!', 'bg-danger');
        return;
    }

    confirmActionType = 'delete';
    confirmAccountId = maTk;
    confirmTab = tab;
    document.getElementById('confirmText').innerText = `Bạn có chắc muốn xóa tài khoản ${name}?`;
    const btn = document.getElementById('btnConfirmAction');
    btn.innerText = 'Xóa';
    btn.className = 'btn btn-danger w-50 fw-bold';
    btn.onclick = executeConfirmAction;
    new bootstrap.Modal(document.getElementById('modalConfirm')).show();
}

async function executeConfirmAction() {
    if (confirmActionType !== 'delete' || !confirmAccountId) {
        toastMessage('Không xác định thao tác.', 'bg-danger');
        return;
    }

    try {
        const res = await fetch(`/Admin/Account/XoaTaiKhoan?maTk=${confirmAccountId}&tab=${confirmTab}`, { method: 'DELETE' });
        bootstrap.Modal.getInstance(document.getElementById('modalConfirm')).hide();
        if (res.ok) {
            const data = await readJsonResponse(res);
            toastMessage(data.message || 'Đã xóa tài khoản thành công!');
            setTimeout(() => window.location.reload(), 800);
        } else {
            const err = await res.text();
            toastMessage(err || 'Không thể xóa tài khoản!', 'bg-danger');
        }
    } catch (err) {
        toastMessage(err.message || 'Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

async function saveUser() {
    const type = document.getElementById('userType').value;
    const id = document.getElementById('userId').value;
    const accountId = document.getElementById('userAccountId').value;
    const accountType = document.getElementById('accountTypeSelect').value;

    const payload = {
        hoTen: document.getElementById('userFullName').value.trim(),
        gioiTinh: document.getElementById('userGender').value,
        ngaySinh: document.getElementById('userBirthDate').value || null,
        soDienThoai: document.getElementById('userPhone').value.trim(),
        email: document.getElementById('userEmail').value.trim(),
        cccd: document.getElementById('userCccd').value.trim(),
        diaChi: document.getElementById('userAddress').value.trim(),
        chucVu: document.getElementById('userRole').value.trim(),
        loaiTaiKhoan: accountType,
        matKhau: document.getElementById('userPassword').value.trim()
    };

    if (!payload.hoTen || !payload.soDienThoai || !payload.email || !payload.cccd) {
        toastMessage('Vui lòng nhập đầy đủ thông tin.', 'bg-warning text-dark');
        return;
    }

    if (!payload.ngaySinh) {
        toastMessage('Vui lòng chọn ngày sinh.', 'bg-warning text-dark');
        return;
    }

    const birthDate = new Date(payload.ngaySinh);
    const today = new Date();

    let age = today.getFullYear() - birthDate.getFullYear();

    const monthDiff = today.getMonth() - birthDate.getMonth();
    const dayDiff = today.getDate() - birthDate.getDate();

    if (monthDiff < 0 || (monthDiff === 0 && dayDiff < 0)) {
        age--;
    }

    if (birthDate > today) {
        toastMessage('Ngày sinh không hợp lệ (lớn hơn ngày hiện tại).', 'bg-danger text-white');
        return;
    }

    if (age < 18) {
        toastMessage('Khách hàng phải từ 18 tuổi trở lên.', 'bg-warning text-dark');
        return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(payload.email)) {
        toastMessage('Email không hợp lệ.', 'bg-warning text-dark');
        return;
    }

    const sdtRegex = /^(03|05|07|08|09)\d{8}$/;
    if (!sdtRegex.test(payload.soDienThoai)) {
        toastMessage('Số điện thoại không hợp lệ.', 'bg-warning text-dark');
        return;
    }

    const cccdRegex = /^\d{9}$|^\d{12}$/;
    if (!cccdRegex.test(payload.cccd)) {
        toastMessage('CCCD/PASSPORT phải gồm 9 hoặc 12 chữ số.', 'bg-warning text-dark');
        return;
    }

    if (!payload.loaiTaiKhoan) {
        toastMessage('Vui lòng chọn loại tài khoản.', 'bg-warning text-dark');
        return;
    }

    const normalize = accountType.toLowerCase();
    const resolvedType = normalize.includes('khách') ? 'customer' : 'staff';
    if (resolvedType === 'staff' && !payload.chucVu) {
        toastMessage('Vui lòng nhập chức vụ.', 'bg-warning text-dark');
        return;
    }

    if (!id && !payload.matKhau) {
        toastMessage('Vui lòng nhập mật khẩu.', 'bg-warning text-dark');
        return;
    }

    if (payload.matKhau && payload.matKhau.length < 6) {
        toastMessage('Mật khẩu phải có ít nhất 6 ký tự.', 'bg-warning text-dark');
        return;
    }

    let url = '';
    let entityType = type || resolvedType;

    if (!id) {
        url = entityType === 'staff' ? '/Admin/Account/ThemNhanVien' : '/Admin/Account/ThemKhachHang';
    } else {
        url = entityType === 'staff'
            ? '/Admin/Account/CapNhatNhanVien'
            : '/Admin/Account/CapNhatKhachHang';

        if (entityType === 'staff') {
            payload.maNv = id;
        } else {
            payload.maKh = id;
        }
        payload.maTk = accountId || null;
    }

    try {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            bootstrap.Modal.getInstance(document.getElementById('modalAddUser')).hide();
            toastMessage('Đã cập nhật tài khoản thành công!');
            setTimeout(() => window.location.reload(), 800);
        } else {
            const err = await res.text();
            toastMessage(err || 'Không thể cập nhật tài khoản!', 'bg-danger');
        }
    } catch (err) {
        toastMessage('Lỗi kết nối máy chủ!', 'bg-danger');
    }
}

async function readJsonResponse(res) {
    const contentType = res.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
        return await res.json();
    }
    const text = await res.text();
    throw new Error(text || 'Phản hồi không hợp lệ từ máy chủ.');
}
