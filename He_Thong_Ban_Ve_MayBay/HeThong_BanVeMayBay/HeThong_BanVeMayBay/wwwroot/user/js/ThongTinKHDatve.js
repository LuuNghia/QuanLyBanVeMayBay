document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('bookingForm');
    if (!form) return;

    // Lấy ngày giới hạn từ thuộc tính data của input
    const dobInput = document.getElementById('paxDob');
    const maxDateAttr = dobInput ? dobInput.getAttribute('data-max-date') : null;

    // Khởi tạo Flatpickr
    if (dobInput && typeof flatpickr !== 'undefined') {
        flatpickr("#paxDob", {
            dateFormat: "d/m/Y",
            locale: "vn",
            allowInput: true,
            disableMobile: "true"
        });
    }

    form.addEventListener('submit', function(e) {
        e.preventDefault();

        let isValid = true;
        let firstErrorElement = null;

        // Xóa trạng thái lỗi cũ
        form.querySelectorAll('.form-control, .form-select').forEach(el => {
            el.classList.remove('is-invalid-custom');
        });

        // 1. Kiểm tra để trống cho các trường required
        form.querySelectorAll('[required]').forEach(el => {
            if (!el.value || el.value.trim() === '') {
                isValid = false;
                el.classList.add('is-invalid-custom');
                if (!firstErrorElement) firstErrorElement = el;
            }
        });

        // 2. Kiểm tra định dạng Email (Bắt buộc @gmail.com)
        const emailInput = document.getElementById('contactEmail');
        if (emailInput) {
            const emailVal = emailInput.value.trim();
            if (emailVal && !emailVal.endsWith('@gmail.com')) {
                isValid = false;
                emailInput.classList.add('is-invalid-custom');
                if (!firstErrorElement) firstErrorElement = emailInput;
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi định dạng Email',
                        text: 'Email phải có đuôi @gmail.com (Ví dụ: abc@gmail.com)',
                        confirmButtonText: 'Đã hiểu'
                    });
                } else {
                    alert('Email phải có đuôi @gmail.com');
                }
                return;
            }
        }

        // 3. Kiểm tra số điện thoại (10 số, bắt đầu bằng 0)
        const phoneInput = form.querySelector('[name="ContactPhone"]');
        if (phoneInput && phoneInput.value && !/^0[0-9]{9}$/.test(phoneInput.value)) {
            isValid = false;
            phoneInput.classList.add('is-invalid-custom');
            if (!firstErrorElement) firstErrorElement = phoneInput;
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'Số điện thoại không hợp lệ',
                    text: 'Số điện thoại phải có 10 chữ số và bắt đầu bằng số 0',
                    confirmButtonText: 'Đã hiểu'
                });
            } else {
                alert('Số điện thoại không hợp lệ');
            }
            return;
        }

        // 4. Kiểm tra tuổi (>= 18 cho Người lớn)
        const loaiKhach = form.querySelector('[name="LoaiHanhKhach"]').value;
        if (dobInput && dobInput.value && maxDateAttr && loaiKhach === 'Người lớn') {
            const parts = dobInput.value.split('/');
            if (parts.length === 3) {
                const dob = new Date(parts[2], parts[1] - 1, parts[0]);
                const maxDate = new Date(maxDateAttr);
                if (dob > maxDate) {
                    isValid = false;
                    dobInput.classList.add('is-invalid-custom');
                    if (!firstErrorElement) firstErrorElement = dobInput;
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'Lỗi ngày sinh',
                            text: 'Hành khách "Người lớn" phải từ 18 tuổi trở lên',
                            confirmButtonText: 'Đã hiểu'
                        });
                    } else {
                        alert('Hành khách "Người lớn" phải từ 18 tuổi trở lên');
                    }
                    return;
                }
            }
        }

        if (!isValid) {
            if (firstErrorElement) firstErrorElement.focus();
            return;
        }

        // Tất cả hợp lệ -> Submit qua AJAX
        const btnSubmit = form.querySelector('button[type="submit"]');
        const originalBtnContent = btnSubmit.innerHTML;
        
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang xử lý...';

        const formData = new FormData(form);
        const data = {};
        formData.forEach((value, key) => data[key] = value);

        $.ajax({
            url: '/User/Booking/TiepTucThanhToan',
            type: 'POST',
            data: data,
            success: function(response) {
                if (response.success) {
                    window.location.href = response.redirectUrl;
                } else {
                    btnSubmit.disabled = false;
                    btnSubmit.innerHTML = originalBtnContent;
                    Swal.fire('Lỗi', response.message, 'error');
                }
            },
            error: function() {
                btnSubmit.disabled = false;
                btnSubmit.innerHTML = originalBtnContent;
                Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ', 'error');
            }
        });
    });

    // Xóa viền đỏ khi người dùng bắt đầu sửa lại
    form.querySelectorAll('.form-control, .form-select').forEach(el => {
        el.addEventListener('input', function() {
            this.classList.remove('is-invalid-custom');
        });
    });
});
