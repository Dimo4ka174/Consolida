function formatNumberWithSpaces(number) {
    if (number === null || number === undefined || isNaN(number)) {
        return '—';
    }

    // Округляем до 2 знаков после запятой
    const rounded = Math.round(number * 100) / 100;
    const parts = rounded.toFixed(2).split('.');
    const integerPart = parts[0];
    const decimalPart = parts[1] || '00';

    // Добавляем пробелы тысяч
    const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');

    return `${formattedInteger}.${decimalPart}`;
}

// Парсинг форматированного числа обратно в число
function parseFormattedNumber(formattedString) {
    if (!formattedString || formattedString === '—') return 0;

    // Убираем пробелы и заменяем запятую на точку
    const cleaned = formattedString
        .toString()
        .replace(/\s/g, '')
        .replace(/,/g, '.');

    const result = parseFloat(cleaned);
    return isNaN(result) ? 0 : result;
}

// Экспорт функций
window.formatter = {
    formatNumberWithSpaces,
    parseFormattedNumber
};