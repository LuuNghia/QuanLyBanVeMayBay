    let currentId = '';
    let isEditMode = false;
    const toastEl = document.getElementById('liveToast');
    const bsToast = new bootstrap.Toast(toastEl, { delay: 2500 });

    // Khai báo các Modal để điều khiển đóng/mở mượt mà
    const modalPolicy = new bootstrap.Modal(document.getElementById('modalPolicy'));
    const modalDelete = new bootstrap.Modal(document.getElementById('modalDelete'));
    const modalTienIch = new bootstrap.Modal(document.getElementById('modalTienIch'));
    const modalDeleteTienIch = new bootstrap.Modal(document.getElementById('modalDeleteTienIch'));

document.addEventListener('DOMContentLoaded', () => {
    const hash = window.location.hash;
    if (hash) {
        const tabTrigger = document.querySelector(`button[data-bs-toggle="pill"][data-bs-target="${hash}"]`);
        if (tabTrigger) {
            const tab = new bootstrap.Tab(tabTrigger);
            tab.show();
        }
    }

    document.querySelectorAll('button[data-bs-toggle="pill"]').forEach(tabTrigger => {
        tabTrigger.addEventListener('shown.bs.tab', event => {
            const target = event.target.getAttribute('data-bs-target');
            if (target) {
                history.replaceState(null, document.title, `${window.location.pathname}${window.location.search}${target}`);
            }
        });
    });
});

    function showToast(msg, isSuccess = true) {
        document.getElementById('toastMessage').innerText = msg;
        toastEl.className = `toast align-items-center text-white border-0 shadow-lg rounded-pill px-3 py-2 ${isSuccess ? 'bg-success' : 'bg-danger'}`;
        bsToast.show();
    }

    // --- QUẢN LÝ HẠNG GHẾ ---

    function openAddModal() {
        isEditMode = false;
        currentId = '';
        document.getElementById('modalPolicyTitle').innerText = 'Thêm hạng vé mới';
        document.getElementById('policyForm').reset();

        // Bỏ check tất cả tiện ích
        document.querySelectorAll('input[name="tienIchIds"]').forEach(chk => chk.checked = false);

        modalPolicy.show();
    }

    async function openEditModal(id) {
        isEditMode = true;
        currentId = id;
        document.getElementById('modalPolicyTitle').innerText = 'Cập nhật hạng vé';

        try {
            const res = await fetch(`/Admin/Flight/LayThongTinHangGhe?id=${id}`);
            const data = await res.json();

            document.getElementById('pName').value = data.tenHangGhe;
            document.getElementById('pColor').value = data.mauSac || 'secondary';
            document.getElementById('pMultiplier').value = data.heSoGia;
            document.getElementById('pCarryOn').value = data.hanhLyXachTay;
            document.getElementById('pChecked').value = data.hanhLyKyGui;
            document.getElementById('pRefund').value = data.hoanVe;
            document.getElementById('pReschedule').value = data.doiLich;

            const selectedIds = Array.isArray(data.maTienIchIds) ? data.maTienIchIds : [];
            document.querySelectorAll('input[name="tienIchIds"]').forEach(chk => {
                chk.checked = selectedIds.includes(parseInt(chk.value));
            });

            modalPolicy.show();
        } catch (e) {
            showToast("Lỗi tải dữ liệu!", false);
        }
    }

    async function savePolicy() {
        // Thu thập danh sách ID tiện ích đã chọn
        const selectedTienIches = Array.from(document.querySelectorAll('input[name="tienIchIds"]:checked'))
            .map(chk => parseInt(chk.value));

        const payload = {
            MaHangGhe: currentId,
            TenHangGhe: document.getElementById('pName').value.trim(),
            MauSac: document.getElementById('pColor').value,
            HeSoGia: parseFloat(document.getElementById('pMultiplier').value),
            HanhLyXachTay: document.getElementById('pCarryOn').value,
            HanhLyKyGui: document.getElementById('pChecked').value,
            HoanVe: document.getElementById('pRefund').value,
            DoiLich: document.getElementById('pReschedule').value,
            MaTienIchIds: selectedTienIches // Gửi mảng ID lên Backend
        };

        if (!payload.TenHangGhe || isNaN(payload.HeSoGia)) {
            showToast("Vui lòng nhập tên và hệ số giá!", false);
            return;
        }

        const url = isEditMode ? '/Admin/Flight/CapNhatHangGhe' : '/Admin/Flight/ThemHangGhe';

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                modalPolicy.hide();
                showToast(isEditMode ? "Đã cập nhật thành công!" : "Đã thêm mới thành công!");
                setTimeout(() => location.reload(), 800);
            } else {
                const message = await res.text();
                showToast(message || "Có lỗi xảy ra khi lưu!", false);
            }
        } catch (e) { showToast("Lỗi kết nối!", false); }
    }

    function openDeleteModal(id) {
        currentId = id;
        modalDelete.show();
    }

    async function confirmDelete() {
        try {
            const res = await fetch(`/Admin/Flight/XoaHangGhe?id=${currentId}`, { method: 'DELETE' });
            if (res.ok) {
                modalDelete.hide();
                showToast("Đã xóa hạng ghế thành công.");

                const card = document.getElementById(`card-${currentId}`);
                if (card) {
                    card.style.transition = "all 0.5s ease";
                    card.style.opacity = "0";
                    card.style.transform = "translateY(10px)";
                    setTimeout(() => card.remove(), 500);
                }
            } else {
                const message = await res.text();
                showToast(message || "Không thể xóa hạng ghế!", false);
            }
        } catch (e) {
            showToast("Lỗi hệ thống!", false);
        }
    }

    // --- QUẢN LÝ TIỆN ÍCH ---

    function openAddTienIchModal() {
        document.getElementById('tienIchModalTitle').innerText = "Thêm tiện ích hệ thống";
        document.getElementById('tiId').value = "0";
        document.getElementById('tienIchForm').reset();
        updateIconPreview('bi-star-fill');
        modalTienIch.show();
    }

    function openEditTienIchModal(id, name, icon) {
        document.getElementById('tienIchModalTitle').innerText = "Cập nhật tiện ích";
        document.getElementById('tiId').value = id;
        document.getElementById('tiName').value = name;
        document.getElementById('tiIcon').value = icon;
        updateIconPreview(icon);
        modalTienIch.show();
    }

    function updateIconPreview(val) {
        const icon = val.trim() || 'bi-star-fill';
        document.getElementById('iconPreview').className = `bi ${icon} text-warning`;
    }

    async function saveTienIch() {
        const payload = {
            MaTienIch: parseInt(document.getElementById('tiId').value),
            TenTienIch: document.getElementById('tiName').value.trim(),
            Icon: document.getElementById('tiIcon').value.trim() || "bi-star-fill"
        };

        if (!payload.TenTienIch) {
            showToast("Vui lòng nhập tên tiện ích!", false);
            return;
        }

        try {
            const res = await fetch('/Admin/Flight/LuuTienIch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                modalTienIch.hide();
                showToast("Đã lưu tiện ích thành công!");
                setTimeout(() => location.reload(), 800);
            } else {
                showToast("Lỗi khi lưu tiện ích!", false);
            }
        } catch (e) { showToast("Lỗi hệ thống!", false); }
    }

    // --- XỬ LÝ XÓA ĐẸP (KHÔNG DÙNG CONFIRM) ---

    function deleteTienIch(id) {
        currentId = id; // Dùng chung biến currentId để lưu ID cần xóa
        modalDeleteTienIch.show(); // Mở modal xác nhận thay vì confirm()
    }

    async function executeDeleteTienIch() {
        try {
            const res = await fetch(`/Admin/Flight/XoaTienIch?id=${currentId}`, { method: 'DELETE' });
            if (res.ok) {
                modalDeleteTienIch.hide();
                showToast("Đã xóa tiện ích thành công.");

                // Hiệu ứng bay màu dòng dữ liệu
                const row = document.getElementById(`tr-ti-${currentId}`);
                if (row) {
                    row.style.transition = "all 0.5s ease";
                    row.style.opacity = "0";
                    row.style.transform = "translateX(20px)";
                    setTimeout(() => row.remove(), 500);
                }
            } else {
                showToast("Không thể xóa tiện ích đang được sử dụng!", false);
            }
        } catch (e) { showToast("Lỗi hệ thống!", false); }
    }