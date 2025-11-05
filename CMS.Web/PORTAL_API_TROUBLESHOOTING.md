# Portal API Troubleshooting Guide

## 🔧 Common Issues and Solutions

### Issue: "Cannot read properties of undefined (reading 'portalLogin')"

**Cause:** The `portal-api.js` file is not loaded or is loaded after `portal-auth.js`.

**Solution:**
1. Verify script loading order in `_Layout.cshtml`:
   ```html
   <!-- CORRECT ORDER -->
   <script src="~/js/portal-api.js"></script>      <!-- MUST BE FIRST -->
   <script src="~/js/portal-auth.js"></script>
   <script src="~/js/portal-navigation.js"></script>
   ```

2. Clear browser cache (Ctrl + F5)

3. Check browser console for errors:
   ```javascript
   // Type in console to verify PortalAPI is loaded
   console.log(window.PortalAPI);
   ```

**Status:** ✅ FIXED (November 5, 2025)
- Added `portal-api.js` to _Layout.cshtml before other portal scripts
- Added safety checks in `portal-auth.js`

---

### Issue: Portal login not working

**Debugging Steps:**

1. **Check if PortalAPI is loaded:**
   ```javascript
   // In browser console
   console.log(window.PortalAPI);
   // Should show an object with methods
   ```

2. **Verify API endpoint:**
   ```javascript
   console.log(window.PortalAPI.config.baseUrl);
   // Should show: https://nanacaring-backend.onrender.com
   ```

3. **Test API directly:**
   ```javascript
   await window.PortalAPI.portalLogin('dependent@demo.com', 'Emma123!');
   ```

4. **Check network requests:**
   - Open DevTools → Network tab
   - Look for request to `/api/portal/admin-login`
   - Check response status and body

---

### Issue: Dependencies not loading

**Use the dependency checker:**

1. Uncomment in `_Layout.cshtml`:
   ```html
   <script src="~/js/portal-init-check.js" asp-append-version="true"></script>
   ```

2. Refresh page and check console for dependency report

3. Look for missing functions marked with ❌

**Expected Output:**
```
✅ PortalAPI: Available
✅ PortalAPI.portalLogin: Available
✅ handlePortalLogin: Available
...
Success Rate: 100%
```

---

### Issue: CORS errors

**Symptom:** 
```
Access to fetch at 'https://nanacaring-backend.onrender.com/...' has been blocked by CORS
```

**Solution:**
```javascript
// Switch to local API mode
window.PortalAPI.setUseLocal(true);
```

Or update the backend to allow your origin.

---

### Issue: Token not persisting

**Check token storage:**
```javascript
// In browser console
console.log(localStorage.getItem('portal-token'));
console.log(localStorage.getItem('portal-logged-in'));
console.log(localStorage.getItem('portal-user-email'));
```

**Clear and retry:**
```javascript
window.PortalAPI.logout();
// Then login again
```

---

### Issue: Authentication required errors

**Verify token exists:**
```javascript
console.log(window.PortalAPI.isPortalLoggedIn());
// Should return true if logged in
```

**Get current user:**
```javascript
console.log(window.PortalAPI.getCurrentUser());
// Should show user object
```

---

## 🧪 Testing Checklist

Before reporting an issue, verify:

- [ ] Browser cache cleared (Ctrl + F5)
- [ ] No console errors
- [ ] `portal-api.js` loads before other portal scripts
- [ ] Network tab shows API requests
- [ ] Token is stored in localStorage
- [ ] API base URL is correct

## 🔍 Debugging Tools

### 1. Check All Portal Dependencies
```javascript
// Run in console
if (window.portalDependencyCheck) {
    console.log(window.portalDependencyCheck);
} else {
    console.log('Dependency checker not loaded');
}
```

### 2. Test API Endpoints Manually
```javascript
// Test each endpoint
await window.PortalAPI.adminLogin('admin@nanacaring.com', 'nanacaring2025');
await window.PortalAPI.portalLogin('dependent@demo.com', 'Emma123!');
await window.PortalAPI.getPortalUserDetails();
await window.PortalAPI.getPortalUserAccounts();
await window.PortalAPI.getPortalUserTransactions({ page: 1, limit: 10 });
```

### 3. Use the Test Suite
Open `test-portal-api.html` in browser and run all tests.

### 4. Monitor Network Traffic
- Open DevTools → Network tab
- Filter by "Fetch/XHR"
- Look for API requests
- Check request headers (Authorization)
- Check response status and body

## 📝 Script Loading Order

**Critical:** Scripts MUST load in this exact order:

1. `portal-api.js` ← **MUST BE FIRST**
2. `portal-auth.js`
3. `portal-navigation.js`
4. `portal-dashboard.js`
5. `portal-accounts.js`
6. `portal-transactions.js`
7. `portal-profile.js`
8. `portal-beneficiaries.js`

**Why?** All other scripts depend on `window.PortalAPI` being available.

## 🚨 Emergency Reset

If all else fails:

```javascript
// 1. Clear all portal data
localStorage.removeItem('portal-token');
localStorage.removeItem('portal-logged-in');
localStorage.removeItem('portal-user-email');
localStorage.removeItem('portal-current-user');
sessionStorage.clear();

// 2. Hard refresh
// Press Ctrl + Shift + R (or Cmd + Shift + R on Mac)

// 3. Verify API is loaded
console.log(window.PortalAPI ? 'API Loaded ✅' : 'API Missing ❌');
```

## 📞 Getting Help

1. Check browser console for errors
2. Run dependency check
3. Test with test-portal-api.html
4. Check network tab for failed requests
5. Verify script loading order in page source

## 🔗 Related Files

- `/wwwroot/js/portal-api.js` - Main API service
- `/wwwroot/js/portal-auth.js` - Authentication handling
- `/wwwroot/js/portal-init-check.js` - Dependency checker
- `/Pages/Shared/_Layout.cshtml` - Script loading
- `/test-portal-api.html` - Test suite

---

**Last Updated:** November 5, 2025
**Status:** All known issues resolved ✅
