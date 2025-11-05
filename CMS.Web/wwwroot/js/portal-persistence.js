/**
 * Portal Data Persistence Manager
 * Handles saving and loading portal data across page reloads
 */

window.PortalPersistence = {
    // Storage keys
    KEYS: {
        TOKEN: 'portal-token',
        USER: 'portal-current-user',
        EMAIL: 'portal-user-email',
        LOGGED_IN: 'portal-logged-in',
        LAST_SECTION: 'portal-last-section'
    },

    /**
     * Save complete portal session data
     */
    saveSession(token, user) {
        try {
            console.log('💾 Saving portal session data...');
            
            localStorage.setItem(this.KEYS.TOKEN, token);
            localStorage.setItem(this.KEYS.USER, JSON.stringify(user));
            localStorage.setItem(this.KEYS.EMAIL, user.email);
            localStorage.setItem(this.KEYS.LOGGED_IN, 'true');
            
            console.log('✅ Portal session saved:', {
                userId: user.id,
                email: user.email,
                accountsCount: user.Accounts?.length || 0,
                tokenLength: token.length
            });
            
            return true;
        } catch (error) {
            console.error('❌ Failed to save portal session:', error);
            return false;
        }
    },

    /**
     * Load portal session data
     */
    loadSession() {
        try {
            const token = localStorage.getItem(this.KEYS.TOKEN);
            const userStr = localStorage.getItem(this.KEYS.USER);
            const isLoggedIn = localStorage.getItem(this.KEYS.LOGGED_IN) === 'true';
            
            if (!token || !userStr || !isLoggedIn) {
                console.log('ℹ️ No valid portal session found');
                return null;
            }
            
            const user = JSON.parse(userStr);
            
            console.log('✅ Portal session loaded:', {
                userId: user.id,
                email: user.email,
                accountsCount: user.Accounts?.length || 0,
                hasToken: !!token
            });
            
            return { token, user };
        } catch (error) {
            console.error('❌ Failed to load portal session:', error);
            return null;
        }
    },

    /**
     * Check if user is logged in
     */
    isLoggedIn() {
        return localStorage.getItem(this.KEYS.LOGGED_IN) === 'true' && 
               localStorage.getItem(this.KEYS.TOKEN) !== null;
    },

    /**
     * Get current user data
     */
    getCurrentUser() {
        try {
            const userStr = localStorage.getItem(this.KEYS.USER);
            return userStr ? JSON.parse(userStr) : null;
        } catch (error) {
            console.error('❌ Failed to get current user:', error);
            return null;
        }
    },

    /**
     * Save last visited section
     */
    saveLastSection(section) {
        localStorage.setItem(this.KEYS.LAST_SECTION, section);
    },

    /**
     * Get last visited section
     */
    getLastSection() {
        return localStorage.getItem(this.KEYS.LAST_SECTION) || 'dashboard';
    },

    /**
     * Clear all portal data
     */
    clearSession() {
        console.log('🧹 Clearing portal session...');
        
        Object.values(this.KEYS).forEach(key => {
            localStorage.removeItem(key);
        });
        
        console.log('✅ Portal session cleared');
    },

    /**
     * Initialize portal UI based on stored session
     */
    initializePortalUI() {
        console.log('🚀 Initializing portal UI...');
        
        if (!this.isLoggedIn()) {
            console.log('ℹ️ User not logged in - showing CMS UI');
            this.showCMSUI();
            return false;
        }
        
        console.log('✅ User logged in - showing portal UI');
        this.showPortalUI();
        
        // Load last visited section or default to dashboard
        const lastSection = this.getLastSection();
        console.log('📂 Loading section:', lastSection);
        
        if (typeof window.showPortalSection === 'function') {
            window.showPortalSection(lastSection);
        }
        
        // Load data for ALL sections to ensure they're ready when user clicks
        console.log('🔄 Preloading data for ALL sections...');
        this.loadSectionData('dashboard');
        this.loadSectionData('profile');
        this.loadSectionData('accounts');
        this.loadSectionData('transactions');
        this.loadSectionData('beneficiaries');
        
        return true;
    },

    /**
     * Show portal UI elements
     */
    showPortalUI() {
        const mainSidebar = document.getElementById('mainSidebar');
        const portalSidebar = document.getElementById('portalSidebar');
        const cmsContent = document.getElementById('cmsContent');
        const portalContent = document.getElementById('portalContent');
        const portalUserName = document.getElementById('portalUserName');
        
        // Toggle sidebars
        if (mainSidebar) mainSidebar.style.display = 'none';
        if (portalSidebar) portalSidebar.style.display = 'block';
        
        // Toggle content areas
        if (cmsContent) cmsContent.style.display = 'none';
        if (portalContent) portalContent.style.display = 'block';
        
        // Update user name
        const user = this.getCurrentUser();
        if (portalUserName && user) {
            const displayName = user.firstName && user.surname 
                ? `${user.firstName} ${user.surname}` 
                : user.email;
            portalUserName.textContent = displayName;
        }
        
        console.log('✅ Portal UI elements shown');
    },

    /**
     * Show CMS UI elements
     */
    showCMSUI() {
        const mainSidebar = document.getElementById('mainSidebar');
        const portalSidebar = document.getElementById('portalSidebar');
        const cmsContent = document.getElementById('cmsContent');
        const portalContent = document.getElementById('portalContent');
        
        // Toggle sidebars
        if (mainSidebar) mainSidebar.style.display = 'block';
        if (portalSidebar) portalSidebar.style.display = 'none';
        
        // Toggle content areas
        if (cmsContent) cmsContent.style.display = 'block';
        if (portalContent) portalContent.style.display = 'none';
        
        console.log('✅ CMS UI elements shown');
    },

    /**
     * Load data for specific portal section
     */
    loadSectionData(section) {
        if (!this.isLoggedIn()) {
            console.warn('⚠️ Not logged in, skipping data load');
            return;
        }

        console.log('📊 Loading data for section:', section);

        // Add small delay to ensure DOM is ready
        setTimeout(() => {
            switch(section) {
                case 'dashboard':
                    if (typeof window.loadDashboardData === 'function') {
                        console.log('📈 Calling loadDashboardData...');
                        window.loadDashboardData();
                    } else {
                        console.warn('⚠️ loadDashboardData function not found');
                    }
                    break;
                    
                case 'profile':
                    if (typeof window.loadProfileData === 'function') {
                        console.log('👤 Calling loadProfileData...');
                        window.loadProfileData();
                    }
                    break;
                    
                case 'accounts':
                    if (typeof window.loadAccountsData === 'function') {
                        console.log('💳 Calling loadAccountsData...');
                        window.loadAccountsData();
                    }
                    break;
                    
                case 'transactions':
                    if (typeof window.loadTransactionsData === 'function') {
                        console.log('💰 Calling loadTransactionsData...');
                        window.loadTransactionsData();
                    }
                    break;
                    
                default:
                    console.log('ℹ️ No data loader for section:', section);
            }
        }, 150);
    }
};

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    setTimeout(() => {
        window.PortalPersistence.initializePortalUI();
    }, 100);
});

console.log('🎯 Portal Persistence Manager loaded');