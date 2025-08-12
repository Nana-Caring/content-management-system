/**
 * Portal Profile Module
 * Handles profile data loading, editing, and updating
 */

let isEditMode = false;
let originalProfileData = {};

/**
 * Load profile data from API
 */
async function loadProfileData() {
    try {
        const response = await makeAuthenticatedRequest('/api/portal/me');

        if (response.ok) {
            const data = await response.json();
            originalProfileData = { ...data.user }; // Store original data
            populateProfile(data.user);
        } else {
            throw new Error('Failed to load profile data');
        }
    } catch (error) {
        console.error('Error loading profile data:', error);
        showProfileError('Error loading profile data');
    }
}

/**
 * Populate profile form with user data
 */
function populateProfile(user) {
    // Basic personal information
    document.getElementById('firstName').value = user.firstName || '';
    document.getElementById('middleName').value = user.middleName || '';
    document.getElementById('surname').value = user.surname || '';
    document.getElementById('email').value = user.email || '';
    document.getElementById('phoneNumber').value = user.phoneNumber || '';
    
    // Postal address
    const postalLine1 = document.getElementById('postalAddressLine1');
    const postalLine2 = document.getElementById('postalAddressLine2');
    const postalCity = document.getElementById('postalCity');
    const postalProvince = document.getElementById('postalProvince');
    const postalCode = document.getElementById('postalCode');
    
    if (postalLine1) postalLine1.value = user.postalAddressLine1 || '';
    if (postalLine2) postalLine2.value = user.postalAddressLine2 || '';
    if (postalCity) postalCity.value = user.postalCity || '';
    if (postalProvince) postalProvince.value = user.postalProvince || '';
    if (postalCode) postalCode.value = user.postalCode || '';
    
    // Home address
    const homeLine1 = document.getElementById('homeAddressLine1');
    const homeLine2 = document.getElementById('homeAddressLine2');
    const homeCity = document.getElementById('homeCity');
    const homeProvince = document.getElementById('homeProvince');
    const homeCode = document.getElementById('homeCode');
    
    if (homeLine1) homeLine1.value = user.homeAddressLine1 || '';
    if (homeLine2) homeLine2.value = user.homeAddressLine2 || '';
    if (homeCity) homeCity.value = user.homeCity || '';
    if (homeProvince) homeProvince.value = user.homeProvince || '';
    if (homeCode) homeCode.value = user.homeCode || '';
    
    // Display information in profile card
    updateProfileDisplay(user);
}

/**
 * Update profile display information
 */
function updateProfileDisplay(user) {
    // Full name display
    const displayName = document.getElementById('displayName');
    if (displayName) {
        const fullName = [user.firstName, user.middleName, user.surname]
            .filter(name => name && name.trim())
            .join(' ');
        displayName.textContent = fullName || 'N/A';
    }
    
    // Profile avatar initials
    const profileAvatar = document.getElementById('profileAvatar');
    if (profileAvatar && user.firstName && user.surname) {
        const initials = (user.firstName.charAt(0) + user.surname.charAt(0)).toUpperCase();
        profileAvatar.innerHTML = initials;
    }
    
    // Role and status information
    const userRole = document.getElementById('userRole');
    const accountRole = document.getElementById('accountRole');
    const accountStatus = document.getElementById('accountStatus');
    
    if (userRole) userRole.textContent = user.role || 'User';
    if (accountRole) accountRole.textContent = user.role || 'User';
    if (accountStatus) {
        accountStatus.textContent = user.status || 'Active';
        accountStatus.className = `badge ${user.status === 'active' ? 'bg-success' : 'bg-warning'}`;
    }
    
    // Dates
    const memberSince = document.getElementById('memberSince');
    const lastUpdated = document.getElementById('lastUpdated');
    
    if (memberSince && user.createdAt) {
        memberSince.textContent = new Date(user.createdAt).toLocaleDateString();
    }
    if (lastUpdated && user.updatedAt) {
        lastUpdated.textContent = new Date(user.updatedAt).toLocaleDateString();
    }
    
    // Account statistics
    const userAccountsCount = document.getElementById('userAccountsCount');
    const userDependentsCount = document.getElementById('userDependentsCount');
    
    if (userAccountsCount) {
        userAccountsCount.textContent = user.accounts ? user.accounts.length : 0;
    }
    if (userDependentsCount) {
        userDependentsCount.textContent = user.Dependents ? user.Dependents.length : 0;
    }
}

/**
 * Toggle profile edit mode
 */
function toggleEditMode() {
    isEditMode = !isEditMode;
    const inputs = document.querySelectorAll('#profileForm input:not(#email)');
    const editBtn = document.getElementById('editBtn');
    const actionButtons = document.getElementById('actionButtons');
    
    if (isEditMode) {
        // Enable editing
        inputs.forEach(input => input.removeAttribute('readonly'));
        if (editBtn) editBtn.style.display = 'none';
        if (actionButtons) actionButtons.style.display = 'block';
        
        // Store original data for cancel functionality
        const formData = new FormData(document.getElementById('profileForm'));
        originalProfileData = Object.fromEntries(formData);
    } else {
        // Disable editing
        inputs.forEach(input => input.setAttribute('readonly', true));
        if (editBtn) editBtn.style.display = 'block';
        if (actionButtons) actionButtons.style.display = 'none';
    }
}

/**
 * Save profile changes
 */
async function saveProfile() {
    const form = document.getElementById('profileForm');
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    
    // Remove empty strings and convert to proper format
    const cleanedData = {};
    for (const [key, value] of Object.entries(data)) {
        if (value && value.trim()) {
            cleanedData[key] = value.trim();
        } else {
            cleanedData[key] = null;
        }
    }
    
    try {
        const response = await makeAuthenticatedRequest('/api/portal/me', {
            method: 'PUT',
            body: JSON.stringify(cleanedData)
        });

        if (response.ok) {
            const result = await response.json();
            
            // Update the form with the response data
            populateProfile(result.user);
            
            // Exit edit mode
            toggleEditMode();
            
            // Show success message
            showProfileSuccess('Profile updated successfully!');
        } else {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to update profile');
        }
    } catch (error) {
        console.error('Error saving profile:', error);
        showProfileError('Failed to save profile changes: ' + error.message);
    }
}

/**
 * Cancel profile editing
 */
function cancelEdit() {
    // Restore original data
    populateProfile(originalProfileData);
    
    // Exit edit mode
    toggleEditMode();
}

/**
 * Show profile success message
 */
function showProfileSuccess(message) {
    // Remove any existing alerts
    removeProfileAlerts();
    
    const alert = document.createElement('div');
    alert.className = 'alert alert-success alert-dismissible fade show';
    alert.innerHTML = `
        <i class="bi bi-check-circle me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const profileCard = document.querySelector('#portal-profile .card-body');
    if (profileCard) {
        profileCard.insertBefore(alert, profileCard.firstChild);
    }
}

/**
 * Show profile error message
 */
function showProfileError(message) {
    // Remove any existing alerts
    removeProfileAlerts();
    
    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-dismissible fade show';
    alert.innerHTML = `
        <i class="bi bi-exclamation-triangle me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const profileCard = document.querySelector('#portal-profile .card-body');
    if (profileCard) {
        profileCard.insertBefore(alert, profileCard.firstChild);
    }
}

/**
 * Remove existing profile alerts
 */
function removeProfileAlerts() {
    const existingAlerts = document.querySelectorAll('#portal-profile .alert');
    existingAlerts.forEach(alert => alert.remove());
}
