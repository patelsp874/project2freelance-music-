// API Configuration
// Loading Screen Management
function hideLoadingScreen() {
    const loadingScreen = document.getElementById('loadingScreen');
    if (loadingScreen) {
        loadingScreen.classList.add('fade-out');
        setTimeout(() => {
            loadingScreen.style.display = 'none';
        }, 500);
    }
}

// Animated Counter for Stats
function animateCounters() {
    const counters = document.querySelectorAll('.stat-number');
    
    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute('data-target'));
        const duration = 2000; // 2 seconds
        const increment = target / (duration / 16); // 60fps
        let current = 0;
        
        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                current = target;
                clearInterval(timer);
            }
            counter.textContent = Math.floor(current);
        }, 16);
    });
}

// Interactive Button Effects
function initInteractiveButtons() {
    const buttons = document.querySelectorAll('.interactive-btn');
    
    buttons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px) scale(1.02)';
        });
        
        button.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0) scale(1)';
        });
        
        button.addEventListener('click', function(e) {
            // Create ripple effect
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;
            
            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${x}px;
                top: ${y}px;
                background: rgba(255,255,255,0.3);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple 0.6s ease-out;
                pointer-events: none;
            `;
            
            this.appendChild(ripple);
            
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });
}

// Add ripple animation CSS (only if not already added)
if (!document.querySelector('style[data-ripple-css]')) {
    const rippleCSS = `
    @keyframes ripple {
        to {
            transform: scale(2);
            opacity: 0;
        }
    }
    `;

    // Inject ripple CSS
    const style = document.createElement('style');
    style.setAttribute('data-ripple-css', 'true');
    style.textContent = rippleCSS;
    document.head.appendChild(style);
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Hide loading screen after 2 seconds
    setTimeout(hideLoadingScreen, 2000);
    
    // Initialize interactive elements
    initInteractiveButtons();
    
    // Animate counters when hero section is visible
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animateCounters();
                observer.unobserve(entry.target);
            }
        });
    });
    
    const heroStats = document.querySelector('.hero-stats');
    if (heroStats) {
        observer.observe(heroStats);
    }
});

const API_BASE_URL = 'http://localhost:5168/api';

// Hash function that produces consistent results matching C#'s GetHashCode()
function simpleHash(str) {
    let hash = 0;
    if (str.length === 0) return hash;
    
    for (let i = 0; i < str.length; i++) {
        const char = str.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash | 0; // Convert to 32-bit signed integer
    }
    
    return hash;
}

// Test function to verify hash consistency (can be removed in production)
function testHashFunction() {
    const testPassword = "password123";
    const hash = simpleHash(testPassword);
    console.log(`Hash of "${testPassword}": ${hash}`);
    console.log(`Hash as string: "${hash.toString()}"`);
    
    // Test multiple times to ensure consistency
    for (let i = 0; i < 5; i++) {
        const testHash = simpleHash(testPassword);
        console.log(`Test ${i + 1}: ${testHash}`);
    }
    
    return hash;
}

// Authentication Functions
async function performSignupAPI(firstName, lastName, email, password, accountType) {
    try {
        console.log('Attempting signup with:', { FirstName: firstName, LastName: lastName, Email: email, AccountType: accountType });
        console.log('Password hash:', simpleHash(password));
        
        const response = await fetch(`${API_BASE_URL}/Auth/signup`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                FirstName: firstName, 
                LastName: lastName, 
                Email: email, 
                Password: simpleHash(password).toString(), 
                AccountType: accountType 
            })
        });

        console.log('Response status:', response.status);
        console.log('Response headers:', response.headers);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Signup API error:', errorText);
            throw new Error(`HTTP error! status: ${response.status} - ${errorText}`);
        }

        const result = await response.json();
        console.log('Signup result:', result);
        return result;
    } catch (error) {
        console.error('Signup error:', error);
        return { success: false, error: error.message };
    }
}

async function performLoginAPI(email, password) {
    try {
        console.log('Attempting login with:', { Email: email });
        console.log('Password hash:', simpleHash(password));
        
        const response = await fetch(`${API_BASE_URL}/Auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: email, Password: simpleHash(password).toString() })
        });

        console.log('Login response status:', response.status);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Login API error:', errorText);
            throw new Error(`HTTP error! status: ${response.status} - ${errorText}`);
        }

        const result = await response.json();
        console.log('Login result:', result);
        return result;
    } catch (error) {
        console.error('Login error:', error);
        return { success: false, error: error.message };
    }
}

async function logoutUserAPI() {
    try {
        // Since we're not using sessions anymore, logout is just frontend cleanup
        return { success: true, message: 'Logged out successfully' };
    } catch (error) {
        console.error('Logout error:', error);
        return { success: false, error: error.message };
    }
}

// Wrapper functions for backward compatibility
async function performSignup(firstName, lastName, email, password, accountType) {
    return await performSignupAPI(firstName, lastName, email, password, accountType);
}

async function performLogin(email, password) {
    return await performLoginAPI(email, password);
}

// Notification System
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

// Bottom Corner Notification System
function showBottomCornerNotification(message, type = 'success') {
    // Remove any existing bottom corner notifications
    const existingNotification = document.querySelector('.bottom-corner-notification');
    if (existingNotification) {
        existingNotification.remove();
    }
    
    const notification = document.createElement('div');
    notification.className = `bottom-corner-notification ${type}`;
    
    const iconMap = {
        success: '✓',
        error: '✕',
        warning: '⚠',
        info: 'ℹ'
    };
    
    notification.innerHTML = `
        <div class="notification-content">
            <div class="notification-icon">${iconMap[type] || iconMap.info}</div>
            <div class="notification-text">${message}</div>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
    `;
    
    document.body.appendChild(notification);
    
    // Auto remove after 4 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.style.animation = 'slideInFromBottom 0.3s ease-in reverse';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.remove();
                }
            }, 300);
        }
    }, 4000);
}

// Authentication UI Functions
window.showSignupModal = function() {
    const modal = new bootstrap.Modal(document.getElementById('signupModal'));
    modal.show();
};

window.showLoginModal = function() {
    const modal = new bootstrap.Modal(document.getElementById('loginModal'));
    modal.show();
};

window.handleSignup = async function() {
    const firstName = document.getElementById('signupFirstName').value.trim();
    const lastName = document.getElementById('signupLastName').value.trim();
    const email = document.getElementById('signupEmail').value.trim();
    const password = document.getElementById('signupPassword').value;
    const confirmPassword = document.getElementById('signupConfirmPassword').value;
    const accountType = document.getElementById('signupAccountType').value;

    if (!firstName || !lastName || !email || !password || !accountType) {
        showNotification('Please fill in all required fields.', 'warning');
        return;
    }

    if (password !== confirmPassword) {
        showNotification('Passwords do not match.', 'warning');
        return;
    }

    if (password.length < 6) {
        showNotification('Password must be at least 6 characters.', 'warning');
        return;
    }

    const result = await performSignup(firstName, lastName, email, password, accountType);
    
    if (result.success) {
        showBottomCornerNotification('Account created successfully! Please log in.', 'success');
        bootstrap.Modal.getInstance(document.getElementById('signupModal')).hide();
        // Clear form
        document.getElementById('signupForm').reset();
    } else {
        showNotification(result.error || 'Signup failed. Please try again.', 'danger');
    }
};

window.handleLogin = async function() {
    const email = document.getElementById('loginEmail').value.trim();
    const password = document.getElementById('loginPassword').value;

    if (!email || !password) {
        showNotification('Please enter both email and password.', 'warning');
        return;
    }

    const result = await performLogin(email, password);
    
    if (result.success) {
        sessionStorage.setItem('userEmail', email);
        sessionStorage.setItem('userName', result.userName);
        sessionStorage.setItem('accountType', result.accountType);
        
        showBottomCornerNotification(`Welcome back, ${result.userName}!`, 'success');
        bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
        
        // Update UI
        updateAuthUI();
        
        // Clear form
        document.getElementById('loginForm').reset();
    } else {
        showNotification(result.error || 'Login failed. Please check your credentials.', 'danger');
    }
};

window.logoutUser = async function() {
    showNotification('Logging you out...', 'info');
    
    const result = await logoutUserAPI();
    
    if (result.success) {
        // Clear local storage
        sessionStorage.removeItem('userEmail');
        sessionStorage.removeItem('userEmail');
        sessionStorage.removeItem('userName');
        sessionStorage.removeItem('accountType');
        
        showBottomCornerNotification('You have been logged out successfully.', 'success');
        
        // Update UI
        updateAuthUI();
        
        // Redirect to home page
        showPage('home');
    } else {
        showNotification(result.error || 'Logout failed. Please try again.', 'danger');
    }
};

// Debounce mechanism for updateAuthUI
let updateAuthUITimeout = null;

function updateAuthUI() {
    // Clear any pending update
    if (updateAuthUITimeout) {
        clearTimeout(updateAuthUITimeout);
    }
    
    // Debounce the update to prevent multiple rapid calls
    updateAuthUITimeout = setTimeout(() => {
        updateAuthUIInternal();
    }, 50);
}

function updateAuthUIInternal() {
    const userEmail = sessionStorage.getItem('userEmail');
    const userName = sessionStorage.getItem('userName');
    const accountType = sessionStorage.getItem('accountType');
    
    const loginSignupButtons = document.getElementById('loginSignupButtons');
    const userInfo = document.getElementById('userInfo');
    
    // Dashboard links
    const studentDashboardLink = document.getElementById('studentDashboardLink');
    const teacherDashboardLink = document.getElementById('teacherDashboardLink');
    const adminDashboardLink = document.getElementById('adminDashboardLink');
    
    console.log('updateAuthUI called:', { userEmail, userName, accountType, loginSignupButtons, userInfo });
    
    if (userEmail && userName) {
        // User is logged in
        console.log('User is logged in, hiding login/signup buttons and showing role-specific dashboard');
        if (loginSignupButtons) loginSignupButtons.classList.add('d-none');
        
        // Show user info
        if (userInfo) {
            userInfo.classList.remove('d-none');
            userInfo.innerHTML = `
                <span class="text-primary me-3">
                    Welcome, ${userName} (${accountType})
                </span>
                <button id="logoutBtn" class="btn btn-outline btn-sm" onclick="logoutUser()">Logout</button>
            `;
        }
        
        // Show only the dashboard for the user's role
        if (accountType) {
            // Hide all dashboard links first
            if (studentDashboardLink) studentDashboardLink.classList.add('d-none');
            if (teacherDashboardLink) teacherDashboardLink.classList.add('d-none');
            if (adminDashboardLink) adminDashboardLink.classList.add('d-none');
            
            // Show only the appropriate dashboard based on role
            switch (accountType.toLowerCase()) {
                case 'student':
                    if (studentDashboardLink) studentDashboardLink.classList.remove('d-none');
                    break;
                case 'teacher':
                    if (teacherDashboardLink) teacherDashboardLink.classList.remove('d-none');
                    break;
                case 'admin':
                    if (adminDashboardLink) adminDashboardLink.classList.remove('d-none');
                    break;
                default:
                    console.warn('Unknown account type:', accountType);
            }
        }
    } else {
        // User is not logged in
        console.log('User is not logged in, showing login/signup buttons and hiding all dashboards');
        if (loginSignupButtons) loginSignupButtons.classList.remove('d-none');
        if (userInfo) userInfo.classList.add('d-none');
        
        // Hide all dashboard links
        if (studentDashboardLink) studentDashboardLink.classList.add('d-none');
        if (teacherDashboardLink) teacherDashboardLink.classList.add('d-none');
        if (adminDashboardLink) adminDashboardLink.classList.add('d-none');
    }
}

// Student Dashboard API Functions
async function getAvailableTeachers(instrument = '') {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        console.log('Searching for teachers with instrument:', instrument);
        console.log('Using user email:', userEmail);

        const response = await fetch(`${API_BASE_URL}/Auth/teachers/list`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Instrument: instrument })
        });

        console.log('API response status:', response.status);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('API error response:', errorText);
            throw new Error(`HTTP error! status: ${response.status} - ${errorText}`);
        }

        const result = await response.json();
        console.log('API response data:', result);
        
        return result;
    } catch (error) {
        console.error('Get available teachers error:', error);
        return { success: false, error: error.message };
    }
}

async function getTeacherSchedule(teacherId) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        console.log('Getting teacher schedule for teacherId:', teacherId, 'type:', typeof teacherId);
        
        const response = await fetch(`${API_BASE_URL}/Auth/teacher-schedule/get`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ TeacherId: teacherId.toString() })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('Teacher schedule API response:', result);
        return result;
    } catch (error) {
        console.error('Get teacher schedule error:', error);
        return { success: false, error: error.message };
    }
}

async function bookLesson(teacherEmail, lessonDate) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        console.log('Booking lesson:', { StudentEmail: userEmail, TeacherEmail: teacherEmail, Day: lessonDate });

        const response = await fetch(`${API_BASE_URL}/Auth/student-studying/add`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                StudentEmail: userEmail, 
                TeacherEmail: teacherEmail, 
                Day: lessonDate
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Booking API error:', errorText);
            throw new Error(`HTTP error! status: ${response.status} - ${errorText}`);
        }

        const result = await response.json();
        console.log('Booking result:', result);
        return result;
    } catch (error) {
        console.error('Book lesson error:', error);
        return { success: false, error: error.message };
    }
}

async function getStudentLessons() {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/student-studying/get`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ StudentEmail: userEmail })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Get student lessons error:', error);
        return { success: false, error: error.message };
    }
}

// Teacher Dashboard API Functions
async function getTeacherProfile() {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-profile/get`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: userEmail })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Get teacher profile error:', error);
        return { success: false, error: error.message };
    }
}

async function createTeacherProfile(profileData) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-profile/create`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                Email: userEmail, 
                Name: profileData.name,
                Instrument: profileData.instrument,
                Bio: profileData.bio,
                ClassFull: profileData.classFull,
                ClassLimit: profileData.classLimit,
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Create teacher profile error:', error);
        return { success: false, error: error.message };
    }
}

async function updateTeacherProfileAPI(profileData) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-profile/update`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                Email: userEmail, 
                Name: profileData.name,
                Instrument: profileData.instrument,
                Bio: profileData.bio,
                ClassFull: profileData.classFull,
                ClassLimit: profileData.classLimit,
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Update teacher profile error:', error);
        return { success: false, error: error.message };
    }
}

async function addTeacherAvailabilityAPI(day, startTime, endTime) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-availability/add`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: userEmail, Day: day, StartTime: startTime, EndTime: endTime })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Add teacher availability error:', error);
        return { success: false, error: error.message };
    }
}

async function getTeacherAvailability() {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-availability/get`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: userEmail })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Get teacher availability error:', error);
        return { success: false, error: error.message };
    }
}

async function setTeacherAvailability(availability) {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/teacher-availability/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: userEmail, Availability: availability })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Set teacher availability error:', error);
        return { success: false, error: error.message };
    }
}

async function getTeacherLessons() {
    try {
        const userEmail = sessionStorage.getItem('userEmail');
        if (!userEmail) {
            return { success: false, error: 'No active session. Please login again.' };
        }

        const response = await fetch(`${API_BASE_URL}/Auth/student-studying/get`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ StudentEmail: userEmail })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        return result;
    } catch (error) {
        console.error('Get teacher lessons error:', error);
        return { success: false, error: error.message };
    }
}

// Student Dashboard UI Functions
window.loadStudentDashboard = async function() {
    const container = document.getElementById('student-dashboard');
    if (!container) return;

    // Check if user is logged in
    const userEmail = sessionStorage.getItem('userEmail');
    const accountType = sessionStorage.getItem('accountType');
    
    if (!userEmail) {
        container.innerHTML = `
            <div class="container-fluid py-4">
                <div class="alert alert-warning">
                    <h4>Please Log In</h4>
                    <p>You need to be logged in as a student to access the student dashboard.</p>
                    <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#loginModal">Log In</button>
                </div>
            </div>
        `;
        return;
    }

    if (accountType !== 'student') {
        container.innerHTML = `
            <div class="container-fluid py-4">
                <div class="alert alert-info">
                    <h4>Teacher Account</h4>
                    <p>You are logged in as a teacher. Switch to the teacher dashboard to manage your profile and lessons.</p>
                    <a href="#teacher-dashboard" class="btn btn-primary" onclick="showPage('teacher-dashboard')">Go to Teacher Dashboard</a>
                </div>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="container-fluid py-4">
            <div class="d-flex align-items-center justify-content-between flex-wrap gap-2 mb-4">
                <div>
                    <h1 class="h2 mb-0">Student Dashboard</h1>
                    <p class="text-muted mb-0">Find teachers, book lessons, and track your progress</p>
                </div>
                <div>
                    <a href="#home" class="btn btn-outline-secondary" onclick="showPage('home')"><i class="fas fa-arrow-left me-1"></i> Back to Home</a>
                </div>
            </div>

            <!-- Teacher Search -->
            <div class="card mb-4">
                <div class="card-header">
                    <h5><i class="fas fa-search me-2"></i>Find Teachers</h5>
                </div>
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-6">
                            <label for="instrumentFilter" class="form-label">Filter by Instrument:</label>
                            <select id="instrumentFilter" class="form-select">
                                <option value="">All Instruments</option>
                                <option value="Piano">Piano</option>
                                <option value="Guitar">Guitar</option>
                                <option value="Violin">Violin</option>
                                <option value="Drums">Drums</option>
                                <option value="Bass">Bass</option>
                                <option value="Saxophone">Saxophone</option>
                                <option value="Flute">Flute</option>
                                <option value="Voice">Voice</option>
                            </select>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">&nbsp;</label>
                            <div>
                                <button class="btn btn-primary" onclick="loadAvailableTeachers()">
                                    <i class="fas fa-search me-1"></i>Search Teachers
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Available Teachers -->
            <div class="card mb-4">
                <div class="card-header">
                    <h5><i class="fas fa-users me-2"></i>Available Teachers</h5>
                </div>
                <div class="card-body">
                    <div id="teachersList">
                        <div class="text-center text-muted">
                            <i class="fas fa-music fa-3x mb-3"></i>
                            <p>Click "Search Teachers" to see available teachers</p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- My Lessons -->
            <div class="card">
                <div class="card-header">
                    <h5><i class="fas fa-calendar-check me-2"></i>My Lessons</h5>
                </div>
                <div class="card-body">
                    <div id="studentLessons">Loading your lessons...</div>
                </div>
            </div>
        </div>
    `;

    await loadStudentLessons();
};

window.loadAvailableTeachers = async function() {
    const instrumentFilter = document.getElementById('instrumentFilter').value;
    const teachersList = document.getElementById('teachersList');
    
    teachersList.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div><p>Loading teachers...</p></div>';
    
    const result = await getAvailableTeachers(instrumentFilter);
    
    if (result.success && result.teachers && result.teachers.length > 0) {
        teachersList.innerHTML = result.teachers.map(teacher => `
            <div class="card mb-3">
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-8">
                            <h6 class="card-title">
                                <i class="fas fa-user-tie me-2 text-primary"></i>${teacher.name}
                            </h6>
                            <p class="card-text">
                                <strong><i class="fas fa-music me-1"></i>Instrument:</strong> ${teacher.instrument}<br>
                                <strong><i class="fas fa-info-circle me-1"></i>Bio:</strong> ${teacher.bio}<br>
                                <strong><i class="fas fa-envelope me-1"></i>Contact:</strong> ${teacher.email}<br>
                                <small class="text-muted">
                                    <i class="fas fa-calendar-check me-1"></i>${teacher.availabilityCount} available time slots
                                </small>
                            </p>
                        </div>
                        <div class="col-md-4 text-end">
                            <button class="btn btn-primary" onclick="viewTeacherSchedule(${teacher.userId}, '${teacher.name}')">
                                <i class="fas fa-calendar-alt me-1"></i>View Availability
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    } else {
        teachersList.innerHTML = '<div class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No teachers found. Try adjusting your search criteria.</div>';
    }
};

window.viewTeacherSchedule = async function(teacherId, teacherName) {
    const teachersList = document.getElementById('teachersList');
    teachersList.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div><p>Loading teacher availability...</p></div>';
    
    const result = await getTeacherSchedule(teacherId);
    console.log('viewTeacherSchedule result:', result);
    
    if (result.success && result.schedule && result.schedule.length > 0) {
        // Create a comprehensive availability view
        const availabilityHtml = `
            <div class="mb-3">
                <button class="btn btn-outline-secondary" onclick="loadAvailableTeachers()">
                    <i class="fas fa-arrow-left me-1"></i>Back to Teachers
                </button>
            </div>
            <div class="card">
                <div class="card-header">
                    <h5><i class="fas fa-calendar-alt me-2"></i>${teacherName}'s Availability</h5>
                </div>
                <div class="card-body">
                    <div class="row">
                        ${result.schedule.map(slot => `
                            <div class="col-md-6 col-lg-4 mb-3">
                                <div class="card border-success">
                                    <div class="card-body text-center">
                                        <h6 class="card-title text-success">${slot.day}</h6>
                                        <p class="card-text">
                                            <i class="fas fa-clock me-1"></i>
                                            ${slot.startTime} - ${slot.endTime}
                                        </p>
                                        <button class="btn btn-success btn-sm" onclick="bookLessonSlot(${teacherId}, '${teacherName}', '${slot.day}', '${slot.startTime}', '${slot.endTime}')">
                                            <i class="fas fa-bookmark me-1"></i>Book This Slot
                                        </button>
                                    </div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
        
        teachersList.innerHTML = availabilityHtml;
    } else {
        teachersList.innerHTML = `
            <div class="alert alert-info">
                <h6>No Availability Found</h6>
                <p>This teacher hasn't set their availability yet. Please try another teacher.</p>
                <button class="btn btn-outline-primary" onclick="loadAvailableTeachers()">Back to Teachers</button>
            </div>
        `;
    }
};

window.bookLessonSlot = async function(teacherId, teacherName, dayOfWeek, startTime, endTime) {
    // Get teacher's email from the teachers list
    const teachersResult = await getAvailableTeachers();
    if (!teachersResult.success || !teachersResult.teachers) {
        showNotification('Unable to load teacher information.', 'error');
        return;
    }
    
    const teacher = teachersResult.teachers.find(t => t.userId == teacherId);
    if (!teacher) {
        showNotification('Teacher not found.', 'error');
        return;
    }
    
    // Get teacher's schedule to determine available days
    const scheduleResult = await getTeacherSchedule(teacherId);
    
    if (!scheduleResult.success || !scheduleResult.schedule) {
        showNotification('Unable to load teacher schedule.', 'error');
        return;
    }
    
    // Find the specific time slot
    const timeSlot = scheduleResult.schedule.find(slot => 
        slot.day === dayOfWeek && 
        slot.startTime === startTime && 
        slot.endTime === endTime
    );
    
    if (!timeSlot) {
        showNotification('This time slot is no longer available.', 'error');
        return;
    }
    
    // Create a booking modal with calendar
    const modalHtml = `
        <div class="modal fade" id="bookingModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Book Lesson with ${teacherName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="alert alert-info">
                            <strong>Time Slot:</strong> ${dayOfWeek}, ${startTime} - ${endTime}
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Select Date</label>
                            <div id="lessonCalendar" class="calendar-container"></div>
                            <input type="hidden" id="selectedDate" />
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="confirmBooking('${teacher.email}', '${teacherName}', '${dayOfWeek}', '${startTime}', '${endTime}')">
                            <i class="fas fa-bookmark me-1"></i>Book Lesson
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('bookingModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    // Add modal to page
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('bookingModal'));
    modal.show();
    
    // Generate calendar with only available days
    generateLessonCalendar(dayOfWeek, startTime, endTime);
};

// Function to generate calendar with only available days
function generateLessonCalendar(dayOfWeek, startTime, endTime) {
    const calendarContainer = document.getElementById('lessonCalendar');
    const today = new Date();
    const currentMonth = today.getMonth();
    const currentYear = today.getFullYear();
    
    // Get day of week number (0 = Sunday, 1 = Monday, etc.)
    const dayMap = {
        'Sunday': 0, 'Monday': 1, 'Tuesday': 2, 'Wednesday': 3,
        'Thursday': 4, 'Friday': 5, 'Saturday': 6
    };
    const targetDayOfWeek = dayMap[dayOfWeek];
    
    // Generate calendar for next 3 months
    let calendarHtml = '<div class="calendar">';
    
    for (let monthOffset = 0; monthOffset < 3; monthOffset++) {
        const month = new Date(currentYear, currentMonth + monthOffset, 1);
        const monthName = month.toLocaleString('default', { month: 'long', year: 'numeric' });
        
        calendarHtml += `
            <div class="calendar-month mb-4">
                <h6 class="text-center mb-3">${monthName}</h6>
                <div class="calendar-grid">
                    <div class="calendar-header">
                        <div class="calendar-day-header">Sun</div>
                        <div class="calendar-day-header">Mon</div>
                        <div class="calendar-day-header">Tue</div>
                        <div class="calendar-day-header">Wed</div>
                        <div class="calendar-day-header">Thu</div>
                        <div class="calendar-day-header">Fri</div>
                        <div class="calendar-day-header">Sat</div>
                    </div>
                    <div class="calendar-body">
        `;
        
        // Get first day of month and number of days
        const firstDay = month.getDay();
        const daysInMonth = new Date(currentYear, currentMonth + monthOffset + 1, 0).getDate();
        
        // Add empty cells for days before month starts
        for (let i = 0; i < firstDay; i++) {
            calendarHtml += '<div class="calendar-day empty"></div>';
        }
        
        // Add days of month
        for (let day = 1; day <= daysInMonth; day++) {
            const date = new Date(currentYear, currentMonth + monthOffset, day);
            const dayOfWeekNum = date.getDay();
            const isPast = date < today;
            const isTargetDay = dayOfWeekNum === targetDayOfWeek;
            const isAvailable = isTargetDay && !isPast;
            
            const dateString = date.toISOString().split('T')[0];
            
            calendarHtml += `
                <div class="calendar-day ${isAvailable ? 'available' : 'unavailable'} ${isPast ? 'past' : ''}" 
                     ${isAvailable ? `onclick="selectDate('${dateString}')"` : ''}>
                    ${day}
                </div>
            `;
        }
        
        calendarHtml += `
                    </div>
                </div>
            </div>
        `;
    }
    
    calendarHtml += '</div>';
    calendarContainer.innerHTML = calendarHtml;
}

// Function to select a date
window.selectDate = function(dateString) {
    // Remove previous selection
    document.querySelectorAll('.calendar-day.selected').forEach(day => {
        day.classList.remove('selected');
    });
    
    // Add selection to clicked day
    event.target.classList.add('selected');
    
    // Store selected date
    document.getElementById('selectedDate').value = dateString;
    
    // Show confirmation
    const date = new Date(dateString);
    const formattedDate = date.toLocaleDateString('en-US', { 
        weekday: 'long', 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric' 
    });
    
    showNotification(`Selected: ${formattedDate}`, 'success');
};

window.confirmBooking = async function(teacherEmail, teacherName, dayOfWeek, startTime, endTime) {
    const lessonDate = document.getElementById('selectedDate').value;
    
    if (!lessonDate) {
        showNotification('Please select a date for your lesson.', 'warning');
        return;
    }
    
    // Validate date is not in the past
    const today = new Date().toISOString().split('T')[0];
    if (lessonDate < today) {
        showNotification('Please select a future date.', 'warning');
        return;
    }
    
    const result = await bookLesson(teacherEmail, lessonDate);
    
    if (result.success) {
        showNotification(`Lesson booked successfully with ${teacherName}!`, 'success');
        bootstrap.Modal.getInstance(document.getElementById('bookingModal')).hide();
        await loadStudentLessons(); // Refresh lessons
        await loadAvailableTeachers(); // Go back to teachers list
    } else {
        showNotification(result.error || 'Failed to book lesson. Please try again.', 'danger');
    }
};

async function loadStudentLessons() {
    const studentLessons = document.getElementById('studentLessons');
    if (!studentLessons) return;
    
    const result = await getStudentLessons();
    
    if (result.success && result.lessons && result.lessons.length > 0) {
        studentLessons.innerHTML = `
            <div class="table-responsive">
                <table class="table table-striped">
                    <thead>
                        <tr>
                            <th><i class="fas fa-user-tie me-1"></i>Teacher</th>
                            <th><i class="fas fa-music me-1"></i>Instrument</th>
                            <th><i class="fas fa-calendar me-1"></i>Date</th>
                            <th><i class="fas fa-clock me-1"></i>Time</th>
                            <th><i class="fas fa-laptop me-1"></i>Type</th>
                            <th><i class="fas fa-check-circle me-1"></i>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${result.lessons.map(lesson => `
                            <tr>
                                <td><strong>${lesson.teacherName || 'N/A'}</strong></td>
                                <td><span class="badge bg-secondary">${lesson.instrument}</span></td>
                                <td>${lesson.lessonDate}</td>
                                <td>${lesson.lessonTime}</td>
                                <td><span class="badge ${lesson.lessonType === 'Virtual' ? 'bg-info' : 'bg-warning'}">${lesson.lessonType}</span></td>
                                <td><span class="badge bg-success">${lesson.status}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    } else {
        studentLessons.innerHTML = `
            <div class="alert alert-info">
                <i class="fas fa-info-circle me-2"></i>
                <strong>No lessons booked yet!</strong><br>
                Find a teacher above and book your first lesson to get started.
            </div>
        `;
    }
}

// Teacher Dashboard UI Functions
window.loadTeacherDashboard = async function() {
    const container = document.getElementById('teacher-dashboard');
    if (!container) return;

    // Check if user is logged in
    const userEmail = sessionStorage.getItem('userEmail');
    const accountType = sessionStorage.getItem('accountType');
    
    if (!userEmail) {
        container.innerHTML = `
            <div class="container-fluid py-4">
                <div class="alert alert-warning">
                    <h4>Please Log In</h4>
                    <p>You need to be logged in as a teacher to access the teacher dashboard.</p>
                    <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#loginModal">Log In</button>
                </div>
            </div>
        `;
        return;
    }

    if (accountType !== 'teacher') {
        container.innerHTML = `
            <div class="container-fluid py-4">
                <div class="alert alert-info">
                    <h4>Student Account</h4>
                    <p>You are logged in as a student. Switch to the student dashboard to find teachers and book lessons.</p>
                    <a href="#student-dashboard" class="btn btn-primary" onclick="showPage('student-dashboard')">Go to Student Dashboard</a>
                </div>
            </div>
        `;
        return;
    }

    // Check if teacher profile is complete
    try {
        const profileResult = await getTeacherProfile();
        
        if (profileResult.success && profileResult.profile) {
            // Profile exists, show dashboard content
            showTeacherDashboardContent();
            loadTeacherDashboardData();
        } else {
            // Profile incomplete, show onboarding form
            showTeacherOnboardingForm();
        }
    } catch (error) {
        console.error('Error checking teacher profile:', error);
        // On error, show onboarding form
        showTeacherOnboardingForm();
    }
}

function showTeacherOnboardingForm() {
    const onboardingDiv = document.getElementById('teacher-onboarding');
    const dashboardDiv = document.getElementById('teacher-dashboard-content');
    
    if (onboardingDiv) onboardingDiv.classList.remove('d-none');
    if (dashboardDiv) dashboardDiv.classList.add('d-none');
    
    // Auto-fill name and email from session storage
    const userName = sessionStorage.getItem('userName');
    const userEmail = sessionStorage.getItem('userEmail');
    
    if (userName) {
        const nameInput = document.getElementById('teacherName');
        if (nameInput) nameInput.value = userName;
    }
    
    if (userEmail) {
        const emailInput = document.getElementById('teacherEmail');
        if (emailInput) emailInput.value = userEmail;
    }
    
    // Add form submission handler
    const form = document.getElementById('teacherOnboardingForm');
    if (form) {
        form.addEventListener('submit', handleTeacherOnboarding);
    }
}

function showTeacherDashboardContent() {
    const onboardingDiv = document.getElementById('teacher-onboarding');
    const dashboardDiv = document.getElementById('teacher-dashboard-content');
    
    if (onboardingDiv) onboardingDiv.classList.add('d-none');
    if (dashboardDiv) dashboardDiv.classList.remove('d-none');
}

async function handleTeacherOnboarding(event) {
    event.preventDefault();
    
    const formData = {
        name: document.getElementById('teacherName').value.trim(),
        instrument: document.getElementById('teacherInstrument').value,
        classFull: document.getElementById('teacherClassFull').checked ? 1 : 0,
        classLimit: parseInt(document.getElementById('teacherClassLimit').value),
        email: document.getElementById('teacherEmail').value.trim(),
        bio: document.getElementById('teacherBio').value.trim()
    };
    
    // Validate form data
    if (!formData.name || !formData.instrument || !formData.classLimit || 
        !formData.email || !formData.bio) {
        alert('Please fill in all required fields.');
        return;
    }
    
    try {
        const result = await createTeacherProfile(formData);
        
        if (result.success) {
            // Profile created successfully, show dashboard
            showTeacherDashboardContent();
            loadTeacherDashboardData();
            
            // Show success message
            showNotification('Profile created successfully! You can now start teaching.', 'success');
        } else {
            alert('Error creating profile: ' + result.error);
        }
    } catch (error) {
        console.error('Error creating teacher profile:', error);
        alert('Error creating profile. Please try again.');
    }
}

async function loadTeacherDashboardData() {
    try {
        // Load teacher profile data
        const profileResult = await getTeacherProfile();
        
        if (profileResult.success && profileResult.profile) {
            const profile = profileResult.profile;
            
            // Update dashboard stats (placeholder data for now)
            document.getElementById('totalStudents').textContent = '0';
            document.getElementById('weeklyLessons').textContent = '0';
            document.getElementById('monthlyEarnings').textContent = '$0';
            document.getElementById('teacherRating').textContent = '5.0';
            
            // Load upcoming lessons (placeholder for now)
            const upcomingLessonsDiv = document.getElementById('upcomingLessons');
            if (upcomingLessonsDiv) {
                upcomingLessonsDiv.innerHTML = `
                    <div class="text-center py-4">
                        <i class="fas fa-calendar-times fa-3x text-muted mb-3"></i>
                        <h6 class="text-muted">No upcoming lessons</h6>
                        <p class="text-muted">Students will be able to book lessons once you set your availability.</p>
                </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading teacher dashboard data:', error);
    }
}

// Modal functions for teacher dashboard
function showAvailabilityModal() {
    const modal = new bootstrap.Modal(document.getElementById('availabilityModal'));
    modal.show();
    loadCurrentAvailability();
}

function showEditProfileModal() {
    const modal = new bootstrap.Modal(document.getElementById('editProfileModal'));
    modal.show();
    loadTeacherProfileForEdit();
}

async function loadTeacherProfileForEdit() {
    try {
        const result = await getTeacherProfile();
        
        if (result.success && result.profile) {
            const profile = result.profile;
            
            // Populate edit form fields
            document.getElementById('editTeacherName').value = profile.name || '';
            document.getElementById('editTeacherInstrument').value = profile.instrument || '';
            document.getElementById('editTeacherClassFull').checked = profile.classFull || false;
            document.getElementById('editTeacherClassLimit').value = profile.classLimit || '';
            document.getElementById('editTeacherEmail').value = profile.email || '';
            document.getElementById('editTeacherBio').value = profile.bio || '';
        }
    } catch (error) {
        console.error('Error loading teacher profile for edit:', error);
    }
}

async function updateTeacherProfile() {
    const formData = {
        name: document.getElementById('editTeacherName').value.trim(),
        instrument: document.getElementById('editTeacherInstrument').value,
        classFull: document.getElementById('editTeacherClassFull').checked ? 1 : 0,
        classLimit: parseInt(document.getElementById('editTeacherClassLimit').value),
        email: document.getElementById('editTeacherEmail').value.trim(),
        bio: document.getElementById('editTeacherBio').value.trim()
    };
    
    // Validate form data
    if (!formData.name || !formData.instrument || !formData.classLimit || 
        !formData.email || !formData.bio) {
        alert('Please fill in all required fields.');
        return;
    }
    
    try {
        const result = await updateTeacherProfileAPI(formData);
        
        if (result.success) {
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('editProfileModal'));
            modal.hide();
            
            // Reload dashboard data
            loadTeacherDashboardData();
            
            // Show success message
            showNotification('Profile updated successfully!', 'success');
        } else {
            alert('Error updating profile: ' + result.error);
        }
    } catch (error) {
        console.error('Error updating teacher profile:', error);
        alert('Error updating profile. Please try again.');
    }
}

async function addTeacherAvailability() {
    const day = document.getElementById('availabilityDay').value;
    const startTime = document.getElementById('availabilityStartTime').value;
    const endTime = document.getElementById('availabilityEndTime').value;
    
    if (!day || !startTime || !endTime) {
        alert('Please fill in all availability fields.');
        return;
    }
    
    try {
        const result = await addTeacherAvailabilityAPI(day, startTime, endTime);
        
        if (result.success) {
            // Clear form
            document.getElementById('availabilityDay').value = '';
            document.getElementById('availabilityStartTime').value = '';
            document.getElementById('availabilityEndTime').value = '';
            
            // Reload availability list
            loadCurrentAvailability();
            
            // Show success message
            showNotification('Availability added successfully!', 'success');
        } else {
            alert('Error adding availability: ' + result.error);
        }
    } catch (error) {
        console.error('Error adding teacher availability:', error);
        alert('Error adding availability. Please try again.');
    }
}

async function loadCurrentAvailability() {
    try {
        const result = await getTeacherAvailability();
        
        const availabilityList = document.getElementById('currentAvailabilityList');
        if (!availabilityList) return;
        
        if (result.success && result.availability && result.availability.length > 0) {
            availabilityList.innerHTML = result.availability.map(avail => `
                <div class="d-flex justify-content-between align-items-center mb-2 p-2 border rounded">
                    <div>
                        <strong>${avail.day}</strong><br>
                        <small class="text-muted">${avail.startTime} - ${avail.endTime}</small>
                </div>
                    <button class="btn btn-sm btn-outline-danger" onclick="removeAvailability('${avail.day}')">
                        <i class="fas fa-trash"></i>
                        </button>
                    </div>
            `).join('');
        } else {
            availabilityList.innerHTML = '<p class="text-muted mb-0">No availability set yet.</p>';
        }
    } catch (error) {
        console.error('Error loading current availability:', error);
    }
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.parentNode.removeChild(notification);
        }
    }, 5000);
}

async function loadTeacherProfile() {
    const teacherProfileSummary = document.getElementById('teacherProfileSummary');
    if (!teacherProfileSummary) return;
    
    const result = await getTeacherProfile();
    
    if (result.success && result.profile) {
        const profile = result.profile;
        teacherProfileSummary.innerHTML = `
            <div class="row">
                <div class="col-md-8">
                    <h6>${profile.name}</h6>
                    <p><strong>Instrument:</strong> ${profile.instrument}</p>
                    <p><strong>Bio:</strong> ${profile.bio}</p>
                    <p><strong>Contact:</strong> ${profile.email}</p>
                </div>
                <div class="col-md-4">
                    <div class="text-end">
                        <span class="badge bg-success">Profile Complete</span>
                    </div>
                </div>
            </div>
        `;
        
        // Populate modal fields for editing
        document.getElementById('teacherName').value = profile.name;
        document.getElementById('teacherInstrument').value = profile.instrument;
        document.getElementById('teacherBio').value = profile.bio;
        document.getElementById('teacherContact').value = profile.email;
    } else {
        teacherProfileSummary.innerHTML = `
            <div class="alert alert-warning">
                <h6>Profile Not Created</h6>
                <p>Please create your teacher profile to start teaching.</p>
                <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#teacherProfileModal">
                    Create Profile
                </button>
            </div>
        `;
        
        // Clear modal fields for new profile
        document.getElementById('teacherName').value = '';
        document.getElementById('teacherInstrument').value = '';
        document.getElementById('teacherBio').value = '';
        document.getElementById('teacherContact').value = '';
    }
}

window.saveTeacherAvailability = async function() {
    const availability = [];
    
    ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'].forEach(day => {
        const isAvailable = document.getElementById(`${day}Available`).checked;
        const startTime = document.getElementById(`${day}Start`).value;
        const endTime = document.getElementById(`${day}End`).value;
        
        if (isAvailable && startTime && endTime) {
            availability.push({
                dayOfWeek: day.charAt(0).toUpperCase() + day.slice(1),
                startTime: startTime,
                endTime: endTime
            });
        }
    });
    
    if (availability.length === 0) {
        showNotification('Please select at least one available time slot.', 'warning');
        return;
    }
    
    const result = await setTeacherAvailability(availability);
    
    if (result.success) {
        showNotification('Availability saved successfully!', 'success');
    } else {
        showNotification(result.error || 'Failed to save availability. Please try again.', 'danger');
    }
};

async function loadTeacherLessons() {
    const teacherLessons = document.getElementById('teacherLessons');
    if (!teacherLessons) return;
    
    const result = await getTeacherLessons();
    
    if (result.success && result.lessons && result.lessons.length > 0) {
        teacherLessons.innerHTML = `
            <div class="table-responsive">
                <table class="table table-striped">
                    <thead>
                        <tr>
                            <th><i class="fas fa-user me-1"></i>Student</th>
                            <th><i class="fas fa-music me-1"></i>Instrument</th>
                            <th><i class="fas fa-calendar me-1"></i>Date</th>
                            <th><i class="fas fa-clock me-1"></i>Time</th>
                            <th><i class="fas fa-laptop me-1"></i>Type</th>
                            <th><i class="fas fa-check-circle me-1"></i>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${result.lessons.map(lesson => `
                            <tr>
                                <td><strong>${lesson.studentName}</strong></td>
                                <td><span class="badge bg-secondary">${lesson.instrument}</span></td>
                                <td>${lesson.lessonDate}</td>
                                <td>${lesson.lessonTime}</td>
                                <td><span class="badge ${lesson.lessonType === 'Virtual' ? 'bg-info' : 'bg-warning'}">${lesson.lessonType}</span></td>
                                <td><span class="badge bg-success">${lesson.status}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    } else {
        teacherLessons.innerHTML = `
            <div class="alert alert-info">
                <i class="fas fa-info-circle me-2"></i>
                <strong>No lessons booked yet!</strong><br>
                Set your availability above to start receiving bookings from students.
            </div>
        `;
    }
}

async function loadTeacherAvailability() {
    const result = await getTeacherAvailability();
    
    if (result.success && result.availability) {
        result.availability.forEach(slot => {
            const day = slot.dayOfWeek.toLowerCase();
            const availableCheckbox = document.getElementById(`${day}Available`);
            const startTimeInput = document.getElementById(`${day}Start`);
            const endTimeInput = document.getElementById(`${day}End`);
            
            if (availableCheckbox && startTimeInput && endTimeInput) {
                availableCheckbox.checked = true;
                startTimeInput.value = slot.startTime;
                endTimeInput.value = slot.endTime;
            }
        });
    }
}

window.saveTeacherProfile = async function() {
    const name = document.getElementById('teacherName').value.trim();
    const instrument = document.getElementById('teacherInstrument').value;
    const bio = document.getElementById('teacherBio').value.trim();
    const contactInfo = document.getElementById('teacherContact').value.trim();
    
    if (!name || !instrument || !bio || !contactInfo) {
        showNotification('Please fill in all fields.', 'warning');
        return;
    }
    
    // Check if profile exists to determine create vs update
    const existingProfile = await getTeacherProfile();
    let result;
    
    if (existingProfile.success && existingProfile.profile) {
        result = await updateTeacherProfile(name, instrument, bio, contactInfo);
    } else {
        result = await createTeacherProfile(name, instrument, bio, contactInfo);
    }
    
    if (result.success) {
        showNotification('Profile saved successfully!', 'success');
        bootstrap.Modal.getInstance(document.getElementById('teacherProfileModal')).hide();
        await loadTeacherProfile(); // Refresh profile display
    } else {
        showNotification(result.error || 'Failed to save profile. Please try again.', 'danger');
    }
};

// Password visibility toggle
window.togglePasswordVisibility = function(inputId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(inputId + 'Icon');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
};

// Modal switching functionality
window.switchToSignup = function() {
    const loginModalInstance = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
    if (loginModalInstance) {
        loginModalInstance.hide();
    }
    
    // Wait for modal to be fully hidden before showing the next one
    setTimeout(() => {
        // Remove focus from any elements in the hidden modal
        document.getElementById('loginModal').querySelectorAll('*').forEach(el => {
            if (el === document.activeElement) {
                el.blur();
            }
        });
        
        const signupModal = new bootstrap.Modal(document.getElementById('signupModal'));
        signupModal.show();
    }, 500);
};

window.switchToLogin = function() {
    const signupModalInstance = bootstrap.Modal.getInstance(document.getElementById('signupModal'));
    if (signupModalInstance) {
        signupModalInstance.hide();
    }
    
    // Wait for modal to be fully hidden before showing the next one
    setTimeout(() => {
        // Remove focus from any elements in the hidden modal
        document.getElementById('signupModal').querySelectorAll('*').forEach(el => {
            if (el === document.activeElement) {
                el.blur();
            }
        });
        
        const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
        loginModal.show();
    }, 500);
};

// Enhanced form validation
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validatePassword(password) {
    return password.length >= 6;
}

function showModalError(modalId, message) {
    const errorDiv = document.getElementById(modalId + 'Error');
    const successDiv = document.getElementById(modalId + 'Success');
    
    if (errorDiv && successDiv) {
        errorDiv.textContent = message;
        errorDiv.classList.remove('d-none');
        successDiv.classList.add('d-none');
    }
}

function showModalSuccess(modalId, message) {
    const errorDiv = document.getElementById(modalId + 'Error');
    const successDiv = document.getElementById(modalId + 'Success');
    
    if (errorDiv && successDiv) {
        successDiv.textContent = message;
        successDiv.classList.remove('d-none');
        errorDiv.classList.add('d-none');
    }
}

function hideModalMessages(modalId) {
    const errorDiv = document.getElementById(modalId + 'Error');
    const successDiv = document.getElementById(modalId + 'Success');
    
    if (errorDiv) errorDiv.classList.add('d-none');
    if (successDiv) successDiv.classList.add('d-none');
}

function setButtonLoading(buttonId, isLoading) {
    const button = document.querySelector(`[onclick="${buttonId}"]`);
    if (button) {
        const textSpan = button.querySelector('.btn-text');
        const spinnerSpan = button.querySelector('.btn-spinner');
        
        if (isLoading) {
            button.classList.add('loading');
            button.disabled = true;
        } else {
            button.classList.remove('loading');
            button.disabled = false;
        }
    }
}

// Initialize auth UI on page load
document.addEventListener('DOMContentLoaded', function() {
    updateAuthUI();
    
    // Add form validation on input
    const emailInputs = document.querySelectorAll('input[type="email"]');
    emailInputs.forEach(input => {
        input.addEventListener('blur', function() {
            if (this.value && !validateEmail(this.value)) {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
            } else if (this.value) {
                this.classList.add('is-valid');
                this.classList.remove('is-invalid');
            }
        });
    });
    
    const passwordInputs = document.querySelectorAll('input[type="password"]');
    passwordInputs.forEach(input => {
        input.addEventListener('blur', function() {
            if (this.value && !validatePassword(this.value)) {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
            } else if (this.value) {
                this.classList.add('is-valid');
                this.classList.remove('is-invalid');
            }
        });
    });
});

// Minimal navigation & smooth scrolling

(function() {
    const scrollToHash = (hash) => {
        if (!hash || hash === '#') return;
        const target = document.querySelector(hash);
        if (target) {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    // Header links: prefer native hash + smooth scroll
    document.addEventListener('click', (e) => {
        const link = e.target.closest('a[href^="#"]');
        if (!link) return;
        const href = link.getAttribute('href');
        if (!href || href === '#') return;
        
        // Handle admin dashboard navigation
        if (href === '#admin-dashboard') {
            showPage('admin-dashboard');
            return;
        }
        
        // Allow default to set the hash, then smooth scroll
        setTimeout(() => scrollToHash(href), 0);
    });

    // On load and hash changes
    window.addEventListener('load', () => {
        const yearEl = document.getElementById('year');
        if (yearEl) yearEl.textContent = new Date().getFullYear();
        if (location.hash === '#admin-dashboard') {
            showPage('admin-dashboard');
        } else if (location.hash === '#teacher-dashboard') {
            showPage('teacher-dashboard');
        } else if (location.hash === '#student-dashboard') {
            showPage('student-dashboard');
        } else {
            // Default to home
            showPage('home');
            if (location.hash) scrollToHash(location.hash);
        }

        // Hide loader after a short delay to showcase animation
        const loader = document.getElementById('loader');
        if (loader) {
            setTimeout(() => {
                loader.style.opacity = '0';
                loader.style.transition = 'opacity .5s ease';
                setTimeout(() => loader.remove(), 500);
            }, 2000);
        }

        // Scroll reveal
        const revealItems = document.querySelectorAll('.reveal');
        const reveal = () => {
            const trigger = window.innerHeight * 0.9;
            revealItems.forEach(el => {
                const rect = el.getBoundingClientRect();
                if (rect.top < trigger) el.classList.add('revealed');
            });
        };
        window.addEventListener('scroll', reveal, { passive: true });
        reveal();

        // Parallax on culture visual
        const pv = document.querySelector('.parallax');
        if (pv) {
            const onScroll = () => {
                const y = window.scrollY || window.pageYOffset;
                pv.style.transform = `translateY(${Math.min(0, (y - pv.offsetTop) * 0.05)}px)`;
            };
            window.addEventListener('scroll', onScroll, { passive: true });
            onScroll();
        }

        // Initialize admin dashboard if needed
        if (location.hash === '#admin-dashboard') {
            loadAdminDashboard();
        }
    });
    window.addEventListener('hashchange', () => {
        if (location.hash === '#admin-dashboard') {
            showPage('admin-dashboard');
        } else if (location.hash === '#teacher-dashboard') {
            showPage('teacher-dashboard');
        } else if (location.hash === '#student-dashboard') {
            showPage('student-dashboard');
        } else {
            showPage('home');
            if (location.hash) scrollToHash(location.hash);
        }
    });
})();

// ===========================================
// PAGE NAVIGATION
// ===========================================

function showPage(pageId) {
    // Hide all page content
    const allPages = document.querySelectorAll('.page-content');
    allPages.forEach(page => page.classList.add('d-none'));
    
    // Show the requested page
    const targetPage = document.getElementById(pageId);
    if (targetPage) {
        targetPage.classList.remove('d-none');
        
        // Load specific page data
        if (pageId === 'admin-dashboard') {
            loadAdminDashboard();
        } else if (pageId === 'teacher-dashboard') {
            loadTeacherDashboard();
        } else if (pageId === 'student-dashboard') {
            loadStudentDashboard();
        }
    }
    
    // Handle landing page elements visibility
    const landingPageElements = [
        document.querySelector('.ads-band'),
        document.querySelector('.feature-band'),
        document.querySelector('.py-5'), // testimonials section
        document.querySelector('.site-footer')
    ];
    
    const isDashboardPage = ['admin-dashboard', 'teacher-dashboard', 'student-dashboard'].includes(pageId);
    
    landingPageElements.forEach(element => {
        if (element) {
            if (isDashboardPage) {
                element.classList.add('d-none');
            } else {
                element.classList.remove('d-none');
            }
        }
    });
    
    // Update URL hash
    location.hash = pageId;
}

// ===========================================
// ADMIN DASHBOARD FUNCTIONALITY
// ===========================================

// Mock data for admin dashboard
const adminData = {
    revenue: {
        quarters: ['Q1 2024', 'Q2 2024', 'Q3 2024', 'Q4 2024'],
        amounts: [125000, 145000, 168000, 192000]
    },
    referrals: {
        sources: ['Social Media', 'Google Search', 'Word of Mouth', 'Email Marketing', 'Referral Program'],
        percentages: [35, 28, 20, 12, 5]
    },
    instruments: [
        { name: 'Piano', lessons: 1240 },
        { name: 'Guitar', lessons: 980 },
        { name: 'Violin', lessons: 650 },
        { name: 'Voice', lessons: 580 },
        { name: 'Drums', lessons: 420 },
        { name: 'Bass', lessons: 320 }
    ],
    lessons: [
        { id: 1, student: 'Sarah Johnson', teacher: 'Mike Chen', instrument: 'Piano', date: '2024-01-15', time: '10:00 AM', status: 'completed' },
        { id: 2, student: 'David Smith', teacher: 'Lisa Wang', instrument: 'Guitar', date: '2024-01-16', time: '2:00 PM', status: 'upcoming' },
        { id: 3, student: 'Emma Davis', teacher: 'John Rodriguez', instrument: 'Violin', date: '2024-01-17', time: '4:30 PM', status: 'completed' },
        { id: 4, student: 'Alex Brown', teacher: 'Maria Garcia', instrument: 'Voice', date: '2024-01-18', time: '11:00 AM', status: 'upcoming' },
        { id: 5, student: 'Chris Wilson', teacher: 'Tom Anderson', instrument: 'Drums', date: '2024-01-19', time: '3:00 PM', status: 'completed' }
    ],
    users: [
        { id: 1, name: 'Sarah Johnson', email: 'sarah@email.com', role: 'student', joinDate: '2023-08-15' },
        { id: 2, name: 'Mike Chen', email: 'mike@email.com', role: 'teacher', joinDate: '2023-07-20' },
        { id: 3, name: 'David Smith', email: 'david@email.com', role: 'student', joinDate: '2023-09-10' },
        { id: 4, name: 'Lisa Wang', email: 'lisa@email.com', role: 'teacher', joinDate: '2023-06-05' },
        { id: 5, name: 'Emma Davis', email: 'emma@email.com', role: 'student', joinDate: '2023-10-12' }
    ],
    repeatStats: {
        totalStudents: 150,
        repeatStudents: 95,
        percentage: 63.3
    },
    revenueDistribution: {
        byInstrument: [
            { instrument: 'Piano', revenue: 45000, percentage: 23.4 },
            { instrument: 'Guitar', revenue: 38000, percentage: 19.8 },
            { instrument: 'Violin', revenue: 32000, percentage: 16.7 },
            { instrument: 'Voice', revenue: 28000, percentage: 14.6 },
            { instrument: 'Drums', revenue: 25000, percentage: 13.0 },
            { instrument: 'Bass', revenue: 24000, percentage: 12.5 }
        ],
        byStudent: [
            { student: 'Sarah Johnson', revenue: 2400, percentage: 1.25 },
            { student: 'David Smith', revenue: 1800, percentage: 0.94 },
            { student: 'Emma Davis', revenue: 1600, percentage: 0.83 },
            { student: 'Alex Brown', revenue: 1400, percentage: 0.73 },
            { student: 'Chris Wilson', revenue: 1200, percentage: 0.63 }
        ]
    }
};

let charts = {};

function loadAdminDashboard() {
    updateQuickStats();
    initializeCharts();
    loadTables();
    setupEventListeners();
}

function updateQuickStats() {
    const totalRevenue = adminData.revenue.amounts.reduce((sum, amount) => sum + amount, 0);
    document.getElementById('totalRevenue').textContent = `$${totalRevenue.toLocaleString()}`;
    document.getElementById('totalUsers').textContent = adminData.users.length;
    document.getElementById('totalLessons').textContent = adminData.lessons.length;
    document.getElementById('repeatRate').textContent = `${adminData.repeatStats.percentage}%`;
}

function initializeCharts() {
    // Revenue Chart
    const revenueCtx = document.getElementById('revenueChart');
    if (revenueCtx) {
        charts.revenue = new Chart(revenueCtx, {
            type: 'bar',
            data: {
                labels: adminData.revenue.quarters,
                datasets: [{
                    label: 'Revenue ($)',
                    data: adminData.revenue.amounts,
                    backgroundColor: 'rgba(26, 77, 46, 0.8)',
                    borderColor: 'rgba(26, 77, 46, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }

    // Referral Chart
    const referralCtx = document.getElementById('referralChart');
    if (referralCtx) {
        charts.referral = new Chart(referralCtx, {
            type: 'pie',
            data: {
                labels: adminData.referrals.sources,
                datasets: [{
                    data: adminData.referrals.percentages,
                    backgroundColor: [
                        'rgba(26, 77, 46, 0.8)',
                        'rgba(201, 163, 58, 0.8)',
                        'rgba(84, 106, 95, 0.8)',
                        'rgba(15, 62, 40, 0.8)',
                        'rgba(47, 79, 79, 0.8)'
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }
    
    // Repeat Chart
    const repeatCtx = document.getElementById('repeatChart');
    if (repeatCtx) {
        charts.repeat = new Chart(repeatCtx, {
            type: 'doughnut',
            data: {
                labels: ['Repeat Students', 'One-time Students'],
                datasets: [{
                    data: [adminData.repeatStats.repeatStudents, adminData.repeatStats.totalStudents - adminData.repeatStats.repeatStudents],
                    backgroundColor: ['rgba(26, 77, 46, 0.8)', 'rgba(201, 163, 58, 0.8)']
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    // Instrument Revenue Chart
    const instrumentRevenueCtx = document.getElementById('instrumentRevenueChart');
    if (instrumentRevenueCtx) {
        charts.instrumentRevenue = new Chart(instrumentRevenueCtx, {
            type: 'pie',
            data: {
                labels: adminData.revenueDistribution.byInstrument.map(item => item.instrument),
                datasets: [{
                    data: adminData.revenueDistribution.byInstrument.map(item => item.revenue),
                    backgroundColor: [
                        'rgba(26, 77, 46, 0.8)',
                        'rgba(201, 163, 58, 0.8)',
                        'rgba(84, 106, 95, 0.8)',
                        'rgba(15, 62, 40, 0.8)',
                        'rgba(47, 79, 79, 0.8)',
                        'rgba(34, 139, 34, 0.8)'
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    // Student Revenue Chart
    const studentRevenueCtx = document.getElementById('studentRevenueChart');
    if (studentRevenueCtx) {
        charts.studentRevenue = new Chart(studentRevenueCtx, {
            type: 'bar',
            data: {
                labels: adminData.revenueDistribution.byStudent.map(item => item.student),
                datasets: [{
                    label: 'Revenue ($)',
                    data: adminData.revenueDistribution.byStudent.map(item => item.revenue),
                    backgroundColor: 'rgba(201, 163, 58, 0.8)',
                    borderColor: 'rgba(201, 163, 58, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }
}

function loadTables() {
    loadReferralTable();
    loadInstrumentsTable();
    loadLessonsTable();
    loadUsersTable();
    loadRepeatStats();
}

function loadReferralTable() {
    const container = document.getElementById('referralTable');
    if (!container) return;

    let html = '<table class="table table-striped"><thead><tr><th>Source</th><th>Percentage</th></tr></thead><tbody>';
    adminData.referrals.sources.forEach((source, index) => {
        html += `<tr><td>${source}</td><td>${adminData.referrals.percentages[index]}%</td></tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

function loadInstrumentsTable() {
    const container = document.getElementById('instrumentsTable');
    if (!container) return;

    let html = '<table class="table table-striped"><thead><tr><th>Rank</th><th>Instrument</th><th>Lessons Booked</th></tr></thead><tbody>';
    adminData.instruments.forEach((instrument, index) => {
        html += `<tr><td>${index + 1}</td><td>${instrument.name}</td><td>${instrument.lessons}</td></tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

function loadLessonsTable() {
    const container = document.getElementById('lessonsTable');
    if (!container) return;

    let html = '<table class="table table-striped"><thead><tr><th>Student</th><th>Teacher</th><th>Instrument</th><th>Date</th><th>Time</th><th>Status</th></tr></thead><tbody>';
    adminData.lessons.forEach(lesson => {
        const statusBadge = lesson.status === 'completed' ? 'badge bg-success' : 'badge bg-warning';
        html += `<tr>
            <td>${lesson.student}</td>
            <td>${lesson.teacher}</td>
            <td>${lesson.instrument}</td>
            <td>${lesson.date}</td>
            <td>${lesson.time}</td>
            <td><span class="${statusBadge}">${lesson.status}</span></td>
        </tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

function loadUsersTable() {
    const container = document.getElementById('usersTable');
    if (!container) return;

    let html = '<table class="table table-striped"><thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Join Date</th></tr></thead><tbody>';
    adminData.users.forEach(user => {
        const roleBadge = user.role === 'student' ? 'badge bg-primary' : 'badge bg-success';
        html += `<tr>
            <td>${user.name}</td>
            <td>${user.email}</td>
            <td><span class="${roleBadge}">${user.role}</span></td>
            <td>${user.joinDate}</td>
        </tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

function loadRepeatStats() {
    const container = document.getElementById('repeatStats');
    if (!container) return;

    const html = `
        <div class="row text-center">
            <div class="col-6">
                <h4 class="text-primary">${adminData.repeatStats.totalStudents}</h4>
                <p class="text-muted">Total Students</p>
            </div>
            <div class="col-6">
                <h4 class="text-success">${adminData.repeatStats.repeatStudents}</h4>
                <p class="text-muted">Repeat Students</p>
            </div>
        </div>
        <div class="mt-3">
            <div class="progress">
                <div class="progress-bar bg-success" style="width: ${adminData.repeatStats.percentage}%"></div>
            </div>
            <small class="text-muted">${adminData.repeatStats.percentage}% repeat rate</small>
        </div>
    `;
    container.innerHTML = html;
}

function setupEventListeners() {
    // Tab change listeners
    const tabs = document.querySelectorAll('#adminTabs button[data-bs-toggle="tab"]');
    tabs.forEach(tab => {
        tab.addEventListener('shown.bs.tab', function(event) {
            const target = event.target.getAttribute('data-bs-target');
            if (target === '#lessons') {
                showLessonsView('table');
            }
        });
    });
}

// Global functions for onclick handlers
window.showLessonsView = function(view) {
    const tableView = document.getElementById('lessonsTable');
    const calendarView = document.getElementById('lessonsCalendar');
    const buttons = document.querySelectorAll('[onclick*="showLessonsView"]');
    
    buttons.forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');
    
    if (view === 'table') {
        tableView.classList.remove('d-none');
        calendarView.classList.add('d-none');
    } else {
        tableView.classList.add('d-none');
        calendarView.classList.remove('d-none');
        loadLessonsCalendar();
    }
};

window.searchLessons = function() {
    const searchTerm = document.getElementById('lessonsSearch').value.toLowerCase();
    const rows = document.querySelectorAll('#lessonsTable tbody tr');
    
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(searchTerm) ? '' : 'none';
    });
};

window.filterUsers = function() {
    const filter = document.getElementById('userRoleFilter').value;
    const rows = document.querySelectorAll('#usersTable tbody tr');
    
    rows.forEach(row => {
        if (!filter) {
            row.style.display = '';
        } else {
            const role = row.querySelector('.badge').textContent;
            row.style.display = role === filter ? '' : 'none';
        }
    });
};

window.searchUsers = function() {
    const searchTerm = document.getElementById('usersSearch').value.toLowerCase();
    const rows = document.querySelectorAll('#usersTable tbody tr');
    
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(searchTerm) ? '' : 'none';
    });
};

function loadLessonsCalendar() {
    const container = document.getElementById('lessonsCalendar');
    if (!container) return;

    const html = `
        <div class="calendar-view">
            <div class="row">
                <div class="col-12">
                    <h5>Calendar View</h5>
                    <div class="calendar-grid">
                        ${adminData.lessons.map(lesson => `
                            <div class="calendar-event">
                                <strong>${lesson.student}</strong><br>
                                ${lesson.instrument} with ${lesson.teacher}<br>
                                <small>${lesson.date} at ${lesson.time}</small>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        </div>
    `;
    container.innerHTML = html;
}

// ===========================================
// TEACHER DASHBOARD FUNCTIONALITY
// ===========================================

const teacherData = {
    profile: null,
    pricing: { type: 'flat', flat: 40, custom: 55 },
    availability: [],
    lessons: [
        { student: 'Ava Lee', instrument: 'Piano', date: '2024-02-10', time: '10:00 AM', type: 'virtual' },
        { student: 'Noah Kim', instrument: 'Guitar', date: '2024-02-12', time: '2:00 PM', type: 'in-person' }
    ]
};

function loadTeacherDashboard() {
    renderTeacherProfileSummary();
    renderEarningsSummary();
    renderAvailability();
    renderTeacherLessons();

    // Toggle pricing groups
    document.getElementById('flatRate').addEventListener('change', togglePricingGroups);
    document.getElementById('customRate').addEventListener('change', togglePricingGroups);
}

function saveTeacherProfile() {
    const name = document.getElementById('tName').value.trim();
    const instrument = document.getElementById('tInstrument').value.trim();
    const bio = document.getElementById('tBio').value.trim();
    const email = document.getElementById('tEmail').value.trim();

    if (!name || !instrument || !bio || !email) {
        alert('Please fill in all required fields.');
        return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        alert('Please enter a valid email.');
        return;
    }

    teacherData.profile = { name, instrument, bio, email };
    renderTeacherProfileSummary();
}

function renderTeacherProfileSummary() {
    const container = document.getElementById('tProfileSummary');
    if (!container) return;
    if (!teacherData.profile) {
        container.innerHTML = '<p class="text-muted mb-0">Your saved profile will appear here.</p>';
        return;
    }
    const p = teacherData.profile;
    container.innerHTML = `
        <div class="d-flex align-items-start gap-3">
            <div class="rounded-circle bg-light border" style="width:64px;height:64px;display:grid;place-items:center;">${p.name.charAt(0)}</div>
            <div>
                <h5 class="mb-1">${p.name}</h5>
                <div class="text-muted">${p.instrument}</div>
                <p class="mt-2 mb-2">${p.bio}</p>
                <a href="mailto:${p.email}">${p.email}</a>
            </div>
            </div>
    `;
}

function togglePricingGroups() {
    const isFlat = document.getElementById('flatRate').checked;
    document.getElementById('flatRateGroup').classList.toggle('d-none', !isFlat);
    document.getElementById('customRateGroup').classList.toggle('d-none', isFlat);
}

function saveTeacherRates() {
    const isFlat = document.getElementById('flatRate').checked;
    const flat = parseFloat(document.getElementById('flatRateInput').value || teacherData.pricing.flat);
    const custom = parseFloat(document.getElementById('customRateInput').value || teacherData.pricing.custom);
    teacherData.pricing = { type: isFlat ? 'flat' : 'custom', flat, custom };
    renderEarningsSummary();
}

function renderEarningsSummary() {
    const container = document.getElementById('tEarningsSummary');
    if (!container) return;
    const sampleLessons = 12;
    const rate = teacherData.pricing.type === 'flat' ? teacherData.pricing.flat : teacherData.pricing.custom;
    const total = sampleLessons * rate;
    container.innerHTML = `
        <div class="row text-center">
            <div class="col-6">
                <h4 class="text-primary">$${rate}</h4>
                <p class="text-muted">Rate / Lesson</p>
            </div>
            <div class="col-6">
                <h4 class="text-success">$${total}</h4>
                <p class="text-muted">Earnings (12 lessons)</p>
            </div>
        </div>
    `;
}

function addAvailability() {
    const date = document.getElementById('availDate').value;
    const time = document.getElementById('availTime').value;
    const duration = parseInt(document.getElementById('availDuration').value, 10);
    if (!date || !time || !duration) {
        alert('Please select date, time, and duration.');
        return;
    }
    const key = `${date} ${time}`;
    if (teacherData.availability.some(a => a.key === key)) {
        alert('This slot is already added.');
        return;
    }
    teacherData.availability.push({ key, date, time, duration });
    renderAvailability();
}

function renderAvailability() {
    const container = document.getElementById('tAvailabilityList');
    if (!container) return;
    if (teacherData.availability.length === 0) {
        container.innerHTML = '<p class="text-muted mb-0">No availability added yet.</p>';
        return;
    }
    let html = '<ul class="list-group">';
    teacherData.availability.forEach(slot => {
        html += `<li class="list-group-item d-flex justify-content-between align-items-center">
            <span>${slot.date} at ${slot.time} · ${slot.duration} min</span>
            <button class="btn btn-sm btn-outline-danger" onclick="removeAvailability('${slot.key}')">Remove</button>
        </li>`;
    });
    html += '</ul>';
    container.innerHTML = html;
}

window.removeAvailability = function(key) {
    teacherData.availability = teacherData.availability.filter(s => s.key !== key);
    renderAvailability();
};

function renderTeacherLessons() {
    const container = document.getElementById('tLessonsTable');
    if (!container) return;
    const dateFilter = document.getElementById('tFilterDate');
    const instFilter = document.getElementById('tFilterInstrument');
    let lessons = [...teacherData.lessons];
    if (dateFilter && dateFilter.value) {
        lessons = lessons.filter(l => l.date === dateFilter.value);
    }
    if (instFilter && instFilter.value) {
        const term = instFilter.value.toLowerCase();
        lessons = lessons.filter(l => l.instrument.toLowerCase().includes(term));
    }
    if (lessons.length === 0) {
        container.innerHTML = '<p class="text-muted mb-0">No lessons found.</p>';
        return;
    }
    let html = '<table class="table table-striped"><thead><tr><th>Student</th><th>Instrument</th><th>Date</th><th>Time</th><th>Type</th></tr></thead><tbody>';
    lessons.forEach(l => {
        html += `<tr><td>${l.student}</td><td>${l.instrument}</td><td>${l.date}</td><td>${l.time}</td><td>${l.type}</td></tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

// Expose teacher handlers
window.saveTeacherProfile = saveTeacherProfile;
window.saveTeacherRates = saveTeacherRates;
window.addAvailability = addAvailability;

// ===========================================
// SIMPLE AUTH (Mock)
// ===========================================

// Mock data removed - using API-based authentication

window.performSignup = async function() {
    const firstName = document.getElementById('suFirst').value.trim();
    const lastName = document.getElementById('suLast').value.trim();
    const email = document.getElementById('suEmail').value.trim();
    const password = document.getElementById('suPassword').value;
    const password2 = document.getElementById('suPassword2').value;
    const accountType = document.querySelector('input[name="suRole"]:checked').value;
    
    // Clear previous messages
    hideModalMessages('signup');
    
    // Validation
    if (!firstName || !lastName || !email || !password || !password2) { 
        showModalError('signup', 'Please fill in all required fields.');
        return; 
    }
    
    if (!validateEmail(email)) {
        showModalError('signup', 'Please enter a valid email address.');
        return;
    }
    
    if (!validatePassword(password)) {
        showModalError('signup', 'Password must be at least 6 characters long.');
        return;
    }
    
    if (password !== password2) { 
        showModalError('signup', 'Passwords do not match.');
        return; 
    }
    
    // Set loading state
    setButtonLoading('performSignup()', true);
    
    try {
        const result = await performSignupAPI(firstName, lastName, email, password, accountType);
        
        if (result.success) {
            showBottomCornerNotification('Account created successfully! Please sign in.', 'success');
            setTimeout(() => {
                bootstrap.Modal.getInstance(document.getElementById('signupModal')).hide();
                // Clear form
                document.getElementById('signupForm').reset();
                hideModalMessages('signup');
                // Switch to login modal
                switchToLogin();
            }, 1500);
        } else {
            // Show specific error message from API
            const errorMessage = result.error || 'Failed to create account. Please try again.';
            showModalError('signup', errorMessage);
        }
    } catch (error) {
        console.error('Signup error:', error);
        showModalError('signup', 'Unable to connect to server. Please check your internet connection and try again.');
    } finally {
        setButtonLoading('performSignup()', false);
    }
};

window.performLogin = async function() {
    const email = document.getElementById('authLoginEmail').value.trim();
    const password = document.getElementById('authLoginPassword').value;
    
    // Clear previous messages
    hideModalMessages('login');
    
    // Validation
    if (!email || !password) {
        showModalError('login', 'Please enter both email and password.');
        return;
    }
    
    if (!validateEmail(email)) {
        showModalError('login', 'Please enter a valid email address.');
        return;
    }
    
    // Set loading state
    setButtonLoading('performLogin()', true);
    
    try {
        const result = await performLoginAPI(email, password);
        
        if (result.success) {
            sessionStorage.setItem('userEmail', result.userEmail);
            sessionStorage.setItem('userEmail', email);
            sessionStorage.setItem('userName', `${result.user.firstName} ${result.user.lastName}`);
            sessionStorage.setItem('accountType', result.user.accountType);
            
            showBottomCornerNotification(`Welcome back, ${result.user.firstName}!`, 'success');
            
            setTimeout(() => {
                bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
                // Clear form
                document.getElementById('loginForm').reset();
                hideModalMessages('login');
                // Update UI
                updateAuthUI();
            }, 1500);
        } else {
            // Show specific error message from API
            const errorMessage = result.error || 'Invalid credentials. Please try again.';
            showModalError('login', errorMessage);
        }
    } catch (error) {
        console.error('Login error:', error);
        showModalError('login', 'Unable to connect to server. Please check your internet connection and try again.');
    } finally {
        setButtonLoading('performLogin()', false);
    }
};

// Old mock authentication functions removed - using API-based authentication

// ===========================================
// STUDENT DASHBOARD FUNCTIONALITY
// ===========================================

const studentData = {
    profile: null,
    teachers: [
        { id: 1, name: 'Mike Chen', instrument: 'Guitar', availability: ['2024-02-12 10:00', '2024-02-13 14:00'] },
        { id: 2, name: 'Lisa Wang', instrument: 'Piano', availability: ['2024-02-12 11:00', '2024-02-15 16:30'] },
        { id: 3, name: 'Maria Garcia', instrument: 'Violin', availability: ['2024-02-14 09:30', '2024-02-16 15:00'] }
    ],
    bookings: []
};

function loadStudentDashboard() {
    // Load initial teacher list
    loadTeachersList();
}

async function loadTeachersList() {
    const container = document.getElementById('sTeachersGrid');
    if (!container) return;
    
    // Show loading state
    container.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div><p>Loading teachers...</p></div>';
    
    try {
        const result = await getAvailableTeachers('');
        
        if (result.success && result.teachers && result.teachers.length > 0) {
            container.innerHTML = result.teachers.map(teacher => `
                <div class="card mb-3">
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-8">
                                <h6 class="card-title">
                                    <i class="fas fa-user-tie me-2 text-primary"></i>${teacher.name}
                                </h6>
                                <p class="text-muted mb-1">
                                    <i class="fas fa-music me-2"></i>${teacher.instrument}
                                </p>
                                <p class="text-muted mb-1">
                                    <i class="fas fa-info-circle me-2"></i>${teacher.bio}
                                </p>
                                <p class="text-muted mb-0">
                                    <i class="fas fa-envelope me-2"></i>${teacher.email}
                                </p>
                            </div>
                            <div class="col-md-4 text-end">
                                <button class="btn btn-primary" onclick="viewTeacherSchedule(${teacher.userId}, '${teacher.name}')">
                                    <i class="fas fa-calendar me-1"></i>View Schedule
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `).join('');
        } else {
            container.innerHTML = `
                <div class="alert alert-info text-center">
                    <i class="fas fa-info-circle me-2"></i>
                    <h6>No Teachers Available</h6>
                    <p class="mb-0">No teachers have created profiles yet. Teachers need to sign up and create their profiles to appear in search results.</p>
                </div>
            `;
        }
    } catch (error) {
        console.error('Error loading teachers:', error);
        container.innerHTML = '<div class="alert alert-danger"><i class="fas fa-exclamation-triangle me-2"></i>Error loading teachers. Please try again.</div>';
    }
}

function saveStudentProfile() {
    const name = document.getElementById('sName').value.trim();
    const instrument = document.getElementById('sInstrument').value.trim();
    const email = document.getElementById('sEmail').value.trim();
    if (!name || !instrument || !email) { alert('Please fill in all fields'); return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { alert('Enter a valid email'); return; }
    studentData.profile = { name, instrument, email };
    renderStudentProfileSummary();
}

function renderStudentProfileSummary() {
    const container = document.getElementById('sProfileSummary');
    if (!container) return;
    if (!studentData.profile) {
        container.innerHTML = '<p class="text-muted mb-0">Your saved profile will appear here.</p>';
        return;
    }
    const p = studentData.profile;
    container.innerHTML = `
        <div>
            <h5 class="mb-1">${p.name}</h5>
            <div class="text-muted">${p.instrument}</div>
            <a href="mailto:${p.email}">${p.email}</a>
        </div>
    `;
}

function renderScheduleTable() {
    const container = document.getElementById('sScheduleTable');
    if (!container) return;
    let html = '<table class="table table-striped"><thead><tr><th>Teacher</th><th>Instrument</th><th>Slot</th><th>Type</th><th>Action</th></tr></thead><tbody>';
    studentData.teachers.forEach(t => {
        t.availability.forEach(slot => {
            html += `<tr>
                <td>${t.name}</td><td>${t.instrument}</td><td>${slot}</td>
                <td><span class="badge bg-secondary">${document.getElementById('lessonType')?.value || 'virtual'}</span></td>
                <td><button class="btn btn-sm btn-primary" onclick="bookSlot(${t.id}, '${slot}')">Book</button></td>
            </tr>`;
        });
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

function showStudentScheduleView(view) {
    const table = document.getElementById('sScheduleTable');
    const cal = document.getElementById('sScheduleCalendar');
    const buttons = document.querySelectorAll('[onclick*="showStudentScheduleView"]');
    buttons.forEach(b => b.classList.remove('active'));
    event.target.classList.add('active');
    if (view === 'table') { table.classList.remove('d-none'); cal.classList.add('d-none'); }
    else { table.classList.add('d-none'); cal.classList.remove('d-none'); renderScheduleCalendar(); }
}

function renderScheduleCalendar() {
    const container = document.getElementById('sScheduleCalendar');
    if (!container) return;
    // Simple month grid mock
    container.innerHTML = '<div class="text-muted">Calendar view coming soon (mock)</div>';
}

window.bookSlot = function(teacherId, slot) {
    const type = document.getElementById('lessonType')?.value || 'virtual';
    const teacher = studentData.teachers.find(t => t.id === teacherId);
    // Remove slot from availability
    teacher.availability = teacher.availability.filter(s => s !== slot);
    studentData.bookings.push({ teacherId, teacher: teacher.name, instrument: teacher.instrument, slot, type });
    renderScheduleTable();
    alert(`Booked ${teacher.name} for ${slot} (${type}).`);
}

function renderTeachersGrid() {
    const container = document.getElementById('sTeachersGrid');
    if (!container) return;
    container.innerHTML = studentData.teachers.map(t => `
        <div class="card mb-2"><div class="card-body d-flex justify-content-between align-items-center">
            <div>
                <h6 class="mb-1">${t.name}</h6>
                <div class="text-muted">${t.instrument}</div>
            </div>
            <button class="btn btn-sm btn-outline-primary" onclick="openTeacherSlots(${t.id})">View Slots</button>
        </div></div>
    `).join('');
}

window.filterTeachersByInstrument = async function() {
    const term = document.getElementById('sSearchInstrument').value.trim();
    const container = document.getElementById('sTeachersGrid');
    
    if (!container) return;
    
    // Show loading state
    container.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div><p>Searching teachers...</p></div>';
    
    try {
        const result = await getAvailableTeachers(term);
        
        if (result.success && result.teachers && result.teachers.length > 0) {
            container.innerHTML = result.teachers.map(teacher => `
                <div class="card mb-3">
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-8">
                                <h6 class="card-title">
                                    <i class="fas fa-user-tie me-2 text-primary"></i>${teacher.name}
                                </h6>
                                <p class="text-muted mb-1">
                                    <i class="fas fa-music me-2"></i>${teacher.instrument}
                                </p>
                                <p class="text-muted mb-1">
                                    <i class="fas fa-info-circle me-2"></i>${teacher.bio}
                                </p>
                                <p class="text-muted mb-0">
                                    <i class="fas fa-envelope me-2"></i>${teacher.email}
                                </p>
            </div>
                            <div class="col-md-4 text-end">
                                <button class="btn btn-primary" onclick="viewTeacherSchedule(${teacher.userId}, '${teacher.name}')">
                                    <i class="fas fa-calendar me-1"></i>View Schedule
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
    `).join('');
        } else {
            if (term) {
                container.innerHTML = `
                    <div class="alert alert-info text-center">
                        <i class="fas fa-search me-2"></i>
                        <h6>No Teachers Found</h6>
                        <p class="mb-0">No teachers found for "${term}". Try searching for a different instrument or check back later.</p>
                    </div>
                `;
            } else {
                container.innerHTML = `
                    <div class="alert alert-info text-center">
                        <i class="fas fa-info-circle me-2"></i>
                        <h6>No Teachers Available</h6>
                        <p class="mb-0">No teachers have created profiles yet. Teachers need to sign up and create their profiles to appear in search results.</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error searching teachers:', error);
        container.innerHTML = '<div class="alert alert-danger"><i class="fas fa-exclamation-triangle me-2"></i>Error loading teachers. Please try again.</div>';
    }
}

window.openTeacherSlots = function(teacherId) {
    const teacher = studentData.teachers.find(t => t.id === teacherId);
    alert(`${teacher.name} slots: \n${teacher.availability.join('\n')}`);
}

window.saveStudentProfile = saveStudentProfile;
window.showStudentScheduleView = showStudentScheduleView;
window.verifyStudentCard = function(){ alert('Card verification mock'); };

// =============================
// Lesson Room interactions
// =============================
let activeLesson = null;

window.openLessonRoom = function(lesson) {
    activeLesson = lesson;
    document.getElementById('lessonRoomInfo').textContent = `${lesson.teacher} • ${lesson.instrument} • ${lesson.slot}`;
    document.getElementById('externalLink').href = 'https://meet.google.com';
    showPage('lesson-room');
};

window.sendLessonMessage = function() {
    const input = document.getElementById('messageInput');
    const txt = input.value.trim();
    if (!txt) return;
    const thread = document.getElementById('messageThread');
    const bubble = document.createElement('div');
    bubble.className = 'p-2 mb-1 rounded border';
    bubble.textContent = txt;
    thread.appendChild(bubble);
    thread.scrollTop = thread.scrollHeight;
    input.value = '';
};

window.uploadSheetMusic = function(e) {
    const file = e.target.files && e.target.files[0];
    if (!file) return;
    const viewer = document.getElementById('sheetViewer');
    if (file.type === 'application/pdf') {
        viewer.innerHTML = `<p>PDF uploaded: ${file.name}</p>`;
    } else if (file.type.startsWith('image/')) {
        const url = URL.createObjectURL(file);
        viewer.innerHTML = `<img src="${url}" alt="Sheet" style="max-width:100%">`;
    } else {
        viewer.innerHTML = '<p class="text-muted">Unsupported file type.</p>';
    }
};

window.downloadIcs = function() {
    if (!activeLesson) { alert('No active lesson'); return; }
    const dt = activeLesson.slot.replace(' ', 'T');
    const dtEnd = dt; // simple mock
    const ics = `BEGIN:VCALENDAR\nVERSION:2.0\nBEGIN:VEVENT\nDTSTART:${dt}\nDTEND:${dtEnd}\nSUMMARY:Music Lesson with ${activeLesson.teacher}\nLOCATION:Online\nEND:VEVENT\nEND:VCALENDAR`;
    const blob = new Blob([ics], { type: 'text/calendar' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'lesson.ics'; a.click();
    URL.revokeObjectURL(url);
};

window.saveEndLesson = function() {
    const summary = document.getElementById('endSummary').value.trim();
    const rating = document.getElementById('endRating').value;
    console.log('Lesson saved', { summary, rating, activeLesson });
    bootstrap.Modal.getInstance(document.getElementById('endLessonModal')).hide();
    alert('Lesson summary saved. Great job!');
};

// Test function to verify button visibility
function testButtonVisibility() {
    const loginBtn = document.getElementById('loginBtn');
    const signupBtn = document.getElementById('signupBtn');
    const loginSignupButtons = document.getElementById('loginSignupButtons');
    
    console.log('Button visibility test:', {
        loginBtn: loginBtn,
        signupBtn: signupBtn,
        loginSignupButtons: loginSignupButtons,
        hasDNone: loginSignupButtons ? loginSignupButtons.classList.contains('d-none') : 'N/A',
        computedStyle: loginSignupButtons ? window.getComputedStyle(loginSignupButtons).display : 'N/A'
    });
}

// Make test function available globally
window.testButtonVisibility = testButtonVisibility;
