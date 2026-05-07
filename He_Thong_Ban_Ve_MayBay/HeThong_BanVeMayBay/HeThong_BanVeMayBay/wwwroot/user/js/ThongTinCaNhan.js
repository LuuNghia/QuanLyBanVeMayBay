$(document).ready(function () {
    // Khởi tạo Flatpickr cho ngày sinh
    flatpickr("#txtBirthDate", {
        dateFormat: "d/m/Y",
        locale: "vn",
        allowInput: true
    });

    loadProfile();

    // Sự kiện Cập nhật hồ sơ
    $('#btnUpdateProfile').click(function () {
        updateProfile();
    });

    // Sự kiện Đổi mật khẩu
    $('#btnConfirmChangePass').click(function () {
        changePassword();
    });

    // Sự kiện Thay đổi ảnh đại diện
    $('#fileAvatar').change(function () {
        updateAvatar(this);
    });

    // Xử lý chuyển đổi active trong Sidebar
    $('.profile-nav .nav-link').click(function () {
        if ($(this).attr('href') === 'javascript:void(0)' || $(this).data('bs-toggle') === 'modal') {
            $('.profile-nav .nav-link').removeClass('active');
            $(this).addClass('active');
        }
    });
});

function toastDemo(message, bgClass = 'bg-success') {
    if (window.Swal) {
        Swal.fire({
            title: bgClass.includes('danger') ? 'Lỗi' : 'Thành công',
            text: message,
            icon: bgClass.includes('danger') ? 'error' : 'success',
            timer: bgClass.includes('danger') ? 5000 : 2000,
            showConfirmButton: bgClass.includes('danger'),
            showCloseButton: true
        });
        return;
    }
    alert(message);
}

async function loadProfile() {
    try {
        const response = await fetch('/User/Profile/GetProfileAPI');
        const result = await response.json();

        if (result.success) {
            const data = result.data;

            $('#sideFullName').text(data.hoTen || 'Khách hàng');
            $('#sideMembership').text('Thành viên ' + (data.loaiThanhVien || 'Mới'));
            $('#sidePoints').text((data.diemTichLuy || 0).toLocaleString() + ' điểm');
            $('#sideAvatar').attr('src', data.anhDaiDien);

            $('#txtFullName').val(data.hoTen);
            $('#selGender').val(data.gioiTinh || 'Nam');
            
            // Xử lý ngày sinh từ dd-mm-yyyy sang dd/mm/yyyy cho Flatpickr
            if (data.ngaySinh) {
                const parts = data.ngaySinh.split('-');
                if (parts.length === 3) {
                    $('#txtBirthDate').val(`${parts[0]}/${parts[1]}/${parts[2]}`);
                }
            }
            
            $('#txtCccd').val(data.cccd);
            $('#txtEmail').val(data.email);
            $('#txtPhone').val(data.sdt);
            $('#txtAddress').val(data.diaChi);

        } else {
            toastDemo(result.message, 'bg-danger');
        }
    } catch (error) {
        console.error('Error loading profile:', error);
        toastDemo('Lỗi kết nối máy chủ', 'bg-danger');
    }
}

async function updateProfile() {
    const rawDate = $('#txtBirthDate').val(); 
    let formattedDate = null;
    
    if (rawDate) {
        const parts = rawDate.split('/');
        if (parts.length === 3) {
            formattedDate = `${parts[2]}-${parts[1]}-${parts[0]}`;
        }
    }

    const data = {
        hoTen: $('#txtFullName').val(),
        gioiTinh: $('#selGender').val(),
        ngaySinh: formattedDate,
        cccd: $('#txtCccd').val(),
        sdt: $('#txtPhone').val(),
        diaChi: $('#txtAddress').val(),
        email: $('#txtEmail').val()
    };

    if (!data.hoTen || data.hoTen.trim().length < 2) {
        toastDemo('Họ tên không hợp lệ (tối thiểu 2 ký tự)', 'bg-danger');
        return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!data.email || !emailRegex.test(data.email)) {
        toastDemo('Email không đúng định dạng', 'bg-danger');
        return;
    }

    const phoneRegex = /(84|0[3|5|7|8|9])+([0-9]{8})\b/;
    if (!data.sdt || !phoneRegex.test(data.sdt)) {
        toastDemo('Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)', 'bg-danger');
        return;
    }

    const cccdRegex = /^[0-9]{9,12}$/;
    if (!data.cccd || !cccdRegex.test(data.cccd)) {
        toastDemo('Số CCCD/Passport không hợp lệ (9-12 chữ số)', 'bg-danger');
        return;
    }

    const btn = $('#btnUpdateProfile');
    const originalText = btn.text();
    btn.prop('disabled', true).text('ĐANG XỬ LÝ...');

    try {
        const response = await fetch('/User/Profile/UpdateProfileAPI', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.success) {
            toastDemo(result.message);
            $('#sideFullName').text(data.hoTen);
        } else {
            toastDemo(result.message, 'bg-danger');
        }
    } catch (error) {
        toastDemo('Lỗi khi cập nhật hồ sơ', 'bg-danger');
    } finally {
        btn.prop('disabled', false).text(originalText);
    }
}

async function changePassword() {
    const oldPass = $('#txtOldPass').val();
    const newPass = $('#txtNewPass').val();
    const confirmPass = $('#txtConfirmPass').val();

    if (!oldPass || !newPass) {
        toastDemo('Vui lòng điền đầy đủ thông tin', 'bg-danger');
        return;
    }

    if (newPass !== confirmPass) {
        toastDemo('Xác nhận mật khẩu không khớp', 'bg-danger');
        return;
    }

    try {
        const response = await fetch('/User/Profile/ChangePasswordAPI', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ oldPassword: oldPass, newPassword: newPass })
        });
        const result = await response.json();

        if (result.success) {
            toastDemo(result.message);
            $('#changePassModal').modal('hide');
            $('#changePassForm')[0].reset();
        } else {
            toastDemo(result.message, 'bg-danger');
        }
    } catch (error) {
        toastDemo('Lỗi khi đổi mật khẩu', 'bg-danger');
    }
}

async function updateAvatar(input) {
    if (!input.files || !input.files[0]) return;

    const formData = new FormData();
    formData.append('avatar', input.files[0]);

    try {
        const response = await fetch('/User/Profile/UpdateAvatarAPI', {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        if (result.success) {
            toastDemo(result.message);
            $('#sideAvatar').attr('src', result.url);
        } else {
            toastDemo(result.message, 'bg-danger');
        }
    } catch (error) {
        toastDemo('Lỗi khi tải ảnh lên', 'bg-danger');
    }
}
