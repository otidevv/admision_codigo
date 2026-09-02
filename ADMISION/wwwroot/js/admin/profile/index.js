document.querySelectorAll('.toggle-pw').forEach(btn => {
    btn.addEventListener('click', () => {
        const input = btn.parentElement.querySelector('input');
        if (!input) return;
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        btn.querySelector('i').className = show ? 'ti ti-eye-off text-xs' : 'ti ti-eye text-xs';
    });
});

const newPw = document.getElementById('NewPassword');
const confirmPw = document.getElementById('ConfirmPassword');
const bar = document.getElementById('pwStrengthBar');
const label = document.getElementById('pwStrengthLabel');
const matchLabel = document.getElementById('pwMatchLabel');

function strength(pw) {
    if (!pw) return { width: '0%', color: '#f43f5e', text: '—' };
    let s = 0;
    if (pw.length >= 8) s++;
    if (pw.length >= 12) s++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) s++;
    if (/\d/.test(pw)) s++;
    if (/[^A-Za-z0-9]/.test(pw)) s++;
    const presets = [
        { w: '20%', c: '#f43f5e', t: 'Muy débil' },
        { w: '40%', c: '#f59e0b', t: 'Débil' },
        { w: '60%', c: '#eab308', t: 'Aceptable' },
        { w: '80%', c: '#84cc16', t: 'Fuerte' },
        { w: '100%', c: '#10b981', t: 'Muy fuerte' },
    ];
    const p = presets[Math.min(s - 1, 4)] || presets[0];
    return { width: p.w, color: p.c, text: p.t };
}

newPw?.addEventListener('input', () => {
    const r = strength(newPw.value);
    bar.style.width = r.width;
    bar.style.background = r.color;
    label.textContent = 'Seguridad: ' + r.text;
    checkMatch();
});
confirmPw?.addEventListener('input', checkMatch);

function checkMatch() {
    if (!confirmPw.value) {
        matchLabel.textContent = '—';
        matchLabel.className = 'text-[10.5px] font-semibold text-ink-400 mt-1.5';
        return;
    }
    if (newPw.value === confirmPw.value) {
        matchLabel.textContent = '✓ Las contraseñas coinciden';
        matchLabel.className = 'text-[10.5px] font-semibold text-emerald-600 mt-1.5';
    } else {
        matchLabel.textContent = '✗ Las contraseñas no coinciden';
        matchLabel.className = 'text-[10.5px] font-semibold text-rose-500 mt-1.5';
    }
}
