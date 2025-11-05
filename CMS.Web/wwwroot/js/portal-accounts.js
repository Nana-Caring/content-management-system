/**
 * Portal Accounts Module
 * Handles account data loading and display
 */

/**
 * Load accounts data from API
 */
async function loadAccountsData() {
    const container = document.getElementById('accountsContainer');
    if (!container) return;
    
    // Show loading state
    container.innerHTML = `
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-muted mt-2">Loading accounts...</p>
        </div>
    `;
    
    try {
        console.log('📦 Loading accounts from login response cache...');
        
        // Get user data from login response (cached by PortalPersistence)
        const user = window.PortalPersistence?.getCurrentUser();
        
        if (!user || !user.Accounts) {
            throw new Error('User data or accounts not found. Please log in again.');
        }
        
        console.log('✅ Accounts loaded for user:', user.email);
        console.log('💳 Account details:', user.Accounts);
        
        // User data already includes accounts (attached during login)
        const accounts = user.Accounts;
        
        // Combine user accounts and dependent accounts if we have user data
        const allAccounts = combineAllAccounts(user);
        
        if (allAccounts.length === 0) {
            container.innerHTML = `
                <div class="alert alert-info" role="alert">
                    <i class="bi bi-info-circle me-2"></i>
                    No accounts found.
                </div>
            `;
            return;
        }
        
        // Render accounts table
        renderAccountsTable(container, allAccounts, user);
        
    } catch (error) {
        console.error('Error loading accounts data:', error);
        
        let errorMessage = 'Error loading accounts data';
        if (error.message.includes('not found') || error.message.includes('log in')) {
            errorMessage = 'Authentication required. Please log in to view accounts.';
        } else if (error.message) {
            errorMessage = error.message;
        }
        
        container.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Error:</strong> ${errorMessage}
                <button onclick="loadAccountsData()" class="btn btn-sm btn-outline-danger float-end">
                    <i class="bi bi-arrow-clockwise"></i> Retry
                </button>
            </div>
        `;
    }
}

/**
 * Combine user accounts and dependent accounts
 */
function combineAllAccounts(user) {
    let allAccounts = [];
    
    // Handle both 'Accounts' and 'accounts' property names
    const userAccounts = user.Accounts || user.accounts || [];
    
    // Add user's own accounts
    userAccounts.forEach(account => {
        allAccounts.push({
            ...account,
            ownerName: `${user.firstName || ''} ${user.surname || user.lastName || ''}`.trim() || 'Current User',
            ownerType: 'Primary Account Holder',
            ownerEmail: user.email
        });
    });
    
    // Handle both 'Dependents' and 'dependents' property names
    const userDependents = user.Dependents || user.dependents || [];
    
    // Add dependent accounts
    if (userDependents.length > 0) {
        userDependents.forEach(dependent => {
            const dependentAccounts = dependent.Accounts || dependent.accounts || [];
            dependentAccounts.forEach(account => {
                allAccounts.push({
                    ...account,
                    ownerName: `${dependent.firstName || ''} ${dependent.surname || dependent.lastName || ''}`.trim() || 'Dependent',
                    ownerType: 'Dependent',
                    ownerEmail: dependent.email
                });
            });
        });
    }
    
    return allAccounts;
}

/**
 * Render accounts table
 */
function renderAccountsTable(container, accounts, user) {
    const totalBalance = accounts.reduce((sum, account) => sum + parseFloat(account.balance || 0), 0);
    const activeAccounts = accounts.filter(account => account.status === 'active').length;
    
    container.innerHTML = `
        <div class="table-responsive">
            <table class="table table-bordered table-hover">
                <thead class="table-light">
                    <tr>
                        <th>Account Number</th>
                        <th>Account Type</th>
                        <th>Owner</th>
                        <th>Relationship</th>
                        <th>Balance</th>
                        <th>Currency</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
                    ${accounts.map(account => `
                        <tr>
                            <td>
                                <span class="fw-bold">${escapeHtml(account.accountNumber || 'N/A')}</span>
                            </td>
                            <td>
                                <span class="badge bg-info">${escapeHtml(account.accountType || 'Standard')}</span>
                            </td>
                            <td>
                                <div>
                                    <div class="fw-semibold">${escapeHtml(account.ownerName)}</div>
                                    <small class="text-muted">${escapeHtml(account.ownerEmail)}</small>
                                </div>
                            </td>
                            <td>
                                <span class="badge ${account.ownerType === 'Primary Account Holder' ? 'bg-primary' : 'bg-secondary'}">
                                    ${escapeHtml(account.ownerType)}
                                </span>
                            </td>
                            <td>
                                <span class="fw-bold text-success">
                                    R${parseFloat(account.balance || 0).toFixed(2)}
                                </span>
                            </td>
                            <td>${escapeHtml(account.currency || 'ZAR')}</td>
                            <td>
                                <span class="badge ${account.status === 'active' ? 'bg-success' : 'bg-warning'}">
                                    ${escapeHtml(account.status || 'Unknown')}
                                </span>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
        
        <div class="mt-4">
            <div class="row">
                <div class="col-md-4">
                    <div class="card bg-primary text-white">
                        <div class="card-body text-center">
                            <h5 class="card-title">Total Balance</h5>
                            <h3 class="mb-0">R${totalBalance.toFixed(2)}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-success text-white">
                        <div class="card-body text-center">
                            <h5 class="card-title">Active Accounts</h5>
                            <h3 class="mb-0">${activeAccounts}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-info text-white">
                        <div class="card-body text-center">
                            <h5 class="card-title">Total Accounts</h5>
                            <h3 class="mb-0">${accounts.length}</h3>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}
