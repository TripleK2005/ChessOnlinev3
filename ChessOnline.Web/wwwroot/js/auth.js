$(() => {
    // --- Login
    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        $.ajax({
            url: '/Account/Login',
            method: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.success) {
                    window.location.href = '/Home/Index';
                } else {
                    alert(res.message || 'Invalid credentials.');
                }
            },
            error: function () {
                alert('Server error.');
            }
        });
    });

    // --- Register
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();
        const pass = $('[name="Password"]').val();
        const confirm = $('[name="ConfirmPassword"]').val();
        if (pass !== confirm) {
            alert('Passwords do not match.');
            return;
        }
        $.ajax({
            url: '/Account/Register',
            method: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.success) {
                    alert('Registration successful!');
                    window.location.href = '/Account/Login';
                } else {
                    alert(res.message || 'Failed to register.');
                }
            },
            error: function () {
                alert('Server error.');
            }
        });
    });
});
