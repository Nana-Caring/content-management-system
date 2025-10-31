/**
 * Products Reducer - Manages product state
 */

// Action Types
const PRODUCT_ACTIONS = {
    FETCH_START: 'products/fetchStart',
    FETCH_SUCCESS: 'products/fetchSuccess',
    FETCH_FAILURE: 'products/fetchFailure',
    CREATE_START: 'products/createStart',
    CREATE_SUCCESS: 'products/createSuccess',
    CREATE_FAILURE: 'products/createFailure',
    UPDATE_START: 'products/updateStart',
    UPDATE_SUCCESS: 'products/updateSuccess',
    UPDATE_FAILURE: 'products/updateFailure',
    DELETE_START: 'products/deleteStart',
    DELETE_SUCCESS: 'products/deleteSuccess',
    DELETE_FAILURE: 'products/deleteFailure',
    SET_SELECTED: 'products/setSelected',
    SET_FILTERS: 'products/setFilters',
    CLEAR_ERROR: 'products/clearError'
};

// Initial state
const initialProductsState = {
    items: [],
    selectedProduct: null,
    isLoading: false,
    isCreating: false,
    isUpdating: false,
    isDeleting: false,
    error: null,
    filters: {
        search: '',
        category: '',
        brand: '',
        isActive: null,
        inStock: null,
        sortBy: 'createdAt',
        sortOrder: 'desc',
        page: 1,
        limit: 50
    },
    pagination: {
        total: 0,
        pages: 0,
        currentPage: 1
    }
};

// Products Reducer
function productsReducer(state = initialProductsState, action) {
    switch (action.type) {
        case PRODUCT_ACTIONS.FETCH_START:
            return {
                ...state,
                isLoading: true,
                error: null
            };
            
        case PRODUCT_ACTIONS.FETCH_SUCCESS:
            return {
                ...state,
                isLoading: false,
                items: action.payload.products,
                pagination: action.payload.pagination || state.pagination,
                error: null
            };
            
        case PRODUCT_ACTIONS.FETCH_FAILURE:
            return {
                ...state,
                isLoading: false,
                error: action.payload.error
            };
            
        case PRODUCT_ACTIONS.CREATE_START:
            return {
                ...state,
                isCreating: true,
                error: null
            };
            
        case PRODUCT_ACTIONS.CREATE_SUCCESS:
            return {
                ...state,
                isCreating: false,
                items: [action.payload.product, ...state.items],
                error: null
            };
            
        case PRODUCT_ACTIONS.CREATE_FAILURE:
            return {
                ...state,
                isCreating: false,
                error: action.payload.error
            };
            
        case PRODUCT_ACTIONS.UPDATE_START:
            return {
                ...state,
                isUpdating: true,
                error: null
            };
            
        case PRODUCT_ACTIONS.UPDATE_SUCCESS:
            return {
                ...state,
                isUpdating: false,
                items: state.items.map(item => 
                    item.id === action.payload.product.id 
                        ? action.payload.product 
                        : item
                ),
                selectedProduct: action.payload.product.id === state.selectedProduct?.id 
                    ? action.payload.product 
                    : state.selectedProduct,
                error: null
            };
            
        case PRODUCT_ACTIONS.UPDATE_FAILURE:
            return {
                ...state,
                isUpdating: false,
                error: action.payload.error
            };
            
        case PRODUCT_ACTIONS.DELETE_START:
            return {
                ...state,
                isDeleting: true,
                error: null
            };
            
        case PRODUCT_ACTIONS.DELETE_SUCCESS:
            return {
                ...state,
                isDeleting: false,
                items: state.items.filter(item => item.id !== action.payload.productId),
                selectedProduct: state.selectedProduct?.id === action.payload.productId 
                    ? null 
                    : state.selectedProduct,
                error: null
            };
            
        case PRODUCT_ACTIONS.DELETE_FAILURE:
            return {
                ...state,
                isDeleting: false,
                error: action.payload.error
            };
            
        case PRODUCT_ACTIONS.SET_SELECTED:
            return {
                ...state,
                selectedProduct: action.payload.product
            };
            
        case PRODUCT_ACTIONS.SET_FILTERS:
            return {
                ...state,
                filters: { ...state.filters, ...action.payload.filters }
            };
            
        case PRODUCT_ACTIONS.CLEAR_ERROR:
            return {
                ...state,
                error: null
            };
            
        default:
            return state;
    }
}

// Action Creators
const productActions = {
    // Sync actions
    fetchStart: () => ({ type: PRODUCT_ACTIONS.FETCH_START }),
    fetchSuccess: (products, pagination) => ({
        type: PRODUCT_ACTIONS.FETCH_SUCCESS,
        payload: { products, pagination }
    }),
    fetchFailure: (error) => ({
        type: PRODUCT_ACTIONS.FETCH_FAILURE,
        payload: { error }
    }),
    
    setSelected: (product) => ({
        type: PRODUCT_ACTIONS.SET_SELECTED,
        payload: { product }
    }),
    
    setFilters: (filters) => ({
        type: PRODUCT_ACTIONS.SET_FILTERS,
        payload: { filters }
    }),
    
    clearError: () => ({ type: PRODUCT_ACTIONS.CLEAR_ERROR }),
    
    // Async actions
    fetchProducts: (filters = {}) => async (dispatch, getState) => {
        dispatch(productActions.fetchStart());
        
        try {
            const state = getState();
            const currentFilters = { ...state.products.filters, ...filters };
            
            // Build query string
            const queryParams = new URLSearchParams();
            Object.entries(currentFilters).forEach(([key, value]) => {
                if (value !== null && value !== undefined && value !== '') {
                    queryParams.append(key, value);
                }
            });
            
            const response = await fetch(`/api/products?${queryParams.toString()}`, {
                headers: {
                    'Authorization': `Bearer ${state.auth.token}`,
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.ok) {
                const data = await response.json();
                dispatch(productActions.fetchSuccess(data.products || data, data.pagination));
                return { success: true, data };
            } else {
                const error = await response.text();
                dispatch(productActions.fetchFailure(error));
                return { success: false, error };
            }
        } catch (error) {
            dispatch(productActions.fetchFailure(error.message));
            return { success: false, error: error.message };
        }
    },
    
    createProduct: (productData) => async (dispatch, getState) => {
        dispatch({ type: PRODUCT_ACTIONS.CREATE_START });
        
        try {
            const state = getState();
            const response = await fetch('/Products/Index?handler=Create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams(productData).toString()
            });
            
            if (response.ok) {
                // Refresh products list
                dispatch(productActions.fetchProducts());
                dispatch({ type: PRODUCT_ACTIONS.CREATE_SUCCESS, payload: { product: productData } });
                return { success: true };
            } else {
                const error = await response.text();
                dispatch({ type: PRODUCT_ACTIONS.CREATE_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: PRODUCT_ACTIONS.CREATE_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    updateProduct: (productId, productData) => async (dispatch, getState) => {
        dispatch({ type: PRODUCT_ACTIONS.UPDATE_START });
        
        try {
            const response = await fetch('/Products/Index?handler=Edit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ id: productId, ...productData }).toString()
            });
            
            if (response.ok) {
                dispatch(productActions.fetchProducts());
                dispatch({ type: PRODUCT_ACTIONS.UPDATE_SUCCESS, payload: { product: { id: productId, ...productData } } });
                return { success: true };
            } else {
                const error = await response.text();
                dispatch({ type: PRODUCT_ACTIONS.UPDATE_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: PRODUCT_ACTIONS.UPDATE_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    deleteProduct: (productId) => async (dispatch, getState) => {
        dispatch({ type: PRODUCT_ACTIONS.DELETE_START });
        
        try {
            const response = await fetch('/Products/Index?handler=Delete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: new URLSearchParams({ id: productId }).toString()
            });
            
            if (response.ok) {
                dispatch({ type: PRODUCT_ACTIONS.DELETE_SUCCESS, payload: { productId } });
                return { success: true };
            } else {
                const error = await response.text();
                dispatch({ type: PRODUCT_ACTIONS.DELETE_FAILURE, payload: { error } });
                return { success: false, error };
            }
        } catch (error) {
            dispatch({ type: PRODUCT_ACTIONS.DELETE_FAILURE, payload: { error: error.message } });
            return { success: false, error: error.message };
        }
    },
    
    getProductById: (productId) => async (dispatch, getState) => {
        try {
            const response = await fetch(`/Products/Index?handler=ProductJson&id=${productId}`);
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    dispatch(productActions.setSelected(result.data));
                    return { success: true, data: result.data };
                } else {
                    return { success: false, error: result.message };
                }
            } else {
                const error = await response.text();
                return { success: false, error };
            }
        } catch (error) {
            return { success: false, error: error.message };
        }
    }
};

// Selectors
const productSelectors = {
    getAllProducts: (state) => state.products.items,
    getProductById: (state, id) => state.products.items.find(p => p.id === id),
    getSelectedProduct: (state) => state.products.selectedProduct,
    getFilters: (state) => state.products.filters,
    getIsLoading: (state) => state.products.isLoading,
    getError: (state) => state.products.error
};

// Export to global scope
window.CMS = window.CMS || {};
window.CMS.productsReducer = productsReducer;
window.CMS.productActions = productActions;
window.CMS.productSelectors = productSelectors;
window.CMS.PRODUCT_ACTIONS = PRODUCT_ACTIONS;