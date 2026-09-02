(function () {
    const initialData = window.AdminUsersData || [];
    let availableRoles = [];

    $(document).ready(function () {
        DT.load('usersTable', { data: initialData });

        $('#filterText').on('input', function () {
            const val = $(this).val().toLowerCase();
            const filtered = initialData.filter(u =>
                (u.fullName || '').toLowerCase().includes(val) ||
                (u.document || '').toLowerCase().includes(val) ||
                (u.userName || '').toLowerCase().includes(val)
            );
            DT.load('usersTable', { data: filtered });
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'edit') openUserModal(row.id);
            else if (key === 'delete') deleteUser(row.id, row.userName);
            else if (key === 'reset-pass') resetUserPassword(row.id, row.userName, row.email, row.isDisabled);
        });

        document.getElementById('bulkResetPassBtn').addEventListener('click', bulkResetPasswords);

        // Validar nombre de usuario al perder foco (solo en modo creación)
        document.getElementById('userName').addEventListener('blur', async function () {
            const userId = document.getElementById('userId').value;
            if (userId) return;

            const username = this.value.trim();
            if (!username) return;

            try {
                const response = await fetch(`/admin/usuarios/check-username?username=${encodeURIComponent(username)}`);
                if (response.ok) {
                    const result = await response.json();
                    if (result.taken) {
                        this.classList.add('ring-rose-500', 'ring-2');
                        const helper = this.closest('.form-field')?.querySelector('.form-helper');
                        if (helper) {
                            helper.textContent = 'El nombre de usuario ya existe. Ingrese otro.';
                            helper.classList.add('text-rose-600');
                        }
                    } else {
                        this.classList.remove('ring-rose-500', 'ring-2');
                        const helper = this.closest('.form-field')?.querySelector('.form-helper');
                        if (helper) {
                            helper.textContent = '';
                            helper.classList.remove('text-rose-600');
                        }
                    }
                }
            } catch (e) {
                // silencioso
            }
        });

        // Lookup por documento al perder foco (solo en modo creación)
        document.getElementById('document').addEventListener('blur', async function () {
            const userId = document.getElementById('userId').value;
            if (userId) return; // solo en nuevo usuario

            const doc = this.value.trim();
            if (!doc) return;

            try {
                const response = await fetch(`/admin/usuarios/lookup-by-document/${encodeURIComponent(doc)}`);
                if (response.ok) {
                    const user = await response.json();
                    document.getElementById('name').value = user.name || '';
                    document.getElementById('firstNameFather').value = user.firstNameFather || '';
                    document.getElementById('firstNameMother').value = user.firstNameMother || '';
                    document.getElementById('documentType').value = user.documentType || 'DNI';
                    document.getElementById('email').value = user.email || '';
                    document.getElementById('phoneNumber').value = user.phoneNumber || '';
                    document.getElementById('genero').value = user.genero || '';
                    document.getElementById('civilStatus').value = user.civilStatus || '';
                    document.getElementById('address').value = user.address || '';
                    if (user.birthdate) setBirthdate(user.birthdate.split('T')[0]);

                    Swal.fire({
                        title: 'Usuario existente',
                        text: 'Se encontró un usuario con ese documento. Se autocompletaron los datos.',
                        icon: 'info',
                        confirmButtonColor: '#f54477',
                        timer: 3000,
                        timerProgressBar: true
                    });
                }
            } catch (e) {
                // silencioso
            }
        });
    });

    async function loadRoles() {
        if (availableRoles.length === 0) {
            const response = await fetch('/admin/usuarios/all-roles');
            availableRoles = await response.json();
        }
        renderRoles();
    }

    function renderRoles(selectedIds = []) {
        const container = document.getElementById('rolesList');
        // Mismas clases que _FormCheckbox.cshtml para mantener consistencia visual.
        container.innerHTML = availableRoles.map(role => `
            <label class="form-check form-check--card cursor-pointer">
                <input type="checkbox" name="SelectedRoleIds" value="${role.id}"
                       ${selectedIds.includes(role.id) ? 'checked' : ''}
                       class="role-checkbox" />
                <span class="leading-tight">
                    <span class="form-check__text">${role.name}</span>
                </span>
            </label>
        `).join('');
    }

    // Helpers para el input de fecha (Flatpickr) y el helper text de password.
    function setBirthdate(value) {
        const fp = window.ADM?.FlatpickrRegistry?.['birthdate'];
        if (fp) {
            fp.setDate(value || null, true);
        } else {
            const input = document.getElementById('birthdate');
            if (input) input.value = value || '';
        }
    }

    function setPasswordHelper(text) {
        const field = document.getElementById('password')?.closest('.form-field');
        const helper = field?.querySelector('.form-helper');
        if (helper) helper.textContent = text || '';
    }

    async function openUserModal(userId = null) {
        const form = document.getElementById('userForm');
        const title = document.querySelector('#userModal .adm-modal__title');

        form.reset();
        setBirthdate(null);
        document.getElementById('userId').value = userId || '';
        await loadRoles();

        if (userId) {
            if (title) title.textContent = 'Editar usuario';
            setPasswordHelper('Dejar en blanco para mantener la actual.');
            try {
                const response = await fetch(`/admin/usuarios/get-user/${userId}`);
                if (response.ok) {
                    const user = await response.json();
                    document.getElementById('name').value = user.name || '';
                    document.getElementById('firstNameFather').value = user.firstNameFather || '';
                    document.getElementById('firstNameMother').value = user.firstNameMother || '';
                    document.getElementById('userName').value = user.userName || '';
                    document.getElementById('documentType').value = user.documentType || 'DNI';
                    document.getElementById('document').value = user.document || '';
                    document.getElementById('email').value = user.email || '';
                    document.getElementById('phoneNumber').value = user.phoneNumber || '';
                    document.getElementById('genero').value = user.genero || '';
                    document.getElementById('civilStatus').value = user.civilStatus || '';
                    document.getElementById('address').value = user.address || '';
                    if (user.birthdate) setBirthdate(user.birthdate.split('T')[0]);
                    renderRoles(user.selectedRoleIds || []);
                    document.getElementById('disableUser').checked = !!user.isDisabled;
                }
            } catch (e) {
                Swal.fire('Error', 'No se pudieron cargar los datos del usuario', 'error');
            }
        } else {
            if (title) title.textContent = 'Nuevo usuario';
            setPasswordHelper('Mínimo 6 caracteres.');
            renderRoles();
        }

        window.ADM?.Modal?.open('userModal');
    }

    function closeUserModal() {
        window.ADM?.Modal?.close('userModal');
    }

    document.getElementById('userForm').addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData(this);
        const data = {
            Id: formData.get('Id') || null,
            Name: formData.get('Name'),
            FirstNameFather: formData.get('FirstNameFather'),
            FirstNameMother: formData.get('FirstNameMother'),
            UserName: formData.get('UserName'),
            Email: formData.get('Email'),
            PhoneNumber: formData.get('PhoneNumber'),
            Genero: formData.get('Genero'),
            CivilStatus: formData.get('CivilStatus'),
            DocumentType: formData.get('DocumentType'),
            Document: formData.get('Document'),
            Address: formData.get('Address'),
            Birthdate: formData.get('Birthdate') ? formData.get('Birthdate') : null,
            Password: formData.get('Password'),
            ConfirmPassword: formData.get('ConfirmPassword'),
            SelectedRoleIds: Array.from(document.querySelectorAll('.role-checkbox:checked')).map(cb => cb.value),
            IsDisabled: document.getElementById('disableUser').checked
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            const response = await fetch('/admin/usuarios/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(data)
            });

            if (response.ok) {
                Swal.fire({
                    title: 'Éxito',
                    text: 'Usuario guardado correctamente',
                    icon: 'success',
                    confirmButtonColor: '#f54477'
                }).then(() => location.reload());
            } else {
                const result = await response.json();
                Swal.fire({
                    title: 'Error',
                    html: result.errors ? result.errors.join('<br>') : 'Error al guardar usuario',
                    icon: 'error',
                    confirmButtonColor: '#f54477'
                });
            }
        } catch (e) {
            Swal.fire({
                title: 'Error',
                text: 'Ocurrió un error inesperado',
                icon: 'error',
                confirmButtonColor: '#f54477'
            });
        }
    });

    async function toggleBlock(userId, block) {
        let reason = "";
        if (block) {
            const { value: text } = await Swal.fire({
                title: 'Bloquear usuario',
                input: 'textarea',
                inputLabel: 'Motivo del bloqueo',
                inputPlaceholder: 'Escriba el motivo aquí…',
                showCancelButton: true,
                confirmButtonColor: '#f54477',
                cancelButtonColor: '#6b7280',
                cancelButtonText: 'Cancelar',
                confirmButtonText: 'Confirmar bloqueo',
                reverseButtons: true
            });

            if (text === undefined) return;
            reason = text;
        } else {
            const result = await Swal.fire({
                title: '¿Desbloquear usuario?',
                text: 'El usuario recuperará su acceso al sistema.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#f54477',
                cancelButtonColor: '#6b7280',
                confirmButtonText: 'Sí, desbloquear',
                cancelButtonText: 'Cancelar',
                reverseButtons: true
            });
            if (!result.isConfirmed) return;
        }

        try {
            const response = await fetch(`/admin/usuarios/toggle-block/${userId}?reason=${encodeURIComponent(reason)}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
            });

            if (response.ok) {
                Swal.fire({
                    title: 'Éxito',
                    text: block ? 'Usuario bloqueado correctamente' : 'Usuario desbloqueado correctamente',
                    icon: 'success',
                    confirmButtonColor: '#f54477'
                }).then(() => location.reload());
            } else {
                Swal.fire({
                    title: 'Error',
                    text: 'No se pudo completar la acción',
                    icon: 'error',
                    confirmButtonColor: '#f54477'
                });
            }
        } catch (e) {
            Swal.fire({
                title: 'Error',
                text: 'Ocurrió un error inesperado',
                icon: 'error',
                confirmButtonColor: '#f54477'
            });
        }
    }

    async function deleteUser(userId, userName) {
        const result = await Swal.fire({
            title: '¿Eliminar usuario?',
            html: `Estás a punto de eliminar al usuario <strong>"${userName}"</strong>. Esta acción no se puede deshacer.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });

        if (result.isConfirmed) {
            try {
                const response = await fetch(`/admin/usuarios/delete/${userId}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
                });

                if (response.ok) {
                    Swal.fire({
                        title: 'Eliminado',
                        text: 'El usuario ha sido eliminado correctamente.',
                        icon: 'success',
                        confirmButtonColor: '#f54477'
                    }).then(() => location.reload());
                } else {
                    const error = await response.text();
                    Swal.fire({
                        title: 'Error',
                        text: error || 'No se pudo eliminar el usuario.',
                        icon: 'error',
                        confirmButtonColor: '#f54477'
                    });
                }
            } catch (e) {
                Swal.fire({
                    title: 'Error',
                    text: 'Ocurrió un error inesperado.',
                    icon: 'error',
                    confirmButtonColor: '#f54477'
                });
            }
        }
    }

    window.openUserModal = openUserModal;
    window.closeUserModal = closeUserModal;
    window.toggleBlock = toggleBlock;
    window.deleteUser = deleteUser;

    function resetToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    async function resetUserPassword(userId, userName, email, isDisabled) {
        if (isDisabled && isDisabled !== 'Activo') {
            Swal.fire({
                title: 'No disponible',
                text: `El usuario "${userName}" está ${isDisabled}. No se pueden enviar credenciales.`,
                icon: 'warning',
                confirmButtonColor: '#f54477'
            });
            return;
        }
        if (!email) {
            Swal.fire({
                title: 'Sin correo',
                text: `El usuario "${userName}" no tiene un correo electrónico registrado.`,
                icon: 'warning',
                confirmButtonColor: '#f54477'
            });
            return;
        }

        const result = await Swal.fire({
            title: '¿Restablecer contraseña?',
            html: `Se generará una nueva contraseña para <strong>${userName}</strong> y se enviará a <strong>${email}</strong>.`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, restablecer',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });
        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/admin/usuarios/reset-password/${userId}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': resetToken() }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.emailSent) {
                    Swal.fire({
                        title: 'Éxito',
                        text: `Credenciales enviadas a ${data.userName}.`,
                        icon: 'success',
                        confirmButtonColor: '#f54477'
                    });
                } else {
                    Swal.fire({
                        title: 'Correo no enviado',
                        html: `Se restableció la contraseña de <strong>${data.userName}</strong>.<br><br>Contraseña temporal: <strong class="font-mono">${data.tempPassword || ''}</strong><br><span class="text-xs text-ink-400">${data.emailError || 'No se pudo enviar el correo.'}</span>`,
                        icon: 'warning',
                        confirmButtonColor: '#f54477'
                    });
                }
            } else {
                const err = await response.json();
                Swal.fire({
                    title: 'Error',
                    text: err.error || 'No se pudo restablecer la contraseña.',
                    icon: 'error',
                    confirmButtonColor: '#f54477'
                });
            }
        } catch (e) {
            Swal.fire({
                title: 'Error',
                text: 'Ocurrió un error inesperado.',
                icon: 'error',
                confirmButtonColor: '#f54477'
            });
        }
    }

    async function bulkResetPasswords() {
        let candidates = [];
        try {
            const response = await fetch('/admin/usuarios/password-reset-candidates');
            if (!response.ok) throw new Error();
            candidates = await response.json();
        } catch (e) {
            Swal.fire({
                title: 'Error',
                text: 'No se pudieron consultar los usuarios elegibles.',
                icon: 'error',
                confirmButtonColor: '#f54477'
            });
            return;
        }

        if (!candidates.length) {
            Swal.fire({
                title: 'Sin destinatarios',
                text: 'No hay usuarios administrativos activos con correo registrado.',
                icon: 'info',
                confirmButtonColor: '#f54477'
            });
            return;
        }

        const preview = candidates.slice(0, 5).map(c => c.userName).join(', ') + (candidates.length > 5 ? ', …' : '');
        const result = await Swal.fire({
            title: '¿Enviar credenciales?',
            html: `Se restablecerá la contraseña y se enviará el correo a <strong>${candidates.length}</strong> usuario(s).<br><span class="text-xs text-ink-400">${preview}</span>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, enviar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });
        if (!result.isConfirmed) return;

        Swal.fire({
            title: 'Enviando…',
            text: 'Restableciendo contraseñas y enviando correos.',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        try {
            const response = await fetch('/admin/usuarios/reset-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': resetToken()
                },
                body: JSON.stringify({ userIds: candidates.map(c => c.userId) })
            });
            const data = await response.json();

            if (response.ok) {
                let html = `<strong>${data.sent}</strong> correo(s) enviado(s) correctamente.`;
                if (data.failed && data.failed.length) {
                    html += `<div class="text-xs text-left mt-3 max-h-40 overflow-y-auto text-ink-500">${data.failed.map(f => `• ${f}`).join('<br>')}</div>`;
                }
                Swal.fire({
                    title: 'Completado',
                    html,
                    icon: data.failed && data.failed.length ? 'warning' : 'success',
                    confirmButtonColor: '#f54477'
                });
            } else {
                Swal.fire({
                    title: 'Error',
                    text: data.error || 'No se pudo completar el envío.',
                    icon: 'error',
                    confirmButtonColor: '#f54477'
                });
            }
        } catch (e) {
            Swal.fire({
                title: 'Error',
                text: 'Ocurrió un error inesperado.',
                icon: 'error',
                confirmButtonColor: '#f54477'
            });
        }
    }
})();
