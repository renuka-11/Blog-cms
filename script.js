// API Base URL - Update this to match your backend
const API_BASE_URL = 'http://localhost:5000/api/v1';

// Load posts when page loads
document.addEventListener('DOMContentLoaded', () => {
    console.log('Homepage loaded');
    loadAllPosts();
    checkLoginStatus();
});

// Load all posts
async function loadAllPosts() {
    showLoading();
    try {
        console.log('Fetching posts from:', `${API_BASE_URL}/Posts`);
        const response = await fetch(`${API_BASE_URL}/Posts`);
        if (!response.ok) throw new Error('Failed to fetch posts');
        
        const posts = await response.json();
        console.log(`Loaded ${posts.length} posts`);
        displayPosts(posts);
    } catch (error) {
        console.error('Error loading posts:', error);
        showError('Failed to load posts. Make sure the backend is running.');
    }
}

// Filter posts by category
async function filterPosts(category) {
    showLoading();
    try {
        console.log('Filtering by category:', category);
        const response = await fetch(`${API_BASE_URL}/Posts?category=${encodeURIComponent(category)}`);
        if (!response.ok) throw new Error('Failed to fetch posts');
        
        const posts = await response.json();
        console.log(`Found ${posts.length} posts in category ${category}`);
        displayPosts(posts);
    } catch (error) {
        console.error('Error filtering posts:', error);
        showError('Failed to filter posts.');
    }
}

// Search posts
async function searchPosts() {
    const searchTerm = document.getElementById('searchInput').value.trim();
    if (!searchTerm) {
        loadAllPosts();
        return;
    }

    showLoading();
    try {
        console.log('Searching for:', searchTerm);
        const response = await fetch(`${API_BASE_URL}/Posts/search?q=${encodeURIComponent(searchTerm)}`);
        if (!response.ok) throw new Error('Failed to search posts');
        
        const posts = await response.json();
        console.log(`Found ${posts.length} posts matching "${searchTerm}"`);
        displayPosts(posts);
    } catch (error) {
        console.error('Error searching posts:', error);
        showError('Failed to search posts.');
    }
}

// Display posts in the container
function displayPosts(posts) {
    const container = document.getElementById('posts-container');
    
    if (!posts || posts.length === 0) {
        container.innerHTML = '<p class="no-posts">No posts found.</p>';
        return;
    }
    
    container.innerHTML = posts.map(post => `
        <div class="post-card" data-id="${post.id}">
            <h2>${escapeHtml(post.title)}</h2>
            <div class="post-meta">
                <span class="category">${escapeHtml(post.category)}</span>
                <span class="date">${formatDate(post.createdAt)}</span>
                <span class="author">By: ${escapeHtml(formatAuthor(post.createdBy))}</span>
            </div>
            <div class="post-content">${formatContent(post.content)}</div>
            ${post.updatedAt ? `<div class="updated">Updated: ${formatDate(post.updatedAt)}</div>` : ''}
        </div>
    `).join('');
}

// Helper: Format author (show email if it's an email, otherwise show shortened ID)
function formatAuthor(author) {
    if (!author) return 'Unknown';
    // If it looks like an email (contains @), show as is
    if (author.includes('@')) return author;
    // Otherwise it's a user ID - show first 8 chars
    return author.substring(0, 8) + '...';
}

// Helper: Format date
function formatDate(dateString) {
    if (!dateString) return 'Unknown date';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Helper: Format content (truncate long content)
function formatContent(content) {
    if (!content) return '';
    const maxLength = 300;
    if (content.length <= maxLength) return content;
    return content.substring(0, maxLength) + '...';
}

// Helper: Escape HTML to prevent XSS
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Show loading indicator
function showLoading() {
    const container = document.getElementById('posts-container');
    container.innerHTML = '<div class="loading">Loading posts...</div>';
}

// Show error message
function showError(message) {
    const container = document.getElementById('posts-container');
    container.innerHTML = `<div class="error-message">${message}</div>`;
}

// Check login status (for UI)
function checkLoginStatus() {
    const token = localStorage.getItem('authToken');
    const statusDiv = document.getElementById('login-status');
    if (statusDiv) {
        if (token) {
            const user = localStorage.getItem('userEmail') || 'Logged in';
            statusDiv.innerHTML = `✅ Logged in as: ${user} <button onclick="logout()">Logout</button>`;
            statusDiv.className = 'login-status logged-in';
        } else {
            statusDiv.innerHTML = '🔐 Not logged in. <a href="admin.html">Login here</a>';
            statusDiv.className = 'login-status logged-out';
        }
    }
}

// Logout function
function logout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userFullName');
    localStorage.removeItem('userRoles');
    checkLoginStatus();
    loadAllPosts();
}

// Make functions global
window.filterPosts = filterPosts;
window.searchPosts = searchPosts;
window.loadAllPosts = loadAllPosts;
window.logout = logout;