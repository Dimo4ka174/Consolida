document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.getElementById('sidebar-nav');
    const sidebar = document.querySelector('.sidebar');
    const backdrop = document.getElementById('sidebarBackdrop');

    if (!toggleBtn || !sidebar || !backdrop) {
        console.warn('[Sidebar] Элементы не найдены');
        return;
    }

    function open() {
        sidebar.classList.add('show');
        toggleBtn.classList.add('active');
        backdrop.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    function close() {
        sidebar.classList.remove('show');
        toggleBtn.classList.remove('active');
        backdrop.classList.remove('show');
        document.body.style.overflow = '';
    }

    function toggle() {
        sidebar.classList.contains('show') ? close() : open();
    }

    // Кнопка гамбургера
    toggleBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        toggle();
    });

    // Клик по подложке
    backdrop.addEventListener('click', close);

    // Escape
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar.classList.contains('show')) {
            close();
        }
    });
});