document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.save-btn-calculation').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            await handleExportCalculation(this);
        });
    });

    async function handleExportCalculation(button) {
        const orderId = button.dataset.orderId;
        try {
            const model = window.dataCollector.collectOrderData(orderId, false);
            // Для экспорта можно добавить дополнительную информацию, если нужно
            // model = await addCalculationDetails(model);

            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Формирование...';

            const response = await fetch('/Orders/ExportCalculation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(model)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Ошибка сервера: ${response.status} ${errorText}`);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Расчет_заказа_${orderId}_${new Date().toISOString().slice(0, 10)}.xlsx`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();

            await modal.alert('Расчет успешно сформирован!', 'Успех');
        } catch (error) {
            console.error('Ошибка экспорта:', error);
            await modal.alert(`Ошибка: ${error.message}`, 'Ошибка экспорта');
        } finally {
            button.disabled = false;
            button.innerHTML = '<i class="bi bi-file-earmark-spreadsheet"></i> Сохранить расчет';
        }
    }
});