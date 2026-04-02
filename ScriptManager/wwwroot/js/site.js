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
    const rows = document.querySelectorAll(rowSelector);

    if (!input) return;

    const filterFn = debounce(() => {
        const value = input.value.trim().toLowerCase();

        rows.forEach(row => {
            const text = row.innerText.toLowerCase();
            row.style.display = text.includes(value) ? "" : "none";
        });
    }, 300);

    input.addEventListener("input", filterFn);
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

function openCreateScriptModal() {
    const content = `
        <form id="createScriptForm" class="row g-3">
            <div class="col-md-6">
                <label class="form-label">Script Name</label>
                <input id="scriptName" class="form-control" />
            </div>

            <div class="col-md-6">
                <label class="form-label">Developer Id</label>
                <input id="developerId" class="form-control" type="number" value="1" />
            </div>

            <div class="col-md-12">
                <label class="form-label">SQL Script</label>
                <textarea id="sqlScript" class="form-control" rows="6"></textarea>
            </div>

            <div class="col-md-12">
                <label class="form-label">Rollback Script</label>
                <textarea id="rollbackScript" class="form-control" rows="4"></textarea>
            </div>

            <div class="col-md-6">
                <label class="form-label">Batch Id</label>
                <input id="batchId" class="form-control" type="number" />
            </div>

            <div class="col-12 d-flex justify-content-end gap-2 mt-3">
                <button type="button" class="btn btn-light" data-bs-dismiss="modal">Vazgeç</button>
                <button type="button" class="btn btn-primary" onclick="submitCreateScript()">Kaydet</button>
            </div>
        </form>
    `;

    openGlobalModal("Create Script", content);
}

async function submitCreateScript() {
    showToast("Create Script form altyapısı hazır", "info");
}