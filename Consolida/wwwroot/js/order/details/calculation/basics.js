(function () {
    'use strict';

    const fmt = window.formatter?.formatNumberWithSpaces || (n => n.toFixed(2));

    window.recalculateProductTable = function () {
        const exchangeRate = window.toNumber(document.getElementById('exchangeRateInput'));
        const bankCommissionRate = window.toNumber(document.getElementById('bankCommissionInput'));
        document.querySelectorAll('.product-row').forEach(row => {
            const priceCny = window.toNumber(row.querySelector('.price-cny').textContent.split('(')[0].trim());
            const quantity = window.toNumber(row.querySelector('.quantity-input'));
            const priceRub = priceCny * exchangeRate;
            const totalPriceRub = priceRub * quantity;
            const commission = priceRub * (bankCommissionRate / 100);
            const priceWithCommission = priceRub + commission;
            const totalPriceWithCommission = priceWithCommission * quantity;

            row.querySelector('.price-in-rubles').textContent = fmt(priceRub) + ' (₽)';
            row.querySelector('.price-in-rubles-total').textContent = fmt(totalPriceRub) + ' (₽)';
            row.querySelector('.bank-commission').textContent = fmt(commission) + ' (₽)';
            row.querySelector('.bank-commission-sum-cost').textContent = fmt(priceWithCommission) + ' (₽)';
            row.querySelector('.bank-commission-sum-cost-total').textContent = fmt(totalPriceWithCommission) + ' (₽)';
        });
    };

    window.recalculateMarginsAndExpenses = function () {
        const exchangeRate = window.toNumber(document.getElementById('exchangeRateInput'));
        let totalProfit = 0, totalUnforeseen = 0;
        document.querySelectorAll('.product-margin').forEach(row => {
            const productId = row.dataset.productId;
            const priceCny = window.toNumber(row.querySelector('[data-price-cny]').dataset.priceCny);
            const quantity = window.toNumber(document.querySelector(`.quantity-input[data-product-id="${productId}"]`));
            const marginRate = window.toNumber(row.querySelector('.margin-input'));
            const unforeseenRate = window.toNumber(row.querySelector('.unforeseen-expenses-input'));
            const dutyRate = window.toNumber(row.querySelector('.rate-input'));

            const priceRub = priceCny * exchangeRate;
            const marginCost = priceRub * (marginRate / 100);
            const priceAfterMargin = priceRub + marginCost;
            const dutyCost = priceRub * quantity * (dutyRate / 100);
            const unforeseenCost = priceRub * quantity * (unforeseenRate / 100);

            row.querySelector('.price-after-margin').textContent = fmt(priceAfterMargin) + ' (₽)';
            row.querySelector('.total-price-after-margin').textContent = fmt(priceAfterMargin * quantity) + ' (₽)';
            row.querySelector('.margin-cost').textContent = fmt(marginCost * quantity) + ' (₽)';
            row.querySelector('.price-with-rate').textContent = fmt(dutyCost) + ' (₽)';
            row.querySelector('.unforeseen-expenses-cost').textContent = fmt(unforeseenCost) + ' (₽)';

            totalProfit += marginCost * quantity;
            totalUnforeseen += unforeseenCost;
        });
        document.getElementById('total-profit').textContent = fmt(totalProfit) + ' (₽)';
        const unforeseenEl = document.getElementById('total-unforeseen-expenses');
        if (unforeseenEl) unforeseenEl.textContent = fmt(totalUnforeseen) + ' ₽';
    };

    window.syncGlobalMargin = function () {
        const globalValue = window.toNumber(document.getElementById('marginInput'));
        document.querySelectorAll('.margin-input').forEach(input => {
            if (input.dataset.fieldState === 'manual') return;
            input.value = globalValue.toFixed(2);
            input.dataset.numericValue = globalValue;
        });
    };

    window.syncGlobalUnforeseen = function () {
        const globalValue = window.toNumber(document.getElementById('unforeseenExpensesInput'));
        document.querySelectorAll('.unforeseen-expenses-input').forEach(input => {
            if (input.dataset.fieldState === 'manual') return;
            input.value = globalValue.toFixed(2);
            input.dataset.numericValue = globalValue;
        });
    };

    // Поиск кодов ТНВЭД
    window.setupCodeSearch = function () {
        document.querySelectorAll('.code-search').forEach(input => {
            // Поиск при вводе текста
            input.addEventListener('input', debounce(async (e) => {
                const row = e.target.closest('tr');
                const rateInput = row.querySelector('.rate-input');
                if (e.target.value.length < 2) return;

                try {
                    const resp = await fetch(`/api/OrderAPI/SearchCodes?query=${encodeURIComponent(e.target.value)}`);
                    if (!resp.ok) throw new Error('Network error');
                    const codes = await resp.json();

                    if (codes && codes.length > 0) {
                        const code = codes[0];
                        e.target.value = code.name;
                        e.target.dataset.codeId = code.id;
                        e.target.dataset.codeName = code.name;
                        e.target.dataset.isNewCode = 'false';

                        const newRate = code.rate.toFixed(2);
                        rateInput.value = newRate;
                        rateInput.dataset.numericValue = newRate;
                        rateInput.dataset.codeName = code.name;
                        rateInput.dataset.codeId = code.id;
                        rateInput.dataset.isNewCode = 'false';
                        rateInput.classList.remove('rate-highlight');

                        await saveCodeRate(code.name, code.rate, code.id);
                        if (window.recalculateMarginsAndExpenses) window.recalculateMarginsAndExpenses();
                        if (window.recalculateOrderTotals) window.recalculateOrderTotals();
                    } else {
                        // Код не найден – готовим поле для ручного ввода
                        e.target.dataset.codeId = '';
                        e.target.dataset.isNewCode = 'true';
                        e.target.dataset.codeName = e.target.value;

                        rateInput.dataset.codeId = '';
                        rateInput.dataset.isNewCode = 'true';
                        rateInput.dataset.codeName = e.target.value;
                        rateInput.classList.add('rate-highlight');
                        rateInput.value = '';
                        if (window.recalculateMarginsAndExpenses) window.recalculateMarginsAndExpenses();
                    }
                } catch (error) {
                    console.error('Search error:', error);
                }
            }, 500));

            // Обработчик ручного ввода ставки для НОВОГО кода
            const row = input.closest('tr');
            const rateInput = row.querySelector('.rate-input');
            if (rateInput) {
                rateInput.addEventListener('change', async function () {
                    const codeName = this.dataset.codeName || row.querySelector('.code-search')?.value;
                    const isNewCode = this.dataset.isNewCode === 'true';
                    const rawValue = this.value.trim();

                    if (isNewCode && codeName && rawValue !== '') {
                        const newRate = window.toNumber(rawValue);
                        const result = await saveCodeRate(codeName, newRate, null);
                        if (result && result.codeId) {
                            const codeSearch = row.querySelector('.code-search');
                            if (codeSearch) {
                                codeSearch.dataset.codeId = result.codeId;
                                codeSearch.dataset.isNewCode = 'false';
                            }
                            this.dataset.codeId = result.codeId;
                            this.dataset.isNewCode = 'false';
                            this.classList.remove('rate-highlight');
                        }
                    } else if (!isNewCode && codeName) {
                        const currentRate = window.toNumber(this.value);
                        await saveCodeRate(codeName, currentRate, parseInt(this.dataset.codeId, 10));
                    }
                    if (window.recalculateMarginsAndExpenses) window.recalculateMarginsAndExpenses();
                    if (window.recalculateOrderTotals) window.recalculateOrderTotals();
                });
            }
        });
    };

    async function saveCodeRate(name, rate, codeId) {
        try {
            const resp = await fetch('/api/OrderAPI/UpdateCodeRate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Id: codeId, Name: name, NewRate: rate })
            });
            return await resp.json();
        } catch (e) {
            console.error('SaveCodeRate error:', e);
            return null;
        }
    }

    function debounce(fn, delay) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    // Инициализация после загрузки
    document.addEventListener('DOMContentLoaded', () => {
        if (window.setupCodeSearch) window.setupCodeSearch();
    });
})();