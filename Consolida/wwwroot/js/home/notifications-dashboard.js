(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.notification-complete-btn').forEach(btn => {
            btn.addEventListener('click', async function () {
                const id = this.dataset.notificationId;
                if (!id) return;

                if (window.modal && typeof window.modal.confirm === 'function') {
                    try {
                        const ok = await window.modal.confirm('Отметить напоминание как выполненное?', 'Подтверждение');
                        if (!ok) return;
                    } catch (e) {
                        // если на странице нет #custom-modal — откатываемся на нативный confirm
                        if (!window.confirm('Отметить напоминание как выполненное?')) return;
                    }
                }

                const original = this.innerHTML;
                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

                try {
                    const resp = await fetch(`/api/OrderNotificationAPI/Complete/${id}`, { method: 'POST' });
                    let result = null;
                    try { result = await resp.json(); } catch (_) { /* ignore */ }

                    if (!resp.ok || !result || !result.success) {
                        throw new Error((result && result.message) || `HTTP ${resp.status}`);
                    }

                    // Home: удаляем карточку
                    this.closest('.notif-card')?.remove();
                    // OrderNotifications: удаляем строку
                    this.closest('tr')?.remove();

                    const grid = document.querySelector('.notifications-grid');
                    if (grid && grid.children.length === 0) {
                        document.querySelector('.notifications-dashboard')?.remove();
                    }
                } catch (e) {
                    console.error('Complete error:', e);
                    this.disabled = false;
                    this.innerHTML = original;
                    alert('Не удалось: ' + e.message);
                }
            });
        });

        document.querySelectorAll('.notification-restore-btn').forEach(btn => {
            btn.addEventListener('click', async function () {
                const id = this.dataset.notificationId;
                if (!id) return;

                const original = this.innerHTML;
                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

                try {
                    const resp = await fetch(`/api/OrderNotificationAPI/Restore/${id}`, { method: 'POST' });
                    let result = null;
                    try { result = await resp.json(); } catch (_) { }

                    if (!resp.ok || !result || !result.success) {
                        throw new Error((result && result.message) || `HTTP ${resp.status}`);
                    }

                    location.reload();
                } catch (e) {
                    console.error('Restore error:', e);
                    this.disabled = false;
                    this.innerHTML = original;
                    alert('Не удалось: ' + e.message);
                }
            });
        });
    });
})();