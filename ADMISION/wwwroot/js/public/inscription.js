// --- Helper: abre la guía/constancia en nueva pestaña para descarga ----
function triggerDownload(url) {
    // In-app browsers (Facebook, WhatsApp, TikTok, Instagram) block window.open.
    // Fallback: create a hidden <a> with download attribute and click it.
    try {
        var win = window.open(url, '_blank');
        if (!win || win.closed || typeof win === 'undefined') {
            throw new Error('popup-blocked');
        }
    } catch (_) {
        var a = document.createElement('a');
        a.href = url;
        a.download = '';
        a.style.display = 'none';
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { document.body.removeChild(a); }, 3000);
    }
}


// --- CAPTCHA helpers (Cloudflare Turnstile invisible para lookups PII) ---
// El widget invisible llama a `onLookupCaptchaSolved` cuando termina de calcular
// un token. Los tokens son de un solo uso, así que tras consumirlo en check-user
// reseteamos el widget para que entregue uno nuevo.
let __lookupCaptchaToken = null;
let __lookupCaptchaWaiters = [];
window.onLookupCaptchaSolved = function (token) {
    __lookupCaptchaToken = token;
    while (__lookupCaptchaWaiters.length) __lookupCaptchaWaiters.shift()(token);
};
window.onLookupCaptchaError = function () {
    while (__lookupCaptchaWaiters.length) __lookupCaptchaWaiters.shift()(null);
};

// --- CAPTCHA SUBMIT: auto-refresh to prevent token expiry ---
// Turnstile tokens expire after ~300 s. When the user spends >5 min
// filling the form the token goes stale. We reset the widget every 90 s
// and also right before the actual POST.
let __submitCaptchaTimer = null;
let __submitCaptchaWaiters = [];
window.onSubmitCaptchaSolved = function (token) {
    document.getElementById('captcha-expired-hint')?.classList.add('hidden');
    while (__submitCaptchaWaiters.length) __submitCaptchaWaiters.shift()(token);
};
window.onSubmitCaptchaError = function () {
    while (__submitCaptchaWaiters.length) __submitCaptchaWaiters.shift()(null);
};

function resetSubmitCaptcha() {
    if (!window.captchaConfig || !window.captchaConfig.enabled) return;
    if (window.captchaConfig.provider !== 'Turnstile') return;
    if (window.turnstile) {
        var el = document.getElementById('turnstile-submit');
        if (el) {
            try { window.turnstile.reset(el); } catch (_) { /* noop */ }
        }
    }
}

function startSubmitCaptchaAutoRefresh() {
    stopSubmitCaptchaAutoRefresh();
    __submitCaptchaTimer = setInterval(function () {
        var hint = document.getElementById('captcha-expired-hint');
        if (hint) hint.classList.remove('hidden');
        resetSubmitCaptcha();
    }, 90000); // every 90 seconds
}

function stopSubmitCaptchaAutoRefresh() {
    if (__submitCaptchaTimer) {
        clearInterval(__submitCaptchaTimer);
        __submitCaptchaTimer = null;
    }
}

async function getSubmitCaptchaToken() {
    if (!window.captchaConfig || !window.captchaConfig.enabled) return true;
    if (window.captchaConfig.provider === 'ReCaptcha') return true;

    // If a fresh token already exists in the field, use it
    var form = document.getElementById('inscriptionForm');
    var tsField = form ? form.querySelector('[name="cf-turnstile-response"]') : null;
    if (tsField && tsField.value) return true;

    // Otherwise reset the widget and wait for the callback
    resetSubmitCaptcha();
    return new Promise(function (resolve) {
        __submitCaptchaWaiters.push(resolve);
        setTimeout(function () {
            var idx = __submitCaptchaWaiters.indexOf(resolve);
            if (idx >= 0) {
                __submitCaptchaWaiters.splice(idx, 1);
                resolve(false);
            }
        }, 15000);
    });
}

async function getLookupCaptchaToken() {
    if (!window.captchaConfig || !window.captchaConfig.enabled) return null;
    if (__lookupCaptchaToken) return __lookupCaptchaToken;
    return new Promise(resolve => {
        __lookupCaptchaWaiters.push(resolve);
        // 10s timeout: si Turnstile no responde, libera al caller con null.
        setTimeout(() => {
            const idx = __lookupCaptchaWaiters.indexOf(resolve);
            if (idx >= 0) {
                __lookupCaptchaWaiters.splice(idx, 1);
                resolve(null);
            }
        }, 10000);
    });
}

function consumeLookupCaptchaToken() {
    __lookupCaptchaToken = null;
    if (window.turnstile) {
        const el = document.getElementById('captcha-lookup');
        if (el) {
            try { window.turnstile.reset(el); } catch (_) { /* noop */ }
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // --- REGISTRY AND UTILS ---
    window.customSelectRegistry = window.customSelectRegistry || {};
    
    // --- COUNTDOWN LOGIC ---
    const countdownEl = document.getElementById('countdown');
    if (countdownEl) {
        const targetDateStr = countdownEl.dataset.endDate;
        if (targetDateStr) {
            const targetDate = new Date(targetDateStr).getTime();
            
            function updateCountdown() {
                const now = new Date().getTime();
                const distance = targetDate - now;

                if (distance < 0) {
                    countdownEl.innerHTML = "<span class='text-white font-bold uppercase tracking-widest py-4'>El proceso de inscripción ha finalizado</span>";
                    return;
                }

                const days = Math.floor(distance / (1000 * 60 * 60 * 24));
                const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
                const seconds = Math.floor((distance % (1000 * 60)) / 1000);

                document.getElementById('days').innerText = days.toString().padStart(2, '0');
                document.getElementById('hours').innerText = hours.toString().padStart(2, '0');
                document.getElementById('minutes').innerText = minutes.toString().padStart(2, '0');
                document.getElementById('seconds').innerText = seconds.toString().padStart(2, '0');
            }

            setInterval(updateCountdown, 1000);
            updateCountdown();
        }
    }

    // --- BIRTH DATE: 3 selects → hidden yyyy-MM-dd ---
    function updateBirthDateHidden() {
        const d = document.getElementById('BirthDate_Day')?.value;
        const m = document.getElementById('BirthDate_Month')?.value;
        const y = document.getElementById('BirthDate_Year')?.value;
        const hidden = document.querySelector('[name="BirthDate"]');
        if (hidden && d && m && y) {
            hidden.value = `${y}-${m}-${d}`;
        }
        toggleGuardianSection();
    }
    ['BirthDate_Day', 'BirthDate_Month', 'BirthDate_Year'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.addEventListener('change', updateBirthDateHidden);
    });

    // --- SECTION TRACKING ---
    window.currentSection = 1;
    window.completedSections = { 1: false, 2: false, 3: false };

    function goToSection(num) {
      if (num === window.currentSection) {
        var body = document.getElementById('section' + num);
        if (body) body.classList.toggle('hidden');
        return;
      }
      var oldBody = document.getElementById('section' + window.currentSection);
      var newBody = document.getElementById('section' + num);
      if (oldBody) oldBody.classList.add('hidden');
      if (newBody) newBody.classList.remove('hidden');
      window.currentSection = num;
      var card = document.getElementById('card-section' + num);
      if (card) window.scrollTo({ top: card.offsetTop - 100, behavior: 'smooth' });
    }

    // Click handlers for section headers — only navigate to completed/current sections
    [1, 2, 3].forEach(function(n) {
      var card = document.getElementById('card-section' + n);
      if (!card) return;
      var header = card.querySelector('div:first-child');
      if (!header) return;
      header.addEventListener('click', function(e) {
        if (e.target.closest('button') || e.target.closest('a') || e.target.closest('input')) return;
        var isAccessible = n === 1 || window.completedSections[n - 1];
        if (!isAccessible) return;
        goToSection(n);
      });
    });

    window.nextSection = function(current, next) {
        // Pre-validation for Step 1
        if (current === 1) {
            const fields = {
                'Nacionalidad': document.getElementById('Nationality').value,
                'Tipo de Documento': document.getElementById('DocumentType').value,
                'Número de Documento': document.querySelector('[name="DocumentNumber"]').value,
                'Modalidad': document.getElementById('ModalityId').value,
                'Tipo de Postulante': document.getElementById('TypePostulantId').value,
                'Celular': document.querySelector('[name="PhoneNumber"]').value,
                'Carrera': document.getElementById('CareerId').value,
                'Dirección': document.querySelector('[name="Address"]')?.value,
                'País de Procedencia': document.getElementById('CountryId')?.value
            };

            const missing = Object.keys(fields).filter(key => !fields[key]);

            // Conditional Payment Validation
            const paymentSection = document.getElementById('paymentSection');
            if (paymentSection && paymentSection.style.display !== 'none') {
                const amount = document.querySelector('[name="PaymentAmount"]').value;
                const isExonerated = !amount || parseFloat(amount) <= 0;

                if (!isExonerated) {
                    const methodPayment = document.getElementById('MethodPaymentId').value;
                    const code = document.getElementById('PaymentCodeHidden').value;
                    const voucherInput = document.querySelector('[name="PaymentVoucher"]');
                    const hasVoucher = voucherInput && voucherInput.files && voucherInput.files.length > 0;

                    if (!methodPayment) missing.push('Medio de Pago');
                    if (!code) missing.push('Código de Operación');
                    if (!hasVoucher) missing.push('Foto del Comprobante');
                }
            }

            // Validate type postulant requirement file (descuento)
            const tpReqSection = document.getElementById('typePostulantRequirementSection');
            if (tpReqSection && tpReqSection.style.display !== 'none') {
                const tpFileInput = tpReqSection.querySelector('input[type="file"][required]');
                if (tpFileInput && !tpFileInput.value) {
                    missing.push('Requisito del Tipo de Postulante');
                }
            }

            // Validate profile photo if the section is visible (modality requires it)
            const profilePhotoSection = document.getElementById('profilePhotoSection');
            if (profilePhotoSection && profilePhotoSection.style.display !== 'none') {
                const ppInput = document.getElementById('ProfilePhotoDropzone_input');
                if (ppInput && !ppInput.files?.length) {
                    missing.push('Foto de perfil');
                }
            }

            if (missing.length > 0) {
                Swal.fire({
                    title: 'Campos incompletos',
                    html: `Por favor complete los siguientes campos obligatorios:<br><br><ul class="text-left list-disc list-inside">${missing.map(m => `<li>${m}</li>`).join('')}</ul>`,
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                return;
            }

            // Validate document number format by type
            var docType = document.getElementById('DocumentType')?.value;
            var docNum = docNumberInput?.value || '';
            if (docType === 'DNI' && !/^\d{8}$/.test(docNum)) {
                Swal.fire({
                    title: 'Documento inválido',
                    text: 'Para DNI debe ingresar exactamente 8 dígitos.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                if (docNumberInput) docNumberInput.focus();
                return;
            }
            if (docType === 'CE' && (!docNum || docNum.length > 15)) {
                Swal.fire({
                    title: 'Documento inválido',
                    text: 'Carnet de Extranjería debe tener máximo 15 caracteres.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                if (docNumberInput) docNumberInput.focus();
                return;
            }
            if (docType === 'PASAPORTE' && (!docNum || docNum.length > 20)) {
                Swal.fire({
                    title: 'Documento inválido',
                    text: 'Pasaporte debe tener máximo 20 caracteres.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                if (docNumberInput) docNumberInput.focus();
                return;
            }

            // Validate phone: exactly 9 digits
            var phone = document.querySelector('[name="PhoneNumber"]')?.value || '';
            if (!/^\d{9}$/.test(phone)) {
                Swal.fire({
                    title: 'Celular inválido',
                    text: 'El celular debe tener exactamente 9 dígitos numéricos.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                document.querySelector('[name="PhoneNumber"]')?.focus();
                return;
            }

            // Validate ubigeo code for Peruvians — only when the ubigeo block is visible
            var nat = document.getElementById('Nationality')?.value;
            var peruContainer = document.getElementById('peruUbigeoContainer');
            if (nat === 'Peruano' && peruContainer && !peruContainer.classList.contains('hidden')) {
                var ubigeoCode = document.getElementById('UbigeoCode')?.value;
                var ubigeoValid = document.getElementById('UbigeoIdHidden')?.value;
                if (!ubigeoCode || ubigeoCode.length !== 6 || !ubigeoValid) {
                    Swal.fire({
                        title: 'Ubigeo inválido',
                        text: 'Debe ingresar un código de ubigeo válido de 6 dígitos encontrado en su DNI.',
                        icon: 'warning',
                        confirmButtonColor: '#10b981'
                    });
                    document.getElementById('UbigeoCode')?.focus();
                    return;
                }
            }
        }

        // Pre-validation for Step 2
        if (current === 2) {
            const requirements = requirementsContainer.querySelectorAll('input[type="file"][required]');
            let missing = false;
            requirements.forEach(input => {
                if (!input.value) {
                    missing = true;
                    input.classList.add('border-red-500');
                } else {
                    input.classList.remove('border-red-500');
                }
            });

            if (missing) {
                Swal.fire({
                    title: 'Documentos faltantes',
                    text: 'Debe cargar todos los documentos obligatorios para continuar.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                return;
            }
        }

        // Pre-validation for Step 3
        if (current === 3) {
            var missing3 = [];

            // Validate SchoolType if wrapper is visible
            var stWrapper = document.getElementById('schoolTypeWrapper');
            if (stWrapper && !stWrapper.classList.contains('hidden')) {
                if (!document.getElementById('SchoolType')?.value) {
                    missing3.push('Gestión educativa');
                }
            }

            if (missing3.length > 0) {
                Swal.fire({
                    title: 'Campos incompletos',
                    html: `Por favor complete los siguientes campos obligatorios:<br><br><ul class="text-left list-disc list-inside">${missing3.map(m => `<li>${m}</li>`).join('')}</ul>`,
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                return;
            }
        }

        // Mark current as completed
        window.completedSections[current] = true;
        window.currentSection = next;

        const curDiv = document.getElementById('section' + current);
        const curCard = document.getElementById('card-section' + current);
        const nextDiv = document.getElementById('section' + next);
        const nextCard = document.getElementById('card-section' + next);

        // Styling for closed/completed
        curDiv.classList.add('hidden');
        const header = curCard.querySelector('div:first-child');
        header.classList.remove('bg-primary', 'text-white', 'bg-ink-100', 'text-ink-700', 'bg-slate-100', 'text-slate-700');
        header.classList.add('bg-emerald-500', 'text-white'); // Completed color
        if (!header.innerHTML.includes('LISTO')) {
             header.innerHTML = header.innerHTML.replace(/PASO \d\/\d/, '<span class="step-badge text-white/80 text-[10px] px-3 py-1 rounded-full tracking-[0.15em] font-semibold"><i class="ti ti-circle-check mr-1"></i> LISTO</span>');
        }

        // Styling for opening
        nextCard.classList.remove('opacity-50', 'pointer-events-none');
        nextCard.classList.add('border-primary', 'shadow-lg');
        const nextHeader = nextCard.querySelector('div:first-child');
        nextHeader.classList.remove('bg-ink-100', 'text-ink-700', 'bg-slate-100', 'text-slate-700');
        nextHeader.classList.add('bg-primary', 'text-white');
        nextDiv.classList.remove('hidden');

        // When section 3 becomes visible: reset submit captcha for a fresh token
        // and start auto-refresh to prevent expiry while user keeps filling fields.
        if (next === 3) {
            resetSubmitCaptcha();
            startSubmitCaptchaAutoRefresh();
        }

        window.scrollTo({ top: nextCard.offsetTop - 100, behavior: 'smooth' });
    }

    window.prevSection = function(current, prev) {
        const curDiv = document.getElementById('section' + current);
        const prevDiv = document.getElementById('section' + prev);
        const prevCard = document.getElementById('card-section' + prev);

        curDiv.classList.add('hidden');
        prevDiv.classList.remove('hidden');
        window.scrollTo({ top: prevCard.offsetTop - 100, behavior: 'smooth' });
    }

    // --- DATA LOADING LOGIC ---
    const modalitySelect = document.getElementById('ModalityId');
    const typeModalitySelect = document.getElementById('TypeModalityId');
    const requirementsContainer = document.getElementById('requirementsContainer');
    const paymentSection = document.getElementById('paymentSection');

    async function loadPaymentInfo(modalityId, typeModalityId) {
        const typePostulantId = document.getElementById('TypePostulantId')?.value;

        // Si no hay modalidad o no hay tipo de postulante seleccionado, ocultamos la sección
        if (!modalityId || !typePostulantId) {
            paymentSection.style.display = 'none';
            return;
        }

        try {
            let url = `/public/payment-info?modalityId=${modalityId}`;
            if (typeModalityId) url += `&typeModalityId=${typeModalityId}`;
            if (typePostulantId) url += `&typePostulantId=${typePostulantId}`;

            const response = await fetch(url);
            const info = await response.json();

            if (info.requiresPayment) {
                paymentSection.style.display = 'block';
                // Si el monto final es 0, ocultar los campos de pago (código, voucher)
                togglePaymentFields(info.finalAmount > 0);
                document.getElementById('paymentConceptTitle').innerText = `Concepto: ${info.conceptDescription} (${info.conceptCode})`;
                document.getElementById('baseAmount').innerText = info.baseAmount.toFixed(2);
                
                const discountContainer = document.getElementById('discountContainer');
                if (info.discountPercentage > 0) {
                    discountContainer.classList.remove('hidden');
                    document.getElementById('discountValue').innerText = info.discountPercentage;
                } else {
                    discountContainer.classList.add('hidden');
                }
                
                document.getElementById('finalAmountText').innerText = info.finalAmount.toFixed(2);
                document.querySelector('[name="PaymentAmount"]').value = info.finalAmount.toFixed(2);
            } else {
                paymentSection.style.display = 'none';
                document.querySelector('[name="PaymentAmount"]').value = "0.00";
                var voucherInput = document.querySelector('[name="PaymentVoucher"]');
                if (voucherInput) voucherInput.removeAttribute('required');
            }

            // Type postulant requirement: mostrar si el tipo de postulante tiene un requisito asignado
            if (typePostulantId) {
                loadTypePostulantRequirement(typePostulantId);
            } else {
                document.getElementById('typePostulantRequirementSection').style.display = 'none';
            }
        } catch (error) {
        }
    }

    function togglePaymentFields(show) {
        const codeBanco = document.getElementById('paymentCodeBanco');
        const codeCaja = document.getElementById('paymentCodeCaja');
        const dropzone = document.getElementById('PaymentVoucherDropzone')?.closest('.space-y-4')?.querySelector('._CustomDropzone') 
            || document.querySelector('[name="PaymentVoucher"]')?.closest('.form-field') 
            || document.querySelector('[id^="PaymentVoucherDropzone"]')?.closest('div');
        const guideContainer = document.getElementById('paymentGuideContainer');
        const paymentCodeHidden = document.getElementById('PaymentCodeHidden');

        if (!show) {
            // Ocultar y limpiar campos de pago
            if (codeBanco) codeBanco.classList.add('hidden');
            if (codeCaja) codeCaja.classList.add('hidden');
            if (guideContainer) guideContainer.classList.add('hidden');
            if (dropzone) dropzone.style.display = 'none';
            if (paymentCodeHidden) paymentCodeHidden.value = '';
            var voucherInput = document.querySelector('[name="PaymentVoucher"]');
            if (voucherInput) voucherInput.removeAttribute('required');
            ['PaymentCodeBancoInput', 'PaymentCodeCajaPart1', 'PaymentCodeCajaPart2'].forEach(id => {
                const el = document.getElementById(id);
                if (el) { el.value = ''; el.removeAttribute('required'); }
            });
        } else {
            const methodId = document.getElementById('MethodPaymentId')?.value;
            if (methodId) {
                updatePaymentGuide(methodId);
                updatePaymentCodeFormat(methodId);
            } else {
                if (codeBanco) codeBanco.classList.remove('hidden');
            }
            if (dropzone) dropzone.style.display = '';
            var voucherInput = document.querySelector('[name="PaymentVoucher"]');
            if (voucherInput) voucherInput.setAttribute('required', 'required');
            // Marcar los inputs de código visibles como required
            const methodId2 = document.getElementById('MethodPaymentId')?.value;
            const name = methodId2 ? (window.inscriptionData?.methodPayments || []).find(m => (m.id || m.Id) == methodId2)?.name?.toUpperCase() : '';
            if (name && name.includes('CAJA')) {
                ['PaymentCodeCajaPart1', 'PaymentCodeCajaPart2'].forEach(id => {
                    const el = document.getElementById(id);
                    if (el) el.setAttribute('required', 'required');
                });
            } else {
                const el = document.getElementById('PaymentCodeBancoInput');
                if (el) el.setAttribute('required', 'required');
            }
        }
    }

    function isExonerated() {
        const amount = document.querySelector('[name="PaymentAmount"]')?.value;
        return !amount || parseFloat(amount) <= 0;
    }

    function updatePaymentGuide(methodPaymentId) {
        if (isExonerated()) return;
        const container = document.getElementById('paymentGuideContainer');
        const img = document.getElementById('paymentGuideImg');
        const caption = document.getElementById('paymentGuideCaption');
        if (!container || !img) return;

        const methods = window.inscriptionData?.methodPayments || [];
        const method = methods.find(m => (m.id || m.Id) == methodPaymentId);
        if (!method) { container.classList.add('hidden'); return; }

        const name = (method.name || '').toUpperCase();
        if (name.includes('BANCO')) {
            img.src = '/img/banco.jpeg';
            if (caption) caption.textContent = 'Referencia para pagos por BANCO DE LA NACIÓN';
            container.classList.remove('hidden');
        } else if (name.includes('CAJA')) {
            img.src = '/img/caja.jpeg';
            if (caption) caption.textContent = 'Referencia para pagos por CAJA UNAMAD';
            container.classList.remove('hidden');
        } else {
            container.classList.add('hidden');
        }
    }

    function updatePaymentCodeFormat(methodPaymentId) {
        if (isExonerated()) return;
        const bancoInput = document.getElementById('paymentCodeBanco');
        const cajaInput = document.getElementById('paymentCodeCaja');
        const hiddenInput = document.getElementById('PaymentCodeHidden');
        if (!bancoInput || !cajaInput || !hiddenInput) return;

        const methods = window.inscriptionData?.methodPayments || [];
        const method = methods.find(m => (m.id || m.Id) == methodPaymentId);
        if (!method) { bancoInput.classList.remove('hidden'); cajaInput.classList.add('hidden'); return; }

        const name = (method.name || '').toUpperCase();
        if (name.includes('CAJA')) {
            cajaInput.classList.remove('hidden');
            bancoInput.classList.add('hidden');
            // Clean banco input
            const bancoEl = document.getElementById('PaymentCodeBancoInput');
            if (bancoEl) { bancoEl.value = ''; bancoEl.removeAttribute('required'); }
            ['PaymentCodeCajaPart1', 'PaymentCodeCajaPart2'].forEach(id => {
                const el = document.getElementById(id);
                if (el) el.setAttribute('required', 'required');
            });
            syncCajaCode();
        } else {
            bancoInput.classList.remove('hidden');
            cajaInput.classList.add('hidden');
            // Clean caja inputs
            ['PaymentCodeCajaPart1', 'PaymentCodeCajaPart2'].forEach(id => {
                const el = document.getElementById(id);
                if (el) { el.value = ''; el.removeAttribute('required'); }
            });
            const bancoEl = document.getElementById('PaymentCodeBancoInput');
            if (bancoEl) bancoEl.setAttribute('required', 'required');
            syncBancoCode();
        }
    }

    function syncBancoCode() {
        const val = document.getElementById('PaymentCodeBancoInput')?.value || '';
        document.getElementById('PaymentCodeHidden').value = val;
    }

    function syncCajaCode() {
        const p1 = document.getElementById('PaymentCodeCajaPart1')?.value || '';
        const p2 = document.getElementById('PaymentCodeCajaPart2')?.value || '';
        document.getElementById('PaymentCodeHidden').value = p1 + '-' + p2;
    }

    async function loadTypePostulantRequirement(typePostulantId) {
        const section = document.getElementById('typePostulantRequirementSection');
        const container = document.getElementById('typePostulantReqContainer');
        if (!section || !container || !typePostulantId) return;

        try {
            const response = await fetch(`/public/type-postulant-requirement/${typePostulantId}`);
            const req = await response.json();

            if (req && req.id) {
                const name = req.name || req.Name || 'Requisito del Tipo de Postulante';
                container.innerHTML = `
                    <div class="bg-white ring-soft rounded-md p-4">
                        <div class="flex items-center gap-3 mb-3">
                            <span class="w-8 h-8 rounded-md bg-primary/10 flex items-center justify-center shrink-0">
                                <i class="ti ti-file-upload text-primary text-xs"></i>
                            </span>
                            <div>
                                <p class="text-sm font-semibold text-ink-900">${name}</p>
                                <p class="text-[10px] text-ink-400 uppercase tracking-wide font-semibold">REQUERIDO</p>
                            </div>
                        </div>
                        <input type="file" name="Requirements_${req.id}" required
                               class="w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-xs file:font-black file:bg-primary file:text-white hover:file:bg-emerald-600 transition-colors" />
                    </div>
                `;
                section.style.display = 'block';
            } else {
                section.style.display = 'none';
                container.innerHTML = '';
            }
        } catch (error) {
            section.style.display = 'none';
        }
    }

    async function loadRequirements(modalityId, typeModalityId) {
        if (!modalityId) return;

        const typePostulantId = document.getElementById('TypePostulantId')?.value;
        requirementsContainer.innerHTML = '<div class="col-span-full py-10 text-center text-slate-500"><i class="ti ti-loader-2 fa-spin mr-2"></i> Cargando requisitos...</div>';

        try {
            let url = `/public/requirements?modalityId=${modalityId}`;
            if (typeModalityId) url += `&typeModalityId=${typeModalityId}`;
            if (typePostulantId) url += `&typePostulantId=${typePostulantId}`;
            
            const response = await fetch(url);
            const requirements = await response.json();
            
            requirementsContainer.innerHTML = '';
            if (requirements.length === 0) {
                requirementsContainer.innerHTML = '<div class="col-span-full py-10 text-center text-slate-500 bg-slate-50 rounded-lg border-2 border-dashed border-slate-200"><i class="ti ti-info-circle mr-2"></i> No se requieren documentos adicionales para esta modalidad.</div>';
                return;
            }

            requirements.forEach(req => {
                const div = document.createElement('div');
                div.className = 'group';
                div.innerHTML = `
                     <div class="bg-white border-2 border-slate-200 rounded-xl p-6 transition-all hover:border-primary/50 hover:shadow-md">
                        <div class="flex items-center gap-4 mb-4">
                            <div class="h-12 w-12 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xl">
                                <i class="ti ti-file-upload"></i>
                            </div>
                            <div>
                                <h4 class="font-bold text-slate-800 text-sm">${req.name}</h4>
                                <p class="text-[10px] text-slate-400 uppercase font-semibold">REQUERIDO</p>
                            </div>
                        </div>
                        <input type="file" name="Requirements_${req.fileRequirementManagementId}" required 
                               class="w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-xs file:font-black file:bg-primary file:text-white hover:file:bg-emerald-600 transition-colors" />
                    </div>
                `;
                requirementsContainer.appendChild(div);
            });
        } catch (error) {
            requirementsContainer.innerHTML = '<div class="col-span-full py-10 text-center text-red-500">Error al cargar requisitos.</div>';
        }
    }

    // Tabla local typeModalityId -> kind (external | internal | normal)
    window.typeModalityKinds = window.typeModalityKinds || {};

    function updateTransferSection() {
        const section = document.getElementById('transferSection');
        const ext = document.getElementById('transferExternal');
        const intl = document.getElementById('transferInternal');
        const title = document.getElementById('transferTitle');
        if (!section || !ext || !intl) return;

        const typeId = document.getElementById('TypeModalityId')?.value;
        const kind = typeId ? window.typeModalityKinds[typeId] : null;

        if (kind === 'external') {
            section.classList.remove('hidden');
            ext.classList.remove('hidden');
            intl.classList.add('hidden');
            if (title) title.textContent = 'Información de Traslado Externo';
        } else if (kind === 'internal') {
            section.classList.remove('hidden');
            intl.classList.remove('hidden');
            ext.classList.add('hidden');
            if (title) title.textContent = 'Información de Traslado Interno';
        } else {
            section.classList.add('hidden');
            ext.classList.add('hidden');
            intl.classList.add('hidden');
            // Limpiar valores para que no se envíen
            ['SourceUniversityId', 'SourceCareerId'].forEach(id => {
                if (window.customSelectRegistry[id]) window.customSelectRegistry[id].clear();
            });
            const nameInput = document.querySelector('[name="SourceCareerName"]');
            if (nameInput) nameInput.value = '';
        }
    }

    async function loadModalityInfo(modalityId) {
        const section = document.getElementById('modalityDatesSection');
        const examBox = document.getElementById('modalityExamDateBox');
        const resultsBox = document.getElementById('modalityResultsDateBox');
        const examText = document.getElementById('modalityExamDateText');
        const resultsText = document.getElementById('modalityResultsDateText');

        // Summary elements below countdown
        const sumExamBox = document.getElementById('summaryExamBox');
        const sumResultsBox = document.getElementById('summaryResultsBox');
        const sumExamText = document.getElementById('summaryExamText');
        const sumResultsText = document.getElementById('summaryResultsText');

        const hideAll = () => {
            if (section) section.classList.add('hidden');
            examBox?.classList.add('hidden');
            resultsBox?.classList.add('hidden');
            if (examText) examText.textContent = '';
            if (resultsText) resultsText.textContent = '';
            sumExamBox?.classList.add('hidden');
            sumResultsBox?.classList.add('hidden');
            if (sumExamText) sumExamText.textContent = '';
            if (sumResultsText) sumResultsText.textContent = '';
        };

        if (!modalityId) { hideAll(); return; }

        try {
            const response = await fetch(`/public/modality-info/${modalityId}`);
            if (!response.ok) { hideAll(); return; }
            const info = await response.json();

            const fmt = (iso) => {
                if (!iso) return null;
                const [y, m, d] = iso.split('-').map(Number);
                const date = new Date(y, m - 1, d);
                return date.toLocaleDateString('es-PE', { day: '2-digit', month: 'long', year: 'numeric' });
            };

            const examStr = fmt(info.examDate);
            const resultsStr = fmt(info.resultsPublicationDate);

            if (examStr && examText) {
                examText.textContent = examStr;
                examBox?.classList.remove('hidden');
            } else { examBox?.classList.add('hidden'); }

            if (resultsStr && resultsText) {
                resultsText.textContent = resultsStr;
                resultsBox?.classList.remove('hidden');
            } else { resultsBox?.classList.add('hidden'); }

            if (examStr || resultsStr) section?.classList.remove('hidden');
            else section?.classList.add('hidden');

            // Populate summary below countdown
            if (sumExamText) sumExamText.textContent = examStr || '';
            if (sumResultsText) sumResultsText.textContent = resultsStr || '';
            if (sumExamBox) sumExamBox.classList.toggle('hidden', !examStr);
            if (sumResultsBox) sumResultsBox.classList.toggle('hidden', !resultsStr);

            updateModalitySummary();
        } catch (err) {
            hideAll();
        }
    }

    function updateModalitySummary() {
        const section = document.getElementById('modalitySummary');
        if (!section) return;

        const modBox = document.getElementById('summaryModalityBox');
        const typeBox = document.getElementById('summaryTypeBox');
        const modText = document.getElementById('summaryModalityText');
        const typeText = document.getElementById('summaryTypeText');

        let visible = false;

        // Modality name
        const modalityId = document.getElementById('ModalityId')?.value;
        if (modalityId) {
            const mods = window.inscriptionData?.modalities || [];
            const mod = mods.find(m => (m.id || m.Id) == modalityId);
            if (mod) {
                modText.textContent = mod.name || mod.Name || '';
                modBox.classList.remove('hidden');
                visible = true;
            } else {
                modBox.classList.add('hidden');
            }
        } else {
            modBox.classList.add('hidden');
        }

        // Type modality name
        const typeId = document.getElementById('TypeModalityId')?.value;
        if (typeId && window.typeModalityNames?.[typeId]) {
            typeText.textContent = window.typeModalityNames[typeId];
            typeBox.classList.remove('hidden');
            visible = true;
        } else {
            typeBox.classList.add('hidden');
        }

        section.classList.toggle('hidden', !visible);
    }

    async function loadTypeModalities(modalityId) {
        loadModalityInfo(modalityId);
        if (!modalityId) {
            document.getElementById('typeModalityWrapper').classList.add('hidden');
            window.typeModalityKinds = {};
            window.typeModalityNames = {};
            updateTransferSection();
            updateModalitySummary();
            return;
        }

        try {
            const response = await fetch(`/public/type-modalities/${modalityId}`);
            const types = await response.json();

            const wrapper = document.getElementById('typeModalityWrapper');
            window.typeModalityKinds = {};
            window.typeModalityNames = {};
            if (types.length > 0) {
                wrapper.classList.remove('hidden');
                const localOptions = types.map(t => {
                    const id = t.id || t.Id;
                    const name = t.name || t.Name;
                    const kind = t.kind || t.Kind || 'normal';
                    window.typeModalityKinds[id] = kind;
                    window.typeModalityNames[id] = name;
                    return { id, name };
                });
                loadStaticToCustom('TypeModalityId', localOptions);
            } else {
                wrapper.classList.add('hidden');
                if (window.customSelectRegistry['TypeModalityId']) window.customSelectRegistry['TypeModalityId'].clear();
            }

            // Wait for custom select to update if necessary, then load requirements and payment info
            setTimeout(() => {
                const currentTypeModalityId = document.getElementById('TypeModalityId')?.value;
                loadRequirements(modalityId, currentTypeModalityId);
                loadPaymentInfo(modalityId, currentTypeModalityId);
                updateTransferSection();
                updateModalitySummary();
            }, 50);
        } catch (error) {
        }
    }

    function loadCareersByModality(modalityId, typeModalityId) {
        const modMap = (window.inscriptionData?.modalityCareerMap) || {};
        const typeModMap = (window.inscriptionData?.typeModalityCareerMap) || {};
        const allCareers = window.inscriptionData?.careers || [];
        let filtered = allCareers;

        // Step 1: Filter by Modality (ModalityCareer)
        const modCareerIds = modalityId ? (modMap[modalityId] || []) : [];
        if (modCareerIds.length > 0) {
            const idSet = new Set(modCareerIds.map(id => typeof id === 'string' ? id : id.toString()));
            filtered = filtered.filter(c => idSet.has((c.id || c.Id || '').toString()));
        } else if (modalityId) {
            filtered = [];
        }

        // Step 2: Further filter by TypeModality (TypeModalityCareer)
        if (typeModalityId && filtered.length > 0) {
            const typeCareerIds = typeModMap[typeModalityId] || [];
            if (typeCareerIds.length > 0) {
                const typeIdSet = new Set(typeCareerIds.map(id => typeof id === 'string' ? id : id.toString()));
                filtered = filtered.filter(c => typeIdSet.has((c.id || c.Id || '').toString()));
            }
        }

        if (window.customSelectRegistry['CareerId']) {
            if (filtered.length > 0) {
                loadStaticToCustom('CareerId', filtered);
                // Si el valor actual ya no está en el listado filtrado, limpiar
                var currentVal = document.getElementById('CareerId')?.value;
                if (currentVal) {
                    var stillValid = filtered.some(function(c) { return (c.id || c.Id || '').toString() === currentVal; });
                    if (!stillValid) {
                        window.customSelectRegistry['CareerId'].clear();
                    }
                }
            } else {
                loadStaticToCustom('CareerId', []);
                window.customSelectRegistry['CareerId'].clear();
            }
        }
    }

    function updateProfilePhotoSection() {
        const section = document.getElementById('profilePhotoSection');
        if (!section) return;
        const modId = document.getElementById('ModalityId')?.value;
        const required = modId && window.inscriptionData?.modalityFlags?.[modId]?.requiresProfilePhoto;
        if (required) {
            section.style.display = '';
            const input = document.getElementById('ProfilePhotoDropzone_input');
            if (input) input.setAttribute('required', 'required');
        } else {
            section.style.display = 'none';
            const input = document.getElementById('ProfilePhotoDropzone_input');
            if (input) input.removeAttribute('required');
        }
    }

    function updateEducationSections() {
        const modId = document.getElementById('ModalityId')?.value;
        const flags = modId ? window.inscriptionData?.modalityFlags?.[modId] : null;

        const levelWrapper = document.getElementById('educationalLevelWrapper');
        const gradeWrapper = document.getElementById('gradeWrapper');

        if (schoolTypeWrapper) {
            var schoolId = document.getElementById('SchoolId')?.value;
            var isAbroad = checkAbroad?.checked;
            var needsManualType = isAbroad || schoolId === 'OTHER';
            schoolTypeWrapper.classList.toggle('hidden', !needsManualType);
        }

        if (levelWrapper) {
            levelWrapper.classList.toggle('hidden', !(flags?.requiresEducationalLevel));
        }

        if (gradeWrapper) {
            gradeWrapper.classList.toggle('hidden', !(flags?.requiresGrade));
        }
    }

    function loadGradesByLevel(level) {
        if (window.customSelectRegistry['Grade']) {
            var grades = [];
            if (level === 'PRIMARIA') {
                grades = [{ id: '1', name: '1°' }, { id: '2', name: '2°' }, { id: '3', name: '3°' }, { id: '4', name: '4°' }, { id: '5', name: '5°' }, { id: '6', name: '6°' }];
            } else if (level === 'SECUNDARIA') {
                grades = [{ id: '1', name: '1°' }, { id: '2', name: '2°' }, { id: '3', name: '3°' }, { id: '4', name: '4°' }, { id: '5', name: '5°' }];
            }
            window.customSelectRegistry['Grade'].setOptions(grades);
        }
    }

    document.getElementById('EducationalLevel')?.addEventListener('change', function (e) {
        loadGradesByLevel(e.target.value);
    });

    if (modalitySelect) {
         modalitySelect.addEventListener('change', (e) => {
             loadTypeModalities(e.target.value);
             loadCareersByModality(e.target.value);
             updateProfilePhotoSection();
             updateEducationSections();
             // Al cambiar modalidad se debe re-seleccionar tipo de modalidad y carrera
             if (window.customSelectRegistry['CareerId']) {
                 window.customSelectRegistry['CareerId'].clear();
             }
         });
    }
    
    if (typeModalitySelect) {
        typeModalitySelect.addEventListener('change', (e) => {
            const modId = modalitySelect.value;
            const typeId = e.target.value;
            loadCareersByModality(modId, typeId);
            loadRequirements(modId, typeId);
            loadPaymentInfo(modId, typeId);
            updateTransferSection();
            updateModalitySummary();
        });
    }
    
    const typePostulantSelect = document.getElementById('TypePostulantId');
    if (typePostulantSelect) {
        typePostulantSelect.addEventListener('change', () => {
             const modId = modalitySelect.value;
             const typeId = document.getElementById('TypeModalityId').value;
             loadRequirements(modId, typeId);
             loadPaymentInfo(modId, typeId);
        });
    }

    // --- USER CHECK LOGIC ---
    const docNumberInput = document.querySelector('[name="DocumentNumber"]');
    const docTypeInput = document.getElementById('DocumentType');

    var _checkingUser = false;

    async function checkUser() {
        if (_checkingUser) return;
        const docNum = docNumberInput.value;
        const docType = docTypeInput.value;

        if (docNum.length >= 8 && docType) {
            _checkingUser = true;
            try {
                const headers = {};
                const captchaToken = await getLookupCaptchaToken();
                if (captchaToken) headers['X-Captcha-Token'] = captchaToken;

                const response = await fetch(`/public/check-user?docType=${docType}&docNumber=${docNum}`, { headers });

                // Token consumido (válido o no) → refrescar para la siguiente búsqueda.
                consumeLookupCaptchaToken();

                if (response.status === 401) {
                    return; // Captcha rechazado: silenciar, el usuario igual completará a mano.
                }
                if (!response.ok) {
                    clearAutoFilledFields();
                    return;
                }
                if (response.ok) {
                    const user = await response.json();
                    if (user) {
                        // Autocomplete Personal Data
                        document.querySelector('[name="Name"]').value = user.name;
                        document.querySelector('[name="FatherSurname"]').value = user.firstNameFather;
                        document.querySelector('[name="MotherSurname"]').value = user.firstNameMother;
                        const bd = user.birthdate.split('T')[0];
                        const [y, m, d] = bd.split('-');
                        const bdDay = document.getElementById('BirthDate_Day');
                        const bdMonth = document.getElementById('BirthDate_Month');
                        const bdYear = document.getElementById('BirthDate_Year');
                        if (bdDay) bdDay.value = d;
                        if (bdMonth) bdMonth.value = m;
                        if (bdYear) bdYear.value = y;
                        updateBirthDateHidden();
                        document.querySelector('[name="Email"]').value = user.email;
                        document.querySelector('[name="PhoneNumber"]').value = user.phoneNumber;
                        
                        if (window.customSelectRegistry['Genero']) {
                            const genName = user.genero === 'M' ? 'Masculino' : 'Femenino';
                            window.customSelectRegistry['Genero'].setValue(user.genero, genName);
                        }

                        // ── Address ──
                        const addrInput = document.querySelector('[name="Address"]');
                        if (addrInput && user.address) addrInput.value = user.address;

                        // ── CountryId + Nationality ──
                        // Nacionalidad = "Peruano" porque tiene DNI (ya fue seteado en step 1).
                        // CountryId = país de procedencia / nacimiento (puede ser distinto de Perú).
                        // El modo ubigeo (código vs cascada) depende de CountryId, NO de Nationality.
                        if (user.countryId && window.customSelectRegistry['CountryId']) {
                            // CountryId — set value y dispara change (carga departamentos + modo)
                            var countryData = (initData.countries || []).find(function(c) {
                                return c.id === user.countryId;
                            });
                            if (countryData) {
                                window.customSelectRegistry['CountryId'].setValue(countryData.id, countryData.name);
                            }
                        }

                        // Aplicar modo ubigeo según el país EFECTIVAMENTE seleccionado:
                        // si el usuario no trae país (o su país no está en el catálogo),
                        // se conserva la selección actual (por defecto Perú) → input de código.
                        applyUbigeoModeByCountry();

                        // ── Ubigeo (Nacional mode — code input) ──
                        if (user.ubigeoCode) {
                            const codeInput = document.getElementById('UbigeoCode');
                            if (codeInput && !codeInput.closest('.hidden')) {
                                codeInput.value = user.ubigeoCode;
                                validateUbigeoCode();
                            }
                        }

                        // ── Ubigeo (cascade mode — extranjero) ──
                        function setCascadeValue(id, val) {
                            const r = window.customSelectRegistry[id];
                            if (!r || !val) return;
                            const list = document.getElementById('options_' + id);
                            let label = '';
                            if (list) {
                                const opt = list.querySelector(`[data-value="${val}"]`);
                                if (opt) label = opt.textContent;
                            }
                            r.setValue(val, label);
                        }
                        const cascadeBlock = document.getElementById('ubigeoCascadeBlock');
                        if (cascadeBlock && !cascadeBlock.classList.contains('hidden')
                            && (user.departmentId || user.provincieId || user.ubigeoId)) {
                            if (user.departmentId) {
                                setCascadeValue('DepartmentId', user.departmentId);
                                setTimeout(() => {
                                    if (user.provincieId) setCascadeValue('ProvincieId', user.provincieId);
                                    setTimeout(() => {
                                        if (user.ubigeoId) setCascadeValue('DistritId', user.ubigeoId);
                                    }, 450);
                                }, 450);
                            }
                        }

                        // ── School data ──
                        if (user.otherSchool) {
                            const otherInput = document.querySelector('[name="OtherSchool"]');
                            if (otherInput) otherInput.value = user.otherSchool;
                        }
                        // Cascade escolar (departamento → provincia → distrito → colegio)
                        setTimeout(() => {
                            if (user.schoolDepartmentId) setCascadeValue('SchoolDepartmentId', user.schoolDepartmentId);
                            setTimeout(() => {
                                if (user.schoolProvincieId) setCascadeValue('SchoolProvincieId', user.schoolProvincieId);
                                setTimeout(() => {
                                    if (user.schoolDistritId) setCascadeValue('SchoolDistritId', user.schoolDistritId);
                                    setTimeout(() => {
                                        if (user.schoolId) setCascadeValue('SchoolId', user.schoolId);
                                    }, 450);
                                }, 450);
                            }, 450);
                        }, 450);

                        // Lock personal data fields
                        const fieldsToLock = ['Name', 'FatherSurname', 'MotherSurname', 'BirthDate'];
                        fieldsToLock.forEach(f => {
                            const el = document.querySelector(`[name="${f}"]`);
                            if (el) {
                                el.readOnly = true;
                                el.classList.add('bg-slate-50', 'text-slate-500');
                            }
                        });

                        // Lock birth date selects
                        ['BirthDate_Day', 'BirthDate_Month', 'BirthDate_Year'].forEach(id => {
                            const el = document.getElementById(id);
                            if (el) {
                                el.disabled = true;
                                el.classList.add('bg-slate-50', 'text-slate-500', 'cursor-not-allowed');
                            }
                        });

                        // Lock ubigeo fields (national — code input)
                        const ubigeoCodeEl = document.getElementById('UbigeoCode');
                        if (ubigeoCodeEl && user.ubigeoCode) {
                            ubigeoCodeEl.readOnly = true;
                            ubigeoCodeEl.classList.add('bg-slate-50', 'text-slate-500');
                        }

                        // Lock ubigeo fields (cascade) solo si hubo datos autocompletados.
                        // Sin inscripción previa la cascada debe quedar editable.
                        if (user.departmentId || user.provincieId || user.ubigeoId) {
                            ['DepartmentId', 'ProvincieId', 'DistritId'].forEach(id => {
                                if (window.customSelectRegistry[id]) {
                                    window.customSelectRegistry[id].disable();
                                }
                            });
                        }

                        // Lock address
                        const addrEl = document.querySelector('[name="Address"]');
                        if (addrEl && user.address) {
                            addrEl.readOnly = true;
                            addrEl.classList.add('bg-slate-50', 'text-slate-500');
                        }
                        
                        // Notify user
                        const toast = Swal.mixin({
                            toast: true,
                            position: 'top-end',
                            showConfirmButton: false,
                            timer: 3000,
                            timerProgressBar: true
                        });
                        toast.fire({
                            icon: 'info',
                            title: 'Datos recuperados del sistema'
                        });
                    }
                }
            } catch (error) {
                console.error('[checkUser] Error al verificar documento:', error);
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'warning',
                    title: 'No se pudo verificar el documento',
                    text: 'Intente nuevamente o continúe ingresando sus datos manualmente.',
                    showConfirmButton: false,
                    timer: 4000,
                    timerProgressBar: true
                });
            } finally {
                _checkingUser = false;
            }
        } else {
            // Doc number too short → clear previously auto-filled data
            clearAutoFilledFields();
        }
    }

    function clearAutoFilledFields() {
        // Clear main personal fields
        ['Name', 'FatherSurname', 'MotherSurname', 'Email'].forEach(name => {
            const el = document.querySelector(`[name="${name}"]`);
            if (el) {
                el.value = '';
                el.readOnly = false;
                el.classList.remove('bg-slate-50', 'text-slate-500');
            }
        });

        // BirthDate selects + hidden — unlock
        ['BirthDate_Day', 'BirthDate_Month', 'BirthDate_Year'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.value = '';
                el.disabled = false;
                el.classList.remove('bg-slate-50', 'text-slate-500', 'cursor-not-allowed');
            }
        });
        const bdHidden = document.getElementById('BirthDate');
        if (bdHidden) {
            bdHidden.value = '';
            bdHidden.readOnly = false;
            bdHidden.classList.remove('bg-slate-50', 'text-slate-500');
        }

        // Phone
        const phoneInput = document.querySelector('[name="PhoneNumber"]');
        if (phoneInput) phoneInput.value = '';

        // Genero custom select
        if (window.customSelectRegistry['Genero']) {
            window.customSelectRegistry['Genero'].clear();
        }

        // Address — unlock
        const addrInput = document.querySelector('[name="Address"]');
        if (addrInput) {
            addrInput.value = '';
            addrInput.readOnly = false;
            addrInput.classList.remove('bg-slate-50', 'text-slate-500');
        }

        // Ubigeo code — unlock
        const codeInput = document.getElementById('UbigeoCode');
        if (codeInput) {
            codeInput.value = '';
            codeInput.readOnly = false;
            codeInput.classList.remove('bg-slate-50', 'text-slate-500');
        }
        const ubigeoHidden = document.getElementById('UbigeoIdHidden');
        if (ubigeoHidden) ubigeoHidden.value = '';
        const ubigeoStatus = document.getElementById('ubigeoStatus');
        if (ubigeoStatus) {
            ubigeoStatus.classList.add('hidden');
            ubigeoStatus.textContent = '';
        }

        // Ubigeo cascade selects — re-enable
        ['DepartmentId', 'ProvincieId', 'DistritId'].forEach(id => {
            if (window.customSelectRegistry[id]) {
                window.customSelectRegistry[id].clear();
                window.customSelectRegistry[id].enable();
            }
        });

        // School data
        const otherInput = document.querySelector('[name="OtherSchool"]');
        if (otherInput) otherInput.value = '';
        ['SchoolDepartmentId', 'SchoolProvincieId', 'SchoolDistritId', 'SchoolId'].forEach(id => {
            if (window.customSelectRegistry[id]) {
                window.customSelectRegistry[id].clear();
            }
        });
    }

    docNumberInput?.addEventListener('blur', checkUser);
    docTypeInput?.addEventListener('change', function () {
        applyDocNumberRules();
        clearAutoFilledFields();
        checkUser();
    });

    // --- Document Number rules by document type ---
    function applyDocNumberRules() {
        var docType = docTypeInput?.value;
        var input = docNumberInput;
        if (!input) return;

        if (docType === 'DNI') {
            input.inputMode = 'numeric';
            input.maxLength = 8;
            input.value = input.value.replace(/\D/g, '').slice(0, 8);
        } else if (docType === 'CE') {
            input.inputMode = 'text';
            input.maxLength = 15;
            input.value = input.value.slice(0, 15);
        } else if (docType === 'PASAPORTE') {
            input.inputMode = 'text';
            input.maxLength = 20;
            input.value = input.value.slice(0, 20);
        }
    }

    docNumberInput?.addEventListener('input', function () {
        var docType = docTypeInput?.value;
        if (docType === 'DNI') {
            this.value = this.value.replace(/\D/g, '').slice(0, 8);
        }
        clearAutoFilledFields();
    });

    // --- NACIONALIDAD ⇄ TIPO DE DOCUMENTO ---
    // Si la nacionalidad es "Nacional" se fuerza DNI y se bloquea el cambio de tipo
    // de documento. Para otras nacionalidades se habilita la selección manual.
    function applyNationalityRules(nationality) {
        if (window.customSelectRegistry['DocumentType']) {
            if (nationality === 'Peruano') {
                loadStaticToCustom('DocumentType', [{ id: 'DNI', name: 'DNI' }]);
                window.customSelectRegistry['DocumentType'].setValue('DNI', 'DNI');
            } else {
                loadStaticToCustom('DocumentType', [
                    { id: 'CE', name: 'Carnet de Extranjería' },
                    { id: 'PASAPORTE', name: 'Pasaporte' }
                ]);
                window.customSelectRegistry['DocumentType'].clear();
            }
        }

        applyUbigeoModeByCountry();
    }

    // El modo ubigeo (código DNI vs cascada) depende del PAÍS DE PROCEDENCIA
    // (CountryId): mientras sea Perú se muestra el input de código; al cambiar
    // a otro país se muestra la cascada departamento → provincia → distrito.
    function applyUbigeoModeByCountry() {
        var countryId = document.getElementById('CountryId')?.value;
        var peruCountry = (initData.countries || []).find(function(c) {
            return c.name.toUpperCase().indexOf('PERÚ') !== -1;
        });
        var isPeru = !countryId || (peruCountry ? countryId === peruCountry.id : true);
        applyUbigeoMode(isPeru ? 'Peruano' : 'Extranjero');
    }

    document.getElementById('Nationality')?.addEventListener('change', (e) => {
        applyNationalityRules(e.target.value);
    });

    function applyUbigeoMode(nationality) {
        const byCode = document.getElementById('ubigeoByCodeBlock');
        const cascade = document.getElementById('ubigeoCascadeBlock');
        if (!byCode || !cascade) return;

        const useCode = nationality === 'Peruano';

        byCode.classList.toggle('hidden', !useCode);
        cascade.classList.toggle('hidden', useCode);

        toggleBlockInputs(byCode, useCode);
        toggleBlockInputs(cascade, !useCode);

        if (useCode) {
            // Limpiar selects cascada al pasar a modo código (no quedan valores fantasma)
            ['DepartmentId', 'ProvincieId', 'DistritId'].forEach(id => {
                if (window.customSelectRegistry[id]) window.customSelectRegistry[id].clear();
            });
        } else {
            // Limpiar input/hidden del modo código
            const codeInput = document.getElementById('UbigeoCode');
            const hidden = document.getElementById('UbigeoIdHidden');
            if (codeInput) codeInput.value = '';
            if (hidden) hidden.value = '';
            const status = document.getElementById('ubigeoStatus');
            if (status) { status.className = 'mt-3 hidden text-sm rounded-xl p-3'; status.textContent = ''; }
        }
    }

    function toggleBlockInputs(block, enabled) {
        const inputs = block.querySelectorAll('input, select, textarea');
        inputs.forEach(el => {
            if (enabled) el.removeAttribute('disabled');
            else el.setAttribute('disabled', 'disabled');
        });
    }

    // Validación AJAX del código de ubigeo
    const codeInput = document.getElementById('UbigeoCode');
    const codeHidden = document.getElementById('UbigeoIdHidden');
    const codeStatus = document.getElementById('ubigeoStatus');

    function setUbigeoStatus(kind, message) {
        if (!codeStatus) return;
        codeStatus.classList.remove('hidden');
        const base = 'mt-3 text-sm rounded-xl p-3 border';
        if (kind === 'ok') {
            codeStatus.className = `${base} bg-emerald-50 border-emerald-200 text-emerald-800`;
            codeStatus.innerHTML = `<i class="ti ti-circle-check mr-1"></i> ${message}`;
        } else if (kind === 'err') {
            codeStatus.className = `${base} bg-red-50 border-red-200 text-red-700`;
            codeStatus.innerHTML = `<i class="ti ti-alert-circle mr-1"></i> ${message}`;
        } else {
            codeStatus.className = `${base} bg-slate-50 border-slate-200 text-slate-600`;
            codeStatus.innerHTML = `<i class="ti ti-loader-2 fa-spin mr-1"></i> ${message}`;
        }
    }

    async function validateUbigeoCode() {
        if (!codeInput || !codeHidden) return;
        const raw = (codeInput.value || '').replace(/\D/g, '').slice(0, 6);
        codeInput.value = raw;
        codeHidden.value = '';

        if (raw.length === 0) {
            if (codeStatus) { codeStatus.classList.add('hidden'); codeStatus.textContent = ''; }
            return;
        }
        if (raw.length !== 6) {
            setUbigeoStatus('err', 'El ubigeo debe tener 6 dígitos.');
            return;
        }

        setUbigeoStatus('info', 'Verificando...');
        try {
            const resp = await fetch(`/public/ubigeo-by-code/${raw}`);
            if (!resp.ok) { setUbigeoStatus('err', 'No se pudo validar el ubigeo.'); return; }
            const data = await resp.json();
            if (!data.found) {
                setUbigeoStatus('err', 'El código no corresponde a ningún distrito.');
                return;
            }
            codeHidden.value = data.distritId;
            setUbigeoStatus('ok', `${data.departmentName} › ${data.provinceName} › <strong>${data.distritName}</strong>`);
        } catch (err) {
            setUbigeoStatus('err', 'Error de red al validar.');
        }
    }

    codeInput?.addEventListener('input', () => {
        // Mantener solo dígitos, máximo 6
        codeInput.value = (codeInput.value || '').replace(/\D/g, '').slice(0, 6);
        if (codeInput.value.length === 6) validateUbigeoCode();
        else if (codeStatus) { codeStatus.classList.add('hidden'); codeHidden.value = ''; }
    });
    codeInput?.addEventListener('blur', validateUbigeoCode);

    // --- UBIGEO CASCADING ---
    function loadStaticToCustom(id, data) {
        const list = document.getElementById('options_' + id);
        if (!list) return;
        list.innerHTML = '';
        data.forEach(item => {
            const li = document.createElement('li');
            li.className = 'px-4 py-3 select-option transition-all';
            const name = item.name || item.Name || "";
            const value = item.id || item.Id || "";
            li.textContent = name;
            li.dataset.value = value;
            li.onclick = () => window.customSelectRegistry[id].setValue(value, name);
            list.appendChild(li);
        });
    }

    // Manual load for initial data
    const initData = window.inscriptionData || {};
    
    setTimeout(() => {
        if (window.customSelectRegistry['Nationality']) {
            loadStaticToCustom('Nationality', [{ id: 'Peruano', name: 'Peruano' }, { id: 'Extranjero', name: 'Extranjero' }]);
            window.customSelectRegistry['Nationality'].setValue('Peruano', 'Peruano');
        }

        if (window.customSelectRegistry['DocumentType']) {
            loadStaticToCustom('DocumentType', [{ id: 'DNI', name: 'DNI' }, { id: 'CE', name: 'Carnet de Extranjería' }, { id: 'PASAPORTE', name: 'Pasaporte' }]);
            window.customSelectRegistry['DocumentType'].setValue('DNI', 'DNI');
        }

        applyDocNumberRules();

        applyNationalityRules(document.getElementById('Nationality')?.value || 'Peruano');

        if (window.customSelectRegistry['ModalityId'] && initData.modalities) {
            loadStaticToCustom('ModalityId', initData.modalities);
        }

        if (window.customSelectRegistry['TypePostulantId'] && initData.typePostulants) {
            loadStaticToCustom('TypePostulantId', initData.typePostulants);
        }

        if (window.customSelectRegistry['MethodPaymentId'] && initData.methodPayments) {
            loadStaticToCustom('MethodPaymentId', initData.methodPayments);
            // When method payment changes, update guide image and code format
            document.getElementById('MethodPaymentId').addEventListener('change', (e) => {
                updatePaymentGuide(e.target.value);
                updatePaymentCodeFormat(e.target.value);
            });
        }

        if (window.customSelectRegistry['CivilStatus']) {
            loadStaticToCustom('CivilStatus', [
                { id: 'Soltero', name: 'Soltero(a)' },
                { id: 'Casado', name: 'Casado(a)' },
                { id: 'Divorciado', name: 'Divorciado(a)' },
                { id: 'Viudo', name: 'Viudo(a)' }
            ]);
            window.customSelectRegistry['CivilStatus'].setValue('Soltero', 'Soltero(a)');
        }

        if (window.customSelectRegistry['Genero']) {
            loadStaticToCustom('Genero', [{ id: 'M', name: 'Masculino' }, { id: 'F', name: 'Femenino' }]);
            window.customSelectRegistry['Genero'].setValue('M', 'Masculino');
        }

        if (window.customSelectRegistry['DisabilityTypeIds'] && initData.disabilityTypes) {
            const noDisability = { id: '', name: 'NINGUNA' };
            const disabilityOptions = [noDisability, ...initData.disabilityTypes];
            loadStaticToCustom('DisabilityTypeIds', disabilityOptions);
            window.customSelectRegistry['DisabilityTypeIds'].setValue('', 'NINGUNA');
        }

        if (window.customSelectRegistry['SchoolType']) {
            loadStaticToCustom('SchoolType', [{ id: 'Público', name: 'Pública' }, { id: 'Privado', name: 'Privada' }]);
            window.customSelectRegistry['SchoolType'].setValue('Público', 'Pública');
        }

        if (window.customSelectRegistry['EducationalLevel']) {
            window.customSelectRegistry['EducationalLevel'].setOptions([
                { id: 'PRIMARIA', name: 'Primaria' },
                { id: 'SECUNDARIA', name: 'Secundaria' }
            ]);
        }

        if (window.customSelectRegistry['Grade']) {
            var initialGrades = [];
            for (var gi = 1; gi <= 6; gi++) {
                initialGrades.push({ id: gi.toString(), name: gi + '°' });
            }
            window.customSelectRegistry['Grade'].setOptions(initialGrades);
        }

        if (window.customSelectRegistry['CountryId'] && initData.countries) {
            loadStaticToCustom('CountryId', initData.countries);
            const peru = initData.countries.find(c => c.name.toUpperCase().includes("PERÚ"));
            if (peru) {
                window.customSelectRegistry['CountryId'].setValue(peru.id, peru.name);
                document.getElementById('peruUbigeoContainer')?.classList.remove('hidden');

                // Load Depts for Peru
                loadDepartments(peru.id);
            }
        }

        if (window.customSelectRegistry['SourceUniversityId'] && initData.universities) {
            loadStaticToCustom('SourceUniversityId', initData.universities);
        }
        if (window.customSelectRegistry['SourceCareerId'] && initData.careersAll) {
            loadStaticToCustom('SourceCareerId', initData.careersAll);
        }

        // --- CAREER INITIAL LOAD (first by modality if pre-selected) ---
        const initialModalityId = document.getElementById('ModalityId')?.value;
        const initialTypeModalityId = document.getElementById('TypeModalityId')?.value;
        if (initialModalityId) {
            loadCareersByModality(initialModalityId, initialTypeModalityId);
            loadTypeModalities(initialModalityId);
        } else if (window.customSelectRegistry['CareerId'] && initData.careers) {
            loadStaticToCustom('CareerId', initData.careers);
        }

        updateProfilePhotoSection();
        updateEducationSections();
    }, 500);

    // Disability select change listener
    document.getElementById('DisabilityTypeIds')?.addEventListener('change', (e) => {
        const value = e.target.value;
        const conadisWrapper = document.getElementById('conadisWrapper');
        if (conadisWrapper) {
            if (value && value !== '') {
                conadisWrapper.classList.remove('hidden');
            } else {
                conadisWrapper.classList.add('hidden');
                const conadisInput = document.querySelector('[name="ConadisNumber"]');
                if (conadisInput) conadisInput.value = '';
            }
        }
    });

    function setUbigeoContainerVisible(visible) {
        const container = document.getElementById('peruUbigeoContainer');
        if (!container) return;
        container.classList.toggle('hidden', !visible);
        if (!visible) {
            // Clear dependent selections so nothing gets submitted
            ['DepartmentId', 'ProvincieId', 'DistritId'].forEach(id => {
                if (window.customSelectRegistry[id]) window.customSelectRegistry[id].clear();
            });
        }
    }

    async function loadDepartments(countryId) {
        if (!window.customSelectRegistry['DepartmentId']) return;
        if (!countryId) {
            setUbigeoContainerVisible(false);
            return;
        }
        try {
            const response = await fetch(`/public/departments/${countryId}`);
            const data = await response.json();
            if (data && data.length > 0) {
                loadStaticToCustom('DepartmentId', data);
                setUbigeoContainerVisible(true);
            } else {
                loadStaticToCustom('DepartmentId', []);
                setUbigeoContainerVisible(false);
            }
        } catch (e) {
            setUbigeoContainerVisible(false);
        }
    }

    document.getElementById('CountryId')?.addEventListener('change', (e) => {
        const countryId = e.target.value;
        // Reset dependent selections before reloading
        ['ProvincieId', 'DistritId'].forEach(id => {
            if (window.customSelectRegistry[id]) window.customSelectRegistry[id].clear();
        });
        loadDepartments(countryId);
        applyUbigeoModeByCountry();
    });

    document.getElementById('DepartmentId')?.addEventListener('change', (e) => {
        const depId = e.target.value;
        if (window.customSelectRegistry['ProvincieId']) window.customSelectRegistry['ProvincieId'].clear();
        if (window.customSelectRegistry['DistritId']) window.customSelectRegistry['DistritId'].clear();
        if (depId) {
             fetch(`/public/provinces/${depId}`).then(r => r.json()).then(data => loadStaticToCustom('ProvincieId', data));
        }
    });

    document.getElementById('ProvincieId')?.addEventListener('change', (e) => {
        const provId = e.target.value;
        if (window.customSelectRegistry['DistritId']) window.customSelectRegistry['DistritId'].clear();
        if (provId) {
             fetch(`/public/districts/${provId}`).then(r => r.json()).then(data => loadStaticToCustom('DistritId', data));
        }
    });

    // --- SCHOOL SELECTION LOGIC ---
    const checkAbroad = document.getElementById('checkAbroad');
    const schoolUbigeoFilter = document.getElementById('schoolUbigeoFilter');
    const schoolSelectWrapper = document.getElementById('schoolSelectWrapper');
    const otherSchoolWrapper = document.getElementById('otherSchoolWrapper');
    const schoolTypeWrapper = document.getElementById('schoolTypeWrapper');
    let schoolManagementMap = {};

    function updateSchoolTypeFromSelection() {
        const schoolId = document.getElementById('SchoolId')?.value;
        const isAbroad = checkAbroad.checked;

        if (isAbroad || schoolId === 'OTHER') {
            if (schoolTypeWrapper) {
                schoolTypeWrapper.classList.remove('hidden');
            }
            return;
        }

        var management = schoolManagementMap[schoolId];
        if (window.customSelectRegistry['SchoolType']) {
            if (management) {
                var label = management === 'PRIVADO' ? 'Privada' : 'Pública';
                window.customSelectRegistry['SchoolType'].setValue(management, label);
            } else {
                window.customSelectRegistry['SchoolType'].setValue('Publico', 'Pública');
            }
        }
        if (schoolTypeWrapper) {
            schoolTypeWrapper.classList.add('hidden');
        }
    }

    function toggleSchoolFilters() {
        const isAbroad = checkAbroad.checked;
        if (isAbroad) {
            schoolUbigeoFilter.classList.add('hidden');
            schoolSelectWrapper.classList.add('hidden');
            otherSchoolWrapper.classList.remove('hidden');
            if (window.customSelectRegistry['SchoolId']) window.customSelectRegistry['SchoolId'].clear();
            schoolManagementMap = {};
        } else {
            schoolUbigeoFilter.classList.remove('hidden');
            schoolSelectWrapper.classList.remove('hidden');
            checkOtherSchoolVisibility();
        }
    }

    function checkOtherSchoolVisibility() {
        const schoolId = document.getElementById('SchoolId')?.value;
        if (!checkAbroad.checked) {
            if (schoolId === 'OTHER') {
                otherSchoolWrapper.classList.remove('hidden');
            } else {
                otherSchoolWrapper.classList.add('hidden');
            }
        }
        updateSchoolTypeFromSelection();
    }

    checkAbroad?.addEventListener('change', toggleSchoolFilters);

    document.getElementById('SchoolDepartmentId')?.addEventListener('change', (e) => {
        const depId = e.target.value;
        if (window.customSelectRegistry['SchoolProvincieId']) window.customSelectRegistry['SchoolProvincieId'].clear();
        if (window.customSelectRegistry['SchoolDistritId']) window.customSelectRegistry['SchoolDistritId'].clear();
        if (window.customSelectRegistry['SchoolId']) window.customSelectRegistry['SchoolId'].clear();
        schoolManagementMap = {};
        if (depId) {
             fetch(`/public/provinces/${depId}`).then(r => r.json()).then(data => loadStaticToCustom('SchoolProvincieId', data));
        }
    });

    document.getElementById('SchoolProvincieId')?.addEventListener('change', (e) => {
        const provId = e.target.value;
        if (window.customSelectRegistry['SchoolDistritId']) window.customSelectRegistry['SchoolDistritId'].clear();
        if (window.customSelectRegistry['SchoolId']) window.customSelectRegistry['SchoolId'].clear();
        schoolManagementMap = {};
        if (provId) {
             fetch(`/public/districts/${provId}`).then(r => r.json()).then(data => loadStaticToCustom('SchoolDistritId', data));
        }
    });

    document.getElementById('SchoolDistritId')?.addEventListener('change', (e) => {
        const distId = e.target.value;
        if (window.customSelectRegistry['SchoolId']) window.customSelectRegistry['SchoolId'].clear();
        schoolManagementMap = {};
        if (distId) {
             fetch(`/public/schools/${distId}`).then(r => r.json()).then(data => {
                 schoolManagementMap = {};
                 data.forEach(function(s) {
                     if (s.id && s.management) schoolManagementMap[s.id] = s.management;
                 });
                 const schools = [...data, { id: 'OTHER', name: 'OTRO (MI COLEGIO NO ESTÁ EN LA LISTA)' }];
                 loadStaticToCustom('SchoolId', schools);
             });
        }
    });

    document.getElementById('SchoolId')?.addEventListener('change', checkOtherSchoolVisibility);

    // Initial load for school depts
    setTimeout(() => {
        if (window.customSelectRegistry['SchoolDepartmentId'] && initData.countries) {
            const peru = initData.countries.find(c => c.name.toUpperCase().includes("PERÚ"));
            if (peru) {
                fetch(`/public/departments/${peru.id}`).then(r => r.json()).then(data => loadStaticToCustom('SchoolDepartmentId', data));
            }
        }
    }, 1000);

    // --- GUARDIAN (apoderado) — visible sólo si BirthDate => menor de 18 ---
    // Se calcula la edad exacta considerando mes/día para evitar falsos positivos
    // en cumpleaños recientes.
    const guardianSection = document.getElementById('guardianSection');
    const birthDateInput = document.querySelector('[name="BirthDate"]');

    function calcAge(yyyyMmDd) {
        if (!yyyyMmDd) return null;
        const d = new Date(yyyyMmDd);
        if (isNaN(d.getTime())) return null;
        const today = new Date();
        let age = today.getFullYear() - d.getFullYear();
        const m = today.getMonth() - d.getMonth();
        if (m < 0 || (m === 0 && today.getDate() < d.getDate())) age--;
        return age;
    }

    const GUARDIAN_FIELDS = ['GuardianName', 'GuardianFatherSurname', 'GuardianMotherSurname', 'GuardianDni', 'GuardianPhone', 'GuardianEmail'];

    function toggleGuardianSection() {
        if (!guardianSection || !birthDateInput) return;
        const age = calcAge(birthDateInput.value);
        const isMinor = age !== null && age >= 0 && age < 18;
        if (isMinor) {
            guardianSection.classList.remove('hidden');
        } else {
            guardianSection.classList.add('hidden');
            // Limpiar campos cuando ya no aplica — evita que viajen valores ocultos.
            GUARDIAN_FIELDS.forEach(n => {
                const el = document.querySelector(`[name="${n}"]`);
                if (el) el.value = '';
            });
        }
    }

    if (birthDateInput) {
        birthDateInput.addEventListener('change', toggleGuardianSection);
        birthDateInput.addEventListener('input', toggleGuardianSection);
        toggleGuardianSection();
    }

    // Sanitiza el DNI del apoderado a sólo dígitos.
    document.querySelector('[name="GuardianDni"]')?.addEventListener('input', function (e) {
        e.target.value = e.target.value.replace(/\D/g, '').slice(0, 8);
    });

    // Sanitiza el teléfono del apoderado a sólo dígitos.
    document.querySelector('[name="GuardianPhone"]')?.addEventListener('input', function (e) {
        e.target.value = e.target.value.replace(/\D/g, '').slice(0, 12);
    });

    // Sanitiza el celular del postulante a sólo dígitos, máximo 9.
    document.querySelector('[name="PhoneNumber"]')?.addEventListener('input', function (e) {
        e.target.value = e.target.value.replace(/\D/g, '').slice(0, 9);
    });

    // --- PAYMENT CODE INPUT HANDLERS ---
    // Banco: digits only, max 8, sync to hidden
    document.getElementById('PaymentCodeBancoInput')?.addEventListener('input', function (e) {
        this.value = this.value.replace(/\D/g, '').slice(0, 8);
        syncBancoCode();
    });

    // Caja part 1: digits only, max 3, auto-advance, sync to hidden
    document.getElementById('PaymentCodeCajaPart1')?.addEventListener('input', function (e) {
        this.value = this.value.replace(/\D/g, '').slice(0, 3);
        if (this.value.length === 3) {
            document.getElementById('PaymentCodeCajaPart2')?.focus();
        }
        syncCajaCode();
    });

    // Caja part 2: digits only, max 8, sync to hidden
    document.getElementById('PaymentCodeCajaPart2')?.addEventListener('input', function (e) {
        this.value = this.value.replace(/\D/g, '').slice(0, 8);
        syncCajaCode();
    });

    // --- INITIAL MODALITY SUMMARY (for FixedModality case) ---
    setTimeout(() => {
        updateModalitySummary();
    }, 600);

    // --- FORM SUBMISSION ---
    const form = document.getElementById('inscriptionForm');
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const terms = document.querySelector('[name="TermsAccepted"]');
            if (terms && !terms.checked) {
                Swal.fire({
                    title: 'Términos y Condiciones',
                    text: 'Debe aceptar la declaración jurada para continuar.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                return;
            }

            // Validación de campos condicionales (Nivel educativo / Grado) — Opcionales

            // Validación del apoderado cuando el postulante es menor de edad.
            const guardianSectionEl = document.getElementById('guardianSection');
            if (guardianSectionEl && !guardianSectionEl.classList.contains('hidden')) {
                const gName = document.querySelector('[name="GuardianName"]')?.value.trim() || '';
                const gFather = document.querySelector('[name="GuardianFatherSurname"]')?.value.trim() || '';
                const gMother = document.querySelector('[name="GuardianMotherSurname"]')?.value.trim() || '';
                const gDni = document.querySelector('[name="GuardianDni"]')?.value.trim() || '';
                const gPhone = document.querySelector('[name="GuardianPhone"]')?.value.trim() || '';
                const gEmail = document.querySelector('[name="GuardianEmail"]')?.value.trim() || '';

                const missingGuardian = [];
                if (!gName) missingGuardian.push('Nombres del apoderado');
                if (!gFather) missingGuardian.push('Apellido paterno del apoderado');
                if (!gMother) missingGuardian.push('Apellido materno del apoderado');
                if (!gDni) missingGuardian.push('DNI del apoderado');
                else if (!/^\d{8}$/.test(gDni)) missingGuardian.push('DNI del apoderado (8 dígitos)');
                if (!gPhone) missingGuardian.push('Teléfono del apoderado');
                else if (!/^\d{6,12}$/.test(gPhone)) missingGuardian.push('Teléfono del apoderado (formato inválido)');
                if (gEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(gEmail)) missingGuardian.push('Correo del apoderado (formato inválido)');

                if (missingGuardian.length > 0) {
                    Swal.fire({
                        title: 'Datos del apoderado',
                        html: `Como eres menor de edad, completa los siguientes datos:<br><br><ul class="text-left list-disc list-inside">${missingGuardian.map(m => `<li>${m}</li>`).join('')}</ul>`,
                        icon: 'warning',
                        confirmButtonColor: '#10b981'
                    });
                    return;
                }
            }

            // Validación de pago: si la sección está visible y hay monto > 0,
            // el código de operación es obligatorio junto con el voucher.
            const paySection = document.getElementById('paymentSection');
            if (paySection && paySection.style.display !== 'none') {
                const payAmount = document.querySelector('[name="PaymentAmount"]')?.value;
                const isExonerated = !payAmount || parseFloat(payAmount) <= 0;
                if (!isExonerated) {
                    const payMethod = document.getElementById('MethodPaymentId')?.value;
                    const payCode = document.getElementById('PaymentCodeHidden')?.value;
                    const payVoucherInput = document.querySelector('[name="PaymentVoucher"]');
                    const hasPayVoucher = payVoucherInput && payVoucherInput.files && payVoucherInput.files.length > 0;
                    const missingPay = [];
                    if (!payMethod) missingPay.push('Medio de Pago');
                    if (!payCode) missingPay.push('Código de Operación / Voucher');
                    if (!hasPayVoucher) missingPay.push('Foto del Comprobante');
                    if (missingPay.length > 0) {
                        Swal.fire({
                            title: 'Información de pago incompleta',
                            html: `Por favor complete los siguientes campos:<br><br><ul class="text-left list-disc list-inside">${missingPay.map(m => `<li>${m}</li>`).join('')}</ul>`,
                            icon: 'warning',
                            confirmButtonColor: '#10b981'
                        });
                        return;
                    }
                }
            }

            // Validación de Captcha (visible) antes de enviar
            if (window.captchaConfig && window.captchaConfig.enabled) {
                const tsField = form.querySelector('[name="cf-turnstile-response"]');
                const grField = form.querySelector('[name="g-recaptcha-response"]');
                const captchaValue = (tsField && tsField.value) || (grField && grField.value);
                if (!captchaValue) {
                    Swal.fire({
                        title: 'Verificación pendiente',
                        text: 'Resuelve la verificación anti-bot al final del formulario antes de continuar.',
                        icon: 'warning',
                        confirmButtonColor: '#10b981'
                    });
                    return;
                }
            }

            // Stop auto-refresh timer — we're about to submit
            stopSubmitCaptchaAutoRefresh();

            // Refresh the Turnstile token to ensure it hasn't expired while filling the form
            var captchaReady = await getSubmitCaptchaToken();
            if (!captchaReady) {
                Swal.fire({
                    title: 'Verificación expirada',
                    text: 'La verificación anti-bot expiró. Por favor espere un momento mientras se renueva, e intente de nuevo.',
                    icon: 'warning',
                    confirmButtonColor: '#10b981'
                });
                startSubmitCaptchaAutoRefresh();
                return;
            }

            Swal.fire({
                title: 'Procesando Inscripción',
                text: 'Por favor espere un momento...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const formData = new FormData(form);

            // Fix DJ: Ensure TermsAccepted is always included as boolean
            const termsChecked = document.querySelector('[name="TermsAccepted"]')?.checked;
            formData.set('TermsAccepted', termsChecked ? 'true' : 'false');

            // Fix SchoolId "OTHER" binding issue
            if (formData.get('SchoolId') === 'OTHER') {
                formData.set('SchoolId', '');
            }

            // Auto-fill SchoolType from selected school's management
            var schoolIdVal = formData.get('SchoolId');
            var schoolTypeVal = formData.get('SchoolType');
            if (schoolIdVal && (!schoolTypeVal || schoolTypeVal === '')) {
                var mgmt = schoolManagementMap[schoolIdVal];
                formData.set('SchoolType', mgmt || 'Publico');
            }

            // Prepend country code (51) to phone number
            var rawPhone = formData.get('PhoneNumber');
            if (rawPhone) {
                formData.set('PhoneNumber', rawPhone.replace(/\D/g, ''));
            }

            // Nullable Guid?/List<Guid>? fields: ASP.NET model binder rejects
            // empty strings. Remove empty values so they bind as null / empty list.
            const nullableGuidFields = [
                'CountryId', 'DepartmentId', 'ProvincieId', 'UbigeoId',
                'SchoolId', 'SchoolDepartmentId', 'SchoolProvincieId', 'SchoolDistritId',
                'SchoolUbigeoId', 'TypeModalityId', 'DisabilityTypeIds',
                'SourceUniversityId', 'SourceCareerId'
            ];
            nullableGuidFields.forEach(field => {
                const values = formData.getAll(field);
                if (values.length === 0) return;
                const hasNonEmpty = values.some(v => v !== '' && v !== null && v !== undefined);
                if (!hasNonEmpty) {
                    formData.delete(field);
                    return;
                }
                // If the field has both empty and non-empty entries (e.g. multi-select),
                // rewrite keeping only the non-empty ones.
                if (values.some(v => v === '' || v === null || v === undefined)) {
                    formData.delete(field);
                    values
                        .filter(v => v !== '' && v !== null && v !== undefined)
                        .forEach(v => formData.append(field, v));
                }
            });

            // Safety net: any remaining hidden-select with empty value is likely a
            // nullable Guid. Drop it so model binding doesn't fail.
            document.querySelectorAll('input.hidden-select').forEach(input => {
                if (!input.name) return;
                if (nullableGuidFields.includes(input.name)) return; // already handled
                const values = formData.getAll(input.name);
                if (values.length === 1 && values[0] === '') {
                    // Keep required ones so the server returns a meaningful "required"
                    // error instead of silently dropping the value.
                    if (!input.hasAttribute('required')) {
                        formData.delete(input.name);
                    }
                }
            });

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    credentials: 'same-origin',
                    body: formData
                });

                // Handle non-JSON responses (e.g., IIS 413, 502 HTML error pages)
                var result;
                var ct = response.headers.get('content-type') || '';
                if (ct.indexOf('application/json') !== -1) {
                    result = await response.json();
                } else {
                    // Non-JSON error page from IIS/proxy — construct synthetic error
                    result = {
                        success: false,
                        _httpStatus: response.status,
                        message: response.status === 413
                            ? 'El total de los archivos excede el límite permitido.'
                            : 'Error del servidor (' + response.status + ').'
                    };
                }

                if (response.ok && result.success) {
                    const downloadUrl = result.downloadUrl || null;

                    // Dispara la descarga AUTOMÁTICAMENTE al recibir el success,
                    // antes de mostrar el SweetAlert. Como el endpoint envía
                    // Content-Disposition: attachment, el navegador descarga el
                    // PDF directamente sin abrir una pestaña nueva (no hay
                    // popup-blocker que estorbe).
                    if (downloadUrl) {
                        triggerDownload(downloadUrl);
                    }

                    const html = `
                        <div class="text-left">
                            <p class="text-sm text-slate-700 mb-3">${result.message || 'Tu inscripción ha sido registrada correctamente.'}</p>
                            ${downloadUrl ? `
                            <div class="bg-emerald-50 border border-emerald-200 rounded-md px-3 py-2.5 text-xs text-emerald-800 leading-relaxed">
                                <strong class="block uppercase tracking-wide text-[10px] mb-1">
                                    <i class="ti ti-file-download mr-1"></i> Guía descargándose
                                </strong>
                                La guía de inscripción se está descargando automáticamente.
                                Si no se descargó, usa el botón <strong>Descargar nuevamente</strong>.
                                Revísala para conocer los detalles del proceso de admisión.
                            </div>` : ''}
                        </div>`;

                    Swal.fire({
                        title: '¡Inscripción Exitosa!',
                        html,
                        icon: 'success',
                        showCancelButton: !!downloadUrl,
                        confirmButtonText: 'Volver al inicio',
                        cancelButtonText: '<i class="ti ti-file-download mr-1"></i> Descargar guía',
                        confirmButtonColor: '#10b981',
                        cancelButtonColor: '#f54477',
                        allowOutsideClick: false,
                        allowEscapeKey: false,
                        reverseButtons: false
                    }).then((res) => {
                        if (res.dismiss === Swal.DismissReason.cancel && downloadUrl) {
                            // Reintento manual: el usuario clickeó "Descargar nuevamente".
                            triggerDownload(downloadUrl);
                            // No redirigir aún — dejamos el alert cerrado, el usuario
                            // puede recargar /inscription si quiere volver al inicio.
                            setTimeout(() => { window.location.href = '/'; }, 1200);
                        } else {
                            window.location.href = '/';
                        }
                    });
                } else {
                    // Caso especial: archivo rechazado por validación
                    if (result.fileName || result.fileReason) {
                        const esc = s => String(s || '').replace(/[&<>"']/g, c =>
                            ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
                        const html = `
                            <div class="text-left">
                                ${result.fileContext ? `
                                <div class="inline-flex items-center gap-2 bg-amber-50 border border-amber-200 text-amber-800 text-xs font-bold px-3 py-1 rounded-full uppercase tracking-wide mb-3">
                                    <i class="ti ti-alert-triangle"></i>
                                    ${esc(result.fileContext)}
                                </div>` : ''}
                                <p class="text-sm text-slate-700 mb-2">
                                    No se pudo procesar el archivo
                                    <strong class="text-red-700 break-all">"${esc(result.fileName)}"</strong>.
                                </p>
                                ${result.fileReason ? `
                                <div class="bg-red-50 border border-red-200 rounded-lg px-3 py-2 text-xs text-red-700 mb-3">
                                    <strong class="block text-[11px] uppercase tracking-wide mb-0.5">Motivo</strong>
                                    ${esc(result.fileReason)}
                                </div>` : ''}
                                <p class="text-xs text-slate-500 leading-relaxed">
                                    Revisa que el archivo sea el correcto, que tenga la extensión permitida y que no esté dañado.
                                    Si el archivo proviene de otro sistema, intenta volver a exportarlo o abrirlo y guardarlo de nuevo como PDF.
                                </p>
                            </div>`;

                        Swal.fire({
                            title: 'Archivo no válido',
                            html,
                            icon: 'error',
                            confirmButtonText: 'Entendido',
                            confirmButtonColor: '#ef4444',
                            width: 520
                        });
                        return;
                    }

                    // Error genérico
                    let errorMessage = result.message || 'Hubo un problema al procesar su solicitud.';
                    if (result.errors && result.errors.length > 0) {
                        errorMessage += '<br><br><ul class="text-left text-xs list-disc pl-5">' +
                                       result.errors.map(e => `<li>${e}</li>`).join('') +
                                       '</ul>';
                    }

                    // If captcha failed, refresh the widget and restart auto-refresh
                    if (result.captchaError) {
                        resetSubmitCaptcha();
                        startSubmitCaptchaAutoRefresh();
                    }

                    Swal.fire({
                        title: 'Error en el Registro',
                        html: errorMessage,
                        icon: 'error',
                        confirmButtonColor: '#ef4444'
                    });
                }
            } catch (error) {
                // Restart auto-refresh so the user can retry
                resetSubmitCaptcha();
                startSubmitCaptchaAutoRefresh();
                Swal.fire({
                    title: 'Error de Conexión',
                    text: 'No se pudo contactar con el servidor. Intente nuevamente.',
                    icon: 'error',
                    confirmButtonColor: '#ef4444'
                });
            }
        });
    }
});
