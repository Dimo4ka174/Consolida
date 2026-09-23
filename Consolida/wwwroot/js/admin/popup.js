document.addEventListener("DOMContentLoaded", function () {
    // Удаление пользователя
    document.querySelectorAll('.delete-user-btn').forEach(button => {
        button.addEventListener('click', function () {
            const userId = this.getAttribute('data-user-id');
            const userName = this.getAttribute('data-user-name');

            document.getElementById('userToDeleteName').textContent = userName;
            document.getElementById('userIdToDelete').value = userId;

            new bootstrap.Modal(document.getElementById('deleteUserModal')).show();
        });
    });

    // Сброс пароля
    document.querySelectorAll('.reset-password-btn').forEach(button => {
        button.addEventListener('click', function () {
            const userId = this.getAttribute('data-user-id');
            const userName = this.getAttribute('data-user-name');

            document.getElementById('userToResetName').textContent = userName;
            document.getElementById('userIdToReset').value = userId;

            new bootstrap.Modal(document.getElementById('resetPasswordModal')).show();
        });
    });
});