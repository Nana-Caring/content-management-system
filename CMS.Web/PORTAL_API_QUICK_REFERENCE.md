# Portal API Quick Reference Guide

## 🚀 Quick Start

### 1. Include the API Service
```html
<script src="/js/portal-api.js"></script>
```

### 2. Basic Usage
```javascript
// Admin login
const adminResult = await PortalAPI.adminLogin('admin@nanacaring.com', 'nanacaring2025');

// Portal login
const portalResult = await PortalAPI.portalLogin('user@demo.com', 'password');

// Get user details
const userDetails = await PortalAPI.getPortalUserDetails();
```

## 📚 API Methods Reference

### Admin Methods

#### `adminLogin(email, password)`
```javascript
const result = await PortalAPI.adminLogin('admin@nanacaring.com', 'password');
// Returns: { accessToken, jwt, user }
```

#### `getUsers(params)`
```javascript
const result = await PortalAPI.getUsers({
    page: 1,
    limit: 25,
    search: 'john',
    role: 'dependent',
    status: 'active'
});
// Returns: { success, data: { users, total, page, pageCount, limit } }
```

### Portal Methods

#### `portalLogin(username, password)`
```javascript
const result = await PortalAPI.portalLogin('user@demo.com', 'password');
// Returns: { token, user }
```

#### `getPortalUserDetails()`
```javascript
const result = await PortalAPI.getPortalUserDetails();
// Returns: { user, recentTransactions }
```

#### `getPortalUserAccounts()`
```javascript
const result = await PortalAPI.getPortalUserAccounts();
// Returns: { accounts }
```

#### `getPortalUserTransactions(params)`
```javascript
const result = await PortalAPI.getPortalUserTransactions({
    page: 1,
    limit: 10,
    accountId: '123',
    type: 'purchase',
    startDate: '2025-01-01',
    endDate: '2025-12-31'
});
// Returns: { transactions, pagination }
```

### Utility Methods

#### `isPortalLoggedIn()`
```javascript
if (PortalAPI.isPortalLoggedIn()) {
    // User is logged into portal
}
```

#### `isAdminAuthenticated()`
```javascript
if (PortalAPI.isAdminAuthenticated()) {
    // Admin is authenticated
}
```

#### `getCurrentUser()`
```javascript
const user = PortalAPI.getCurrentUser();
// Returns: user object or null
```

#### `logout()`
```javascript
PortalAPI.logout(); // Clears all tokens
```

#### `setUseLocal(useLocal)`
```javascript
PortalAPI.setUseLocal(true);  // Use local API
PortalAPI.setUseLocal(false); // Use remote API
```

## 🔧 Configuration

### Change API Base URL
```javascript
PortalAPI.config.baseUrl = 'https://your-api.com';
```

### Change Timeout
```javascript
PortalAPI.config.timeout = 60000; // 60 seconds
```

### Change Retry Attempts
```javascript
PortalAPI.config.retryAttempts = 3;
```

## ⚠️ Error Handling

```javascript
try {
    const result = await PortalAPI.portalLogin('user@demo.com', 'password');
    console.log('Success:', result);
} catch (error) {
    console.error('Error:', error.message);
    
    // Check error status
    if (error.status === 401) {
        console.log('Authentication failed');
    } else if (error.status === 404) {
        console.log('User not found');
    }
}
```

## 📊 Response Formats

### Admin Login Response
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 0,
    "firstName": "Admin",
    "email": "admin@nanacaring.com",
    "role": "admin"
  }
}
```

### Portal Login Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 13,
    "firstName": "Emma",
    "surname": "Johnson",
    "email": "dependent@demo.com",
    "role": "dependent",
    "status": "active"
  }
}
```

### Get User Details Response
```json
{
  "user": {
    "id": 13,
    "firstName": "Emma",
    "surname": "Johnson",
    "email": "dependent@demo.com",
    "Accounts": [
      {
        "id": "uuid",
        "accountNumber": "3962948402",
        "accountType": "Baby Care",
        "balance": 97.6
      }
    ]
  },
  "recentTransactions": [...]
}
```

### Get Transactions Response
```json
{
  "transactions": [
    {
      "id": "trans_001",
      "amount": -25.99,
      "type": "purchase",
      "description": "Baby formula",
      "createdAt": "2025-11-04T10:30:00.000Z",
      "account": {
        "accountNumber": "3962948402",
        "accountType": "Baby Care"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 10,
    "total": 25,
    "totalPages": 3
  }
}
```

## 🎯 Common Patterns

### Login Flow
```javascript
async function performLogin(email, password) {
    try {
        // Step 1: Portal login
        const result = await PortalAPI.portalLogin(email, password);
        
        // Step 2: Load user details
        const details = await PortalAPI.getPortalUserDetails();
        
        // Step 3: Update UI
        updateDashboard(details);
        
        return true;
    } catch (error) {
        showError(error.message);
        return false;
    }
}
```

### Load Dashboard Data
```javascript
async function loadDashboard() {
    try {
        // Check if logged in
        if (!PortalAPI.isPortalLoggedIn()) {
            showLoginModal();
            return;
        }
        
        // Load user details
        const userData = await PortalAPI.getPortalUserDetails();
        
        // Load accounts
        const accountData = await PortalAPI.getPortalUserAccounts();
        
        // Load recent transactions
        const transData = await PortalAPI.getPortalUserTransactions({
            page: 1,
            limit: 5
        });
        
        // Update UI
        renderDashboard(userData, accountData, transData);
    } catch (error) {
        showError(error.message);
    }
}
```

### Pagination Handler
```javascript
async function loadPage(pageNumber) {
    try {
        const data = await PortalAPI.getPortalUserTransactions({
            page: pageNumber,
            limit: 10
        });
        
        renderTransactions(data.transactions);
        renderPagination(data.pagination);
    } catch (error) {
        showError(error.message);
    }
}
```

## 🧪 Testing

### Run Test Suite
1. Open `test-portal-api.html` in browser
2. Click "Run All Tests"
3. Check results

### Manual Testing
```javascript
// In browser console
await PortalAPI.adminLogin('admin@nanacaring.com', 'nanacaring2025');
await PortalAPI.portalLogin('dependent@demo.com', 'Emma123!');
await PortalAPI.getPortalUserDetails();
```

## 📱 Demo Users

| Email | Password | Role |
|-------|----------|------|
| admin@nanacaring.com | nanacaring2025 | admin |
| dependent@demo.com | Emma123! | dependent |
| funder@demo.com | Demo123!@# | funder |
| caregiver@demo.com | Demo123!@# | caregiver |

## 💡 Tips

1. **Always check authentication before API calls**
2. **Use try-catch for error handling**
3. **Implement loading states for better UX**
4. **Cache user data when appropriate**
5. **Clear tokens on logout**
6. **Validate data before displaying**

## 🐛 Troubleshooting

### "No token, authorization denied"
```javascript
// Check if logged in
console.log('Logged in:', PortalAPI.isPortalLoggedIn());

// Re-login if needed
await PortalAPI.portalLogin('user@demo.com', 'password');
```

### "Network request failed"
```javascript
// Check API availability
const isHealthy = await PortalAPI.healthCheck();
console.log('API healthy:', isHealthy);

// Try local API
PortalAPI.setUseLocal(true);
```

### CORS Issues
```javascript
// Use local proxy endpoint
PortalAPI.setUseLocal(true);
```

---

**Need more help?** Check the full documentation in `PORTAL_API_INTEGRATION.md`
