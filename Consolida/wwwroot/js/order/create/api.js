import { selectedProducts, apiSettings } from './main.js';
import { parseFormattedPrice } from './formatters.js';

export async function saveProductsToSession() {
    const productsData = selectedProducts.map(p => {
        const price = parseFormattedPrice(p.price) || parseFloat(p.price) || 0.01;

        return {
            ProductId: p.productId,
            ProductName: p.productName,
            Model: p.model || '',
            Manufacturer: p.manufacturer ? {
                Id: p.manufacturer.id || null,
                Name: p.manufacturer.name || ''
            } : null,
            DeliveryDate: p.deliveryDate || new Date().toISOString().split('T')[0],
            LeadTime: Math.max(0, parseInt(p.leadTime, 10) || 0),
            Quantity: Math.max(1, parseInt(p.quantity)) || 1,
            Weight: Math.max(0.1, parseFloat(p.weight)) || 0.1,
            Price: price > 0 ? price : 0.01,
            Comment: p.comment || ''
        };
    });

    const response = await fetch(apiSettings.updateSessionUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(productsData)
    });

    if (!response.ok) {
        const errorData = await response.json();
        console.error('Детали ошибки:', errorData);
        throw new Error(errorData.Message || 'Ошибка сервера');
    }
}