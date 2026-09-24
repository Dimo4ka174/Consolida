export function formatPriceInput(inputElement) {
    const cursorPosition = inputElement.selectionStart;
    const originalLength = inputElement.value.length;
    
    let value = inputElement.value.replace(/[^\d,]/g, '');
    
    const numericValue = parseFloat(value.replace(',', '.')) || 0;
    
    const parts = numericValue.toFixed(2).split('.');
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    
    const formattedValue = parts.join(',');
    
    inputElement.value = formattedValue;
    
    const newLength = formattedValue.length;
    const lengthDiff = newLength - originalLength;
    const newCursorPosition = cursorPosition + lengthDiff;
    
    inputElement.setSelectionRange(newCursorPosition, newCursorPosition);
    
    return numericValue;
}

export function parseFormattedPrice(formattedValue) {
    if (typeof formattedValue === 'number') {
        return formattedValue;
    }

    if (typeof formattedValue === 'string') {
        const numericString = formattedValue.replace(/\s/g, '').replace(',', '.');
        return parseFloat(numericString) || 0;
    }

    return parseFloat(formattedValue) || 0;
}