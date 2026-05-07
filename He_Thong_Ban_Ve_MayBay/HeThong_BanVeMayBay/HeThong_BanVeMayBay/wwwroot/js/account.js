$(document).ready(function () {
    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $('#btnLogin');
        const originalText = btn.text();

        btn.html('<span class="spinner-border spinner-border-sm"></span> Đang xử lý...').prop('disabled', true);

        $.ajax({
            url: '/Account/Login',
            type: 'POST',
            data: {
                username: $('#username').val(),
                password: $('#password').val()
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Đăng nhập thành công!',
                        text: 'Chào mừng bạn quay trở lại!',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.href = response.redirectUrl;
                    });
                } else {
                    btn.text(originalText).prop('disabled', false);
                    Swal.fire('Thất bại', response.message, 'error');
                }
            },
            error: function () {
                btn.text(originalText).prop('disabled', false);
                Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ.', 'error');
            }
        });
    });

    // Xử lý Đăng ký
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $('#btnRegister');
        const originalText = btn.text();

        btn.html('<span class="spinner-border spinner-border-sm"></span> Đang xử lý...').prop('disabled', true);

        $.ajax({
            url: '/Account/Register',
            type: 'POST',
            data: {
                fullName: $('#fullName').val(),
                username: $('#username').val(),
                password: $('#password').val(),
                email: $('#email').val(),
                phone: $('#phone').val(),
                cccd: $('#cccd').val()
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Đăng ký thành công!',
                        text: response.message,
                        timer: 2000,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.href = '/Account/Login';
                    });
                } else {
                    btn.text(originalText).prop('disabled', false);
                    Swal.fire('Thất bại', response.message, 'error');
                }
            },
            error: function () {
                btn.text(originalText).prop('disabled', false);
                Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ.', 'error');
            }
        });
    });
});
