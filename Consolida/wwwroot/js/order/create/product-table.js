import { formatPriceInput, parseFormattedPrice } from './formatters.js';
import { elements, selectedProducts } from './main.js';
import { calculateTotals } from './calculator.js';

export function initProductTable() {
    if (selectedProducts.length > 0) {
        updateSelectedProductsTable();
    }

    $(document)
        .on('click', '.remove-product', handleRemoveProduct)
        .on('change', '.quantity-input, .price-input, .weight-input, .model-input, .comment-input, .lead-time-input', handleProductFieldChange)
        .on('input', '.price-input', handlePriceInput)
        .on('blur', '.price-input', handlePriceBlur);
}

function handlePriceInput() {
    const input = this;
    const cursorPos = input.selectionStart;
    const value = input.value;

    const newValue = value.replace(/[^\d,]/g, '');

    if (newValue !== value) {
        input.value = newValue;
        input.setSelectionRange(cursorPos - 1, cursorPos - 1);
    }
}

function handlePriceBlur() {
    const index = $(this).data('index');
    const numericValue = formatPriceInput(this);
    selectedProducts[index].price = numericValue;
    calculateTotals();
}

export function updateSelectedProductsTable() {
    elements.selectedProductsBody.empty();

    if (selectedProducts.length === 0) {
        elements.selectedProductsTable.hide();
        return;
    }

    selectedProducts.forEach((product, index) => {
        elements.selectedProductsBody.append(createProductRow(product, index));
    });

    elements.selectedProductsTable.show();
    calculateTotals();
}

function handleRemoveProduct() {
    const index = $(this).data('index');
    selectedProducts.splice(index, 1);
    updateSelectedProductsTable();
}

function handleProductFieldChange() {
    const index = $(this).data('index');
    const field = $(this).attr('class').split(' ').find(c => c.endsWith('-input')).replace('-input', '');
    let value = $(this).val();

    if (field === 'price') {
        value = parseFormattedPrice(value);
    } else if (field === 'lead-time') {
        value = Math.max(0, parseInt(value, 10) || 0);
        $(this).val(value);
        selectedProducts[index].leadTime = value;
        calculateTotals();
        return;
    } else if (['quantity', 'weight'].includes(field)) {
        value = Math.max(0, parseFloat(value) || 0);
        $(this).val(field === 'quantity' ? Math.max(1, value) : value.toFixed(2));
    }

    selectedProducts[index][field] = value;
    calculateTotals();
}

function createProductRow(product, index) {
    const deliveryDate = product.deliveryDate
        ? new Date(product.deliveryDate).toISOString().split('T')[0]
        : new Date().toISOString().split('T')[0];

    const initialPrice = product.price || 0;
    const formattedPrice = initialPrice.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ' ').replace('.', ',');

    return `
    <tr data-index="${index}">
      <td>${product.productName}</td>
      <td><textarea class="form-control form-control-small model-input" rows="3" data-index="${index}">${product.model || ''}</textarea></td>
      <td>${product.manufacturer?.name || ''}</td>
      <td><textarea class="form-control form-control-small comment-input" data-index="${index}">${product.comment || ''}</textarea></td>
      <td><input type="date" class="form-control form-control-small delivery-date-input" value="${deliveryDate}" data-index="${index}" /></td>
      <td><input type="number" class="form-control form-control-small lead-time-input" value="${product.leadTime ?? 0}" min="0" step="1" data-index="${index}" /></td>
      <td><input type="number" class="form-control form-control-small quantity-input" value="${product.quantity}" min="1" data-index="${index}" /></td>
      <td><input type="number" class="form-control form-control-small weight-input" value="${product.weight.toFixed(2)}" min="0" step="0.01" data-index="${index}" /></td>
      <!-- Изменяем type на "text" для цены -->
      <td><input type="text" class="form-control form-control-small price-input" value="${formattedPrice}" data-index="${index}" inputmode="decimal" /></td>
      <td><button type="button" class="btn btn-sm btn-delete btn-icon-only remove-product" data-index="${index}" title="Удалить товар" aria-label="Удалить товар"></button></td>
    </tr>`;
}

export function addProductToTable(product) {
    // Проверяем, не добавлен ли уже этот продукт (по id)
    const exists = selectedProducts.some(p => p.productId === product.id);
    if (exists) {
        alert('Этот товар уже добавлен');
        return;
    }

    // Создаём объект продукта для таблицы (сопоставляем поля)
    const newProduct = {
        productId: product.id,
        productName: product.name,
        model: product.model || '',
        manufacturer: product.manufacturer ? { id: product.manufacturer.id, name: product.manufacturer.name } : null,
        price: product.price || 0,
        quantity: 1,
        weight: 0.1,
        deliveryDate: new Date().toISOString().split('T')[0],
        leadTime: 0,
        comment: ''
    };

    selectedProducts.push(newProduct);
    updateSelectedProductsTable();
}