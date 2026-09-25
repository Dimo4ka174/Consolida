document.addEventListener('DOMContentLoaded', function () {
    // Выбор диапазона недель
    const weekSpanSelect = document.getElementById('weekSpanSelect');
    if (weekSpanSelect) {
        weekSpanSelect.addEventListener('change', function () {
            const weightLimit = document.getElementById('weightLimitSelect')?.value || '';
            const url = `/Consolidation?weekSpan=${this.value}${weightLimit ? '&weightLimit=' + weightLimit : ''}`;
            window.location.href = url;
        });
    }

    // Выбор лимита веса
    const weightLimitSelect = document.getElementById('weightLimitSelect');
    if (weightLimitSelect) {
        weightLimitSelect.addEventListener('change', function () {
            const weekSpan = document.getElementById('weekSpanSelect')?.value || 1;
            const url = `/Consolidation?weekSpan=${weekSpan}${this.value ? '&weightLimit=' + this.value : ''}`;
            window.location.href = url;
        });
    }

    // Модалка добавления лимита веса
    const addWeightLimitBtn = document.getElementById('addWeightLimitBtn');
    if (addWeightLimitBtn) {
        addWeightLimitBtn.addEventListener('click', function () {
            document.getElementById('newWeightLimitValue').value = '';
            new bootstrap.Modal(document.getElementById('addWeightLimitModal')).show();
        });
    }

    document.getElementById('confirmAddWeightLimit')?.addEventListener('click', async function () {
        const input = document.getElementById('newWeightLimitValue');
        const value = parseFloat(input.value);
        if (!value || value <= 0 || value > 100000) {
            alert('Введите корректное значение от 1 до 100000 кг');
            return;
        }

        const formData = new FormData();
        formData.append('value', value);

        const response = await fetch('/Consolidation/AddWeightLimit', {
            method: 'POST',
            body: formData
        });

        if (response.ok) window.location.reload();
        else {
            const err = await response.text();
            alert('Ошибка: ' + err);
        }
    });

    // Подсветка карточек по чипам
    const chips = document.querySelectorAll('.suggestion-chip');
    const cards = document.querySelectorAll('[data-order-id]');
    chips.forEach(chip => {
        chip.addEventListener('click', function () {
            const ids = JSON.parse(this.dataset.orderIds);
            cards.forEach(card => card.classList.remove('highlight'));
            cards.forEach(card => {
                const id = parseInt(card.dataset.orderId);
                if (ids.includes(id)) card.classList.add('highlight');
            });
        });
    });

    // Принятие подсказки
    document.querySelectorAll('.accept-suggestion').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.stopPropagation();
            const chip = this.previousElementSibling;
            if (!chip || !chip.classList.contains('suggestion-chip')) return;

            const orderIds = JSON.parse(chip.dataset.orderIds);
            const poolId = chip.dataset.poolId ? parseInt(chip.dataset.poolId) : null;

            const response = await fetch('/Consolidation/AcceptSuggestion', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ orderIds, poolId })
            });

            if (response.ok) window.location.reload();
            else {
                const error = await response.json();
                alert('Ошибка: ' + error.message);
            }
        });
    });

    // Ручное создание отгрузки из выбранных заказов
    const createPoolBtn = document.getElementById('createPoolBtn');
    if (createPoolBtn) {
        createPoolBtn.addEventListener('click', async function () {
            const checked = document.querySelectorAll('.order-checkbox:checked');
            const orderIds = Array.from(checked).map(cb => parseInt(cb.value));
            if (orderIds.length < 2) {
                alert('Выберите минимум два заказа');
                return;
            }

            const response = await fetch('/Consolidation/AcceptSuggestion', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ orderIds, poolId: null })
            });

            if (response.ok) window.location.reload();
            else {
                const error = await response.json();
                alert('Ошибка: ' + error.message);
            }
        });
    }

    // Открытие модального окна добавления в отгрузку
    document.querySelectorAll('.btn-add-to-pool').forEach(btn => {
        btn.addEventListener('click', function () {
            document.getElementById('selectedOrderId').value = this.dataset.orderId;
            new bootstrap.Modal(document.getElementById('addToPoolModal')).show();
        });
    });

    // Подтверждение добавления в отгрузку
    document.getElementById('confirmAddToPool')?.addEventListener('click', async function () {
        const orderId = document.getElementById('selectedOrderId').value;
        const poolId = document.getElementById('targetPoolSelect').value;
        if (!orderId || !poolId) {
            alert('Выберите отгрузку');
            return;
        }

        const response = await fetch(
            `/Consolidation/AddOrderToPool?orderId=${orderId}&poolId=${poolId}${getWeightLimitSuffix()}`,
            { method: 'POST' });

        if (response.ok) window.location.reload();
        else {
            const error = await response.text();
            alert('Ошибка: ' + error);
        }
    });

    // Открытие модалки смены статуса
    document.querySelectorAll('.set-pool-status-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const poolId = this.dataset.poolId;
            const targetWeek = parseInt(this.dataset.targetWeek) || 0;
            document.getElementById('poolStatusPoolId').value = poolId;

            const date = new Date();
            date.setDate(date.getDate() + targetWeek * 7);
            document.getElementById('poolDeliveryDate').value = date.toISOString().split('T')[0];

            new bootstrap.Modal(document.getElementById('setPoolStatusModal')).show();
        });
    });

    // Подтверждение смены статуса
    document.getElementById('confirmSetPoolStatus')?.addEventListener('click', async function () {
        const poolId = document.getElementById('poolStatusPoolId').value;
        const newStatusId = parseInt(document.getElementById('poolStatusSelect').value);
        const deliveryDate = document.getElementById('poolDeliveryDate').value;

        const response = await fetch('/Consolidation/SetPoolStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ poolId: parseInt(poolId), newStatus: newStatusId, expectedDeliveryDate: deliveryDate || null })
        });

        if (response.ok) window.location.reload();
        else {
            const error = await response.json();
            alert('Ошибка: ' + error.message);
        }
    });

    // ---------- История отгрузки ----------
    document.querySelectorAll('.pool-history-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const poolId = this.dataset.poolId;
            document.getElementById('poolHistoryTitle').textContent = poolId;

            const response = await fetch(`/Consolidation/GetPoolHistory?poolId=${poolId}`);
            if (!response.ok) {
                alert('Не удалось загрузить историю');
                return;
            }

            const history = await response.json();
            const list = document.getElementById('poolHistoryList');
            const empty = document.getElementById('poolHistoryEmpty');
            list.innerHTML = '';

            if (!history || history.length === 0) {
                empty.style.display = 'block';
            } else {
                empty.style.display = 'none';
                history.forEach(h => {
                    const li = document.createElement('li');
                    li.className = 'pool-history-item';

                    const date = new Date(h.changedAt);
                    const dateStr = date.toLocaleString('ru-RU', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: '2-digit', minute: '2-digit'
                    });

                    li.innerHTML = `
                        <div class="pool-history-header">
                            <span class="pool-history-event">${escapeHtml(h.description)}</span>
                            <span class="pool-history-date">${dateStr}</span>
                        </div>
                        <div class="pool-history-author">${escapeHtml(h.changedBy)}</div>
                    `;
                    list.appendChild(li);
                });
            }

            new bootstrap.Modal(document.getElementById('poolHistoryModal')).show();
        });
    });

    // ---------- Drag and Drop ----------
    let draggedOrderId = null;
    let draggedSource = null;
    let draggedSourcePoolId = null;

    const toolbar = document.getElementById('consolidationToolbar');

    document.addEventListener('dragstart', function (e) {
        const card = e.target.closest('.draggable-order');
        if (!card || card.classList.contains('inactive')
            || card.classList.contains('locked')
            || card.classList.contains('locked-remote')) {
            e.preventDefault();
            return;
        }

        if (e.target.closest('a, button, input, select, form, .btn-icon-only')) {
            e.preventDefault();
            return;
        }

        draggedOrderId = parseInt(card.dataset.orderId);
        draggedSource = card.dataset.source;
        draggedSourcePoolId = card.dataset.poolId ? parseInt(card.dataset.poolId) : null;

        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', draggedOrderId);
        card.classList.add('dragging');

        // Подсветка тулбара и зоны удаления
        if (toolbar) toolbar.classList.add('drag-active');

        if (window.consolidationConnection) {
            window.consolidationConnection
                .invoke('BroadcastOrderDragStart', draggedOrderId)
                .catch(function (err) { console.warn('[SignalR] DragStart broadcast failed:', err); });
        }
    });

    document.addEventListener('dragend', function (e) {
        const card = e.target.closest('.draggable-order');
        if (card) card.classList.remove('dragging');
        document.querySelectorAll('.drop-target').forEach(el => el.classList.remove('drop-target'));

        if (toolbar) toolbar.classList.remove('drag-active');

        if (draggedOrderId && window.consolidationConnection) {
            window.consolidationConnection
                .invoke('BroadcastOrderDragEnd', draggedOrderId)
                .catch(function (err) { console.warn('[SignalR] DragEnd broadcast failed:', err); });
        }

        draggedOrderId = null;
        draggedSource = null;
        draggedSourcePoolId = null;
    });

    document.addEventListener('dragover', function (e) {
        if (!draggedOrderId) return;

        const poolCard = e.target.closest('.pool-card');
        const freeColumn = e.target.closest('.free-column');
        const freeDropZone = e.target.closest('.free-drop-zone');
        const freeOrder = e.target.closest('.free-order');

        if (poolCard) {
            const targetPoolId = parseInt(poolCard.dataset.poolId);
            if (draggedSource === 'pool' && draggedSourcePoolId === targetPoolId) {
                e.dataTransfer.dropEffect = 'none';
            } else {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
                poolCard.classList.add('drop-target');
            }
        } else if (freeOrder) {
            const targetOrderId = parseInt(freeOrder.dataset.orderId);
            if (draggedOrderId !== targetOrderId
                && !freeOrder.classList.contains('locked')
                && !freeOrder.classList.contains('locked-remote')) {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = 'move';
                freeOrder.classList.add('drop-target');
            }
        } else if (freeColumn || freeDropZone) {
            if (draggedSource === 'pool') {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
                (freeColumn || freeDropZone).classList.add('drop-target');
            }
        }
    });

    document.addEventListener('dragleave', function (e) {
        const targets = ['.pool-card', '.free-column', '.free-drop-zone', '.free-order'];
        targets.forEach(selector => {
            const el = e.target.closest(selector);
            if (el) el.classList.remove('drop-target');
        });
    });

    document.addEventListener('drop', async function (e) {
        if (!draggedOrderId) return;
        e.preventDefault();

        const currentDraggedOrderId = draggedOrderId;
        const currentDraggedSource = draggedSource;
        const currentDraggedSourcePoolId = draggedSourcePoolId;

        const poolCard = e.target.closest('.pool-card');
        const freeColumn = e.target.closest('.free-column');
        const freeDropZone = e.target.closest('.free-drop-zone');
        const freeOrder = e.target.closest('.free-order');

        try {
            if (poolCard) {
                const targetPoolId = parseInt(poolCard.dataset.poolId);
                if (currentDraggedSource === 'pool' && currentDraggedSourcePoolId === targetPoolId) return;

                let url;
                if (currentDraggedSource === 'free') {
                    url = `/Consolidation/AddOrderToPool?orderId=${currentDraggedOrderId}&poolId=${targetPoolId}${getWeightLimitSuffix()}`;
                } else if (currentDraggedSource === 'pool') {
                    url = `/Consolidation/MoveOrderToPool?orderId=${currentDraggedOrderId}&targetPoolId=${targetPoolId}${getWeightLimitSuffix()}`;
                }
                await sendRequest(url, { method: 'POST' });
            } else if (freeOrder) {
                const targetOrderId = parseInt(freeOrder.dataset.orderId);
                if (isNaN(targetOrderId) || currentDraggedOrderId === targetOrderId) return;
                if (freeOrder.classList.contains('locked') || freeOrder.classList.contains('locked-remote')) return;

                if (currentDraggedSource === 'free') {
                    await sendRequest('/Consolidation/AcceptSuggestion', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ orderIds: [currentDraggedOrderId, targetOrderId], poolId: null })
                    });
                } else if (currentDraggedSource === 'pool') {
                    await sendRequest(
                        `/Consolidation/RemoveOrderFromPool?orderId=${currentDraggedOrderId}`,
                        { method: 'POST' },
                        false);
                    await sendRequest('/Consolidation/AcceptSuggestion', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ orderIds: [currentDraggedOrderId, targetOrderId], poolId: null })
                    });
                }
            } else if (freeColumn || freeDropZone) {
                if (currentDraggedSource === 'pool') {
                    await sendRequest(`/Consolidation/RemoveOrderFromPool?orderId=${currentDraggedOrderId}`, { method: 'POST' });
                }
            }
        } catch (err) {
            alert(err.message || 'Ошибка при выполнении операции');
            window.location.reload();
        }

        document.querySelectorAll('.drop-target').forEach(el => el.classList.remove('drop-target'));
    });

    function getWeightLimitSuffix() {
        const sel = document.getElementById('weightLimitSelect');
        return sel && sel.value ? `&weightLimit=${sel.value}` : '';
    }

    async function sendRequest(url, options = {}, reload = true) {
        const response = await fetch(url, options);

        if (response.ok) {
            if (reload) window.location.reload();
            return true;
        }

        let message = 'Ошибка при выполнении операции';

        if (response.status === 409) {
            message = 'Объект редактируется другим пользователем';
        }

        try {
            const error = await response.json();
            message = error.message || message;
        } catch {
            try {
                const text = await response.text();
                if (text) message = text;
            } catch { /* ignore */ }
        }

        throw new Error(message);
    }

    function escapeHtml(text) {
        if (text == null) return '';
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }
});