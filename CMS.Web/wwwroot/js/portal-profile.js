/**
 * Portal Profile Module
 * Handles profile data loading, editing, and updating
 */

let isEditMode = false;
let originalProfileData = {};
let cachedProfileData = null;

// Function to populate profile when section becomes visible
function populateProfileWhenVisible() {
    if (!cachedProfileData) return;
    
    console.log('👁️ Profile section is visible, populating with cached data...');
    
    // Direct population without retries since section is visible - USING CORRECT IDs!
    const firstNameInput = document.getElementById('profile-firstName');
    const middleNameInput = document.getElementById('profile-middleName');
    const lastNameInput = document.getElementById('profile-surname');
    const emailInput = document.getElementById('profile-email');
    const phoneInput = document.getElementById('profile-phoneNumber');
    const addressLine1Input = document.getElementById('profile-postalAddressLine1');
    const addressLine2Input = document.getElementById('profile-postalAddressLine2');
    const cityInput = document.getElementById('profile-postalCity');
    const provinceInput = document.getElementById('profile-postalProvince');
    const postalCodeInput = document.getElementById('profile-postalCode');
    
    if (firstNameInput) {
        firstNameInput.value = cachedProfileData.firstName;
        console.log('✅ Set firstName:', cachedProfileData.firstName);
    } else {
        console.error('❌ profile-firstName element not found!');
    }
    
    if (middleNameInput) {
        middleNameInput.value = cachedProfileData.middleName || '';
        console.log('✅ Set middleName:', cachedProfileData.middleName);
    }
    
    if (lastNameInput) {
        lastNameInput.value = cachedProfileData.surname;
        console.log('✅ Set surname:', cachedProfileData.surname);
    } else {
        console.error('❌ profile-surname element not found!');
    }
    
    if (emailInput) {
        emailInput.value = cachedProfileData.email;
        console.log('✅ Set email:', cachedProfileData.email);
    } else {
        console.error('❌ profile-email element not found!');
    }
    
    if (phoneInput) {
        phoneInput.value = cachedProfileData.phoneNumber || '';
        console.log('✅ Set phoneNumber:', cachedProfileData.phoneNumber);
    }
    
    if (addressLine1Input) {
        addressLine1Input.value = cachedProfileData.postalAddressLine1 || '';
        console.log('✅ Set postalAddressLine1:', cachedProfileData.postalAddressLine1);
    }
    
    if (addressLine2Input) {
        addressLine2Input.value = cachedProfileData.postalAddressLine2 || '';
        console.log('✅ Set postalAddressLine2:', cachedProfileData.postalAddressLine2);
    }
        console.log('✅ Set DOB:', dobInput.value);
    }
    
    if (genderInput && cachedProfileData.Idnumber) {
        const genderDigit = parseInt(cachedProfileData.Idnumber.charAt(6));
        genderInput.value = genderDigit >= 5 ? 'Male' : 'Female';
        console.log('✅ Set gender:', genderInput.value);
    }
    
    if (addressInput) {
        addressInput.value = `Role: ${cachedProfileData.role}, Relation: ${cachedProfileData.relation}`;
        console.log('✅ Set address:', addressInput.value);
    }
    
    // Update display elements
    updateProfileDisplay(cachedProfileData);
    
    console.log('🎉 PROFILE POPULATED WITH MANDLA DATA!');
}

/**
 * Load profile data from API
 */
async function loadProfileData() {
    console.log('� === PROFILE LOADING STARTED ===');
    console.log('�📦 Loading profile data...');
    
    // Check if PortalPersistence exists
    console.log('🔍 PortalPersistence available:', !!window.PortalPersistence);
    
    // Get user data from PortalPersistence (stored from API response)
    const user = window.PortalPersistence?.getCurrentUser();
    
    console.log('👤 User from PortalPersistence:', user);
    
    if (!user) {
        console.error('❌ No user data available');
        console.log('🔍 Checking localStorage directly...');
        const directUser = localStorage.getItem('portal-current-user');
        console.log('📦 Direct localStorage user:', directUser);
        showProfileError('No user data available. Please log in again.');
        return;
    }
    
    console.log('✅ Raw user data from API:', user);
    console.log('📋 User details breakdown:', {
        id: user.id,
        firstName: user.firstName,
        middleName: user.middleName,
        surname: user.surname,
        email: user.email,
        role: user.role,
        Idnumber: user.Idnumber,
        relation: user.relation,
        createdAt: user.createdAt,
        updatedAt: user.updatedAt,
        hasAccounts: !!user.Accounts,
        accountCount: user.Accounts?.length || 0
    });
    
    // Store data for later use when section becomes visible
    console.log('💾 Storing profile data...');
    cachedProfileData = { ...user };
    originalProfileData = { ...user };
    
    console.log('🎨 Calling populateProfile...');
    populateProfile(user);
    
    // Also make sure profile is populated when section is shown
    window.populateProfileWhenVisible = populateProfileWhenVisible;
    
    console.log('🔥 === PROFILE LOADING COMPLETED ===');
}

/**
 * Populate profile form with user data
 */
function populateProfile(user) {
    console.log('� === POPULATE PROFILE STARTED ===');
    console.log('�📝 Populating profile with user data:', user);
    console.log('🔍 User object keys:', Object.keys(user));
    
    // Basic personal information (matching actual HTML IDs)
    // Wait a moment for DOM to be ready and try multiple times if needed
    let attempts = 0;
    const maxAttempts = 5;
    
    function tryPopulateElements() {
        attempts++;
        console.log(`🔍 Attempt ${attempts} to find profile elements...`);
        
        const firstNameInput = document.getElementById('profile-firstName');
        const lastNameInput = document.getElementById('profile-surname');
        const emailInput = document.getElementById('profile-email');
        const phoneInput = document.getElementById('profile-phoneNumber');
        const middleNameInput = document.getElementById('profile-middleName');
        
        console.log('🔍 Profile elements found:', {
            firstNameInput: !!firstNameInput,
            lastNameInput: !!lastNameInput,
            emailInput: !!emailInput,
            phoneInput: !!phoneInput,
            middleNameInput: !!middleNameInput
        });
        
        // If elements are found, populate them
        if (firstNameInput && lastNameInput && emailInput) {
            console.log('✅ Elements found! Populating with Mandla data...');
            populateElements();
            return true;
        }
        
        // If not found and we haven't reached max attempts, try again
        if (attempts < maxAttempts) {
            console.log(`⚠️ Elements not found, retrying in 100ms... (attempt ${attempts}/${maxAttempts})`);
            setTimeout(tryPopulateElements, 100);
            return false;
        }
        
        console.error('❌ Profile elements not found after 5 attempts!');
        return false;
    }
    
    function populateElements() {
        // Get fresh references to elements - USING CORRECT IDs!
        const firstNameInput = document.getElementById('profile-firstName');
        const lastNameInput = document.getElementById('profile-surname');
        const emailInput = document.getElementById('profile-email');
        const phoneInput = document.getElementById('profile-phoneNumber');
        const middleNameInput = document.getElementById('profile-middleName');
    
        // DISPLAY EXACT API DATA - NO FALLBACKS!
        if (firstNameInput) {
            firstNameInput.value = user.firstName; // API: "Mandla"
            console.log('✅ Set firstName from API:', user.firstName);
        }
        
        if (lastNameInput) {
            lastNameInput.value = user.surname; // API: "Khumalo"
            console.log('✅ Set lastName from API:', user.surname);
        }
        
        if (emailInput) {
            emailInput.value = user.email; // API: "mandla.khumalo@youthmail.com"
            console.log('✅ Set email from API:', user.email);
        }
        
        if (phoneInput) {
            phoneInput.value = user.Idnumber; // API: "0509208040089" - show ID number
            console.log('✅ Set ID number as phone from API:', user.Idnumber);
        }
        
        if (dobInput) {
            // Extract birth date from SA ID number (YYMMDD format)
            if (user.Idnumber && user.Idnumber.length >= 6) {
                const idDate = user.Idnumber.substring(0, 6); // "050920"
                const year = '20' + idDate.substring(0, 2); // "2005"
                const month = idDate.substring(2, 4); // "09"
                const day = idDate.substring(4, 6); // "20"
                dobInput.value = `${year}-${month}-${day}`;
                console.log('✅ Set DOB extracted from ID:', dobInput.value);
            }
        }
        
        if (genderInput) {
            // Extract gender from SA ID number (7th digit)
            if (user.Idnumber && user.Idnumber.length >= 7) {
                const genderDigit = parseInt(user.Idnumber.charAt(6)); // "8"
                genderInput.value = genderDigit >= 5 ? 'Male' : 'Female'; // 8 >= 5 = Male
                console.log('✅ Set gender from ID (digit ' + genderDigit + '):', genderInput.value);
            }
        }
        
        if (addressInput) {
            // Show role and relation info from API since no address fields
            addressInput.value = `Role: ${user.role}, Relation: ${user.relation}`;
            console.log('✅ Set role/relation as address from API:', addressInput.value);
        }
        
        console.log('🎉 PROFILE ELEMENTS POPULATED WITH MANDLA DATA!');
    }
    
    // Start trying to populate elements
    tryPopulateElements();
    
    // Display information in profile card
    console.log('🎨 Calling updateProfileDisplay...');
    updateProfileDisplay(user);
    
    console.log('🔥 === POPULATE PROFILE COMPLETED ===');
}

/**
 * Update profile display information
 */
function updateProfileDisplay(user) {
    console.log('🎨 Updating profile display for user:', user.email);
    
    // Full name display (using actual HTML ID)
    const fullNameElement = document.getElementById('profileFullName');
    if (fullNameElement) {
        const fullName = [user.firstName, user.middleName, user.surname]
            .filter(name => name && name.trim())
            .join(' ');
        fullNameElement.textContent = fullName || 'N/A';
        console.log('✅ Updated full name display:', fullName);
    } else {
        console.warn('⚠️ profileFullName element not found');
    }
    
    // Email display (using actual HTML ID)
    const emailElement = document.getElementById('profileEmail');
    if (emailElement) {
        emailElement.textContent = user.email || 'N/A';
        console.log('✅ Updated email display:', user.email);
    } else {
        console.warn('⚠️ profileEmail element not found');
    }
    
    // Profile avatar initials
    const profileAvatar = document.getElementById('profileAvatar');
    if (profileAvatar && user.firstName && user.surname) {
        const initials = (user.firstName.charAt(0) + user.surname.charAt(0)).toUpperCase();
        profileAvatar.innerHTML = initials;
        console.log('✅ Updated avatar initials:', initials);
    } else if (profileAvatar) {
        profileAvatar.innerHTML = user.email ? user.email.charAt(0).toUpperCase() : 'U';
        console.log('✅ Updated avatar with email initial');
    }
    
    // Profile status
    const statusElement = document.getElementById('profileStatus');
    if (statusElement) {
        statusElement.textContent = user.role ? user.role.charAt(0).toUpperCase() + user.role.slice(1) : 'Active';
        console.log('✅ Updated status:', user.role || 'Active');
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
    const inputs = document.querySelectorAll('#profileForm input:not(#profile-email)');
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
