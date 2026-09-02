(function () {
  var cfg = window.PersonQueryConfig || {};
  var apiId = cfg.apiId;
  var form = document.getElementById('queryForm');
  var btn = document.getElementById('executeBtn');
  var dniInput = document.getElementById('dni');

  var academicPanel = document.getElementById('academicResult');
  var academicMeta = document.getElementById('academicMeta');
  var academicError = document.getElementById('academicError');
  var academicContent = document.getElementById('academicContent');
  var academicRaw = document.getElementById('academicRaw');
  var saveAcademicBtn = document.getElementById('saveAcademicBtn');

  var paymentPanel = document.getElementById('paymentResult');
  var paymentMeta = document.getElementById('paymentMeta');
  var paymentError = document.getElementById('paymentError');
  var paymentContent = document.getElementById('paymentContent');
  var paymentRaw = document.getElementById('paymentRaw');
  var savePaymentBtn = document.getElementById('savePaymentBtn');

  var errorPanel = document.getElementById('errorResult');
  var errorContent = document.getElementById('errorContent');

  function hideAll() {
    [academicPanel, paymentPanel, errorPanel].forEach(function (p) { p.classList.add('hidden'); });
  }

  function showLoading() {
    btn.disabled = true;
    btn.innerHTML = '<i class="ti ti-loader-2 fa-spin text-xs"></i> Consultando\u2026';
  }

  function hideLoading() {
    btn.disabled = false;
    btn.innerHTML = '<i class="ti ti-player-play text-xs"></i> Consultar';
  }

  function esc(s) {
    return String(s ?? '').replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  function fmtMoney(n) {
    return 'S/ ' + Number(n).toFixed(2);
  }

  function fmtDate(s) {
    if (!s) return '\u2014';
    var d = new Date(s);
    return d.toLocaleDateString('es-PE', { year: 'numeric', month: 'short', day: 'numeric' }) + ' ' +
           d.toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit' });
  }

  function statusBadge(status) {
    var map = { 0: 'b-gray', 1: 'b-amber', 2: 'b-green', 3: 'b-red' };
    var label = { 0: 'Pendiente', 1: 'Pendiente', 2: 'Pagado', 3: 'Anulado' };
    return '<span class="badge ' + (map[status] || 'b-gray') + ' text-[10px]">' + (label[status] || status) + '</span>';
  }

  function renderAcademic(data, logId, durationMs) {
    hideAll();
    academicPanel.classList.remove('hidden');
    academicMeta.textContent = 'log ' + (logId || '').slice(0, 8) + '\u2026 \u00B7 ' + durationMs + ' ms';
    academicError.classList.add('hidden');
    saveAcademicBtn.classList.remove('hidden');
    saveAcademicBtn.dataset.logid = logId;

    if (!data || !data.items || data.items.length === 0) {
      academicContent.innerHTML = '<div class="text-center py-8 text-ink-400 text-sm">No se encontraron datos acad\u00E9micos para este DNI.</div>';
      academicRaw.textContent = '(sin contenido)';
      return;
    }

    var items = data.items;
    var first = items[0];
    var html = '';

    // Person header card
    html += '<div class="bg-primary-50/50 dark:bg-primary-500/5 rounded-lg ring-1 ring-primary-100 dark:ring-primary-500/20 p-4 sm:p-5">' +
      '<div class="flex flex-wrap items-start justify-between gap-3">' +
      '<div>' +
      '<h3 class="text-lg font-bold text-ink-900 dark:text-ink-100">' + esc(first.name) + ' ' + esc(first.paternalSurname) + ' ' + esc(first.maternalSurname) + '</h3>' +
      '<div class="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-sm text-ink-600 dark:text-ink-300">' +
      '<span><strong>DNI:</strong> ' + esc(first.dni) + '</span>' +
      '<span><strong>Usuario:</strong> ' + esc(first.userName) + '</span>';

    if (first.email) html += '<span><strong>Email:</strong> ' + esc(first.email) + '</span>';
    if (first.personalEmail) html += '<span><strong>Email personal:</strong> ' + esc(first.personalEmail) + '</span>';

    html += '</div></div>' +
      '<span class="badge b-green text-[10px]">' + items.length + ' carrera(s)</span>' +
      '</div></div>';

    // Career table
    html += '<div class="overflow-x-auto rounded-lg ring-1 ring-ink-200/60 dark:ring-ink-800/60">' +
      '<table class="atlas w-full"><thead><tr>' +
      '<th>Carrera</th><th>Facultad</th><th class="text-right">Cr\u00E9ditos aprobados</th>' +
      '</tr></thead><tbody>';

    for (var i = 0; i < items.length; i++) {
      var item = items[i];
      html += '<tr>' +
        '<td class="font-semibold text-ink-900 dark:text-ink-100">' + esc(item.careerName) + '</td>' +
        '<td class="text-ink-600 dark:text-ink-300">' + esc(item.facultyName) + '</td>' +
        '<td class="text-right font-mono font-semibold text-ink-800 dark:text-ink-100">' + Number(item.totalCreditsApproved).toFixed(2) + '</td>' +
        '</tr>';
    }

    html += '</tbody></table></div>';
    academicContent.innerHTML = html;
    academicRaw.textContent = JSON.stringify(data, null, 2);
  }

  function renderPayments(data, logId, durationMs) {
    hideAll();
    paymentPanel.classList.remove('hidden');
    paymentMeta.textContent = 'log ' + (logId || '').slice(0, 8) + '\u2026 \u00B7 ' + durationMs + ' ms';
    paymentError.classList.add('hidden');
    savePaymentBtn.classList.remove('hidden');
    savePaymentBtn.dataset.logid = logId;

    if (!data || data.length === 0) {
      paymentContent.innerHTML = '<div class="text-center py-8 text-ink-400 text-sm">No se encontraron pagos para este DNI.</div>';
      paymentRaw.textContent = '(sin contenido)';
      return;
    }

    var totalPayments = 0;
    for (var vi = 0; vi < data.length; vi++) {
      totalPayments += (data[vi].payments ? data[vi].payments.length : 0);
    }

    var html = '<div class="bg-amber-50/50 dark:bg-amber-500/5 rounded-lg ring-1 ring-amber-100 dark:ring-amber-500/20 p-4 sm:p-5 mb-4">' +
      '<div class="flex flex-wrap items-start justify-between gap-3">' +
      '<div>' +
      '<h3 class="text-lg font-bold text-ink-900 dark:text-ink-100">' + esc(data[0].fullName) + '</h3>' +
      '<p class="text-sm text-ink-500 mt-1">' + data.length + ' comprobante(s) \u00B7 ' + totalPayments + ' pago(s)</p>' +
      '</div></div></div>';

    for (var v = 0; v < data.length; v++) {
      var voucher = data[v];
      var open = v === 0 ? 'open' : '';
      html += '<details class="rounded-lg ring-1 ring-ink-200/60 dark:ring-ink-800/60 overflow-hidden' + (v > 0 ? ' mt-3' : '') + '" ' + open + '>' +
        '<summary class="cursor-pointer px-4 py-3 bg-ink-50 dark:bg-ink-800/60 hover:bg-ink-100 dark:hover:bg-ink-800/80 transition-colors flex items-center justify-between gap-3 text-sm font-semibold">' +
        '<span class="flex items-center gap-2"><i class="ti ti-receipt text-ink-400"></i>' +
        '<span class="font-mono">' + esc(voucher.serialVoucher) + '</span></span>' +
        '<span class="text-xs text-ink-400 font-normal">' + (voucher.payments ? voucher.payments.length : 0) + ' pago(s)</span>' +
        '</summary>' +
        '<div class="overflow-x-auto">' +
        '<table class="atlas w-full"><thead><tr>' +
        '<th>Descripci\u00F3n</th><th class="text-right">Subtotal</th><th class="text-right">Dto.</th>' +
        '<th class="text-right">Total</th><th class="text-center">Estado</th><th>Fecha</th><th>Per\u00EDodo</th><th>Cajero</th>' +
        '</tr></thead><tbody>';

      if (voucher.payments && voucher.payments.length > 0) {
        for (var p = 0; p < voucher.payments.length; p++) {
          var pay = voucher.payments[p];
          html += '<tr>' +
            '<td class="max-w-xs text-ink-800 dark:text-ink-100">' +
            '<div class="text-[13px] leading-snug">' + esc(pay.description) + '</div>';

          if (pay.amountInWords) {
            html += '<div class="text-[10px] text-ink-400 mt-0.5 italic">' + esc(pay.amountInWords) + '</div>';
          }

          html += '</td>' +
            '<td class="text-right font-mono text-sm text-ink-700 dark:text-ink-200">' + fmtMoney(pay.subTotal) + '</td>' +
            '<td class="text-right font-mono text-sm text-rose-500">' + fmtMoney(pay.discount) + '</td>' +
            '<td class="text-right font-mono text-sm font-bold text-ink-900 dark:text-ink-100">' + fmtMoney(pay.total) + '</td>' +
            '<td class="text-center">' + statusBadge(pay.status) + '</td>' +
            '<td class="text-xs text-ink-600 dark:text-ink-300 whitespace-nowrap font-mono">' + fmtDate(pay.paymentDate) + '</td>' +
            '<td class="text-xs text-ink-600 dark:text-ink-300 font-mono">' + esc(pay.termName || '\u2014') + '</td>' +
            '<td class="text-xs text-ink-600 dark:text-ink-300 font-mono">' + esc(pay.cashier || '\u2014') + '</td>' +
            '</tr>';
        }
      } else {
        html += '<tr><td colspan="8" class="text-center text-ink-400 text-sm py-4">Sin pagos en este comprobante</td></tr>';
      }

      html += '</tbody></table></div>' +
        '<div class="px-4 py-2 bg-ink-50/50 dark:bg-ink-800/40 text-[10px] text-ink-400 font-mono flex flex-wrap gap-x-4">' +
        '<span>Creado por: ' + esc(voucher.payments && voucher.payments[0] ? voucher.payments[0].createdBy : '\u2014') + '</span>' +
        '<span>Dependencia: ' + esc(voucher.payments && voucher.payments[0] ? voucher.payments[0].name : '\u2014') + '</span>' +
        '</div></details>';
    }

    paymentContent.innerHTML = html;
    paymentRaw.textContent = JSON.stringify(data, null, 2);
  }

  // ───────── Form submit ─────────
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    var dni = dniInput.value.trim();
    if (!dni || dni.length < 8) return;

    showLoading();
    hideAll();

    var formData = new FormData(form);
    try {
      var res = await fetch('/admin/consultas/' + apiId + '/ejecutar', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });

      if (!res.ok) {
        var errData = await res.json().catch(function () { return { message: 'Error de red' }; });
        hideAll();
        errorPanel.classList.remove('hidden');
        errorContent.textContent = errData.message || 'HTTP ' + res.status;
        return;
      }

      var data = await res.json();

      if (!data.success) {
        hideAll();
        errorPanel.classList.remove('hidden');
        errorContent.innerHTML =
          '<div class="flex items-center gap-2 mb-2">' +
          '<span class="badge b-red font-mono">HTTP ' + (data.statusCode || '\u2014') + '</span>' +
          '<span class="text-ink-400 text-xs font-mono">' + (data.durationMs || '\u2014') + ' ms</span>' +
          '</div>' +
          '<p>' + esc(data.error || 'La consulta fall\u00F3.') + '</p>';
        return;
      }

      if (data.category === 'Academic') {
        renderAcademic(data.data, data.logId, data.durationMs);
      } else if (data.category === 'Payment') {
        renderPayments(data.data, data.logId, data.durationMs);
      } else {
        hideAll();
        errorPanel.classList.remove('hidden');
        errorContent.textContent = 'Tipo de API no soportado.';
      }
    } catch (err) {
      hideAll();
      errorPanel.classList.remove('hidden');
      errorContent.textContent = 'Error de conexi\u00F3n: ' + err.message;
    } finally {
      hideLoading();
    }
  });

  // ───────── Save Academic ─────────
  saveAcademicBtn.addEventListener('click', async function () {
    var logId = saveAcademicBtn.dataset.logid;
    var dni = dniInput.value.trim();
    saveAcademicBtn.disabled = true;
    saveAcademicBtn.innerHTML = '<i class="ti ti-loader-2 fa-spin text-xs"></i> Guardando\u2026';
    try {
      var fd = new FormData();
      fd.append('__RequestVerificationToken', (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '');
      fd.append('apiId', apiId);
      fd.append('dni', dni);
      fd.append('logId', logId);

      var res = await fetch('/admin/consultas/salvar-academico', { method: 'POST', body: fd });
      var result = await res.json();
      if (result.success) {
        saveAcademicBtn.innerHTML = '<i class="ti ti-check text-xs"></i> Guardado';
        saveAcademicBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md ring-1 ring-emerald-200 dark:ring-emerald-500/30 text-emerald-600 dark:text-emerald-300 text-sm font-semibold';
      } else {
        saveAcademicBtn.innerHTML = '<i class="ti ti-x text-xs"></i> Error';
        setTimeout(function () {
          saveAcademicBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar en BD';
          saveAcademicBtn.disabled = false;
          saveAcademicBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md bg-white dark:bg-ink-800 ring-soft text-ink-700 dark:text-ink-200 text-sm font-semibold hover:bg-ink-100 dark:hover:bg-ink-700 transition-all';
        }, 3000);
      }
    } catch (e) {
      saveAcademicBtn.innerHTML = '<i class="ti ti-x text-xs"></i> Error';
      setTimeout(function () {
        saveAcademicBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar en BD';
        saveAcademicBtn.disabled = false;
        saveAcademicBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md bg-white dark:bg-ink-800 ring-soft text-ink-700 dark:text-ink-200 text-sm font-semibold hover:bg-ink-100 dark:hover:bg-ink-700 transition-all';
      }, 3000);
    }
  });

  // ───────── Save Payment ─────────
  savePaymentBtn.addEventListener('click', async function () {
    var logId = savePaymentBtn.dataset.logid;
    var dni = dniInput.value.trim();
    savePaymentBtn.disabled = true;
    savePaymentBtn.innerHTML = '<i class="ti ti-loader-2 fa-spin text-xs"></i> Guardando\u2026';
    try {
      var fd = new FormData();
      fd.append('__RequestVerificationToken', (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '');
      fd.append('apiId', apiId);
      fd.append('dni', dni);
      fd.append('logId', logId);

      var res = await fetch('/admin/consultas/salvar-pagos', { method: 'POST', body: fd });
      var result = await res.json();
      if (result.success) {
        savePaymentBtn.innerHTML = '<i class="ti ti-check text-xs"></i> Guardado';
        savePaymentBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md ring-1 ring-emerald-200 dark:ring-emerald-500/30 text-emerald-600 dark:text-emerald-300 text-sm font-semibold';
      } else {
        savePaymentBtn.innerHTML = '<i class="ti ti-x text-xs"></i> Error';
        setTimeout(function () {
          savePaymentBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar en BD';
          savePaymentBtn.disabled = false;
          savePaymentBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md bg-white dark:bg-ink-800 ring-soft text-ink-700 dark:text-ink-200 text-sm font-semibold hover:bg-ink-100 dark:hover:bg-ink-700 transition-all';
        }, 3000);
      }
    } catch (e) {
      savePaymentBtn.innerHTML = '<i class="ti ti-x text-xs"></i> Error';
      setTimeout(function () {
        savePaymentBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar en BD';
        savePaymentBtn.disabled = false;
        savePaymentBtn.className = 'inline-flex items-center gap-2 px-4 py-2 rounded-md bg-white dark:bg-ink-800 ring-soft text-ink-700 dark:text-ink-200 text-sm font-semibold hover:bg-ink-100 dark:hover:bg-ink-700 transition-all';
      }, 3000);
    }
  });

  // ───────── Enter key shortcut ─────────
  dniInput.addEventListener('keydown', function (e) {
    if (e.key === 'Enter') form.dispatchEvent(new Event('submit'));
  });
})();
