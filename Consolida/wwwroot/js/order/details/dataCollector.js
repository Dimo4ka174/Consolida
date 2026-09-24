/**
 * Централизованный сбор данных заказа для сохранения/экспорта.
 * Использует общие DOM-элементы и TaxTypeIds из глобальной области.
 */
window.dataCollector = (function () {
    'use strict';

    // Получение TaxTypeIds (передаётся из Razor)
    function getTaxTypeIds() {
        const el = document.getElementById('tax-type-ids');
        if (el && el.dataset.taxTypes) {
            try {
                return JSON.parse(el.dataset.taxTypes);
            } catch (e) {
                console.error('dataCollector: failed to parse TaxTypeIds', e);
            }
        }
        return {};
    }

    // Поиск названия налога по ID (по таблице tax-input)
    function getTaxNameById(taxId, taxTable) {
        // Ищем заголовок колонки в таблице
        const headerCell = taxTable?.querySelector(`th[data-tax-id="${taxId}"]`);
        if (headerCell) {
            const nameEl = headerCell.querySelector('.tax-name');
            if (nameEl) return nameEl.textContent.trim();
            // fallback: текст до скобки
            return headerCell.textContent.split('(')[0].trim();
        }
        return null;
    }

    // Сбор общих налогов заказа
    function collectOrderTaxes(taxTypeIds) {
        const taxes = {};

        const commissionInput = document.getElementById('bankCommissionInput');
        if (commissionInput && commissionInput.dataset.taxId) {
            const id = parseInt(commissionInput.dataset.taxId);
            taxes[id] = window.toNumber(commissionInput.value);
        }

        const marginInput = document.getElementById('marginInput');
        if (marginInput && marginInput.dataset.taxId) {
            const id = parseInt(marginInput.dataset.taxId);
            taxes[id] = window.toNumber(marginInput.value);
        }

        const unforeseenInput = document.getElementById('unforeseenExpensesInput');
        if (unforeseenInput && unforeseenInput.dataset.taxId) {
            const id = parseInt(unforeseenInput.dataset.taxId);
            taxes[id] = window.toNumber(unforeseenInput.value);
        }

        // Бейджи – единственный верный источник общих ставок
        document.querySelectorAll('.tax-badge-modern').forEach(badge => {
            const taxId = parseInt(badge.dataset.taxId);
            if (!taxId || taxes.hasOwnProperty(taxId)) return;
            taxes[taxId] = parseFloat(badge.dataset.taxCost) || 0;
        });

        return taxes;
    }

    // Сбор данных товаров
    function collectProductsData(taxTypeIds) {
        const products = [];
        document.querySelectorAll('.product-row').forEach((row, index) => {
            const productId = parseInt(row.dataset.productId);
            const marginRow = document.querySelector(`.product-margin[data-product-id="${productId}"]`);
            const costRow = document.querySelector(`.product-cost[data-index="${index}"]`);

            const product = {
                ProductId: productId,
                ProductName: row.querySelector('td:nth-child(2)').textContent.trim(),
                Model: row.querySelector('td:nth-child(3)').textContent.trim(),
                DeliveryDate: row.querySelector('.delivery-date')?.textContent || null,
                Comment: document.querySelector(`.comment-input[data-product-id="${productId}"]`)?.value || '',
                LeadTime: getNumericInput(row.querySelector('.lead-time-input')),
                Quantity: getNumericInput(row.querySelector('.quantity-input')),
                Price: getNumericCell(row.querySelector('.price-cny')),
                Weight: getNumericInput(row.querySelector('.weight-input')),
                CodeTNVDId: getCodeTNVDId(marginRow),
                CodeTNVD: marginRow?.querySelector('.code-search')?.value || '',
                MetrologicalNumber: document.querySelector(`.metrological-number-input[data-product-id="${productId}"]`)?.value.trim() || null,
                MetrologicalExpiryDate: document.querySelector(`.metrological-expiry-input[data-product-id="${productId}"]`)?.value || null,
                MarginRate: window.toNumber(marginRow?.querySelector('.margin-input')),
                UnforeseenExpensesRate: window.toNumber(marginRow?.querySelector('.unforeseen-expenses-input')),
                ProductTaxes: collectProductTaxes(productId, taxTypeIds, marginRow),
                CalculatedTotals: getCalculatedTotals(costRow)  
            };

            products.push(product);
        });

        return products;
    }

    function getNumericInput(input) {
        if (!input) return 0;
        return window.toNumber(input) || 0;
    }

    function getNumericCell(cell) {
        if (!cell) return 0;
        const text = cell.textContent;
        if (window.formatter?.parseFormattedNumber) {
            return window.formatter.parseFormattedNumber(text);
        }
        return parseFloat(text.replace(/[^\d.,]/g, '').replace(',', '.')) || 0;
    }

    function getCodeTNVDId(marginRow) {
        if (!marginRow) return null;
        const codeInput = marginRow.querySelector('.code-search');
        const codeId = codeInput?.dataset.codeId;
        return codeId ? parseInt(codeId) : null;
    }

    function collectProductTaxes(productId, taxTypeIds, marginRow) {
        const taxes = {};
        const taxRows = document.querySelectorAll(`.product-tax[data-product-id="${productId}"]`);
        taxRows.forEach(taxRow => {
            taxRow.querySelectorAll('.unit-input').forEach(input => {
                const taxId = parseInt(input.dataset.taxId);
                if (!taxId) return;
                const isManual = input.dataset.fieldState === 'manual';
                taxes[taxId] = {
                    value: window.toNumber(input),
                    isManual: isManual
                };
            });
        });

        // Маржа и непредвиденные
        if (marginRow) {
            const marginInput = marginRow.querySelector('.margin-input');
            if (marginInput && taxTypeIds["Маржа"]) {
                const isManual = marginInput.dataset.fieldState === 'manual';
                taxes[taxTypeIds["Маржа"]] = {
                    value: window.toNumber(marginInput),
                    isManual: isManual
                };
            }
            const unforeseenInput = marginRow.querySelector('.unforeseen-expenses-input');
            if (unforeseenInput && taxTypeIds["Не предвиденные расходы"]) {
                const isManual = unforeseenInput.dataset.fieldState === 'manual';
                taxes[taxTypeIds["Не предвиденные расходы"]] = {
                    value: window.toNumber(unforeseenInput),
                    isManual: isManual
                };
            }
        }
        return taxes;
    }

    function getCalculatedTotals(costRow) {
        return {
            UnitPriceWithoutVAT: getCellValue(costRow, '.price-without-vat-unit'),
            AmountWithoutVAT: getCellValue(costRow, '.price-without-vat-total'),
            UnitPriceVAT: getCellValue(costRow, '.price-with-vat-unit'),
            AmountVAT: getCellValue(costRow, '.price-with-vat-total')
        };
    }

    function getCellValue(row, selector) {
        const cell = row?.querySelector(selector);
        if (!cell) return 0;
        const raw = cell.dataset?.rawValue || cell.textContent;
        const cleaned = String(raw).replace(/\s/g, '').replace(',', '.');
        return parseFloat(cleaned) || 0;
    }

    // Публичный метод сбора полного заказа
    function collectOrderData(orderId, isDownload = false) {
        const taxTypeIds = getTaxTypeIds();
        const statusSelect = document.getElementById('statusSelect');
        const statusId = statusSelect ? parseInt(statusSelect.value) : 1;

        return {
            OrderId: parseInt(orderId),
            OrderNumber: extractOrderNumber(),
            ExchangeRate: window.toNumber(document.getElementById('exchangeRateInput')),
            DownloadDocument: isDownload,
            DateCreationTKP: isDownload ? new Date().toISOString() : null,
            Comment: document.getElementById('orderComment')?.value || '',
            StatusId: statusId,
            Products: collectProductsData(taxTypeIds),
            OrderTaxes: collectOrderTaxes(taxTypeIds),
        };
    }

    function extractOrderNumber() {
        const header = document.getElementById('order-number-header');
        return header ? header.textContent.replace('Заказ №', '').trim() : '';
    }

    return {
        collectOrderData: collectOrderData
    };
})();
