window.Custom2FA = (function () {
  const TOKEN_KEY = "custom2fa.accessToken";
  const MFA_KEY = "custom2fa.mfaTicket";
  const PROVIDERS_KEY = "custom2fa.providers";

  function getToken() { return localStorage.getItem(TOKEN_KEY) || ""; }
  function setToken(v) {
    if (v) localStorage.setItem(TOKEN_KEY, v);
    else localStorage.removeItem(TOKEN_KEY);
  }
  function getMfaTicket() { return sessionStorage.getItem(MFA_KEY) || ""; }
  function setMfaTicket(v) {
    if (v) sessionStorage.setItem(MFA_KEY, v);
    else sessionStorage.removeItem(MFA_KEY);
  }
  function getProviders() {
    try { return JSON.parse(sessionStorage.getItem(PROVIDERS_KEY) || "[]"); }
    catch { return []; }
  }
  function setProviders(list) {
    sessionStorage.setItem(PROVIDERS_KEY, JSON.stringify(list || []));
  }

  function renderNav(active) {
    const el = document.getElementById("topnav");
    if (!el) return;
    const authed = !!getToken();
    const links = [
      ["index.html", "首页"],
      ["register.html", "注册"],
      ["login.html", "登录"],
      ["mfa.html", "二次认证"],
      ["manage.html", "管理 2FA"]
    ];
    el.innerHTML = links.map(([href, text]) => {
      const on = active === href ? ' style="color:var(--accent);font-weight:700"' : "";
      return `<a href="${href}"${on}>${text}</a>`;
    }).join("") +
      `<span>${authed ? "已登录(有 access_token)" : "未登录"}</span>` +
      (authed ? `<a href="#" id="logoutLink">退出</a>` : "");
    const logout = document.getElementById("logoutLink");
    if (logout) {
      logout.addEventListener("click", (e) => {
        e.preventDefault();
        setToken("");
        setMfaTicket("");
        setProviders([]);
        location.href = "index.html";
      });
    }
  }

  async function api(path, { method = "GET", body, auth = false } = {}) {
    const headers = { "Content-Type": "application/json" };
    if (auth) {
      const token = getToken();
      if (!token) throw new Error("缺少 access_token，请先登录");
      headers.Authorization = "Bearer " + token;
    }
    const res = await fetch(path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    let data = null;
    const text = await res.text();
    try { data = text ? JSON.parse(text) : null; } catch { data = { raw: text }; }
    return { ok: res.ok, status: res.status, data };
  }

  function showOut(el, payload, ok = true) {
    if (!el) return;
    el.className = "out " + (ok ? "ok" : "err");
    el.textContent = typeof payload === "string" ? payload : JSON.stringify(payload, null, 2);
  }

  function applyAuthResult(data) {
    if (data?.accessToken) {
      setToken(data.accessToken);
      setMfaTicket("");
      setProviders([]);
    }
    if (data?.requiresTwoFactor && data?.mfaTicket) {
      setMfaTicket(data.mfaTicket);
      setProviders(data.providers || []);
      setToken("");
    }
  }

  return {
    getToken, setToken, getMfaTicket, setMfaTicket, getProviders, setProviders,
    renderNav, api, showOut, applyAuthResult
  };
})();
