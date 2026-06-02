let BASE_URL = 'http://localhost:8080';
let dormsCache = [];
let studentsCache = [];

// ── CORE API
async function api(path, method = 'GET', body = null) {
    const opts = { method, headers: { 'Content-Type': 'application/json' } };
    if (body) opts.body = JSON.stringify(body);
    const r = await fetch(BASE_URL + path, opts);
    if (!r.ok) throw new Error(`HTTP ${r.status}: ${r.statusText}`);
    if (r.status === 204) return null;
    return r.json();
}

// ── URL CONFIG
function updateBaseUrl() {
    BASE_URL = document.getElementById('baseUrlInput').value.replace(/\/$/, '');
    document.getElementById('api-url-display').textContent = BASE_URL.replace(/https?:\/\//, '');
}

async function testConnection() {
    try {
        await api('/api/Dorms');
        toast('Підключення успішне ✅');
    } catch (e) {
        toast('Не вдалось підключитись: ' + e.message, 'error');
    }
}

// ── NAVIGATION
function navigate(page) {
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    document.querySelectorAll('#sidebar .nav-link').forEach(n => n.classList.remove('active'));

    document.getElementById('page-' + page).classList.add('active');
    const btn = document.querySelector(`[data-nav="${page}"]`);
    if (btn) btn.classList.add('active');

    const titles = {
        dashboard: 'Огляд системи',
        dorms: 'Гуртожитки',
        rooms: 'Кімнати',
        students: 'Студенти',
        checkins: 'Заселення',
        payments: 'Платежі',
        api: 'API Тестування'
    };
    document.getElementById('topbar-title').textContent = titles[page] || page;

    const addPages = ['dorms', 'rooms', 'students', 'checkins', 'payments'];
    const addBtn = document.getElementById('topbar-action');
    if (addPages.includes(page)) {
        addBtn.classList.remove('d-none');
        const modalName = page === 'checkins' ? 'checkin' : page.slice(0, -1);
        addBtn.onclick = () => openModal(modalName);
    } else {
        addBtn.classList.add('d-none');
    }

    if (page === 'dashboard') loadDashboard();
    if (page === 'dorms') loadDorms();
    if (page === 'rooms') loadRoomsPage();
    if (page === 'students') loadStudentsPage();
    if (page === 'checkins') loadCheckIns();
    if (page === 'payments') loadPaymentsPage();
}

// ── MODALS
function openModal(name) {
    const el = document.getElementById('modal-' + name);
    if (el) {
        el.classList.add('open');
        if (name === 'room') populateDormSelects();
    }
}
function closeModal(name) {
    const el = document.getElementById('modal-' + name);
    if (el) el.classList.remove('open');
}
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.modal-overlay').forEach(m => {
        m.addEventListener('click', e => { if (e.target === m) m.classList.remove('open'); });
    });

    updateBaseUrl();
    loadDashboard();
    document.getElementById('ci-checkin').value = new Date().toISOString().slice(0, 10);
    document.getElementById('p-date').value = new Date().toISOString().slice(0, 10);
});

// ── TOAST
function toast(msg, type = 'success') {
    const c = document.getElementById('toast-container');
    const t = document.createElement('div');
    t.className = `toast-custom ${type}`;
    t.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span> ${msg}`;
    c.appendChild(t);
    setTimeout(() => t.remove(), 3500);
}

// ── UTILITIES
function formatDate(d) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('uk-UA', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function initials(name) {
    if (!name) return '?';
    return name.trim().split(/\s+/).slice(0, 2).map(w => w[0]).join('').toUpperCase();
}

function badgeClass(status) {
    if (!status) return 'secondary';
    const s = status.toLowerCase();
    if (s.includes('сплачено')) return 'success';
    if (s.includes('очікується')) return 'warning';
    if (s.includes('прострочено')) return 'danger';
    return 'secondary';
}

function filterTable(tableId, query) {
    const q = query.toLowerCase();
    document.querySelectorAll('#' + tableId + ' tbody tr').forEach(tr => {
        tr.style.display = tr.textContent.toLowerCase().includes(q) ? '' : 'none';
    });
}

function loader(cols) {
    return `<tr><td colspan="${cols}" class="text-center py-4 text-muted"><span class="spinner-tiny"></span>Завантаження…</td></tr>`;
}

// ── DASHBOARD
async function loadDashboard() {
    try {
        const [dorms, rooms, students, checkins, payments] = await Promise.allSettled([
            api('/api/Dorms'), api('/api/Rooms'), api('/api/Students'),
            api('/api/CheckIns'), api('/api/Payments')
        ]);
        const d = r => (r.status === 'fulfilled' ? r.value || [] : []);
        const DD = d(dorms), RR = d(rooms), SS = d(students), CC = d(checkins), PP = d(payments);

        document.getElementById('stat-dorms').textContent = DD.length;
        document.getElementById('stat-rooms').textContent = RR.length;
        document.getElementById('stat-students').textContent = SS.length;
        document.getElementById('stat-checkins').textContent = CC.filter(c => !c.checkOutDate).length;

        const sb = document.getElementById('dash-students');
        sb.innerHTML = SS.length
            ? SS.slice(-5).reverse().map(s => `
                <div class="d-flex align-items-center gap-2 px-3 py-2 border-bottom">
                  <div class="bg-primary bg-opacity-10 text-primary rounded-2 d-flex align-items-center justify-content-center fw-bold"
                       style="width:32px;height:32px;font-size:0.7rem;flex-shrink:0">${initials(s.fullName)}</div>
                  <div>
                    <div class="fw-semibold small">${s.fullName}</div>
                    <div class="text-muted" style="font-size:0.75rem">${s.faculty || '—'}</div>
                  </div>
                </div>`).join('')
            : '<div class="text-center text-muted py-3 small">Немає студентів</div>';

        const db = document.getElementById('dash-dorms');
        db.innerHTML = DD.length
            ? DD.map(d => `
                <div class="d-flex align-items-center gap-2 px-3 py-2 border-bottom">
                  <span class="fs-5">🏢</span>
                  <div>
                    <div class="fw-semibold small">${d.name}</div>
                    <div class="text-muted" style="font-size:0.75rem">${d.address}</div>
                  </div>
                </div>`).join('')
            : '<div class="text-center text-muted py-3 small">Немає гуртожитків</div>';

        const pb = document.getElementById('dash-payments');
        pb.innerHTML = PP.length
            ? PP.slice(-5).reverse().map(p => `
                <div class="d-flex align-items-center gap-2 px-3 py-2 border-bottom">
                  <span class="badge bg-${badgeClass(p.status)}">${p.status}</span>
                  <span class="ms-auto small fw-semibold">${parseFloat(p.amount).toFixed(2)} ₴</span>
                </div>`).join('')
            : '<div class="text-center text-muted py-3 small">Немає платежів</div>';

        const cb = document.getElementById('dash-checkins');
        const active = CC.filter(c => !c.checkOutDate);
        cb.innerHTML = active.length
            ? active.slice(-5).map(c => `
                <div class="d-flex align-items-center gap-2 px-3 py-2 border-bottom">
                  <span class="badge bg-success">Активне</span>
                  <span class="small ms-2">Студент #${c.studentId} — Кімн. #${c.roomId}</span>
                </div>`).join('')
            : '<div class="text-center text-muted py-3 small">Активних заселень немає</div>';

    } catch (e) { toast('Помилка dashboard: ' + e.message, 'error'); }
}

// ── DORMS
async function loadDorms() {
    document.getElementById('dorms-body').innerHTML = loader(5);
    try {
        dormsCache = await api('/api/Dorms') || [];
        renderDorms(dormsCache);
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

function renderDorms(data) {
    const tbody = document.getElementById('dorms-body');
    if (!data.length) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">Гуртожитків поки немає</td></tr>`;
        return;
    }
    tbody.innerHTML = data.map(d => `
        <tr>
          <td><span class="badge bg-secondary">#${d.id}</span></td>
          <td class="fw-semibold">${d.name}</td>
          <td>${d.address}</td>
          <td>${d.floors}</td>
          <td>${d.manager}</td>
        </tr>`).join('');
}

async function saveDorm() {
    const body = {
        name: document.getElementById('dorm-name').value,
        address: document.getElementById('dorm-address').value,
        floors: parseInt(document.getElementById('dorm-floors').value),
        manager: document.getElementById('dorm-manager').value
    };
    if (!body.name || !body.address) { toast('Заповніть всі поля', 'error'); return; }
    try {
        await api('/api/Dorms', 'POST', body);
        closeModal('dorm'); loadDorms(); toast('Гуртожиток додано ✨');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

// ── ROOMS
async function loadRoomsPage() {
    await populateDormSelects();
    await loadRooms();
}

async function loadRooms() {
    const dormId = document.getElementById('rooms-dorm-filter').value;
    document.getElementById('rooms-body').innerHTML = loader(7);
    try {
        const all = await api('/api/Rooms') || [];
        const data = dormId ? all.filter(r => r.dormId == dormId) : all;
        renderRooms(data);
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function loadAvailableRooms() {
    const dormId = document.getElementById('rooms-dorm-filter').value;
    if (!dormId) { toast('Оберіть гуртожиток для перегляду вільних кімнат', 'error'); return; }
    try {
        const data = await api(`/api/Rooms/dorm/${dormId}/available`) || [];
        renderRooms(data);
        toast(`Знайдено ${data.length} вільних кімнат`);
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

function renderRooms(data) {
    const tbody = document.getElementById('rooms-body');
    if (!data.length) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">Кімнат не знайдено</td></tr>`;
        return;
    }
    tbody.innerHTML = data.map(r => {
        const dorm = dormsCache.find(d => d.id === r.dormId);
        return `<tr>
          <td><span class="badge bg-secondary">#${r.id}</span></td>
          <td class="fw-semibold">№${r.roomNumber}</td>
          <td>${r.floor}</td>
          <td>${r.roomType}</td>
          <td>${r.capacity}</td>
          <td>${dorm ? dorm.name : '#' + r.dormId}</td>
          <td><span class="avail-yes">● Доступна</span></td>
        </tr>`;
    }).join('');
}

async function populateDormSelects() {
    try {
        if (!dormsCache.length) dormsCache = await api('/api/Dorms') || [];
        const opts = dormsCache.map(d => `<option value="${d.id}">${d.name}</option>`).join('');
        const rf = document.getElementById('rooms-dorm-filter');
        const rd = document.getElementById('room-dorm-id');
        const sf = document.getElementById('students-dorm-filter');
        if (rf) rf.innerHTML = '<option value="">Всі гуртожитки</option>' + opts;
        if (rd) rd.innerHTML = opts;
        if (sf) sf.innerHTML = '<option value="">Всі студенти</option>' + opts;
    } catch (e) { /* silent */ }
}

async function saveRoom() {
    const body = {
        roomNumber: document.getElementById('room-number').value,
        floor: parseInt(document.getElementById('room-floor').value),
        capacity: parseInt(document.getElementById('room-capacity').value),
        roomType: document.getElementById('room-type').value,
        dormId: parseInt(document.getElementById('room-dorm-id').value)
    };
    try {
        await api('/api/Rooms', 'POST', body);
        closeModal('room'); loadRooms(); toast('Кімнату додано ✨');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

// ── STUDENTS 
async function loadStudentsPage() {
    await populateDormSelects();
    await loadStudents();
}

async function loadStudents() {
    const dormId = document.getElementById('students-dorm-filter')?.value;
    document.getElementById('students-body').innerHTML = loader(7);
    try {
        const url = dormId ? `/api/Students?dormId=${dormId}` : '/api/Students';
        studentsCache = await api(url) || [];
        renderStudents(studentsCache);

        const sel = document.getElementById('payment-student-filter');
        if (sel) {
            const all = await api('/api/Students') || [];
            sel.innerHTML = '<option value="">Оберіть студента для суми</option>'
                + all.map(s => `<option value="${s.id}">${s.fullName}</option>`).join('');
        }
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

function renderStudents(data) {
    const tbody = document.getElementById('students-body');
    if (!data.length) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">Студентів не знайдено</td></tr>`;
        return;
    }
    tbody.innerHTML = data.map(s => `
        <tr>
          <td><span class="badge bg-secondary">#${s.id}</span></td>
          <td class="fw-semibold">
            <div class="d-flex align-items-center gap-2">
              <div class="bg-primary bg-opacity-10 text-primary rounded-2 d-flex align-items-center justify-content-center fw-bold"
                   style="width:28px;height:28px;font-size:0.7rem;flex-shrink:0">${initials(s.fullName)}</div>
              ${s.fullName}
            </div>
          </td>
          <td><code>${s.ticketNumber}</code></td>
          <td>${s.faculty}</td>
          <td>${formatDate(s.dateOfBirth)}</td>
          <td>${s.contactInfo}</td>
          <td>
            <div class="d-flex gap-1">
              <button class="btn btn-sm btn-outline-secondary" onclick="editStudent(${s.id})">✏️</button>
              <button class="btn btn-sm btn-outline-danger"    onclick="deleteStudent(${s.id})">🗑</button>
            </div>
          </td>
        </tr>`).join('');
}

async function saveStudent() {
    const body = {
        fullName: document.getElementById('s-fullname').value,
        ticketNumber: document.getElementById('s-ticket').value,
        dateOfBirth: document.getElementById('s-dob').value,
        faculty: document.getElementById('s-faculty').value,
        contactInfo: document.getElementById('s-contact').value
    };
    if (!body.fullName) { toast('Введіть ПІБ', 'error'); return; }
    try {
        await api('/api/Students', 'POST', body);
        closeModal('student'); loadStudents(); toast('Студента додано ✨');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function editStudent(id) {
    try {
        const s = await api('/api/Students/' + id);
        document.getElementById('edit-s-id').value = s.id;
        document.getElementById('edit-s-fullname').value = s.fullName;
        document.getElementById('edit-s-ticket').value = s.ticketNumber;
        document.getElementById('edit-s-dob').value = s.dateOfBirth?.slice(0, 10);
        document.getElementById('edit-s-faculty').value = s.faculty;
        document.getElementById('edit-s-contact').value = s.contactInfo;
        openModal('edit-student');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function updateStudent() {
    const id = document.getElementById('edit-s-id').value;
    const body = {
        id: parseInt(id),
        fullName: document.getElementById('edit-s-fullname').value,
        ticketNumber: document.getElementById('edit-s-ticket').value,
        dateOfBirth: document.getElementById('edit-s-dob').value,
        faculty: document.getElementById('edit-s-faculty').value,
        contactInfo: document.getElementById('edit-s-contact').value
    };
    try {
        await api('/api/Students/' + id, 'PUT', body);
        closeModal('edit-student'); loadStudents(); toast('Дані оновлено ✅');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function deleteStudent(id) {
    if (!confirm('Видалити студента #' + id + '?')) return;
    try {
        await api('/api/Students/' + id, 'DELETE');
        loadStudents(); toast('Студента видалено');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

// ── CHECKINS
async function loadCheckIns() {
    document.getElementById('checkins-body').innerHTML = loader(7);
    try {
        const data = await api('/api/CheckIns') || [];
        if (!data.length) {
            document.getElementById('checkins-body').innerHTML =
                `<tr><td colspan="7" class="text-center text-muted py-4">Заселень немає</td></tr>`;
            return;
        }
        document.getElementById('checkins-body').innerHTML = data.map(c => {
            const active = !c.checkOutDate;
            return `<tr>
              <td><span class="badge bg-secondary">#${c.id}</span></td>
              <td>#${c.studentId}</td>
              <td>#${c.roomId}</td>
              <td>${formatDate(c.checkInDate)}</td>
              <td>${c.checkOutDate ? formatDate(c.checkOutDate) : '—'}</td>
              <td><code>${c.contractNumber}</code></td>
              <td><span class="badge bg-${active ? 'success' : 'secondary'}">${active ? 'Активне' : 'Завершено'}</span></td>
            </tr>`;
        }).join('');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function saveCheckIn() {
    const checkout = document.getElementById('ci-checkout').value;
    const body = {
        studentId: parseInt(document.getElementById('ci-student').value),
        roomId: parseInt(document.getElementById('ci-room').value),
        checkInDate: document.getElementById('ci-checkin').value,
        checkOutDate: checkout || null,
        contractNumber: document.getElementById('ci-contract').value
    };
    if (!body.studentId || !body.roomId || !body.checkInDate) {
        toast('Заповніть обовʼязкові поля', 'error'); return;
    }
    try {
        await api('/api/CheckIns', 'POST', body);
        closeModal('checkin'); loadCheckIns(); toast('Заселення оформлено ✨');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

// ── PAYMENTS
async function loadPaymentsPage() {
    loadStudents();
    await loadPayments();
}

async function loadPayments() {
    document.getElementById('payments-body').innerHTML = loader(5);
    try {
        const data = await api('/api/Payments') || [];
        if (!data.length) {
            document.getElementById('payments-body').innerHTML =
                `<tr><td colspan="5" class="text-center text-muted py-4">Платежів немає</td></tr>`;
            return;
        }
        document.getElementById('payments-body').innerHTML = data.map(p => `
            <tr>
              <td><span class="badge bg-secondary">#${p.id}</span></td>
              <td>#${p.studentId}</td>
              <td class="fw-semibold">${parseFloat(p.amount).toFixed(2)} ₴</td>
              <td>${formatDate(p.paymentDate)}</td>
              <td><span class="badge bg-${badgeClass(p.status)}">${p.status}</span></td>
            </tr>`).join('');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function loadTotalForStudent() {
    const id = document.getElementById('payment-student-filter').value;
    const block = document.getElementById('total-payment-block');
    if (!id) { block.style.display = 'none'; return; }
    try {
        const total = await api(`/api/Payments/student/${id}/total`);
        document.getElementById('total-payment-val').textContent = parseFloat(total).toFixed(2) + ' ₴';
        block.style.display = 'flex';
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

async function savePayment() {
    const body = {
        studentId: parseInt(document.getElementById('p-student').value),
        amount: parseFloat(document.getElementById('p-amount').value),
        paymentDate: document.getElementById('p-date').value,
        status: document.getElementById('p-status').value
    };
    if (!body.studentId || !body.amount) { toast('Заповніть всі поля', 'error'); return; }
    try {
        await api('/api/Payments', 'POST', body);
        closeModal('payment'); loadPayments(); toast('Платіж зареєстровано ✨');
    } catch (e) { toast('Помилка: ' + e.message, 'error'); }
}

// ── API TESTER
async function sendTestRequest() {
    const method = document.getElementById('test-method').value;
    const url = document.getElementById('test-url').value;
    const bodyRaw = document.getElementById('test-body').value;
    const pre = document.getElementById('api-response');
    pre.textContent = '⏳ Надсилаю запит…';
    try {
        const opts = { method, headers: { 'Content-Type': 'application/json' } };
        if (bodyRaw.trim() && (method === 'POST' || method === 'PUT')) opts.body = bodyRaw;
        const r = await fetch(BASE_URL + url, opts);
        const text = await r.text();
        let parsed;
        try { parsed = JSON.parse(text); } catch { parsed = text; }
        pre.textContent = `HTTP ${r.status} ${r.statusText}\n\n` + JSON.stringify(parsed, null, 2);
        toast(`${method} ${url} — ${r.status}`);
    } catch (e) {
        pre.textContent = '❌ Помилка: ' + e.message;
        toast('Запит не вдався: ' + e.message, 'error');
    }
}