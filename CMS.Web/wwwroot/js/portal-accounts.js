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
    
    try {
        const response = await makeAuthenticatedRequest('/api/portal/me');

        if (response.ok) {
            const data = await response.json();
            const user = data.user;
            
            // Combine user accounts and dependent accounts
            const allAccounts = combineAllAccounts(user);
            
            if (allAccounts.length === 0) {
                container.innerHTML = '<p class="text-muted">No accounts found.</p>';
                return;
            }
            
            // Render accounts table
            renderAccountsTable(container, allAccounts, user);
            
        } else {
            throw new Error('Failed to load accounts data');
        }
    } catch (error) {
        console.error('Error loading accounts data:', error);
        container.innerHTML = '<p class="text-danger">Error loading accounts data</p>';
    }
}

/**
 * Combine user accounts and dependent accounts
 */
function combineAllAccounts(user) {
    let allAccounts = [];
    
    // Add user's own accounts
    if (user.accounts) {
        user.accounts.forEach(account => {
            allAccounts.push({
                ...account,
                ownerName: `${user.firstName} ${user.surname}`,
                ownerType: 'Primary Account Holder',
                ownerEmail: user.email
            });
        });
    }
    
    // Add dependent accounts
    if (user.Dependents && user.Dependents.length > 0) {
        user.Dependents.forEach(dependent => {
            if (dependent.accounts) {
                dependent.accounts.forEach(account => {
                    allAccounts.push({
                        ...account,
                        ownerName: `${dependent.firstName} ${dependent.surname}`,
                        ownerType: 'Dependent',
                        ownerEmail: dependent.email
                    });
                });
            }
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
