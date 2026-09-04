const SESSION_KEY = "customerPortal.session";

function loadSession() {
  try {
    return JSON.parse(sessionStorage.getItem(SESSION_KEY) || "null");
  } catch {
    return null;
  }
}

function saveSession(session) {
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  renderSession();
}

function clearSession() {
  sessionStorage.removeItem(SESSION_KEY);
  renderSession();
}

function renderSession() {
  const session = loadSession();
  const status = document.getElementById("session-status");
  const logoutBtn = document.getElementById("logout-btn");
  if (session) {
    status.textContent = `Вход выполнен: ${session.customer.email} (id=${session.customer.id}, роль=${session.customer.role})`;
    logoutBtn.hidden = false;
    const profileId = document.querySelector('#profile-form input[name="id"]');
    if (profileId && !profileId.value) profileId.value = session.customer.id;
  } else {
    status.textContent = "Не выполнен вход.";
    logoutBtn.hidden = true;
  }
}

function showResult(el, ok, data) {
  el.hidden = false;
  el.classList.toggle("ok", ok);
  el.classList.toggle("err", !ok);
  el.textContent = typeof data === "string" ? data : JSON.stringify(data, null, 2);
}

async function callApi(path, options = {}) {
  const session = loadSession();
  const headers = { ...(options.headers || {}) };
  if (session?.accessToken) {
    headers["Authorization"] = `${session.tokenType || "Bearer"} ${session.accessToken}`;
  }
  const response = await fetch(path, { ...options, headers });
  const text = await response.text();
  const body = text ? JSON.parse(text) : null;
  return { ok: response.ok, status: response.status, body };
}

document.getElementById("register-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.target;
  const resultEl = document.getElementById("register-result");
  const { ok, status, body } = await callApi("/api/v1/customers", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email: form.email.value,
      password: form.password.value,
    }),
  });
  showResult(resultEl, ok, ok ? body : { status, ...body });
  if (ok) form.reset();
});

document.getElementById("login-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.target;
  const resultEl = document.getElementById("login-result");
  const { ok, status, body } = await callApi("/api/v1/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email: form.email.value,
      password: form.password.value,
    }),
  });
  showResult(resultEl, ok, ok ? { customer: body.customer } : { status, ...body });
  if (ok) {
    saveSession(body);
    form.reset();
  }
});

document.getElementById("profile-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.target;
  const resultEl = document.getElementById("profile-result");
  const id = form.id.value;
  const { ok, status, body } = await callApi(`/api/v1/customers/${id}`, { method: "GET" });
  showResult(resultEl, ok, ok ? body : { status, ...body });
});

document.getElementById("logout-btn").addEventListener("click", clearSession);

renderSession();
