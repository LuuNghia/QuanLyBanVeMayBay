function searchFlights() {
    document.getElementById('flight-results-section').style.display = 'block';
    window.scrollTo({ top: document.getElementById('flight-results-section').offsetTop - 100, behavior: 'smooth' });
}

function selectFlight(id, plane, price) {
    document.getElementById('flight-results-section').style.display = 'none';
    document.getElementById('search-area').style.display = 'none';
    document.getElementById('seat-selection-section').style.display = 'block';

    document.getElementById('plane-name').innerText = plane;
    document.getElementById('summary-flight').innerText = id;

    renderSeats(price);
}

function renderSeats(price) {
    const map = document.getElementById('seat-map');
    map.innerHTML = '';
    for (let i = 1; i <= 60; i++) {
        const row = Math.ceil(i / 6);
        const col = String.fromCharCode(64 + (i % 6 === 0 ? 6 : i % 6));
        const seatId = row + col;
        const seat = document.createElement('div');
        seat.className = 'seat';
        if (i <= 12) seat.classList.add('business');
        if (i === 15 || i === 24) seat.classList.add('occupied');

        seat.innerText = seatId;
        seat.onclick = function () {
            if (this.classList.contains('occupied')) return;
            document.querySelectorAll('.seat').forEach(s => s.classList.remove('selected'));
            this.classList.add('selected');
            document.getElementById('selected-seat').innerText = seatId;
            const finalPrice = this.classList.contains('business') ? price * 1.5 : price;
            document.getElementById('total-price').innerText = finalPrice.toLocaleString() + " VNĐ";
        };
        map.appendChild(seat);
    }
}