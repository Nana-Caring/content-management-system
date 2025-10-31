/**
 * Products Page Redux Integration
 * Connects the Products page to the Redux store
 */

class ProductsPageComponent extends window.CMS.StoreComponent {
    constructor(element, store) {
        super(element, store);
    }
    
    init() {
        this.setupFormBindings();
        this.setupTableBinding();
        this.setupFilters();
        this.setupModals();
        
        // Connect to store
        this.connect(
            (state) => ({
                products: state.products.items,
                isLoading: state.products.isLoading,
                error: state.products.error,
                filters: state.products.filters
            }),
            (dispatch) => ({
                fetchProducts: (filters) => dispatch(window.CMS.productActions.fetchProducts(filters)),
                createProduct: (data) => dispatch(window.CMS.productActions.createProduct(data)),
                updateProduct: (id, data) => dispatch(window.CMS.productActions.updateProduct(id, data)),
                deleteProduct: (id) => dispatch(window.CMS.productActions.deleteProduct(id)),
                getProductById: (id) => dispatch(window.CMS.productActions.getProductById(id)),
                setFilters: (filters) => dispatch(window.CMS.productActions.setFilters(filters))
            })
        );
    }
    
    setupFormBindings() {
        // Create Product Form
        const createForm = document.getElementById('createProductForm');
        if (createForm) {
            this.connector.bindForm(createForm, window.CMS.productActions.createProduct, {
                onSuccess: (result) => {
                    console.log('Product created successfully:', result);
                    this.closeModal('createProductModal');
                },
                onError: (error) => {
                    console.error('Failed to create product:', error);
                },
                resetOnSuccess: true,
                transform: (data) => {
                    // Transform form data before sending
                    return {
                        ...data,
                        images: data.imagesText ? data.imagesText.split('\n').filter(s => s.trim()) : [],
                        tags: data.tagsText ? data.tagsText.split('\n').filter(s => s.trim()) : [],
                        inStock: data.inStock === 'true' || data.inStock === 'on',
                        isActive: data.isActive === 'true' || data.isActive === 'on',
                        requiresAgeVerification: data.requiresAgeVerification === 'true' || data.requiresAgeVerification === 'on'
                    };
                }
            });
        }
        
        // Edit Product Form
        const editForm = document.getElementById('editProductForm');
        if (editForm) {
            this.connector.bindForm(editForm, (data) => 
                window.CMS.productActions.updateProduct(data.id, data), {
                onSuccess: (result) => {
                    console.log('Product updated successfully:', result);
                    this.closeModal('editProductModal');
                },
                onError: (error) => {
                    console.error('Failed to update product:', error);
                },
                transform: (data) => {
                    return {
                        ...data,
                        images: data.imagesText ? data.imagesText.split('\n').filter(s => s.trim()) : [],
                        tags: data.tagsText ? data.tagsText.split('\n').filter(s => s.trim()) : [],
                        inStock: data.inStock === 'true' || data.inStock === 'on',
                        isActive: data.isActive === 'true' || data.isActive === 'on',
                        requiresAgeVerification: data.requiresAgeVerification === 'true' || data.requiresAgeVerification === 'on'
                    };
                }
            });
        }
        
        // Delete buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-action="delete-product"]')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.handleDelete(productId);
            }
            
            if (e.target.matches('[data-action="edit-product"]')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.handleEdit(productId);
            }
        });
    }
    
    setupTableBinding() {
        const table = document.querySelector('#productsTable');
        if (table) {
            this.connector.bindTable(
                table,
                (state) => ({
                    items: state.products.items,
                    isLoading: state.products.isLoading,
                    error: state.products.error
                }),
                (product) => `
                    <tr>
                        <td>${product.id || ''}</td>
                        <td>${product.name || 'Unknown Product'}</td>
                        <td>${product.brand || ''}</td>
                        <td>${product.category || ''}</td>
                        <td>${product.price || ''}</td>
                        <td>
                            <span class="badge ${product.isActive ? 'bg-success' : 'bg-secondary'}">
                                ${product.isActive ? 'Active' : 'Inactive'}
                            </span>
                        </td>
                        <td>
                            <span class="badge ${product.inStock ? 'bg-success' : 'bg-warning'}">
                                ${product.inStock ? 'In Stock' : 'Out of Stock'}
                            </span>
                        </td>
                        <td>${new Date(product.createdAt || Date.now()).toLocaleDateString()}</td>
                        <td>
                            <button class="btn btn-sm btn-primary" data-action="edit-product" data-product-id="${product.id}">
                                <i class="bi bi-pencil"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" data-action="delete-product" data-product-id="${product.id}">
                                <i class="bi bi-trash"></i>
                            </button>
                        </td>
                    </tr>
                `,
                {
                    emptyMessage: 'No products found',
                    loadingMessage: 'Loading products...'
                }
            );
        }
    }
    
    setupFilters() {
        // Search input
        const searchInput = document.querySelector('[name="search"]');
        if (searchInput) {
            window.CMS.StoreHelpers.createDebouncedSearch(
                searchInput,
                window.CMS.productActions.fetchProducts,
                500
            );
        }
        
        // Filter dropdowns
        const filterElements = document.querySelectorAll('[data-filter]');
        filterElements.forEach(element => {
            element.addEventListener('change', (e) => {
                const filterName = element.getAttribute('data-filter');
                const filterValue = e.target.value;
                
                this.store.dispatch(window.CMS.productActions.setFilters({
                    [filterName]: filterValue
                }));
                
                this.store.dispatch(window.CMS.productActions.fetchProducts());
            });
        });
    }
    
    setupModals() {
        // Modal event handlers
        const createModal = document.getElementById('createProductModal');
        if (createModal) {
            createModal.addEventListener('hidden.bs.modal', () => {
                this.store.dispatch(window.CMS.uiActions.toggleModal('createProduct', false));
            });
        }
        
        const editModal = document.getElementById('editProductModal');
        if (editModal) {
            editModal.addEventListener('hidden.bs.modal', () => {
                this.store.dispatch(window.CMS.uiActions.toggleModal('editProduct', false));
            });
        }
    }
    
    async handleEdit(productId) {
        try {
            // Fetch product data
            const result = await this.store.dispatch(
                window.CMS.productActions.getProductById(productId)
            );
            
            if (result.success) {
                this.populateEditForm(result.data);
                this.openModal('editProductModal');
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'Failed to load product data',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error loading product:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error loading product data',
                    'error'
                )
            );
        }
    }
    
    async handleDelete(productId) {
        if (!confirm('Are you sure you want to delete this product?')) {
            return;
        }
        
        try {
            const result = await this.store.dispatch(
                window.CMS.productActions.deleteProduct(productId)
            );
            
            if (result.success) {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'Product deleted successfully',
                        'success'
                    )
                );
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        result.error || 'Failed to delete product',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error deleting product:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error deleting product',
                    'error'
                )
            );
        }
    }
    
    populateEditForm(product) {
        const form = document.getElementById('editProductForm');
        if (!form) return;
        
        // Populate form fields
        const fields = {
            id: product.id,
            name: product.name,
            brand: product.brand,
            price: product.price,
            category: product.category,
            sku: product.sku,
            description: product.description,
            detailedDescription: product.detailedDescription,
            image: product.image,
            imagesText: product.images ? product.images.join('\n') : '',
            stockQuantity: product.stockQuantity,
            tagsText: product.tags ? product.tags.join('\n') : '',
            minAge: product.minAge,
            maxAge: product.maxAge,
            ageCategory: product.ageCategory
        };
        
        Object.entries(fields).forEach(([name, value]) => {
            const input = form.querySelector(`[name="${name}"]`);
            if (input) {
                if (input.type === 'checkbox') {
                    input.checked = Boolean(value);
                } else {
                    input.value = value || '';
                }
            }
        });
        
        // Handle checkboxes
        const checkboxes = {
            inStock: product.inStock,
            isActive: product.isActive,
            requiresAgeVerification: product.requiresAgeVerification
        };
        
        Object.entries(checkboxes).forEach(([name, value]) => {
            const input = form.querySelector(`[name="${name}"]`);
            if (input) {
                input.checked = Boolean(value);
            }
        });
    }
    
    openModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        }
    }
    
    closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            const bsModal = bootstrap.Modal.getInstance(modal);
            if (bsModal) {
                bsModal.hide();
            }
        }
    }
    
    update(props, actions) {
        // Handle store state updates
        console.log('Products page updated with state:', props);
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on the products page
    if (window.location.pathname.includes('/Products')) {
        const productsContainer = document.querySelector('.products-page') || document.body;
        
        // Initialize the products page component
        window.CMS.productsPageComponent = new ProductsPageComponent(
            productsContainer,
            window.CMS.store
        );
        
        // Load initial products data
        window.CMS.store.dispatch(window.CMS.productActions.fetchProducts());
        
        console.log('Products page Redux integration initialized');
    }
});