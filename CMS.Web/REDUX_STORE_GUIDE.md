# CMS Redux Store Documentation

## Overview

This CMS now includes a complete Redux-like state management system implemented in vanilla JavaScript. It provides centralized state management for the entire application with predictable data flow and automatic UI updates.

## Architecture

### Core Components

1. **Store (`redux-store.js`)** - Core Redux implementation
2. **Slices** - Feature-specific reducers and actions:
   - `auth-slice.js` - Authentication state
   - `products-slice.js` - Product management
   - `users-slice.js` - User management
3. **Main Store (`store.js`)** - Combined reducers and store configuration
4. **Store Connector (`store-connector.js`)** - UI binding utilities
5. **Page Integrations** - Page-specific Redux connections

## State Structure

```javascript
{
  auth: {
    user: Object|null,
    token: string|null,
    isAuthenticated: boolean,
    isLoading: boolean,
    error: string|null
  },
  products: {
    items: Array,
    selectedProduct: Object|null,
    isLoading: boolean,
    isCreating: boolean,
    isUpdating: boolean,
    isDeleting: boolean,
    error: string|null,
    filters: Object,
    pagination: Object
  },
  users: {
    items: Array,
    selectedUser: Object|null,
    stats: Object,
    isLoading: boolean,
    isBlocking: boolean,
    isSuspending: boolean,
    isDeleting: boolean,
    error: string|null,
    filters: Object
  },
  ui: {
    modals: Object,
    notifications: Array,
    sidebar: Object,
    theme: string,
    loading: Object
  }
}
```

## Usage Examples

### Accessing the Store

```javascript
// Get current state
const state = window.CMS.store.getState();

// Subscribe to changes
const unsubscribe = window.CMS.store.subscribe((state) => {
    console.log('State changed:', state);
});

// Dispatch actions
window.CMS.store.dispatch(window.CMS.productActions.fetchProducts());
```

### Using Actions

#### Products
```javascript
// Fetch products
window.CMS.store.dispatch(window.CMS.productActions.fetchProducts());

// Create product
window.CMS.store.dispatch(window.CMS.productActions.createProduct({
    name: 'New Product',
    brand: 'Brand Name',
    price: '29.99',
    category: 'Electronics'
}));

// Update product
window.CMS.store.dispatch(window.CMS.productActions.updateProduct(123, {
    name: 'Updated Product Name'
}));

// Delete product
window.CMS.store.dispatch(window.CMS.productActions.deleteProduct(123));
```

#### Users
```javascript
// Fetch users
window.CMS.store.dispatch(window.CMS.userActions.fetchUsers());

// Block user
window.CMS.store.dispatch(window.CMS.userActions.blockUser(456, 'Violation of terms'));

// Suspend user
window.CMS.store.dispatch(window.CMS.userActions.suspendUser(456, 'Inappropriate behavior', 7));
```

#### UI Actions
```javascript
// Show notification
window.CMS.store.dispatch(window.CMS.uiActions.addNotification(
    'Operation completed successfully',
    'success'
));

// Toggle modal
window.CMS.store.dispatch(window.CMS.uiActions.toggleModal('createProduct', true));
```

### Using Store Connector

#### Bind Form to Store
```javascript
const form = document.getElementById('productForm');
window.CMS.connector.bindForm(form, window.CMS.productActions.createProduct, {
    onSuccess: (result) => {
        console.log('Product created:', result);
    },
    onError: (error) => {
        console.error('Failed to create product:', error);
    },
    resetOnSuccess: true
});
```

#### Bind Table to Store
```javascript
const table = document.getElementById('productsTable');
window.CMS.connector.bindTable(
    table,
    (state) => ({
        items: state.products.items,
        isLoading: state.products.isLoading,
        error: state.products.error
    }),
    (product) => `
        <tr>
            <td>${product.name}</td>
            <td>${product.price}</td>
            <td>
                <button onclick="editProduct(${product.id})">Edit</button>
            </td>
        </tr>
    `
);
```

#### Auto-update Element
```javascript
const statusElement = document.getElementById('status');
window.CMS.connector.bindElement(
    statusElement,
    (state) => state.products.isLoading,
    (element, isLoading) => {
        element.textContent = isLoading ? 'Loading...' : 'Ready';
    }
);
```

### Creating Custom Components

```javascript
class MyComponent extends window.CMS.StoreComponent {
    init() {
        this.connect(
            // Map state to props
            (state) => ({
                data: state.products.items,
                isLoading: state.products.isLoading
            }),
            // Map dispatch to props
            (dispatch) => ({
                fetchData: () => dispatch(window.CMS.productActions.fetchProducts()),
                createItem: (data) => dispatch(window.CMS.productActions.createProduct(data))
            })
        );
    }
    
    update(props, actions) {
        // Handle state updates
        if (props.isLoading) {
            this.element.classList.add('loading');
        } else {
            this.element.classList.remove('loading');
        }
    }
}

// Initialize component
const component = new MyComponent(document.getElementById('myElement'), window.CMS.store);
```

## Features

### ✅ Automatic Persistence
- All CRUD operations automatically persist to the server
- Real-time UI updates reflect server state
- Optimistic updates for better UX

### ✅ Error Handling
- Comprehensive error handling for all API calls
- User-friendly error notifications
- Fallback mechanisms for failed operations

### ✅ Loading States
- Loading indicators for all async operations
- Prevents multiple simultaneous operations
- Visual feedback during data operations

### ✅ Notifications System
- Toast notifications for success/error messages
- Auto-dismiss after 5 seconds
- Bootstrap-styled notifications

### ✅ Form Integration
- Automatic form binding to Redux actions
- Form validation and error display
- Reset forms on successful submission

### ✅ Table Integration
- Auto-updating tables bound to store state
- Loading and empty state messages
- Dynamic row rendering

### ✅ Modal Management
- Centralized modal state management
- Easy open/close modal actions
- Form integration within modals

## Best Practices

1. **Always use actions to update state** - Never mutate state directly
2. **Handle async operations** - Use the built-in async middleware for API calls
3. **Provide user feedback** - Show loading states and notifications
4. **Handle errors gracefully** - Always include error handling in async actions
5. **Use selectors** - Create reusable state selectors for complex data access
6. **Keep components simple** - Use the StoreComponent base class for consistent patterns

## Extending the Store

### Adding New Reducers
```javascript
// Create new slice
const newFeatureReducer = (state = initialState, action) => {
    switch (action.type) {
        case 'newFeature/action':
            return { ...state, /* updated state */ };
        default:
            return state;
    }
};

// Add to root reducer (in store.js)
const rootReducer = window.CMS.combineReducers({
    // ... existing reducers
    newFeature: newFeatureReducer
});
```

### Adding New Actions
```javascript
const newFeatureActions = {
    doSomething: (data) => async (dispatch, getState) => {
        dispatch({ type: 'newFeature/start' });
        try {
            const result = await api.doSomething(data);
            dispatch({ type: 'newFeature/success', payload: result });
            return { success: true, data: result };
        } catch (error) {
            dispatch({ type: 'newFeature/failure', payload: error.message });
            return { success: false, error: error.message };
        }
    }
};
```

## Debugging

The store includes a logger middleware (enabled in development) that logs all actions and state changes to the console:

```javascript
// View current state
console.log(window.CMS.store.getState());

// Monitor specific state changes
window.CMS.store.subscribe(() => {
    const state = window.CMS.store.getState();
    console.log('Products:', state.products.items.length);
});
```

## Integration Status

- ✅ **Products Page** - Full Redux integration with CRUD operations
- ✅ **Users Page** - Full Redux integration with user management
- ✅ **Authentication** - Login/logout state management
- ✅ **UI State** - Modal and notification management
- 🔄 **Accounts Page** - Basic structure ready for integration
- 🔄 **Transactions Page** - Basic structure ready for integration

## Performance Considerations

- State updates trigger minimal re-renders through selective subscriptions
- Debounced search inputs prevent excessive API calls
- Optimistic updates provide immediate user feedback
- Error boundaries prevent state corruption
- Automatic cleanup prevents memory leaks

This Redux store provides a solid foundation for scalable state management across the entire CMS application.