window.TaxStateManager = (function () {
    const PRECISION = 2;
    const productValues = new Map();

    const init = () => {
        // Инициализация не требуется
    };

    const saveAllFieldStates = () => {
        productValues.clear();

        document.querySelectorAll('.product-tax-table').forEach(table => {
            table.querySelectorAll('tbody tr').forEach(row => {
                const productId = row.dataset.productId;
                if (!productId) return;

                const productData = productValues.get(productId) || {};
                row.querySelectorAll('.unit-input').forEach(input => {
                    const taxId = input.dataset.taxId;
                    if (taxId) {
                        productData[taxId] = {
                            value: input.value,
                            isCalculated: input.dataset.isCalculated === 'true'
                        };
                    }
                });
                const metroNumber = row.querySelector('.metrological-number-input');
                if (metroNumber) {
                    productData['__metrological_number'] = { value: metroNumber.value };
                }
                const metroExpiry = row.querySelector('.metrological-expiry-input');
                if (metroExpiry) {
                    productData['__metrological_expiry'] = { value: metroExpiry.value };
                }
                productValues.set(productId, productData);
            });
        });

        return productValues;
    };

    const restoreFieldStates = (savedValues) => {
        if (!savedValues || savedValues.size === 0) return;

        document.querySelectorAll('.product-tax-table').forEach(table => {
            table.querySelectorAll('tbody tr').forEach(row => {
                const productId = row.dataset.productId;
                if (!productId || !savedValues.has(productId)) return;

                const productData = savedValues.get(productId);
                row.querySelectorAll('.unit-input').forEach(input => {
                    const taxId = input.dataset.taxId;
                    if (taxId && productData[taxId]) {
                        const saved = productData[taxId];
                        input.value = saved.value;
                        input.dataset.isCalculated = saved.isCalculated ? 'true' : 'false';
                        input.dataset.numericValue = window.toNumber(saved.value);

                        // Восстанавливаем состояние на основе значения и флага
                        if (!saved.isCalculated) {
                            input.dataset.fieldState = 'manual';
                            const numericValue = window.toNumber(saved.value);
                            if (numericValue === 0) {
                                input.classList.add('unit-manual-zero');
                                input.classList.remove('unit-modified', 'unit-auto');
                            } else {
                                input.classList.add('unit-modified');
                                input.classList.remove('unit-auto', 'unit-manual-zero');
                            }
                        } else {
                            input.dataset.fieldState = 'auto';
                            input.classList.add('unit-auto');
                            input.classList.remove('unit-modified', 'unit-manual-zero');
                        }

                        updateSumForInput(input);
                    }
                });
                const metroNumber = row.querySelector('.metrological-number-input');
                if (metroNumber && productData['__metrological_number']) {
                    metroNumber.value = productData['__metrological_number'].value || '';
                }
                const metroExpiry = row.querySelector('.metrological-expiry-input');
                if (metroExpiry && productData['__metrological_expiry']) {
                    metroExpiry.value = productData['__metrological_expiry'].value || '';
                }
            });
        });
    };

    const updateSumForInput = (input) => {
        const row = input.closest('tr');
        const taxId = input.dataset.taxId;
        const sumCell = row.querySelector(`.sum-input[data-tax-id="${taxId}"]`);
        const productId = row.dataset.productId;

        if (sumCell && productId) {
            const quantity = getProductQuantity(productId);
            const unitValue = window.toNumber(input.value);
            const total = unitValue * quantity;
            sumCell.textContent = formatNumber(total);
            sumCell.dataset.rawValue = total;
        }
    };

    const getProductQuantity = (productId) => {
        const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
        return input ? window.toNumber(input) : 0;
    };

    const formatNumber = (number) => {
        if (window.formatter?.formatNumberWithSpaces) {
            return window.formatter.formatNumberWithSpaces(number);
        }
        return number.toFixed(PRECISION);
    };

    const getProductValues = () => productValues;

    return {
        init,
        saveAllFieldStates,
        restoreFieldStates,
        updateSumForInput,
        getProductValues
    };
})();