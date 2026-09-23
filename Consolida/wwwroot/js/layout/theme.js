(function () {
    'use strict';

    const STORAGE_KEY = 'routecost-theme';
    const HTML = document.documentElement;

    // Применяем тему как можно раньше — до DOMContentLoaded,
    // чтобы не мигало при загрузке
    function getInitialTheme() {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved === 'light' || saved === 'dark') return saved;

        // Системная тема
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    function applyTheme(theme) {
        HTML.setAttribute('data-theme', theme);
        updateToggleButton(theme);
    }

    function updateToggleButton(theme) {
        const btn = document.getElementById('themeToggle');
        if (!btn) return;
        // Иконка и aria-label
        if (theme === 'dark') {
            btn.setAttribute('aria-label', 'Включить светлую тему');
            btn.title = 'Светлая тема';
        } else {
            btn.setAttribute('aria-label', 'Включить тёмную тему');
            btn.title = 'Тёмная тема';
        }
    }

    function toggleTheme() {
        const current = HTML.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem(STORAGE_KEY, next);
        applyTheme(next);
    }

    // Применяем тему немедленно (до отрисовки)
    applyTheme(getInitialTheme());

    // Обработчик кнопки
    document.addEventListener('DOMContentLoaded', function () {
        const btn = document.getElementById('themeToggle');
        if (btn) {
            btn.addEventListener('click', toggleTheme);
            // Обновим иконку в зависимости от текущей темы
            updateToggleButton(HTML.getAttribute('data-theme'));
        }
    });

    // Опционально: следим за системной темой,
    // если пользователь не выбрал вручную
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
            if (!localStorage.getItem(STORAGE_KEY)) {
                applyTheme(e.matches ? 'dark' : 'light');
            }
        });
    }
})();