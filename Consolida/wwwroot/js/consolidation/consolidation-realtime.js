(function () {
    'use strict';

    const root = document.querySelector('.consolidation-container');
    if (!root) return;

    const currentUser = root.dataset.currentUser || '';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/consolidation')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Экспортируем соединение, чтобы его мог использовать consolidation.js
    window.consolidationConnection = connection;

    // ---------- Изменение доски → перезагрузка страницы ----------
    connection.on('ConsolidationBoardChanged', function (info) {
        if (info && info.changedBy === currentUser) return;
        console.log('[SignalR] Board changed:', info);
        window.location.reload();
    });

    // ---------- Remote drag: карта активных drag'ов от других пользователей ----------
    const remoteDragState = new Map();
    const DRAG_TTL_MS = 30000;

    connection.on('OrderDragStarted', function (data) {
        if (!data || typeof data.orderId === 'undefined') return;

        const orderId = parseInt(data.orderId);
        const userId = data.userId || 'Unknown';

        clearRemoteDragState(orderId);
        applyRemoteLockBadge(orderId, userId);

        const timer = setTimeout(function () {
            removeRemoteLockBadge(orderId);
            remoteDragState.delete(orderId);
        }, DRAG_TTL_MS);

        remoteDragState.set(orderId, { timer: timer, userId: userId });
        console.log('[SignalR] Remote drag start:', orderId, 'by', userId);
    });

    connection.on('OrderDragEnded', function (data) {
        if (!data || typeof data.orderId === 'undefined') return;
        const orderId = parseInt(data.orderId);
        clearRemoteDragState(orderId);
        removeRemoteLockBadge(orderId);
        console.log('[SignalR] Remote drag end:', orderId);
    });

    function clearRemoteDragState(orderId) {
        const existing = remoteDragState.get(orderId);
        if (existing && existing.timer) clearTimeout(existing.timer);
        remoteDragState.delete(orderId);
    }

    function applyRemoteLockBadge(orderId, userId) {
        document.querySelectorAll('.draggable-order[data-order-id="' + orderId + '"]').forEach(function (card) {
            card.classList.add('locked-remote');
            card.setAttribute('draggable', 'false');

            if (!card.querySelector('.lock-badge.remote-lock')) {
                const badge = document.createElement('div');
                badge.className = 'lock-badge remote-lock mb-1';
                badge.dataset.userId = userId;
                badge.textContent = '🔒 ' + userId + ' (перетаскивает)';
                card.insertBefore(badge, card.firstChild);
            }
        });
    }

    function removeRemoteLockBadge(orderId) {
        document.querySelectorAll('.draggable-order[data-order-id="' + orderId + '"]').forEach(function (card) {
            card.classList.remove('locked-remote');

            const badge = card.querySelector('.lock-badge.remote-lock');
            if (badge) badge.remove();

            if (!card.classList.contains('locked') && !card.classList.contains('inactive')) {
                card.setAttribute('draggable', 'true');
            }
        });
    }

    // ---------- Presence: список активных пользователей ----------
    connection.on('ViewersChanged', function (viewers) {
        renderViewers(viewers);
    });

    function renderViewers(viewers) {
        const panel = document.getElementById('viewersPanel');
        const list = document.getElementById('viewersList');
        if (!panel || !list) return;

        const others = (viewers || []).filter(v => v.userId !== currentUser);

        if (others.length === 0) {
            panel.style.display = 'none';
            return;
        }

        panel.style.display = 'flex';
        list.innerHTML = others
            .map(v => '<span class="viewer-chip">' + escapeHtml(v.userId) + '</span>')
            .join('');
    }

    function escapeHtml(text) {
        if (text == null) return '';
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // ---------- Подключение ----------
    connection.start()
        .then(function () {
            console.log('[SignalR] Connected to /hubs/consolidation');
            // Сразу запрашиваем список зрителей
            return connection.invoke('RequestViewers');
        })
        .catch(function (err) { console.error('[SignalR] Connection failed:', err); });

    // При переподключении запрашиваем список заново
    connection.onreconnected(function () {
        console.log('[SignalR] Reconnected');
        connection.invoke('RequestViewers').catch(function (err) {
            console.warn('[SignalR] RequestViewers after reconnect failed:', err);
        });
    });
})();