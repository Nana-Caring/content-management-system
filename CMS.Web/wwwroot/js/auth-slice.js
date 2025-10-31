/**
 * Auth Reducer - Manages authentication state
 */

// Action Types
const AUTH_ACTIONS = {
    LOGIN_START: 'auth/loginStart',
    LOGIN_SUCCESS: 'auth/loginSuccess',
    LOGIN_FAILURE: 'auth/loginFailure',
    LOGOUT: 'auth/logout',
    REFRESH_TOKEN: 'auth/refreshToken',
    SET_USER: 'auth/setUser',
    CLEAR_ERROR: 'auth/clearError'
};

// Initial state
const initialAuthState = {
    user: null,
    token: null,
    isAuthenticated: false,
    isLoading: false,
    error: null,
    refreshToken: null,
    expiresAt: null
};

// Auth Reducer
function authReducer(state = initialAuthState, action) {
    switch (action.type) {
        case AUTH_ACTIONS.LOGIN_START:
            return {
                ...state,
                isLoading: true,
                error: null
            };
            
        case AUTH_ACTIONS.LOGIN_SUCCESS:
            return {
                ...state,
                isLoading: false,
                isAuthenticated: true,
                user: action.payload.user,
                token: action.payload.token,
                refreshToken: action.payload.refreshToken,
                expiresAt: action.payload.expiresAt,
                error: null
            };
            
        case AUTH_ACTIONS.LOGIN_FAILURE:
            return {
                ...state,
                isLoading: false,
                isAuthenticated: false,
                user: null,
                token: null,
                error: action.payload.error
            };
            
        case AUTH_ACTIONS.LOGOUT:
            return {
                ...initialAuthState
            };
            
        case AUTH_ACTIONS.SET_USER:
            return {
                ...state,
                user: { ...state.user, ...action.payload }
            };
            
        case AUTH_ACTIONS.CLEAR_ERROR:
            return {
                ...state,
                error: null
            };
            
        default:
            return state;
    }
}

// Action Creators
const authActions = {
    loginStart: () => ({ type: AUTH_ACTIONS.LOGIN_START }),
    
    loginSuccess: (payload) => ({
        type: AUTH_ACTIONS.LOGIN_SUCCESS,
        payload
    }),
    
    loginFailure: (error) => ({
        type: AUTH_ACTIONS.LOGIN_FAILURE,
        payload: { error }
    }),
    
    logout: () => ({ type: AUTH_ACTIONS.LOGOUT }),
    
    setUser: (user) => ({
        type: AUTH_ACTIONS.SET_USER,
        payload: user
    }),
    
    clearError: () => ({ type: AUTH_ACTIONS.CLEAR_ERROR }),
    
    // Async action for login
    login: (credentials) => async (dispatch, getState) => {
        dispatch(authActions.loginStart());
        
        try {
            const response = await fetch('/Auth/Login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(credentials)
            });
            
            if (response.ok) {
                const data = await response.json();
                dispatch(authActions.loginSuccess(data));
                
                // Store in session storage
                sessionStorage.setItem('cms_auth', JSON.stringify(data));
                
                return { success: true, data };
            } else {
                const error = await response.text();
                dispatch(authActions.loginFailure(error));
                return { success: false, error };
            }
        } catch (error) {
            dispatch(authActions.loginFailure(error.message));
            return { success: false, error: error.message };
        }
    },
    
    // Initialize auth from session storage
    initializeAuth: () => (dispatch) => {
        try {
            const storedAuth = sessionStorage.getItem('cms_auth');
            if (storedAuth) {
                const authData = JSON.parse(storedAuth);
                
                // Check if token is still valid
                if (authData.expiresAt && new Date() < new Date(authData.expiresAt)) {
                    dispatch(authActions.loginSuccess(authData));
                } else {
                    // Token expired, clear storage
                    sessionStorage.removeItem('cms_auth');
                }
            }
        } catch (error) {
            console.error('Error initializing auth:', error);
            sessionStorage.removeItem('cms_auth');
        }
    }
};

// Export to global scope
window.CMS = window.CMS || {};
window.CMS.authReducer = authReducer;
window.CMS.authActions = authActions;
window.CMS.AUTH_ACTIONS = AUTH_ACTIONS;