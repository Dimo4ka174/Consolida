(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const DEBOUNCE_MS = 400;
        const debounceTimers = new WeakMap();

        function getTwoMonthsFromNow() {
            const d = new Date();
            d.setMonth(d.getMonth() + 2);
            return d;
        }

        function highlightIfExpiring(input) {
            const value = input.value;
            if (!value) {
                input.classList.remove('date-expiring');
                return;
            }
            const expiryDate = new Date(value);
            if (isNaN(expiryDate.getTime())) {
                input.classList.remove('date-expiring');
                return;
            }
            if (expiryDate.getTime() < getTwoMonthsFromNow().getTime()) {
                input.classList.add('date-expiring');
            } else {
                input.classList.remove('date-expiring');
            }
        }

        async function fetchMetrologicalInfo(number) {
            try {
                const response = await fetch(
                    `/api/OrderAPI/SearchMetrologicalInfo?number=${encodeURIComponent(number)}`
                );
                if (!response.ok) return null;
                const data = await response.json();
                return data && data.success ? data : null;
            } catch (err) {
                console.error('MetrologicalInfo lookup failed:', err);
                return null;
            }
        }

        // Автодополнение по номеру ГРСИ (делегированный обработчик — работает и на динамических полях)
        document.addEventListener('input', function (e) {
            const target = e.target;
            if (!target.classList.contains('metrological-number-input')) return;

            const value = target.value.trim();

            const prev = debounceTimers.get(target);
            if (prev) clearTimeout(prev);

            if (value.length === 0) return;

            const timer = setTimeout(async () => {
                const data = await fetchMetrologicalInfo(value);
                if (data && data.expiryDate) {
                    const productId = target.dataset.productId;
                    const expiryInput = document.querySelector(
                        `.metrological-expiry-input[data-product-id="${productId}"]`
                    );
                    if (expiryInput) {
                        expiryInput.value = data.expiryDate;
                        highlightIfExpiring(expiryInput);
                    }
                }
            }, DEBOUNCE_MS);

            debounceTimers.set(target, timer);
        });

        // Подсветка даты при вводе/изменении
        document.addEventListener('input', function (e) {
            if (e.target.classList.contains('metrological-expiry-input')) {
                highlightIfExpiring(e.target);
            }
        });
        document.addEventListener('change', function (e) {
            if (e.target.classList.contains('metrological-expiry-input')) {
                highlightIfExpiring(e.target);
            }
        });

        // Первичная подсветка уже отрисованных полей
        document.querySelectorAll('.metrological-expiry-input').forEach(highlightIfExpiring);
    });
})();