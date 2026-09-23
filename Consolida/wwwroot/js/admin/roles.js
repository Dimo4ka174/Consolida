// Модалки для страницы /Roles: редактирование и удаление ролей
document.addEventListener('DOMContentLoaded', function () {
    // Редактирование роли
    document.querySelectorAll('.edit-role-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            const roleName = this.getAttribute('data-role-name');
            document.getElementById('oldRoleName').value = roleName;
            document.getElementById('newRoleName').value = roleName;
        });
    });

    // Удаление роли
    document.querySelectorAll('.delete-role-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            const roleName = this.getAttribute('data-role-name');
            document.getElementById('roleToDeleteName').textContent = roleName;
            document.getElementById('roleNameToDelete').value = roleName;
        });
    });
});