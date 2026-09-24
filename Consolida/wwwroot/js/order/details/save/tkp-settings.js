document.addEventListener('DOMContentLoaded', function () {
    const tkpModalEl = document.getElementById('tkpSettingsModal');
    if (!tkpModalEl) return;

    const tkpModal = new bootstrap.Modal(tkpModalEl);
    let currentOrderId = null;

    // При клике на «Выгрузить ТКП» открываем модалку
    document.querySelectorAll('.save-btn-TKP').forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            currentOrderId = this.dataset.orderId;

            // Сбрасываем номер ТКП и подсказываем ID заказа
            const tkpNumberInput = document.getElementById('tkpNumber');
            tkpNumberInput.value = '';
            tkpNumberInput.placeholder = `например: ${currentOrderId}`;

            // Заполняем поля значениями по умолчанию, если они пусты
            if (!document.getElementById('tkpDeliveryTime').value) {
                document.getElementById('tkpDeliveryTime').value = 'недель с момента получения предоплаты';
            }
            if (!document.getElementById('tkpDeliveryTerms').value) {
                document.getElementById('tkpDeliveryTerms').value = 'до склада Заказчика (стоимость доставки включена в стоимость товара)';
            }
            if (!document.getElementById('tkpPaymentTerms').value) {
                document.getElementById('tkpPaymentTerms').value = '100% предоплата';
            }

            tkpModal.show();
        });
    });

    // При нажатии «Сформировать ТКП»
    document.getElementById('tkpGenerateBtn').addEventListener('click', async function () {
        if (!currentOrderId) return;

        const enteredNumber = document.getElementById('tkpNumber').value.trim();
        // Если не ввели — используем Id заказа
        const finalNumber = enteredNumber || String(currentOrderId);

        const settings = {
            tkpNumber: enteredNumber, // сервер сам подставит Order.Id, если пусто
            deliveryTime: document.getElementById('tkpDeliveryTime').value.trim(),
            deliveryTerms: document.getElementById('tkpDeliveryTerms').value.trim(),
            paymentTerms: document.getElementById('tkpPaymentTerms').value.trim()
        };

        const model = window.dataCollector.collectOrderData(currentOrderId, true);
        model.TkpSettings = settings;

        tkpModal.hide();

        const btn = document.querySelector(`.save-btn-TKP[data-order-id="${currentOrderId}"]`);
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Формирование...';
        }

        try {
            const response = await fetch('/Orders/SaveDataOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(model)
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');

            const year = new Date().getFullYear().toString().slice(-2);
            a.href = url;
            a.download = `05-${finalNumber}_${year} ТКП МПСА-технологии.xlsx`;

            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();

            await modal.alert('ТКП успешно сформировано и сохранено!', 'Успех', () => location.reload());
        } catch (error) {
            console.error('Ошибка выгрузки ТКП:', error);
            await modal.alert(`Ошибка: ${error.message}`, 'Ошибка выгрузки ТКП');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = 'Выгрузить ТКП';
            }
        }
    });
});