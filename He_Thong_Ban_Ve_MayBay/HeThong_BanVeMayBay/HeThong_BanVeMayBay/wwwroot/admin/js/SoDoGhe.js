let selectedSeats = []; // Mảng chứa ID các ghế đang được chọn
let currentPlaneId = '';
let currentPlaneName = '';
let currentPlaneTotalSeats = 0;

// Hàm hiển thị thông báo đẹp bằng Bootstrap Modal
function showNotification(message, type = 'info') {
    const titleEl = document.getElementById('customAlertTitle');
    const msgEl = document.getElementById('customAlertMessage');
    const iconEl = document.getElementById('customAlertIcon');

    if (!titleEl || !msgEl || !iconEl) {
        alert(message);
        return;
    }

    msgEl.innerText = message;
    
    if (type === 'success') {
        titleEl.innerText = 'Thành công';
        iconEl.className = 'bi bi-check-circle text-success mb-3';
    } else if (type === 'error') {
        titleEl.innerText = 'Lỗi';
        iconEl.className = 'bi bi-x-circle text-danger mb-3';
    } else if (type === 'warning') {
        titleEl.innerText = 'Cảnh báo';
        iconEl.className = 'bi bi-exclamation-triangle text-warning mb-3';
    } else {
        titleEl.innerText = 'Thông báo';
        iconEl.className = 'bi bi-info-circle text-primary mb-3';
    }

    const modalEl = document.getElementById('customAlertModal');
    const myModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    myModal.show();
}

// Hàm xác nhận tùy biến
function showConfirm(message, callback) {
    const msgEl = document.getElementById('customConfirmMessage');
    const btnConfirm = document.getElementById('customConfirmBtn');
    
    if (!msgEl || !btnConfirm) {
        if(confirm(message)) callback();
        return;
    }

    msgEl.innerText = message;
    
    // Xóa event listener cũ để tránh lặp lại
    const newBtnConfirm = btnConfirm.cloneNode(true);
    btnConfirm.parentNode.replaceChild(newBtnConfirm, btnConfirm);
    
    const modalEl = document.getElementById('customConfirmModal');
    const myModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    
    newBtnConfirm.addEventListener('click', function() {
        myModal.hide();
        callback();
    });
    
    myModal.show();
}

// Khởi tạo ngay khi tải trang
document.addEventListener('DOMContentLoaded', function () {
    const firstPlane = document.querySelector('#planeDropdown .dropdown-item');
    if (firstPlane) {
        firstPlane.click();
    }
});

function selectPlane(maLoaiMb, tenLoaiMb, soLuongGhe = 0) {
    currentPlaneId = maLoaiMb;
    currentPlaneName = tenLoaiMb;
    currentPlaneTotalSeats = soLuongGhe;
    
    document.getElementById('aircraftName').innerText = tenLoaiMb;
    
    const modalPlaneName = document.getElementById('modalPlaneName');
    if (modalPlaneName) modalPlaneName.innerText = tenLoaiMb;
    
    const modalTotalSeats = document.getElementById('modalTotalSeats');
    if (modalTotalSeats) modalTotalSeats.innerText = soLuongGhe;
    
    const maxSeatsCount = document.getElementById('maxSeatsCount');
    if (maxSeatsCount) maxSeatsCount.innerText = soLuongGhe;
    
    loadSoDoGhe(maLoaiMb);
}

function loadSoDoGhe(maLoaiMb) {
    const container = document.getElementById('seatMapContainer');
    container.innerHTML = '<div class="text-center my-5"><div class="spinner-border text-primary" role="status"></div><br/>Đang tải sơ đồ ghế...</div>';
    
    fetch(`/Admin/Flight/LaySoDoGhe?maLoaiMb=${maLoaiMb}`)
        .then(response => response.json())
        .then(data => {
            if(data.success && data.data && data.data.length > 0) {
                renderSoDoGheDaLuu(data.data);
            } else {
                container.innerHTML = '<div class="text-muted m-auto text-center"><i class="bi bi-grid-3x3-gap display-1 opacity-25"></i><br>Chưa có sơ đồ.<br>Sử dụng công cụ bên phải để tạo.</div>';
                document.getElementById('totalSeatsDisplay').innerText = "0";
                selectedSeats = [];
                updatePropertyPanel();
            }
        })
        .catch(err => {
            console.error(err);
            container.innerHTML = '<div class="text-danger m-auto text-center"><i class="bi bi-exclamation-triangle display-1 opacity-25"></i><br>Lỗi khi tải sơ đồ.</div>';
        });
}

function getClassRank(className) {
    if(!className) return 0;
    const name = className.toLowerCase();
    if(name.includes('nhất')) return 100;
    if(name.includes('thương gia')) return 80;
    if(name.includes('đặc biệt')) return 60;
    if(name.includes('phổ thông')) return 40;
    return 10;
}

const colLabels = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L'];

function renderSoDoGheDaLuu(seats) {
    const container = document.getElementById('seatMapContainer');
    let html = '<div class="w-100 p-4">';
    
    // Tìm maxCot để vẽ Header
    let maxCot = 0;
    seats.forEach(s => { if(s.viTriCot > maxCot) maxCot = s.viTriCot; });
    if(maxCot === 0) maxCot = 6; 

    // Header Cột
    const mid = Math.floor(maxCot / 2);
    html += `<div class="d-flex justify-content-center align-items-center mb-4 text-muted fw-bold" style="font-size: 13px; gap: 8px;">`;
    for(let i = 0; i < mid; i++) html += `<div style="width: 40px;" class="text-center">${colLabels[i]}</div>`;
    html += `<div style="width: 40px;"></div>`; // Aisle space
    for(let i = mid; i < maxCot; i++) html += `<div style="width: 40px;" class="text-center">${colLabels[i]}</div>`;
    html += `</div>`;

    const classGroups = {};
    seats.forEach(seat => {
        const classId = seat.maHangGhe || "OTHER";
        if(!classGroups[classId]) {
            classGroups[classId] = {
                id: classId,
                name: seat.tenHangGhe || "Khác",
                rank: getClassRank(seat.tenHangGhe),
                color: seat.mauSac || "#ccc",
                seats: []
            };
        }
        classGroups[classId].seats.push(seat);
    });

    const sortedGroups = Object.values(classGroups).sort((a, b) => b.rank - a.rank);
    let totalSeatsCount = 0;

    sortedGroups.forEach(group => {
        const rowMap = {};
        group.seats.forEach(seat => {
            let r = seat.viTriHang || 0;
            if(!rowMap[r]) rowMap[r] = [];
            rowMap[r].push(seat);
        });

        const rows = Object.keys(rowMap).map(Number).sort((a,b) => a - b);
        html += `<div class="cabin-title fw-bold text-center my-4" style="color: ${group.color}; letter-spacing: 2px;"><i class="bi bi-star-fill me-2"></i>${group.name.toUpperCase()}</div>`;

        rows.forEach(r => {
            html += `<div class="seat-row w-100 d-flex justify-content-center mb-3" style="gap: 8px;">`;
            const rowSeats = rowMap[r];
            
            for(let j = 1; j <= maxCot; j++) {
                const seat = rowSeats.find(s => s.viTriCot === j);
                if(seat) {
                    const statusClass = seat.trangThai ? '' : 'seat-status-maintenance';
                    const sColor = seat.trangThai === false ? "#6c757d" : group.color;
                    const isDark = sColor === "#dc3545" || sColor === "#0d6efd" || sColor === "#6c757d";
                    const textColor = isDark ? "text-white" : "text-dark";
                    
                    const displayNo = r + colLabels[j - 1];

                    html += `
                        <div class="seat-wrapper ${statusClass}" id="seat_${seat.maGhe}" 
                             data-id="${seat.maGhe}" data-seat="${displayNo}" data-mahangghe="${seat.maHangGhe}" 
                             data-phuthu="${seat.phuThu}" data-hang="${r}" data-cot="${j}" 
                             onclick="toggleSeat('${seat.maGhe}')">
                            <div style="width: 40px; height: 42px; border-width: 2px; border-style: solid; border-radius: 8px; background-color: ${sColor}; border-color: ${sColor};" 
                                 class="seat-shape d-flex align-items-center justify-content-center fw-bold small ${textColor}">
                                 ${displayNo}
                            </div>
                        </div>`;
                    totalSeatsCount++;
                } else {
                    html += `<div class="empty-space" style="width: 40px; height: 42px;"></div>`;
                }

                if(j === mid) {
                    html += `<div class="aisle-number text-center" style="width: 40px; display: flex; justify-content: center; align-items: center; background: #f8fafc; border-radius: 4px; font-size: 11px; color: #94a3b8; font-weight: bold;">${r}</div>`;
                }
            }
            html += `</div>`;
        });
    });

    html += '</div>';
    container.innerHTML = html;
    document.getElementById('totalSeatsDisplay').innerText = totalSeatsCount;
    selectedSeats = [];
    updatePropertyPanel();
}

function generateAutoLayout() {
    const totalAllocated = calculateTotalAllocation();
    if(currentPlaneTotalSeats > 0 && totalAllocated > currentPlaneTotalSeats) {
        showNotification(`Phân bổ (${totalAllocated}) vượt quá sức chứa (${currentPlaneTotalSeats})!`, 'error');
        return;
    }

    const container = document.getElementById('seatMapContainer');
    let html = '';
    let totalSeatsRendered = 0;
    let currentRow = 1;

    const allocationElements = Array.from(document.querySelectorAll('.allocation-item'));
    const sortedAllocations = allocationElements.sort((a, b) => {
        const nameA = a.getAttribute('data-class-name');
        const nameB = b.getAttribute('data-class-name');
        return getClassRank(nameB) - getClassRank(nameA);
    });
    
    let maxCot = 4;
    sortedAllocations.forEach(item => {
        if(parseInt(item.querySelector('.seat-allocation-input').value) > 0) {
            const val = item.querySelector('.seat-layout-select').value;
            if(val === "3-3" && maxCot < 6) maxCot = 6;
        }
    });

    const mid = Math.floor(maxCot / 2);
    html += `<div class="w-100 p-4"><div class="d-flex justify-content-center align-items-center mb-4 text-muted fw-bold" style="font-size: 13px; gap: 8px;">`;
    for(let i = 0; i < mid; i++) html += `<div style="width: 40px;" class="text-center">${colLabels[i]}</div>`;
    html += `<div style="width: 40px;"></div>`;
    for(let i = mid; i < maxCot; i++) html += `<div style="width: 40px;" class="text-center">${colLabels[i]}</div>`;
    html += `</div>`;

    sortedAllocations.forEach(item => {
        const count = parseInt(item.querySelector('.seat-allocation-input').value) || 0;
        if(count <= 0) return;
        
        const className = item.getAttribute('data-class-name');
        const classId = item.getAttribute('data-class-id');
        const color = item.getAttribute('data-color');
        const layoutVal = item.querySelector('.seat-layout-select').value;
        const seatsPerRow = layoutVal === "2-2" ? 4 : 6;
        
        html += `<div class="cabin-title fw-bold text-center my-4" style="color: ${color}; letter-spacing: 2px;"><i class="bi bi-star-fill me-2"></i>KHOANG ${className.toUpperCase()}</div>`;
        
        let seatsRemaining = count;
        while(seatsRemaining > 0) {
            if (currentRow === 13) { currentRow++; continue; }
            
            html += `<div class="seat-row w-100 d-flex justify-content-center mb-3" style="gap: 8px;">`;
            let drawnInRow = 0;
            let seatsToDraw = Math.min(seatsPerRow, seatsRemaining);

            for(let j = 1; j <= maxCot; j++) {
                let shouldDraw = false;
                if(seatsPerRow === 6) {
                    if(drawnInRow < seatsToDraw) shouldDraw = true;
                } else { 
                    if(maxCot === 6) {
                        if((j === 1 || j === 2 || j === 5 || j === 6) && drawnInRow < seatsToDraw) shouldDraw = true;
                    } else {
                        if(drawnInRow < seatsToDraw) shouldDraw = true;
                    }
                }

                if(shouldDraw) {
                    let seatId = currentRow + colLabels[j - 1];
                    const isDark = color === "#dc3545" || color === "#0d6efd" || color === "#6c757d";
                    const textColor = isDark ? "text-white" : "text-dark";
                    let tempId = Date.now() + Math.random();

                    html += `
                        <div class="seat-wrapper" id="seat_${tempId}" 
                             data-seat="${seatId}" data-mahangghe="${classId}" 
                             data-phuthu="0" data-hang="${currentRow}" data-cot="${j}" 
                             onclick="toggleSeat('${tempId}')">
                            <div style="width: 40px; height: 42px; border-width: 2px; border-style: solid; border-radius: 8px; background-color: ${color || '#eee'}; border-color: ${color || '#ccc'};" 
                                 class="seat-shape d-flex align-items-center justify-content-center fw-bold small ${textColor}">
                                 ${seatId}
                            </div>
                        </div>`;
                    drawnInRow++;
                    totalSeatsRendered++;
                } else {
                    html += `<div class="empty-space" style="width: 40px; height: 42px;"></div>`;
                }

                if(j === mid) {
                    html += `<div class="aisle-number text-center" style="width: 40px; display: flex; justify-content: center; align-items: center; background: #f8fafc; border-radius: 4px; font-size: 11px; color: #94a3b8; font-weight: bold;">${currentRow}</div>`;
                }
            }
            html += `</div>`;
            seatsRemaining -= drawnInRow;
            currentRow++;
        }
    });

    html += '</div>';
    container.innerHTML = html || '<div class="text-center m-auto opacity-25">Sơ đồ trống</div>';
    document.getElementById('totalSeatsDisplay').innerText = totalSeatsRendered;
    container.scrollTop = 0;

    const modalInstance = bootstrap.Modal.getInstance(document.getElementById('autoLayoutModal'));
    if (modalInstance) modalInstance.hide();
    clearSelection();
}

function saveConfiguration(showSuccessNotify = true) {
    const total = document.getElementById('totalSeatsDisplay').innerText;
    if (total === "0" || isNaN(parseInt(total))) {
        if(showSuccessNotify) showNotification("Sơ đồ trống!", "warning");
        return;
    }
    if (!currentPlaneId) return;

    const seatWrappers = document.querySelectorAll('.seat-wrapper');
    const danhSachGhe = Array.from(seatWrappers).map(seat => ({
        SoGhe: seat.getAttribute('data-seat'),
        MaHangGhe: seat.getAttribute('data-mahangghe'),
        PhuThu: parseFloat(seat.getAttribute('data-phuthu')) || 0,
        TrangThai: !seat.classList.contains('seat-status-maintenance'),
        ViTriHang: parseInt(seat.getAttribute('data-hang')),
        ViTriCot: parseInt(seat.getAttribute('data-cot'))
    }));

    fetch('/Admin/Flight/LuuSoDoGhe', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ MaLoaiMb: currentPlaneId, DanhSachGhe: danhSachGhe })
    })
    .then(res => res.json())
    .then(data => {
        if(data.success) {
            if(showSuccessNotify) showNotification(data.message, "success");
            loadSoDoGhe(currentPlaneId);
        } else {
            showNotification("Lỗi: " + data.message, "error");
        }
    })
    .catch(err => {
        console.error(err);
        showNotification("Lỗi kết nối!", "error");
    });
}

function calculateTotalAllocation() {
    let total = 0;
    document.querySelectorAll('.seat-allocation-input').forEach(input => total += parseInt(input.value) || 0);
    const countEl = document.getElementById('allocatedSeatsCount');
    if (countEl) {
        countEl.innerText = total;
        countEl.className = (total > currentPlaneTotalSeats) ? 'text-danger fw-bold' : 'text-primary fw-bold';
    }
    return total;
}

function autoFillRemaining() {
    if (currentPlaneTotalSeats <= 0) return;
    const inputs = document.querySelectorAll('.seat-allocation-input');
    if(inputs.length === 0) return;
    let totalBefore = 0;
    for(let i = 0; i < inputs.length - 1; i++) totalBefore += parseInt(inputs[i].value) || 0;
    const remaining = currentPlaneTotalSeats - totalBefore;
    if(remaining >= 0) {
        inputs[inputs.length - 1].value = remaining;
        calculateTotalAllocation();
    }
}

function toggleSeat(seatId) {
    const seatEl = document.getElementById('seat_' + seatId);
    const index = selectedSeats.indexOf(seatId);
    if (index > -1) {
        selectedSeats.splice(index, 1);
        seatEl.classList.remove('selected');
    } else {
        selectedSeats.push(seatId);
        seatEl.classList.add('selected');
    }
    updatePropertyPanel();
}

function clearSelection() {
    selectedSeats.forEach(id => {
        const el = document.getElementById('seat_' + id);
        if(el) el.classList.remove('selected');
    });
    selectedSeats = [];
    updatePropertyPanel();
}

function updatePropertyPanel() {
    const title = document.getElementById('selectedCountText');
    if (selectedSeats.length === 0) {
        title.innerText = "Chưa chọn ghế nào";
        document.getElementById('propExtraPrice').value = "";
    } else if (selectedSeats.length === 1) {
        const seatEl = document.getElementById('seat_' + selectedSeats[0]);
        if (seatEl) {
            title.innerText = "Đang chỉnh sửa Ghế " + seatEl.getAttribute('data-seat');
            document.getElementById('propSeatClass').value = seatEl.getAttribute('data-mahangghe');
            const pt = seatEl.getAttribute('data-phuthu');
            document.getElementById('propExtraPrice').value = pt !== "0" ? pt : "";
            document.getElementById('propStatus').value = seatEl.classList.contains('seat-status-maintenance') ? 'maintenance' : 'active';
        }
    } else {
        title.innerText = "Đang chỉnh sửa " + selectedSeats.length + " ghế";
    }
}

function applyProperties() {
    if (selectedSeats.length === 0) return;
    const classSelect = document.getElementById('propSeatClass');
    const seatClassId = classSelect.value;
    const seatColor = classSelect.options[classSelect.selectedIndex].getAttribute('data-color');
    const phuThu = document.getElementById('propExtraPrice').value || 0;
    const status = document.getElementById('propStatus').value;

    selectedSeats.forEach(id => {
        const seatEl = document.getElementById('seat_' + id);
        if(!seatEl) return;
        seatEl.setAttribute('data-mahangghe', seatClassId);
        seatEl.setAttribute('data-phuthu', phuThu);
        const shapeEl = seatEl.querySelector('.seat-shape');
        if(shapeEl) { 
            shapeEl.style.backgroundColor = seatColor; 
            shapeEl.style.borderColor = seatColor; 
            
            const isDark = seatColor === "#dc3545" || seatColor === "#0d6efd" || seatColor === "#6c757d";
            if (isDark) {
                shapeEl.classList.remove('text-dark');
                shapeEl.classList.add('text-white');
            } else {
                shapeEl.classList.remove('text-white');
                shapeEl.classList.add('text-dark');
            }
        }
        if (status === 'maintenance') seatEl.classList.add('seat-status-maintenance');
        else seatEl.classList.remove('seat-status-maintenance');
    });
    saveConfiguration(false);
    showNotification('Đã cập nhật ' + selectedSeats.length + ' ghế!', "success");
    clearSelection();
}

function clearMap() {
    showConfirm("Bạn có chắc chắn muốn xóa trắng sơ đồ ghế hiện tại không?", function() {
        document.getElementById('seatMapContainer').innerHTML = '<div class="text-muted m-auto text-center"><i class="bi bi-grid-3x3-gap display-1 opacity-25"></i><br>Đã xóa trắng.</div>';
        document.getElementById('totalSeatsDisplay').innerText = "0";
        clearSelection();
    });
}
