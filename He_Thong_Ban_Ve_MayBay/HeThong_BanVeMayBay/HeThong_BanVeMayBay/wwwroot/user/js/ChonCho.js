let selectedSeats = [];
const hiddenBasePriceEl = document.getElementById('hiddenBasePrice');
const basePrice = hiddenBasePriceEl ? parseInt(hiddenBasePriceEl.value || 0) : 0;

const selectedSeatsContainer = document.getElementById('selectedSeatsContainer');
const dynamicSeatRows = document.getElementById('dynamicSeatRows');
const billBasePrice = document.getElementById('billBasePrice');
const labelBasePrice = document.getElementById('labelBasePrice');
const billTotal = document.getElementById('billTotal');
const btnConfirm = document.getElementById('btnConfirm');

const condCarryon = document.getElementById('cond-carryon');
const condChecked = document.getElementById('cond-checked');
const condAmenities = document.getElementById('cond-amenities');

document.querySelectorAll('.seat.available').forEach(seat => {
    seat.addEventListener('click', function () {
        const seatId = this.getAttribute('data-seat-id');
        const seatNo = this.getAttribute('data-seat-no');
        const className = this.getAttribute('data-class-name');
        const price = parseInt(this.getAttribute('data-price') || 0);
        const coefficient = parseFloat(this.getAttribute('data-coefficient') || 1.0);
        const color = this.getAttribute('data-color');
        const carryon = this.getAttribute('data-carryon');
        const checked = this.getAttribute('data-checked');
        const amenities = this.getAttribute('data-amenities');

        if (this.classList.contains('selected')) {
            this.classList.remove('selected');
            this.style.backgroundColor = 'white';
            this.querySelector('span').style.color = color;
            selectedSeats = selectedSeats.filter(s => s.id !== seatId);
        } else {
            this.classList.add('selected');
            this.style.backgroundColor = color;
            this.querySelector('span').style.color = 'white';
            selectedSeats.push({ 
                id: seatId, 
                no: seatNo, 
                type: className, 
                price: price,
                coefficient: coefficient,
                color: color,
                carryon: carryon,
                checked: checked,
                amenities: amenities
            });
        }

        updatePanel();
    });
});

function updatePanel() {
    if (selectedSeats.length === 0) {
        resetPanel();
        return;
    }

    // Update Badges
    selectedSeatsContainer.innerHTML = '';
    selectedSeats.forEach(s => {
        const badge = document.createElement('div');
        badge.className = 'badge px-3 py-2 fs-6';
        badge.style.backgroundColor = s.color;
        badge.style.color = 'white';
        badge.innerText = s.no;
        selectedSeatsContainer.appendChild(badge);
    });

    // Update Details (Conditions) - Show from the last selected seat
    const lastSeat = selectedSeats[selectedSeats.length - 1];
    condCarryon.innerText = lastSeat.carryon || "--";
    condChecked.innerText = lastSeat.checked || "--";
    
    if (lastSeat.amenities) {
        const list = lastSeat.amenities.split(',').map(a => a.trim());
        condAmenities.innerHTML = list.map(item => 
            `<div class="d-flex justify-content-between align-items-center mb-1">
                <span class="text-muted"><i class="bi bi-check2-circle text-success me-1"></i> ${item}</span>
            </div>`
        ).join('');
    } else {
        condAmenities.innerHTML = '<div class="text-end text-muted">Cơ bản</div>';
    }

    // Update Billing
    if (labelBasePrice) labelBasePrice.innerText = 'Giá vé (x' + selectedSeats.length + ')';
    
    let totalBase = 0;
    let totalSurcharge = 0;
    
    if (dynamicSeatRows) {
        dynamicSeatRows.innerHTML = '';
        selectedSeats.forEach(s => {
            const seatBasePrice = Math.round(basePrice * s.coefficient);
            totalBase += seatBasePrice;
            totalSurcharge += s.price;
            
            const row = document.createElement('div');
            row.className = 'bill-row animated fadeInDown';
            row.style.fontSize = '13px';
            row.style.marginBottom = '8px';
            row.style.display = 'flex';
            row.style.justifyContent = 'space-between';
            
            row.innerHTML = '<span>Phụ thu ghế <strong>' + s.no + '</strong> (' + s.type + ')</span>' +
                            '<span class=\"fw-bold text-danger\">+' + s.price.toLocaleString('vi-VN') + ' đ</span>';
            dynamicSeatRows.appendChild(row);
        });

        if (billBasePrice) billBasePrice.innerText = totalBase.toLocaleString('vi-VN') + " đ";
        
        const total = totalBase + totalSurcharge;
        if (billTotal) billTotal.innerText = total.toLocaleString('vi-VN') + " đ";
    }

    if (btnConfirm) btnConfirm.disabled = false;
}

function resetPanel() {
    if (selectedSeatsContainer) selectedSeatsContainer.innerHTML = '<div class=\"badge bg-secondary px-3 py-2 fs-6\" id=\"badgeSeatId\">Chưa chọn ghế</div>';
    if (condCarryon) condCarryon.innerText = "--";
    if (condChecked) condChecked.innerText = "--";
    if (condAmenities) condAmenities.innerHTML = '<div class="text-end text-muted">--</div>';

    if (labelBasePrice) labelBasePrice.innerText = "Giá vé (x0)";
    if (billBasePrice) billBasePrice.innerText = "0 đ";
    
    if (dynamicSeatRows) {
        dynamicSeatRows.innerHTML = '<div class=\"bill-row text-muted small\"><span>Phụ thu ghế (Chưa chọn)</span><span>0 đ</span></div>';
    }
    if (billTotal) billTotal.innerText = "0 đ";

    if (btnConfirm) btnConfirm.disabled = true;
}

function submitSelection() {
    if (selectedSeats.length > 0) {
        const seatIds = selectedSeats.map(s => s.id).join(',');
        const seatNos = selectedSeats.map(s => s.no).join(',');
        
        const maCb = document.querySelector('h4.fw-bold').innerText.replace('Chuyến bay ', '').trim();
        window.location.href = '/User/Booking/ThongTinKHDatVe?maCb=' + encodeURIComponent(maCb) + '&seats=' + encodeURIComponent(seatNos) + '&ids=' + encodeURIComponent(seatIds);
    }
}
