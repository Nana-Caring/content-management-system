/**
 * Portal API Initialization Checker
 * This script checks if all portal dependencies are loaded correctly
 */

(function() {
    'use strict';
    
    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', checkPortalDependencies);
    } else {
        checkPortalDependencies();
    }
    
    function checkPortalDependencies() {
        const checks = {
            'PortalAPI': window.PortalAPI,
            'PortalAPI.portalLogin': window.PortalAPI?.portalLogin,
            'PortalAPI.getPortalUserDetails': window.PortalAPI?.getPortalUserDetails,
            'PortalAPI.getPortalUserAccounts': window.PortalAPI?.getPortalUserAccounts,
            'PortalAPI.getPortalUserTransactions': window.PortalAPI?.getPortalUserTransactions,
            'PortalAPI.adminLogin': window.PortalAPI?.adminLogin,
            'PortalAPI.getUsers': window.PortalAPI?.getUsers,
            'PortalPersistence': window.PortalPersistence,
            'PortalPersistence.isLoggedIn': window.PortalPersistence?.isLoggedIn,
            'PortalPersistence.getCurrentUser': window.PortalPersistence?.getCurrentUser,
            'PortalPersistence.saveSession': window.PortalPersistence?.saveSession,
            'handlePortalLogin': typeof handlePortalLogin !== 'undefined',
            'checkPortalLoginStatus': typeof checkPortalLoginStatus !== 'undefined',
            'initializePortalAuth': typeof initializePortalAuth !== 'undefined',
            'loadDashboardData': typeof loadDashboardData !== 'undefined',
            'loadAccountsData': typeof loadAccountsData !== 'undefined',
            'loadTransactionsData': typeof loadTransactionsData !== 'undefined',
            'loadProfileData': typeof loadProfileData !== 'undefined'
        };
        
        const results = {
            passed: 0,
            failed: 0,
            total: Object.keys(checks).length
        };
        
        console.group('🔍 Portal API Dependency Check');
        
        Object.keys(checks).forEach(key => {
            const status = checks[key];
            const icon = status ? '✅' : '❌';
            const message = `${icon} ${key}: ${status ? 'Available' : 'MISSING'}`;
            
            if (status) {
                console.log(message);
                results.passed++;
            } else {
                console.error(message);
                results.failed++;
            }
        });
        
        console.log('\n📊 Summary:');
        console.log(`Total Checks: ${results.total}`);
        console.log(`✅ Passed: ${results.passed}`);
        console.log(`❌ Failed: ${results.failed}`);
        console.log(`Success Rate: ${((results.passed / results.total) * 100).toFixed(1)}%`);
        
        if (results.failed > 0) {
            console.warn('\n⚠️ Some dependencies are missing! This may cause portal functionality to fail.');
            console.warn('Make sure all portal JavaScript files are loaded in the correct order:');
            console.warn('1. portal-api.js (MUST BE FIRST)');
            console.warn('2. portal-auth.js');
            console.warn('3. portal-navigation.js');
            console.warn('4. portal-dashboard.js');
            console.warn('5. portal-accounts.js');
            console.warn('6. portal-transactions.js');
            console.warn('7. portal-profile.js');
        } else {
            console.log('\n✅ All portal dependencies loaded successfully!');
        }
        
        console.groupEnd();
        
        // Store results globally for debugging
        window.portalDependencyCheck = {
            timestamp: new Date().toISOString(),
            results: results,
            details: checks
        };
    }
})();
