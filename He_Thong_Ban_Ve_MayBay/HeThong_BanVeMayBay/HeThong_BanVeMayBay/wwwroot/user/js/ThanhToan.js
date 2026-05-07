function startTimer(duration, display) {
    let timer = duration, minutes, seconds;
    const interval = setInterval(function () {
        minutes = parseInt(timer / 60, 10);
        seconds = parseInt(timer % 60, 10);

        minutes = minutes < 10 ? "0" + minutes : minutes;
        seconds = seconds < 10 ? "0" + seconds : seconds;

        display.textContent = minutes + ":" + seconds;

        if (--timer < 0) {
            clearInterval(interval);
            display.textContent = "HẾT HẠN";
            Swal.fire({
                icon: 'warning',
                title: 'Hết thời gian thanh toán',
                text: 'Vui lòng thực hiện lại quy trình đặt vé.',
                confirmButtonText: 'Quay lại'
            }).then(() => {
                window.location.href = '/User/Home/Index';
            });
        }
    }, 1000);
}

document.addEventListener('DOMContentLoaded', function () {
    // Khởi tạo đếm ngược 10 phút
    const tenMinutes = 60 * 10,
        display = document.querySelector('#time');
    if (display) startTimer(tenMinutes, display);

    const btnConfirmPay = document.getElementById('btnConfirmPay');
    if (btnConfirmPay) {
        btnConfirmPay.addEventListener('click', function () {
            const btn = this;
            const originalHtml = btn.innerHTML;

            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang xác nhận...';

            $.ajax({
                url: '/User/Booking/XacNhanThanhToan',
                type: 'POST',
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Thanh toán thành công!',
                            text: 'Yêu cầu của bạn đã được xử lý. Vé điện tử đã được xuất.',
                            confirmButtonText: '<i class="bi bi-ticket-perforated me-2"></i>Xem vé ngay',
                            confirmButtonColor: '#003580',
                            allowOutsideClick: false
                        }).then((result) => {
                            if (result.isConfirmed) {
                                window.location.href = '/User/Booking/LichSuDatVe';
                            }
                        });
                    } else {
                        btn.disabled = false;
                        btn.innerHTML = originalHtml;
                        Swal.fire('Lỗi', response.message, 'error');
                    }
                },
                error: function () {
                    btn.disabled = false;
                    btn.innerHTML = originalHtml;
                    Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ', 'error');
                }
            });
        });
    }
});

function selectMethod(el) {
    document.querySelectorAll('.method-item').forEach(m => m.classList.remove('active'));
    el.classList.add('active');
}
