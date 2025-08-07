// Nana Caring Loader Utility
class NanaLoader {
    constructor() {
        this.activeLoaders = new Set();
        this.loadingStates = new Map();
    }

    // Show full screen loader
    showFullLoader(message = 'Loading Portal...', subtitle = '') {
        this.hideFullLoader(); // Remove any existing loader
        
        const loader = document.createElement('div');
        loader.id = 'nanaFullLoader';
        loader.className = 'nana-loader fade-in';
        
        loader.innerHTML = `
            <div class="loader-logo">
                <i class="bi bi-grid-3x3-gap-fill"></i>
            </div>
            <div class="loader-text">${message}</div>
            ${subtitle ? `<div class="loader-text" style="font-size: 0.9rem; opacity: 0.8;">${subtitle}</div>` : ''}
            <div class="loader-progress">
                <div class="loader-progress-bar"></div>
            </div>
            <div class="loader-dots">
                <div class="loader-dot"></div>
                <div class="loader-dot"></div>
                <div class="loader-dot"></div>
            </div>
        `;
        
        document.body.appendChild(loader);
        this.activeLoaders.add('full');
        
        return loader;
    }

    // Hide full screen loader
    hideFullLoader() {
        const loader = document.getElementById('nanaFullLoader');
        if (loader) {
            loader.classList.remove('fade-in');
            loader.classList.add('fade-out');
            
            setTimeout(() => {
                if (loader.parentNode) {
                    loader.parentNode.removeChild(loader);
                }
            }, 300);
            
            this.activeLoaders.delete('full');
        }
    }

    // Show section loader (overlays a specific section)
    showSectionLoader(sectionId, message = 'Loading...') {
        const section = document.getElementById(sectionId);
        if (!section) return null;

        this.hideSectionLoader(sectionId); // Remove any existing loader
        
        const loader = document.createElement('div');
        loader.id = `loader-${sectionId}`;
        loader.className = 'nana-loader section-loader fade-in';
        
        loader.innerHTML = `
            <div class="loader-logo">
                <i class="bi bi-grid-3x3-gap-fill"></i>
            </div>
            <div class="loader-text">${message}</div>
            <div class="loader-dots">
                <div class="loader-dot"></div>
                <div class="loader-dot"></div>
                <div class="loader-dot"></div>
            </div>
        `;
        
        section.style.position = 'relative';
        section.appendChild(loader);
        this.activeLoaders.add(`section-${sectionId}`);
        
        return loader;
    }

    // Hide section loader
    hideSectionLoader(sectionId) {
        const loader = document.getElementById(`loader-${sectionId}`);
        if (loader) {
            loader.classList.remove('fade-in');
            loader.classList.add('fade-out');
            
            setTimeout(() => {
                if (loader.parentNode) {
                    loader.parentNode.removeChild(loader);
                }
            }, 300);
            
            this.activeLoaders.delete(`section-${sectionId}`);
        }
    }

    // Show inline loader (for smaller content areas)
    showInlineLoader(containerId, message = 'Loading...') {
        const container = document.getElementById(containerId);
        if (!container) return null;

        const loader = document.createElement('div');
        loader.className = 'nana-loader inline-loader';
        loader.id = `inline-loader-${containerId}`;
        
        loader.innerHTML = `
            <div class="loader-logo" style="width: 60px; height: 60px; font-size: 1.5rem; margin-bottom: 1rem;">
                <i class="bi bi-grid-3x3-gap-fill" style="font-size: 1.8rem;"></i>
            </div>
            <div class="loader-text" style="color: #667eea; font-size: 0.9rem;">${message}</div>
            <div class="loader-dots">
                <div class="loader-dot" style="background: #667eea;"></div>
                <div class="loader-dot" style="background: #667eea;"></div>
                <div class="loader-dot" style="background: #667eea;"></div>
            </div>
        `;
        
        container.innerHTML = '';
        container.appendChild(loader);
        this.activeLoaders.add(`inline-${containerId}`);
        
        return loader;
    }

    // Hide inline loader
    hideInlineLoader(containerId) {
        const loader = document.getElementById(`inline-loader-${containerId}`);
        if (loader && loader.parentNode) {
            loader.parentNode.removeChild(loader);
            this.activeLoaders.delete(`inline-${containerId}`);
        }
    }

    // Show button loading state
    showButtonLoader(buttonElement, loadingText = 'Loading...') {
        if (!buttonElement) return;

        const originalText = buttonElement.innerHTML;
        const originalDisabled = buttonElement.disabled;
        
        // Store original state
        this.loadingStates.set(buttonElement, {
            originalText,
            originalDisabled
        });
        
        buttonElement.disabled = true;
        buttonElement.innerHTML = `
            <span class="btn-loader">
                <span class="btn-spinner"></span>
                ${loadingText}
            </span>
        `;
        
        this.activeLoaders.add(`button-${buttonElement.id || 'unknown'}`);
    }

    // Hide button loading state
    hideButtonLoader(buttonElement) {
        if (!buttonElement) return;

        const state = this.loadingStates.get(buttonElement);
        if (state) {
            buttonElement.innerHTML = state.originalText;
            buttonElement.disabled = state.originalDisabled;
            this.loadingStates.delete(buttonElement);
        }
        
        this.activeLoaders.delete(`button-${buttonElement.id || 'unknown'}`);
    }

    // Show table loading overlay
    showTableLoader(tableId, message = 'Loading data...') {
        const table = document.getElementById(tableId);
        if (!table) return;

        // Add loading class to table
        table.classList.add('table-loader');
        
        // Create loading overlay
        const overlay = document.createElement('div');
        overlay.className = 'table-loading';
        overlay.id = `table-loader-${tableId}`;
        
        overlay.innerHTML = `
            <div class="nana-loader inline-loader" style="height: 120px;">
                <div class="loader-logo" style="width: 40px; height: 40px; font-size: 1rem; margin-bottom: 0.5rem;">
                    <i class="bi bi-grid-3x3-gap-fill" style="font-size: 1.2rem;"></i>
                </div>
                <div class="loader-text" style="color: #667eea; font-size: 0.8rem;">${message}</div>
            </div>
        `;
        
        table.style.position = 'relative';
        table.appendChild(overlay);
        this.activeLoaders.add(`table-${tableId}`);
    }

    // Hide table loading overlay
    hideTableLoader(tableId) {
        const table = document.getElementById(tableId);
        const overlay = document.getElementById(`table-loader-${tableId}`);
        
        if (table) {
            table.classList.remove('table-loader');
        }
        
        if (overlay && overlay.parentNode) {
            overlay.parentNode.removeChild(overlay);
        }
        
        this.activeLoaders.delete(`table-${tableId}`);
    }

    // Show card loading shimmer
    showCardLoader(cardElement) {
        if (!cardElement) return;
        
        cardElement.classList.add('card-loading');
        this.activeLoaders.add(`card-${cardElement.id || 'unknown'}`);
    }

    // Hide card loading shimmer
    hideCardLoader(cardElement) {
        if (!cardElement) return;
        
        cardElement.classList.remove('card-loading');
        this.activeLoaders.delete(`card-${cardElement.id || 'unknown'}`);
    }

    // Progress loader for long operations
    showProgressLoader(message = 'Processing...', progress = 0) {
        this.hideProgressLoader();
        
        const loader = document.createElement('div');
        loader.id = 'nanaProgressLoader';
        loader.className = 'nana-loader fade-in';
        
        loader.innerHTML = `
            <div class="loader-logo">
                <i class="bi bi-grid-3x3-gap-fill"></i>
            </div>
            <div class="loader-text">${message}</div>
            <div class="loader-progress" style="width: 300px; height: 8px; margin: 1rem 0;">
                <div class="loader-progress-bar" id="progressBar" style="width: ${progress}%; animation: none; background: linear-gradient(90deg, #667eea, #764ba2);"></div>
            </div>
            <div class="loader-text" style="font-size: 0.9rem;" id="progressText">${progress}%</div>
        `;
        
        document.body.appendChild(loader);
        this.activeLoaders.add('progress');
        
        return loader;
    }

    // Update progress
    updateProgress(progress, message) {
        const progressBar = document.getElementById('progressBar');
        const progressText = document.getElementById('progressText');
        const loaderText = document.querySelector('#nanaProgressLoader .loader-text');
        
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }
        
        if (progressText) {
            progressText.textContent = `${progress}%`;
        }
        
        if (message && loaderText) {
            loaderText.textContent = message;
        }
    }

    // Hide progress loader
    hideProgressLoader() {
        const loader = document.getElementById('nanaProgressLoader');
        if (loader) {
            loader.classList.remove('fade-in');
            loader.classList.add('fade-out');
            
            setTimeout(() => {
                if (loader.parentNode) {
                    loader.parentNode.removeChild(loader);
                }
            }, 300);
            
            this.activeLoaders.delete('progress');
        }
    }

    // Hide all loaders
    hideAllLoaders() {
        this.hideFullLoader();
        this.hideProgressLoader();
        
        // Hide all section loaders
        document.querySelectorAll('[id^="loader-"]').forEach(loader => {
            if (loader.parentNode) {
                loader.parentNode.removeChild(loader);
            }
        });
        
        // Hide all table loaders
        document.querySelectorAll('.table-loader').forEach(table => {
            table.classList.remove('table-loader');
        });
        
        document.querySelectorAll('[id^="table-loader-"]').forEach(overlay => {
            if (overlay.parentNode) {
                overlay.parentNode.removeChild(overlay);
            }
        });
        
        // Hide all card loaders
        document.querySelectorAll('.card-loading').forEach(card => {
            card.classList.remove('card-loading');
        });
        
        // Reset all button loaders
        this.loadingStates.forEach((state, button) => {
            this.hideButtonLoader(button);
        });
        
        this.activeLoaders.clear();
        this.loadingStates.clear();
    }

    // Check if any loader is active
    isLoading() {
        return this.activeLoaders.size > 0;
    }

    // Get active loaders
    getActiveLoaders() {
        return Array.from(this.activeLoaders);
    }
}

// Create global loader instance
window.nanaLoader = new NanaLoader();

// Helper functions for easier access
window.showLoader = (message, subtitle) => window.nanaLoader.showFullLoader(message, subtitle);
window.hideLoader = () => window.nanaLoader.hideFullLoader();
window.showSectionLoader = (sectionId, message) => window.nanaLoader.showSectionLoader(sectionId, message);
window.hideSectionLoader = (sectionId) => window.nanaLoader.hideSectionLoader(sectionId);
window.showButtonLoader = (button, text) => window.nanaLoader.showButtonLoader(button, text);
window.hideButtonLoader = (button) => window.nanaLoader.hideButtonLoader(button);

// Auto-hide loaders on page unload
window.addEventListener('beforeunload', () => {
    window.nanaLoader.hideAllLoaders();
});
