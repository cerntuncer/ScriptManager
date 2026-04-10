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

function initGlobalSearch() {
    const input = document.getElementById("globalSearchInput");
    const resultPanel = document.getElementById("globalSearchResult");

    if (!input || !resultPanel) return;

    const handleSearch = debounce(() => {
        const value = input.value.trim().toLowerCase();

        if (!value) {
            resultPanel.classList.add("d-none");
            resultPanel.innerHTML = "";
            return;
        }

        resultPanel.classList.remove("d-none");
        resultPanel.innerHTML = `
            <div class="p-2">
                <div><strong>${value}</strong> için arama sonucu altyapısı hazır.</div>
                <small class="text-muted">İstersen bunu sonra API tabanlı aramaya bağlarız.</small>
            </div>`;
    }, 400);

    input.addEventListener("input", handleSearch);
}

function setupTableFilter(inputId, rowSelector) {
    const input = document.getElementById(inputId);

    if (!input) return;

    const filterFn = debounce(() => {
        const value = input.value.trim().toLowerCase();
        const rows = document.querySelectorAll(rowSelector);

        rows.forEach(row => {
            const text = row.innerText.toLowerCase();
            row.style.display = text.includes(value) ? "" : "none";
        });
    }, 300);

    input.addEventListener("input", filterFn);
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
    const title = `İncele — ${d.tableName || "çakışma"}`;

    const col = (side, cls) => {
        const ro = !side.canEdit;
        const roAttr = ro ? "readonly" : "";
        return `<div class="col-lg-6">
            <h6 class="mb-1">${escapeHtml(side.name || "")}</h6>
            <div class="small text-muted mb-2">${escapeHtml(side.developer || "")}${ro ? " · salt okunur" : ""}</div>
            <label class="form-label small">SQL</label>
            <textarea class="form-control font-monospace mb-2 ${cls}-sql" rows="10" ${roAttr}></textarea>
            <label class="form-label small">Rollback</label>
            <textarea class="form-control font-monospace ${cls}-rb" rows="5" ${roAttr}></textarea>
        </div>`;
    };

    const html = `
      <div class="cr-wrap" data-conflict-id="${Number(d.conflictId)}" data-a-id="${Number(a.id)}" data-b-id="${Number(b.id)}">
        <div class="mb-2"><span class="badge text-bg-warning text-dark">${escapeHtml(d.tableName || "")}</span></div>
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
        const arb = w.querySelector(".cra-rb");
        const bsql = w.querySelector(".crb-sql");
        const brb = w.querySelector(".crb-rb");
        if (asql) asql.value = a.sqlScript ?? "";
        if (arb) arb.value = a.rollbackScript ?? "";
        if (bsql) bsql.value = b.sqlScript ?? "";
        if (brb) brb.value = b.rollbackScript ?? "";
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
    window.location.reload();
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

function swPickerNodeHtml(c) {
    const id = Number(c.batchId);
    const nm = escapeHtml(c.name || "");
    const lockBadge = c.isLocked ? ` <span class="badge bg-secondary">kilitli</span>` : "";
    const disabled = c.canAddScript ? "" : "disabled";
    const hasChildren = c.hasChildren === true;
    const toggle = hasChildren
        ? `<button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2" onclick="swPickerToggleNode(this)">+</button>`
        : `<span class="text-muted small px-2">•</span>`;
    return `
        <li class="sw-node mb-1" data-id="${id}" data-has-children="${hasChildren ? "1" : "0"}" data-loaded="0">
            <div class="d-flex align-items-center gap-2">
                ${toggle}
                <input class="form-check-input sw-node-check" type="checkbox" data-id="${id}" ${disabled} onchange="swPickerSelectNode(this)" />
                <span>${nm}${lockBadge}</span>
            </div>
            <ul class="sw-children list-unstyled ms-4 mt-1 d-none"></ul>
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
        childrenWrap.innerHTML = `<li class="small text-muted">Alt klasör yok.</li>`;
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
    btn.textContent = hidden ? "+" : "−";
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
    });
    if (checked && id > 0) swPickerPickLeaf(id);
    else {
        document.getElementById("swSelectedBatchId").value = "";
        document.getElementById("swLeafPickedHint")?.classList.add("d-none");
    }
}

function swNewFolderButtonHtml() {
    return `<button type="button" class="btn btn-sm btn-outline-primary" onclick="swGoFolderTreePage()">Yeni klasör oluştur</button>`;
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
                if (btn) btn.textContent = "−";
                container = ul;
            }
        }
    }
    const target = host.querySelector(`.sw-node-check[data-id="${want}"]`);
    if (target && !target.disabled) {
        target.checked = true;
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
        host.innerHTML = `<div class="small text-muted mb-2">Henüz klasör yok.</div>${swNewFolderButtonHtml()}`;
        return;
    }
    host.innerHTML = `<div class="d-flex justify-content-end mb-2">${swNewFolderButtonHtml()}</div><ul class="list-unstyled mb-0">${children.map(swPickerNodeHtml).join("")}</ul>`;
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
    openPoolBatchNameModalInner("Üst seviye klasör oluştur");
}

function openPoolChildBatchModal(parentId) {
    const pid = parentId != null ? Number(parentId) : 0;
    if (!pid) return;
    window.__pendingPoolBatchParentId = pid;
    openPoolBatchNameModalInner("Alt batch oluştur");
}

function openPoolBatchNameModalInner(title) {
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
                <label class="form-label">Klasör adı</label>
                <input id="poolBatchNameInput" class="form-control" autocomplete="off" />
            </div>
            <div class="col-12">
                <label class="form-label">Oluşturan</label>
                <select id="poolBatchCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitPoolBatchCreate()">Oluştur</button>
            </div>
        </form>`;
    openGlobalModal(title, content);
}

async function submitPoolBatchCreate() {
    const url = window.__batchesPageUrls?.addPoolFolder;
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
                <div id="swLeafPickedHint" class="alert alert-success py-1 px-2 small d-none">Seçilen klasöre eklenecek.</div>
                <div id="swBatchPickerHost" class="border rounded p-2 bg-light" style="max-height:16rem;overflow:auto"></div>
                <small class="text-muted d-block mt-1">Kilitli klasörlere script eklenemez. Yeni klasör oluşturmak istersen bu alandaki butonu kullanabilirsin.</small>
            </div>
            <div class="col-md-12"><label class="form-label">Script adı</label><input id="scriptName" class="form-control" autocomplete="off" /></div>
            <div class="col-md-12"><label class="form-label">Geliştirici</label><select id="createDeveloperId" class="form-select">${devOpts}</select></div>
            <div class="col-md-12"><label class="form-label">SQL script</label><textarea id="sqlScript" class="form-control font-monospace" rows="8" placeholder="CREATE / ALTER ..."></textarea></div>
            <div class="col-md-12"><label class="form-label">Rollback (isteğe bağlı)</label><textarea id="rollbackScript" class="form-control font-monospace" rows="4"></textarea></div>
            <div id="swSqlValidateBox" class="col-12 alert d-none small mb-0" role="status"></div>
            <div class="col-12 d-flex justify-content-end flex-wrap gap-2 mt-3">
                <button type="button" class="btn btn-outline-secondary" onclick="validateScriptWizardSql()">SQL kontrol et</button>
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" id="swCreateScriptBtn" class="btn btn-primary" onclick="submitCreateScript()">Oluştur</button>
            </div>
        </form>`;

    openGlobalModal("Yeni script", content);

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
    const name = data.scriptName ?? data.ScriptName ?? "";
    const status = data.status ?? data.Status ?? "";
    const batchName = data.batchName ?? data.BatchName ?? "";
    const devName = data.developerName ?? data.DeveloperName ?? "";
    const created = data.createdAtDisplay ?? data.CreatedAtDisplay ?? "";
    const canDel = data.canDelete === true || data.CanDelete === true;
    const delPh = window.__scriptsPageUrls?.deleteUrlPlaceholder;
    const delBtn =
        canDel && delPh
            ? `<button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteScriptFromList(${Number(
                  scriptId
              )}, this)">Sil</button>`
            : "";

    const tr = document.createElement("tr");
    tr.innerHTML = `
        <td>${escapeHtml(name)}</td>
        <td>${escapeHtml(status)}</td>
        <td>${escapeHtml(batchName)}</td>
        <td>${escapeHtml(devName)}</td>
        <td>${escapeHtml(rb)}</td>
        <td><span class="text-muted">—</span></td>
        <td>${escapeHtml(created)}</td>
        <td class="text-nowrap"><a class="btn btn-sm btn-table" href="${detailHref}">Detay</a> ${delBtn}</td>`;
    tbody.insertBefore(tr, tbody.firstChild);

    const filterInput = document.getElementById("scriptFilterInput");
    if (filterInput && filterInput.value.trim()) {
        filterInput.dispatchEvent(new Event("input"));
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
    if (tr) tr.remove();
    else window.location.reload();
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
    const box = document.getElementById("swSqlValidateBox");
    const btn = document.getElementById("swCreateScriptBtn");
    if (!box) return;
    if (!data || data.success === false) {
        box.className = "col-12 alert alert-danger small mb-0";
        box.classList.remove("d-none");
        box.innerHTML = escapeHtml(data?.message || "Doğrulama hatası.");
        if (btn) btn.disabled = true;
        return;
    }
    if (data.isValid) {
        box.className = "col-12 alert alert-success small mb-0";
        box.classList.remove("d-none");
        box.textContent = "T-SQL sözdizimi uygun.";
        if (btn) btn.disabled = false;
        return;
    }
    box.className = "col-12 alert alert-danger small mb-0";
    box.classList.remove("d-none");
    const items = (data.issues || []).map(
        (i) =>
            `${i.source} batch ${i.batchNumber}, satır ${i.line}, sütun ${i.column}: ${i.message}`
    );
    box.innerHTML =
        "<strong>Sözdizimi hataları:</strong><ul class='mb-0 mt-1 small'>" +
        items.map((t) => `<li>${escapeHtml(t)}</li>`).join("") +
        "</ul>";
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
        } catch {
            devData = null;
        }
    }

    if (devData && Array.isArray(devData.developers) && devData.developers.length > 0) {
        meta.developers = devData.developers;
    }

    const rootsRes = await fetchTreeChildrenList(null, 0);
    const roots = (rootsRes.children || []).filter((x) => x.canPackageRelease === true);
    const rootOpts = roots.length
        ? `<option value="">Ana kök seçin...</option>${roots
              .map((x) => `<option value="${x.batchId}">${escapeHtml(x.name || "")}</option>`)
              .join("")}`
        : `<option value="">Onaylı ana kök klasör bulunamadı</option>`;

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
        <form id="createReleaseForm" class="row g-3">
            <div class="col-md-12">
                <label class="form-label">Release adı</label>
                <input id="releaseName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Versiyon</label>
                <input id="releaseVersion" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Oluşturan</label>
                <select id="releaseCreatedBy" class="form-select">${devOpts}</select>
            </div>
            <div class="col-md-12">
                <label class="form-label">Ana kök klasör</label>
                <select id="existingRootBatchId" class="form-select">${rootOpts}</select>
                <small class="text-muted d-block mt-1">Sadece release'e uygun (onaylı) ana kök klasörler listelenir.</small>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateRelease()" ${roots.length ? "" : "disabled"}>Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Yeni release", content);
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

    const releaseName = data.releaseName ?? data.ReleaseName ?? "";
    const version = data.version ?? data.Version ?? "";
    const scriptCount = data.scriptCount ?? data.ScriptCount ?? 0;
    const rbCount = data.rollbackScriptCount ?? data.RollbackScriptCount ?? 0;
    const created = data.createdAtDisplay ?? data.CreatedAtDisplay ?? "";

    const adminCb =
        document.getElementById("releaseSelectAll") != null
            ? `<td><input type="checkbox" class="release-row-cb form-check-input" value="${escapeHtml(idStr)}" /></td>`
            : "";

    const tr = document.createElement("tr");
    tr.innerHTML = `${adminCb}
        <td>${escapeHtml(releaseName)}</td>
        <td>${escapeHtml(version)}</td>
        <td>${escapeHtml(String(scriptCount))}</td>
        <td>${escapeHtml(String(rbCount))}</td>
        <td>${escapeHtml(created)}</td>
        <td><a class="btn btn-sm btn-table" href="${detailHref}">Detay</a></td>`;
    tbody.insertBefore(tr, tbody.firstChild);

    const filterInput = document.getElementById("releaseFilterInput");
    if (filterInput && filterInput.value.trim()) {
        filterInput.dispatchEvent(new Event("input"));
    }
}

async function submitCreateRelease() {
    const url = window.__releaseCreateUrls?.create;
    if (!url) {
        showToast("Release kaydı için sunucu adresi tanımlı değil.", "error");
        return;
    }

    const name = document.getElementById("releaseName")?.value?.trim();
    const version = document.getElementById("releaseVersion")?.value?.trim();
    const createdByRaw = document.getElementById("releaseCreatedBy")?.value?.trim();
    const createdBy = effectiveUserIdFromForm(createdByRaw);

    if (!name || !version) {
        showToast("Release adı ve versiyon zorunludur.", "error");
        return;
    }
    if (!createdBy) {
        showToast("Oluşturan kullanıcıyı seçin.", "error");
        return;
    }

    const rid = Number(document.getElementById("existingRootBatchId")?.value || 0);
    if (!rid) {
        showToast("Ana kök klasör seçin.", "error");
        return;
    }
    const payload = {
        name,
        version,
        createdBy,
        rootMode: "existing",
        existingRootBatchId: rid,
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
    const newReleaseId = data.releaseId ?? data.ReleaseId;
    const ph = window.__releaseCreateUrls?.detailUrlPlaceholder;
    if (newReleaseId && ph) {
        window.location.href = String(ph).replace(/999999999/g, String(newReleaseId));
        return;
    }
    appendReleaseRowFromCreateResponse(data);
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
}
