(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Ждём, пока все модули станут доступны
        if (!window.TaxColumnManager || !window.TaxCalculator) {
            console.warn('Required modules not ready, retrying...');
            setTimeout(arguments.callee, 50);
            return;
        }

        window.TaxColumnManager.init();

        // Обработчик для unit-input (ручной ввод)
        document.addEventListener('input', (e) => {
            if (e.target.classList.contains('unit-input')) {
                const value = e.target.value.trim();
                if (value === '') {
                    e.target.dataset.fieldState = 'auto';
                    e.target.dataset.isCalculated = 'true';
                    e.target.dataset.numericValue = 0;
                    e.target.classList.remove('unit-modified', 'unit-manual-zero');
                    e.target.classList.add('unit-auto');
                    const row = e.target.closest('tr');
                    const taxId = e.target.dataset.taxId;
                    const sumCell = row.querySelector(`.sum-input[data-tax-id="${taxId}"]`);
                    if (sumCell) {
                        sumCell.textContent = '0.00';
                        sumCell.dataset.rawValue = 0;
                    }
                } else {
                    const numericValue = window.toNumber(value);
                    e.target.dataset.fieldState = 'manual';
                    e.target.dataset.isCalculated = 'false';
                    e.target.dataset.numericValue = numericValue;
                    if (numericValue === 0) {
                        e.target.classList.add('unit-manual-zero');
                        e.target.classList.remove('unit-modified', 'unit-auto');
                    } else {
                        e.target.classList.add('unit-modified');
                        e.target.classList.remove('unit-auto', 'unit-manual-zero');
                    }
                    if (window.TaxStateManager) {
                        window.TaxStateManager.updateSumForInput(e.target);
                    }
                }
                fullRecalc();  // важный момент: после ручного изменения запускаем полный пересчёт
            }
        });

        // Обработчик focus для unit-input
        document.addEventListener('focus', (e) => {
            if (e.target.classList.contains('unit-input')) {
                const numericValue = e.target.dataset.numericValue;
                if (numericValue) {
                    e.target.value = numericValue;
                }
            }
        }, true);

        // Обработчик для tax-input
        document.addEventListener('input', (e) => {
            if (e.target.classList.contains('tax-input')) {
                const taxId = e.target.dataset.taxId;
                const newValue = window.toNumber(e.target.value) || 0;
                const badge = document.querySelector(`.tax-badge-modern[data-tax-id="${taxId}"]`);
                if (badge) badge.dataset.taxCost = newValue;
                fullRecalc();
            }
        });

        // Обработчик двойного клика для сброса
        document.addEventListener('dblclick', (e) => {
            if (!e.target.classList.contains('unit-input')) return;
            e.target.value = '';
            e.target.dataset.fieldState = 'auto';
            e.target.dataset.isCalculated = 'true';
            e.target.classList.remove('unit-modified', 'unit-manual-zero');
            e.target.classList.add('unit-auto');
            const row = e.target.closest('tr');
            const taxId = e.target.dataset.taxId;
            const sumCell = row.querySelector(`.sum-input[data-tax-id="${taxId}"]`);
            if (sumCell) {
                sumCell.textContent = '0.00';
                sumCell.dataset.rawValue = 0;
            }
            fullRecalc();
        });

        // Обработчики blur для tax-input
        document.addEventListener('blur', (e) => {
            if (e.target.classList.contains('tax-input') && !e.target.value.trim()) {
                const taxId = e.target.dataset.taxId;
                const badge = document.querySelector(`.tax-badge-modern[data-tax-id="${taxId}"]`);
                if (badge) {
                    const cost = parseFloat(badge.dataset.taxCost) || 0;
                    e.target.value = cost.toFixed(2);
                    fullRecalc();
                }
            }
        }, true);

        // Обработчик blur для unit-input
        document.addEventListener('blur', (e) => {
            if (e.target.classList.contains('unit-input') && !e.target.value.trim()) {
                e.target.dataset.fieldState = 'auto';
                e.target.dataset.isCalculated = 'true';
                e.target.classList.remove('unit-modified', 'unit-manual-zero');
                e.target.classList.add('unit-auto');
                fullRecalc();
            }
        }, true);

        document.addEventListener('blur', (e) => {
            if (e.target.classList.contains('unit-input')) {
                const value = e.target.value;
                if (value && value !== '') {
                    const numericValue = window.toNumber(value);
                    e.target.dataset.numericValue = numericValue;
                    if (window.formatter) {
                        e.target.value = window.formatter.formatNumberWithSpaces(numericValue);
                    }
                }
            }
        }, true);

        // Главная функция пересчёта налогов
        window.recalculateTaxes = function () {
            if (!window.TaxCalculator) return;
            const { products } = window.TaxCalculator.calculateProductsData();

            document.querySelectorAll('.product-tax-table').forEach(table => {
                const taxInputs = Array.from(table.querySelectorAll('.tax-input')).map(input => ({
                    taxId: input.dataset.taxId,
                    expectedTotal: window.toNumber(input) || 0
                }));
                window.TaxCalculator.distributeTaxes(table, products, taxInputs);
            });

            if (window.recalculateOrderTotals) {
                window.recalculateOrderTotals();
            }
            forceUpdateTotals();
        };

        const exchangeRateInput = document.getElementById('exchangeRateInput');
        const bankRateInput = document.getElementById('bankCommissionInput');
        const marginGlobalInput = document.getElementById('marginInput');
        const unforeseenGlobalInput = document.getElementById('unforeseenExpensesInput');

        function fullRecalc() {
            if (window.recalculateProductTable) window.recalculateProductTable();
            if (window.recalculateMarginsAndExpenses) window.recalculateMarginsAndExpenses();
            if (window.recalculateTaxes) window.recalculateTaxes();
            if (window.recalculateOrderTotals) window.recalculateOrderTotals();
        }

        const debouncedFullRecalc = debounce(fullRecalc, 300);
        [exchangeRateInput, bankRateInput, marginGlobalInput, unforeseenGlobalInput].forEach(el => {
            if (el) el.addEventListener('input', debouncedFullRecalc);
        });

        document.addEventListener('input', e => {
            if (e.target.matches('.quantity-input, .margin-input, .unforeseen-expenses-input, .rate-input')) {
                debouncedFullRecalc();
            }
        });

        function debounce(func, wait) {
            let timeout;
            return (...args) => { clearTimeout(timeout); timeout = setTimeout(() => func.apply(this, args), wait); };
        }

        function forceUpdateTotals() {
            document.querySelectorAll('.product-tax-table').forEach(table => {
                const taxInputs = Array.from(table.querySelectorAll('.tax-input'));
                taxInputs.forEach(input => {
                    const taxId = input.dataset.taxId;
                    const expectedTotal = window.toNumber(input) || 0;
                    let actualTotal = 0;
                    table.querySelectorAll(`.sum-input[data-tax-id="${taxId}"]`).forEach(cell => {
                        actualTotal += parseFloat(cell.dataset.rawValue) || 0;
                    });
                    const totalCell = table.querySelector(`.total-sum[data-tax-id="${taxId}"]`);
                    if (totalCell) {
                        totalCell.textContent = window.formatter.formatNumberWithSpaces(actualTotal);
                        totalCell.dataset.rawValue = actualTotal;
                        const mismatch = Math.abs(expectedTotal - actualTotal) > 0.015;
                        totalCell.classList.toggle('tax-mismatch', mismatch);
                        input.classList.toggle('tax-mismatch', mismatch);
                    }
                });
            });
        }

        // Построение таблиц налогов и первичный расчёт
        function initializeTaxTables() {
            if (!window.initialTaxes || !window.initialProducts) return;

            const savedState = new Map();

            // Восстанавливаем ручные состояния только для сохранённого заказа
            if (window.isOrderSaved) {
                window.initialProducts.forEach(product => {
                    const productState = {};
                    if (product.productTaxes) {
                        product.productTaxes.forEach(tax => {
                            const taxTypeIds = JSON.parse(document.getElementById('tax-type-ids').dataset.taxTypes);
                            const taxId = taxTypeIds[tax.name];
                            if (taxId) {
                                productState[taxId] = {
                                    value: tax.value.toString(),
                                    isCalculated: tax.isCalculated
                                };
                            }
                        });
                    }
                    savedState.set(product.productId.toString(), productState);
                });
            }

            var taxesWithUnit = window.initialTaxes.map(function (t) {
                return { id: t.id, name: t.name, cost: t.cost, unit: t.measureUnit || t.unit || '₽', isCalculated: t.isCalculated };
            });
            window.TaxColumnManager.buildInitialTables(taxesWithUnit, savedState);

            setTimeout(() => {
                if (window.recalculateProductTable) window.recalculateProductTable();
                if (window.recalculateMarginsAndExpenses) window.recalculateMarginsAndExpenses();
                if (window.recalculateTaxes) window.recalculateTaxes();
                if (window.recalculateOrderTotals) window.recalculateOrderTotals();
                if (window.setupCodeSearch) window.setupCodeSearch();
            }, 200);
        }

        marginGlobalInput.addEventListener('input', () => {
            if (window.syncGlobalMargin) window.syncGlobalMargin();
            fullRecalc();
        });
        unforeseenGlobalInput.addEventListener('input', () => {
            if (window.syncGlobalUnforeseen) window.syncGlobalUnforeseen();
            fullRecalc();
        });

        initializeTaxTables();
    });
})();