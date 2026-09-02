// ── CAPTCHA helpers (Cloudflare Turnstile invisible) ────────────────────
let __consultaCaptchaToken = null;
let __consultaCaptchaWaiters = [];
window.onConsultaCaptchaSolved = function (token) {
    __consultaCaptchaToken = token;
    while (__consultaCaptchaWaiters.length) __consultaCaptchaWaiters.shift()(token);
};
window.onConsultaCaptchaError = function () {
    while (__consultaCaptchaWaiters.length) __consultaCaptchaWaiters.shift()(null);
};

async function getConsultaCaptchaToken() {
    if (!window.captchaConfig || !window.captchaConfig.enabled) return null;
    if (__consultaCaptchaToken) return __consultaCaptchaToken;
    return new Promise(resolve => {
        __consultaCaptchaWaiters.push(resolve);
        setTimeout(() => {
            const idx = __consultaCaptchaWaiters.indexOf(resolve);
            if (idx >= 0) {
                __consultaCaptchaWaiters.splice(idx, 1);
                resolve(null);
            }
        }, 20000);
    });
}

function consumeConsultaCaptchaToken() {
    __consultaCaptchaToken = null;
    if (window.turnstile) {
        const el = document.getElementById('captcha-consulta');
        if (el) {
            try { window.turnstile.reset(el); } catch (_) { }
        }
    }
}

// ── Document input rules by type ────────────────────────────────────────
const DOC_RULES = {
    DNI:       { maxLength: 8,  inputMode: 'numeric',  pattern: /\D/g, placeholder: 'Ingrese su DNI (8 dígitos)',      label: 'DNI' },
    CE:        { maxLength: 15, inputMode: 'text',      pattern: null,  placeholder: 'Ingrese su C.E. (máx. 15 caracteres)', label: 'C.E.' },
    PASAPORTE: { maxLength: 20, inputMode: 'text',      pattern: null,  placeholder: 'Ingrese su Pasaporte (máx. 20 caracteres)', label: 'Pasaporte' }
};

function applyConsultaDocRules() {
    const docType = document.getElementById('docType')?.value || 'DNI';
    const input = document.getElementById('docNumber');
    if (!input) return;
    const rules = DOC_RULES[docType] || DOC_RULES.DNI;
    input.maxLength = rules.maxLength;
    input.inputMode = rules.inputMode;
    input.placeholder = rules.placeholder;
    if (rules.pattern) {
        input.value = input.value.replace(rules.pattern, '').slice(0, rules.maxLength);
    } else {
        input.value = input.value.slice(0, rules.maxLength);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const docTypeSelect = document.getElementById('docType');
    const dniInput = document.getElementById('docNumber');

    if (docTypeSelect) {
        docTypeSelect.addEventListener('change', applyConsultaDocRules);
    }

    if (dniInput) {
        dniInput.addEventListener('input', function () {
            const docType = docTypeSelect?.value || 'DNI';
            const rules = DOC_RULES[docType] || DOC_RULES.DNI;
            if (rules.pattern) {
                this.value = this.value.replace(rules.pattern, '').slice(0, rules.maxLength);
            } else {
                this.value = this.value.slice(0, rules.maxLength);
            }
        });
    }

    const form = document.getElementById('consultaForm');
    const loadingEl = document.getElementById('loadingIndicator');
    const resultContainer = document.getElementById('resultContainer');
    const btnConsultar = document.getElementById('btnConsultar');

    resultContainer.addEventListener('click', async function (e) {
        const btn = e.target.closest('.js-download-constancia');
        if (!btn) return;
        e.preventDefault();

        const inscriptionId = btn.dataset.inscriptionId;
        if (!inscriptionId) return;

        const originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader-2 fa-spin text-sm"></i> Abriendo...';

        try {
            const response = await fetch(`/consulta-inscripcion/${inscriptionId}/descargar`);
            if (!response.ok) {
                Swal.fire({ title: 'Error', text: 'No se pudo generar la constancia.', icon: 'error', confirmButtonColor: '#10b981' });
                return;
            }
            const blob = await response.blob();
            const disposition = response.headers.get('Content-Disposition') || '';
            const match = disposition.match(/filename="?([^";\n]+)"?/);
            const fileName = match ? match[1] : 'constancia.pdf';

            // Try creating a blob URL + programmatic click first (works in most browsers).
            // Fall back to direct navigation if in-app browser blocks it (Facebook, WhatsApp, etc.).
            var downloaded = false;
            try {
                var url = URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                a.style.display = 'none';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                setTimeout(function () { URL.revokeObjectURL(url); }, 5000);
                downloaded = true;
            } catch (_) { /* ignore — will try fallback */ }

            if (!downloaded) {
                // Fallback: navigate directly to the blob URL
                var fallbackUrl = URL.createObjectURL(blob);
                window.location.href = fallbackUrl;
            }
        } catch (e) {
            Swal.fire({ title: 'Error', text: 'No se pudo contactar con el servidor.', icon: 'error', confirmButtonColor: '#10b981' });
            Swal.fire({ title: 'Error', text: 'No se pudo contactar con el servidor.', icon: 'error', confirmButtonColor: '#10b981' });
        } finally {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
        }
    });

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const docType = docTypeSelect?.value || 'DNI';
        const docNumber = dniInput.value.trim();
        const rules = DOC_RULES[docType] || DOC_RULES.DNI;

        if (docNumber.length < 8 || docNumber.length > rules.maxLength) {
            Swal.fire({
                title: 'Documento inválido',
                text: `El ${rules.label} debe tener entre 8 y ${rules.maxLength} caracteres.`,
                icon: 'warning',
                confirmButtonColor: '#10b981'
            });
            return;
        }

        btnConsultar.disabled = true;
        btnConsultar.innerHTML = '<i class="ti ti-loader-2 fa-spin text-xs"></i> Consultando...';
        resultContainer.classList.add('hidden');
        resultContainer.innerHTML = '';
        loadingEl.classList.remove('hidden');

        try {
            const headers = {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            };
            const captchaToken = await getConsultaCaptchaToken();
            if (window.captchaConfig && window.captchaConfig.enabled && !captchaToken) {
                loadingEl.classList.add('hidden');
                btnConsultar.disabled = false;
                btnConsultar.innerHTML = '<i class="ti ti-search text-xs"></i> Consultar inscripción';
                Swal.fire({
                    title: 'Verificación requerida',
                    text: 'Complete la verificación del captcha antes de continuar.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                return;
            }
            if (captchaToken) headers['X-Captcha-Token'] = captchaToken;

            const response = await fetch('/consulta-inscripcion/buscar', {
                method: 'POST',
                headers: {
                    ...headers,
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: new URLSearchParams({ docType, docNumber })
            });

            consumeConsultaCaptchaToken();
            loadingEl.classList.add('hidden');

            if (!response.ok) {
                resultContainer.innerHTML = errorCard('Error de conexión', 'No se pudo completar la consulta. Intente nuevamente.');
                resultContainer.classList.remove('hidden');
                return;
            }

            const data = await response.json();

            if (data.captchaRequired) {
                resultContainer.innerHTML = errorCard('Verificación requerida', data.message || 'Recargue la página e intente nuevamente.', 'shield-lock');
                resultContainer.classList.remove('hidden');
            } else if (data.found) {
                resultContainer.innerHTML = buildResultCards(data.inscriptions, data.whatsappPhone);
                resultContainer.classList.remove('hidden');
            } else {
                resultContainer.innerHTML = notFoundCard(data.message);
                resultContainer.classList.remove('hidden');
            }
        } catch (error) {
            loadingEl.classList.add('hidden');
            resultContainer.innerHTML = errorCard('Error de conexión', 'No se pudo contactar con el servidor. Verifique su conexión e intente nuevamente.', 'wifi-off');
            resultContainer.classList.remove('hidden');
        } finally {
            btnConsultar.disabled = false;
            btnConsultar.innerHTML = '<i class="ti ti-search text-xs"></i> Consultar inscripción';
        }
    });
});


// ── Card builders ──────────────────────────────────────────────────────

function buildResultCards(inscriptions, whatsappPhone) {
    const postulant = inscriptions[0];
    const cardsHtml = inscriptions.map(ins => buildInscriptionCard(ins, whatsappPhone)).join('');

    return `
        <div class="bg-white ring-soft rounded-md p-6 sm:p-7">
            <div class="flex items-start gap-4 pb-5 mb-5 border-b border-ink-100">
                <span class="w-12 h-12 rounded-md bg-emerald-50 text-emerald-600 inline-flex items-center justify-center text-xl shrink-0">
                    <i class="ti ti-circle-check"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <div class="eyebrow text-emerald-700">Inscripciones encontradas</div>
                    <h2 class="text-[18px] font-semibold tracking-tight text-ink-900 mt-0.5">${escapeHtml(postulant.fullName)}</h2>
                    <p class="text-[12.5px] text-ink-500 mt-0.5">${escapeHtml(postulant.documentType || 'DNI')} ${postulant.documentNumber} · ${inscriptions.length} inscripción(es) en el periodo activo</p>
                </div>
            </div>
            ${cardsHtml}
        </div>`;
}

function buildInscriptionCard(data, whatsappPhone) {
    const active = data.isModalityActive;
    const isSimulacroDownloadable = !active && data.isMockExam && data.canDownload;
    const cardOpacity = (active || isSimulacroDownloadable) ? '' : 'opacity-60';
    const headerBg = (active || isSimulacroDownloadable) ? 'bg-primary-600' : 'bg-slate-400';
    const headerText = active ? 'text-white' : 'text-white';
    const subText = active ? 'text-white/80' : 'text-white/70';

    const stateBadge = (active || isSimulacroDownloadable) ? getStateBadge(data.state) : `<span class="badge b-gray text-[10px]">Proceso culminado</span>`;

    const filesHtml = buildFilesTable(data.files);
    const observationsHtml = buildObservationsSection(data.observations);

    const hasFileObservations = data.files && data.files.some(f => f.observation);
    const hasInscriptionObservations = data.observations && data.observations.length > 0;
    const hasObservations = hasFileObservations || hasInscriptionObservations;

    // Simulacro constancia is always downloadable, even if modality is inactive
    const canDownloadSimulacro = !hasObservations && data.canDownload && data.isMockExam;
    const canDownloadNormal = active && data.canDownload && !hasObservations && !data.isMockExam;
    const canDownload = canDownloadSimulacro || canDownloadNormal;

    let actionsHtml = '';
    if (!active && !canDownloadSimulacro) {
        actionsHtml = `<span class="text-[12px] text-ink-400 italic">Proceso de admisión culminado</span>`;
    } else if (hasObservations && whatsappPhone) {
        actionsHtml = `<a href="https://wa.me/${escapeHtml(whatsappPhone)}" target="_blank" rel="noopener noreferrer"
                         class="btn-outline-primary text-sm font-bold px-5 py-2.5 rounded-md inline-flex items-center justify-center gap-2 bg-emerald-40 text-emerald-600 border-emerald-200 hover:bg-emerald-50 hover:text-emerald-300 transition-all">
                            <i class="ti ti-brand-whatsapp text-base"></i>
                            Contactar por WhatsApp
                        </a>`;
    } else if (canDownloadSimulacro) {
        actionsHtml = `<button type="button"
                         class="btn-grad text-sm font-bold px-6 py-2.5 rounded-md inline-flex items-center justify-center gap-2 js-download-constancia"
                         data-inscription-id="${data.inscriptionId}">
                            <i class="ti ti-file-download text-sm"></i>
                            Descargar Constancia
                        </button>`;
    } else {
        const reason = getNoDownloadReason(data.state);
        actionsHtml = `<span class="text-[12px] text-ink-400 italic">${reason}</span>`;
    }

    return `
        <div class="rounded-md ring-soft border border-ink-200/60 overflow-hidden mb-4 ${cardOpacity}">
            <div class="px-4 sm:px-5 py-3.5 ${headerBg} flex items-center justify-between gap-3">
                <div class="min-w-0">
                    <h3 class="text-[15px] font-bold ${headerText} truncate">${escapeHtml(data.modalityName)}</h3>
                    ${data.typeModalityName
                        ? `<p class="text-[12px] ${subText} truncate">${escapeHtml(data.typeModalityName)}</p>`
                        : ''}
                </div>
                ${stateBadge}
            </div>

            <hr class="border-ink-200/60 dark:border-ink-700">

            <div class="px-4 sm:px-5 py-3.5">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1.5 text-[12.5px]">
                    <div><span class="text-ink-400">Carrera:</span> <span class="font-semibold text-ink-800 dark:text-ink-100">${escapeHtml(data.careerName)}</span></div>
                    <div><span class="text-ink-400">Código:</span> <span class="font-mono font-semibold text-ink-800 dark:text-ink-100">${escapeHtml(data.codePostulant)}</span></div>
                    <div><span class="text-ink-400">Periodo:</span> <span class="font-semibold text-ink-800 dark:text-ink-100">${escapeHtml(data.termName)}</span></div>
                    <div><span class="text-ink-400">Registro:</span> <span class="font-semibold text-ink-800 dark:text-ink-100">${data.inscriptionDate}</span></div>
                </div>
            </div>

            ${filesHtml}
            ${observationsHtml}

            <div class="px-4 sm:px-5 py-3.5 border-t border-ink-200/60 dark:border-ink-700 flex justify-center">
                ${actionsHtml}
            </div>
        </div>`;
}

function buildObservationsSection(observations) {
    if (!observations || observations.length === 0) return '';

    const items = observations.map(o => `
        <div class="flex items-start gap-2.5 py-1.5 border-b border-ink-100/60 last:border-b-0">
            <i class="ti ti-message text-amber-500 text-[12px] mt-0.5 shrink-0"></i>
            <div class="min-w-0 flex-1">
                <p class="text-[11.5px] text-ink-700">${escapeHtml(o.observation)}</p>
                <p class="text-[9.5px] text-ink-400 mt-0.5">
                    <span class="font-semibold">${escapeHtml(o.createdBy)}</span>
                    <span class="mx-1">·</span>
                    ${o.createdAt}
                </p>
            </div>
        </div>
    `).join('');

    return `
        <div class="px-4 sm:px-5 py-2.5 bg-amber-50/30 dark:bg-amber-500/5 border-t border-ink-200/60 dark:border-ink-700">
            <h4 class="text-[10.5px] font-bold text-ink-500 uppercase tracking-[0.08em] mb-1.5">
                <i class="ti ti-message text-[9px] mr-1"></i> Observaciones del expediente
            </h4>
            <div class="space-y-0 divide-y divide-ink-100/40">${items}</div>
        </div>`;
}

function buildFilesTable(files) {
    if (!files || files.length === 0) return '';

    const rows = files.map(f => {
        let statusIcon, statusColor;
        if (f.observation) {
            statusIcon = 'ti-alert-circle';
            statusColor = 'text-amber-600';
        } else if (f.isValidated) {
            statusIcon = 'ti-circle-check';
            statusColor = 'text-emerald-600';
        } else {
            statusIcon = 'ti-hourglass';
            statusColor = 'text-ink-400';
        }

        const obsHtml = f.observation
            ? `<span class="text-amber-700 text-[10.5px]">${escapeHtml(f.observation)}</span>`
            : `<span class="text-ink-400 italic text-[10.5px]">—</span>`;

        const kindBadge = f.kind === 'payment'
            ? `<span class="badge b-amber text-[9px] mr-1"><i class="ti ti-credit-card text-[8px]"></i> Pago</span>`
            : `<span class="badge b-primary text-[9px] mr-1"><i class="ti ti-paperclip text-[8px]"></i> Requisito</span>`;

        return `<tr>
                    <td class="py-1 pr-2">
                        <span class="text-[11.5px] text-ink-700 dark:text-ink-200 flex items-center gap-1 flex-wrap">
                            ${kindBadge}${escapeHtml(f.name)}
                        </span>
                    </td>
                    <td class="py-1 px-2 text-center">
                        <i class="ti ${statusIcon} ${statusColor} text-[13px]" title="${f.isValidated ? 'Aprobado' : f.observation ? 'Observado' : 'Pendiente'}"></i>
                    </td>
                    <td class="py-1 pl-2">${obsHtml}</td>
                </tr>`;
    }).join('');

    return `
        <div class="px-4 sm:px-5 py-2.5 bg-ink-50/40 dark:bg-ink-800/20 border-t border-ink-200/60 dark:border-ink-700">
            <h4 class="text-[10.5px] font-bold text-ink-500 uppercase tracking-[0.08em] mb-1.5">
                <i class="ti ti-files text-[9px] mr-1"></i> Documentos adjuntos
            </h4>
            <table class="w-full text-[11px]">
                <thead>
                    <tr class="text-ink-400 border-b border-ink-200/40 dark:border-ink-700/40">
                        <th class="text-left py-1 pr-2 font-semibold">Documento</th>
                        <th class="text-center py-1 px-2 font-semibold w-[28px]">Val.</th>
                        <th class="text-left py-1 pl-2 font-semibold">Observación</th>
                    </tr>
                </thead>
                <tbody>${rows}</tbody>
            </table>
        </div>`;
}

// ── Helpers ────────────────────────────────────────────────────────────

function getStateBadge(state) {
    const map = {
        'Pendiente': ['bg-amber-500', 'Pendiente'],
        'Aprobado': ['bg-green-500', 'Aprobado'],
        'Observado': ['bg-amber-500', 'Observado'],
        'Rechazado': ['bg-red-500', 'Rechazado'],
        'Retirado': ['bg-gray-500', 'Retirado']
    };
    const [cls, label] = map[state] || ['bg-gray-500', state || '—'];
    return `<span class="badge ${cls} text-white text-[10px]">${label}</span>`;
}

function getNoDownloadReason(state) {
    if (state === 'Pendiente') return 'Pendiente de verificación de documentos';
    if (state === 'Observado') return 'Documentos con observaciones pendientes';
    if (state === 'Rechazado' || state === 'Retirado') return 'Inscripción no válida';
    return 'No disponible para descarga';
}


function escapeHtml(str) {
    if (!str) return '—';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// ── Generic card templates ────────────────────────────────────────────

function errorCard(title, message, icon) {
    icon = icon || 'circle-x';
    return `
        <div class="bg-white ring-soft rounded-md p-6 sm:p-7 text-center">
            <span class="inline-flex w-14 h-14 rounded-md bg-rose-50 text-rose-600 items-center justify-center text-2xl mb-3">
                <i class="ti ti-${icon}"></i>
            </span>
            <h2 class="text-[18px] font-semibold text-ink-900">${title}</h2>
            <p class="text-[12.5px] text-ink-500 mt-1.5">${message}</p>
        </div>`;
}

function notFoundCard(message) {
    return `
        <div class="bg-white ring-soft rounded-md p-6 sm:p-7 text-center">
            <span class="inline-flex w-14 h-14 rounded-md bg-amber-50 text-amber-600 items-center justify-center text-2xl mb-3">
                <i class="ti ti-file-unknown"></i>
            </span>
            <h2 class="text-[18px] font-semibold text-ink-900">Inscripción no encontrada</h2>
            <p class="text-[12.5px] text-ink-500 mt-1.5 max-w-md mx-auto">${message}</p>
        </div>`;
}
