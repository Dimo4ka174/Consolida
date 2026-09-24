class StatusHistoryModal {
    constructor() {
        this.modalId = 'statusHistoryModal';
        this.abortController = null;
        this.initModal();
        this.attachEvent();
    }

    initModal() {
        this.modalElement = document.getElementById(this.modalId);
        if (!this.modalElement) {
            this.createModal();
        }
        this.modal = $(this.modalElement);
    }

    createModal() {
        const html = `
        <div class="modal fade" id="${this.modalId}" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-xl modal-dialog-scrollable">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">История изменений статуса</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть"></button>
                    </div>
                    <div class="modal-body">
                        <div id="${this.modalId}Loader" class="text-center py-4">
                            <div class="spinner-border text-primary" role="status"><span class="visually-hidden">Загрузка...</span></div>
                        </div>
                        <div id="${this.modalId}Content" style="display: none;">
                            <table class="table table-hover">
                                <thead><tr><th>Дата</th><th>Пользователь</th><th>Статус</th></tr></thead>
                                <tbody id="${this.modalId}TableBody"></tbody>
                            </table>
                        </div>
                        <div id="${this.modalId}Empty" class="text-center text-muted py-4" style="display: none;">
                            <i class="bi bi-clock-history fs-2"></i><p class="mt-2">История изменений отсутствует</p>
                        </div>
                        <div id="${this.modalId}Error" class="alert alert-danger" style="display: none;"></div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Закрыть</button>
                    </div>
                </div>
            </div>
        </div>`;
        document.body.insertAdjacentHTML('beforeend', html);
        this.modalElement = document.getElementById(this.modalId);
        this.modal = $(this.modalElement);
    }

    attachEvent() {
        // Кнопка "История статусов" в заголовке
        const historyBtn = document.querySelector('.status-history-link button');
        if (historyBtn) {
            historyBtn.addEventListener('click', (e) => {
                const orderId = document.getElementById('orderId')?.value;
                if (orderId) this.show(orderId);
            });
        }
    }

    showSection(sectionName) {
        ['Loader', 'Content', 'Empty', 'Error'].forEach(s => {
            const el = document.getElementById(`${this.modalId}${s}`);
            if (el) el.style.display = 'none';
        });
        const target = document.getElementById(`${this.modalId}${sectionName}`);
        if (target) target.style.display = 'block';
    }

    async show(orderId) {
        if (this.abortController) {
            this.abortController.abort();
        }
        this.abortController = new AbortController();
        const signal = this.abortController.signal;

        this.showSection('Loader');
        this.modal.modal('show');

        try {
            const response = await fetch(`/api/OrderAPI/GetOrderStatusHistory/${orderId}`, { signal });
            const result = await response.json();

            if (result.success && result.data?.length > 0) {
                const tbody = document.getElementById(`${this.modalId}TableBody`);
                tbody.innerHTML = result.data.map(item => `
                    <tr>
                        <td class="text-nowrap">${item.changeDate}</td>
                        <td>${item.changedBy || 'System'}</td>
                        <td>
                            <span class="badge bg-secondary">
                                ${item.oldStatusDisplay || item.oldStatus} → ${item.newStatusDisplay || item.newStatus}
                            </span>
                        </td>
                    </tr>
                `).join('');
                this.showSection('Content');
            } else if (result.success) {
                this.showSection('Empty');
            } else {
                this.showError(result.message || 'Ошибка при загрузке истории');
            }
        } catch (error) {
            if (error.name === 'AbortError') return;
            console.error('Error loading status history:', error);
            this.showError('Ошибка соединения с сервером');
        }
    }

    showError(message) {
        const el = document.getElementById(`${this.modalId}Error`);
        if (el) {
            el.textContent = message;
            this.showSection('Error');
        }
    }
}

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    window.statusHistoryModal = new StatusHistoryModal();
});