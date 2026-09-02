(function () {
    const initialData = window.AdminTeachersData || [];

    $(document).ready(function () {
        DT.load('teachersTable', { data: initialData });

        $('#filterText').on('input', function () {
            const val = $(this).val().toLowerCase();
            const filtered = initialData.filter(t =>
                (t.fullName || '').toLowerCase().includes(val) ||
                (t.document || '').toLowerCase().includes(val) ||
                (t.specialization || '').toLowerCase().includes(val)
            );
            DT.load('teachersTable', { data: filtered });
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'edit') openTeacherModal(row.id);
            else if (key === 'toggle') toggleActive(row.id, row.statusText === 'Activo');
            else if (key === 'delete') deleteTeacher(row.id, row.fullName);
        });
    });

    async function openTeacherModal(teacherId = null) {
        const form = document.getElementById('teacherForm');
        const title = document.querySelector('#teacherModal .adm-modal__title');

        form.reset();
        document.getElementById('teacherId').value = '';
        document.getElementById('teacherUserId').value = '';
        document.getElementById('isActive').checked = true;

        if (teacherId) {
            if (title) title.textContent = 'Editar docente';
            try {
                const response = await fetch(`/admin/docentes/get-teacher/${teacherId}`);
                if (response.ok) {
                    const t = await response.json();
                    document.getElementById('teacherId').value = t.id || '';
                    document.getElementById('teacherUserId').value = t.userId || '';
                    document.getElementById('name').value = t.name || '';
                    document.getElementById('firstNameFather').value = t.firstNameFather || '';
                    document.getElementById('firstNameMother').value = t.firstNameMother || '';
                    document.getElementById('documentType').value = t.documentType || 'DNI';
                    document.getElementById('document').value = t.document || '';
                    document.getElementById('email').value = t.email || '';
                    document.getElementById('phoneNumber').value = t.phoneNumber || '';
                    document.getElementById('genero').value = t.genero || '';
                    document.getElementById('address').value = t.address || '';
                    document.getElementById('specialization').value = t.specialization || '';
                    document.getElementById('degree').value = t.degree || '';
                    document.getElementById('type').value = t.type || '';
                    document.getElementById('isActive').checked = t.isActive;
                    if (t.birthdate) {
                        const fp = window.ADM?.FlatpickrRegistry?.['birthdate'];
                        if (fp) fp.setDate(t.birthdate.split('T')[0], true);
                        else document.getElementById('birthdate').value = t.birthdate.split('T')[0];
                    }
                }
            } catch (e) {
                Swal.fire('Error', 'No se pudieron cargar los datos del docente', 'error');
            }
        } else {
            if (title) title.textContent = 'Nuevo docente';
        }

        window.ADM?.Modal?.open('teacherModal');
    }

    document.getElementById('teacherForm').addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData(this);
        const data = {
            Id: formData.get('Id') || null,
            UserId: formData.get('UserId') || null,
            Name: formData.get('Name'),
            FirstNameFather: formData.get('FirstNameFather'),
            FirstNameMother: formData.get('FirstNameMother'),
            DocumentType: formData.get('DocumentType'),
            Document: formData.get('Document'),
            Email: formData.get('Email'),
            PhoneNumber: formData.get('PhoneNumber'),
            Genero: formData.get('Genero'),
            Address: formData.get('Address'),
            Birthdate: formData.get('Birthdate') ? formData.get('Birthdate') : null,
            Specialization: formData.get('Specialization'),
            Degree: formData.get('Degree'),
            Type: formData.get('Type'),
            IsActive: document.getElementById('isActive').checked
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            const response = await fetch('/admin/docentes/save', {
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
                    text: 'Docente guardado correctamente',
                    icon: 'success',
                    confirmButtonColor: '#f54477'
                }).then(() => location.reload());
            } else {
                const result = await response.json();
                Swal.fire({
                    title: 'Error',
                    html: result.errors ? result.errors.join('<br>') : 'Error al guardar docente',
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

    async function toggleActive(teacherId, currentlyActive) {
        const action = currentlyActive ? 'inhabilitar' : 'habilitar';
        const result = await Swal.fire({
            title: `¿${currentlyActive ? 'Inhabilitar' : 'Habilitar'} docente?`,
            text: `El docente será ${currentlyActive ? 'inhabilitado' : 'habilitado'} y ${currentlyActive ? 'no podrá' : 'podrá'} ser asignado a exámenes.`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: `Sí, ${action}`,
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });

        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/admin/docentes/toggle-active/${teacherId}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
            });

            if (response.ok) {
                Swal.fire({
                    title: 'Éxito',
                    text: `Docente ${action}do correctamente`,
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

    async function deleteTeacher(teacherId, teacherName) {
        const result = await Swal.fire({
            title: '¿Eliminar docente?',
            html: `Estás a punto de eliminar al docente <strong>"${teacherName}"</strong>. Esta acción no se puede deshacer.`,
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
                const response = await fetch(`/admin/docentes/delete/${teacherId}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
                });

                if (response.ok) {
                    Swal.fire({
                        title: 'Eliminado',
                        text: 'El docente ha sido eliminado correctamente.',
                        icon: 'success',
                        confirmButtonColor: '#f54477'
                    }).then(() => location.reload());
                } else {
                    const error = await response.text();
                    Swal.fire({
                        title: 'Error',
                        text: error || 'No se pudo eliminar el docente.',
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

    window.openTeacherModal = openTeacherModal;
})();
