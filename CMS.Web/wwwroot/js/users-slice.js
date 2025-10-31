/**
 * Users Reducer - Manages user state
 */

// Action Types
const USER_ACTIONS = {
    FETCH_START: 'users/fetchStart',
    FETCH_SUCCESS: 'users/fetchSuccess',
    FETCH_FAILURE: 'users/fetchFailure',
    CREATE_START: 'users/createStart',
    CREATE_SUCCESS: 'users/createSuccess',
    CREATE_FAILURE: 'users/createFailure',
    UPDATE_START: 'users/updateStart',
    UPDATE_SUCCESS: 'users/updateSuccess',
    UPDATE_FAILURE: 'users/updateFailure',
    DELETE_START: 'users/deleteStart',
    DELETE_SUCCESS: 'users/deleteSuccess',
    DELETE_FAILURE: 'users/deleteFailure',
    BLOCK_START: 'users/blockStart',
    BLOCK_SUCCESS: 'users/blockSuccess',
    BLOCK_FAILURE: 'users/blockFailure',
    UNBLOCK_START: 'users/unblockStart',
    UNBLOCK_SUCCESS: 'users/unblockSuccess',
    UNBLOCK_FAILURE: 'users/unblockFailure',
    SUSPEND_START: 'users/suspendStart',
    SUSPEND_SUCCESS: 'users/suspendSuccess',
    SUSPEND_FAILURE: 'users/suspendFailure',
    SET_SELECTED: 'users/setSelected',
    SET_FILTERS: 'users/setFilters',
    SET_STATS: 'users/setStats',
    CLEAR_ERROR: 'users/clearError'
};

// Initial state
const initialUsersState = {
    items: [],
    selectedUser: null,
    stats: {
        total: 0,
        active: 0,
        blocked: 0,
        suspended: 0
    },
    isLoading: false,
    isCreating: false,
    isUpdating: false,
    isDeleting: false,
    isBlocking: false,
    isSuspending: false,
    error: null,
    filters: {
        search: '',
        role: '',
        relation: '',
        createdFrom: null,
        createdTo: null,
        sortBy: 'createdAt',
        sortDirection: 'desc'
    }
};

// Users Reducer
function usersReducer(state = initialUsersState, action) {
    switch (action.type) {
        case USER_ACTIONS.FETCH_START:
            return {
                ...state,
                isLoading: true,
                error: null
            };
            
        case USER_ACTIONS.FETCH_SUCCESS:
            return {
                ...state,
                isLoading: false,
                items: action.payload.users,
                stats: action.payload.stats || state.stats,
                error: null
            };
            
        case USER_ACTIONS.FETCH_FAILURE:
            return {
                ...state,
                isLoading: false,
                error: action.payload.error
            };
            
        case USER_ACTIONS.BLOCK_START:
        case USER_ACTIONS.UNBLOCK_START:
            return {
                ...state,
                isBlocking: true,
                error: null
            };
            
        case USER_ACTIONS.BLOCK_SUCCESS:
        case USER_ACTIONS.UNBLOCK_SUCCESS:
            return {
                ...state,
                isBlocking: false,
                items: state.items.map(user => 
                    user.id === action.payload.user.id 
                        ? action.payload.user 
                        : user
                ),
                selectedUser: state.selectedUser?.id === action.payload.user.id 
                    ? action.payload.user 
                    : state.selectedUser,
                error: null
            };
            
        case USER_ACTIONS.BLOCK_FAILURE:
        case USER_ACTIONS.UNBLOCK_FAILURE:
            return {
                ...state,
                isBlocking: false,
                error: action.payload.error
            };
            
        case USER_ACTIONS.SUSPEND_START:
            return {
                ...state,
                isSuspending: true,
                error: null
            };
            
        case USER_ACTIONS.SUSPEND_SUCCESS:
            return {
                ...state,
                isSuspending: false,
                items: state.items.map(user => 
                    user.id === action.payload.user.id 
                        ? action.payload.user 
                        : user
                ),
                selectedUser: state.selectedUser?.id === action.payload.user.id 
                    ? action.payload.user 
                    : state.selectedUser,
                error: null
            };
            
        case USER_ACTIONS.SUSPEND_FAILURE:
            return {
                ...state,
                isSuspending: false,
                error: action.payload.error
            };
            
        case USER_ACTIONS.DELETE_START:
            return {
                ...state,
                isDeleting: true,
                error: null
            };
            
        case USER_ACTIONS.DELETE_SUCCESS:
            return {
                ...state,
                isDeleting: false,
                items: state.items.filter(user => user.id !== action.payload.userId),
                selectedUser: state.selectedUser?.id === action.payload.userId 
                    ? null 
                    : state.selectedUser,
                error: null
            };
            
        case USER_ACTIONS.DELETE_FAILURE:
            return {
                ...state,
                isDeleting: false,
                error: action.payload.error
            };
            
        case USER_ACTIONS.SET_SELECTED:
            return {
                ...state,
                selectedUser: action.payload.user
            };
            
        case USER_ACTIONS.SET_FILTERS:
            return {
                ...state,
                filters: { ...state.filters, ...action.payload.filters }
            };
            
        case USER_ACTIONS.SET_STATS:
            return {
                ...state,
                stats: action.payload.stats
            };
            
        case USER_ACTIONS.CLEAR_ERROR:
            return {
                ...state,
                error: null
            };
            
        default:
            return state;
    }
}

// Action Creators
const userActions = {
    // Sync actions
    setSelected: (user) => ({
        type: USER_ACTIONS.SET_SELECTED,
        payload: { user }
    }),
    
    setFilters: (filters) => ({
        type: USER_ACTIONS.SET_FILTERS,
        payload: { filters }
    }),
    
    setStats: (stats) => ({
        type: USER_ACTIONS.SET_STATS,
        payload: { stats }
    }),
    
    clearError: () => ({ type: USER_ACTIONS.CLEAR_ERROR }),
    
    // Async actions
    fetchUsers: (filters = {}) => async (dispatch, getState) => {
        dispatch({ type: USER_ACTIONS.FETCH_START });
        
        try {
            const state = getState();
            const currentFilters = { ...state.users.filters, ...filters };
            
            // Build query string
            const queryParams = new URLSearchParams();
            Object.entries(currentFilters).forEach(([key, value]) => {
                if (value !== null && value !== undefined && value !== '') {
                    queryParams.append(key, value);
                }
            });
            
            const response = await fetch(`/Users?${queryParams.toString()}`, {
                headers: {
                    'Authorization': `Bearer ${state.auth.token}`,
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.ok) {
                const html = await response.text();
                // Parse users from server response or use API endpoint
                // For now, assuming we'll add an API endpoint
                dispatch({ type: USER_ACTIONS.FETCH_SUCCESS, payload: { users: [], stats: {} } });
                return { success: true };
            } else {
                const error = await response.text();
                dispatch({ type: USER_ACTIONS.FETCH_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: USER_ACTIONS.FETCH_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    blockUser: (userId, reason) => async (dispatch, getState) => {
        dispatch({ type: USER_ACTIONS.BLOCK_START });
        
        try {
            const response = await fetch(`/Users/Index?handler=Block`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ userId, reason }).toString()
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    dispatch({ type: USER_ACTIONS.BLOCK_SUCCESS, payload: { user: result.data } });
                    return { success: true, data: result.data };
                } else {
                    dispatch({ type: USER_ACTIONS.BLOCK_FAILURE, payload: { error: result.message } });
                    return { success: false, error: result.message };
                }
            } else {
                const error = await response.text();
                dispatch({ type: USER_ACTIONS.BLOCK_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: USER_ACTIONS.BLOCK_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    unblockUser: (userId) => async (dispatch, getState) => {
        dispatch({ type: USER_ACTIONS.UNBLOCK_START });
        
        try {
            const response = await fetch(`/Users/Index?handler=Unblock`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ userId }).toString()
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    dispatch({ type: USER_ACTIONS.UNBLOCK_SUCCESS, payload: { user: result.data } });
                    return { success: true, data: result.data };
                } else {
                    dispatch({ type: USER_ACTIONS.UNBLOCK_FAILURE, payload: { error: result.message } });
                    return { success: false, error: result.message };
                }
            } else {
                const error = await response.text();
                dispatch({ type: USER_ACTIONS.UNBLOCK_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: USER_ACTIONS.UNBLOCK_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    suspendUser: (userId, reason, days) => async (dispatch, getState) => {
        dispatch({ type: USER_ACTIONS.SUSPEND_START });
        
        try {
            const response = await fetch(`/Users/Index?handler=Suspend`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ userId, reason, days: days || '' }).toString()
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    dispatch({ type: USER_ACTIONS.SUSPEND_SUCCESS, payload: { user: result.data } });
                    return { success: true, data: result.data };
                } else {
                    dispatch({ type: USER_ACTIONS.SUSPEND_FAILURE, payload: { error: result.message } });
                    return { success: false, error: result.message };
                }
            } else {
                const error = await response.text();
                dispatch({ type: USER_ACTIONS.SUSPEND_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: USER_ACTIONS.SUSPEND_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    deleteUser: (userId, confirmDelete = false, deleteData = true) => async (dispatch, getState) => {
        dispatch({ type: USER_ACTIONS.DELETE_START });
        
        try {
            const response = await fetch(`/Users/Index?handler=Delete`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ userId, confirmDelete, deleteData }).toString()
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    dispatch({ type: USER_ACTIONS.DELETE_SUCCESS, payload: { userId } });
                    return { success: true };
                } else {
                    dispatch({ type: USER_ACTIONS.DELETE_FAILURE, payload: { error: result.message } });
                    return { success: false, error: result.message };
                }
            } else {
                const error = await response.text();
                dispatch({ type: USER_ACTIONS.DELETE_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: USER_ACTIONS.DELETE_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    }
};

// Selectors
const userSelectors = {
    getAllUsers: (state) => state.users.items,
    getUserById: (state, id) => state.users.items.find(u => u.id === id),
    getSelectedUser: (state) => state.users.selectedUser,
    getStats: (state) => state.users.stats,
    getFilters: (state) => state.users.filters,
    getIsLoading: (state) => state.users.isLoading,
    getError: (state) => state.users.error
};

// Export to global scope
window.CMS = window.CMS || {};
window.CMS.usersReducer = usersReducer;
window.CMS.userActions = userActions;
window.CMS.userSelectors = userSelectors;
window.CMS.USER_ACTIONS = USER_ACTIONS;