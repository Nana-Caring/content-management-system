/**
 * Users Page Redux Integration
 * Connects the Users page to the Redux store
 */

class UsersPageComponent extends window.CMS.StoreComponent {
    constructor(element, store) {
        super(element, store);
    }
    
    init() {
        this.setupTableBinding();
        this.setupFilters();
        this.setupActionButtons();
        
        // Connect to store
        this.connect(
            (state) => ({
                users: state.users.items,
                stats: state.users.stats,
                isLoading: state.users.isLoading,
                error: state.users.error,
                filters: state.users.filters
            }),
            (dispatch) => ({
                fetchUsers: (filters) => dispatch(window.CMS.userActions.fetchUsers(filters)),
                blockUser: (userId, reason) => dispatch(window.CMS.userActions.blockUser(userId, reason)),
                unblockUser: (userId) => dispatch(window.CMS.userActions.unblockUser(userId)),
                suspendUser: (userId, reason, days) => dispatch(window.CMS.userActions.suspendUser(userId, reason, days)),
                deleteUser: (userId, confirm, deleteData) => dispatch(window.CMS.userActions.deleteUser(userId, confirm, deleteData)),
                setFilters: (filters) => dispatch(window.CMS.userActions.setFilters(filters))
            })
        );
    }
    
    setupTableBinding() {
        const table = document.querySelector('#usersTable');
        if (table) {
            this.connector.bindTable(
                table,
                (state) => ({
                    items: state.users.items,
                    isLoading: state.users.isLoading,
                    error: state.users.error
                }),
                (user) => `
                    <tr class="${user.isBlocked ? 'table-danger' : user.status === 'Suspended' ? 'table-warning' : ''}">
                        <td>${user.id || ''}</td>
                        <td>
                            <div class="d-flex align-items-center">
                                <div>
                                    <div class="fw-bold">${user.firstName || ''} ${user.surname || ''}</div>
                                    <small class="text-muted">${user.email || ''}</small>
                                </div>
                            </div>
                        </td>
                        <td>
                            <span class="badge bg-info">${user.role || 'User'}</span>
                        </td>
                        <td>
                            <span class="badge ${this.getStatusBadgeClass(user.status || 'Active')}">
                                ${user.status || 'Active'}
                            </span>
                        </td>
                        <td>${user.phoneNumber || 'N/A'}</td>
                        <td>${new Date(user.createdAt || Date.now()).toLocaleDateString()}</td>
                        <td>
                            <div class="btn-group" role="group">
                                <button class="btn btn-sm btn-outline-primary" 
                                        data-action="view-user" 
                                        data-user-id="${user.id}"
                                        title="View Details">
                                    <i class="bi bi-eye"></i>
                                </button>
                                ${user.isBlocked 
                                    ? `<button class="btn btn-sm btn-outline-success" 
                                              data-action="unblock-user" 
                                              data-user-id="${user.id}"
                                              title="Unblock User">
                                          <i class="bi bi-unlock"></i>
                                       </button>`
                                    : `<button class="btn btn-sm btn-outline-warning" 
                                              data-action="block-user" 
                                              data-user-id="${user.id}"
                                              title="Block User">
                                          <i class="bi bi-lock"></i>
                                       </button>`
                                }
                                <button class="btn btn-sm btn-outline-info" 
                                        data-action="suspend-user" 
                                        data-user-id="${user.id}"
                                        title="Suspend User">
                                    <i class="bi bi-pause-circle"></i>
                                </button>
                                <button class="btn btn-sm btn-outline-danger" 
                                        data-action="delete-user" 
                                        data-user-id="${user.id}"
                                        title="Delete User">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                        </td>
                    </tr>
                `,
                {
                    emptyMessage: 'No users found',
                    loadingMessage: 'Loading users...'
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
                window.CMS.userActions.fetchUsers,
                500
            );
        }
        
        // Role filter
        const roleFilter = document.querySelector('[data-filter="role"]');
        if (roleFilter) {
            roleFilter.addEventListener('change', (e) => {
                this.store.dispatch(window.CMS.userActions.setFilters({
                    role: e.target.value
                }));
                this.store.dispatch(window.CMS.userActions.fetchUsers());
            });
        }
        
        // Date range filters
        const dateFromInput = document.querySelector('[name="createdFrom"]');
        const dateToInput = document.querySelector('[name="createdTo"]');
        
        if (dateFromInput) {
            dateFromInput.addEventListener('change', (e) => {
                this.store.dispatch(window.CMS.userActions.setFilters({
                    createdFrom: e.target.value
                }));
                this.store.dispatch(window.CMS.userActions.fetchUsers());
            });
        }
        
        if (dateToInput) {
            dateToInput.addEventListener('change', (e) => {
                this.store.dispatch(window.CMS.userActions.setFilters({
                    createdTo: e.target.value
                }));
                this.store.dispatch(window.CMS.userActions.fetchUsers());
            });
        }
    }
    
    setupActionButtons() {
        document.addEventListener('click', (e) => {
            const userId = e.target.closest('[data-user-id]')?.getAttribute('data-user-id');
            if (!userId) return;
            
            if (e.target.closest('[data-action="view-user"]')) {
                e.preventDefault();
                this.handleViewUser(userId);
            }
            
            if (e.target.closest('[data-action="block-user"]')) {
                e.preventDefault();
                this.handleBlockUser(userId);
            }
            
            if (e.target.closest('[data-action="unblock-user"]')) {
                e.preventDefault();
                this.handleUnblockUser(userId);
            }
            
            if (e.target.closest('[data-action="suspend-user"]')) {
                e.preventDefault();
                this.handleSuspendUser(userId);
            }
            
            if (e.target.closest('[data-action="delete-user"]')) {
                e.preventDefault();
                this.handleDeleteUser(userId);
            }
        });
    }
    
    getStatusBadgeClass(status) {
        const statusClasses = {
            'Active': 'bg-success',
            'Inactive': 'bg-secondary',
            'Blocked': 'bg-danger',
            'Suspended': 'bg-warning'
        };
        return statusClasses[status] || 'bg-secondary';
    }
    
    handleViewUser(userId) {
        // Navigate to user details page
        window.location.href = `/Users/Details/${userId}`;
    }
    
    async handleBlockUser(userId) {
        const reason = prompt('Please enter a reason for blocking this user:');
        if (!reason) return;
        
        try {
            const result = await this.store.dispatch(
                window.CMS.userActions.blockUser(userId, reason)
            );
            
            if (result.success) {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'User blocked successfully',
                        'success'
                    )
                );
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        result.error || 'Failed to block user',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error blocking user:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error blocking user',
                    'error'
                )
            );
        }
    }
    
    async handleUnblockUser(userId) {
        if (!confirm('Are you sure you want to unblock this user?')) {
            return;
        }
        
        try {
            const result = await this.store.dispatch(
                window.CMS.userActions.unblockUser(userId)
            );
            
            if (result.success) {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'User unblocked successfully',
                        'success'
                    )
                );
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        result.error || 'Failed to unblock user',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error unblocking user:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error unblocking user',
                    'error'
                )
            );
        }
    }
    
    async handleSuspendUser(userId) {
        const reason = prompt('Please enter a reason for suspending this user:');
        if (!reason) return;
        
        const daysStr = prompt('How many days to suspend? (leave empty for indefinite)');
        const days = daysStr ? parseInt(daysStr) : null;
        
        try {
            const result = await this.store.dispatch(
                window.CMS.userActions.suspendUser(userId, reason, days)
            );
            
            if (result.success) {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'User suspended successfully',
                        'success'
                    )
                );
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        result.error || 'Failed to suspend user',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error suspending user:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error suspending user',
                    'error'
                )
            );
        }
    }
    
    async handleDeleteUser(userId) {
        const confirmDelete = confirm(
            'Are you sure you want to delete this user? This action cannot be undone.'
        );
        
        if (!confirmDelete) return;
        
        const deleteData = confirm(
            'Do you also want to delete all associated user data (accounts, transactions)?'
        );
        
        try {
            const result = await this.store.dispatch(
                window.CMS.userActions.deleteUser(userId, true, deleteData)
            );
            
            if (result.success) {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        'User deleted successfully',
                        'success'
                    )
                );
            } else {
                this.store.dispatch(
                    window.CMS.uiActions.addNotification(
                        result.error || 'Failed to delete user',
                        'error'
                    )
                );
            }
        } catch (error) {
            console.error('Error deleting user:', error);
            this.store.dispatch(
                window.CMS.uiActions.addNotification(
                    'Error deleting user',
                    'error'
                )
            );
        }
    }
    
    update(props, actions) {
        // Handle store state updates
        this.updateStats(props.stats);
        console.log('Users page updated with state:', props);
    }
    
    updateStats(stats) {
        // Update stats display if elements exist
        const statsElements = {
            total: document.querySelector('[data-stat="total-users"]'),
            active: document.querySelector('[data-stat="active-users"]'),
            blocked: document.querySelector('[data-stat="blocked-users"]'),
            suspended: document.querySelector('[data-stat="suspended-users"]')
        };
        
        Object.entries(statsElements).forEach(([key, element]) => {
            if (element && stats[key] !== undefined) {
                element.textContent = stats[key];
            }
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on the users page
    if (window.location.pathname.includes('/Users')) {
        const usersContainer = document.querySelector('.users-page') || document.body;
        
        // Initialize the users page component
        window.CMS.usersPageComponent = new UsersPageComponent(
            usersContainer,
            window.CMS.store
        );
        
        // Load initial users data
        window.CMS.store.dispatch(window.CMS.userActions.fetchUsers());
        
        console.log('Users page Redux integration initialized');
    }
});