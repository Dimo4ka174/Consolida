(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const statusSelect = document.getElementById('statusSelect');
        if (!statusSelect) return;

        const orderId = statusSelect.dataset.orderId;
        let currentStatusId = statusSelect.value;

        // Словарь переводов
        const statusTranslations = {
            'None': 'Нет статуса',
            'Registered': 'Зарегистрирован',
            'Calculated': 'Произведен расчёт',
            'ExhibitTKP': 'Выставлено ТКП',
            'TransferredProduction': 'Передан в производство',
            'RequiresPayment': 'Требует оплаты',
            'Paid': 'Оплачен',
            'OnTheWay': 'В пути',
            'Shipped': 'Отгружен',
            'OrderClosed': 'Заказ закрыт',
            'Archive': 'Архив'
        };

        function translateStatus(status) {
            return statusTranslations[status] || status;
        }

        // Функция форматирования даты
        function formatDate(dateString) {
            try {
                const date = new Date(dateString);
                if (!isNaN(date.getTime())) {
                    return date.toLocaleString('ru-RU', {
                        day: '2-digit', month: '2-digit', year: '2-digit',
                        hour: '2-digit', minute: '2-digit'
                    }).replace(',', '');
                }
            } catch (e) { }
            return dateString;
        }

        // Обновление информации в кнопке истории
        function updateStatusHistoryInfo(changeDate, changedBy) {
            const historyButton = document.querySelector('.status-history-link .btn');
            if (!historyButton) return;

            const formattedDate = formatDate(changeDate);
            historyButton.title = `Последнее изменение: ${formattedDate}`;

            const historyContent = historyButton.querySelector('.status-history-content');
            if (historyContent) {
                let changerSpan = historyContent.querySelector('.status-changer');
                let dateSpan = historyContent.querySelector('.status-change-date');
                if (!changerSpan) {
                    changerSpan = document.createElement('span');
                    changerSpan.className = 'status-changer';
                    historyContent.appendChild(changerSpan);
                }
                if (!dateSpan) {
                    dateSpan = document.createElement('span');
                    dateSpan.className = 'status-change-date';
                    historyContent.appendChild(dateSpan);
                }
                changerSpan.textContent = changedBy || 'System';
                dateSpan.textContent = `(${formattedDate})`;
            }
        }

        // Подсветка статуса
        function highlightStatusSelect(statusId) {
            const classes = ['status-new', 'status-calculated', 'status-waiting',
                'status-ordered', 'status-delivered', 'status-completed', 'status-cancelled'];
            statusSelect.classList.remove(...classes);
            const map = {
                '1': 'status-new', '2': 'status-calculated', '3': 'status-waiting',
                '4': 'status-ordered', '5': 'status-delivered', '6': 'status-completed',
                '7': 'status-cancelled', '8': 'status-delivered', '9': 'status-completed', '10': 'status-cancelled'
            };
            if (map[statusId]) statusSelect.classList.add(map[statusId]);
        }

        // Показ уведомления
        function showNotification(message, changeDate, changedBy, type = 'success') {
            const container = document.getElementById('globalNotificationContainer');
            if (!container) return;

            const notification = document.createElement('div');
            notification.className = `status-notification status-notification-${type}`;

            let dateHtml = changeDate ? `<div class="status-notification-date">${formatDate(changeDate)}</div>` : '';
            let changerHtml = changedBy ? `<div class="status-notification-changer">Изменил: ${changedBy}</div>` : '';

            const icon = type === 'success' ? '✅' : '❌';
            notification.innerHTML = `
                <div class="status-notification-content">
                    <span class="status-notification-icon">${icon}</span>
                    <div class="status-notification-text-wrapper">
                        <div class="status-notification-message">${message}</div>
                        ${dateHtml} ${changerHtml}
                    </div>
                </div>
                <button class="status-notification-close" aria-label="Закрыть">×</button>
            `;

            container.appendChild(notification);

            const closeBtn = notification.querySelector('.status-notification-close');
            closeBtn.addEventListener('click', () => {
                notification.classList.add('hiding');
                setTimeout(() => notification.remove(), 300);
            });
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.classList.add('hiding');
                    setTimeout(() => notification.remove(), 300);
                }
            }, 5000);
        }

        // Обработчик смены статуса
        statusSelect.addEventListener('change', async function () {
            const newStatusId = this.value;
            if (newStatusId === currentStatusId) return;

            statusSelect.disabled = true;
            statusSelect.classList.add('status-updating');
            const oldValue = currentStatusId;
            currentStatusId = newStatusId;

            try {
                const response = await fetch('/api/OrderAPI/UpdateOrderStatus', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ orderId: parseInt(orderId), statusId: parseInt(newStatusId) })
                });

                if (!response.ok) throw new Error('Ошибка сервера');

                const result = await response.json();

                if (result.success) {
                    const oldTranslated = translateStatus(result.oldStatus);
                    const newTranslated = translateStatus(result.newStatus);
                    showNotification(
                        `Статус изменен с "${oldTranslated}" на "${newTranslated}"`,
                        result.changeDate,
                        result.changedBy || 'System',
                        'success'
                    );
                    updateStatusHistoryInfo(result.changeDate, result.changedBy || 'System');
                    highlightStatusSelect(newStatusId);
                    statusSelect.value = newStatusId;
                } else {
                    statusSelect.value = oldValue;
                    currentStatusId = oldValue;
                    showNotification(result.message || 'Ошибка обновления статуса', null, null, 'danger');
                }
            } catch (error) {
                statusSelect.value = oldValue;
                currentStatusId = oldValue;
                showNotification('Ошибка соединения с сервером', null, null, 'danger');
            } finally {
                statusSelect.disabled = false;
                statusSelect.classList.remove('status-updating');
            }
        });

        // Инициализация подсветки при загрузке
        highlightStatusSelect(currentStatusId);
    });
})();