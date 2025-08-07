/**
 * Portal Authentication Module
 * Handles login, logout, and authentication state management
 */

// API Configuration
const PORTAL_CONFIG = {
    apiBaseUrl: 'https://nanacaring-backend.onrender.com',
    endpoints: {
        adminLogin: '/api/portal/admin-login',
        userProfile: '/api/portal/me'
    },
    storage: {
        token: 'admin-token',
        userEmail: 'admin-user-email',
        userPassword: 'admin-user-password',
        loginFlag: 'portal-logged-in'
    }
};

/**
 * Show the portal login modal
 */
function showLoginModal() {
    const modal = new bootstrap.Modal(document.getElementById('loginModal'));
    modal.show();
}

/**
 * Handle portal login form submission
 */
function initializePortalAuth() {
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handlePortalLogin);
    }
}

/**
 * Handle login form submit
 */
async function handlePortalLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    if (!username || !password) {
        alert('Please enter both username and password');
        return;
    }
    
    try {
        const response = await fetch(PORTAL_CONFIG.apiBaseUrl + PORTAL_CONFIG.endpoints.adminLogin, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username: username, password: password })
        });
        
        let data, rawText;
        try {
            rawText = await response.text();
            data = JSON.parse(rawText);
        } catch {
            data = {};
        }
        
        if (response.ok && data && data.token) {
            // Store credentials and token
            localStorage.setItem(PORTAL_CONFIG.storage.token, data.token);
            localStorage.setItem(PORTAL_CONFIG.storage.userEmail, username);
            localStorage.setItem(PORTAL_CONFIG.storage.userPassword, password);
            localStorage.setItem(PORTAL_CONFIG.storage.loginFlag, 'true');
            
            // Close modal
            bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
            
            // Reload page to show portal sidebar
            window.location.reload();
        } else {
            let errorMsg = 'Login failed.';
            errorMsg += '\nStatus: ' + response.status + ' ' + response.statusText;
            errorMsg += '\nResponse: ' + rawText;
            alert(errorMsg);
        }
    } catch (err) {
        alert('Login failed. Please try again. Error: ' + err);
    }
}

/**
 * Check portal login status and toggle sidebars
 */
function checkPortalLoginStatus() {
    const isPortalLoggedIn = localStorage.getItem(PORTAL_CONFIG.storage.loginFlag) === 'true';
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
        const userEmail = localStorage.getItem(PORTAL_CONFIG.storage.userEmail);
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
    // Clear portal login flag and stored data
    localStorage.removeItem(PORTAL_CONFIG.storage.loginFlag);
    localStorage.removeItem(PORTAL_CONFIG.storage.token);
    localStorage.removeItem(PORTAL_CONFIG.storage.userEmail);
    localStorage.removeItem(PORTAL_CONFIG.storage.userPassword);
    
    // Reload page to show main sidebar
    window.location.reload();
}

/**
 * Get stored authentication token
 */
function getAuthToken() {
    return localStorage.getItem(PORTAL_CONFIG.storage.token);
}

/**
 * Make authenticated API request
 */
async function makeAuthenticatedRequest(endpoint, options = {}) {
    const token = getAuthToken();
    if (!token) {
        throw new Error('No authentication token found');
    }
    
    const defaultOptions = {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    };
    
    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...(options.headers || {})
        }
    };
    
    return fetch(PORTAL_CONFIG.apiBaseUrl + endpoint, mergedOptions);
}
