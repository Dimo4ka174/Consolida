window.TaxColumnManager = (function () {
    const ITEMS_PER_TABLE = 5;
    const METROLOGICAL_TAX_NAME = 'Первичная поверка';
    let container = null;

    const formatNumber = (number) => {
        if (window.formatter?.formatNumberWithSpaces) {
            return window.formatter.formatNumberWithSpaces(number);
        }
        return number.toFixed(2);
    };

    const init = () => {
        container = document.getElementById('taxTablesContainer');
    };

    const saveCurrentState = () => {
        return window.TaxStateManager?.saveAllFieldStates() || new Map();
    };

    const removeTaxColumn = (taxId) => {
        if (!container) container = document.getElementById('taxTablesContainer');
        if (!container) return;

        const savedState = saveCurrentState();
        const allTaxes = [];

        document.querySelectorAll('.tax-badge-modern').forEach(badge => {
            const tid = badge.dataset.taxId;
            if (tid && tid !== taxId) {
                allTaxes.push({
                    id: tid,
                    name: badge.dataset.taxName,
                    cost: parseFloat(badge.dataset.taxCost) || 0,
                    unit: badge.dataset.taxUnit || '₽'
                });
            }
        });

        if (allTaxes.length === 0) {
            container.innerHTML = '<p class="text-body-secondary text-center mb-0">Нет дополнительных расходов</p>';
            return;
        }

        rebuildAllTables(allTaxes, savedState);
    };

    const addTaxColumn = (taxData) => {
        if (!container) container = document.getElementById('taxTablesContainer');
        if (!container) return;

        const savedState = saveCurrentState();
        const allTaxes = [];

        document.querySelectorAll('.tax-badge-modern').forEach(badge => {
            allTaxes.push({
                id: badge.dataset.taxId,
                name: badge.dataset.taxName,
                cost: parseFloat(badge.dataset.taxCost) || 0,
                unit: badge.dataset.taxUnit || '₽'
            });
        });

        rebuildAllTables(allTaxes, savedState);
    };

    const rebuildAllTables = (allTaxes, savedState) => {
        container.innerHTML = '';
        if (allTaxes.length === 0) {
            container.innerHTML = '<p class="text-body-secondary text-center mb-0">Нет дополнительных расходов</p>';
            return;
        }

        // Разделяем налоги: обычные и «Первичная поверка» (её всегда выносим отдельно)
        const regularTaxes = allTaxes.filter(t => t.name !== METROLOGICAL_TAX_NAME);
        const metroTaxes = allTaxes.filter(t => t.name === METROLOGICAL_TAX_NAME);

        // Группируем обычные налоги по ITEMS_PER_TABLE
        const taxGroups = [];
        for (let i = 0; i < regularTaxes.length; i += ITEMS_PER_TABLE) {
            taxGroups.push(regularTaxes.slice(i, i + ITEMS_PER_TABLE));
        }

        // Метро — всегда отдельной таблицей в конце
        if (metroTaxes.length > 0) {
            taxGroups.push(metroTaxes);
        }

        taxGroups.forEach((group, groupIndex) => {
            const table = createEmptyTable(groupIndex);
            group.forEach(tax => addColumnToTable(table, groupIndex, tax, savedState));
        });

        setTimeout(() => {
            updateTableIndices();
            window.TaxStateManager?.restoreFieldStates(savedState);
            if (window.setupCodeSearch) window.setupCodeSearch();
        }, 50);
    };

    const createEmptyTable = (index) => {
        const template = document.getElementById('taxTableTemplate');
        const table = template.content.cloneNode(true).querySelector('table');
        table.dataset.tableIndex = index;
        container.appendChild(table);
        return table;
    };

    const addColumnToTable = (table, tableIndex, taxData, savedState) => {
        const isMetro = taxData.name === METROLOGICAL_TAX_NAME;
        const colspan = isMetro ? 4 : 2;

        const headerRow = table.querySelector('thead tr:first-child');
        const inputRow = table.querySelector('thead tr:nth-child(2)');
        const labelRow = table.querySelector('thead tr:nth-child(3)');
        const tbody = table.querySelector('tbody');
        const footerRow = table.querySelector('tfoot tr');

        // ===== Заголовок =====
        const headerTemplate = document.getElementById('taxColumnTemplate');
        const headerClone = headerTemplate.content.cloneNode(true);
        const headerCell = headerClone.querySelector('th');
        headerCell.setAttribute('colspan', String(colspan));
        headerCell.dataset.taxId = taxData.id;
        headerCell.dataset.taxName = taxData.name;
        headerCell.querySelector('.tax-name').textContent = taxData.name;
        headerCell.querySelector('.tax-unit').textContent = `(${taxData.unit})`;
        headerCell.querySelector('.remove-tax-from-table').dataset.taxId = taxData.id;
        headerCell.querySelector('.remove-tax-from-table').dataset.taxName = taxData.name;
        headerRow.appendChild(headerCell);

        // ===== Общий tax-input (colspan растягиваем) =====
        const inputTemplate = document.getElementById('taxInputTemplate');
        const inputClone = inputTemplate.content.cloneNode(true);
        const input = inputClone.querySelector('input');
        input.dataset.taxId = taxData.id;
        input.dataset.taxName = taxData.name;
        input.value = (taxData.cost || 0).toFixed(2);
        const inputTd = inputClone.querySelector('td');
        inputTd.setAttribute('colspan', String(colspan));
        inputRow.appendChild(inputTd);

        // ===== Заголовки колонок (Ед., Сум. + возможно 2 доп.) =====
        const labelTemplate = document.getElementById('taxHeaderLabelsTemplate');
        labelTemplate.content.cloneNode(true).querySelectorAll('th').forEach(label => {
            labelRow.appendChild(label.cloneNode(true));
        });
        if (isMetro) {
            ['Номер ГРСИ', 'Срок действия'].forEach(text => {
                const th = document.createElement('th');
                th.textContent = text;
                th.className = 'text-center metrological-header';
                labelRow.appendChild(th);
            });
        }

        // ===== Строки товаров =====
        document.querySelectorAll('.product-row').forEach((product, idx) => {
            const productId = product.dataset.productId;
            let row = tbody.querySelector(`tr[data-product-id="${productId}"]`);

            if (!row) {
                row = document.createElement('tr');
                row.className = 'product-tax';
                row.dataset.productId = productId;
                row.dataset.productIndex = idx;
                row.innerHTML = `<td>${idx + 1}</td>`;
                tbody.appendChild(row);
            }

            const cellTemplate = document.getElementById('taxCellTemplate');
            const cellClone = cellTemplate.content.cloneNode(true);

            const unitInput = cellClone.querySelector('.unit-input');
            unitInput.dataset.taxId = taxData.id;
            unitInput.dataset.taxName = taxData.name;
            unitInput.dataset.uniqueId = `unit_${tableIndex}_${taxData.id}_${idx}`;

            const sumCell = cellClone.querySelector('.sum-input');
            sumCell.dataset.taxId = taxData.id;
            sumCell.dataset.productId = productId;

            if (savedState?.has(productId) && savedState.get(productId)[taxData.id]) {
                const saved = savedState.get(productId)[taxData.id];
                unitInput.value = saved.value;
                unitInput.dataset.isCalculated = saved.isCalculated ? 'true' : 'false';
                unitInput.dataset.numericValue = window.toNumber(saved.value);

                if (!saved.isCalculated && saved.value && parseFloat(saved.value) !== 0) {
                    unitInput.dataset.fieldState = 'manual';
                    unitInput.classList.add('unit-modified');
                    unitInput.classList.remove('unit-auto', 'unit-manual-zero');
                } else {
                    unitInput.dataset.fieldState = 'auto';
                    unitInput.classList.add('unit-auto');
                    unitInput.classList.remove('unit-modified', 'unit-manual-zero');
                }

                const total = window.toNumber(saved.value) * getProductQuantity(productId);
                sumCell.textContent = window.formatter?.formatNumberWithSpaces(total) || total.toFixed(2);
                sumCell.dataset.rawValue = total;
            } else {
                unitInput.value = '0.00';
                unitInput.dataset.fieldState = 'auto';
                unitInput.dataset.isCalculated = 'false';
                unitInput.dataset.numericValue = 0;
                unitInput.classList.add('unit-auto');
                unitInput.classList.remove('unit-modified', 'unit-manual-zero');
                sumCell.textContent = '0.00';
                sumCell.dataset.rawValue = 0;
            }

            row.appendChild(cellClone.querySelector('td:first-child'));
            row.appendChild(cellClone.querySelector('.sum-input'));

            // ===== Доп. 2 колонки для «Первичной поверки» =====
            if (isMetro) {
                const savedNumber = savedState?.get(productId)?.['__metrological_number']?.value;
                const savedExpiry = savedState?.get(productId)?.['__metrological_expiry']?.value;
                const productMeta = (window.initialProducts || []).find(
                    p => String(p.productId) === String(productId)
                );

                const numberTd = document.createElement('td');
                numberTd.className = 'text-center';
                const numberInput = document.createElement('input');
                numberInput.type = 'text';
                numberInput.className = 'form-control form-control-small metrological-number-input';
                numberInput.dataset.productId = productId;
                numberInput.value = savedNumber ?? productMeta?.metrologicalNumber ?? '';
                numberTd.appendChild(numberInput);

                const expiryTd = document.createElement('td');
                expiryTd.className = 'text-center';
                const expiryInput = document.createElement('input');
                expiryInput.type = 'date';
                expiryInput.className = 'form-control form-control-small metrological-expiry-input';
                expiryInput.dataset.productId = productId;
                expiryInput.value = savedExpiry ?? productMeta?.metrologicalExpiryDate ?? '';
                expiryTd.appendChild(expiryInput);

                row.appendChild(numberTd);
                row.appendChild(expiryTd);
            }
        });

        // ===== Футер =====
        const footerTemplate = document.getElementById('taxTotalTemplate');
        const footerClone = footerTemplate.content.cloneNode(true);
        const totalCell = footerClone.querySelector('.total-sum');
        totalCell.dataset.taxId = taxData.id;
        totalCell.dataset.taxName = taxData.name;
        totalCell.dataset.rawValue = taxData.cost || 0;
        totalCell.textContent = window.formatter?.formatNumberWithSpaces(taxData.cost || 0) || (taxData.cost || 0).toFixed(2);
        footerRow.appendChild(footerClone.querySelector('td:first-child'));
        footerRow.appendChild(totalCell);

        if (isMetro) {
            footerRow.appendChild(document.createElement('td'));
            footerRow.appendChild(document.createElement('td'));
        }
    };

    const getProductQuantity = (productId) => {
        const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
        return input ? window.toNumber(input) : 0;
    };

    const updateTableIndices = () => {
        container.querySelectorAll('.product-tax-table').forEach((table, index) => {
            table.dataset.tableIndex = index;
            table.querySelectorAll('tbody tr').forEach((row, rowIndex) => {
                row.querySelector('td:first-child').textContent = rowIndex + 1;
                row.dataset.productIndex = rowIndex;
            });
        });
    };

    // Функция для начального построения таблиц
    const buildInitialTables = (taxes, savedState) => {
        rebuildAllTables(taxes, savedState);
    };

    return { init, removeTaxColumn, addTaxColumn, buildInitialTables };
})();