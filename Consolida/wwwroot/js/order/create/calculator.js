import { elements, selectedProducts } from './main.js';
import { parseFormattedPrice } from './formatters.js';

export function calculateTotals() {
    const totals = selectedProducts.reduce((acc, product) => {
        const qty = parseFloat(product.quantity) || 0;
        const price = parseFormattedPrice(product.price) || 0;
        return {
            weight: acc.weight + (qty * (parseFloat(product.weight) || 0)),
            cost: acc.cost + (qty * price)
        };
    }, { weight: 0, cost: 0 });

    const formattedWeight = totals.weight.toFixed(2).replace('.', ',');
    const formattedCost = totals.cost.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ' ').replace('.', ',');

    elements.totalWeight.text(formattedWeight);
    elements.totalCost.text(formattedCost);
}