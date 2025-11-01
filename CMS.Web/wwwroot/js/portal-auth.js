/**
 * Portal Authentication Module
 * Handles login, logout, and authentication state management
 */

// API Configuration (guarded to avoid re-declaration if script is loaded twice)
window.PORTAL_CONFIG = window.PORTAL_CONFIG || {
    // Use same-origin API to avoid CORS and leverage local PortalController
    apiBaseUrl: '',
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
        const response = await fetch(window.PORTAL_CONFIG.apiBaseUrl + window.PORTAL_CONFIG.endpoints.adminLogin, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            // Server expects { Email, Password }
            body: JSON.stringify({ email: username, password: password })
        });
        
        let data, rawText;
        try {
            rawText = await response.text();
            data = JSON.parse(rawText);
        } catch {
            data = {};
        }
        
        if (response.ok && data && (data.token || data.accessToken || data.jwt)) {
            // Store credentials and token
            const tokenValue = data.token || data.accessToken || data.jwt;
            localStorage.setItem(window.PORTAL_CONFIG.storage.token, tokenValue);
            localStorage.setItem(window.PORTAL_CONFIG.storage.userEmail, username);
            localStorage.setItem(window.PORTAL_CONFIG.storage.userPassword, password);
            localStorage.setItem(window.PORTAL_CONFIG.storage.loginFlag, 'true');
            
            // Sync with Redux store auth system
            syncWithReduxAuth(tokenValue, username, data);
            
            // Close modal
            bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
            
            // Reload page to show portal sidebar
            window.location.reload();
        } else {
            let errorMsg = 'Login failed. ';
            
            // Try to parse the error message from the server
            try {
                const errorData = JSON.parse(rawText);
                if (errorData.message) {
                    errorMsg += errorData.message;
                } else {
                    errorMsg += 'Please check your credentials and try again.';
                }
            } catch {
                errorMsg += 'Please check your credentials and try again.';
            }
            
            // Show error in modal instead of alert
            showLoginError(errorMsg);
        }
    } catch (err) {
        showLoginError('Login failed. Please try again. Error: ' + err);
    }
}

/**
 * Check portal login status and toggle sidebars
 */
function checkPortalLoginStatus() {
    const isPortalLoggedIn = localStorage.getItem(window.PORTAL_CONFIG.storage.loginFlag) === 'true';
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
    const userEmail = localStorage.getItem(window.PORTAL_CONFIG.storage.userEmail);
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
    // Clear all authentication data (portal and Redux)
    clearAllAuth();
    
    // Reload page to show main sidebar
    window.location.reload();
}

/**
 * Get stored authentication token
 */
function getAuthToken() {
    // Try portal localStorage first
    let token = localStorage.getItem(window.PORTAL_CONFIG.storage.token);
    
    // If not found, try Redux sessionStorage
    if (!token) {
        try {
            const reduxAuth = sessionStorage.getItem('cms_auth');
            if (reduxAuth) {
                const authData = JSON.parse(reduxAuth);
                token = authData.token;
            }
        } catch (error) {
            console.error('Error parsing Redux auth data:', error);
        }
    }
    
    return token;
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
    
    return fetch(window.PORTAL_CONFIG.apiBaseUrl + endpoint, mergedOptions);
}

/**
 * Sync portal authentication with Redux store
 */
function syncWithReduxAuth(token, email, authData) {
    try {
        // Create Redux-compatible auth data
        const reduxAuthData = {
            user: {
                email: email,
                name: authData.name || email,
                role: authData.role || 'admin'
            },
            token: token,
            refreshToken: authData.refreshToken || null,
            expiresAt: authData.expiresAt || new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(), // 24 hours from now
            isAuthenticated: true
        };
        
        // Store in sessionStorage for Redux store
        sessionStorage.setItem('cms_auth', JSON.stringify(reduxAuthData));
        
        // Dispatch to Redux store if it exists
        if (window.CMS && window.CMS.store && window.CMS.authActions) {
            window.CMS.store.dispatch(window.CMS.authActions.loginSuccess(reduxAuthData));
        }
        
        console.log('Portal auth synced with Redux store');
    } catch (error) {
        console.error('Error syncing portal auth with Redux store:', error);
    }
}

/**
 * Clear both portal and Redux authentication
 */
function clearAllAuth() {
    // Clear portal auth
    localStorage.removeItem(window.PORTAL_CONFIG.storage.loginFlag);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.token);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.userEmail);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.userPassword);
    
    // Clear Redux auth
    sessionStorage.removeItem('cms_auth');
    
    // Dispatch logout to Redux store if it exists
    if (window.CMS && window.CMS.store && window.CMS.authActions) {
        window.CMS.store.dispatch(window.CMS.authActions.logout());
    }
}

/**
 * Initialize and sync authentication systems on page load
 */
function initializeAuthSync() {
    try {
        // Check if portal is logged in
        const isPortalLoggedIn = localStorage.getItem(window.PORTAL_CONFIG.storage.loginFlag) === 'true';
        const portalToken = localStorage.getItem(window.PORTAL_CONFIG.storage.token);
        const portalEmail = localStorage.getItem(window.PORTAL_CONFIG.storage.userEmail);
        
        // Check Redux auth
        const reduxAuthData = sessionStorage.getItem('cms_auth');
        
        if (isPortalLoggedIn && portalToken && !reduxAuthData) {
            // Portal is logged in but Redux store is empty - sync them
            console.log('Syncing portal auth to Redux store...');
            syncWithReduxAuth(portalToken, portalEmail, { token: portalToken });
        } else if (!isPortalLoggedIn && reduxAuthData) {
            // Redux has auth but portal doesn't - clear Redux
            console.log('Clearing orphaned Redux auth data...');
            sessionStorage.removeItem('cms_auth');
        }
    } catch (error) {
        console.error('Error during auth sync initialization:', error);
    }
}

/**
 * Show error message in the login modal
 */
function showLoginError(message) {
    // Remove any existing error alerts
    const existingError = document.getElementById('loginError');
    if (existingError) {
        existingError.remove();
    }
    
    // Create error element
    const errorDiv = document.createElement('div');
    errorDiv.id = 'loginError';
    errorDiv.className = 'alert alert-danger py-2 mb-3';
    errorDiv.style.cssText = 'border-left: 4px solid #dc3545; background: #f8d7da; border: 1px solid #f5c6cb;';
    
    const errorContent = document.createElement('small');
    errorContent.className = 'mb-0';
    errorContent.style.color = '#721c24';
    errorContent.innerHTML = `<i class="bi bi-exclamation-circle me-1"></i><strong>Error:</strong> ${message.replace(/\n/g, '<br>')}`;
    
    errorDiv.appendChild(errorContent);
    
    // Insert before the login button
    const loginForm = document.getElementById('loginForm');
    const loginButton = loginForm.querySelector('button[type="submit"]');
    if (loginButton) {
        loginForm.insertBefore(errorDiv, loginButton);
    }
    
    // Auto-remove after 8 seconds
    setTimeout(() => {
        if (errorDiv && errorDiv.parentNode) {
            errorDiv.remove();
        }
    }, 8000);
}

// Initialize auth sync when the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Small delay to ensure all systems are loaded
    setTimeout(initializeAuthSync, 100);
});
