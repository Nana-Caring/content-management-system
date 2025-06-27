// Redux Toolkit setup for client-side state management
// This file provides client-side state management capabilities

class StateManager {
    constructor() {
        this.state = {
            user: {
                isAuthenticated: false,
                profile: null,
                preferences: {}
            },
            ui: {
                loading: false,
                notifications: [],
                sidebarOpen: false,
                theme: 'light'
            },
            data: {
                cache: new Map(),
                lastUpdated: {}
            }
        };
        
        this.listeners = new Set();
        this.middleware = [];
        
        // Initialize from sessionStorage if available
        this.loadFromStorage();
    }

    // Core state management methods
    getState() {
        return { ...this.state };
    }

    setState(newState) {
        const prevState = { ...this.state };
        this.state = { ...this.state, ...newState };
        
        // Run middleware
        this.middleware.forEach(middleware => {
            middleware(prevState, this.state);
        });
        
        // Notify listeners
        this.listeners.forEach(listener => {
            listener(this.state, prevState);
        });
        
        // Save to storage
        this.saveToStorage();
    }

    updateState(path, value) {
        const pathArray = path.split('.');
        const newState = { ...this.state };
        let current = newState;
        
        for (let i = 0; i < pathArray.length - 1; i++) {
            current[pathArray[i]] = { ...current[pathArray[i]] };
            current = current[pathArray[i]];
        }
        
        current[pathArray[pathArray.length - 1]] = value;
        this.setState(newState);
    }

    // Subscription methods
    subscribe(listener) {
        this.listeners.add(listener);
        return () => this.listeners.delete(listener);
    }

    addMiddleware(middleware) {
        this.middleware.push(middleware);
    }

    // User state methods
    setUser(userProfile) {
        this.updateState('user.profile', userProfile);
        this.updateState('user.isAuthenticated', !!userProfile);
    }

    logout() {
        this.updateState('user.profile', null);
        this.updateState('user.isAuthenticated', false);
        this.updateState('user.preferences', {});
        
        // Clear sensitive data
        sessionStorage.removeItem('cms_auth_token');
        
        // Notify server about logout
        this.dispatch('logout');
    }

    setUserPreference(key, value) {
        const preferences = { ...this.state.user.preferences, [key]: value };
        this.updateState('user.preferences', preferences);
    }

    // UI state methods
    setLoading(isLoading, message = '') {
        this.updateState('ui.loading', isLoading);
        if (message) {
            this.addNotification('info', 'Loading', message);
        }
    }

    addNotification(type, title, message, duration = 5000) {
        const notification = {
            id: Date.now().toString(),
            type,
            title,
            message,
            timestamp: new Date(),
            read: false
        };
        
        const notifications = [...this.state.ui.notifications, notification];
        this.updateState('ui.notifications', notifications);
        
        // Auto-remove notification after duration
        if (duration > 0) {
            setTimeout(() => {
                this.removeNotification(notification.id);
            }, duration);
        }
        
        return notification.id;
    }

    removeNotification(id) {
        const notifications = this.state.ui.notifications.filter(n => n.id !== id);
        this.updateState('ui.notifications', notifications);
    }

    markNotificationAsRead(id) {
        const notifications = this.state.ui.notifications.map(n => 
            n.id === id ? { ...n, read: true } : n
        );
        this.updateState('ui.notifications', notifications);
    }

    toggleSidebar() {
        this.updateState('ui.sidebarOpen', !this.state.ui.sidebarOpen);
    }

    setTheme(theme) {
        this.updateState('ui.theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    }

    // Data caching methods
    setCache(key, data, ttl = 300000) { // 5 minutes default TTL
        this.state.data.cache.set(key, {
            data,
            timestamp: Date.now(),
            ttl
        });
        this.updateState('data.lastUpdated', { ...this.state.data.lastUpdated, [key]: Date.now() });
    }

    getCache(key) {
        const cached = this.state.data.cache.get(key);
        if (cached && (Date.now() - cached.timestamp) < cached.ttl) {
            return cached.data;
        }
        this.state.data.cache.delete(key);
        return null;
    }

    clearCache(pattern = null) {
        if (pattern) {
            const regex = new RegExp(pattern);
            for (const key of this.state.data.cache.keys()) {
                if (regex.test(key)) {
                    this.state.data.cache.delete(key);
                }
            }
        } else {
            this.state.data.cache.clear();
        }
        this.saveToStorage();
    }

    // Persistence methods
    saveToStorage() {
        try {
            const stateToSave = {
                user: this.state.user,
                ui: {
                    theme: this.state.ui.theme,
                    sidebarOpen: this.state.ui.sidebarOpen
                }
            };
            sessionStorage.setItem('cms_client_state', JSON.stringify(stateToSave));
        } catch (error) {
            console.warn('Failed to save state to storage:', error);
        }
    }

    loadFromStorage() {
        try {
            const savedState = sessionStorage.getItem('cms_client_state');
            if (savedState) {
                const parsedState = JSON.parse(savedState);
                this.state = { ...this.state, ...parsedState };
                
                // Apply theme if saved
                if (parsedState.ui?.theme) {
                    document.documentElement.setAttribute('data-theme', parsedState.ui.theme);
                }
            }
        } catch (error) {
            console.warn('Failed to load state from storage:', error);
        }
    }

    // Action dispatch for server communication
    async dispatch(action, payload = {}) {
        try {
            const response = await fetch(`/api/state/${action}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(payload)
            });
            
            if (response.ok) {
                const result = await response.json();
                return result;
            } else {
                throw new Error(`Action ${action} failed: ${response.statusText}`);
            }
        } catch (error) {
            console.error('Dispatch error:', error);
            this.addNotification('error', 'Error', `Failed to execute ${action}`);
            throw error;
        }
    }

    // Utility methods
    reset() {
        this.state = {
            user: {
                isAuthenticated: false,
                profile: null,
                preferences: {}
            },
            ui: {
                loading: false,
                notifications: [],
                sidebarOpen: false,
                theme: 'light'
            },
            data: {
                cache: new Map(),
                lastUpdated: {}
            }
        };
        sessionStorage.removeItem('cms_client_state');
        this.saveToStorage();
    }

    // Debugging helper
    debug() {
        console.log('Current State:', this.getState());
        console.log('Cache Size:', this.state.data.cache.size);
        console.log('Listeners:', this.listeners.size);
    }
}

// Create global state manager instance
window.CmsStateManager = new StateManager();

// Add some useful middleware
window.CmsStateManager.addMiddleware((prevState, newState) => {
    // Log state changes in development
    if (window.location.hostname === 'localhost') {
        console.log('State changed:', { prevState, newState });
    }
});

// Auto-save user preferences when they change
window.CmsStateManager.addMiddleware((prevState, newState) => {
    if (JSON.stringify(prevState.user.preferences) !== JSON.stringify(newState.user.preferences)) {
        // Debounce API call to save preferences
        clearTimeout(window.preferenceSaveTimeout);
        window.preferenceSaveTimeout = setTimeout(() => {
            window.CmsStateManager.dispatch('savePreferences', newState.user.preferences);
        }, 1000);
    }
});

// Expose convenient methods globally
window.cms = {
    state: window.CmsStateManager,
    setLoading: (loading, message) => window.CmsStateManager.setLoading(loading, message),
    notify: (type, title, message, duration) => window.CmsStateManager.addNotification(type, title, message, duration),
    cache: (key, data, ttl) => window.CmsStateManager.setCache(key, data, ttl),
    getCache: (key) => window.CmsStateManager.getCache(key)
};

// Initialize theme from system preference if not set
if (!window.CmsStateManager.getState().ui.theme) {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    window.CmsStateManager.setTheme(prefersDark ? 'dark' : 'light');
}

console.log('CMS State Manager initialized');
