document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.save-btn-data').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            await handleSaveOrder(this);
        });
    });

    const modalElement = document.getElementById('custom-modal');
    if (modalElement) {
        $(modalElement).on('hidden.bs.modal', function () {
            document.querySelectorAll('.save-btn-data, .save-btn-TKP, .save-btn-calculation').forEach(btn => {
                btn.disabled = false;
                btn.innerHTML = btn.classList.contains('save-btn-TKP') ? 'Скачать ТКП' :
                    btn.classList.contains('save-btn-calculation') ? 'Скачать расчет' :
                        'Сохранить данные';
            });
        });
    }

    async function handleSaveOrder(button) {
        const isDownload = button.classList.contains('save-btn-TKP');
        const orderId = button.dataset.orderId;

        try {
            const model = window.dataCollector.collectOrderData(orderId, isDownload);
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Обработка...';

            const response = await fetch('/Orders/SaveDataOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(model)
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error);
            }

            var postSaveDiv = document.getElementById('postSaveButtons');
            if (postSaveDiv) {
                postSaveDiv.classList.remove('d-none');
                postSaveDiv.classList.add('d-flex');
            }

            if (isDownload) {
                await downloadTKP(response, orderId);
                await modal.alert('ТКП успешно сформировано и сохранено!', 'Успех', () => location.reload());
            } else {
                await modal.alert('Данные успешно сохранены!', 'Успех', () => location.reload());
            }
        } catch (error) {
            console.error('Ошибка сохранения:', error);
            await modal.alert(`Ошибка: ${error.message}`, 'Ошибка сохранения');
        } finally {
            button.disabled = false;
            button.innerHTML = button.classList.contains('save-btn-TKP') ? 'Скачать ТКП' : 'Сохранить данные';
        }
    }

    async function downloadTKP(response, orderId) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `ТКП_${orderId}.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        a.remove();
    }
});