/*Поиск товаров и добавление их в таблицу*/
import { elements, selectedProducts } from './main.js';
import { updateSelectedProductsTable } from './product-table.js';

export function initProductSearch() {
    elements.productSearch.on('input', debounce(handleProductSearch, 300));
    elements.productSuggestions.on('click', 'li', handleProductSelection);
}

export async function handleProductSearch() {
    const query = elements.productSearch.val().trim();
    if (query.length < 2) {
        elements.productSuggestions.hide().empty();
        return;
    }

    try {
        const response = await fetch(`${window.location.origin}/api/ProductAPI/search?query=${encodeURIComponent(query)}`);
        const data = await response.json();

        elements.productSuggestions.empty();
        if (data.length > 0) {
            data.forEach(product => {
                elements.productSuggestions.append(createProductSuggestionItem(product));
            });
            elements.productSuggestions.show();
        } else {
            elements.productSuggestions.hide();
        }
    } catch (error) {
        console.error('Ошибка при поиске товаров:', error);
        elements.productSuggestions.hide();
    }
}

function handleProductSelection(e) {
    const $li = $(e.currentTarget);
    const manufacturerData = $li.data('manufacturer');

    const product = {
        productId: $li.data('product-id'),
        productName: $li.data('product-name'),
        model: $li.data('model'),
        manufacturer: {
            name: manufacturerData || '',
            id: manufacturerData?.id || null
        },
        quantity: 1,
        weight: parseFloat($li.data('weight')) || 0.1,
        price: parseFloat($li.data('price')) || 0.01,
        deliveryDate: new Date().toISOString().split('T')[0]
    };

    if (!selectedProducts.some(p => p.productId === product.productId)) {
        selectedProducts.push(product);
        updateSelectedProductsTable();
    }

    elements.productSearch.val('').focus();
    elements.productSuggestions.hide();
}

function createProductSuggestionItem(product) {
    return `
    <li class="list-group-item" 
        data-product-id="${product.id}" 
        data-product-name="${product.name}" 
        data-model="${product.model}" 
        data-manufacturer="${product.manufacturer?.name || ''}" 
        data-weight="${product.weight || 0.1}"
        data-price="${product.price || 0}">
        ${product.name} (${product.manufacturer?.name || ''}, ${product.model})
    </li>`;
}

function debounce(func, wait) {
    let timeout;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}