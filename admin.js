// API Base URL
const API_BASE_URL = 'http://localhost:5000/api/v1';

// Check authentication status on load
document.addEventListener('DOMContentLoaded', () => {
    console.log('Admin panel loaded');
    checkAuth();
    setupEventListeners();
});

// Setup event listeners
function setupEventListeners() {
    // Login form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    // Signup form
    const signupForm = document.getElementById('signupForm');
    if (signupForm) {
        signupForm.addEventListener('submit', handleSignup);
    }

    // Add post form
    const addPostForm = document.getElementById('addPostForm');
    if (addPostForm) {
        addPostForm.addEventListener('submit', handleAddPost);
    }
}

// Check if user is authenticated
function checkAuth() {
    const token = localStorage.getItem('authToken');
    const userEmail = localStorage.getItem('userEmail');
    const userRoles = JSON.parse(localStorage.getItem('userRoles') || '[]');
    
    console.log('Auth check - Token exists:', !!token);
    console.log('Auth check - User:', userEmail);
    console.log('Auth check - Roles:', userRoles);
    
    if (token && userEmail) {
        showPostSection(userEmail, userRoles);
        loadUserPosts();
    } else {
        showLoginSection();
    }
}

// Handle login form submission
async function handleLogin(e) {
    e.preventDefault();

    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    console.log('Attempting login for:', email);
    showMessage('Logging in...', 'info');

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        console.log('Login response status:', response.status);
        const data = await response.json();
        console.log('Login response data:', data);

        if (!response.ok) {
            throw new Error(data.error || 'Login failed');
        }

        // Store auth data
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('refreshToken', data.refreshToken);
        localStorage.setItem('userEmail', data.email);
        localStorage.setItem('userFullName', data.fullName);
        localStorage.setItem('userRoles', JSON.stringify(data.roles || []));

        console.log('Token stored in localStorage:', !!data.token);
        
        showMessage('Login successful!', 'success');
        
        // Show post section
        showPostSection(data.email, data.roles || []);
        loadUserPosts();
        
        // Clear login form
        document.getElementById('loginForm').reset();

    } catch (error) {
        console.error('Login error:', error);
        showMessage(error.message || 'Login failed. Please try again.', 'error');
    }
}

// Handle signup form submission
async function handleSignup(e) {
    e.preventDefault();

    const fullName = document.getElementById('signupFullName').value;
    const email = document.getElementById('signupEmail').value;
    const password = document.getElementById('signupPassword').value;

    showMessage('Registering...', 'info');

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ fullName, email, password })
        });

        const data = await response.json();
        if (!response.ok) {
            throw new Error(data.error || 'Registration failed');
        }

        showMessage('Registration successful! You are now logged in.', 'success');

        localStorage.setItem('authToken', data.token);
        localStorage.setItem('refreshToken', data.refreshToken);
        localStorage.setItem('userEmail', data.email);
        localStorage.setItem('userFullName', data.fullName);
        localStorage.setItem('userRoles', JSON.stringify(data.roles || []));

        showPostSection(data.email, data.roles || []);
        loadUserPosts();
        document.getElementById('signupForm').reset();
    } catch (error) {
        console.error('Signup error:', error);
        showMessage(error.message || 'Registration failed. Please try again.', 'error');
    }
}

// Handle add post form submission
async function handleAddPost(e) {
    e.preventDefault();

    // Get form values
    const title = document.getElementById('postTitle').value;
    const category = document.getElementById('postCategory').value;
    const content = document.getElementById('postContent').value;
    const token = localStorage.getItem('authToken');

    // Debug logs
    console.log('=== ADDING POST ===');
    console.log('Title:', title);
    console.log('Category:', category);
    console.log('Content length:', content.length);
    console.log('Token exists:', !!token);
    console.log('Token (first 20 chars):', token ? token.substring(0, 20) + '...' : 'none');
    console.log('API URL:', `${API_BASE_URL}/Posts`);

    if (!token) {
        showMessage('Please login first', 'error');
        showLoginSection();
        return;
    }

    showMessage('Adding post...', 'info');

    try {
        const requestBody = { title, category, content };
        console.log('Request body:', requestBody);

        const response = await fetch(`${API_BASE_URL}/Posts`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(requestBody)
        });

        console.log('Response status:', response.status);
        
        // Get response body
        const responseText = await response.text();
        console.log('Response body:', responseText);

        if (response.status === 401) {
            console.log('Token expired, attempting refresh...');
            const refreshed = await refreshToken();
            if (refreshed) {
                console.log('Token refreshed, retrying...');
                return handleAddPost(e);
            } else {
                throw new Error('Session expired. Please login again.');
            }
        }

        if (!response.ok) {
            let errorMessage = 'Failed to add post';
            try {
                const error = JSON.parse(responseText);
                errorMessage = error.error || errorMessage;
            } catch {
                errorMessage = responseText || errorMessage;
            }
            throw new Error(errorMessage);
        }

        const newPost = JSON.parse(responseText);
        console.log('Post created successfully:', newPost);
        
        showMessage('Post added successfully!', 'success');
        document.getElementById('addPostForm').reset();
        
        // Reload user posts
        loadUserPosts();

    } catch (error) {
        console.error('❌ Error adding post:', error);
        showMessage(error.message || 'Failed to add post. Please try again.', 'error');
    }
}

// Load posts created by current user
async function loadUserPosts() {
    const token = localStorage.getItem('authToken');
    const userEmail = localStorage.getItem('userEmail');
    const container = document.getElementById('user-posts-container');
    
    if (!token || !container) return;

    console.log('Loading user posts...');
    container.innerHTML = '<div class="loading">Loading your posts...</div>';

    try {
        const response = await fetch(`${API_BASE_URL}/Posts`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) throw new Error('Failed to load posts');

        const posts = await response.json();
        console.log(`Loaded ${posts.length} posts`);
        
        // Filter posts by current user's email (if they have Editor role)
        // Admins can see all posts
        const userRoles = JSON.parse(localStorage.getItem('userRoles') || '[]');
        const isAdmin = userRoles.includes('Admin');
        
        const userPosts = isAdmin ? posts : posts.filter(post => post.createdBy === userEmail);
        console.log(`Showing ${userPosts.length} posts for current user`);
        
        displayUserPosts(userPosts);

    } catch (error) {
        console.error('Error loading user posts:', error);
        container.innerHTML = '<div class="error">Failed to load your posts</div>';
    }
}

// Display user's posts with edit/delete options
function displayUserPosts(posts) {
    const container = document.getElementById('user-posts-container');
    const userRoles = JSON.parse(localStorage.getItem('userRoles') || '[]');
    const isAdmin = userRoles.includes('Admin');
    
    if (!posts || posts.length === 0) {
        container.innerHTML = '<p class="no-posts">You haven\'t created any posts yet.</p>';
        return;
    }

    container.innerHTML = posts.map(post => `
        <div class="post-card" data-id="${post.id}">
            <h3>${escapeHtml(post.title)}</h3>
            <div class="post-meta">
                <span class="category">${escapeHtml(post.category)}</span>
                <span class="date">${formatDate(post.createdAt)}</span>
            </div>
            <div class="post-actions">
                <button onclick="editPost(${post.id})" class="btn-edit">Edit</button>
                ${isAdmin ? `<button onclick="deletePost(${post.id})" class="btn-delete">Delete</button>` : ''}
            </div>
        </div>
    `).join('');
}

// Edit post function
async function editPost(id) {
    console.log('Editing post:', id);
    const token = localStorage.getItem('authToken');
    if (!token) {
        showMessage('Please login first', 'error');
        return;
    }

    try {
        // Fetch current post data
        const response = await fetch(`${API_BASE_URL}/Posts/${id}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch post');

        const post = await response.json();
        console.log('Loaded post for editing:', post);

        // Populate form with post data
        document.getElementById('postTitle').value = post.title;
        document.getElementById('postCategory').value = post.category;
        document.getElementById('postContent').value = post.content;

        // Change form to update mode
        const form = document.getElementById('addPostForm');
        const submitBtn = form.querySelector('button[type="submit"]');
        submitBtn.textContent = 'Update Post';
        
        // Handle update instead of create
        form.onsubmit = (e) => handleUpdatePost(e, id);

        // Scroll to form
        form.scrollIntoView({ behavior: 'smooth' });

    } catch (error) {
        console.error('Error editing post:', error);
        showMessage('Failed to load post for editing', 'error');
    }
}

// Handle update post
async function handleUpdatePost(e, id) {
    e.preventDefault();

    const title = document.getElementById('postTitle').value;
    const category = document.getElementById('postCategory').value;
    const content = document.getElementById('postContent').value;
    const token = localStorage.getItem('authToken');

    console.log('Updating post:', id, title);

    showMessage('Updating post...', 'info');

    try {
        const response = await fetch(`${API_BASE_URL}/Posts/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ 
                title, 
                category, 
                content,
                isPublished: true 
            })
        });

        console.log('Update response status:', response.status);

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to update post');
        }

        const updatedPost = await response.json();
        console.log('Post updated:', updatedPost);

        showMessage('Post updated successfully!', 'success');
        
        // Reset form to create mode
        resetPostForm();
        
        // Reload posts
        loadUserPosts();

    } catch (error) {
        console.error('Error updating post:', error);
        showMessage(error.message || 'Failed to update post', 'error');
    }
}

// Delete post function
async function deletePost(id) {
    console.log('Attempting to delete post:', id);
    
    if (!confirm('Are you sure you want to delete this post? This action cannot be undone.')) {
        return;
    }

    const token = localStorage.getItem('authToken');
    if (!token) {
        showMessage('Please login first', 'error');
        return;
    }

    showMessage('Deleting post...', 'info');

    try {
        const response = await fetch(`${API_BASE_URL}/Posts/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        console.log('Delete response status:', response.status);

        if (response.status === 401) {
            throw new Error('Unauthorized. Only admins can delete posts.');
        }

        if (!response.ok) {
            throw new Error('Failed to delete post');
        }

        showMessage('Post deleted successfully!', 'success');
        
        // Reload posts
        loadUserPosts();

    } catch (error) {
        console.error('Error deleting post:', error);
        showMessage(error.message || 'Failed to delete post', 'error');
    }
}

// Refresh token function
async function refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return false;

    console.log('Attempting to refresh token...');

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/refresh-token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ refreshToken })
        });

        if (!response.ok) {
            throw new Error('Failed to refresh token');
        }

        const data = await response.json();
        console.log('Token refreshed successfully');
        
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('refreshToken', data.refreshToken);
        return true;

    } catch (error) {
        console.error('Error refreshing token:', error);
        logout();
        return false;
    }
}

// Logout function
function logout() {
    console.log('Logging out...');
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userFullName');
    localStorage.removeItem('userRoles');
    
    showLoginSection();
    showMessage('Logged out successfully', 'success');
}

// Show signup section
function showSignupSection() {
    document.getElementById('login-section').style.display = 'none';
    document.getElementById('signup-section').style.display = 'block';
    document.getElementById('post-section').style.display = 'none';
}

// Show login section
function showLoginSection() {
    document.getElementById('login-section').style.display = 'block';
    document.getElementById('signup-section').style.display = 'none';
    document.getElementById('post-section').style.display = 'none';
}

// Show post section
function showPostSection(email, roles) {
    document.getElementById('login-section').style.display = 'none';
    document.getElementById('signup-section').style.display = 'none';
    document.getElementById('post-section').style.display = 'block';
    
    const roleText = roles.length ? ` (${roles.join(', ')})` : '';
    document.getElementById('userDisplay').textContent = `Logged in as: ${email}${roleText}`;
}

// Reset post form to create mode
function resetPostForm() {
    const form = document.getElementById('addPostForm');
    form.reset();
    
    const submitBtn = form.querySelector('button[type="submit"]');
    submitBtn.textContent = 'Add Post';
    
    // Restore original submit handler
    form.onsubmit = handleAddPost;
}

// Show message to user
function showMessage(text, type = 'info') {
    const container = document.getElementById('message-container');
    if (!container) return;

    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type}`;
    messageDiv.textContent = text;

    container.innerHTML = '';
    container.appendChild(messageDiv);

    // Auto-hide after 5 seconds
    setTimeout(() => {
        if (messageDiv.parentNode === container) {
            container.innerHTML = '';
        }
    }, 5000);
}

// Helper: Format date
function formatDate(dateString) {
    if (!dateString) return 'Unknown date';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Helper: Escape HTML
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Make functions global
window.editPost = editPost;
window.deletePost = deletePost;
window.logout = logout;