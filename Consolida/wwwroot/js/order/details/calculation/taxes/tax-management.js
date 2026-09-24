document.addEventListener('DOMContentLoaded', function () {
    // Кешируем DOM элементы
    const elements = {
        searchInput: document.getElementById('taxSearchInput'),
        searchResults: document.getElementById('taxSearchResults'),
        searchStatus: document.getElementById('searchStatus'),
        badgesContainer: document.getElementById('currentTaxesList'),
        noTaxesMsg: document.getElementById('noTaxesMessage'),
        cardBody: document.querySelector('.card-body')
    };

    const orderId = document.getElementById('orderId')?.value;

    let searchTimeout = null;
    let currentRequest = null;

    if (!elements.searchInput) return;

    // Кнопка сворачивания
    const cardHeader = document.querySelector('.card-header');
    if (cardHeader) {
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'btn btn-sm btn-link text-secondary p-0 ms-2';
        toggleBtn.innerHTML = '<i class="bi bi-chevron-up"></i>';
        toggleBtn.title = 'Свернуть/развернуть';

        const title = cardHeader.querySelector('h5');
        title?.appendChild(toggleBtn);

        let isCollapsed = false;
        toggleBtn.addEventListener('click', () => {
            isCollapsed = !isCollapsed;
            if (elements.cardBody) {
                elements.cardBody.style.display = isCollapsed ? 'none' : 'block';
                toggleBtn.innerHTML = isCollapsed ? '<i class="bi bi-chevron-down"></i>' : '<i class="bi bi-chevron-up"></i>';
            }
        });
    }

    // Очистка поиска
    document.getElementById('clearSearchBtn')?.addEventListener('click', clearSearch);
    document.getElementById('cancelTaxSearchBtn')?.addEventListener('click', clearSearch);

    function clearSearch() {
        elements.searchInput.value = '';
        elements.searchInput.focus();
        elements.searchResults.innerHTML = '';
        updateSearchStatus('');
    }

    // Поиск с debounce
    elements.searchInput.addEventListener('input', function () {
        const query = this.value.trim();

        if (query.length < 2) {
            elements.searchResults.innerHTML = '';
            updateSearchStatus(query.length > 0 ? 'Введите минимум 2 символа' : '');
            return;
        }

        updateSearchStatus('Поиск...');
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => searchTaxTypes(query), 300);
    });

    function updateSearchStatus(message) {
        if (elements.searchStatus) {
            elements.searchStatus.textContent = message;
        }
    }

    async function searchTaxTypes(query) {
        if (currentRequest) {
            currentRequest.abort();
        }

        const controller = new AbortController();
        currentRequest = controller;

        try {
            const response = await fetch(`/api/OrderAPI/SearchTaxTypes?query=${encodeURIComponent(query)}`, {
                signal: controller.signal
            });

            if (!response.ok) throw new Error('Ошибка поиска');

            const taxes = await response.json();
            displaySearchResults(taxes);
            updateSearchStatus(`Найдено: ${taxes.length}`);
        } catch (error) {
            if (error.name === 'AbortError') return;

            console.error('Ошибка поиска:', error);
            elements.searchResults.innerHTML = '<div class="search-result-item-modern error">Ошибка при поиске</div>';
            updateSearchStatus('Ошибка');
        } finally {
            currentRequest = null;
        }
    }

    function displaySearchResults(taxes) {
        if (!taxes?.length) {
            elements.searchResults.innerHTML = '<div class="search-result-item-modern empty">Ничего не найдено</div>';
            return;
        }

        const existingIds = new Set(
            Array.from(document.querySelectorAll('.tax-badge-modern'))
                .map(badge => badge.dataset.taxId)
        );

        const availableTaxes = taxes.filter(tax => !existingIds.has(tax.id.toString()));

        if (!availableTaxes.length) {
            elements.searchResults.innerHTML = '<div class="search-result-item-modern empty">Все расходы уже добавлены</div>';
            return;
        }

        elements.searchResults.innerHTML = availableTaxes
            .map(tax => `
                <div class="search-result-item-modern" 
                     data-tax-id="${tax.id}"
                     data-tax-name="${escapeHtml(tax.name)}"
                     data-tax-cost="${tax.cost}"
                     data-tax-unit="${tax.measureUnit}">
                    <div class="search-result-icon">
                        <i class="bi bi-plus-circle"></i>
                    </div>
                    <div class="search-result-content">
                        <div class="search-result-name">${escapeHtml(tax.name)}</div>
                        <div class="search-result-details">${formatMoney(tax.cost)} ${tax.measureUnit}</div>
                    </div>
                </div>
            `)
            .join('');

        elements.searchResults.querySelectorAll('.search-result-item-modern').forEach(item => {
            item.addEventListener('click', () => addTaxToOrder({
                id: item.dataset.taxId,
                name: item.dataset.taxName,
                cost: parseFloat(item.dataset.taxCost),
                unit: item.dataset.taxUnit
            }));
        });
    }

    // Вспомогательные функции
    const escapeHtml = (unsafe) => {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' };
        return unsafe.replace(/[&<>"]/g, m => map[m]);
    };

    const formatMoney = (amount) => {
        return new Intl.NumberFormat('ru-RU', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(amount);
    };

    // ========== Функция обновления карточки ГРСИ ==========

    async function addTaxToOrder(taxData) {
        const searchResults = elements.searchResults;
        const originalContent = searchResults.innerHTML;

        try {
            searchResults.innerHTML = '<div class="search-result-item-modern loading">Добавление...</div>';

            const response = await fetch('/api/OrderAPI/AddTaxToOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    OrderId: parseInt(orderId, 10),
                    TaxTypeId: parseInt(taxData.id, 10)
                })
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Ошибка при добавлении');
            }

            if (result.success) {
                const badge = createBadgeElement(result.tax);

                if (!elements.badgesContainer) {
                    createBadgesContainer();
                }

                const existingBadge = document.querySelector(`.tax-badge-modern[data-tax-id="${result.tax.id}"]`);
                if (existingBadge) {
                    showNotification(`Расход "${result.tax.name}" уже существует`, 'warning');
                    searchResults.innerHTML = originalContent;
                    return;
                }

                elements.badgesContainer?.appendChild(badge);

                // Используем новый менеджер для добавления колонки
                if (window.TaxColumnManager) {
                    window.TaxColumnManager.addTaxColumn(result.tax);
                } else if (window.addTaxColumnToTable) {
                    window.addTaxColumnToTable(result.tax);
                } else {
                    console.error('Нет доступной функции для добавления колонки');
                }

                clearSearch();
                showNotification(`Расход "${result.tax.name}" добавлен`, 'success');

                if (elements.noTaxesMsg) {
                    elements.noTaxesMsg.style.display = 'none';
                }

                setTimeout(() => badge.classList.remove('added'), 300);
            }
        } catch (error) {
            console.error('Ошибка:', error);
            showNotification(error.message, 'danger');
            searchResults.innerHTML = originalContent;
        }
    }

    function createBadgesContainer() {
        const container = document.createElement('div');
        container.id = 'currentTaxesList';
        container.className = 'tax-badges-container';

        const hint = elements.cardBody?.querySelector('.d-flex.align-items-center.mt-3');
        if (hint) {
            elements.cardBody.insertBefore(container, hint);
        } else {
            elements.cardBody?.appendChild(container);
        }

        elements.badgesContainer = container;
        return container;
    }

    function createBadgeElement(tax) {
        const badge = document.createElement('div');
        badge.className = 'tax-badge-modern added';
        badge.dataset.taxId = tax.id;
        badge.dataset.taxName = tax.name;
        badge.dataset.taxCost = tax.cost;
        badge.dataset.taxUnit = tax.measureUnit;

        badge.innerHTML = `
            <div class="tax-badge-icon">
                <i class="bi ${getIconClass(tax.name)}"></i>
            </div>
            <div class="tax-badge-content">
                <div class="tax-badge-name">${escapeHtml(tax.name)}</div>
                <div class="tax-badge-value">${formatMoney(tax.cost)} ${tax.measureUnit}</div>
            </div>
            <button type="button" class="tax-badge-remove remove-tax-badge" 
                    data-tax-id="${tax.id}"
                    data-tax-name="${tax.name}"
                    title="Удалить расход">
            </button>
        `;

        return badge;
    }

    const getIconClass = (name) => {
        if (name.includes('Таможен')) return 'bi-truck';
        if (name.includes('Достав')) return 'bi-box-seam';
        if (name.includes('Деклар')) return 'bi-file-text';
        if (name.includes('Термин')) return 'bi-building';
        return 'bi-receipt';
    };

    // Удаление расхода
    document.addEventListener('click', async function (e) {
        const removeBtn = e.target.closest('.remove-tax-badge');
        if (!removeBtn) return;

        e.preventDefault();

        const taxId = removeBtn.dataset.taxId;
        const taxName = removeBtn.dataset.taxName;

        if (!await showConfirmModal(`Удалить расход "${taxName}"?`)) {
            return;
        }

        const originalContent = removeBtn.innerHTML;
        removeBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
        removeBtn.disabled = true;

        try {
            const response = await fetch('/api/OrderAPI/RemoveTaxFromOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    OrderId: parseInt(orderId, 10),
                    TaxTypeId: parseInt(taxId, 10)
                })
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Ошибка при удалении');
            }

            if (result.success) {
                const badge = removeBtn.closest('.tax-badge-modern');
                if (badge) {
                    badge.style.transition = 'all 0.3s';
                    badge.style.opacity = '0';
                    badge.style.transform = 'scale(0.8)';

                    setTimeout(() => {
                        badge.remove();
                        checkEmptyState();
                    }, 300);
                }

                // Используем новый менеджер для удаления колонки
                if (window.TaxColumnManager) {
                    window.TaxColumnManager.removeTaxColumn(taxId);
                } else if (window.removeTaxColumn) {
                    window.removeTaxColumn(taxId);
                } else {
                    console.error('Нет доступной функции для удаления колонки');
                }

                showNotification(`Расход "${taxName}" удален`, 'info');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            showNotification(error.message, 'danger');
            removeBtn.innerHTML = originalContent;
            removeBtn.disabled = false;
        }
    });

    function checkEmptyState() {
        if (!elements.badgesContainer?.children.length) {
            showEmptyState();
        }
    }

    function showEmptyState() {
        if (elements.badgesContainer) {
            elements.badgesContainer.remove();
        }

        if (!elements.noTaxesMsg) {
            const emptyState = document.createElement('div');
            emptyState.id = 'noTaxesMessage';
            emptyState.className = 'empty-state text-center py-4';
            emptyState.innerHTML = `
                <div class="empty-state-icon mb-3">
                    <i class="bi bi-receipt fs-1 text-muted opacity-50"></i>
                </div>
                <p class="text-muted mb-0">Нет дополнительных расходов</p>
            `;

            const hint = elements.cardBody?.querySelector('.d-flex.align-items-center.mt-3');
            if (hint) {
                elements.cardBody.insertBefore(emptyState, hint);
            }
        } else {
            elements.noTaxesMsg.style.display = 'block';
        }
    }

    function showNotification(message, type = 'info') {
        const container = document.getElementById('globalNotificationContainer');
        if (!container) return;

        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.role = 'alert';
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        container.appendChild(alertDiv);
        setTimeout(() => {
            alertDiv.classList.remove('show');
            setTimeout(() => alertDiv.remove(), 300);
        }, 5000);
    }

    function showConfirmModal(message) {
        return new Promise((resolve) => {
            const modal = document.getElementById('custom-modal');
            const modalMessage = document.getElementById('modal-message');
            const confirmBtn = document.getElementById('modal-confirm');
            const cancelBtn = document.getElementById('modal-cancel');

            if (!modal || !modalMessage || !confirmBtn || !cancelBtn) {
                resolve(confirm(message));
                return;
            }

            modalMessage.textContent = message;
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();

            const onConfirm = () => {
                cleanup();
                resolve(true);
            };

            const onCancel = () => {
                cleanup();
                resolve(false);
            };

            const cleanup = () => {
                confirmBtn.removeEventListener('click', onConfirm);
                cancelBtn.removeEventListener('click', onCancel);
                modal.removeEventListener('hidden.bs.modal', onModalHide);
                bsModal.hide();
            };

            const onModalHide = () => {
                cleanup();
                resolve(false);
            };

            confirmBtn.addEventListener('click', onConfirm);
            cancelBtn.addEventListener('click', onCancel);
            modal.addEventListener('hidden.bs.modal', onModalHide);
        });
    }
});