/**
 * Portal Navigation Module
 * Handles section switching and navigation within the portal
 */

/**
 * Check portal login status and toggle sidebars
 */
function checkPortalLoginStatus() {
    // Use PortalPersistence for consistent authentication checking
    const isPortalLoggedIn = window.PortalPersistence?.isLoggedIn() || false;
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
        
        // Update portal user name using PortalPersistence
        const currentUser = window.PortalPersistence?.getCurrentUser();
        if (portalUserName && currentUser?.email) {
            portalUserName.textContent = currentUser.email;
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
    // Use PortalPersistence to clear session data
    if (window.PortalPersistence) {
        console.log('🚪 Logging out from portal...');
        window.PortalPersistence.clearSession();
    } else {
        console.warn('⚠️ PortalPersistence not available, clearing localStorage manually');
        // Fallback to manual cleanup
        const config = window.PORTAL_CONFIG || { 
            storage: { 
                loginFlag: 'portal-logged-in', 
                token: 'admin-token',
                userEmail: 'admin-user-email',
                userPassword: 'admin-user-password'
            } 
        };
        localStorage.removeItem(config.storage.loginFlag);
        localStorage.removeItem(config.storage.token);
        localStorage.removeItem(config.storage.userEmail);
        localStorage.removeItem(config.storage.userPassword);
    }
    
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
    if (event && event.target && event.target.classList) {
        event.target.classList.add('active');
        event.target.style.background = 'rgba(255,255,255,0.2)';
        event.target.style.color = '#fff';
    }
    
    // Hide all portal sections
    const portalSections = document.querySelectorAll('.portal-section');
    portalSections.forEach(sec => sec.style.display = 'none');
    
    // Show selected section - try both portal-${section} and ${section} IDs
    let targetSection = document.getElementById(`portal-${section}`) || document.getElementById(section);
    if (targetSection) {
        targetSection.style.display = 'block';
        console.log(`✅ Showing section: ${section}`);
        
        // Special handling for profile section
        if (section === 'profile' && typeof window.populateProfileWhenVisible === 'function') {
            console.log('👁️ Profile section shown, populating data...');
            setTimeout(() => {
                window.populateProfileWhenVisible();
            }, 100);
        }
    } else {
        console.warn(`⚠️ Section not found: ${section}`);
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
    // Check if user is logged in using PortalPersistence
    if (!window.PortalPersistence?.isLoggedIn()) {
        console.warn('⚠️ Not logged in, skipping data load for section:', section);
        return;
    }

    console.log('📂 Loading data for portal section:', section);

    switch(section) {
        case 'dashboard':
            if (typeof loadDashboardData === 'function') {
                loadDashboardData();
            }
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
            loadBeneficiariesData();
            break;
    }
}
