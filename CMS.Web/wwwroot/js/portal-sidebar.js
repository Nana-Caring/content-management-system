// Portal Sidebar JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Get sidebar elements
    const sidebar = document.querySelector('.portal-sidebar');
    const toggleButton = document.querySelector('.sidebar-toggle');
    const navItems = document.querySelectorAll('.nav-item');

    // Toggle sidebar on mobile
    if (toggleButton) {
        toggleButton.addEventListener('click', function() {
            sidebar.classList.toggle('show');
        });
    }

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(event) {
        if (window.innerWidth <= 768) {
            const isClickInside = sidebar.contains(event.target) || 
                                toggleButton.contains(event.target);
            
            if (!isClickInside && sidebar.classList.contains('show')) {
                sidebar.classList.remove('show');
            }
        }
    });

    // Handle active state for navigation items
    navItems.forEach(item => {
        item.addEventListener('click', function() {
            // Remove active class from all items
            navItems.forEach(navItem => navItem.classList.remove('active'));
            // Add active class to clicked item
            this.classList.add('active');
        });

        // Set active state based on current URL
        if (window.location.pathname === item.getAttribute('href')) {
            item.classList.add('active');
        }
    });

    // Handle responsive behavior
    window.addEventListener('resize', function() {
        if (window.innerWidth > 768) {
            sidebar.classList.remove('show');
        }
    });
});
