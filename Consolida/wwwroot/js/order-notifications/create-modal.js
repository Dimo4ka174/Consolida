(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const modalEl = document.getElementById('createNotifPageModal');
        const openBtn = document.getElementById('openCreateNotifBtn');
        if (!modalEl || !openBtn) return;

        const modal = new bootstrap.Modal(modalEl);
        const orderInput = document.getElementById('createNotifOrderNumber');
        const orderIdInput = document.getElementById('createNotifOrderId');
        const suggestions = document.getElementById('createNotifOrderSuggestions');
        const errorEl = document.getElementById('createNotifError');

        let searchTimeout = null;

        openBtn.addEventListener('click', function () {
            const defaultDate = new Date();
            defaultDate.setDate(defaultDate.getDate() + 7);

            orderInput.value = '';
            orderIdInput.value = '';
            document.getElementById('createNotifTitle').value = '';
            document.getElementById('createNotifDescription').value = '';
            document.getElementById('createNotifDueDate').value =
                defaultDate.toISOString().split('T')[0];
            errorEl.style.display = 'none';
            suggestions.style.display = 'none';

            modal.show();
        });

        // Автодополнение по номеру заказа
        orderInput.addEventListener('input', function () {
            const q = this.value.trim();
            orderIdInput.value = '';

            clearTimeout(searchTimeout);
            if (q.length < 1) {
                suggestions.style.display = 'none';
                return;
            }

            searchTimeout = setTimeout(async () => {
                try {
                    const resp = await fetch(
                        `/api/OrderNotificationAPI/SearchOrders?query=${encodeURIComponent(q)}`
                    );
                    if (!resp.ok) return;
                    const list = await resp.json();
                    renderSuggestions(list);
                } catch (e) {
                    console.warn('Order search failed:', e);
                }
            }, 250);
        });

        function renderSuggestions(list) {
            if (!Array.isArray(list) || list.length === 0) {
                suggestions.style.display = 'none';
                suggestions.innerHTML = '';
                return;
            }

            suggestions.innerHTML = list.map(o => `
                <div class="list-group-item" data-id="${o.id}" data-number="${escapeHtml(o.orderNumber)}">
                    №${escapeHtml(o.orderNumber)}
                </div>
            `).join('');
            suggestions.style.display = 'block';

            suggestions.querySelectorAll('.list-group-item').forEach(item => {
                item.addEventListener('click', () => {
                    orderIdInput.value = item.dataset.id;
                    orderInput.value = item.dataset.number;
                    suggestions.style.display = 'none';
                });
            });
        }

        // Закрыть список при клике вне
        document.addEventListener('click', (e) => {
            if (!suggestions.contains(e.target) && e.target !== orderInput) {
                suggestions.style.display = 'none';
            }
        });

        function escapeHtml(str) {
            return String(str ?? '').replace(/[&<>"]/g, m =>
                ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[m]);
        }

        document.getElementById('createNotifSaveBtn').addEventListener('click', async function () {
            const orderId = parseInt(orderIdInput.value, 10);
            const title = document.getElementById('createNotifTitle').value.trim();
            const dueDate = document.getElementById('createNotifDueDate').value;

            if (!orderId || !title || !dueDate) {
                errorEl.textContent = 'Выберите заказ, заполните заголовок и дату';
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
                        orderId: orderId,
                        title: title,
                        description: document.getElementById('createNotifDescription').value.trim() || null,
                        dueDate: dueDate
                    })
                });
                const result = await resp.json();
                if (!resp.ok || !result.success) throw new Error(result.message || 'Ошибка');

                modal.hide();
                location.reload();
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