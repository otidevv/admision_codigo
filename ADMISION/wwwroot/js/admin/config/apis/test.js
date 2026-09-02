(function () {
    var cfg = window.ApiTestConfig || {};
    var paramFields = document.getElementById('paramFields');

    var params = Array.isArray(cfg.params) ? cfg.params : [];
    var urlParams = Array.isArray(cfg.urlParams) ? cfg.urlParams : [];
    var allParams = params.concat(urlParams);

    // Evitar duplicados por key
    var seen = {};
    allParams = allParams.filter(function (p) {
        var key = p.key;
        if (seen[key]) return false;
        seen[key] = true;
        return true;
    });

    if (allParams.length === 0) {
        paramFields.innerHTML = '<p class="text-sm text-ink-400 col-span-2">Esta API no requiere par\u00E1metros adicionales.</p>';
    } else {
        allParams.forEach(function (p) {
            var wrap = document.createElement('div');
            wrap.className = 'form-field';
            var label = p.label || p.key;
            var required = p.required ? '<span class="required">*</span>' : '';
            wrap.innerHTML =
                '<label class="form-label">' + label + ' ' + required + '</label>' +
                '<div class="form-input-wrapper">' +
                '<i class="ti ti-circle-dot form-input-icon"></i>' +
                '<input type="text" name="parameters[' + p.key + ']" ' + (p.required ? 'required' : '') +
                ' class="form-input" placeholder="' + (p.placeholder || '') + '" />' +
                '</div>';
            paramFields.appendChild(wrap);
        });
    }

    var form = document.getElementById('apiTestForm');
    var btn = document.getElementById('executeBtn');
    var panel = document.getElementById('resultPanel');
    var meta = document.getElementById('resultMeta');
    var errorBox = document.getElementById('resultError');
    var tbody = document.getElementById('resultRows');
    var raw = document.getElementById('rawResponse');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader-2 fa-spin text-xs"></i> Consultando\u2026';

        var formData = new FormData(form);
        try {
            var response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            var data = await response.json();

            panel.classList.remove('hidden');
            tbody.innerHTML = '';
            errorBox.classList.add('hidden');

            var statusBadge = data.success
                ? '<span class="text-emerald-600 font-bold">HTTP ' + data.statusCode + '</span>'
                : '<span class="text-rose-600 font-bold">HTTP ' + (data.statusCode || '\u2014') + '</span>';
            meta.innerHTML = statusBadge + ' \u00B7 ' + (data.durationMs ?? '\u2014') + ' ms \u00B7 log <code>' + (data.logId || '').slice(0, 8) + '\u2026</code>';

            if (!data.success) {
                errorBox.classList.remove('hidden');
                errorBox.textContent = data.error || data.message || 'La consulta fall\u00F3.';
            }

            (data.rows || []).forEach(function (r) {
                var tr = document.createElement('tr');
                tr.innerHTML =
                    '<td class="font-semibold text-ink-700 dark:text-ink-200">' + esc(r.label) + '</td>' +
                    '<td class="text-ink-800 dark:text-ink-100 break-words">' + esc(r.value) + '</td>';
                tbody.appendChild(tr);
            });

            if (!tbody.children.length) {
                tbody.innerHTML = '<tr><td colspan="2" class="px-5 py-6 text-center text-ink-400 text-sm">Sin datos en la respuesta.</td></tr>';
            }

            raw.textContent = data.raw ? pretty(data.raw) : '(sin contenido)';
        } catch (err) {
            panel.classList.remove('hidden');
            errorBox.classList.remove('hidden');
            errorBox.textContent = 'Error de red: ' + err.message;
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-player-play text-xs"></i> Ejecutar consulta';
        }
    });

    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }
    function pretty(s) {
        try { return JSON.stringify(JSON.parse(s), null, 2); }
        catch (e) { return s; }
    }
})();
