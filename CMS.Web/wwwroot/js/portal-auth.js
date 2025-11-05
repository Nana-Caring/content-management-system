/**
 * Portal Authentication Module
 * Handles login, logout, and authentication state management
 */

// API Configuration (guarded to avoid re-declaration if script is loaded twice)
window.PORTAL_CONFIG = window.PORTAL_CONFIG || {
    // Use same-origin API to avoid CORS and leverage local PortalController
    apiBaseUrl: '',
    endpoints: {
        userLogin: '/api/portal/admin-login', // Endpoint accepts any user credentials
        userProfile: '/api/portal/me'
    },
    storage: {
        token: 'portal-token',
        userEmail: 'portal-user-email',
        userPassword: 'portal-user-password',
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
    
    // Prevent double submission
    if (window.portalLoginInProgress) {
        console.log('⚠️ Login already in progress, ignoring duplicate submission');
        return;
    }
    
    const username = document.getElementById('modal-username').value;
    const password = document.getElementById('modal-password').value;
    
    if (!username || !password) {
        showLoginError('Please enter both username and password');
        return;
    }
    
    // Check if PortalAPI is available
    if (!window.PortalAPI || typeof window.PortalAPI.portalLogin !== 'function') {
        console.error('PortalAPI is not loaded. Make sure portal-api.js is loaded before portal-auth.js');
        showLoginError('Portal API service is not available. Please refresh the page and try again.');
        return;
    }
    
    // Set flag to prevent double submission
    window.portalLoginInProgress = true;
    
    // Show loading state
    const submitButton = e.target.querySelector('button[type="submit"]');
    const originalButtonText = submitButton.innerHTML;
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="bi bi-hourglass-split"></i> Logging in...';
    
    try {
        // Use PortalAPI service for login
        const data = await window.PortalAPI.portalLogin(username, password);
        
        // Check for token in various possible fields (token, accessToken, jwt)
        const token = data.token || data.accessToken || data.jwt;
        const user = data.user;
        
        if (token && user) {
            console.log('Portal login successful:', {
                username: username,
                userId: user.id,
                role: user.role,
                tokenLength: token.length
            });
            
            // If token wasn't in 'token' field, normalize it
            if (!data.token) {
                data.token = token;
            }
            
            // Store password for compatibility (if needed)
            localStorage.setItem(window.PORTAL_CONFIG.storage.userPassword, password);
            
            // No need to sync with Redux - portal uses its own auth system
            // The PortalAPI already stores the token and user data
            
            console.log('✅ Login complete - waiting for data to persist...');
            
            // Close modal (but don't wait for it)
            try {
                const modal = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
                if (modal) modal.hide();
            } catch (e) {
                console.log('Modal close skipped:', e.message);
            }
            
            // Verify data is saved before reloading using PortalPersistence
            const isLoggedIn = window.PortalPersistence?.isLoggedIn() || false;
            const currentUser = window.PortalPersistence?.getCurrentUser();
            console.log('🔍 Verifying saved data:', {
                isLoggedIn: isLoggedIn,
                hasUser: !!currentUser,
                userHasAccounts: currentUser?.Accounts?.length || 0
            });
            
            // Use the login response data directly - NO API CALLS!
            console.log('🎯 Using login response data directly...');
            
            // Show portal UI
            const mainSidebar = document.getElementById('mainSidebar');
            const portalSidebar = document.getElementById('portalSidebar');
            const cmsContent = document.getElementById('cmsContent');
            const portalContent = document.getElementById('portalContent');
            
            if (mainSidebar) mainSidebar.style.display = 'none';
            if (portalSidebar) portalSidebar.style.display = 'block';
            if (cmsContent) cmsContent.style.display = 'none';
            if (portalContent) portalContent.style.display = 'block';
            
            // Show dashboard and populate ALL sections with response data
            showPortalSection('dashboard');
            
            // Use the login response data (user + accounts) directly
            const responseData = data; // This is the login response {user, accounts, token}
            console.log('📊 Populating ALL sections with response data:', responseData);
            
            // FORCE LOAD ALL SECTIONS IMMEDIATELY - NO DELAYS!
            console.log('� FORCING ALL SECTIONS TO LOAD NOW!!!');
            
            // Dashboard - IMMEDIATE
            if (typeof updateDashboardData === 'function' && responseData.user?.Accounts) {
                console.log('📈 LOADING DASHBOARD NOW...');
                updateDashboardData(responseData.user);
            }
            if (typeof loadDashboardData === 'function') {
                console.log('📈 CALLING loadDashboardData NOW...');
                loadDashboardData();
            }
            
            // Profile - DIRECT POPULATION FROM LOGIN RESPONSE!
            console.log('👤 POPULATING PROFILE DIRECTLY...');
            if (responseData.user) {
                const user = responseData.user;
                console.log('📝 Profile data:', user);
                
                // Directly set profile form fields using ACTUAL element IDs from _Layout.cshtml
                try {
                    const firstNameEl = document.getElementById('profile-firstName');
                    const middleNameEl = document.getElementById('profile-middleName');
                    const surnameEl = document.getElementById('profile-surname');
                    const emailEl = document.getElementById('profile-email');
                    const phoneEl = document.getElementById('profile-phoneNumber');
                    
                    console.log('🔍 Profile elements found:', {
                        firstName: !!firstNameEl,
                        middleName: !!middleNameEl,
                        surname: !!surnameEl,
                        email: !!emailEl,
                        phone: !!phoneEl
                    });
                    
                    if (firstNameEl) {
                        firstNameEl.value = user.firstName || '';
                        console.log('✅ First name set:', user.firstName);
                    } else {
                        console.error('❌ profile-firstName element not found!');
                    }
                    
                    if (middleNameEl) {
                        middleNameEl.value = user.middleName || '';
                        console.log('✅ Middle name set:', user.middleName);
                    }
                    
                    if (surnameEl) {
                        surnameEl.value = user.surname || '';
                        console.log('✅ Surname set:', user.surname);
                    } else {
                        console.error('❌ profile-surname element not found!');
                    }
                    
                    if (emailEl) {
                        emailEl.value = user.email || '';
                        console.log('✅ Email set:', user.email);
                    } else {
                        console.error('❌ profile-email element not found!');
                    }
                    
                    if (phoneEl) {
                        phoneEl.value = user.phoneNumber || '';
                        console.log('✅ Phone set:', user.phoneNumber);
                    }
                    
                    // Set address fields if available
                    const postalLine1El = document.getElementById('profile-postalAddressLine1');
                    const postalLine2El = document.getElementById('profile-postalAddressLine2');
                    const postalCityEl = document.getElementById('profile-postalCity');
                    const postalProvinceEl = document.getElementById('profile-postalProvince');
                    const postalCodeEl = document.getElementById('profile-postalCode');
                    
                    if (postalLine1El && user.postalAddressLine1) {
                        postalLine1El.value = user.postalAddressLine1;
                        console.log('✅ Postal address line 1 set:', user.postalAddressLine1);
                    }
                    if (postalLine2El && user.postalAddressLine2) {
                        postalLine2El.value = user.postalAddressLine2;
                        console.log('✅ Postal address line 2 set:', user.postalAddressLine2);
                    }
                    if (postalCityEl && user.postalCity) {
                        postalCityEl.value = user.postalCity;
                        console.log('✅ Postal city set:', user.postalCity);
                    }
                    if (postalProvinceEl && user.postalProvince) {
                        postalProvinceEl.value = user.postalProvince;
                        console.log('✅ Postal province set:', user.postalProvince);
                    }
                    if (postalCodeEl && user.postalCode) {
                        postalCodeEl.value = user.postalCode;
                        console.log('✅ Postal code set:', user.postalCode);
                    }
                    
                    console.log('🎯 PROFILE POPULATED DIRECTLY FROM LOGIN!');
                } catch (profileError) {
                    console.error('❌ Profile population error:', profileError);
                }
            }
            
            // Also call the profile loading function for compatibility
            if (typeof loadProfileData === 'function') {
                loadProfileData();
            }
            
            // Accounts - IMMEDIATE
            if (typeof loadAccountsData === 'function') {
                console.log('💳 LOADING ACCOUNTS NOW...');
                loadAccountsData();
            } else {
                console.error('❌ loadAccountsData function NOT FOUND!');
            }
            
            // Transactions - IMMEDIATE
            if (typeof loadTransactionsData === 'function') {
                console.log('💰 LOADING TRANSACTIONS NOW...');
                loadTransactionsData();
            } else {
                console.error('❌ loadTransactionsData function NOT FOUND!');
            }
            
            // Beneficiaries - IMMEDIATE
            if (typeof loadBeneficiariesData === 'function') {
                console.log('👥 LOADING BENEFICIARIES NOW...');
                loadBeneficiariesData();
            } else {
                console.error('❌ loadBeneficiariesData function NOT FOUND!');
            }
            
            console.log('🔥 ALL SECTIONS FORCED TO LOAD - DONE!');
            
            console.log('✅ Portal populated with login response data!');
        } else {
            console.error('Invalid response - missing token or user:', { 
                hasToken: !!token, 
                hasUser: !!user,
                response: data 
            });
            throw new Error('Invalid response from server - missing authentication data');
        }
    } catch (err) {
        console.error('Portal login error:', err);
        
        // Clear the login in progress flag on error
        window.portalLoginInProgress = false;
        
        // Reset button state
        submitButton.disabled = false;
        submitButton.innerHTML = originalButtonText;
        
        // Show error message
        let errorMsg = 'Login failed. ';
        if (err.status === 401) {
            errorMsg += 'Invalid credentials. Please check your username and password.';
        } else if (err.status === 404) {
            errorMsg += 'User not found.';
        } else if (err.message) {
            errorMsg += err.message;
        } else {
            errorMsg += 'Please try again.';
        }
        
        showLoginError(errorMsg);
    }
}

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
    
    console.log('🔍 Checking portal login status:', { 
        isPortalLoggedIn,
        hasMainSidebar: !!mainSidebar,
        hasPortalSidebar: !!portalSidebar,
        hasCmsContent: !!cmsContent,
        hasPortalContent: !!portalContent
    });
    
    if (isPortalLoggedIn) {
        console.log('✅ Portal is logged in - showing portal UI');
        
        // Hide main sidebar, show portal sidebar
        if (mainSidebar) {
            mainSidebar.style.display = 'none';
            console.log('  ✓ Main sidebar hidden');
        }
        if (portalSidebar) {
            portalSidebar.style.display = 'block';
            console.log('  ✓ Portal sidebar shown');
        }
        
        // Hide CMS content, show portal content
        if (cmsContent) {
            cmsContent.style.display = 'none';
            console.log('  ✓ CMS content hidden');
        }
        if (portalContent) {
            portalContent.style.display = 'block';
            console.log('  ✓ Portal content shown');
        }
        
        // Update portal user name using PortalPersistence
        const currentUser = window.PortalPersistence?.getCurrentUser();
        
        console.log('  📧 User email:', currentUser?.email);
        console.log('  👤 User data:', currentUser ? 'Available' : 'Not found');
        
        if (portalUserName) {
            if (currentUser) {
                try {
                    const displayName = currentUser.firstName && currentUser.surname 
                        ? `${currentUser.firstName} ${currentUser.surname}` 
                        : currentUser.email || 'Portal User';
                    portalUserName.textContent = displayName;
                    console.log('  ✓ Portal user name set to:', displayName);
                } catch (e) {
                    portalUserName.textContent = userEmail || 'Portal User';
                    console.log('  ⚠️ Error parsing user data:', e.message);
                }
            } else if (userEmail) {
                portalUserName.textContent = userEmail;
                console.log('  ✓ Portal user name set to email:', userEmail);
            }
        } else {
            console.log('  ⚠️ Portal user name element not found');
        }
        
        // Show dashboard section by default
        console.log('  📊 Loading dashboard section...');
        showPortalSection('dashboard');
    } else {
        console.log('ℹ️ Portal not logged in - showing CMS UI');
        
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
    // Use PortalAPI service if available
    if (window.PortalAPI) {
        const portalToken = window.PortalAPI.storage.portalToken;
        const token = localStorage.getItem(portalToken);
        
        if (token) {
            console.log('Using portal token for API request');
            return token;
        }
    }
    
    // Fallback to legacy portal token
    let portalToken = localStorage.getItem(window.PORTAL_CONFIG.storage.token);
    
    if (portalToken) {
        console.log('Using legacy portal token for API request');
        return portalToken;
    }
    
    // Only fall back to admin token if no portal token exists
    try {
        const reduxAuth = sessionStorage.getItem('cms_auth');
        if (reduxAuth) {
            const authData = JSON.parse(reduxAuth);
            if (authData.token) {
                console.log('Falling back to admin token - portal login may be required');
                return authData.token;
            }
        }
    } catch (error) {
        console.error('Error parsing Redux auth data:', error);
    }
    
    console.warn('No authentication token found');
    return null;
}

/**
 * Make authenticated API request
 */
async function makeAuthenticatedRequest(endpoint, options = {}) {
    const token = getAuthToken();
    if (!token) {
        // Show login modal if no token is found
        console.error('No authentication token found for portal request');
        showLoginModal();
        throw new Error('Authentication required - please login to the portal');
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
    
    try {
        const response = await fetch(window.PORTAL_CONFIG.apiBaseUrl + endpoint, mergedOptions);
        
        // If we get 401, the token might be invalid or expired
        if (response.status === 401) {
            console.error('Authentication failed - token may be invalid or expired');
            // Clear invalid token
            localStorage.removeItem(window.PORTAL_CONFIG.storage.token);
            showLoginModal();
            throw new Error('Authentication failed - please login again');
        }
        
        return response;
    } catch (error) {
        if (error.message.includes('Authentication failed')) {
            throw error;
        }
        console.error('Request failed:', error);
        throw new Error('Network request failed');
    }
}

/**
 * Clear portal authentication
 */
function clearAllAuth() {
    // Clear portal auth from localStorage (using PortalAPI storage keys)
    localStorage.removeItem('portal-logged-in');
    localStorage.removeItem('portal-token');
    localStorage.removeItem('portal-user-email');
    localStorage.removeItem('portal-current-user');
    localStorage.removeItem(window.PORTAL_CONFIG.storage.loginFlag);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.token);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.userEmail);
    localStorage.removeItem(window.PORTAL_CONFIG.storage.userPassword);
    
    console.log('Portal authentication cleared');
}

/**
 * Initialize portal authentication check on page load
 */
function initializeAuthSync() {
    try {
        // Clear the login in progress flag
        window.portalLoginInProgress = false;
        
        // Initialize portal auth without reload checks since we're not reloading anymore
        console.log('� Initializing portal auth...');
        
        // Check if portal is logged in using PortalPersistence
        const isLoggedIn = window.PortalPersistence?.isLoggedIn() || false;
        
        if (isLoggedIn) {
            console.log('✅ Portal session active');
        } else {
            console.log('ℹ️ No active portal session');
        }
    } catch (error) {
        console.error('Error during auth initialization:', error);
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
    setTimeout(() => {
        initializeAuthSync();
        // Also check and show portal if user is logged in
        checkPortalLoginStatus();
    }, 200);
});
