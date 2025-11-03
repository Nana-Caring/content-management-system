// Modal System - Comprehensive popup modals for user actions
class ModalSystem {
    constructor() {
        this.activeModals = new Set();
        this.init();
    }

    init() {
        // Create modal container if it doesn't exist
        if (!document.getElementById('modal-root')) {
            const modalRoot = document.createElement('div');
            modalRoot.id = 'modal-root';
            document.body.appendChild(modalRoot);
        }

        // Handle escape key to close modals
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeTopModal();
            }
        });
    }

    // Show success message
    showSuccess(title, message, options = {}) {
        return this.createModal({
            type: 'success',
            title: title || 'Success',
            message: message,
            showCancel: false,
            confirmText: options.confirmText || 'OK',
            onConfirm: options.onConfirm,
            autoClose: options.autoClose || 3000
        });
    }

    // Show error message
    showError(title, message, options = {}) {
        return this.createModal({
            type: 'error',
            title: title || 'Error',
            message: message,
            showCancel: false,
            confirmText: options.confirmText || 'OK',
            onConfirm: options.onConfirm
        });
    }

    // Show warning message
    showWarning(title, message, options = {}) {
        return this.createModal({
            type: 'warning',
            title: title || 'Warning',
            message: message,
            showCancel: false,
            confirmText: options.confirmText || 'OK',
            onConfirm: options.onConfirm
        });
    }

    // Show info message
    showInfo(title, message, options = {}) {
        return this.createModal({
            type: 'info',
            title: title || 'Information',
            message: message,
            showCancel: false,
            confirmText: options.confirmText || 'OK',
            onConfirm: options.onConfirm
        });
    }

    // Show confirmation dialog
    showConfirm(title, message, options = {}) {
        return this.createModal({
            type: 'confirm',
            title: title || 'Confirm Action',
            message: message,
            showCancel: true,
            confirmText: options.confirmText || 'Confirm',
            cancelText: options.cancelText || 'Cancel',
            onConfirm: options.onConfirm,
            onCancel: options.onCancel,
            confirmClass: options.confirmClass || 'btn-modal-primary'
        });
    }

    // Show loading modal
    showLoading(title, message) {
        const modal = this.createModal({
            type: 'info',
            title: title || 'Please Wait',
            message: `<div class="d-flex align-items-center gap-2">
                        <div class="modal-loading"></div>
                        <span>${message || 'Processing your request...'}</span>
                      </div>`,
            showButtons: false,
            closable: false
        });

        // Return object with close method
        return {
            close: () => this.closeModal(modal.id),
            updateMessage: (newMessage) => {
                const messageEl = modal.element.querySelector('.modal-body');
                if (messageEl) {
                    messageEl.innerHTML = `<div class="d-flex align-items-center gap-2">
                                            <div class="modal-loading"></div>
                                            <span>${newMessage}</span>
                                          </div>`;
                }
            }
        };
    }

    // Create modal element
    createModal(config) {
        const modalId = 'modal-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
        
        const modalHTML = `
            <div class="modal-overlay" id="${modalId}">
                <div class="modal-container modal-${config.type}">
                    <div class="modal-header">
                        <h5 class="modal-title">${config.title}</h5>
                        ${config.closable !== false ? '<button type="button" class="modal-close" data-dismiss="modal">&times;</button>' : ''}
                    </div>
                    <div class="modal-body">
                        ${config.message}
                    </div>
                    ${config.showButtons !== false ? `
                    <div class="modal-footer">
                        ${config.showCancel ? `<button type="button" class="btn-modal btn-modal-light" data-action="cancel">${config.cancelText || 'Cancel'}</button>` : ''}
                        ${config.confirmText ? `<button type="button" class="btn-modal ${config.confirmClass || 'btn-modal-primary'}" data-action="confirm">${config.confirmText}</button>` : ''}
                    </div>
                    ` : ''}
                </div>
            </div>
        `;

        const modalRoot = document.getElementById('modal-root');
        modalRoot.insertAdjacentHTML('beforeend', modalHTML);
        
        const modalElement = document.getElementById(modalId);
        
        // Add event listeners
        this.attachEventListeners(modalElement, config);
        
        // Show modal with animation
        setTimeout(() => {
            modalElement.classList.add('show');
        }, 10);

        // Auto close if specified
        if (config.autoClose) {
            setTimeout(() => {
                this.closeModal(modalId);
            }, config.autoClose);
        }

        // Track active modal
        const modalObj = { id: modalId, element: modalElement, config: config };
        this.activeModals.add(modalObj);

        return modalObj;
    }

    // Attach event listeners to modal
    attachEventListeners(modalElement, config) {
        // Close button
        const closeBtn = modalElement.querySelector('.modal-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                this.closeModal(modalElement.id);
            });
        }

        // Action buttons
        const confirmBtn = modalElement.querySelector('[data-action="confirm"]');
        const cancelBtn = modalElement.querySelector('[data-action="cancel"]');

        if (confirmBtn) {
            confirmBtn.addEventListener('click', async () => {
                if (config.onConfirm) {
                    // Show loading state
                    const originalText = confirmBtn.textContent;
                    confirmBtn.disabled = true;
                    confirmBtn.innerHTML = '<div class="modal-loading"></div> ' + originalText;

                    try {
                        const result = await config.onConfirm();
                        if (result !== false) { // Don't close if onConfirm returns false
                            this.closeModal(modalElement.id);
                        }
                    } catch (error) {
                        console.error('Modal confirm action error:', error);
                        this.showError('Error', 'An error occurred while processing your request.');
                    } finally {
                        confirmBtn.disabled = false;
                        confirmBtn.textContent = originalText;
                    }
                } else {
                    this.closeModal(modalElement.id);
                }
            });
        }

        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                if (config.onCancel) {
                    config.onCancel();
                }
                this.closeModal(modalElement.id);
            });
        }

        // Click outside to close (optional)
        modalElement.addEventListener('click', (e) => {
            if (e.target === modalElement && config.closable !== false) {
                this.closeModal(modalElement.id);
            }
        });
    }

    // Close specific modal
    closeModal(modalId) {
        const modalElement = document.getElementById(modalId);
        if (!modalElement) return;

        modalElement.classList.remove('show');
        
        setTimeout(() => {
            modalElement.remove();
            // Remove from active modals
            this.activeModals.forEach(modal => {
                if (modal.id === modalId) {
                    this.activeModals.delete(modal);
                }
            });
        }, 300);
    }

    // Close the topmost modal
    closeTopModal() {
        if (this.activeModals.size > 0) {
            const modalsArray = Array.from(this.activeModals);
            const topModal = modalsArray[modalsArray.length - 1];
            if (topModal.config.closable !== false) {
                this.closeModal(topModal.id);
            }
        }
    }

    // Close all modals
    closeAllModals() {
        this.activeModals.forEach(modal => {
            this.closeModal(modal.id);
        });
    }

    // Utility methods for common actions

    // Confirm delete action
    confirmDelete(itemName, onConfirm) {
        return this.showConfirm(
            'Confirm Delete', 
            `Are you sure you want to delete "${itemName}"? This action cannot be undone.`,
            {
                confirmText: 'Delete',
                confirmClass: 'btn-modal-danger',
                onConfirm: onConfirm
            }
        );
    }

    // Confirm save action
    confirmSave(onConfirm) {
        return this.showConfirm(
            'Save Changes', 
            'Are you sure you want to save these changes?',
            {
                confirmText: 'Save',
                confirmClass: 'btn-modal-success',
                onConfirm: onConfirm
            }
        );
    }

    // Show operation result
    showOperationResult(success, successMessage, errorMessage) {
        if (success) {
            this.showSuccess('Success', successMessage);
        } else {
            this.showError('Error', errorMessage);
        }
    }
}

// Create global instance
window.Modal = new ModalSystem();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModalSystem;
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Initialize any existing modal triggers
    document.addEventListener('click', (e) => {
        // Handle data-modal attributes for quick modal triggering
        if (e.target.hasAttribute('data-modal')) {
            e.preventDefault();
            const modalType = e.target.getAttribute('data-modal');
            const title = e.target.getAttribute('data-modal-title') || '';
            const message = e.target.getAttribute('data-modal-message') || '';
            
            switch (modalType) {
                case 'success':
                    Modal.showSuccess(title, message);
                    break;
                case 'error':
                    Modal.showError(title, message);
                    break;
                case 'warning':
                    Modal.showWarning(title, message);
                    break;
                case 'info':
                    Modal.showInfo(title, message);
                    break;
                case 'confirm':
                    Modal.showConfirm(title, message);
                    break;
            }
        }
    });
});