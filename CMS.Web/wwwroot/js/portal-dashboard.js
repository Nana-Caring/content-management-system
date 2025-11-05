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
    console.log('🎯 Loading dashboard data...');
    
    // Check if PortalAPI is available
    if (!window.PortalAPI) {
        console.error('❌ PortalAPI not available!');
        showDashboardError('Portal API not loaded. Please refresh the page.');
        return;
    }
    
    // Check if portal token exists
    const portalToken = localStorage.getItem('portal-token');
    console.log('🔑 Portal token exists:', !!portalToken);
    
    if (!portalToken) {
        console.warn('⚠️ No portal token found - user needs to login');
        showDashboardAuthError();
        return;
    }
    
    try {
        // Show loading state
        showDashboardLoading();
        
        console.log('📦 Loading user data from login response cache...');
        
        // Get user data from login response (cached by PortalPersistence)
        const user = window.PortalPersistence?.getCurrentUser();
        
        if (!user || !user.Accounts) {
            console.error('❌ No user data or accounts found in cache');
            throw new Error('User data not found. Please log in again.');
        }
        
        console.log('📥 User data loaded from cache:', {
            userId: user.id,
            email: user.email,
            firstName: user.firstName,
            surname: user.surname,
            accountCount: user.Accounts.length
        });
        
        console.log('💳 Accounts loaded from cache:', user.Accounts);
        
        // User data already includes accounts (attached during login)
        const data = { user };
        
        console.log('📋 Data keys:', Object.keys(data));
        
        if (data && data.user) {
            const transactions = data.recentTransactions || [];
            
            console.log('✅ User data valid:', {
                userId: user.id,
                email: user.email,
                accountCount: user.Accounts ? user.Accounts.length : 0,
                transactionCount: transactions.length
            });
            
            // Calculate statistics
            const stats = calculateDashboardStats(user, transactions);
            
            // Update dashboard elements
            updateDashboardStats(stats);
            
            // Load recent activity
            loadRecentActivity(transactions, user);
            
            console.log('✅ Dashboard loaded successfully');
        } else {
            console.error('❌ Invalid response data:', data);
            throw new Error('Invalid response data - missing user object');
        }
    } catch (error) {
        console.error('❌ Error loading dashboard data:', error);
        console.error('Error details:', {
            message: error.message,
            status: error.status,
            stack: error.stack
        });
        
        // Check if it's an authentication error
        if (error.message.includes('authentication') || error.message.includes('log in again') || error.status === 401) {
            showDashboardAuthError();
        } else {
            showDashboardError(error.message);
        }
    }
}

/**
 * Show loading state for dashboard
 */
function showDashboardLoading() {
    const elements = ['accountBalance', 'totalTransactions', 'totalBeneficiaries', 'activeAccounts'];
    elements.forEach(id => {
        const element = document.getElementById(id);
        if (element) element.innerHTML = '<i class="bi bi-hourglass-split"></i>';
    });
    
    const activityContainer = document.getElementById('recentActivity');
    if (activityContainer) {
        activityContainer.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="text-muted mt-2">Loading dashboard data...</p>
            </div>
        `;
    }
}

/**
 * Calculate dashboard statistics from user data
 */
function calculateDashboardStats(user, transactions) {
    let totalBalance = 0;
    let accountCount = 0;
    let dependentCount = 0;
    
    // Calculate balance from user accounts (handle both 'Accounts' and 'accounts')
    const userAccounts = user.Accounts || user.accounts || [];
    userAccounts.forEach(account => {
        totalBalance += parseFloat(account.balance || 0);
        accountCount++;
    });
    
    // Add dependent accounts (handle both 'Dependents' and 'dependents')
    const userDependents = user.Dependents || user.dependents || [];
    dependentCount = userDependents.length;
    
    userDependents.forEach(dependent => {
        const dependentAccounts = dependent.Accounts || dependent.accounts || [];
        dependentAccounts.forEach(account => {
            totalBalance += parseFloat(account.balance || 0);
            accountCount++;
        });
    });
    
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
function showDashboardError(errorMessage = 'Error loading dashboard data') {
    const elements = ['accountBalance', 'totalTransactions', 'totalBeneficiaries', 'activeAccounts'];
    elements.forEach(id => {
        const element = document.getElementById(id);
        if (element) element.innerHTML = '<span class="text-danger">--</span>';
    });
    
    const activityContainer = document.getElementById('recentActivity');
    if (activityContainer) {
        activityContainer.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Error:</strong> ${errorMessage}
                <button onclick="loadDashboardData()" class="btn btn-sm btn-outline-danger float-end">
                    <i class="bi bi-arrow-clockwise"></i> Retry
                </button>
            </div>
        `;
    }
}

/**
 * Show authentication error state
 */
function showDashboardAuthError() {
    const activityContainer = document.getElementById('recentActivity');
    if (activityContainer) {
        activityContainer.innerHTML = `
            <div class="alert alert-warning" role="alert">
                <i class="bi bi-shield-exclamation me-2"></i>
                <strong>Authentication Required:</strong> Please log in to view your dashboard.
                <button onclick="showLoginModal()" class="btn btn-sm btn-outline-warning float-end">
                    <i class="bi bi-box-arrow-in-right"></i> Login
                </button>
            </div>
        `;
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
