/**
 * Portal Transactions Module
 * Handles transaction data loading and display
 */

/**
 * Load transactions data from API
 */
async function loadTransactionsData() {
    const container = document.getElementById('transactionsContainer');
    if (!container) return;
    
    try {
        const response = await makeAuthenticatedRequest('/api/portal/me');

        if (response.ok) {
            const data = await response.json();
            const transactions = data.recentTransactions || [];
            
            if (transactions.length === 0) {
                container.innerHTML = '<p class="text-muted">No recent transactions found.</p>';
                return;
            }
            
            // Render transactions table
            renderTransactionsTable(container, transactions);
            
        } else {
            throw new Error('Failed to load transactions data');
        }
    } catch (error) {
        console.error('Error loading transactions data:', error);
        container.innerHTML = '<p class="text-danger">Error loading transactions data</p>';
    }
}

/**
 * Render transactions table
 */
function renderTransactionsTable(container, transactions) {
    // Calculate summary statistics
    const totalAmount = transactions.reduce((sum, t) => sum + parseFloat(t.amount || 0), 0);
    const creditTransactions = transactions.filter(t => t.type === 'Credit');
    const debitTransactions = transactions.filter(t => t.type !== 'Credit');
    const totalCredit = creditTransactions.reduce((sum, t) => sum + parseFloat(t.amount || 0), 0);
    const totalDebit = debitTransactions.reduce((sum, t) => sum + parseFloat(t.amount || 0), 0);
    
    container.innerHTML = `
        <!-- Summary Cards -->
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="card bg-primary text-white">
                    <div class="card-body text-center">
                        <h6 class="card-title">Total Transactions</h6>
                        <h4 class="mb-0">${transactions.length}</h4>
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
                <div class="card bg-warning text-white">
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
                        <h4 class="mb-0">R${(totalCredit - totalDebit).toFixed(2)}</h4>
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
                    ${transactions.map(transaction => `
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
                                    ${escapeHtml(transaction.reference || 'N/A')}
                                </span>
                            </td>
                            <td>
                                <div>${escapeHtml(transaction.description || 'No description')}</div>
                                ${transaction.metadata && transaction.metadata.source ? 
                                    `<small class="text-muted">Source: ${escapeHtml(transaction.metadata.source)}</small>` : 
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
                                <span class="badge ${transaction.type === 'Credit' ? 'bg-success' : 'bg-primary'}">
                                    ${escapeHtml(transaction.type || 'Unknown')}
                                </span>
                            </td>
                            <td>
                                <span class="fw-bold ${transaction.type === 'Credit' ? 'text-success' : 'text-primary'}">
                                    ${transaction.type === 'Credit' ? '+' : ''}R${parseFloat(transaction.amount || 0).toFixed(2)}
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
                    `).join('')}
                </tbody>
            </table>
        </div>
        
        ${transactions.length >= 10 ? `
            <div class="text-center mt-3">
                <small class="text-muted">Showing ${transactions.length} most recent transactions</small>
            </div>
        ` : ''}
    `;
}
