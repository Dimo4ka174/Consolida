document.addEventListener('DOMContentLoaded', function () {
    window.toNumber = function (value) {
        if (value === null || value === undefined || value === '' || value === '—') return 0;

        // Если уже число
        if (typeof value === 'number') return value;

        // Если DOM-элемент input
        if (value.nodeType === 1) {
            // Пробуем получить из data-атрибута
            if (value.dataset && value.dataset.numericValue !== undefined) {
                return parseFloat(value.dataset.numericValue) || 0;
            }
            // Иначе парсим value
            value = value.value || '0';
        }

        // Если объект с полем value
        if (value.value !== undefined) {
            value = value.value || '0';
        }

        // Преобразуем строку
        const strValue = value.toString();
        const cleaned = strValue
            .replace(/\s/g, '')
            .replace(/,/g, '.')
            .replace(/[^\d.-]/g, '');

        return parseFloat(cleaned) || 0;
    };

    // Функция для нормализации числа
    function normalizeNumber(value) {
        if (typeof value !== 'string') {
            if (typeof value === 'number') return value.toString();
            return '';
        }

        return value.replace(/,/g, '.');
    }

    // Функция для форматирования
    function formatNumber(value) {
        if (!value && value !== 0) return '';

        const num = window.toNumber(value);
        if (isNaN(num)) return value;

        if (window.formatter) {
            return window.formatter.formatNumberWithSpaces(num);
        }

        return num.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    // Обработчики для input полей
    function handleNumberInput(e) {
        const input = e.target;
        let value = input.value;

        value = value.replace(/[^\d.,\-]/g, '');

        const parts = value.split(/[,.]/);
        if (parts.length > 2) {
            value = parts[0] + '.' + parts.slice(1).join('');
        } else if (parts.length === 2 && parts[1].length > 2) {
            value = parts[0] + '.' + parts[1].substring(0, 2);
        }

        input.value = value;
        input.dataset.numericValue = window.toNumber(value);

        const changeEvent = new Event('change', { bubbles: true });
        input.dispatchEvent(changeEvent);
    }

    function handleNumberBlur(e) {
        const input = e.target;

        if (input.value) {
            const num = window.toNumber(input.value);
            if (!isNaN(num)) {
                input.dataset.numericValue = num;
                if (window.formatter) {
                    input.value = window.formatter.formatNumberWithSpaces(num);
                } else {
                    input.value = num.toFixed(2).replace('.', ',');
                }
            }
        }
    }

    function handleNumberFocus(e) {
        const input = e.target;

        if (input.value) {
            let numericValue = input.dataset.numericValue;
            if (!numericValue) {
                numericValue = window.toNumber(input.value);
            }
            input.value = numericValue.toString().replace('.', ',');
        }
    }

    function setupNumberInputs() {
        const numberInputs = document.querySelectorAll(`
            #exchangeRateInput,
            #bankCommissionInput,
            #marginInput,
            #unforeseenExpensesInput,
            .quantity-input,
            .weight-input,
            .margin-input,
            .unforeseen-expenses-input,
            .rate-input,
            .tax-input,
            .unit-input
        `);

        numberInputs.forEach(input => {
            const initialValue = window.toNumber(input);
            input.dataset.numericValue = initialValue;

            input.addEventListener('input', handleNumberInput);
            input.addEventListener('blur', handleNumberBlur);
            input.addEventListener('focus', handleNumberFocus);

            if (input.value && window.toNumber(input) !== 0) {
                setTimeout(() => {
                    handleNumberBlur({ target: input });
                }, 100);
            }
        });
    }

    setupNumberInputs();

    window.numberValidator = {
        toNumber: window.toNumber,
        formatNumber,
        normalizeNumber
    };
});