# Professional Loader - Global Component

This document explains how to use the global Professional Loader component across all pages in the CMS application.

## Overview

The Professional Loader is now a global component included in the main layout (`_Layout.cshtml`) via the `_ProfessionalLoader.cshtml` partial view. It provides a consistent, professional loading experience across all pages.

## Features

- **Professional Design**: Animated logo with gradient backgrounds
- **Page-Specific Themes**: Different color schemes for different sections
- **Responsive**: Mobile-friendly design
- **Auto-Detection**: Automatically detects page type and applies appropriate theme
- **Legacy Compatibility**: Maintains backward compatibility with existing code

## Usage

### Basic Usage

```javascript
// Show loader
ProfessionalLoader.show('Loading data...');

// Hide loader
ProfessionalLoader.hide();

// Show with delay (recommended for fast operations)
const controller = ProfessionalLoader.showWithDelay('Processing...', 200);
// Later...
controller.hide();
```

### Page-Specific Themes

```javascript
// Set theme for current page (usually done automatically)
ProfessionalLoader.setPageTheme('transactions');

// Available themes:
// - 'transactions' (orange/yellow gradient)
// - 'users' (blue/purple gradient) 
// - 'accounts' (pink/red gradient)
// - 'products' (blue/cyan gradient)
// - 'kyc' (green/cyan gradient)
// - 'default' (orange/yellow gradient)
```

### Navigation and Forms

```javascript
// Navigate with loader
ProfessionalLoader.navigateWithLoader('/Users', 'Loading users...');

// Submit form with loader
ProfessionalLoader.submitFormWithLoader(formElement, 'Saving changes...');
```

### Legacy Compatibility

The following functions are still available for backward compatibility:

```javascript
// Legacy functions (automatically use global loader)
showLoader('Loading...');
hideLoader();
showLoaderWithDelay('Processing...', 200);
handleNavigationWithLoader(url, message);
handleFormSubmissionWithLoader(form, message);
```

## Page Integration

### Automatic Integration

The loader is automatically included in all pages via `_Layout.cshtml`. Page themes are auto-detected based on URL path.

### Manual Theme Setting

For pages that need specific themes, add this script:

```html
<script>
    document.addEventListener('DOMContentLoaded', function() {
        if (window.ProfessionalLoader) {
            window.ProfessionalLoader.setPageTheme('your-theme-name');
        }
    });
</script>
```

## Implementation Details

### File Structure

```
CMS.Web/Pages/Shared/
├── _Layout.cshtml (includes the loader)
└── _ProfessionalLoader.cshtml (loader component)
```

### CSS Classes

The loader uses these main CSS classes:
- `.loader-overlay` - Main container
- `.loader-theme-[theme]` - Theme-specific styling
- `.loader-container` - Content wrapper
- `.logo-circle` - Animated logo
- `.spinner` - Loading animation

### JavaScript API

The global `window.ProfessionalLoader` object provides:
- `show(message, theme)` - Show loader
- `hide()` - Hide loader  
- `showWithDelay(message, delay, theme)` - Show with delay
- `setPageTheme(pageType)` - Set theme
- `setIcon(iconClass)` - Update icon
- `navigateWithLoader(url, message)` - Navigate with loader
- `submitFormWithLoader(form, message)` - Submit form with loader

## Examples

### Loading Data

```javascript
async function loadUserData() {
    const controller = ProfessionalLoader.showWithDelay('Loading users...', 100, 'users');
    
    try {
        const response = await fetch('/api/users');
        const data = await response.json();
        // Process data...
    } catch (error) {
        console.error('Error:', error);
    } finally {
        controller.hide();
    }
}
```

### Form Submission

```javascript
document.getElementById('myForm').addEventListener('submit', function(e) {
    e.preventDefault();
    ProfessionalLoader.submitFormWithLoader(this, 'Saving changes...');
});
```

### Page Navigation

```javascript
document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', function(e) {
        e.preventDefault();
        const url = this.getAttribute('href');
        ProfessionalLoader.navigateWithLoader(url, 'Loading page...');
    });
});
```

## Customization

### Adding New Themes

To add a new theme, update the CSS in `_ProfessionalLoader.cshtml`:

```css
.loader-theme-mytheme .logo-circle {
    background: linear-gradient(135deg, #your-color1 0%, #your-color2 100%);
}
```

And add it to the themes object in JavaScript:

```javascript
const themes = {
    // ...existing themes...
    'mytheme': { theme: 'mytheme', icon: 'fas fa-your-icon' }
};
```

## Best Practices

1. **Use delays for fast operations** to avoid flicker
2. **Set appropriate themes** for better visual consistency
3. **Always hide loaders** in finally blocks or error handlers
4. **Use meaningful messages** to inform users what's happening
5. **Test on mobile devices** to ensure responsive behavior

## Migration from Local Loaders

If you have pages with local loader implementations:

1. Remove the local loader HTML and CSS
2. Update JavaScript to use `ProfessionalLoader` methods
3. Add page theme initialization if needed
4. Test functionality to ensure smooth operation

The global loader maintains backward compatibility, so existing `showLoader()` calls will continue to work.
