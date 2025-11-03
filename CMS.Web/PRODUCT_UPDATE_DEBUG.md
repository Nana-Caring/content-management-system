# 🔧 ASP.NET Product Update Troubleshooting Guide

This guide helps you fix product update issues in the ASP.NET frontend when calling the backend `/admin/products/{id}` endpoint.

## 🚨 Most Likely Issues

### 1) Wrong Endpoint URL
```csharp
// WRONG - Missing /admin prefix
await client.PutAsync($"{baseUrl}/products/{id}", content);

// WRONG - Extra /api prefix
await client.PutAsync($"{baseUrl}/api/admin/products/{id}", content);

// ✅ Correct
await client.PutAsync($"{baseUrl}/admin/products/{id}", content);
```

### 2) Data Type Issues (Price, Booleans, Integers)
```csharp
// ❌ Common mistakes (strings)
new { price = "99.99", stockQuantity = "50", requiresAgeVerification = "true" }

// ✅ Correct types
new { price = 99.99m, stockQuantity = 50, requiresAgeVerification = true }
```

### 3) Enum Case Sensitivity
```csharp
// ❌ Wrong case
category = "healthcare"; ageCategory = "adult";

// ✅ Exact case
category = "Healthcare"; ageCategory = "Adult";
```
Valid categories: Education, Healthcare, Groceries, Transport, Entertainment, Other

Valid age categories: Toddler, Child, Teen, Adult, Senior, All Ages

### 4) Authentication
```csharp
// ✅ Ensure bearer token is set
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
```

## 🧰 What we changed in this repo

- Aligned models and requests to send numeric `price` (decimal) instead of string.
- Added logging of outgoing JSON for POST/PUT requests in `ApiService`.
- The Products page handlers now parse the form `price` into `decimal?` using InvariantCulture before sending.

Files touched:
- `Models/AdminProduct.cs` (Price: `string?` → `decimal?`)
- `Pages/Products/Index.cshtml.cs` (convert form price → `decimal?` in create/update)
- `Services/ApiService.cs` (log request JSON for POST and PUT)

## 🧪 Test the Backend First (Optional Script)

From the backend repo:
```bash
node debug-product-update.js
```

## 🔍 Add Detailed Logging (example pattern)
```csharp
var payload = CreateUpdateObject(model);
var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
_logger.LogInformation($"Sending to backend: {json}");
var res = await _httpClient.PutAsync($"{_baseUrl}/admin/products/{id}", new StringContent(json, Encoding.UTF8, "application/json"));
var body = await res.Content.ReadAsStringAsync();
_logger.LogInformation($"Backend response ({res.StatusCode}): {body}");
```

## ✅ Minimal Safe Update Test
```csharp
var minimal = new { name = $"Test Update {DateTime.UtcNow.Ticks}" };
```

## 📋 Quick Checklist

- [ ] URL: `/admin/products/{id}`
- [ ] Method: `PUT`
- [ ] Content-Type: `application/json`
- [ ] Authorization: `Bearer {token}`
- [ ] Price: decimal (`99.99m`)
- [ ] Category: exact case (e.g., `Healthcare`)
- [ ] Age Category: exact case (e.g., `Adult`)
- [ ] Booleans: true booleans (not strings)
- [ ] Integers: numeric (not strings)
- [ ] Image URL: well-formed or null

## 🔗 Manual cURL (optional)
```bash
curl -X PUT https://nanacaring-backend.onrender.com/admin/products/1 \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
    -d '{
        "name": "Test Product Update",
        "price": 99.99,
        "category": "Healthcare"
    }'
```

## 🧭 When the UI Table Doesn’t Refresh

If the table doesn’t reflect changes immediately:
- Ensure each row has `data-product-id="{id}"`
- Ensure name/description cells have classes `.product-name` and `.product-description`
- The page’s `updateProductInTable(product)` will update the visible row without a full refresh

Tip: in DevTools console, run:
```js
window.debugProductUpdate(PRODUCT_ID)
```
to verify selectors/row lookup.

—
Keep logs from `ApiService` handy. If backend accepts the update but UI doesn’t change, it’s a DOM update/selector issue; otherwise, check types, casing, and the token.
