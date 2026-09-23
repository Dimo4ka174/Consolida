// Логика «Выбрать все» для страницы /Roles/ManageClaims
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.claim-group').forEach(function (group) {
        const toggle = group.querySelector('.select-all-toggle');
        const counter = group.querySelector('.claim-group-selected');
        const checkboxes = Array.from(group.querySelectorAll('.claim-checkbox'));

        if (!toggle || checkboxes.length === 0) return;

        function updateCounters() {
            const selected = checkboxes.filter(function (cb) { return cb.checked; }).length;

            if (counter) counter.textContent = selected;

            const allChecked = selected === checkboxes.length;
            const anyChecked = selected > 0;

            toggle.checked = allChecked;
            toggle.indeterminate = anyChecked && !allChecked;
        }

        // Клик на «Выбрать все»
        toggle.addEventListener('change', function () {
            checkboxes.forEach(function (cb) {
                cb.checked = toggle.checked;
            });
            updateCounters();
        });

        // Клик на конкретный чекбокс
        checkboxes.forEach(function (cb) {
            cb.addEventListener('change', updateCounters);
        });

        // Инициализация при загрузке
        updateCounters();
    });
});