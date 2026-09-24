(function () {
    'use strict';

    // Обработчик клика по кнопкам комментариев
    document.addEventListener('click', function (e) {
        // Комментарий товара
        const productToggle = e.target.closest('.comment-toggle[data-product-id]');
        if (productToggle) {
            const productId = productToggle.dataset.productId;
            const commentRow = document.querySelector(`.comment-row[data-product-id="${productId}"]`);
            if (commentRow) {
                commentRow.classList.toggle('d-none');
                const icon = productToggle.querySelector('i');
                if (icon) {
                    // Когда строка скрыта, d-none есть → показываем "закрытый" конверт
                    const isHidden = commentRow.classList.contains('d-none');
                    icon.classList.toggle('bi-chat-left-text-fill', isHidden);
                    icon.classList.toggle('bi-chat-left-text', !isHidden);
                }
            }
            return;
        }

        // Комментарий заказа
        const orderToggle = e.target.closest('.comment-toggle:not([data-product-id])');
        if (orderToggle) {
            const icon = orderToggle.querySelector('i');
            if (icon) {
                // Ждём, пока Bootstrap обновит aria-expanded
                setTimeout(() => {
                    const isExpanded = orderToggle.getAttribute('aria-expanded') === 'true';
                    icon.classList.toggle('bi-chat-left-text-fill', isExpanded);
                    icon.classList.toggle('bi-chat-left-text', !isExpanded);
                }, 150);
            }
        }
    });

    // Обновление индикатора заполненности при вводе
    document.addEventListener('input', function (e) {
        const textarea = e.target;
        if (!textarea.classList.contains('comment-input')) return;

        const productId = textarea.dataset.productId;
        const hasComment = textarea.value.trim() !== '';
        let button;

        if (productId) {
            button = document.querySelector(`.comment-toggle[data-product-id="${productId}"]`);
        } else if (textarea.id === 'orderComment') {
            button = document.querySelector('.comment-toggle:not([data-product-id])');
        }
        if (button) {
            button.classList.toggle('has-comment', hasComment);
        }
    });
})();