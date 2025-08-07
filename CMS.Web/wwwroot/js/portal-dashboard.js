/**
 * Portal Dashboard Module
 * Handles dashboard data loading and recent activity display
 */

/**
 * Load data for specific portal sections
 */
function loadPortalSectionData(section) {
    const token = localStorage.getItem('admin-token');
    if (!token) return;

    switch(section) {
        case 'dashboard':
            loadDashboardData();
            break;
        case 'profile':
            if (typeof loadProfileData === 'function') {
                loadProfileData();
            }
            break;
        case 'accounts':
            if (typeof loadAccountsData === 'function') {
                loadAccountsData();
            }
            break;
        case 'transactions':
            if (typeof loadTransactionsData === 'function') {
                loadTransactionsData();
            }
            break;
        case 'beneficiaries':
            if (typeof loadBeneficiariesData === 'function') {
                loadBeneficiariesData();
            }
            break;
    }
}

/**
 * Load dashboard data from API
 */
async function loadDashboardData() {
    try {
        const response = await makeAuthenticatedRequest('/api/portal/me');

        if (response.ok) {
            const data = await response.json();
            const user = data.user;
            const transactions = data.recentTransactions || [];
            
            // Calculate statistics
            const stats = calculateDashboardStats(user, transactions);
            
            // Update dashboard elements
            updateDashboardStats(stats);
            
            // Load recent activity
            loadRecentActivity(transactions, user);
        } else {
            throw new Error('Failed to load dashboard data');
        }
    } catch (error) {
        console.error('Error loading dashboard data:', error);
        showDashboardError();
    }
}

/**
 * Calculate dashboard statistics from user data
 */
function calculateDashboardStats(user, transactions) {
    let totalBalance = 0;
    let accountCount = 0;
    let dependentCount = 0;
    
    // Calculate balance from user accounts
    if (user.accounts) {
        user.accounts.forEach(account => {
            totalBalance += parseFloat(account.balance || 0);
            accountCount++;
        });
    }
    
    // Add dependent accounts
    if (user.Dependents) {
        dependentCount = user.Dependents.length;
        user.Dependents.forEach(dependent => {
            if (dependent.accounts) {
                dependent.accounts.forEach(account => {
                    totalBalance += parseFloat(account.balance || 0);
                    accountCount++;
                });
            }
        });
    }
    
    return {
        totalBalance,
        accountCount,
        dependentCount,
        transactionCount: transactions.length
    };
}

/**
 * Update dashboard statistics display
 */
function updateDashboardStats(stats) {
    const balanceElement = document.getElementById('accountBalance');
    const transactionsElement = document.getElementById('totalTransactions');
    const beneficiariesElement = document.getElementById('totalBeneficiaries');
    const accountsElement = document.getElementById('activeAccounts');
    
    if (balanceElement) balanceElement.textContent = `R${stats.totalBalance.toFixed(2)}`;
    if (transactionsElement) transactionsElement.textContent = stats.transactionCount;
    if (beneficiariesElement) beneficiariesElement.textContent = stats.dependentCount;
    if (accountsElement) accountsElement.textContent = stats.accountCount;
}

/**
 * Show dashboard error state
 */
function showDashboardError() {
    const elements = ['accountBalance', 'totalTransactions', 'totalBeneficiaries', 'activeAccounts'];
    elements.forEach(id => {
        const element = document.getElementById(id);
        if (element) element.textContent = 'Error';
    });
    
    const activityContainer = document.getElementById('recentActivity');
    if (activityContainer) {
        activityContainer.innerHTML = '<p class="text-danger">Error loading dashboard data</p>';
    }
}

/**
 * Load recent activity with real transaction data
 */
function loadRecentActivity(transactions = [], user = {}) {
    const container = document.getElementById('recentActivity');
    if (!container) return;
    
    if (transactions.length === 0) {
        container.innerHTML = '<p class="text-muted text-center">No recent activity found.</p>';
        return;
    }
    
    // Show most recent 5 transactions
    const recentTransactions = transactions.slice(0, 5);
    
    container.innerHTML = recentTransactions.map(transaction => `
        <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
            <div>
                <div class="fw-bold">${escapeHtml(transaction.description || 'Transaction')}</div>
                <small class="text-muted">
                    ${new Date(transaction.createdAt).toLocaleDateString()} • 
                    ${escapeHtml(transaction.reference || 'N/A')}
                </small>
            </div>
            <div class="text-end">
                <span class="fw-bold ${transaction.type === 'Credit' ? 'text-success' : 'text-primary'}">
                    ${transaction.type === 'Credit' ? '+' : ''}R${parseFloat(transaction.amount || 0).toFixed(2)}
                </span>
                <br>
                <small class="text-muted">
                    ${transaction.account ? escapeHtml(transaction.account.accountNumber) : 'N/A'}
                </small>
            </div>
        </div>
    `).join('');
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
