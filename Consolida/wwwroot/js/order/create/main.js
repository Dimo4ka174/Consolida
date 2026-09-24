// Импортируем модули
import { initProductSearch, handleProductSearch } from './product-search.js';
import { initProductTable, updateSelectedProductsTable, addProductToTable } from './product-table.js';
import { calculateTotals } from './calculator.js';
import { saveProductsToSession } from './api.js';

// Глобальные переменные (доступны во всех модул¤х)
export const selectedProducts = window.initialProducts || [];
export const apiSettings = window.apiSettings || {};

export const elements = {
    productSearch: $('#productSearch'),
    productSuggestions: $('#productSuggestions'),
    selectedProductsBody: $('#selectedProductsBody'),
    selectedProductsTable: $('#selectedProductsTable'),
    totalWeight: $('#totalWeight'),
    totalCost: $('#totalCost'),
    orderForm: $('form')
};

document.addEventListener('DOMContentLoaded', function () {
    // Инициализация модулей
    initProductSearch();
    initProductTable();

    window.addProductToTable = addProductToTable;

    // Обработчик отправки формы
    elements.orderForm.on('submit', async function (e) {
        e.preventDefault();
        try {
            await saveProductsToSession();
            this.submit();
        } catch (error) {
            console.error('Ошибка при сохранении:', error);
            alert('Ошибка при сохранении данных');
        }
    });
});