document.addEventListener('DOMContentLoaded', function () {
    const duplicateBtn = document.querySelector('.duplicate-order-btn');

    if (duplicateBtn) {
        duplicateBtn.addEventListener('click', async function (e) {
            e.preventDefault();

            const orderId = this.dataset.orderId;

            // Показываем модальное окно подтверждения
            const confirmed = await modal.confirm(
                'Вы уверены, что хотите создать дубликат этого заказа?',
                'Дублирование заказа'
            );

            if (!confirmed) return;

            try {
                // Блокируем кнопку
                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Создание дубля...';

                // Собираем данные текущего заказа для дублирования
                const orderData = await collectOrderDataForDuplicate(orderId);

                // Отправляем запрос на дублирование
                const response = await fetch('/Orders/DuplicateOrder', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(orderData)
                });

                if (!response.ok) {
                    const error = await response.text();
                    throw new Error(error || 'Ошибка при дублировании заказа');
                }

                const result = await response.json();

                // Показываем сообщение об успехе и перенаправляем на новый заказ
                await modal.alert(
                    `Заказ успешно продублирован. Новый номер заказа: ${result.newOrderNumber}`,
                    'Успех',
                    () => {
                        window.location.href = `/Orders/Details/${result.newOrderId}`;
                    }
                );

            } catch (error) {
                console.error('Ошибка дублирования:', error);
                await modal.alert(`Ошибка: ${error.message}`, 'Ошибка дублирования');
            } finally {
                // Разблокируем кнопку
                this.disabled = false;
                this.innerHTML = '<i class="bi bi-files"></i> Дубль';
            }
        });
    }

    // Функция сбора данных для дублирования
    async function collectOrderDataForDuplicate(orderId) {
        // Собираем базовые данные заказа
        const baseData = {
            OriginalOrderId: parseInt(orderId, 10),
            ExchangeRate: window.toNumber(document.getElementById('exchangeRateInput')),
            Comment: document.getElementById('orderComment')?.value || '',
            Products: [],
            OrderTaxes: collectOrderTaxes()
        };

        // Собираем данные по товарам с полной информацией о налогах
        document.querySelectorAll('.product-row').forEach((row, index) => {
            const productId = parseInt(row.dataset.productId, 10);
            const marginRow = document.querySelector(`.product-margin[data-product-id="${productId}"]`);
            const allTaxRows = document.querySelectorAll(`.product-tax[data-product-id="${productId}"]`);

            const productData = {
                ProductId: productId,
                ProductName: row.querySelector('td:nth-child(2)')?.textContent.trim() || '',
                Model: row.querySelector('td:nth-child(3)')?.textContent.trim() || '',
                DeliveryDateString: row.querySelector('.delivery-date')?.textContent || null,
                LeadTime: parseInt(row.querySelector('.lead-time')?.textContent, 10) || 0,
                Quantity: getNumericValue(row.querySelector('.quantity-input'), 0),
                Price: parseFormattedNumber(row.querySelector('.price-cny')?.textContent || '0'),
                Weight: getNumericValue(row.querySelector('.weight-input'), 0),
                Comment: document.querySelector(`.comment-input[data-product-id="${productId}"]`)?.value || '',

                // Маржа
                MarginRate: getNumericValue(marginRow?.querySelector('.margin-input'), 0),

                // Непредвиденные расходы
                UnforeseenExpensesRate: getNumericValue(marginRow?.querySelector('.unforeseen-expenses-input'), 0),

                // Код ТНВЭД
                CodeTNVD: marginRow?.querySelector('.code-search')?.value || '',
                DutyRate: getNumericValue(marginRow?.querySelector('.rate-input'), 0),

                // Налоги продукта
                ProductTaxes: collectAllProductTaxes(allTaxRows)
            };

            baseData.Products.push(productData);
        });

        return baseData;
    }

    function collectOrderTaxes() {
        const taxes = {};
        const taxTypeIds = JSON.parse(document.getElementById('tax-type-ids')?.dataset.taxTypes || '{}');

        // Комиссия банка
        const bankInput = document.getElementById('bankCommissionInput');
        if (bankInput && taxTypeIds["Комиссия банка"]) {
            taxes[taxTypeIds["Комиссия банка"]] = window.toNumber(bankInput.value);
        }

        // Маржа (глобальная)
        const marginInput = document.getElementById('marginInput');
        if (marginInput && taxTypeIds["Маржа"]) {
            taxes[taxTypeIds["Маржа"]] = window.toNumber(marginInput.value);
        }

        // Непредвиденные расходы (глобальные)
        const unforeseenInput = document.getElementById('unforeseenExpensesInput');
        if (unforeseenInput && taxTypeIds["Не предвиденные расходы"]) {
            taxes[taxTypeIds["Не предвиденные расходы"]] = window.toNumber(unforeseenInput.value);
        }

        // Другие налоги из таблиц
        document.querySelectorAll('.tax-input').forEach(input => {
            const taxId = parseInt(input.dataset.taxId, 10);
            if (!isNaN(taxId) && !taxes.hasOwnProperty(taxId)) {
                taxes[taxId] = window.toNumber(input.value);
            }
        });

        return taxes;
    }

    function collectAllProductTaxes(taxRows) {
        const taxes = {};

        taxRows.forEach(taxRow => {
            taxRow.querySelectorAll('.unit-input').forEach(input => {
                const taxId = parseInt(input.dataset.taxId, 10);
                if (!isNaN(taxId)) {
                    taxes[taxId] = window.toNumber(input);
                }
            });
        });

        return taxes;
    }

    function getNumericValue(element, defaultValue = 0) {
        if (!element) return defaultValue;

        if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.tagName === 'SELECT') {
            return window.toNumber(element.value) || defaultValue;
        }

        const text = element.textContent || element.innerText || '';
        return window.toNumber(text) || defaultValue;
    }

    function parseFormattedNumber(formattedString) {
        if (!formattedString || formattedString === '—') return 0;

        if (window.formatter && window.formatter.parseFormattedNumber) {
            return window.formatter.parseFormattedNumber(formattedString);
        }

        const cleaned = formattedString
            .toString()
            .replace(/\s/g, '')
            .replace(/,/g, '.');
        return parseFloat(cleaned) || 0;
    }

    function getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }
});