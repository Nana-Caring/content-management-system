/**
 * Portal Beneficiaries Module
 * Handles beneficiaries data loading and management
 */

/**
 * Load beneficiaries data
 * Note: This is currently using mock data as no specific API endpoint was provided
 */
function loadBeneficiariesData() {
    const container = document.getElementById('beneficiariesContainer');
    if (!container) return;
    
    // Check authentication using PortalPersistence
    if (!window.PortalPersistence?.isLoggedIn()) {
        console.warn('🔒 Not authenticated for beneficiaries');
        container.innerHTML = `
            <div class="alert alert-warning">
                <i class="fas fa-lock me-2"></i>
                Please log in to view your beneficiaries.
            </div>
        `;
        return;
    }
    
    // Mock beneficiaries data for now
    // TODO: Replace with actual API call when endpoint is available
    const beneficiaries = [
        { 
            id: 1,
            name: 'Jane Smith', 
            account: '****9876', 
            bank: 'Standard Bank', 
            added: '2025-01-15',
            status: 'active',
            type: 'family'
        },
        { 
            id: 2,
            name: 'Mike Johnson', 
            account: '****5432', 
            bank: 'FNB', 
            added: '2025-02-20',
            status: 'active',
            type: 'friend'
        }
    ];
    
    renderBeneficiariesGrid(container, beneficiaries);
}

/**
 * Render beneficiaries grid
 */
function renderBeneficiariesGrid(container, beneficiaries) {
    if (beneficiaries.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-people" style="font-size: 3rem; color: #6c757d;"></i>
                <h5 class="mt-3 text-muted">No Beneficiaries Found</h5>
                <p class="text-muted">You haven't added any beneficiaries yet.</p>
                <button class="btn btn-primary" onclick="showAddBeneficiaryModal()">
                    <i class="bi bi-plus-lg me-2"></i>Add Your First Beneficiary
                </button>
            </div>
        `;
        return;
    }
    
    container.innerHTML = `
        <div class="row">
            ${beneficiaries.map(beneficiary => `
                <div class="col-md-6 col-lg-4 mb-4">
                    <div class="card h-100 shadow-sm">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <div class="flex-grow-1">
                                    <h5 class="card-title mb-1">${escapeHtml(beneficiary.name)}</h5>
                                    <p class="text-muted mb-0">${escapeHtml(beneficiary.bank)}</p>
                                </div>
                                <span class="badge ${beneficiary.status === 'active' ? 'bg-success' : 'bg-warning'}">
                                    ${escapeHtml(beneficiary.status)}
                                </span>
                            </div>
                            
                            <div class="mb-3">
                                <small class="text-muted d-block">Account Number</small>
                                <span class="fw-bold">${escapeHtml(beneficiary.account)}</span>
                            </div>
                            
                            <div class="mb-3">
                                <small class="text-muted d-block">Relationship</small>
                                <span class="badge bg-info">${escapeHtml(beneficiary.type)}</span>
                            </div>
                            
                            <div class="mb-3">
                                <small class="text-muted d-block">Added Date</small>
                                <span>${new Date(beneficiary.added).toLocaleDateString()}</span>
                            </div>
                        </div>
                        
                        <div class="card-footer bg-transparent border-top-0">
                            <div class="btn-group w-100" role="group">
                                <button type="button" class="btn btn-outline-primary btn-sm" 
                                        onclick="editBeneficiary(${beneficiary.id})">
                                    <i class="bi bi-pencil"></i> Edit
                                </button>
                                <button type="button" class="btn btn-outline-success btn-sm" 
                                        onclick="transferToBeneficiary(${beneficiary.id})">
                                    <i class="bi bi-arrow-right"></i> Transfer
                                </button>
                                <button type="button" class="btn btn-outline-danger btn-sm" 
                                        onclick="deleteBeneficiary(${beneficiary.id})">
                                    <i class="bi bi-trash"></i> Delete
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `).join('')}
        </div>
        
        <div class="mt-4 text-center">
            <button class="btn btn-primary" onclick="showAddBeneficiaryModal()">
                <i class="bi bi-plus-lg me-2"></i>Add New Beneficiary
            </button>
        </div>
    `;
}

/**
 * Show add beneficiary modal
 */
function showAddBeneficiaryModal() {
    // TODO: Implement add beneficiary modal
    alert('Add beneficiary functionality coming soon!');
}

/**
 * Edit beneficiary
 */
function editBeneficiary(beneficiaryId) {
    // TODO: Implement edit beneficiary functionality
    alert(`Edit beneficiary ${beneficiaryId} - functionality coming soon!`);
}

/**
 * Transfer to beneficiary
 */
function transferToBeneficiary(beneficiaryId) {
    // TODO: Implement transfer functionality
    alert(`Transfer to beneficiary ${beneficiaryId} - functionality coming soon!`);
}

/**
 * Delete beneficiary
 */
function deleteBeneficiary(beneficiaryId) {
    if (confirm('Are you sure you want to delete this beneficiary?')) {
        // TODO: Implement delete beneficiary functionality
        alert(`Delete beneficiary ${beneficiaryId} - functionality coming soon!`);
    }
}
