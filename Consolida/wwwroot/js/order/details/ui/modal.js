class SimpleModal {
    static async confirm(message, title = 'Подтверждение') {
        const modalEl = document.getElementById('custom-modal');

        if (!modalEl) {
            return window.confirm(message);
        }

        const modal = $(modalEl);

        // Настройка содержимого
        document.getElementById('modal-title').textContent = title;
        document.getElementById('modal-message').textContent = message;

        // Управление кнопками
        const cancelBtn = document.getElementById('modal-cancel');
        const confirmBtn = document.getElementById('modal-confirm');

        cancelBtn.style.display = '';
        confirmBtn.style.display = '';
        confirmBtn.textContent = 'Да';
        cancelBtn.textContent = 'Нет';
        confirmBtn.className = 'btn btn-primary';
        cancelBtn.className = 'btn btn-secondary';

        return new Promise((resolve) => {
            const handleConfirm = () => {
                cleanup();
                resolve(true);
            };

            const handleCancel = () => {
                cleanup();
                resolve(false);
            };

            const cleanup = () => {
                confirmBtn.removeEventListener('click', handleConfirm);
                cancelBtn.removeEventListener('click', handleCancel);
                modal.off('shown.bs.modal hidden.bs.modal');
            };

            modal.attr('aria-hidden', 'false').removeAttr('aria-hidden');

            confirmBtn.addEventListener('click', handleConfirm);
            cancelBtn.addEventListener('click', handleCancel);

            modal.modal('show');

            modal.on('shown.bs.modal', () => {
                confirmBtn.focus();
            });
        });
    }

    static async alert(message, title = 'Уведомление', callback = null) {
        const modalEl = document.getElementById('custom-modal');
        if (!modalEl) {
            window.alert(message);
            if (callback && typeof callback === 'function') callback();
            return true;
        }
        const modal = $(modalEl);

        // Настройка содержимого
        document.getElementById('modal-title').textContent = title;
        document.getElementById('modal-message').textContent = message;

        // Управление кнопками
        const cancelBtn = document.getElementById('modal-cancel');
        const confirmBtn = document.getElementById('modal-confirm');

        cancelBtn.style.display = 'none';
        confirmBtn.style.display = '';
        confirmBtn.textContent = 'Подтвердить';
        confirmBtn.className = 'btn btn-create btn-with-check';

        return new Promise((resolve) => {
            const handleConfirm = () => {
                cleanup();
                if (callback && typeof callback === 'function') {
                    callback();
                }
                resolve(true);
            };

            const cleanup = () => {
                confirmBtn.removeEventListener('click', handleConfirm);
                modal.off('shown.bs.modal hidden.bs.modal');
            };

            modal.attr('aria-hidden', 'false').removeAttr('aria-hidden');

            confirmBtn.addEventListener('click', handleConfirm);
            modal.modal('show');

            modal.on('shown.bs.modal', () => {
                confirmBtn.focus();
            });

            modal.on('hidden.bs.modal', () => {
                cleanup();
            });
        });
    }
    static async alertMultiple(messages) {
        const modalEl = document.getElementById('custom-modal');
        if (!modalEl) {
            for (const m of messages) window.alert(`${m.title}\n\n${m.message}`);
            return true;
        }
        const modal = $(modalEl);
        const confirmBtn = document.getElementById('modal-confirm');
        const cancelBtn = document.getElementById('modal-cancel');

        // Настройка кнопок
        cancelBtn.style.display = 'none';
        confirmBtn.style.display = '';
        confirmBtn.textContent = 'Подтвердить';
        confirmBtn.className = 'btn btn-create btn-with-check';

        let currentIndex = 0;

        const showNextMessage = async () => {
            if (currentIndex >= messages.length) {
                modal.modal('hide');
                return;
            }

            const { title, message } = messages[currentIndex++];
            document.getElementById('modal-title').textContent = title;
            document.getElementById('modal-message').textContent = message;

            // Ждем подтверждения текущего сообщения
            await new Promise((resolve) => {
                const handleConfirm = () => {
                    confirmBtn.removeEventListener('click', handleConfirm);
                    resolve();
                };

                confirmBtn.addEventListener('click', handleConfirm);
            });

            // Показываем следующее сообщение
            await showNextMessage();
        };

        return new Promise((resolve) => {
            modal.attr('aria-hidden', 'false').removeAttr('aria-hidden');

            modal.on('hidden.bs.modal', () => {
                modal.off('hidden.bs.modal');
                resolve(true);
            });

            modal.modal('show');
            showNextMessage().catch(console.error);
        });
    }
}

// Инициализация
if (!window.modal) {
    window.modal = SimpleModal;
}