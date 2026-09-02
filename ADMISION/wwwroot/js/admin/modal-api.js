// API global de modales — extraída de _Modal.cshtml para permitir que la sección
// del modal se escriba inline (con partials Razor) sin perder los handlers.
//
// Expone window.ADM.Modal con open/close/toggle/closeAll. Idempotente.
(function () {
    if (window.ADM && window.ADM.Modal) return;
    window.ADM = window.ADM || {};

    var openStack = [];

    function open(id) {
        var el = document.getElementById(id);
        if (!el) return;
        el.classList.add('is-open');
        el.setAttribute('aria-hidden', 'false');
        if (openStack.indexOf(id) === -1) openStack.push(id);
        document.body.style.overflow = 'hidden';
        // Focus al primer elemento focusable.
        requestAnimationFrame(function () {
            var first = el.querySelector('input, select, textarea, button:not([data-modal-close])');
            if (first) first.focus();
        });
        el.dispatchEvent(new CustomEvent('modal:open', { bubbles: true }));
    }

    function close(id) {
        var el = document.getElementById(id);
        if (!el) return;
        el.classList.remove('is-open');
        el.setAttribute('aria-hidden', 'true');
        openStack = openStack.filter(function (x) { return x !== id; });
        if (openStack.length === 0) document.body.style.overflow = '';
        el.dispatchEvent(new CustomEvent('modal:close', { bubbles: true }));
    }

    function toggle(id) {
        var el = document.getElementById(id);
        if (!el) return;
        el.classList.contains('is-open') ? close(id) : open(id);
    }

    function closeAll() {
        openStack.slice().forEach(close);
    }

    window.ADM.Modal = { open: open, close: close, toggle: toggle, closeAll: closeAll };

    // Click delegado: cualquier [data-modal-open] abre y cualquier [data-modal-close]
    // (dentro del modal) cierra.
    document.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-modal-open]');
        if (opener) {
            e.preventDefault();
            open(opener.getAttribute('data-modal-open'));
            return;
        }
        var closer = e.target.closest('[data-modal-close]');
        if (closer) {
            var modal = closer.closest('.adm-modal');
            if (modal) close(modal.id);
        }
    });

    // ESC cierra el modal más reciente.
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && openStack.length) {
            close(openStack[openStack.length - 1]);
        }
    });
})();
