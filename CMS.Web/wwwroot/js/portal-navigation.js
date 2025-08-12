/**
 * Portal Navigation Module
 * Handles section switching and navigation within the portal
 */

/**
 * Check portal login status and toggle sidebars
 */
function checkPortalLoginStatus() {
    const isPortalLoggedIn = localStorage.getItem('portal-logged-in') === 'true';
    const mainSidebar = document.getElementById('mainSidebar');
    const portalSidebar = document.getElementById('portalSidebar');
    const portalUserName = document.getElementById('portalUserName');
    const cmsContent = document.getElementById('cmsContent');
    const portalContent = document.getElementById('portalContent');
    
    if (isPortalLoggedIn) {
        // Hide main sidebar, show portal sidebar
        if (mainSidebar) mainSidebar.style.display = 'none';
        if (portalSidebar) portalSidebar.style.display = 'block';
        
        // Hide CMS content, show portal content
        if (cmsContent) cmsContent.style.display = 'none';
        if (portalContent) portalContent.style.display = 'block';
        
        // Update portal user name
        const userEmail = localStorage.getItem('admin-user-email');
        if (portalUserName && userEmail) {
            portalUserName.textContent = userEmail;
        }
        
        // Show dashboard section by default
        showPortalSection('dashboard');
    } else {
        // Show main sidebar, hide portal sidebar
        if (mainSidebar) mainSidebar.style.display = 'block';
        if (portalSidebar) portalSidebar.style.display = 'none';
        
        // Show CMS content, hide portal content
        if (cmsContent) cmsContent.style.display = 'block';
        if (portalContent) portalContent.style.display = 'none';
    }
}

/**
 * Logout from portal
 */
function logoutFromPortal() {
    // Clear portal login flag
    localStorage.removeItem('portal-logged-in');
    localStorage.removeItem('admin-token');
    localStorage.removeItem('admin-user-email');
    localStorage.removeItem('admin-user-password');
    
    // Reload page to show main sidebar
    window.location.reload();
}

/**
 * Portal section navigation
 */
function showPortalSection(section) {
    // Remove active class from all portal nav buttons
    const portalNavButtons = document.querySelectorAll('.portal-nav-btn');
    portalNavButtons.forEach(btn => {
        btn.classList.remove('active');
        btn.style.background = 'transparent';
        btn.style.color = 'rgba(255,255,255,0.9)';
    });
    
    // Add active class to clicked button
    if (event && event.target) {
        event.target.classList.add('active');
        event.target.style.background = 'rgba(255,255,255,0.2)';
        event.target.style.color = '#fff';
    }
    
    // Hide all portal sections
    const portalSections = document.querySelectorAll('.portal-section');
    portalSections.forEach(sec => sec.style.display = 'none');
    
    // Show selected section
    const targetSection = document.getElementById(`portal-${section}`);
    if (targetSection) {
        targetSection.style.display = 'block';
    }
    
    // If no event (called programmatically), highlight the correct button
    if (!event) {
        const targetButton = document.querySelector(`[onclick*="${section}"]`);
        if (targetButton) {
            targetButton.classList.add('active');
            targetButton.style.background = 'rgba(255,255,255,0.2)';
            targetButton.style.color = '#fff';
        }
    }
    
    // Load section-specific data
    loadPortalSectionData(section);
}

/**
 * Load data for specific portal sections
 */
function loadPortalSectionData(section) {
    const token = getAuthToken();
    if (!token) return;

    switch(section) {
        case 'dashboard':
            loadDashboardData();
            break;
        case 'profile':
            loadProfileData();
            break;
        case 'accounts':
            loadAccountsData();
            break;
        case 'transactions':
            loadTransactionsData();
            break;
        case 'beneficiaries':
            loadBeneficiariesData();
            break;
    }
}
