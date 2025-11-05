/**
 * Portal Transactions Module
 * Handles transaction data loading and display
 */

// Transaction filter state
let currentFilters = {
    page: 1,
    limit: 10,
    accountId: null,
    type: null,
    startDate: null,
    endDate: null
};

/**
 * Load transactions data from API
 */
async function loadTransactionsData(filters = {}) {
    const container = document.getElementById('transactionsContainer');
    if (!container) return;
    
    // Check authentication using PortalPersistence
    if (!window.PortalPersistence?.isLoggedIn()) {
        console.warn('🔒 Not authenticated for transactions');
        container.innerHTML = `
            <div class="alert alert-warning">
                <i class="fas fa-lock me-2"></i>
                Please log in to view your transactions.
            </div>
        `;
        return;
    }
    
    // Update current filters
    currentFilters = { ...currentFilters, ...filters };
    
    // Show loading state
    container.innerHTML = `
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-muted mt-2">Loading transactions...</p>
        </div>
    `;
    
    try {
        // Use PortalAPI to get transactions with filters
        const data = await window.PortalAPI.getPortalUserTransactions(currentFilters);
        
        const transactions = data.transactions || [];
        const pagination = data.pagination || null;
        
        if (transactions.length === 0) {
            container.innerHTML = `
                <div class="alert alert-info" role="alert">
                    <i class="bi bi-info-circle me-2"></i>
                    No transactions found.
                </div>
            `;
            return;
        }
        
        // Render transactions table with pagination
        renderTransactionsTable(container, transactions, pagination);
        
    } catch (error) {
        console.error('Error loading transactions data:', error);
        
        let errorMessage = 'Error loading transactions data';
        if (error.message.includes('authentication') || error.status === 401) {
            errorMessage = 'Authentication required. Please log in to view transactions.';
        } else if (error.message) {
            errorMessage = error.message;
        }
        
        container.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Error:</strong> ${errorMessage}
                <button onclick="loadTransactionsData()" class="btn btn-sm btn-outline-danger float-end">
                    <i class="bi bi-arrow-clockwise"></i> Retry
                </button>
            </div>
        `;
    }
}

/**
 * Render transactions table
 */
function renderTransactionsTable(container, transactions, pagination = null) {
    // Calculate summary statistics
    const totalAmount = transactions.reduce((sum, t) => sum + Math.abs(parseFloat(t.amount || 0)), 0);
    const creditTransactions = transactions.filter(t => parseFloat(t.amount || 0) > 0);
    const debitTransactions = transactions.filter(t => parseFloat(t.amount || 0) < 0);
    const totalCredit = creditTransactions.reduce((sum, t) => sum + parseFloat(t.amount || 0), 0);
    const totalDebit = Math.abs(debitTransactions.reduce((sum, t) => sum + parseFloat(t.amount || 0), 0));
    
    container.innerHTML = `
        <!-- Summary Cards -->
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="card bg-primary text-white">
                    <div class="card-body text-center">
                        <h6 class="card-title">Showing</h6>
                        <h4 class="mb-0">${transactions.length}</h4>
                        ${pagination ? `<small>of ${pagination.total} total</small>` : ''}
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-success text-white">
                    <div class="card-body text-center">
                        <h6 class="card-title">Total Credits</h6>
                        <h4 class="mb-0">R${totalCredit.toFixed(2)}</h4>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-danger text-white">
                    <div class="card-body text-center">
                        <h6 class="card-title">Total Debits</h6>
                        <h4 class="mb-0">R${totalDebit.toFixed(2)}</h4>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-info text-white">
                    <div class="card-body text-center">
                        <h6 class="card-title">Net Amount</h6>
                        <h4 class="mb-0 ${(totalCredit - totalDebit) >= 0 ? '' : 'text-warning'}">
                            R${(totalCredit - totalDebit).toFixed(2)}
                        </h4>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- Transactions Table -->
        <div class="table-responsive">
            <table class="table table-bordered table-hover">
                <thead class="table-light">
                    <tr>
                        <th>Date</th>
                        <th>Reference</th>
                        <th>Description</th>
                        <th>Account</th>
                        <th>Type</th>
                        <th>Amount</th>
                        <th>Category</th>
                    </tr>
                </thead>
                <tbody>
                    ${transactions.map(transaction => {
                        const amount = parseFloat(transaction.amount || 0);
                        const isCredit = amount > 0;
                        const displayType = transaction.type || (isCredit ? 'Credit' : 'Debit');
                        
                        return `
                        <tr>
                            <td>
                                <div class="fw-semibold">
                                    ${new Date(transaction.createdAt).toLocaleDateString()}
                                </div>
                                <small class="text-muted">
                                    ${new Date(transaction.createdAt).toLocaleTimeString()}
                                </small>
                            </td>
                            <td>
                                <span class="fw-bold text-primary">
                                    ${escapeHtml(transaction.reference || transaction.id || 'N/A')}
                                </span>
                            </td>
                            <td>
                                <div>${escapeHtml(transaction.description || 'No description')}</div>
                                ${transaction.metadata && transaction.metadata.merchant ? 
                                    `<small class="text-muted">Merchant: ${escapeHtml(transaction.metadata.merchant)}</small>` : 
                                    ''
                                }
                            </td>
                            <td>
                                ${transaction.account ? `
                                    <div class="fw-semibold">${escapeHtml(transaction.account.accountNumber)}</div>
                                    <small class="text-muted">${escapeHtml(transaction.account.accountType || 'Standard')}</small>
                                ` : '<span class="text-muted">N/A</span>'}
                            </td>
                            <td>
                                <span class="badge ${isCredit ? 'bg-success' : 'bg-danger'}">
                                    ${escapeHtml(displayType)}
                                </span>
                            </td>
                            <td>
                                <span class="fw-bold ${isCredit ? 'text-success' : 'text-danger'}">
                                    ${isCredit ? '+' : ''}R${Math.abs(amount).toFixed(2)}
                                </span>
                            </td>
                            <td>
                                ${transaction.metadata && transaction.metadata.category ? `
                                    <span class="badge bg-secondary">
                                        ${escapeHtml(transaction.metadata.category)}
                                    </span>
                                ` : '<span class="text-muted">-</span>'}
                            </td>
                        </tr>
                    `}).join('')}
                </tbody>
            </table>
        </div>
        
        ${pagination ? renderPagination(pagination) : ''}
    `;
}

/**
 * Render pagination controls
 */
function renderPagination(pagination) {
    const { page, totalPages, total, limit } = pagination;
    
    if (totalPages <= 1) return '';
    
    const pages = [];
    const maxVisiblePages = 5;
    let startPage = Math.max(1, page - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    
    if (endPage - startPage < maxVisiblePages - 1) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
    }
    
    return `
        <nav aria-label="Transaction pagination">
            <div class="d-flex justify-content-between align-items-center">
                <div class="text-muted">
                    Showing ${((page - 1) * limit) + 1} to ${Math.min(page * limit, total)} of ${total} transactions
                </div>
                <ul class="pagination mb-0">
                    <li class="page-item ${page === 1 ? 'disabled' : ''}">
                        <a class="page-link" href="#" onclick="loadTransactionsData({ page: ${page - 1} }); return false;">
                            <i class="bi bi-chevron-left"></i> Previous
                        </a>
                    </li>
                    
                    ${startPage > 1 ? `
                        <li class="page-item">
                            <a class="page-link" href="#" onclick="loadTransactionsData({ page: 1 }); return false;">1</a>
                        </li>
                        ${startPage > 2 ? '<li class="page-item disabled"><span class="page-link">...</span></li>' : ''}
                    ` : ''}
                    
                    ${pages.map(p => `
                        <li class="page-item ${p === page ? 'active' : ''}">
                            <a class="page-link" href="#" onclick="loadTransactionsData({ page: ${p} }); return false;">${p}</a>
                        </li>
                    `).join('')}
                    
                    ${endPage < totalPages ? `
                        ${endPage < totalPages - 1 ? '<li class="page-item disabled"><span class="page-link">...</span></li>' : ''}
                        <li class="page-item">
                            <a class="page-link" href="#" onclick="loadTransactionsData({ page: ${totalPages} }); return false;">${totalPages}</a>
                        </li>
                    ` : ''}
                    
                    <li class="page-item ${page === totalPages ? 'disabled' : ''}">
                        <a class="page-link" href="#" onclick="loadTransactionsData({ page: ${page + 1} }); return false;">
                            Next <i class="bi bi-chevron-right"></i>
                        </a>
                    </li>
                </ul>
            </div>
        </nav>
    `;
}

/**
 * Helper function to escape HTML
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

