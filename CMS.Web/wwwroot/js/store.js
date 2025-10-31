/**
 * Main CMS Store Configuration
 * Combines all reducers and creates the main store instance
 */

// Accounts Reducer (simple implementation)
const initialAccountsState = {
    items: [],
    isLoading: false,
    error: null
};

function accountsReducer(state = initialAccountsState, action) {
    switch (action.type) {
        case 'accounts/fetchStart':
            return { ...state, isLoading: true, error: null };
        case 'accounts/fetchSuccess':
            return { ...state, isLoading: false, items: action.payload, error: null };
        case 'accounts/fetchFailure':
            return { ...state, isLoading: false, error: action.payload };
        default:
            return state;
    }
}

// Transactions Reducer (simple implementation)
const initialTransactionsState = {
    items: [],
    isLoading: false,
    error: null
};

function transactionsReducer(state = initialTransactionsState, action) {
    switch (action.type) {
        case 'transactions/fetchStart':
            return { ...state, isLoading: true, error: null };
        case 'transactions/fetchSuccess':
            return { ...state, isLoading: false, items: action.payload, error: null };
        case 'transactions/fetchFailure':
            return { ...state, isLoading: false, error: action.payload };
        default:
            return state;
    }
}

// UI State Reducer (for modal states, notifications, etc.)
const initialUIState = {
    modals: {
        createProduct: false,
        editProduct: false,
        deleteProduct: false,
        blockUser: false,
        suspendUser: false
    },
    notifications: [],
    sidebar: {
        isCollapsed: false
    },
    theme: 'light',
    loading: {
        global: false
    }
};

function uiReducer(state = initialUIState, action) {
    switch (action.type) {
        case 'ui/toggleModal':
            return {
                ...state,
                modals: {
                    ...state.modals,
                    [action.payload.modal]: action.payload.isOpen
                }
            };
        case 'ui/addNotification':
            return {
                ...state,
                notifications: [...state.notifications, action.payload]
            };
        case 'ui/removeNotification':
            return {
                ...state,
                notifications: state.notifications.filter(n => n.id !== action.payload.id)
            };
        case 'ui/toggleSidebar':
            return {
                ...state,
                sidebar: {
                    ...state.sidebar,
                    isCollapsed: !state.sidebar.isCollapsed
                }
            };
        case 'ui/setTheme':
            return {
                ...state,
                theme: action.payload.theme
            };
        case 'ui/setGlobalLoading':
            return {
                ...state,
                loading: {
                    ...state.loading,
                    global: action.payload.isLoading
                }
            };
        default:
            return state;
    }
}

// Root Reducer
const rootReducer = window.CMS.combineReducers({
    auth: window.CMS.authReducer,
    products: window.CMS.productsReducer,
    users: window.CMS.usersReducer,
    accounts: accountsReducer,
    transactions: transactionsReducer,
    ui: uiReducer
});

// Create Store Instance
const store = new window.CMS.Store(
    rootReducer,
    {}, // Initial state (will be populated)
    [
        window.CMS.asyncMiddleware,
        window.CMS.loggerMiddleware // Remove in production
    ]
);

// UI Actions
const uiActions = {
    toggleModal: (modal, isOpen) => ({
        type: 'ui/toggleModal',
        payload: { modal, isOpen }
    }),
    
    addNotification: (message, type = 'info', duration = 5000) => {
        const id = Date.now().toString();
        return (dispatch) => {
            dispatch({
                type: 'ui/addNotification',
                payload: { id, message, type, timestamp: Date.now() }
            });
            
            if (duration > 0) {
                setTimeout(() => {
                    dispatch(uiActions.removeNotification(id));
                }, duration);
            }
        };
    },
    
    removeNotification: (id) => ({
        type: 'ui/removeNotification',
        payload: { id }
    }),
    
    toggleSidebar: () => ({
        type: 'ui/toggleSidebar'
    }),
    
    setTheme: (theme) => ({
        type: 'ui/setTheme',
        payload: { theme }
    }),
    
    setGlobalLoading: (isLoading) => ({
        type: 'ui/setGlobalLoading',
        payload: { isLoading }
    })
};

// Account Actions (simple)
const accountActions = {
    fetchAccounts: () => async (dispatch) => {
        dispatch({ type: 'accounts/fetchStart' });
        try {
            const response = await fetch('/Accounts');
            if (response.ok) {
                const data = await response.json();
                dispatch({ type: 'accounts/fetchSuccess', payload: data });
            } else {
                dispatch({ type: 'accounts/fetchFailure', payload: 'Failed to fetch accounts' });
            }
        } catch (error) {
            dispatch({ type: 'accounts/fetchFailure', payload: error.message });
        }
    }
};

// Transaction Actions (simple)
const transactionActions = {
    fetchTransactions: () => async (dispatch) => {
        dispatch({ type: 'transactions/fetchStart' });
        try {
            const response = await fetch('/Transactions');
            if (response.ok) {
                const data = await response.json();
                dispatch({ type: 'transactions/fetchSuccess', payload: data });
            } else {
                dispatch({ type: 'transactions/fetchFailure', payload: 'Failed to fetch transactions' });
            }
        } catch (error) {
            dispatch({ type: 'transactions/fetchFailure', payload: error.message });
        }
    }
};

// Store Initialization
function initializeStore() {
    // Initialize authentication from session storage
    store.dispatch(window.CMS.authActions.initializeAuth());
    
    // Load initial data if authenticated
    const state = store.getState();
    if (state.auth.isAuthenticated) {
        // Don't auto-load all data, let pages load what they need
        console.log('Store initialized with authenticated user');
    }
    
    console.log('CMS Redux Store initialized');
}

// Store persistence helpers
function saveStoreState() {
    const state = store.getState();
    
    // Save UI preferences
    localStorage.setItem('cms_ui_preferences', JSON.stringify({
        theme: state.ui.theme,
        sidebar: state.ui.sidebar
    }));
}

function loadStoreState() {
    try {
        const uiPrefs = localStorage.getItem('cms_ui_preferences');
        if (uiPrefs) {
            const prefs = JSON.parse(uiPrefs);
            if (prefs.theme) store.dispatch(uiActions.setTheme(prefs.theme));
            if (prefs.sidebar?.isCollapsed) store.dispatch(uiActions.toggleSidebar());
        }
    } catch (error) {
        console.error('Error loading store state:', error);
    }
}

// Save state on changes
store.subscribe(() => {
    saveStoreState();
});

// Load saved state
loadStoreState();

// Global error handler
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    store.dispatch(uiActions.addNotification(
        'An unexpected error occurred. Please try again.',
        'error'
    ));
});

// Export everything to global scope
window.CMS = window.CMS || {};
window.CMS.store = store;
window.CMS.uiActions = uiActions;
window.CMS.accountActions = accountActions;
window.CMS.transactionActions = transactionActions;
window.CMS.initializeStore = initializeStore;

// Initialize store when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeStore);
} else {
    initializeStore();
}

console.log('CMS Redux Store loaded successfully');