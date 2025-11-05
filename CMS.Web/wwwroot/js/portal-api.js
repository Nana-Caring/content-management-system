/**
 * Portal API Service
 * Complete integration with NANA Portal API endpoints
 * Based on API documentation dated November 5, 2025
 */

// API Configuration
const PortalAPI = {
    config: {
        baseUrl: 'https://nanacaring-backend.onrender.com',
        localBaseUrl: '', // Use same-origin for local API
        // Auto-detect: use local API if on localhost, otherwise use remote
        useLocal: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1',
        timeout: 30000, // 30 second timeout
        retryAttempts: 2
    },

    storage: {
        adminToken: 'admin-token',
        portalToken: 'portal-token',
        userEmail: 'portal-user-email',
        loginFlag: 'portal-logged-in',
        currentUser: 'portal-current-user'
    },

    endpoints: {
        // Admin endpoints
        adminLogin: '/api/auth/admin-login',
        getUsers: '/admin/users',
        
        // Portal endpoints
        portalLogin: '/api/portal/admin-login',
        portalMe: '/api/portal/me',
        portalAccounts: '/api/portal/me/accounts',
        portalTransactions: '/api/portal/me/transactions'
    }
};

/**
 * Get the appropriate base URL based on configuration
 */
function getBaseUrl() {
    return PortalAPI.config.useLocal ? PortalAPI.config.localBaseUrl : PortalAPI.config.baseUrl;
}

/**
 * Get stored admin token
 */
function getAdminToken() {
    return localStorage.getItem(PortalAPI.storage.adminToken);
}

/**
 * Get stored portal token
 */
function getPortalToken() {
    return localStorage.getItem(PortalAPI.storage.portalToken);
}

/**
 * Store admin token
 */
function storeAdminToken(token) {
    localStorage.setItem(PortalAPI.storage.adminToken, token);
}

/**
 * Store portal token and user info
 */
function storePortalToken(token, user) {
    localStorage.setItem(PortalAPI.storage.portalToken, token);
    localStorage.setItem(PortalAPI.storage.loginFlag, 'true');
    if (user) {
        localStorage.setItem(PortalAPI.storage.currentUser, JSON.stringify(user));
        if (user.email) {
            localStorage.setItem(PortalAPI.storage.userEmail, user.email);
        }
    }
}

/**
 * Clear all stored tokens and user data
 */
function clearAllTokens() {
    localStorage.removeItem(PortalAPI.storage.adminToken);
    localStorage.removeItem(PortalAPI.storage.portalToken);
    localStorage.removeItem(PortalAPI.storage.userEmail);
    localStorage.removeItem(PortalAPI.storage.loginFlag);
    localStorage.removeItem(PortalAPI.storage.currentUser);
}

/**
 * Make HTTP request with retry logic
 */
async function makeRequest(url, options = {}, retryCount = 0) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), PortalAPI.config.timeout);

    try {
        const response = await fetch(url, {
            ...options,
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        return response;
    } catch (error) {
        clearTimeout(timeoutId);
        
        // Check for CORS errors
        if (error.message && error.message.includes('CORS')) {
            console.error('🚫 CORS Error detected. Switching to local API mode...');
            PortalAPI.setUseLocal(true);
            throw new Error('CORS policy blocked the request. Please use local API mode or configure CORS on the server.');
        }
        
        // Check if it's a network error that might be CORS-related
        if (error.name === 'TypeError' && error.message.includes('Failed to fetch')) {
            console.warn('⚠️ Network error - possibly CORS related. Using local API is recommended for localhost.');
            // Don't retry CORS errors
            throw new Error('Network request failed. If running locally, try using local API mode: PortalAPI.setUseLocal(true)');
        }
        
        // Retry on timeout or other network errors
        if (retryCount < PortalAPI.config.retryAttempts && 
            (error.name === 'AbortError' || error.message.includes('fetch'))) {
            console.log(`Retry attempt ${retryCount + 1} for ${url}`);
            await new Promise(resolve => setTimeout(resolve, 1000 * (retryCount + 1)));
            return makeRequest(url, options, retryCount + 1);
        }
        
        throw error;
    }
}

/**
 * Parse response and handle errors
 */
async function parseResponse(response) {
    const contentType = response.headers.get('content-type');
    let data;

    if (contentType && contentType.includes('application/json')) {
        data = await response.json();
    } else {
        const text = await response.text();
        try {
            data = JSON.parse(text);
        } catch {
            data = { message: text };
        }
    }

    if (!response.ok) {
        const error = new Error(data.message || `HTTP Error ${response.status}`);
        error.status = response.status;
        error.data = data;
        throw error;
    }

    return data;
}

// ============================================================================
// ADMIN API METHODS
// ============================================================================

/**
 * Admin Login
 * Authenticate administrator and obtain admin token
 * 
 * @param {string} email - Admin email
 * @param {string} password - Admin password
 * @returns {Promise<Object>} - { accessToken, jwt, user }
 */
PortalAPI.adminLogin = async function(email, password) {
    const url = getBaseUrl() + this.endpoints.adminLogin;
    
    console.log('Admin login attempt:', { email, url });
    
    const response = await makeRequest(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ email, password })
    });

    const data = await parseResponse(response);
    
    // Store admin token
    const token = data.accessToken || data.jwt || data.token;
    if (token) {
        storeAdminToken(token);
    }

    console.log('Admin login successful');
    return data;
};

/**
 * Get Users List
 * Retrieve list of all users in the system
 * 
 * @param {Object} params - Query parameters { page, limit, search, role, status }
 * @returns {Promise<Object>} - { success, data: { users, total, page, pageCount, limit } }
 */
PortalAPI.getUsers = async function(params = {}) {
    const token = getAdminToken();
    if (!token) {
        throw new Error('Admin authentication required');
    }

    const queryParams = new URLSearchParams();
    if (params.page) queryParams.append('page', params.page);
    if (params.limit) queryParams.append('limit', params.limit);
    if (params.search) queryParams.append('search', params.search);
    if (params.role) queryParams.append('role', params.role);
    if (params.status) queryParams.append('status', params.status);

    const url = getBaseUrl() + this.endpoints.getUsers + 
                (queryParams.toString() ? '?' + queryParams.toString() : '');

    const response = await makeRequest(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    return await parseResponse(response);
};

// ============================================================================
// PORTAL API METHODS
// ============================================================================

/**
 * Portal Login (Admin as User)
 * Allow admin to log in as a specific user to access their portal view
 * 
 * @param {string} username - User email
 * @param {string} password - User password
 * @returns {Promise<Object>} - { token, user }
 */
PortalAPI.portalLogin = async function(username, password) {
    const url = getBaseUrl() + this.endpoints.portalLogin;
    
    console.log('Portal login attempt:', { username, url });
    
    const response = await makeRequest(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ 
            username: username,
            password: password,
            // Also try with 'email' field for compatibility
            email: username
        })
    });

    const data = await parseResponse(response);
    
    // Log the raw response to see what we're getting
    console.log('📥 Raw API response:', data);
    console.log('📋 Response keys:', Object.keys(data));
    
    // Normalize token field - check multiple possible field names
    const token = data.token || data.accessToken || data.jwt;
    
    if (!data.token && token) {
        // Normalize to 'token' field
        data.token = token;
        console.log('🔄 Normalized token from:', data.accessToken ? 'accessToken' : 'jwt');
    }
    
    // Normalize user object structure
    if (data.user) {
        // Attach accounts to user object if they exist at root level
        if (data.accounts && !data.user.accounts && !data.user.Accounts) {
            data.user.Accounts = data.accounts;
            console.log('✅ Attached', data.accounts.length, 'accounts to user object');
        }
        
        // Attach transactions if they exist at root level
        if (data.transactions && !data.recentTransactions) {
            data.recentTransactions = data.transactions;
            console.log('✅ Attached', data.transactions.length, 'transactions');
        }
    }
    
    // Store portal token and user info if we have both
    if (token && data.user) {
        // Use the new persistence manager for better reliability
        if (window.PortalPersistence) {
            window.PortalPersistence.saveSession(token, data.user);
        } else {
            // Fallback to old method
            storePortalToken(token, data.user);
        }
        
        console.log('✅ Portal login successful:', {
            userId: data.user.id,
            email: data.user.email,
            role: data.user.role,
            accountsCount: data.user.Accounts ? data.user.Accounts.length : 0,
            tokenStored: true
        });
    } else {
        console.error('❌ Portal login response validation failed:', {
            hasToken: !!token,
            hasAccessToken: !!data.accessToken,
            hasJwt: !!data.jwt,
            hasUser: !!data.user,
            responseKeys: Object.keys(data),
            fullResponse: data
        });
    }

    return data;
};

/**
 * Get Portal User Details
 * Get detailed information about the current portal user
 * 
 * @returns {Promise<Object>} - { user, recentTransactions }
 */
PortalAPI.getPortalUserDetails = async function() {
    const token = getPortalToken();
    if (!token) {
        throw new Error('Portal authentication required');
    }

    const url = getBaseUrl() + this.endpoints.portalMe;

    const response = await makeRequest(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    const data = await parseResponse(response);
    
    console.log('📥 Raw portal user details:', data);
    
    // Normalize the response structure
    // The API returns: { user, accounts, transactions }
    // We need to attach accounts and transactions to the user object for compatibility
    if (data.user) {
        // Attach accounts to user object if they exist at root level
        if (data.accounts && !data.user.accounts && !data.user.Accounts) {
            data.user.Accounts = data.accounts;
            console.log('✅ Attached accounts to user object:', data.accounts.length);
        }
        
        // Attach transactions if they exist at root level
        if (data.transactions && !data.recentTransactions) {
            data.recentTransactions = data.transactions;
            console.log('✅ Attached transactions:', data.transactions.length);
        }
        
        // Update stored user info with the enhanced user object
        localStorage.setItem(PortalAPI.storage.currentUser, JSON.stringify(data.user));
    }

    return data;
};

/**
 * Get Portal User Accounts
 * Get all accounts belonging to the portal user
 * 
 * @returns {Promise<Object>} - { accounts }
 */
PortalAPI.getPortalUserAccounts = async function() {
    const token = getPortalToken();
    if (!token) {
        throw new Error('Portal authentication required');
    }

    const url = getBaseUrl() + this.endpoints.portalAccounts;

    const response = await makeRequest(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    return await parseResponse(response);
};

/**
 * Get Portal User Transactions
 * Get transaction history for the portal user
 * 
 * @param {Object} params - Query parameters
 * @param {number} params.page - Page number (default: 1)
 * @param {number} params.limit - Items per page (default: 10, max: 100)
 * @param {string} params.accountId - Filter by account ID
 * @param {string} params.type - Filter by transaction type
 * @param {string} params.startDate - Filter from date (ISO format)
 * @param {string} params.endDate - Filter until date (ISO format)
 * @returns {Promise<Object>} - { transactions, pagination }
 */
PortalAPI.getPortalUserTransactions = async function(params = {}) {
    const token = getPortalToken();
    if (!token) {
        throw new Error('Portal authentication required');
    }

    const queryParams = new URLSearchParams();
    if (params.page) queryParams.append('page', params.page);
    if (params.limit) queryParams.append('limit', params.limit);
    if (params.accountId) queryParams.append('accountId', params.accountId);
    if (params.type) queryParams.append('type', params.type);
    if (params.startDate) queryParams.append('startDate', params.startDate);
    if (params.endDate) queryParams.append('endDate', params.endDate);

    const url = getBaseUrl() + this.endpoints.portalTransactions + 
                (queryParams.toString() ? '?' + queryParams.toString() : '');

    const response = await makeRequest(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    return await parseResponse(response);
};

// ============================================================================
// UTILITY METHODS
// ============================================================================

/**
 * Check if user is logged into portal
 */
PortalAPI.isPortalLoggedIn = function() {
    const flag = localStorage.getItem(this.storage.loginFlag);
    const token = getPortalToken();
    return flag === 'true' && token !== null;
};

/**
 * Check if admin is authenticated
 */
PortalAPI.isAdminAuthenticated = function() {
    return getAdminToken() !== null;
};

/**
 * Get current portal user from storage
 */
PortalAPI.getCurrentUser = function() {
    const userJson = localStorage.getItem(this.storage.currentUser);
    if (userJson) {
        try {
            return JSON.parse(userJson);
        } catch (error) {
            console.error('Error parsing stored user:', error);
            return null;
        }
    }
    return null;
};

/**
 * Logout from portal
 */
PortalAPI.logout = function() {
    clearAllTokens();
    console.log('Portal logout complete');
};

/**
 * Toggle between local and remote API
 */
PortalAPI.setUseLocal = function(useLocal) {
    this.config.useLocal = useLocal;
    console.log(`API mode set to: ${useLocal ? 'LOCAL' : 'REMOTE'}`);
};

/**
 * Get API health status
 */
PortalAPI.healthCheck = async function() {
    try {
        const url = getBaseUrl() + '/health';
        const response = await makeRequest(url, { method: 'GET' });
        return response.ok;
    } catch (error) {
        console.error('Health check failed:', error);
        return false;
    }
};

// Make PortalAPI globally available
window.PortalAPI = PortalAPI;

// Log initialization
console.log('🚀 Portal API Service initialized', {
    mode: PortalAPI.config.useLocal ? 'LOCAL (via proxy)' : 'REMOTE (direct)',
    baseUrl: getBaseUrl(),
    hostname: window.location.hostname,
    endpoint: PortalAPI.config.useLocal ? 'Same-origin API' : PortalAPI.config.baseUrl
});

// Warn if using remote from localhost (CORS will fail)
if (!PortalAPI.config.useLocal && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')) {
    console.warn('⚠️ Warning: Using remote API from localhost. CORS errors may occur. Consider using local mode.');
    console.log('💡 To switch to local mode: PortalAPI.setUseLocal(true)');
}
