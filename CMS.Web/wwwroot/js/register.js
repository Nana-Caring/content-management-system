// Register page functionality
document.addEventListener('DOMContentLoaded', function() {
    const registerForm = document.getElementById('registerForm');
    const registerBtn = document.getElementById('registerBtn');
    const spinner = registerBtn?.querySelector('.spinner-border');

    if (registerForm) {
        registerForm.addEventListener('submit', function(e) {
            // Show loading state
            if (registerBtn && spinner) {
                registerBtn.disabled = true;
                spinner.classList.remove('d-none');
                registerBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Registering...';
            }

            // Validate required fields
            const requiredFields = registerForm.querySelectorAll('[required]');
            let isValid = true;

            requiredFields.forEach(field => {
                if (!field.value.trim()) {
                    field.classList.add('is-invalid');
                    isValid = false;
                } else {
                    field.classList.remove('is-invalid');
                }
            });

            // Validate email format
            const emailField = document.getElementById('email');
            if (emailField && emailField.value) {
                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                if (!emailRegex.test(emailField.value)) {
                    emailField.classList.add('is-invalid');
                    isValid = false;
                } else {
                    emailField.classList.remove('is-invalid');
                }
            }

            // Validate phone format
            const phoneField = document.getElementById('phone');
            if (phoneField && phoneField.value) {
                const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
                if (!phoneRegex.test(phoneField.value.replace(/[\s\-\(\)]/g, ''))) {
                    phoneField.classList.add('is-invalid');
                    isValid = false;
                } else {
                    phoneField.classList.remove('is-invalid');
                }
            }

            if (!isValid) {
                e.preventDefault();
                // Reset button state
                if (registerBtn && spinner) {
                    registerBtn.disabled = false;
                    spinner.classList.add('d-none');
                    registerBtn.innerHTML = 'Register User';
                }
                return false;
            }
        });

        // Real-time validation for required fields
        const inputs = registerForm.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                if (this.hasAttribute('required') && !this.value.trim()) {
                    this.classList.add('is-invalid');
                } else {
                    this.classList.remove('is-invalid');
                }
            });

            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid') && this.value.trim()) {
                    this.classList.remove('is-invalid');
                }
            });
        });
    }

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            if (alert.querySelector('.btn-close')) {
                alert.querySelector('.btn-close').click();
            }
        }, 5000);
    });
});