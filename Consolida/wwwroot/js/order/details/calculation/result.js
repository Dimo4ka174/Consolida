document.addEventListener('DOMContentLoaded', function () {
    try {
        // Кэшируем элементы
        const elements = {
            exchangeRateInput: document.getElementById('exchangeRateInput'),
            bankCommissionInput: document.getElementById('bankCommissionInput'),
            marginInput: document.getElementById('marginInput'),
            unforeseenExpensesInput: document.getElementById('unforeseenExpensesInput'),
            productRows: document.querySelectorAll('.product-row'),
            marginRows: document.querySelectorAll('.product-margin'),
            costRows: document.querySelectorAll('.product-cost'),
            totalWithoutVatCell: document.getElementById('js-total-without-vat'),
            totalWithVatCell: document.getElementById('js-total-with-vat')
        };

        // Дебаунс для оптимизации
        const debounce = (func, wait) => {
            let timeout;
            return (...args) => {
                clearTimeout(timeout);
                timeout = setTimeout(() => func.apply(this, args), wait);
            };
        };

        // Основная функция расчета
        const calculateTotals = debounce(() => {
            const exchangeRate = window.toNumber(elements.exchangeRateInput);
            const bankRate = window.toNumber(elements.bankCommissionInput);
            const vatRate = 1.22; // НДС 22%
            let totalWithoutVat = 0;
            let totalWithVat = 0;
            let totalUnforeseen = 0;

            // Расчет для каждого товара
            elements.costRows.forEach((row, index) => {
                const marginRow = elements.marginRows[index];
                const productRow = elements.productRows[index];

                if (!marginRow || !productRow) return;

                const productId = productRow.dataset.productId;
                const quantity = getProductQuantity(productId);
                const priceCny = getProductPriceCny(productRow);

                // Получаем данные из таблицы маржи
                const totalPriceAfterMargin = getMarginValue(marginRow, '.total-price-after-margin');
                const priceWithRate = getMarginValue(marginRow, '.price-with-rate');
                const unforeseenExpensesCost = getMarginValue(marginRow, '.unforeseen-expenses-cost');

                // Банковская комиссия
                const bankCommission = priceCny * exchangeRate * quantity * (bankRate / 100);

                // Сумма налогов из налоговых таблиц
                const taxesSum = getTaxesForProduct(index);

                // Расчет итоговых цен
                const priceWithoutVatTotal = totalPriceAfterMargin + priceWithRate + unforeseenExpensesCost + taxesSum + bankCommission;
                const priceWithVatTotal = priceWithoutVatTotal * vatRate;
                const priceWithoutVatUnit = quantity > 0 ? priceWithoutVatTotal / quantity : 0;
                const priceWithVatUnit = quantity > 0 ? priceWithVatTotal / quantity : 0;

                // Обновляем DOM
                setValue(row, '.price-without-vat-unit', priceWithoutVatUnit);
                setValue(row, '.price-without-vat-total', priceWithoutVatTotal);
                setValue(row, '.price-with-vat-unit', priceWithVatUnit);
                setValue(row, '.price-with-vat-total', priceWithVatTotal);

                totalWithoutVat += priceWithoutVatTotal;
                totalWithVat += priceWithVatTotal;
                totalUnforeseen += unforeseenExpensesCost;
            });

            // Обновляем итоги
            if (elements.totalWithoutVatCell && elements.totalWithVatCell) {
                elements.totalWithoutVatCell.textContent = formatNumberWithSpaces(totalWithoutVat) + ' ₽';
                elements.totalWithVatCell.textContent = formatNumberWithSpaces(totalWithVat) + ' ₽';
            }

            const unforeseenTotalElement = document.getElementById('total-unforeseen-expenses');
            if (unforeseenTotalElement) {
                unforeseenTotalElement.textContent = formatNumberWithSpaces(totalUnforeseen) + ' ₽';
            }
        }, 300);

        // Вспомогательные функции
        function getProductQuantity(productId) {
            const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
            return input ? window.toNumber(input) : 0;
        }

        function getProductPriceCny(productRow) {
            const priceCell = productRow.querySelector('.price-cny');
            if (!priceCell) return 0;

            const text = priceCell.textContent.replace(/[^\d.,]/g, '');
            return parseFormattedNumber(text);
        }

        function getMarginValue(row, selector) {
            const el = row.querySelector(selector);
            if (!el) return 0;

            if (el.dataset && el.dataset.rawValue !== undefined) {
                return parseFloat(el.dataset.rawValue) || 0;
            }

            // Если это поле ввода, берем значение напрямую
            const input = el.tagName === 'INPUT' ? el : row.querySelector(`${selector}-input`);
            if (input) {
                return window.toNumber(input);
            }

            // Получаем текст и убираем валюту
            const text = el.textContent.replace(/[^\d.,]/g, '');
            return parseFormattedNumber(text);
        }

        function getTaxesForProduct(index) {
            let taxesSum = 0;
            document.querySelectorAll('.product-tax-table').forEach(table => {
                const taxRow = table.querySelectorAll('.product-tax')[index];
                if (taxRow) {
                    // Суммируем значения из sum-input
                    taxRow.querySelectorAll('.sum-input').forEach(cell => {
                        taxesSum += parseFormattedNumber(cell.textContent);
                    });

                    // Также проверяем unit-input, если sum-input пустые
                    if (taxesSum === 0) {
                        taxRow.querySelectorAll('.unit-input').forEach(input => {
                            const unitValue = window.toNumber(input);
                            const quantity = getProductQuantity(taxRow.dataset.productId);
                            taxesSum += unitValue * quantity;
                        });
                    }
                }
            });
            return taxesSum;
        }

        function formatNumberWithSpaces(number) {
            if (window.formatter) {
                return window.formatter.formatNumberWithSpaces(number);
            }

            if (number === null || number === undefined || isNaN(number)) {
                return '—';
            }
            const rounded = Math.round(number * 100) / 100;
            const parts = rounded.toFixed(2).split('.');
            const integerPart = parts[0];
            const decimalPart = parts[1] || '00';
            const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
            return `${formattedInteger}.${decimalPart}`;
        }

        function parseFormattedNumber(formattedString) {
            if (!formattedString || formattedString === '—') return 0;
            const cleaned = formattedString
                .toString()
                .replace(/\s/g, '')
                .replace(/,/g, '.');
            return window.toNumber(cleaned);
        }

        function setValue(row, selector, value) {
            const el = row.querySelector(selector);
            if (el) {
                el.textContent = formatNumberWithSpaces(value);
                el.dataset.calculatedValue = value;
            }
        }

        // Настройка слушателей событий
        function setupEventListeners() {
            const watchedInputs = [
                elements.exchangeRateInput,
                elements.bankCommissionInput,
                elements.marginInput,
                elements.unforeseenExpensesInput
            ];

            watchedInputs.forEach(input => {
                if (input) {
                    input.addEventListener('input', calculateTotals);
                    input.addEventListener('change', calculateTotals);
                }
            });

            document.addEventListener('input', (e) => {
                if (e.target.matches(
                    '.quantity-input, .weight-input, .margin-input, ' +
                    '.unforeseen-expenses-input, .tax-input, .unit-input, ' +
                    '.rate-input, .code-search'
                )) {
                    calculateTotals();
                }
            });

            document.addEventListener('change', (e) => {
                if (e.target.matches(
                    '.quantity-input, .margin-input, .unforeseen-expenses-input, ' +
                    '.tax-input, .unit-input, .rate-input'
                )) {
                    calculateTotals();
                }
            });

            document.addEventListener('blur', (e) => {
                if (e.target.matches(
                    '.quantity-input, .margin-input, .unforeseen-expenses-input, ' +
                    '.tax-input, .unit-input, .rate-input'
                )) {
                    calculateTotals();
                }
            }, true);
        }

        // Инициализация
        function initialize() {
            setupEventListeners();

            setTimeout(() => {
                calculateTotals();
                console.log('Первоначальный расчет выполнен');
            }, 500);
        }

        initialize();

        window.recalculateOrderTotals = calculateTotals;

    } catch (error) {
        console.error("Ошибка в result.js:", error);
    }
});