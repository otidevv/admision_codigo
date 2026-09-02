(function () {
  var searchInput = document.getElementById('obsSearchInput');
  var tbody = document.getElementById('obsTableBody');
  var newBtn = document.getElementById('obsNewBtn');
  var modal = document.getElementById('obsModal');
  var backdrop = document.getElementById('obsModalBackdrop');
  var closeBtn = document.getElementById('obsModalClose');
  var cancelBtn = document.getElementById('obsModalCancel');
  var form = document.getElementById('obsForm');
  var obsIdInput = document.getElementById('modalObsId');
  var titleEl = document.getElementById('obsModalTitle');
  var subtitleEl = document.getElementById('obsModalSubtitle');
  var submitBtn = document.getElementById('obsSubmitBtn');
  var observationInput = document.getElementById('modalObsObservation');
  var scopeWrap = document.getElementById('modalObsScopeWrap');
  var scope = document.getElementById('modalObsScope');
  var inscriptionWrap = document.getElementById('modalObsInscriptionWrap');
  var tipoWrap = document.getElementById('modalObsTipoWrap');
  var tipoSelect = document.getElementById('modalObsTipo');

  var table = document.getElementById('obsTable');
  var canEdit = !!table && table.getAttribute('data-can-edit') === 'true';

  var pathParts = window.location.pathname.split('/');
  var postulantId = null;
  for (var i = 0; i < pathParts.length; i++) {
    if (pathParts[i] === 'postulant-resum' && i + 1 < pathParts.length) {
      postulantId = pathParts[i + 1];
      break;
    }
  }
  if (!postulantId) return;

  var addUrl = '/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/observations/add';

  var debounceTimer;
  searchInput.addEventListener('input', function () {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(doSearch, 300);
  });

  function doSearch() {
    var term = searchInput.value.trim();
    var url = '/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/observations/search?searchTerm=' + encodeURIComponent(term);
    fetch(url)
      .then(function (r) { return r.json(); })
      .then(function (data) { renderTable(data); })
      .catch(function () {});
  }

  function renderTable(items) {
    var colspan = canEdit ? 7 : 6;
    if (!items || items.length === 0) {
      tbody.innerHTML =
        '<tr id="obsEmptyRow">' +
          '<td colspan="' + colspan + '" class="px-4 py-10 text-center">' +
            '<i class="ti ti-message-off text-3xl text-ink-300 mb-2 block"></i>' +
            '<p class="text-ink-400 text-sm font-medium">Sin observaciones que coincidan con la b\u00FAsqueda.</p>' +
          '</td>' +
        '</tr>';
      return;
    }
    var html = '';
    for (var i = 0; i < items.length; i++) {
      var obs = items[i];
      var isUser = obs.kind === 'user';
      var badgeCls = isUser ? 'badge b-violet' : 'badge b-amber';
      var badgeText = isUser ? 'Usuario' : 'Inscripci\u00F3n';
      var createdAt = obs.createdAt ? formatDate(obs.createdAt) : '\u2014';
      var tipoLabel = tipoLabelOf(obs.tipoObservacion);
      var tipoCls = obs.tipoObservacion ? 'b-red' : 'b-gray';
      var contextHtml = '<span class="text-[11px] font-bold tracking-[0.14em] uppercase text-primary-600">' + escHtml(obs.context || '') + '</span>';
      if (obs.codePostulant) {
        contextHtml += ' <span class="ml-2 text-[10px] font-mono text-ink-400 tabular-nums">' + escHtml(obs.codePostulant) + '</span>';
      }
      var updatedHtml = obs.updatedAt
        ? '<p class="text-[10px] text-ink-400 mt-0.5 tabular-nums">Editada: ' + formatDate(obs.updatedAt) + '</p>'
        : '';
      var actionsHtml = '';
      if (canEdit && !isUser) {
        actionsHtml =
          '<td class="px-4 py-3 text-center">' +
            '<button type="button" class="obs-edit-btn inline-flex items-center justify-center w-7 h-7 rounded-md ring-1 ring-primary-200 dark:ring-primary-500/30 text-primary-600 hover:bg-primary-50 dark:hover:bg-primary-500/10 transition-colors" title="Editar observaci\u00F3n" data-obs-id="' + obs.id + '" data-obs-text="' + escAttr(obs.observation || '') + '" data-obs-tipo="' + escAttr(obs.tipoObservacion || '') + '">' +
              '<i class="ti ti-edit text-[10px]"></i>' +
            '</button>' +
          '</td>';
      }
      html +=
        '<tr class="border-b border-ink-100/60 dark:border-ink-800/60 hover:bg-ink-50/40 dark:hover:bg-ink-800/40 transition-colors">' +
          '<td class="px-4 py-3 text-ink-500 tabular-nums text-xs whitespace-nowrap">' + createdAt + '</td>' +
          '<td class="px-4 py-3"><span class="' + badgeCls + '">' + badgeText + '</span></td>' +
          '<td class="px-4 py-3 text-xs whitespace-nowrap"><span class="badge ' + tipoCls + '">' + escHtml(tipoLabel) + '</span></td>' +
          '<td class="px-4 py-3 text-ink-700 dark:text-ink-200">' + contextHtml + '</td>' +
          '<td class="px-4 py-3 text-ink-700 dark:text-ink-200 max-w-xs">' +
            '<p class="truncate" title="' + escAttr(obs.observation || '') + '">' + escHtml(obs.observation || '') + '</p>' +
            updatedHtml +
          '</td>' +
          '<td class="px-4 py-3 text-ink-500 text-xs whitespace-nowrap">' + escHtml(obs.createdBy || '') + '</td>' +
          actionsHtml +
        '</tr>';
    }
    tbody.innerHTML = html;
  }

  var tipoLabels = {
    '1': 'NO PRESENT\u00D3 REQUISITOS COMPLETOS',
    '2': 'NO PRESENT\u00D3 REQUISITOS',
    '3': 'NINGUNA'
  };

  function tipoLabelOf(val) {
    if (!val) return '\u2014';
    return tipoLabels[val] || val;
  }

  function formatDate(val) {
    if (!val) return '\u2014';
    var d = new Date(val);
    if (isNaN(d.getTime())) return val;
    var pad = function (n) { return n < 10 ? '0' + n : n; };
    return pad(d.getDate()) + '/' + pad(d.getMonth() + 1) + '/' + d.getFullYear() + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
  }

  function escHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
  }

  function escAttr(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  function setSelectValue(select, value) {
    var found = false;
    for (var i = 0; i < select.options.length; i++) {
      if (select.options[i].value === value) {
        found = true;
        break;
      }
    }
    if (!found && value) {
      var opt = document.createElement('option');
      opt.value = value;
      opt.textContent = value;
      select.appendChild(opt);
    }
    select.value = value || '';
  }

  function openModal() {
    form.reset();
    form.action = addUrl;
    obsIdInput.value = '';
    titleEl.textContent = 'Nueva observaci\u00F3n';
    subtitleEl.textContent = 'A nivel de usuario o asociada a una inscripci\u00F3n';
    submitBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Registrar';
    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
    syncScope();
  }

  function openEditModal(id, text, tipo) {
    form.reset();
    form.action = '/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/observations/' + id + '/edit';
    obsIdInput.value = id;
    observationInput.value = text;
    setSelectValue(tipoSelect, tipo);
    scopeWrap.style.display = 'none';
    inscriptionWrap.style.display = 'none';
    tipoWrap.style.display = 'block';
    titleEl.textContent = 'Editar observaci\u00F3n';
    subtitleEl.textContent = 'Solo observaciones de inscripci\u00F3n';
    submitBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar cambios';
    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
  }

  function closeModal() {
    modal.classList.add('hidden');
    document.body.style.overflow = '';
  }

  function syncScope() {
    var isInscription = scope.value === 'inscription';
    inscriptionWrap.style.display = isInscription ? 'block' : 'none';
    if (tipoWrap) tipoWrap.style.display = isInscription ? 'block' : 'none';
  }

  tbody.addEventListener('click', function (e) {
    var btn = e.target.closest ? e.target.closest('.obs-edit-btn') : null;
    if (!btn) return;
    openEditModal(btn.getAttribute('data-obs-id'), btn.getAttribute('data-obs-text'), btn.getAttribute('data-obs-tipo'));
  });

  newBtn.addEventListener('click', openModal);
  closeBtn.addEventListener('click', closeModal);
  cancelBtn.addEventListener('click', closeModal);
  backdrop.addEventListener('click', closeModal);
  scope.addEventListener('change', syncScope);

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && !modal.classList.contains('hidden')) {
      closeModal();
    }
  });
})();
