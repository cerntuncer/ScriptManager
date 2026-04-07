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

async function fetchScriptPickerOptionsHtml(pickUrl) {
    const url =
        pickUrl || window.__releaseCreateUrls?.scriptPicker || window.__releaseDetailUrls?.scriptPicker;
    if (!url) return "";
    try {
        const r = await fetch(url, { headers: { Accept: "application/json" } });
        if (!r.ok) return "";
        const d = await r.json();
        const arr = d.scripts || d.Scripts || [];
        return arr
            .map((s) => {
                const id = s.id ?? s.Id;
                const nm = s.name ?? s.Name ?? "";
                const lb = s.label ?? s.Label ?? "";
                const t = `${lb} — ${nm}`;
                return `<option value="${id}">${escapeHtml(t)}</option>`;
            })
            .join("");
    } catch {
        return "";
    }
}

/** Seçili klasör (batch) için script; batch oluşturma bu ekranda yok. */
async function openCreateScriptModal(batchId) {
    const id = batchId != null ? Number(batchId) : 0;
    if (!id) {
        showToast("Önce bir batch seçin (Release detayından “Script ekle”).", "error");
        return;
    }
    window.__pendingScriptBatchId = id;

    const meta = { ...(window.__scriptCreateMeta || { developers: [] }) };
    const devUrl = window.__scriptCreateUrls?.developerOptions;
    if (devUrl) {
        try {
            const r = await fetch(devUrl, { headers: { Accept: "application/json" } });
            if (r.ok) {
                const data = await r.json();
                if (Array.isArray(data.developers) && data.developers.length > 0) {
                    meta.developers = data.developers;
                }
            }
        } catch {
            /* gömülü meta */
        }
    }

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
        <form id="createScriptForm" class="row g-3">
            <div class="col-12">
                <p class="small text-muted mb-0">Seçili batch #${id} altına script eklenir. Alt yapı için aynı ekranda “Alt batch” kullanın.</p>
            </div>
            <div class="col-md-12">
                <label class="form-label">Script adı</label>
                <input id="scriptName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Geliştirici</label>
                <select id="createDeveloperId" class="form-select">${devOpts}</select>
            </div>
            <div class="col-md-12">
                <label class="form-label">SQL script</label>
                <textarea id="sqlScript" class="form-control font-monospace" rows="8" placeholder="CREATE / ALTER ..."></textarea>
            </div>
            <div class="col-md-12">
                <label class="form-label">Rollback (isteğe bağlı)</label>
                <textarea id="rollbackScript" class="form-control font-monospace" rows="4"></textarea>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateScript()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Script oluştur", content);
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
    const list = window.__releaseDetailBatches || [];
    if (!list.length) {
        showToast("Bu release için klasör listesi yüklenemedi.", "error");
        return;
    }

    window.__pendingScriptBatchId = null;
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
    const batchOpts = list
        .map((b) => `<option value="${b.batchId}">${escapeHtml(b.label)}</option>`)
        .join("");

    const content = `
        <form id="createScriptForm" class="row g-3">
            <div class="col-md-12">
                <label class="form-label">Hedef batch</label>
                <select id="scriptTargetBatchId" class="form-select">${batchOpts}</select>
            </div>
            <div class="col-md-12">
                <label class="form-label">Script adı</label>
                <input id="scriptName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Geliştirici</label>
                <select id="createDeveloperId" class="form-select">${devOpts}</select>
            </div>
            <div class="col-md-12">
                <label class="form-label">SQL script</label>
                <textarea id="sqlScript" class="form-control font-monospace" rows="8" placeholder="CREATE / ALTER ..."></textarea>
            </div>
            <div class="col-md-12">
                <label class="form-label">Rollback (isteğe bağlı)</label>
                <textarea id="rollbackScript" class="form-control font-monospace" rows="4"></textarea>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateScript()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Script ekle", content);
}

function appendScriptRowFromCreateResponse(data) {
    const tbody = document.getElementById("scriptTableBody");
    const scriptId = data.scriptId ?? data.ScriptId;
    if (data == null || !scriptId) return;
    if (!tbody) {
        window.location.reload();
        return;
    }

    const placeholder = window.__scriptCreateUrls?.detailUrlPlaceholder || "";
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

async function submitCreateScript() {
    const url =
        window.__scriptCreateUrls?.create || document.body?.getAttribute("data-script-create-url");
    if (!url) {
        showToast("Script kaydı için sunucu adresi tanımlı değil.", "error");
        return;
    }

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
    const developerIdRaw = devSel?.value?.trim();
    const developerId = effectiveUserIdFromForm(developerIdRaw);

    if (!name || !sqlScript) {
        showToast("Script adı ve SQL metni zorunludur.", "error");
        return;
    }
    if (!developerId) {
        showToast("Geliştirici seçin.", "error");
        return;
    }

    const payload = {
        name,
        sqlScript,
        rollbackScript: rollbackScript || null,
        batchId: batchId ?? null,
        developerId
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
    const devUrl = window.__scriptsPageUrls?.developerOptions;
    if (!devUrl) {
        showToast("Sayfa yapılandırması eksik.", "error");
        return;
    }
    let data = null;
    try {
        const r = await fetch(devUrl, { headers: { Accept: "application/json" } });
        if (r.ok) data = await r.json();
    } catch {
        data = null;
    }
    const devs = data?.developers ?? [];
    const devOpts = devs.length
        ? devs.map((d) => `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`).join("")
        : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;
    window.__pendingScriptBatchId = null;

    const content = `
        <form id="createScriptForm" class="row g-3">
            <div class="col-md-12">
                <p class="small text-muted mb-0">Batch seçilmez; script havuzda kalır. Bir sürüme bağlamak için Releases’te yeni release oluştururken veya detaydan script taşıyın.</p>
            </div>
            <div class="col-md-12">
                <label class="form-label">Script adı</label>
                <input id="scriptName" class="form-control" autocomplete="off" />
            </div>
            <div class="col-md-12">
                <label class="form-label">Geliştirici</label>
                <select id="createDeveloperId" class="form-select">${devOpts}</select>
            </div>
            <div class="col-md-12">
                <label class="form-label">SQL script</label>
                <textarea id="sqlScript" class="form-control font-monospace" rows="8" placeholder="CREATE / ALTER ..."></textarea>
            </div>
            <div class="col-md-12">
                <label class="form-label">Rollback (isteğe bağlı)</label>
                <textarea id="rollbackScript" class="form-control font-monospace" rows="4"></textarea>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateScript()">Oluştur</button>
            </div>
        </form>
    `;

    openGlobalModal("Yeni script", content);
}

function releaseCreateRefreshFolderParentSelects() {
    const rows = Array.from(document.querySelectorAll("#releaseCreateFolderHost .release-folder-row"));
    for (const row of rows) {
        const fid = row.dataset.fid;
        const sel = row.querySelector(".folder-parent");
        if (!sel) continue;
        const prev = sel.value;
        let html = `<option value="">Üst klasörün hemen altı (aynı seviye)</option>`;
        for (const r of rows) {
            if (r.dataset.fid === fid) continue;
            const label = (r.querySelector(".folder-name")?.value || "").trim() || "Adsız klasör";
            const id = r.dataset.fid;
            html += `<option value="${id}">${escapeHtml(label)}</option>`;
        }
        sel.innerHTML = html;
        if (prev && Array.from(sel.options).some((o) => o.value === prev)) sel.value = prev;
    }
}

function releaseCreateAddFolderRow() {
    const host = document.getElementById("releaseCreateFolderHost");
    if (!host) return;
    const id = `rf_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`;
    const opts = window.__releaseCreateScriptOptionsHtml ?? "";
    const div = document.createElement("div");
    div.className = "release-folder-row border rounded p-2 mb-2";
    div.dataset.fid = id;
    div.innerHTML = `
        <div class="d-flex justify-content-between align-items-center mb-1">
            <span class="small fw-medium">Klasör</span>
            <button type="button" class="btn btn-sm btn-link text-danger p-0 delete-choose">Kaldır</button>
        </div>
        <label class="form-label small mb-0">Ad</label>
        <input type="text" class="form-control form-control-sm folder-name mb-2" autocomplete="off" />
        <label class="form-label small mb-0">Üst</label>
        <select class="form-select form-select-sm folder-parent mb-2"></select>
        <label class="form-label small mb-0">Bu klasöre alınacak scriptler (Ctrl ile çoklu)</label>
        <select class="form-select form-select-sm folder-scripts" multiple size="4">${opts}</select>`;
    host.appendChild(div);
    div.querySelector(".delete-choose")?.addEventListener("click", () => {
        div.remove();
        releaseCreateRefreshFolderParentSelects();
    });
    div.querySelector(".folder-name")?.addEventListener("input", () => releaseCreateRefreshFolderParentSelects());
    releaseCreateRefreshFolderParentSelects();
}

function releaseCreateBuildFolderTreePayload() {
    const rows = Array.from(document.querySelectorAll("#releaseCreateFolderHost .release-folder-row"));
    const flat = [];
    for (const row of rows) {
        const id = row.dataset.fid;
        const name = row.querySelector(".folder-name")?.value?.trim() ?? "";
        const parentRaw = row.querySelector(".folder-parent")?.value?.trim() ?? "";
        const parentId = parentRaw || null;
        const scriptSel = row.querySelector(".folder-scripts");
        const scriptIds = scriptSel
            ? Array.from(scriptSel.selectedOptions || [])
                  .map((o) => Number(o.value))
                  .filter((n) => n > 0)
            : [];
        flat.push({ id, parentId, name, scriptIds });
    }
    const effIds = new Set(flat.map((f) => f.id));
    for (const f of flat) {
        if (f.parentId && !effIds.has(f.parentId)) f.parentId = null;
    }
    for (const f of flat) {
        const seen = new Set();
        let p = f.parentId;
        while (p) {
            if (seen.has(p)) return { folders: [], error: "Üst klasör seçiminde döngü var." };
            seen.add(p);
            const parent = flat.find((x) => x.id === p);
            p = parent?.parentId ?? null;
        }
    }
    const effective = flat.filter(
        (f) => f.name || f.scriptIds.length > 0 || flat.some((c) => c.parentId === f.id)
    );
    for (const f of effective) {
        const hasChildren = effective.some((c) => c.parentId === f.id);
        if ((f.scriptIds.length > 0 || hasChildren || f.parentId) && !f.name) {
            return { folders: [], error: "Klasör eklediyseniz her birine bir ad verin." };
        }
    }
    const eid = new Set(effective.map((e) => e.id));
    for (const f of effective) {
        if (f.parentId && !eid.has(f.parentId)) f.parentId = null;
    }
    const roots = effective.filter((f) => !f.parentId);
    function mapNode(f) {
        return {
            name: f.name,
            scriptIds: f.scriptIds,
            children: effective.filter((c) => c.parentId === f.id).map(mapNode)
        };
    }
    return { folders: roots.map(mapNode), error: null };
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

    const devOpts =
        meta.developers && meta.developers.length
            ? meta.developers
                  .map(
                      (d) =>
                          `<option value="${d.id}">${escapeHtml(d.name)} (${escapeHtml(d.email)})</option>`
                  )
                  .join("")
            : `<option value="">Kayıtlı kullanıcı bulunamadı</option>`;

    const scriptOpts = await fetchScriptPickerOptionsHtml();
    window.__releaseCreateScriptOptionsHtml = scriptOpts;

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
                <label class="form-label">Üst klasöre alınacak scriptler (isteğe bağlı, Ctrl ile çoklu)</label>
                <select id="createReleaseScriptPick" class="form-select" multiple size="6" style="min-height:8rem">${scriptOpts}</select>
                <small class="text-muted d-block mt-1">Listede yalnızca <strong>Hazır</strong> durumundaki scriptler yer alır. Seçilenler sürümün üst klasörüne yerleştirilir.</small>
            </div>
            <div class="col-md-12">
                <label class="form-label mb-1">İç klasörler (isteğe bağlı)</label>
                <p class="small text-muted mb-2">İstediğiniz derinlikte klasör ekleyin; her satırda üstünü seçip yalnızca <strong>Hazır</strong> scriptleri o klasöre atayın.</p>
                <div id="releaseCreateFolderHost"></div>
                <button type="button" class="btn btn-sm btn-outline-secondary" onclick="releaseCreateAddFolderRow()">Klasör satırı ekle</button>
            </div>
            <div class="col-md-12">
                <p class="small text-muted mb-0">Script veya klasör tanımlamadan da release açabilirsiniz; düzeni sonra detaydan tamamlayın.</p>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateRelease()">Oluştur</button>
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

    const pickEl = document.getElementById("createReleaseScriptPick");
    const selectedScriptIds = pickEl
        ? Array.from(pickEl.selectedOptions || [])
              .map((o) => Number(o.value))
              .filter((n) => n > 0)
        : [];

    const tree = releaseCreateBuildFolderTreePayload();
    if (tree.error) {
        showToast(tree.error, "error");
        return;
    }

    const payload = {
        name,
        version,
        createdBy,
        selectedScriptIds: selectedScriptIds.length ? selectedScriptIds : null,
        folders: tree.folders.length ? tree.folders : null
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
    const rid = data.releaseId ?? data.ReleaseId;
    const ph = window.__releaseCreateUrls?.detailUrlPlaceholder;
    if (rid && ph) {
        window.location.href = String(ph).replace(/999999999/g, String(rid));
        return;
    }
    appendReleaseRowFromCreateResponse(data);
    const modalEl = document.getElementById("globalAppModal");
    bootstrap.Modal.getInstance(modalEl)?.hide();
}

async function submitAssignScriptToRelease() {
    const url = window.__releaseDetailUrls?.assignScript;
    const releaseId = window.__releaseDetailMeta?.releaseId;
    const scriptIdRaw = document.getElementById("assignScriptId")?.value?.trim();
    const targetRaw = document.getElementById("assignTargetBatchId")?.value?.trim();
    const scriptId = scriptIdRaw ? Number(scriptIdRaw) : 0;
    const targetBatchId = targetRaw ? Number(targetRaw) : 0;

    if (!url || !releaseId) {
        showToast("Taşıma adresi tanımlı değil.", "error");
        return;
    }
    if (!scriptId || !targetBatchId) {
        showToast("Script ve hedef klasör seçin.", "error");
        return;
    }

    const res = await fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Accept: "application/json"
        },
        body: JSON.stringify({ releaseId, scriptId, targetBatchId })
    });

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* ignore */
    }

    if (!res.ok || !data?.success) {
        showToast(data?.message || "Taşıma başarısız.", "error");
        return;
    }

    showToast(data.message || "Script taşındı.", "success");
    if (typeof refreshReleaseDetailPanels === "function") await refreshReleaseDetailPanels();
    else window.location.reload();
}