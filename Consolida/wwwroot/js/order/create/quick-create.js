$(function () {
    // --- Элементы для компании и клиента ---
    var companyInput = $('#companySearchInput');
    var companyIdField = $('#CompanyId');
    var companySuggestions = $('#companySuggestions');

    var customerInput = $('#customerSearchInput');
    var customerIdField = $('#CustomerId');
    var customerSuggestions = $('#customerSuggestions');

    var addCompanyBtn = $('#addCompanyBtn');
    var addCustomerBtn = $('#addCustomerBtn');

    var companyModal = new bootstrap.Modal(document.getElementById('createCompanyModal'));
    var customerModal = new bootstrap.Modal(document.getElementById('createCustomerModal'));

    // --- Элементы для продукта ---
    var addProductBtn = $('#addProductBtn');
    var productModal = new bootstrap.Modal(document.getElementById('createProductModal'));
    var saveProductBtn = $('#saveProductBtn');

    var newProductName = $('#newProductName');
    var newProductModel = $('#newProductModel');
    var newProductPrice = $('#newProductPrice');
    var newProductManufacturer = $('#newProductManufacturer');
    var manufacturerList = $('#manufacturerList');

    let searchTimeout;

    // --- Компания ---
    companyInput.on('input', function () {
        var query = $(this).val().trim();
        if (query.length < 2) {
            companySuggestions.hide();
            return;
        }
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            $.getJSON('/api/OrderAPI/SearchCompanies', { query: query }, function (data) {
                companySuggestions.empty();
                if (data.length === 0) {
                    companySuggestions.append('<li class="list-group-item disabled">Ничего не найдено</li>');
                } else {
                    data.forEach(function (company) {
                        companySuggestions.append(
                            `<li class="list-group-item" data-id="${company.id}" data-name="${company.name}">
                                ${company.name}
                            </li>`
                        );
                    });
                }
                companySuggestions.show();
            });
        }, 300);
    });

    companySuggestions.on('click', 'li:not(.disabled)', function () {
        var id = $(this).data('id');
        var name = $(this).data('name');
        console.log('Выбрана компания из списка:', name);
        companyInput.val(name);
        companyIdField.val(id);
        companySuggestions.hide();
        customerInput.prop('disabled', false);
        addCustomerBtn.prop('disabled', false);
        customerInput.val('');
        customerIdField.val('');
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('#companySuggestions, #companySearchInput').length) {
            companySuggestions.hide();
        }
        if (!$(e.target).closest('#customerSuggestions, #customerSearchInput').length) {
            customerSuggestions.hide();
        }
    });

    addCompanyBtn.click(function () {
        $('#newCompanyName').val(companyInput.val());
        companyModal.show();
    });

    $('#saveCompanyBtn').click(async function () {
        var name = $('#newCompanyName').val().trim();
        if (!name) { alert('Введите название'); return; }
        try {
            var response = await fetch('/api/OrderAPI/CreateCompany', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: name })
            });
            var result = await response.json();
            if (result.success) {
                companyInput.val(result.name);
                companyIdField.val(result.id);
                customerInput.prop('disabled', false);
                addCustomerBtn.prop('disabled', false);
                companyModal.hide();
            } else {
                alert(result.message || 'Ошибка');
            }
        } catch (e) { alert('Ошибка соединения'); }
    });

    // --- Клиент ---
    customerInput.on('input', function () {
        var query = $(this).val().trim();
        var companyId = companyIdField.val();
        if (query.length < 2 || !companyId) {
            customerSuggestions.hide();
            return;
        }
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            $.getJSON('/api/OrderAPI/GetCustomersByCompany', {
                companyId: companyId,
                query: query
            }, function (data) {
                var filtered = data.filter(function (c) {
                    var fullName = (c.lastName + ' ' + c.firstName).toLowerCase();
                    return fullName.includes(query.toLowerCase());
                });
                customerSuggestions.empty();
                if (filtered.length === 0) {
                    customerSuggestions.append('<li class="list-group-item disabled">Ничего не найдено</li>');
                } else {
                    filtered.forEach(function (c) {
                        customerSuggestions.append(
                            `<li class="list-group-item" data-id="${c.id}" data-name="${c.lastName} ${c.firstName}">
                                ${c.lastName} ${c.firstName}
                            </li>`
                        );
                    });
                }
                customerSuggestions.show();
            });
        }, 300);
    });

    customerSuggestions.on('click', 'li:not(.disabled)', function () {
        var id = $(this).data('id');
        var name = $(this).data('name');
        customerInput.val(name);
        customerIdField.val(id);
        customerSuggestions.hide();
    });

    addCustomerBtn.click(function () {
        if (!companyIdField.val()) { alert('Сначала выберите компанию'); return; }
        $('#newCustomerLastName').val(customerInput.val());
        customerModal.show();
    });

    $('#saveCustomerBtn').click(async function () {
        var firstName = $('#newCustomerFirstName').val().trim();
        var lastName = $('#newCustomerLastName').val().trim();
        var companyId = companyIdField.val();
        if (!firstName || !lastName) { alert('Введите имя и фамилию'); return; }
        if (!companyId) { alert('Компания не выбрана'); return; }
        try {
            var response = await fetch('/api/OrderAPI/CreateCustomer', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ companyId: parseInt(companyId), firstName, lastName })
            });
            var result = await response.json();
            if (result.success) {
                customerInput.val(result.fullName);
                customerIdField.val(result.id);
                customerModal.hide();
            } else {
                alert(result.message || 'Ошибка');
            }
        } catch (e) { alert('Ошибка соединения'); }
    });

    // --- Продукт ---
    // При открытии модального окна загружаем список производителей
    addProductBtn.click(function () {
        // Очищаем поля
        newProductName.val('');
        newProductModel.val('');
        newProductPrice.val('');
        newProductManufacturer.val('');
        // Загружаем производителей
        $.getJSON('/api/ProductAPI/GetManufacturers', function (data) {
            manufacturerList.empty();
            data.forEach(function (m) {
                manufacturerList.append('<option value="' + m.name + '" data-id="' + m.id + '">');
            });
        }).fail(function () {
            // Если API недоступен, даём возможность ввести вручную
            console.warn('Не удалось загрузить производителей');
        });
        productModal.show();
    });

    // Обработчик создания продукта
    saveProductBtn.click(async function () {
        var name = newProductName.val().trim();
        var model = newProductModel.val().trim();
        var price = parseFloat(newProductPrice.val()) || 0;
        var manufacturerName = newProductManufacturer.val().trim();

        if (!name) { alert('Введите название товара'); return; }
        if (price <= 0) { alert('Введите корректную цену'); return; }

        try {
            // 1. Создаём производителя, если он не выбран из списка и введено имя
            var manufacturerId = null;
            if (manufacturerName) {
                var manufacturerResponse = await fetch('/api/ProductAPI/CreateManufacturer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name: manufacturerName })
                });
                var manufacturerResult = await manufacturerResponse.json();
                if (manufacturerResult.success) {
                    manufacturerId = manufacturerResult.id;
                } else {
                    alert('Ошибка создания производителя: ' + manufacturerResult.message);
                    return;
                }
            }

            // 2. Создаём продукт
            var response = await fetch('/api/ProductAPI/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    name: name,
                    model: model,
                    manufacturerId: manufacturerId,
                    price: price
                })
            });
            var result = await response.json();
            if (result.success) {
                // Добавляем продукт в таблицу (используем функцию из основного модуля)
                if (typeof window.addProductToTable === 'function') {
                    window.addProductToTable(result);
                } else {
                    console.warn('Функция addProductToTable не найдена, но продукт создан');
                }
                productModal.hide();
                // Очищаем поля
                newProductName.val('');
                newProductModel.val('');
                newProductPrice.val('');
                newProductManufacturer.val('');
            } else {
                alert(result.message || 'Ошибка создания товара');
            }
        } catch (e) {
            console.error(e);
            alert('Ошибка соединения');
        }
    });
});