/**
 * Store Connector - Helper functions to connect DOM elements to Redux store
 */

// Store connector utilities
class StoreConnector {
    constructor(store) {
        this.store = store;
        this.subscriptions = new Map();
    }
    
    // Connect a component to store
    connect(component, mapStateToProps, mapDispatchToProps) {
        const subscription = this.store.subscribe(() => {
            const state = this.store.getState();
            const props = mapStateToProps ? mapStateToProps(state) : {};
            const actions = mapDispatchToProps ? mapDispatchToProps(this.store.dispatch) : {};
            
            if (component.update) {
                component.update(props, actions);
            }
        });
        
        this.subscriptions.set(component, subscription);
        
        // Initial update
        const state = this.store.getState();
        const props = mapStateToProps ? mapStateToProps(state) : {};
        const actions = mapDispatchToProps ? mapDispatchToProps(this.store.dispatch) : {};
        
        if (component.update) {
            component.update(props, actions);
        }
        
        return () => {
            const sub = this.subscriptions.get(component);
            if (sub) {
                sub();
                this.subscriptions.delete(component);
            }
        };
    }
    
    // Bind form to store actions
    bindForm(formElement, actionCreator, options = {}) {
        const { 
            onSuccess, 
            onError, 
            transform = (data) => data,
            resetOnSuccess = false 
        } = options;
        
        formElement.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const formData = new FormData(formElement);
            const data = Object.fromEntries(formData.entries());
            const transformedData = transform(data);
            
            try {
                const result = await this.store.dispatch(actionCreator(transformedData));
                
                if (result.success) {
                    if (onSuccess) onSuccess(result);
                    if (resetOnSuccess) formElement.reset();
                    
                    this.store.dispatch(window.CMS.uiActions.addNotification(
                        'Operation completed successfully',
                        'success'
                    ));
                } else {
                    if (onError) onError(result);
                    
                    this.store.dispatch(window.CMS.uiActions.addNotification(
                        result.error || 'Operation failed',
                        'error'
                    ));
                }
            } catch (error) {
                console.error('Form submission error:', error);
                if (onError) onError({ error: error.message });
                
                this.store.dispatch(window.CMS.uiActions.addNotification(
                    'An unexpected error occurred',
                    'error'
                ));
            }
        });
    }
    
    // Bind button to store action
    bindButton(buttonElement, actionCreator, options = {}) {
        const { 
            confirm = false,
            confirmMessage = 'Are you sure?',
            onSuccess,
            onError,
            getData = () => ({})
        } = options;
        
        buttonElement.addEventListener('click', async (e) => {
            e.preventDefault();
            
            if (confirm && !window.confirm(confirmMessage)) {
                return;
            }
            
            try {
                const data = getData();
                const result = await this.store.dispatch(actionCreator(data));
                
                if (result.success) {
                    if (onSuccess) onSuccess(result);
                } else {
                    if (onError) onError(result);
                }
            } catch (error) {
                console.error('Button action error:', error);
                if (onError) onError({ error: error.message });
            }
        });
    }
    
    // Auto-update elements based on store state
    bindElement(element, selector, renderer) {
        return this.store.subscribe(() => {
            const state = this.store.getState();
            const data = selector(state);
            renderer(element, data);
        });
    }
    
    // Bind table to store data
    bindTable(tableElement, selector, rowRenderer, options = {}) {
        const { 
            emptyMessage = 'No data available',
            loadingMessage = 'Loading...',
            errorRenderer = null 
        } = options;
        
        return this.store.subscribe(() => {
            const state = this.store.getState();
            const data = selector(state);
            
            const tbody = tableElement.querySelector('tbody') || tableElement;
            
            if (data.isLoading) {
                tbody.innerHTML = `<tr><td colspan="100%" class="text-center">${loadingMessage}</td></tr>`;
                return;
            }
            
            if (data.error && errorRenderer) {
                tbody.innerHTML = errorRenderer(data.error);
                return;
            }
            
            if (!data.items || data.items.length === 0) {
                tbody.innerHTML = `<tr><td colspan="100%" class="text-center">${emptyMessage}</td></tr>`;
                return;
            }
            
            tbody.innerHTML = data.items.map(rowRenderer).join('');
        });
    }
    
    // Create notification system
    createNotificationSystem(containerSelector = '#notifications') {
        let container = document.querySelector(containerSelector);
        
        if (!container) {
            container = document.createElement('div');
            container.id = 'notifications';
            container.className = 'position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }
        
        return this.store.subscribe(() => {
            const state = this.store.getState();
            const notifications = state.ui.notifications;
            
            container.innerHTML = notifications.map(notification => `
                <div class="toast show align-items-center text-white bg-${this.getBootstrapClass(notification.type)} border-0" 
                     role="alert" aria-live="assertive" aria-atomic="true" data-notification-id="${notification.id}">
                    <div class="d-flex">
                        <div class="toast-body">
                            ${notification.message}
                        </div>
                        <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                                onclick="window.CMS.store.dispatch(window.CMS.uiActions.removeNotification('${notification.id}'))"
                                aria-label="Close"></button>
                    </div>
                </div>
            `).join('');
        });
    }
    
    getBootstrapClass(type) {
        const classMap = {
            success: 'success',
            error: 'danger',
            warning: 'warning',
            info: 'info'
        };
        return classMap[type] || 'info';
    }
}

// Component base class for easier store integration
class StoreComponent {
    constructor(element, store) {
        this.element = element;
        this.store = store;
        this.connector = new StoreConnector(store);
        this.unsubscribe = null;
        
        this.init();
    }
    
    init() {
        // Override in subclasses
    }
    
    connect(mapStateToProps, mapDispatchToProps) {
        this.unsubscribe = this.connector.connect(this, mapStateToProps, mapDispatchToProps);
    }
    
    update(props, actions) {
        // Override in subclasses
    }
    
    destroy() {
        if (this.unsubscribe) {
            this.unsubscribe();
        }
    }
}

// Helper functions for common patterns
const StoreHelpers = {
    // Auto-refresh data when component mounts
    autoRefresh(actionCreator, interval = 30000) {
        // Immediate fetch
        window.CMS.store.dispatch(actionCreator());
        
        // Set up interval
        return setInterval(() => {
            window.CMS.store.dispatch(actionCreator());
        }, interval);
    },
    
    // Create a simple loading indicator
    createLoader(element, selector) {
        return window.CMS.store.subscribe(() => {
            const state = window.CMS.store.getState();
            const isLoading = selector(state);
            
            if (isLoading) {
                element.classList.add('loading');
                element.style.opacity = '0.6';
                element.style.pointerEvents = 'none';
            } else {
                element.classList.remove('loading');
                element.style.opacity = '';
                element.style.pointerEvents = '';
            }
        });
    },
    
    // Debounced search
    createDebouncedSearch(inputElement, actionCreator, delay = 300) {
        let timeoutId = null;
        
        inputElement.addEventListener('input', (e) => {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                const searchTerm = e.target.value.trim();
                window.CMS.store.dispatch(actionCreator({ search: searchTerm }));
            }, delay);
        });
    }
};

// Export to global scope
window.CMS = window.CMS || {};
window.CMS.StoreConnector = StoreConnector;
window.CMS.StoreComponent = StoreComponent;
window.CMS.StoreHelpers = StoreHelpers;

// Create global connector instance
window.CMS.connector = new StoreConnector(window.CMS.store);

// Create notification system
if (window.CMS.store) {
    window.CMS.connector.createNotificationSystem();
}