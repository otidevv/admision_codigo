function toastSuccess(message) {
    Toastify({
        text: message,
        duration: 3000,
        gravity: "top",
        position: "right",
        close: true,
        style: {
            background: "linear-gradient(to right, #16a34a, #22c55e)"
        }
    }).showToast();
}

function toastError(message) {
    Toastify({
        text: message,
        duration: 4000,
        gravity: "top",
        position: "right",
        close: true,
        style: {
            background: "linear-gradient(to right, #dc2626, #ef4444)"
        }
    }).showToast();
}

function toastWarning(message) {
    Toastify({
        text: message,
        duration: 4000,
        gravity: "top",
        position: "right",
        close: true,
        style: {
            background: "linear-gradient(to right, #f59e0b, #fbbf24)"
        }
    }).showToast();
}

function toastInfo(message) {
    Toastify({
        text: message,
        duration: 3000,
        gravity: "top",
        position: "right",
        close: true,
        style: {
            background: "linear-gradient(to right, #2563eb, #3b82f6)"
        }
    }).showToast();
}