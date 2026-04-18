document.addEventListener("DOMContentLoaded", () => {
    initSidebarToggle();
    initCollapseMenus();
    initGlobalSearch();
});

function initSidebarToggle() {
    const btn = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");

    if (!btn || !sidebar) return;

    btn.addEventListener("click", () => {
        sidebar.classList.toggle("open");
    });
}

function initCollapseMenus() {
    const buttons = document.querySelectorAll("[data-collapse-target]");

    buttons.forEach(btn => {
        btn.addEventListener("click", () => {
            const targetId = btn.getAttribute("data-collapse-target");
            const target = document.getElementById(targetId);
            if (!target) return;

            target.classList.toggle("open");
        });
    });
}

function debounce(fn, delay = 350) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => fn.apply(this, args), delay);
    };
}

const _statusLabel = { Draft: "Taslak", Ready: "Hazır", Conflict: "Çakışma", Deleted: "Silindi" };
const _statusCls   = { Draft: "gs-badge--draft", Ready: "gs-badge--ready", Conflict: "gs-badge--conflict" };

function initGlobalSearch() {
    const input = document.getElementById("globalSearchInput");
    const resultPanel = document.getElementById("globalSearchResult");
    if (!input || !resultPanel) return;

    let _abortCtrl = null;

    const handleSearch = debounce(async () => {
        const value = input.value.trim();
        if (!value || value.length < 2) {
            resultPanel.classList.add("d-none");
            resultPanel.innerHTML = "";
            return;
        }

        if (_abortCtrl) _abortCtrl.abort();
        _abortCtrl = new AbortController();

        resultPanel.classList.remove("d-none");
        resultPanel.innerHTML = `<div class="gs-loading">Aranıyor...</div>`;

        try {
            const res = await fetch(`/Scripts/QuickSearch?q=${encodeURIComponent(value)}`, { signal: _abortCtrl.signal });
            const data = await res.json();

            if (!data.results || data.results.length === 0) {
                resultPanel.innerHTML = `<div class="gs-empty">Sonuç bulunamadı</div>`;
                return;
            }

            const rows = data.results.map(r => {
                const badge = `<span class="gs-badge ${_statusCls[r.status] || ''}">${_statusLabel[r.status] || r.status}</span>`;
                const sub = [r.developer, r.batch].filter(Boolean).join(" · ");
                return `<a class="gs-row" href="/Scripts/Detail/${r.id}">
                    <div class="gs-row-main">
                        <span class="gs-name">${escHtml(r.name)}</span>
                        ${badge}
                    </div>
                    ${sub ? `<span class="gs-sub">${escHtml(sub)}</span>` : ''}
                </a>`;
            }).join("");

            resultPanel.innerHTML = rows;
        } catch (e) {
            if (e.name !== "AbortError") {
                resultPanel.innerHTML = `<div class="gs-empty">Arama başarısız</div>`;
            }
        }
    }, 350);

    input.addEventListener("input", handleSearch);

    document.addEventListener("click", e => {
        if (!input.contains(e.target) && !resultPanel.contains(e.target)) {
            resultPanel.classList.add("d-none");
        }
    });

    input.addEventListener("focus", () => {
        if (resultPanel.innerHTML && !resultPanel.classList.contains("d-none")) {
            // keep open
        }
    });
}

/** DB'deki conflict key'ini okunabilir etikete çevirir. Örn: "DDL:USERS" → "Tablo: USERS" */
function conflictLabel(key) {
    if (!key) return key || "";
    const parts = key.split(":");
    if (parts.length < 2) return key;
    const code = parts[0].toUpperCase();
    const obj  = parts[1];
    const sub  = parts.length > 2 ? parts[2] : null;
    switch (code) {
        case "RECORD": return sub ? `Kayıt: ${obj} = ${sub}` : `Kayıt: ${obj}`;
        case "DDL":    return `Tablo: ${obj}`;
        case "OBJ":    return `Nesne: ${obj}`;
        case "DML":    return `DML: ${obj}`;
        default:       return key;
    }
}

function escHtml(str) {
    return String(str).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;");
}

function setupTableFilter(inputId, rowSelector) {
    const input = document.getElementById(inputId);
    if (!input) return;

    // initialize all rows as matching
    document.querySelectorAll(rowSelector).forEach(r => { r.dataset.filterHidden = "0"; });

    const filterFn = debounce(() => {
        const value = input.value.trim().toLowerCase();
        const rows = document.querySelectorAll(rowSelector);
        rows.forEach(row => {
            const matches = !value || row.innerText.toLowerCase().includes(value);
            row.dataset.filterHidden = matches ? "0" : "1";
        });
        // notify pagination if registered, otherwise fall back to direct display
        const firstRow = document.querySelector(rowSelector);
        const tbody = firstRow?.closest("tbody");
        if (tbody?.id && window.__pgState?.[tbody.id]) {
            window.__pgState[tbody.id].refresh(true);
        } else {
            rows.forEach(r => {
                r.style.display = r.dataset.filterHidden === "1" ? "none" : "";
            });
        }
    }, 300);

    input.addEventListener("input", filterFn);
}

/* ================================================
   PAGINATION
   ================================================ */
window.__pgState = window.__pgState || {};

function setupPagination(tbodyId, pageSize) {
    pageSize = pageSize || 15;
    const tbody = document.getElementById(tbodyId);
    if (!tbody) return;

    const tableWrap = tbody.closest(".table-wrap");
    let bar = document.getElementById("pgbar_" + tbodyId);
    if (!bar) {
        bar = document.createElement("div");
        bar.id = "pgbar_" + tbodyId;
        bar.className = "pagination-bar";
        if (tableWrap) tableWrap.insertAdjacentElement("afterend", bar);
        else tbody.closest("table")?.insertAdjacentElement("afterend", bar);
    }

    const state = { currentPage: 1 };

    function render() {
        const allRows = Array.from(tbody.querySelectorAll("tr"));
        const pageableRows = allRows.filter(r => r.dataset.filterHidden !== "1");
        const total = pageableRows.length;
        const pages = Math.max(1, Math.ceil(total / pageSize));
        if (state.currentPage > pages) state.currentPage = pages;
        if (state.currentPage < 1) state.currentPage = 1;

        const start = (state.currentPage - 1) * pageSize;
        const end = start + pageSize;

        allRows.forEach(r => { r.style.display = r.dataset.filterHidden === "1" ? "none" : "none"; });
        pageableRows.forEach((r, i) => { r.style.display = (i >= start && i < end) ? "" : "none"; });

        if (total === 0) { bar.innerHTML = ""; return; }
        if (pages <= 1) {
            bar.innerHTML = `<div class="pg-info">${total} kayıt</div>`;
            return;
        }

        const s = start + 1, e = Math.min(end, total);
        let html = `<div class="pg-info">${s}–${e} / ${total} kayıt</div><div class="pg-controls">`;
        html += `<button class="pg-btn" ${state.currentPage === 1 ? "disabled" : ""} onclick="__pgGoto('${tbodyId}',${state.currentPage - 1})">‹</button>`;
        for (let p = 1; p <= pages; p++) {
            if (pages > 7 && p !== 1 && p !== pages && Math.abs(p - state.currentPage) > 2) {
                if (p === state.currentPage - 3 || p === state.currentPage + 3)
                    html += `<span class="pg-ellipsis">…</span>`;
                continue;
            }
            html += `<button class="pg-btn${p === state.currentPage ? " pg-btn--active" : ""}" onclick="__pgGoto('${tbodyId}',${p})">${p}</button>`;
        }
        html += `<button class="pg-btn" ${state.currentPage === pages ? "disabled" : ""} onclick="__pgGoto('${tbodyId}',${state.currentPage + 1})">›</button></div>`;
        bar.innerHTML = html;
    }

    state.render = render;
    state.refresh = function (resetToFirst) {
        if (resetToFirst) state.currentPage = 1;
        render();
    };

    window.__pgState[tbodyId] = state;
    render();
}

function __pgGoto(tbodyId, page) {
    const s = window.__pgState?.[tbodyId];
    if (!s) return;
    s.currentPage = page;
    s.render();
}

/** Release → batch → script ağacında metin araması */
function setupScriptTreeFilter(inputId, rootSelector) {
    const input = document.getElementById(inputId);
    const root = document.querySelector(rootSelector);
    if (!input || !root) return;

    const clearHidden = () => {
        root.querySelectorAll(".sm-tree-leaf, .sm-tree-batch, .sm-tree-release, .sm-tree-orphan-root").forEach((el) => {
            el.classList.remove("d-none");
        });
    };

    const filterFn = debounce(() => {
        const q = input.value.trim().toLowerCase();
        if (!q) {
            clearHidden();
            return;
        }

        root.querySelectorAll(".sm-tree-leaf").forEach((leaf) => {
            const hay = (leaf.getAttribute("data-sm-search") || "").toLowerCase();
            leaf.classList.toggle("d-none", !hay.includes(q));
        });

        root.querySelectorAll(".sm-tree-batch").forEach((batchNode) => {
            const visible = Array.from(batchNode.querySelectorAll(".sm-tree-leaf")).some(
                (l) => !l.classList.contains("d-none")
            );
            batchNode.classList.toggle("d-none", !visible);
        });

        root.querySelectorAll(".sm-tree-release").forEach((relNode) => {
            const visible = Array.from(relNode.querySelectorAll(".sm-tree-batch")).some(
                (b) => !b.classList.contains("d-none")
            );
            relNode.classList.toggle("d-none", !visible);
        });

        const orphan = root.querySelector(".sm-tree-orphan-root");
        if (orphan) {
            const visible = Array.from(orphan.querySelectorAll(".sm-tree-batch")).some(
                (b) => !b.classList.contains("d-none")
            );
            orphan.classList.toggle("d-none", !visible);
        }
    }, 200);

    input.addEventListener("input", filterFn);
}

/** Dropdown boş kaldığında veya geliştirici kendini seçmek zorunda olduğunda gövdedeki oturum id’si. */
function effectiveUserIdFromForm(selectValueRaw) {
    const n = selectValueRaw ? Number(selectValueRaw) : 0;
    if (n > 0) return n;
    const idAttr = document.body?.dataset?.currentUserId;
    const fall = idAttr ? Number(idAttr) : 0;
    return fall > 0 ? fall : 0;
}

function showToast(message, type = "success") {
    const container = document.getElementById("toastContainer");
    if (!container) return;

    const id = "toast_" + Date.now();
    const bgClass = type === "error"
        ? "text-bg-danger"
        : type === "info"
            ? "text-bg-primary"
            : "text-bg-success";

    const html = `
        <div id="${id}" class="toast align-items-center ${bgClass} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML("beforeend", html);

    const toastElement = document.getElementById(id);
    const toast = new bootstrap.Toast(toastElement, { delay: 2500 });
    toast.show();

    toastElement.addEventListener("hidden.bs.toast", () => toastElement.remove());
}

function openGlobalModal(title, bodyHtml) {
    const titleEl = document.getElementById("globalModalTitle");
    const bodyEl = document.getElementById("globalModalBody");
    const modalEl = document.getElementById("globalAppModal");

    if (!titleEl || !bodyEl || !modalEl) return;

    titleEl.textContent = title;
    bodyEl.innerHTML = bodyHtml;

    const modal = new bootstrap.Modal(modalEl);
    modal.show();
}

function escapeHtml(s) {
    if (s == null) return "";
    return String(s)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

async function openConflictReviewModal(conflictId) {
    const pairBase = window.__conflictsUrls?.pair;
    if (!pairBase) {
        showToast("Sayfa yapılandırması eksik.", "error");
        return;
    }
    const res = await fetch(pairBase + encodeURIComponent(conflictId), {
        headers: { Accept: "application/json" }
    });
    let d = null;
    try {
        d = await res.json();
    } catch {
        /* ignore */
    }
    if (!res.ok || !d || d.scriptA == null || d.scriptB == null) {
        showToast(d?.message || "Çakışma yüklenemedi.", "error");
        return;
    }
    const a = d.scriptA;
    const b = d.scriptB;
    const title = `İncele — ${conflictLabel(d.tableName) || "çakışma"}`;

    const col = (side, cls) => {
        const ro = !side.canEdit;
        const roAttr = ro ? "readonly" : "";
        const badge = ro ? "" : `<span id="${cls}-val-badge" class="sw-sql-badge sw-sql-badge--idle ms-2">— kontrol bekleniyor</span>`;
        const valBox = ro ? "" : `<div id="${cls}-val-box" class="alert d-none small mb-0 mt-1 py-2" role="status"></div>`;
        return `<div class="col-lg-6">
            <h6 class="mb-1">${escapeHtml(side.name || "")}</h6>
            <div class="small text-muted mb-2">${escapeHtml(side.developer || "")}${ro ? " · salt okunur" : ""}</div>
            <label class="form-label small d-flex align-items-center">SQL${badge}</label>
            <textarea class="form-control font-monospace mb-1 ${cls}-sql" rows="10" ${roAttr}></textarea>
            ${valBox}
            <label class="form-label small mt-2">Rollback</label>
            <textarea class="form-control font-monospace ${cls}-rb" rows="5" ${roAttr}></textarea>
        </div>`;
    };

    const html = `
      <div class="cr-wrap" data-conflict-id="${Number(d.conflictId)}" data-a-id="${Number(a.id)}" data-b-id="${Number(b.id)}">
        <div class="mb-2"><span class="badge text-bg-warning text-dark">${escapeHtml(conflictLabel(d.tableName))}</span></div>
        <div class="row g-3">
          ${col(a, "cra")}
          ${col(b, "crb")}
        </div>
        <div class="d-flex flex-wrap justify-content-end gap-2 mt-3">
          <button type="button" class="btn btn-light" data-bs-dismiss="modal">Kapat</button>
          <button type="button" class="btn btn-outline-primary" onclick="submitConflictReview(false)">Kaydet</button>
          <button type="button" class="btn btn-primary" onclick="submitConflictReview(true)">Kaydet ve çakışmayı kapat</button>
        </div>
      </div>`;

    openGlobalModal(title, html);

    const w = document.querySelector(".cr-wrap");
    if (w) {
        const asql = w.querySelector(".cra-sql");
        const arb  = w.querySelector(".cra-rb");
        const bsql = w.querySelector(".crb-sql");
        const brb  = w.querySelector(".crb-rb");
        if (asql) asql.value = a.sqlScript ?? "";
        if (arb)  arb.value  = a.rollbackScript ?? "";
        if (bsql) bsql.value = b.sqlScript ?? "";
        if (brb)  brb.value  = b.rollbackScript ?? "";
        initConflictReviewValidation(w, a, b);
    }
}

async function submitConflictReview(markResolved) {
    const w = document.querySelector(".cr-wrap");
    const url = window.__conflictsUrls?.saveReview;
    if (!w || !url) {
        showToast("Modül adresi eksik.", "error");
        return;
    }
    const cid = Number(w.dataset.conflictId);
    const aid = Number(w.dataset.aId);
    const bid = Number(w.dataset.bId);
    const asql = w.querySelector(".cra-sql");
    const arb = w.querySelector(".cra-rb");
    const bsql = w.querySelector(".crb-sql");
    const brb = w.querySelector(".crb-rb");

    const updates = [
        { scriptId: aid, sqlScript: asql?.value ?? "", rollbackScript: arb?.value ?? "" },
        { scriptId: bid, sqlScript: bsql?.value ?? "", rollbackScript: brb?.value ?? "" }
    ];

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ conflictId: cid, updates, markResolved })
    });
    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }
    if (!res.ok || data?.success === false) {
        showToast(data?.message || (res.status === 403 ? "Bu scripti düzenleme yetkiniz yok." : "İşlem başarısız."), "error");
        return;
    }
    showToast(data?.message || "Tamam.", "success");
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();

    if (markResolved || data?.autoResolved) {
        const resolvedCid = data?.conflictId ?? cid;
        const tr = document.querySelector(`tr[data-conflict-id="${resolvedCid}"]`);
        if (tr) {
            tr.remove();
            window.__pgState?.["conflictTableBody"]?.refresh();
            const h3 = document.querySelector(".panel-header h3");
            if (h3) {
                const remaining = document.querySelectorAll("#conflictTableBody tr").length;
                h3.textContent = `Açık çakışmalar (${remaining})`;
            }
        } else {
            window.location.reload();
        }
    }
}

function scriptWizardRenderDeveloperOptions(devs) {
    if (!devs || !devs.length) return `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;
    return devs
        .map((d) => `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`)
        .join("");
}

function treeChildrenBaseUrl() {
    return (
        window.__releaseDetailUrls?.treeChildren ||
        window.__scriptsPageUrls?.treeChildren ||
        window.__scriptCreateUrls?.treeChildren ||
        window.__batchesPageUrls?.treeChildren ||
        window.__releaseCreateUrls?.treeChildren
    );
}

async function fetchTreeChildrenList(releaseId, parentBatchId) {
    const base = treeChildrenBaseUrl();
    if (!base) return { children: [] };
    const rid = releaseId != null && releaseId > 0 ? Number(releaseId) : "";
    const pid = parentBatchId != null && parentBatchId > 0 ? Number(parentBatchId) : 0;
    const url = `${base}?releaseId=${rid}&parentBatchId=${pid}`;
    try {
        const r = await fetch(url, { headers: { Accept: "application/json" } });
        if (!r.ok) return { children: [] };
        const d = await r.json();
        return { children: d.children || [] };
    } catch {
        return { children: [] };
    }
}

async function fetchBatchPath(batchId, releaseId) {
    const base =
        window.__scriptCreateUrls?.batchPath ||
        window.__scriptsPageUrls?.batchPath ||
        window.__releaseDetailUrls?.batchPath ||
        window.__batchesPageUrls?.batchPath;
    if (!base || !batchId) return { path: [] };
    const rid = releaseId != null && Number(releaseId) > 0 ? `&releaseId=${Number(releaseId)}` : "";
    const url = `${base}?batchId=${Number(batchId)}${rid}`;
    try {
        const r = await fetch(url, { headers: { Accept: "application/json" } });
        if (!r.ok) return { path: [] };
        const d = await r.json();
        return { path: d.path || [] };
    } catch {
        return { path: [] };
    }
}

function toggleBatchTreeBranch(btn) {
    const item = btn.closest(".batch-tree-item");
    if (!item) return;
    const kids = item.querySelector(":scope > .d-flex > .flex-grow-1 > ul.batch-tree-children");
    if (!kids) return;
    const hidden = kids.classList.toggle("d-none");
    btn.textContent = hidden ? "+" : "−";
    btn.setAttribute("aria-expanded", hidden ? "false" : "true");
}

/** Yeni vtree (Versiyonlar sayfası) toggle */
function vtreeToggle(btn) {
    const item = btn.closest(".vtree-item");
    if (!item) return;
    const children = item.querySelector(":scope > .vtree-children");
    if (!children) return;
    const isOpen = children.classList.toggle("vtree-children--hidden");
    btn.classList.toggle("vtree-toggle--open", !isOpen);
    btn.setAttribute("aria-expanded", isOpen ? "false" : "true");
}

/** Klasör seçim modalı: alt klasör ekle / script ekle */
function openFolderActionModal(batchId, batchName, canAddChild, canAddScript, linkedReleaseId) {
    const rid = linkedReleaseId != null ? Number(linkedReleaseId) : 0;
    const cards = [];

    if (canAddChild) {
        cards.push(`
            <button type="button" class="folder-action-card"
                    onclick="closeFolderActionAndRun(() => openPoolChildBatchModal(${batchId}, ${rid}))">
                <span class="folder-action-icon">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                        <line x1="12" y1="11" x2="12" y2="17"/><line x1="9" y1="14" x2="15" y2="14"/>
                    </svg>
                </span>
                <span class="folder-action-body">
                    <span class="folder-action-title">Alt Klasör Ekle</span>
                    <span class="folder-action-sub">Bu klasörün altına yeni bir klasör oluştur</span>
                </span>
            </button>`);
    }

    if (canAddScript) {
        const scriptPreset = rid > 0
            ? `{ poolBatchId: ${batchId}, releaseId: ${rid} }`
            : `{ poolBatchId: ${batchId} }`;
        cards.push(`
            <button type="button" class="folder-action-card"
                    onclick="closeFolderActionAndRun(() => openScriptCreateWizard(${scriptPreset}))">
                <span class="folder-action-icon folder-action-icon--script">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                        <polyline points="14 2 14 8 20 8"/>
                        <line x1="12" y1="11" x2="12" y2="17"/><line x1="9" y1="14" x2="15" y2="14"/>
                    </svg>
                </span>
                <span class="folder-action-body">
                    <span class="folder-action-title">Script Ekle</span>
                    <span class="folder-action-sub">Bu klasöre yeni bir script ekle</span>
                </span>
            </button>`);
    }

    if (!cards.length) {
        if (rid > 0) {
            const base = window.__batchesPageUrls?.releaseDetailBase ?? "/Releases/Detail/";
            const detailUrl = base + rid;
            const content = `
                <div class="folder-action-list">
                    <a class="folder-action-card text-decoration-none" href="${detailUrl}">
                        <span class="folder-action-icon">
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
                            </svg>
                        </span>
                        <span class="folder-action-body">
                            <span class="folder-action-title">Sürüm Detayına Git</span>
                            <span class="folder-action-sub">Bu klasör bir sürüme bağlı. Düzenlemek için sürüm sayfasını kullanın.</span>
                        </span>
                    </a>
                </div>`;
            openGlobalModal(escapeHtml(batchName), content);
        }
        return;
    }

    const content = `<div class="folder-action-list">${cards.join("")}</div>`;
    openGlobalModal(escapeHtml(batchName), content);
}

function closeFolderActionAndRun(fn) {
    const modalEl = document.getElementById("globalAppModal");
    const m = modalEl ? bootstrap.Modal.getInstance(modalEl) : null;
    if (m) { m.hide(); setTimeout(fn, 280); } else fn();
}

function swPickerNodeHtml(c) {
    const id = Number(c.batchId);
    const nm = escapeHtml(c.name || "");
    const isLocked = !!c.isLocked;
    const canSelect = !!c.canAddScript;
    const hasChildren = c.hasChildren === true;

    const toggleBtn = hasChildren
        ? `<button type="button" class="sw-toggle" onclick="swPickerToggleNode(this)" aria-expanded="false">
               <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor"><path d="M8 5l8 7-8 7"/></svg>
           </button>`
        : `<span class="sw-toggle-leaf"></span>`;

    const lockBadge = isLocked ? `<span class="sw-lock-badge">Kilitli</span>` : "";
    const check = canSelect
        ? `<input class="sw-node-check" type="checkbox" data-id="${id}" onchange="swPickerSelectNode(this)" />`
        : `<span class="sw-no-select"></span>`;

    return `
        <li class="sw-node" data-id="${id}" data-has-children="${hasChildren ? "1" : "0"}" data-loaded="0">
            <div class="sw-row ${canSelect ? "sw-row--selectable" : "sw-row--disabled"}">
                ${toggleBtn}
                <svg class="sw-folder-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                ${check}
                <span class="sw-folder-name">${nm}</span>
                ${lockBadge}
            </div>
            <ul class="sw-children list-unstyled d-none"></ul>
        </li>`;
}

async function swPickerLoadChildren(node) {
    if (!node) return;
    if (node.dataset.loaded === "1") return;
    const id = Number(node.dataset.id || 0);
    const rid = window.__scriptWizardReleaseId;
    const childrenWrap = node.querySelector(".sw-children");
    if (!childrenWrap) return;
    const { children } = await fetchTreeChildrenList(rid, id);
    if (!children || !children.length) {
        childrenWrap.innerHTML = `<li class="sw-row sw-row--disabled" style="font-size:12px;padding-left:8px">Alt klasör yok.</li>`;
    } else {
        childrenWrap.innerHTML = children.map(swPickerNodeHtml).join("");
    }
    node.dataset.loaded = "1";
}

async function swPickerToggleNode(btn) {
    const node = btn?.closest(".sw-node");
    if (!node) return;
    const hasChildren = node.dataset.hasChildren === "1";
    if (!hasChildren) return;
    await swPickerLoadChildren(node);
    const childrenWrap = node.querySelector(".sw-children");
    if (!childrenWrap) return;
    const hidden = childrenWrap.classList.toggle("d-none");
    btn.classList.toggle("sw-toggle--open", !hidden);
    btn.setAttribute("aria-expanded", String(!hidden));
}

function swPickerPickLeaf(id) {
    const h = document.getElementById("swSelectedBatchId");
    if (h) h.value = String(id);
    document.getElementById("swLeafPickedHint")?.classList.remove("d-none");
}

function swPickerSelectNode(el) {
    const checked = !!el?.checked;
    const id = Number(el?.dataset?.id || 0);
    document.querySelectorAll(".sw-node-check").forEach((x) => {
        if (x !== el) x.checked = false;
        x.closest(".sw-row")?.classList.remove("sw-row--selected");
    });
    if (checked && id > 0) {
        el.closest(".sw-row")?.classList.add("sw-row--selected");
        swPickerPickLeaf(id);
    } else {
        document.getElementById("swSelectedBatchId").value = "";
        document.getElementById("swLeafPickedHint")?.classList.add("d-none");
    }
}

function swNewFolderButtonHtml() {
    return "";
}

async function deletePoolBatch(batchId, name) {
    const url = window.__batchesPageUrls?.deletePoolBatch;
    if (!url) { showToast("Silme adresi tanımlı değil.", "error"); return; }
    if (!confirm(`"${name}" versiyonu ve tüm içeriği (alt klasörler + scriptler) silinecek. Bu işlem geri alınamaz. Devam?`)) return;

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ batchId })
    });
    let data = null;
    try { data = await res.json(); } catch (_) {}
    if (!res.ok || data?.success === false) {
        showToast(data?.message || "Silinemedi.", "error");
        return;
    }
    showToast(data?.message || "Silindi.", "success");
    // Ağaç elemanını DOM'dan kaldır
    const li = document.querySelector(`.vtree-item [onclick*="openFolderActionModal(${batchId},"]`)?.closest(".vtree-item")
            || document.querySelector(`.vtree-delete-btn[onclick*="deletePoolBatch(${batchId},"]`)?.closest(".vtree-item");
    if (li) li.remove();
    else window.location.reload();
}

function swGoFolderTreePage() {
    const url =
        window.__batchesPageUrls?.index ||
        window.__scriptsPageUrls?.batchesIndex ||
        window.__scriptCreateUrls?.batchesIndex ||
        "/Batches";
    window.location.href = url;
}

async function ensureScriptWizardHasDefaultFolder() {
    const addUrl =
        window.__batchesPageUrls?.addPoolFolder ||
        window.__scriptsPageUrls?.addPoolFolder ||
        window.__scriptCreateUrls?.addPoolFolder;
    if (!addUrl) return false;
    const rid = window.__scriptWizardReleaseId;
    if (rid != null) return false;
    const first = await fetchTreeChildrenList(null, 0);
    if (first.children && first.children.length > 0) return false;

    const createdBy = effectiveUserIdFromForm("");
    if (!createdBy) return false;

    const res = await fetch(addUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
            parentBatchId: 0,
            name: "Genel Klasör",
            createdBy
        })
    });
    if (!res.ok) return false;
    return true;
}

async function swPickerApplyPreset() {
    const want = Number(window.__swPresetLeafId || 0);
    if (!(want > 0)) return;
    const host = document.getElementById("swBatchPickerHost");
    if (!host) return;
    const path = window.__swPresetPathIds || [];
    if (Array.isArray(path) && path.length > 0) {
        let container = host.querySelector(":scope > ul");
        for (let i = 0; i < path.length; i++) {
            const curId = Number(path[i]);
            if (!container) break;
            const node = Array.from(container.children).find(
                (li) => Number(li.dataset?.id || 0) === curId
            );
            if (!node) break;
            if (i < path.length - 1 && node.dataset.hasChildren === "1") {
                await swPickerLoadChildren(node);
                const ul = node.querySelector(".sw-children");
                const btn = node.querySelector("button");
                if (ul) ul.classList.remove("d-none");
                if (btn) { btn.classList.add("sw-toggle--open"); btn.setAttribute("aria-expanded", "true"); }
                container = ul;
            }
        }
    }
    const target = host.querySelector(`.sw-node-check[data-id="${want}"]`);
    if (target && !target.disabled) {
        target.checked = true;
        target.closest(".sw-row")?.classList.add("sw-row--selected");
        swPickerSelectNode(target);
        window.__swPresetLeafId = null;
    }
}

async function renderScriptWizardBatchPicker() {
    const host = document.getElementById("swBatchPickerHost");
    if (!host) return;
    const rid = window.__scriptWizardReleaseId;
    host.innerHTML = `<div class="text-center py-2"><span class="spinner-border spinner-border-sm"></span></div>`;
    const { children } = await fetchTreeChildrenList(rid, 0);
    if (!children || !children.length) {
        host.innerHTML = `<div class="sw-empty-state"><svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg><span>Henüz klasör yok. Versiyonlar sayfasından bir versiyon oluşturun.</span></div>`;
        return;
    }
    host.innerHTML = `<ul class="sw-tree-list list-unstyled mb-0">${children.map(swPickerNodeHtml).join("")}</ul>`;
    await swPickerApplyPreset();
}

window.__relSelectedSubtrees = [];

async function renderReleaseBatchPicker() {
    const host = document.getElementById("relBatchPickerHost");
    if (!host) return;
    host.innerHTML = `<div class="text-center py-2"><span class="spinner-border spinner-border-sm"></span></div>`;
    const { children } = await fetchTreeChildrenList(null, 0);
    const eligible = (children || []).filter((c) => c.canPackageRelease === true && c.isLocked !== true);

    if (!eligible.length) {
        host.innerHTML = `<p class="small text-muted mb-0">Release'e uygun üst seviye klasör yok.</p>`;
        return;
    }

    const selectedSet = new Set((window.__relSelectedSubtrees || []).map((x) => Number(x.id)));
    const rows = eligible
        .map((c) => {
            const id = Number(c.batchId);
            const checked = selectedSet.has(id) ? "checked" : "";
            const nm = escapeHtml(c.name || "");
            return `<label class="d-flex align-items-center gap-2 mb-2">
                <input type="checkbox" class="form-check-input" value="${id}" ${checked} onchange="relTogglePackage(this)" />
                <span>${nm}</span>
            </label>`;
        })
        .join("");
    host.innerHTML = rows;
}

function relTogglePackage(cb) {
    const id = Number(cb?.value || 0);
    if (!(id > 0)) return;
    window.__relSelectedSubtrees = window.__relSelectedSubtrees || [];
    if (cb.checked) {
        if (!window.__relSelectedSubtrees.some((x) => x.id === id))
            window.__relSelectedSubtrees.push({ id, label: String(id) });
    } else {
        window.__relSelectedSubtrees = window.__relSelectedSubtrees.filter((x) => x.id !== id);
    }
}

async function postReleaseTreeLock(releaseId, lock) {
    const url = window.__releaseDetailUrls?.setTreeLock;
    if (!url) {
        showToast("Kilitleme adresi tanımsız.", "error");
        return;
    }
    const msg = lock
        ? "Ağaç kilitlenecek; onaylıyor musunuz?"
        : "Düzenlemeye açılacak; onaylıyor musunuz?";
    if (!confirm(msg)) return;
    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ releaseId: Number(releaseId), lock: !!lock })
    });
    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }
    if (!res.ok || data?.success === false) {
        showToast(data?.message || "İşlem başarısız.", "error");
        return;
    }
    showToast(data?.message || "Tamam.", "success");
    if (typeof refreshReleaseDetailPanels === "function") await refreshReleaseDetailPanels();
    else window.location.reload();
}

function openPoolRootBatchModal() {
    window.__pendingPoolBatchParentId = 0;
    window.__pendingPoolBatchReleaseId = 0;
    openPoolBatchNameModalInner("Versiyon oluştur", "Versiyon adı", "Oluştur");
}

function openPoolChildBatchModal(parentId, linkedReleaseId) {
    const pid = parentId != null ? Number(parentId) : 0;
    if (!pid) return;
    window.__pendingPoolBatchParentId = pid;
    window.__pendingPoolBatchReleaseId = linkedReleaseId != null ? Number(linkedReleaseId) : 0;
    openPoolBatchNameModalInner("Alt Klasör Ekle", "Klasör adı", "Ekle");
}

function openPoolBatchNameModalInner(title, fieldLabel, btnLabel) {
    const meta = { ...(window.__scriptCreateMeta || { developers: [] }) };
    const devOpts =
        meta.developers && meta.developers.length
            ? meta.developers
                  .map(
                      (d) =>
                          `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`
                  )
                  .join("")
            : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;
    const content = `
        <form id="poolBatchCreateForm" class="row g-3">
            <div class="col-12">
                <label class="form-label">${escapeHtml(fieldLabel ?? "Ad")}</label>
                <input id="poolBatchNameInput" class="form-control" autocomplete="off" />
            </div>
            <div class="col-12">
                <label class="form-label">Oluşturan</label>
                <select id="poolBatchCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitPoolBatchCreate()">${escapeHtml(btnLabel ?? "Oluştur")}</button>
            </div>
        </form>`;
    openGlobalModal(title, content);
}

async function submitPoolBatchCreate() {
    const rid = window.__pendingPoolBatchReleaseId || 0;
    const url = rid > 0
        ? (window.__batchesPageUrls?.addReleaseFolder)
        : (window.__batchesPageUrls?.addPoolFolder);
    const name = document.getElementById("poolBatchNameInput")?.value?.trim();
    const createdByRaw = document.getElementById("poolBatchCreatedBy")?.value?.trim();
    const createdBy = effectiveUserIdFromForm(createdByRaw);
    const parentBatchId = window.__pendingPoolBatchParentId || 0;

    if (!url) {
        showToast("Klasör adresi tanımlı değil.", "error");
        return;
    }
    if (!name) {
        showToast("Klasör adı girin.", "error");
        return;
    }
    if (!createdBy) {
        showToast("Oluşturan kullanıcıyı seçin.", "error");
        return;
    }

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ parentBatchId, name, createdBy })
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }
    if (!res.ok || !data || data.success === false) {
        showToast(data?.message || "Batch oluşturulamadı.", "error");
        return;
    }
    showToast(data?.message || "Batch oluşturuldu.", "success");
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
    window.location.reload();
}

async function openScriptCreateWizard(preset) {
    preset = preset || {};
    const ctxUrl = window.__scriptsPageUrls?.createWizardContext || window.__scriptCreateUrls?.createWizardContext;
    if (!ctxUrl) {
        showToast("Sayfa yapılandırması eksik (createWizardContext).", "error");
        return;
    }
    let data = null;
    try {
        const r = await fetch(ctxUrl, { headers: { Accept: "application/json" } });
        if (r.ok) data = await r.json();
    } catch {
        data = null;
    }
    if (!data || !Array.isArray(data.developers)) {
        showToast("Sihirbaz verisi alınamadı.", "error");
        return;
    }
    window.__scriptWizardData = data;
    window.__scriptWizardReleaseId =
        preset.releaseId != null && Number(preset.releaseId) > 0 ? Number(preset.releaseId) : null;
    window.__swPresetLeafId = null;
    window.__swPresetPathIds = [];
    const want = preset.poolBatchId != null ? Number(preset.poolBatchId) : 0;
    if (want > 0) {
        const pathRes = await fetchBatchPath(want, window.__scriptWizardReleaseId);
        const path = pathRes.path || [];
        if (path.length) {
            window.__swPresetPathIds = path.map((p) => Number(p.id));
            window.__swPresetLeafId = Number(path[path.length - 1].id);
        } else {
            window.__swPresetLeafId = want;
        }
    }
    const devOpts = scriptWizardRenderDeveloperOptions(data.developers);
    const rid = window.__scriptWizardReleaseId;
    await ensureScriptWizardHasDefaultFolder();
    const targetLabel = rid
        ? "Bu sürümde klasör seçip doğrudan script ekleyebilirsin."
        : "Klasör ağacından klasör seçip doğrudan script ekleyebilirsin.";

    const content = `
        <form id="createScriptWizardForm" class="row g-3">
            <div class="col-12">
                <label class="form-label">Hedef klasör</label>
                <p class="small text-muted mb-2">${escapeHtml(targetLabel)}</p>
                <input type="hidden" id="swSelectedBatchId" value="" />
                <div id="swLeafPickedHint" class="sw-picked-hint d-none">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                    Seçilen klasöre eklenecek.
                </div>
                <div id="swBatchPickerHost" class="sw-tree-host"></div>
                <small class="text-muted d-block mt-1">Kilitli klasörler seçilemez.</small>
            </div>
            <div class="col-md-12"><label class="form-label">Script adı</label><input id="scriptName" class="form-control" autocomplete="off" /></div>
            <div class="col-md-12"><label class="form-label">Geliştirici</label><select id="createDeveloperId" class="form-select">${devOpts}</select></div>
            <div class="col-md-12">
                <label class="form-label d-flex align-items-center gap-2">
                    SQL script
                    <span id="swSqlStatusBadge" class="sw-sql-badge sw-sql-badge--idle">— kontrol bekleniyor</span>
                </label>
                <textarea id="sqlScript" class="form-control font-monospace" rows="8" placeholder="CREATE / ALTER ..."></textarea>
            </div>
            <div class="col-md-12"><label class="form-label">Rollback (isteğe bağlı)</label><textarea id="rollbackScript" class="form-control font-monospace" rows="4"></textarea></div>
            <div id="swSqlValidateBox" class="col-12 alert d-none small mb-0" role="status"></div>
            <div class="col-12 d-flex justify-content-end flex-wrap gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" id="swCreateScriptBtn" class="btn btn-primary" onclick="submitCreateScript()">Oluştur</button>
            </div>
        </form>`;

    openGlobalModal("Yeni script", content);

    initWizardAutoValidate();
    void renderScriptWizardBatchPicker();
}

async function openCreateScriptModal(batchId) {
    const id = batchId != null ? Number(batchId) : 0;
    if (!id) {
        showToast("Batch seçilemedi.", "error");
        return;
    }
    await openScriptCreateWizard({ poolBatchId: id });
}

function openAddSubfolderModal(parentBatchId) {
    const pid = parentBatchId != null ? Number(parentBatchId) : 0;
    if (!pid) return;

    const meta = { ...(window.__scriptCreateMeta || { developers: [] }) };
    const devOpts =
        meta.developers && meta.developers.length
            ? meta.developers
                  .map(
                      (d) =>
                          `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`
                  )
                  .join("")
            : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;

    const addUrl = window.__releaseDetailUrls?.addFolder;
    if (!addUrl) {
        showToast("Klasör ekleme adresi tanımlı değil.", "error");
        return;
    }

    window.__pendingSubfolderParentId = pid;

    const content = `
        <form id="addSubfolderForm" class="row g-3">
            <div class="col-12">
                <p class="small text-muted mb-0">Üst klasör #${pid} altında yeni alt klasör oluşturulur.</p>
            </div>
            <div class="col-md-12">
                <label class="form-label">Klasör adı</label>
                <input id="subfolderName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Oluşturan</label>
                <select id="subfolderCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitAddSubfolder()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Alt batch oluştur", content);
}

async function submitAddSubfolder() {
    const url = window.__releaseDetailUrls?.addFolder;
    const name = document.getElementById("subfolderName")?.value?.trim();
    const createdByRaw = document.getElementById("subfolderCreatedBy")?.value?.trim();
    const createdBy = effectiveUserIdFromForm(createdByRaw);
    const parentBatchId = window.__pendingSubfolderParentId || 0;

    if (!url || !parentBatchId) {
        showToast("İstek hazırlanamadı.", "error");
        return;
    }
    if (!name) {
        showToast("Klasör adı girin.", "error");
        return;
    }
    if (!createdBy) {
        showToast("Oluşturan kullanıcıyı seçin.", "error");
        return;
    }

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ parentBatchId, name, createdBy })
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }

    if (!res.ok || !data || data.success === false) {
        showToast(data?.message || "Klasör oluşturulamadı.", "error");
        return;
    }

    showToast(data?.message || "Klasör oluşturuldu.", "success");
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
    window.location.reload();
}

function openReleaseDetailAddSubbatchModal() {
    if (!window.__releaseDetailMeta?.releaseId) {
        showToast("Release bilgisi yüklenemedi.", "error");
        return;
    }
    if (!window.__releaseDetailUrls?.addFolder) {
        showToast("Sunucu adresi tanımlı değil.", "error");
        return;
    }

    const meta = { ...(window.__scriptCreateMeta || { developers: [] }) };
    const devOpts =
        meta.developers && meta.developers.length
            ? meta.developers
                  .map(
                      (d) =>
                          `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`
                  )
                  .join("")
            : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;
    const list = window.__releaseDetailBatches || [];
    const batchOpts =
        `<option value="0">— Üst klasörün hemen altına —</option>` +
        list.map((b) => `<option value="${b.batchId}">${escapeHtml(b.label)}</option>`).join("");

    const content = `
        <form class="row g-3">
            <div class="col-md-12">
                <label class="form-label">Konum</label>
                <select id="releaseDetailParentBatchId" class="form-select">${batchOpts}</select>
                <small class="text-muted d-block mt-1">İlk seçenek: sürümün üst klasörünün bir alt düzeyi. Diğerleri: seçtiğiniz klasörün içine yeni klasör ekler.</small>
            </div>
            <div class="col-md-12">
                <label class="form-label">Yeni klasör adı</label>
                <input id="releaseDetailSubfolderName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Oluşturan</label>
                <select id="releaseDetailSubfolderCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitReleaseDetailAddSubfolder()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Alt klasör ekle", content);
}

async function submitReleaseDetailAddSubfolder() {
    const url = window.__releaseDetailUrls?.addFolder;
    const parentBatchId = Number(document.getElementById("releaseDetailParentBatchId")?.value || 0);
    const releaseId = window.__releaseDetailMeta?.releaseId ?? 0;
    const name = document.getElementById("releaseDetailSubfolderName")?.value?.trim();
    const createdByRaw = document.getElementById("releaseDetailSubfolderCreatedBy")?.value?.trim();
    const createdBy = effectiveUserIdFromForm(createdByRaw);

    if (!url) {
        showToast("İstek adresi eksik.", "error");
        return;
    }
    if (!parentBatchId && !releaseId) {
        showToast("Release veya üst batch seçilemedi.", "error");
        return;
    }
    if (!name) {
        showToast("Klasör adı girin.", "error");
        return;
    }
    if (!createdBy) {
        showToast("Oluşturan kullanıcıyı seçin.", "error");
        return;
    }

    const payload =
        parentBatchId > 0
            ? { parentBatchId, name, createdBy }
            : { parentBatchId: 0, releaseId, name, createdBy };

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload)
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }

    if (!res.ok || !data || data.success === false) {
        showToast(data?.message || "Alt klasör oluşturulamadı.", "error");
        return;
    }

    showToast(data?.message || "Alt klasör oluşturuldu.", "success");
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
    if (typeof refreshReleaseDetailPanels === "function") await refreshReleaseDetailPanels();
    else window.location.reload();
}

function openReleaseDetailAddScriptModal() {
    if (window.__releaseDetailMeta?.isCancelled) {
        showToast("Bu sürüm iptal edilmiş.", "error");
        return;
    }
    if (window.__releaseDetailMeta?.isTreeLocked) {
        showToast("Bu sürüm kilitli; önce düzenlemeye açın.", "error");
        return;
    }
    const rid = window.__releaseDetailMeta?.releaseId;
    if (!rid) {
        showToast("Release bilgisi yok.", "error");
        return;
    }
    void openScriptCreateWizard({ releaseId: rid });
}

function appendScriptRowFromCreateResponse(data) {
    const tbody = document.getElementById("scriptTableBody");
    const scriptId = data.scriptId ?? data.ScriptId;
    if (data == null || !scriptId) return;
    if (!tbody) {
        window.location.reload();
        return;
    }

    const placeholder =
        window.__scriptCreateUrls?.detailUrlPlaceholder ||
        window.__scriptsPageUrls?.detailUrlPlaceholder ||
        "";
    const idStr = String(scriptId);
    const detailHref = placeholder
        ? String(placeholder).replace(/999999999/g, idStr)
        : `/Scripts/Detail/${encodeURIComponent(idStr)}`;

    const rb = data.hasRollback === true || data.HasRollback === true ? "Var" : "—";
    const name      = data.scriptName ?? data.ScriptName ?? "";
    const status    = data.status ?? data.Status ?? "";
    const statusKey = data.statusKey ?? data.StatusKey ?? "Draft";
    const batchName = data.batchName ?? data.BatchName ?? "";
    const devName   = data.developerName ?? data.DeveloperName ?? "";
    const created   = data.createdAtDisplay ?? data.CreatedAtDisplay ?? "";
    const canDel    = data.canDelete === true || data.CanDelete === true;
    const delPh     = window.__scriptsPageUrls?.deleteUrlPlaceholder;
    const delBtn    = canDel && delPh
        ? `<button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteScriptFromList(${Number(scriptId)}, this)">Sil</button>`
        : "";

    const badgeCls = { Draft: "script-badge--draft", Ready: "script-badge--ready", Conflict: "script-badge--conflict" }[statusKey] ?? "script-badge--draft";

    const tr = document.createElement("tr");
    tr.innerHTML = `
        <td><a href="${detailHref}" class="table-link">${escapeHtml(name)}</a></td>
        <td><span class="script-badge ${badgeCls}">${escapeHtml(status)}</span></td>
        <td>${escapeHtml(batchName)}</td>
        <td>${escapeHtml(devName)}</td>
        <td>${escapeHtml(rb)}</td>
        <td>${escapeHtml(created)}</td>
        <td class="text-nowrap"><a class="btn btn-sm btn-table" href="${detailHref}">Detay</a> ${delBtn}</td>`;
    tr.dataset.filterHidden = "0";
    tbody.insertBefore(tr, tbody.firstChild);

    const filterInput = document.getElementById("scriptFilterInput");
    if (filterInput && filterInput.value.trim()) {
        filterInput.dispatchEvent(new Event("input"));
    } else {
        window.__pgState?.["scriptTableBody"]?.refresh(true);
    }
}

async function markScriptReadyInline(scriptId, btn) {
    const url = window.__scriptsPageUrls?.changeStatus;
    if (!url) { showToast("URL tanımlı değil.", "error"); return; }

    btn.disabled = true;
    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({ scriptId, newStatus: 3 }) // 3 = Ready
    });
    let data = null;
    try { data = await res.json(); } catch { /* ignore */ }

    if (!res.ok || data?.success === false) {
        showToast(data?.message || "Durum güncellenemedi.", "error");
        btn.disabled = false;
        return;
    }

    showToast("Script hazır olarak işaretlendi.", "success");

    const td = btn.closest("td");
    if (td) {
        const badge = td.querySelector(".script-badge");
        if (badge) {
            badge.className = "script-badge script-badge--ready";
            badge.textContent = "Hazır";
        }
        btn.remove();
    }
}

async function deleteScriptFromList(scriptId, btn) {
    const ph = window.__scriptsPageUrls?.deleteUrlPlaceholder;
    if (!ph || !scriptId) {
        showToast("Silme adresi tanımlı değil.", "error");
        return;
    }
    if (!confirm("Bu script silinecek (geri alınamaz). Emin misiniz?")) return;
    const url = String(ph).replace(/999999999/g, String(scriptId));
    const res = await fetch(url, {
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: "{}"
    });
    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }
    if (!res.ok || data?.success === false) {
        showToast(data?.message || "Silinemedi.", "error");
        return;
    }
    showToast(data?.message || "Silindi.", "success");
    const tr = btn && btn.closest ? btn.closest("tr") : null;
    if (tr) {
        tr.remove();
        window.__pgState?.["scriptTableBody"]?.refresh();
    } else window.location.reload();
}

function downloadTextFile(filename, text) {
    const blob = new Blob([text ?? ""], { type: "text/plain;charset=utf-8" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(a.href);
}

function renderSwSqlValidateBox(data) {
    const box    = document.getElementById("swSqlValidateBox");
    const btn    = document.getElementById("swCreateScriptBtn");
    const badge  = document.getElementById("swSqlStatusBadge");

    function setBadge(state, text) {
        if (!badge) return;
        badge.className = "sw-sql-badge sw-sql-badge--" + state;
        badge.textContent = text;
    }

    if (!box) return;

    if (!data || data.success === false) {
        box.className = "col-12 alert alert-danger small mb-0";
        box.classList.remove("d-none");
        box.innerHTML = escapeHtml(data?.message || "Doğrulama hatası.");
        setBadge("error", "✗ Hatalı");
        if (btn) btn.disabled = true;
        return;
    }

    if (data.isValid) {
        box.className = "d-none";
        setBadge("ok", "✓ SQL geçerli");
        if (btn) btn.disabled = false;
        return;
    }

    box.className = "col-12 alert alert-danger small mb-0";
    box.classList.remove("d-none");

    const issues = data.issues || [];
    const items = issues.map((i) => {
        const prefix = i.source && i.source !== "SQL" ? `(${i.source}) ` : "";
        return prefix + i.message;
    });

    box.innerHTML =
        "<strong>SQL hatası:</strong><ul class='mb-0 mt-1'>" +
        items.map((t) => `<li>${escapeHtml(t)}</li>`).join("") +
        "</ul>";

    setBadge("error", "✗ Hatalı");
    if (btn) btn.disabled = true;
}

/** @returns {Promise<boolean>} */
async function fetchValidateSqlAndRenderBox() {
    const urls = window.__scriptCreateUrls || window.__scriptsPageUrls;
    const url = urls?.validateSql;
    if (!url) {
        renderSwSqlValidateBox({ success: false, message: "Doğrulama adresi tanımlı değil." });
        return false;
    }
    const sqlScript = document.getElementById("sqlScript")?.value ?? "";
    const rollbackScript = document.getElementById("rollbackScript")?.value ?? "";

    // Boşsa badge'i idle'a çek, validasyon yapma
    if (!sqlScript.trim()) {
        const badge = document.getElementById("swSqlStatusBadge");
        if (badge) { badge.className = "sw-sql-badge sw-sql-badge--idle"; badge.textContent = "— kontrol bekleniyor"; }
        document.getElementById("swSqlValidateBox")?.classList.add("d-none");
        const btn = document.getElementById("swCreateScriptBtn");
        if (btn) btn.disabled = false;
        return true;
    }

    // Kontrol başladı
    const badge = document.getElementById("swSqlStatusBadge");
    if (badge) { badge.className = "sw-sql-badge sw-sql-badge--checking"; badge.textContent = "⟳ kontrol ediliyor…"; }
    try {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            body: JSON.stringify({ sqlScript, rollbackScript })
        });
        let data;
        try {
            data = await res.json();
        } catch {
            renderSwSqlValidateBox({ success: false, message: "Sunucu yanıtı okunamadı." });
            return false;
        }
        if (!res.ok) {
            renderSwSqlValidateBox({
                success: false,
                message: data?.message || `HTTP ${res.status}`
            });
            return false;
        }
        renderSwSqlValidateBox(data);
        return !!(data.success && data.isValid);
    } catch {
        renderSwSqlValidateBox({ success: false, message: "Doğrulama isteği gönderilemedi." });
        return false;
    }
}

/** Conflict review modalındaki düzenlenebilir script taraflarına canlı SQL validasyon ekler. */
function initConflictReviewValidation(wrap, sideA, sideB) {
    const url = window.__scriptsPageUrls?.validateSql || window.__scriptCreateUrls?.validateSql;
    if (!url) return;

    const sides = [
        { canEdit: sideA.canEdit, sqlEl: wrap.querySelector(".cra-sql"), rbEl: wrap.querySelector(".cra-rb"), badge: wrap.querySelector("#cra-val-badge"), box: wrap.querySelector("#cra-val-box") },
        { canEdit: sideB.canEdit, sqlEl: wrap.querySelector(".crb-sql"), rbEl: wrap.querySelector(".crb-rb"), badge: wrap.querySelector("#crb-val-badge"), box: wrap.querySelector("#crb-val-box") }
    ];

    for (const s of sides) {
        if (!s.canEdit || !s.sqlEl) continue;

        const validate = debounce(async () => {
            const sql = s.sqlEl?.value ?? "";
            const rb  = s.rbEl?.value  ?? "";

            if (!sql.trim()) {
                if (s.badge) { s.badge.className = "sw-sql-badge sw-sql-badge--idle ms-2"; s.badge.textContent = "— kontrol bekleniyor"; }
                if (s.box)   { s.box.className = "d-none"; }
                return;
            }

            if (s.badge) { s.badge.className = "sw-sql-badge sw-sql-badge--checking ms-2"; s.badge.textContent = "⟳ kontrol ediliyor…"; }

            try {
                const res  = await fetch(url, { method: "POST", headers: { "Content-Type": "application/json", Accept: "application/json" }, body: JSON.stringify({ sqlScript: sql, rollbackScript: rb }) });
                const data = await res.json();

                if (data.isValid) {
                    if (s.badge) { s.badge.className = "sw-sql-badge sw-sql-badge--ok ms-2"; s.badge.textContent = "✓ SQL geçerli"; }
                    if (s.box)   { s.box.className = "d-none"; }
                } else {
                    if (s.badge) { s.badge.className = "sw-sql-badge sw-sql-badge--error ms-2"; s.badge.textContent = "✗ Hatalı"; }
                    if (s.box) {
                        const items = (data.issues || []).map(i => {
                            const prefix = i.source && i.source !== "SQL" ? `(${i.source}) ` : "";
                            return escapeHtml(prefix + i.message);
                        });
                        s.box.className = "alert alert-danger small mb-0 mt-1 py-2";
                        s.box.innerHTML = items.map(t => `<div>${t}</div>`).join("");
                    }
                }
            } catch {
                if (s.badge) { s.badge.className = "sw-sql-badge sw-sql-badge--idle ms-2"; s.badge.textContent = "— doğrulama yapılamadı"; }
            }
        }, 700);

        s.sqlEl.addEventListener("input", validate);
        s.rbEl?.addEventListener("input", validate);
    }
}

function initWizardAutoValidate() {
    const sqlEl = document.getElementById("sqlScript");
    const rbEl  = document.getElementById("rollbackScript");
    if (!sqlEl) return;
    const doValidate = debounce(() => fetchValidateSqlAndRenderBox(), 700);
    sqlEl.addEventListener("input", doValidate);
    rbEl?.addEventListener("input", doValidate);
}

async function validateScriptWizardSql() {
    const ok = await fetchValidateSqlAndRenderBox();
    if (ok) showToast("T-SQL sözdizimi uygun.", "success");
}

function buildScriptWizardPayload() {
    const name = document.getElementById("scriptName")?.value?.trim();
    const sqlScript = document.getElementById("sqlScript")?.value ?? "";
    const rollbackScript = document.getElementById("rollbackScript")?.value ?? "";
    const devSel = document.getElementById("createDeveloperId");
    const developerId = effectiveUserIdFromForm(devSel?.value?.trim());

    if (!name || !sqlScript) {
        showToast("Script adı ve SQL metni zorunludur.", "error");
        return null;
    }
    if (!developerId) {
        showToast("Geliştirici seçin.", "error");
        return null;
    }

    const hidden = document.getElementById("swSelectedBatchId");
    const bid = hidden?.value ? Number(hidden.value) : 0;
    if (!(bid > 0)) {
        showToast("Bir klasör seçin.", "error");
        return null;
    }
    const payload = {
        name,
        sqlScript,
        rollbackScript: rollbackScript || null,
        developerId
    };
    payload.batchId = bid;
    return payload;
}

async function submitCreateScript() {
    const url =
        window.__scriptsPageUrls?.create ||
        window.__scriptCreateUrls?.create ||
        document.body?.getAttribute("data-script-create-url");
    if (!url) {
        showToast("Script kaydı için sunucu adresi tanımlı değil.", "error");
        return;
    }

    let payload;
    if (document.getElementById("createScriptWizardForm")) {
        payload = buildScriptWizardPayload();
        if (!payload) return;
        const sqlOk = await fetchValidateSqlAndRenderBox();
        if (!sqlOk) {
            showToast("SQL sözdizimi hatalı; önce düzeltin.", "error");
            return;
        }
    } else {
        const fromSelect = document.getElementById("scriptTargetBatchId");
        let batchId = null;
        if (fromSelect) {
            const n = Number(fromSelect.value || 0);
            if (n > 0) batchId = n;
        } else {
            const n = Number(window.__pendingScriptBatchId || 0);
            if (n > 0) batchId = n;
        }

        const name = document.getElementById("scriptName")?.value?.trim();
        const sqlScript = document.getElementById("sqlScript")?.value ?? "";
        const rollbackScript = document.getElementById("rollbackScript")?.value ?? "";
        const devSel = document.getElementById("createDeveloperId");
        const developerId = effectiveUserIdFromForm(devSel?.value?.trim());

        if (!name || !sqlScript) {
            showToast("Script adı ve SQL metni zorunludur.", "error");
            return;
        }
        if (!developerId) {
            showToast("Geliştirici seçin.", "error");
            return;
        }

        payload = {
            name,
            sqlScript,
            rollbackScript: rollbackScript || null,
            batchId: batchId ?? null,
            developerId
        };
    }

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload)
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }

    if (!res.ok || !data || data.success === false || data.Success === false) {
        showToast(data?.message || data?.Message || "Kayıt başarısız.", "error");
        return;
    }

    showToast(data?.message || data?.Message || "Script oluşturuldu.", "success");
    if (document.getElementById("release-detail-refresh") && typeof refreshReleaseDetailPanels === "function") {
        await refreshReleaseDetailPanels();
    } else {
        appendScriptRowFromCreateResponse(data);
    }
    window.__pendingScriptBatchId = null;
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
}

async function openScriptsPageCreateModal() {
    window.__pendingScriptBatchId = null;
    await openScriptCreateWizard({});
}

async function openCreateReleaseModal() {
    const meta = { ...(window.__releaseCreateMeta || { developers: [] }) };
    const devUrl = window.__releaseCreateUrls?.developerOptions;

    let devData = null;
    if (devUrl) {
        try {
            const r = await fetch(devUrl, { headers: { Accept: "application/json" } });
            if (r.ok) devData = await r.json();
        } catch { devData = null; }
    }
    if (devData && Array.isArray(devData.developers) && devData.developers.length > 0)
        meta.developers = devData.developers;

    const devOpts =
        meta.developers && meta.developers.length
            ? meta.developers.map((d) => `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`).join("")
            : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;

    // Pool'daki mevcut versiyonları yükle
    const { children: poolVersions } = await fetchTreeChildrenList(null, 0);
    let versionPickerHtml;
    if (!poolVersions || poolVersions.length === 0) {
        versionPickerHtml = `<div class="sw-empty-state" style="padding:12px 0">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
            <span>Havuzda versiyon yok. Önce <a href="/Batches">Versiyonlar</a> sayfasında bir versiyon oluşturun.</span>
        </div>`;
    } else {
        versionPickerHtml = poolVersions.map(v => {
            const isLinked = !!v.linkedReleaseVersion;
            const disabled = isLinked ? "disabled" : "";
            const hint = isLinked ? ` <span class="sw-lock-badge">${escapeHtml(v.linkedReleaseVersion)}</span>` : "";
            return `<label class="rel-vpick-row ${isLinked ? "rel-vpick-row--disabled" : ""}">
                <input type="radio" name="relVpick" class="rel-vpick-radio" value="${v.batchId}" ${disabled} />
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                <span class="rel-vpick-name">${escapeHtml(v.name || "")}</span>${hint}
            </label>`;
        }).join("");
    }

    const content = `
        <form id="createReleaseForm" class="row g-3">
            <div class="col-12">
                <label class="form-label fw-semibold">Sürüm adı</label>
                <input id="releaseName" class="form-control" autocomplete="off"
                       placeholder="ör. v2.1.0, Nisan-Hotfix, 2026-04-16" />
            </div>
            <div class="col-12">
                <label class="form-label fw-semibold">Versiyon seç</label>
                <p class="small text-muted mb-2">Bu sürüme bağlanacak versiyon. Seçilen versiyon kilitlenir.</p>
                <div class="rel-vpick-host">${versionPickerHtml}</div>
            </div>
            <div class="col-12">
                <label class="form-label fw-semibold">Not <span class="text-muted fw-normal">(isteğe bağlı)</span></label>
                <textarea id="releaseDescription" class="form-control" rows="2"
                          placeholder="Bu sürümde neler değişti, hangi modülü etkiliyor..."></textarea>
            </div>
            <div class="col-md-12">
                <label class="form-label fw-semibold">Oluşturan</label>
                <select id="releaseCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateRelease()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Yeni sürüm", content);
}

function appendReleaseRowFromCreateResponse(data) {
    const tbody = document.getElementById("releaseTableBody");
    const releaseId = data.releaseId ?? data.ReleaseId;
    if (!tbody || data == null || !releaseId) return;

    const placeholder = window.__releaseCreateUrls?.detailUrlPlaceholder || "";
    const idStr = String(releaseId);
    const detailHref = placeholder
        ? String(placeholder).replace(/999999999/g, idStr)
        : `/Releases/Detail/${encodeURIComponent(idStr)}`;

    const releaseName = data.releaseName ?? data.ReleaseName ?? data.version ?? data.Version ?? "";
    const scriptCount = data.scriptCount ?? data.ScriptCount ?? 0;
    const rbCount = data.rollbackScriptCount ?? data.RollbackScriptCount ?? 0;
    const created = data.createdAtDisplay ?? data.CreatedAtDisplay ?? "";

    const adminCb =
        document.getElementById("releaseSelectAll") != null
            ? `<td><input type="checkbox" class="release-row-cb form-check-input" value="${escapeHtml(idStr)}" /></td>`
            : "";

    const tr = document.createElement("tr");
    tr.dataset.releaseId = idStr;
    tr.innerHTML = `
        <td><span class="text-muted small">Aktif</span></td>
        <td>${escapeHtml(releaseName)}</td>
        <td>${escapeHtml(String(scriptCount))}</td>
        <td>${escapeHtml(String(rbCount))}</td>
        <td>${escapeHtml(created)}</td>
        <td><a class="btn btn-sm btn-table" href="${detailHref}">Detay</a></td>`;
    tr.dataset.filterHidden = "0";
    tbody.insertBefore(tr, tbody.firstChild);

    const filterInput = document.getElementById("releaseFilterInput");
    if (filterInput && filterInput.value.trim()) {
        filterInput.dispatchEvent(new Event("input"));
    } else {
        window.__pgState?.["releaseTableBody"]?.refresh(true);
    }
}

async function submitCreateRelease() {
    const url = window.__releaseCreateUrls?.create;
    if (!url) {
        showToast("Release kaydı için sunucu adresi tanımlı değil.", "error");
        return;
    }

    const name = document.getElementById("releaseName")?.value?.trim();
    const description = document.getElementById("releaseDescription")?.value?.trim() || null;
    const createdByRaw = document.getElementById("releaseCreatedBy")?.value?.trim();
    const createdBy = effectiveUserIdFromForm(createdByRaw);
    const selectedVersionEl = document.querySelector(".rel-vpick-radio:checked");
    const selectedVersionId = selectedVersionEl ? Number(selectedVersionEl.value) : 0;

    if (!name) {
        showToast("Sürüm adı zorunludur.", "error");
        return;
    }
    if (!selectedVersionId) {
        showToast("Bir versiyon seçin.", "error");
        return;
    }
    if (!createdBy) {
        showToast("Oluşturan kullanıcıyı seçin.", "error");
        return;
    }

    const payload = {
        name,
        version: name,
        description,
        createdBy,
        rootMode: "existing",
        existingRootBatchId: selectedVersionId,
        newRootBatchName: null
    };

    const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload)
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }

    if (!res.ok || !data || data.success === false || data.Success === false) {
        showToast(data?.message || data?.Message || "Kayıt başarısız.", "error");
        return;
    }

    showToast(data?.message || data?.Message || "Release oluşturuldu.", "success");
    appendReleaseRowFromCreateResponse(data);
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
}
