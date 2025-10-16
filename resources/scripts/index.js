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

const users = [];
let currentUser = null;

window.performSignup = function() {
    const first = document.getElementById('suFirst').value.trim();
    const last = document.getElementById('suLast').value.trim();
    const email = document.getElementById('suEmail').value.trim();
    const password = document.getElementById('suPassword').value;
    const password2 = document.getElementById('suPassword2').value;
    const role = document.getElementById('suRole').value;
    if (!first || !last || !email || !password || !password2) { alert('Please fill all fields'); return; }
    if (password !== password2) { alert('Passwords do not match'); return; }
    if (users.some(u => u.email === email)) { alert('Email already registered'); return; }
    const user = { id: Date.now(), first, last, email, password, role };
    users.push(user);
    currentUser = user;
    bootstrap.Modal.getInstance(document.getElementById('signupModal')).hide();
    updateAuthHeader();
};

window.performLogin = function() {
    const email = document.getElementById('authLoginEmail').value.trim();
    const password = document.getElementById('authLoginPassword').value;
    const user = users.find(u => u.email === email && u.password === password);
    if (!user) { alert('Invalid credentials'); return; }
    currentUser = user;
    bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
    updateAuthHeader();
};

function updateAuthHeader() {
    const actions = document.querySelector('.auth-actions');
    if (!actions) return;
    actions.innerHTML = currentUser ? `
        <span class="me-2">Hi, ${currentUser.first}</span>
        <button class="btn btn-outline btn-sm" onclick="logoutUser()">Logout</button>
    ` : `
        <button class="btn btn-outline btn-sm" data-bs-toggle="modal" data-bs-target="#loginModal">Log In</button>
        <button class="btn btn-primary btn-sm" data-bs-toggle="modal" data-bs-target="#signupModal">Sign Up</button>
    `;
}

window.logoutUser = function() {
    currentUser = null;
    updateAuthHeader();
};

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
    renderStudentProfileSummary();
    renderScheduleTable();
    renderTeachersGrid();
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

window.filterTeachersByInstrument = function() {
    const term = (document.getElementById('sSearchInstrument').value || '').toLowerCase();
    const container = document.getElementById('sTeachersGrid');
    const filtered = studentData.teachers.filter(t => t.instrument.toLowerCase().includes(term));
    container.innerHTML = filtered.map(t => `
        <div class="card mb-2"><div class="card-body d-flex justify-content-between align-items-center">
            <div>
                <h6 class="mb-1">${t.name}</h6>
                <div class="text-muted">${t.instrument}</div>
            </div>
            <button class="btn btn-sm btn-outline-primary" onclick="openTeacherSlots(${t.id})">View Slots</button>
        </div></div>
    `).join('');
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
