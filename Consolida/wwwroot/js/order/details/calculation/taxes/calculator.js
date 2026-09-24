window.TaxCalculator = (function () {
    const PRECISION = 2;

    const calculateProductsData = () => {
        const exchangeRate = window.toNumber(document.getElementById('exchangeRateInput'));
        const bankRate = window.toNumber(document.getElementById('bankCommissionInput'));
        const productRows = document.querySelectorAll('.product-row');

        const products = [];
        let totalCost = 0;

        productRows.forEach(row => {
            const productId = row.dataset.productId;
            const quantity = window.toNumber(document.querySelector(`.quantity-input[data-product-id="${productId}"]`));
            const priceCny = window.toNumber(row.querySelector('.price-cny').textContent.split('(')[0].trim());

            const costRub = priceCny * exchangeRate * quantity;
            const bankCommission = costRub * (bankRate / 100);
            const totalProductCost = costRub + bankCommission;

            products.push({
                id: productId,
                quantity,
                costRub: totalProductCost
            });

            totalCost += totalProductCost;
        });

        products.forEach(product => {
            product.share = totalCost > 0 ? product.costRub / totalCost : 0;
        });

        return { products, totalCost };
    };

    const distributeToAutoProducts = (autoProducts, totalTax) => {
        if (autoProducts.length === 0) return;

        const totalAutoCost = autoProducts.reduce((sum, p) => sum + p.product.costRub, 0);

        autoProducts.forEach(entry => {
            const productShare = totalAutoCost > 0 ? entry.product.costRub / totalAutoCost : 1 / autoProducts.length;
            const productTax = totalTax * productShare;
            const productTaxPerUnit = entry.product.quantity > 0
                ? +(productTax / entry.product.quantity).toFixed(PRECISION)
                : 0;

            if (entry.unitInput) {
                const numericValue = productTaxPerUnit;
                entry.unitInput.value = numericValue.toFixed(2);
                entry.unitInput.dataset.numericValue = numericValue;
                entry.unitInput.dataset.fieldState = 'auto';
                entry.unitInput.dataset.isCalculated = 'true';
                entry.unitInput.classList.add('unit-auto');
                entry.unitInput.classList.remove('unit-modified', 'unit-manual-zero');
            }

            if (entry.sumCell) {
                const totalValue = productTax;
                entry.sumCell.textContent = formatNumber(totalValue);
                entry.sumCell.dataset.rawValue = totalValue;
            }
        });
    };

    const isFieldManual = (unitInput) => {
        // Поле считается ручным если:
        // 1. Оно было изменено пользователем (fieldState = 'manual')
        // 2. ИЛИ в БД оно помечено как не просчитанное (isCalculated = false) И имеет значение
        const fieldState = unitInput.dataset.fieldState;
        const isCalculated = unitInput.dataset.isCalculated === 'true';
        const hasValue = unitInput.value && parseFloat(unitInput.value) !== 0;

        return fieldState === 'manual' || (!isCalculated && hasValue);
    };

    const distributeTaxes = (table, products, taxData) => {
        const productRows = table.querySelectorAll('.product-tax');
        if (productRows.length === 0) return;

        taxData.forEach(tax => {
            const taxId = tax.taxId;
            const expectedTotal = tax.expectedTotal;

            let totalDistributed = 0;
            const manualEntries = [];
            const autoProducts = [];
            const zeroManualEntries = [];

            productRows.forEach(row => {
                const unitInput = row.querySelector(`.unit-input[data-tax-id="${taxId}"]`);
                const sumCell = row.querySelector(`.sum-input[data-tax-id="${taxId}"]`);
                const product = products.find(p => p.id === row.dataset.productId);

                if (!unitInput || !product) return;

                const value = unitInput.value.trim();
                const isManual = unitInput.dataset.fieldState === 'manual';
                const isManualZero = isManual && unitInput.dataset.numericValue === '0';

                if (isManualZero) {
                    // Явно выставленный ноль - сохраняем, но не учитываем в распределении
                    zeroManualEntries.push({ row, unitInput, sumCell });
                    if (sumCell) {
                        sumCell.textContent = '0.00';
                        sumCell.dataset.rawValue = 0;
                    }
                }
                else if (isManual && value !== '' && parseFloat(value) !== 0) {
                    // Ручное ненулевое значение
                    const unitValue = window.toNumber(value);
                    const totalValue = unitValue * product.quantity;

                    manualEntries.push({
                        row,
                        unitInput,
                        sumCell,
                        totalValue,
                        unitValue
                    });
                    totalDistributed += totalValue;

                    if (sumCell) {
                        sumCell.textContent = formatNumber(totalValue);
                        sumCell.dataset.rawValue = totalValue;
                    }
                } else {
                    // Авто-режим (пустое поле или не помечено как ручное)
                    autoProducts.push({
                        row,
                        unitInput,
                        sumCell,
                        product
                    });
                }
            });

            // Распределяем остаток на авто-поля
            let remainingTax = expectedTotal - totalDistributed;

            if (autoProducts.length > 0 && remainingTax > 0.01) {
                const totalAutoCost = autoProducts.reduce((sum, p) => sum + p.product.costRub, 0);

                autoProducts.forEach(entry => {
                    const productShare = totalAutoCost > 0 ? entry.product.costRub / totalAutoCost : 1 / autoProducts.length;
                    const productTax = remainingTax * productShare;
                    const productTaxPerUnit = entry.product.quantity > 0
                        ? +(productTax / entry.product.quantity).toFixed(PRECISION)
                        : 0;

                    if (entry.unitInput) {
                        entry.unitInput.value = productTaxPerUnit.toFixed(PRECISION);
                        entry.unitInput.dataset.fieldState = 'auto';
                        entry.unitInput.dataset.isCalculated = 'true';
                        entry.unitInput.dataset.numericValue = productTaxPerUnit;
                        entry.unitInput.classList.add('unit-auto');
                        entry.unitInput.classList.remove('unit-modified', 'unit-manual-zero');
                    }

                    if (entry.sumCell) {
                        entry.sumCell.textContent = formatNumber(productTax);
                        entry.sumCell.dataset.rawValue = productTax;
                    }
                });
            } else if (autoProducts.length > 0 && remainingTax <= 0.01) {
                // Если остаток близок к нулю, обнуляем авто-поля
                autoProducts.forEach(entry => {
                    if (entry.unitInput) {
                        entry.unitInput.value = '0.00';
                        entry.unitInput.dataset.numericValue = 0;
                        entry.unitInput.dataset.fieldState = 'auto';
                        entry.unitInput.dataset.isCalculated = 'true';
                    }
                    if (entry.sumCell) {
                        entry.sumCell.textContent = '0.00';
                        entry.sumCell.dataset.rawValue = 0;
                    }
                });
            }

            // Обновляем итог
            const finalTotal = totalDistributed + (remainingTax > 0 ? remainingTax : 0);
            updateTaxTotal(table, taxId, expectedTotal, finalTotal);
        });
    };

    const updateTaxTotal = (table, taxId, expectedTotal, actualTotal) => {
        const totalCell = table.querySelector(`.total-sum[data-tax-id="${taxId}"]`);
        if (!totalCell) return;

        // Форматируем и сохраняем актуальную сумму
        totalCell.textContent = formatNumber(actualTotal);
        totalCell.dataset.rawValue = actualTotal;

        // Подсвечиваем красным, если есть расхождение
        const isMismatch = Math.abs(expectedTotal - actualTotal) > 0.015;
        totalCell.classList.toggle('tax-mismatch', isMismatch);

        const taxInput = table.querySelector(`.tax-input[data-tax-id="${taxId}"]`);
        if (taxInput) {
            taxInput.classList.toggle('tax-mismatch', isMismatch);

            // Добавляем подсказку
            if (isMismatch) {
                const title = `Ожидаемая сумма: ${formatNumber(expectedTotal)} ₽\nФактическая сумма: ${formatNumber(actualTotal)} ₽`;
                totalCell.title = title;
                taxInput.title = title;
            } else {
                totalCell.title = '';
                taxInput.title = '';
            }
        }
    };

    const formatNumber = (number) => {
        if (window.formatter?.formatNumberWithSpaces) {
            return window.formatter.formatNumberWithSpaces(number);
        }
        return number.toFixed(PRECISION);
    };

    return {
        calculateProductsData,
        distributeTaxes
    };
})();