(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const modalEl = document.getElementById('notificationModal');
        const openBtn = document.getElementById('createNotificationBtn');
        if (!modalEl || !openBtn) return;

        const modal = new bootstrap.Modal(modalEl);
        const orderId = document.getElementById('orderId')?.value;
        const warnEl = document.getElementById('notificationExistingWarning');

        async function loadExistingNotifications() {
            if (!warnEl) return;
            warnEl.style.display = 'none';
            warnEl.innerHTML = '';

            try {
                const resp = await fetch(`/api/OrderNotificationAPI/GetForOrder/${orderId}`);
                if (!resp.ok) return;
                const data = await resp.json();
                if (!data.success || !data.data || !data.data.length) return;

                const active = data.data.filter(n => !n.isCompleted);
                if (!active.length) return;

                const items = active
                    .map(n => `<li><strong>${escapeHtml(n.title)}</strong> — до ${formatDate(n.dueDate)}</li>`)
                    .join('');

                warnEl.innerHTML =
                    `<i class="bi bi-exclamation-triangle-fill me-2"></i>
                     У этого заказа уже есть активные напоминания (${active.length}):
                     <ul class="mb-0 mt-2">${items}</ul>
                     <div class="mt-2 small text-body-secondary">
                        Вы можете создать ещё одно, если это действительно нужно.
                     </div>`;
                warnEl.style.display = 'block';
            } catch (e) {
                console.warn('Failed to load existing notifications:', e);
            }
        }

        function escapeHtml(str) {
            return String(str ?? '').replace(/[&<>"]/g, m =>
                ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[m]);
        }

        function formatDate(iso) {
            if (!iso) return '';
            const d = new Date(iso);
            if (isNaN(d.getTime())) return iso;
            return d.toLocaleDateString('ru-RU');
        }

        openBtn.addEventListener('click', async function () {
            const defaultDate = new Date();
            defaultDate.setDate(defaultDate.getDate() + 7);
            document.getElementById('notifDueDate').value =
                defaultDate.toISOString().split('T')[0];
            document.getElementById('notifTitle').value = '';
            document.getElementById('notifDescription').value = '';
            document.getElementById('notificationModalError').style.display = 'none';

            await loadExistingNotifications();
            modal.show();
        });

        document.getElementById('notificationSaveBtn').addEventListener('click', async function () {
            const title = document.getElementById('notifTitle').value.trim();
            const dueDate = document.getElementById('notifDueDate').value;
            const errorEl = document.getElementById('notificationModalError');

            if (!title || !dueDate) {
                errorEl.textContent = 'Заполните заголовок и дату';
                errorEl.style.display = 'block';
                return;
            }

            this.disabled = true;
            const original = this.textContent;
            this.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

            try {
                const resp = await fetch('/api/OrderNotificationAPI/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        orderId: parseInt(orderId, 10),
                        title: title,
                        description: document.getElementById('notifDescription').value.trim() || null,
                        dueDate: dueDate
                    })
                });
                const result = await resp.json();
                if (!resp.ok || !result.success) throw new Error(result.message || 'Ошибка');

                modal.hide();
                if (window.modal) await window.modal.alert('Напоминание создано', 'Успех');
            } catch (e) {
                errorEl.textContent = e.message;
                errorEl.style.display = 'block';
            } finally {
                this.disabled = false;
                this.textContent = original;
            }
        });
    });
})();