# -*- coding: utf-8 -*-
"""
SUNUM_ScriptManager.pptx — görselli sunum (Pillow ile üretilen görseller).
Gereksinim: pip install python-pptx Pillow
"""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from pptx import Presentation
from pptx.enum.text import MSO_ANCHOR
from pptx.util import Inches, Pt

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "sunum_assets"
OUT = ROOT / "SUNUM_ScriptManager.pptx"

# Dashboard renk paleti (Views/Dashboard ile uyumlu)
C_SKY = "#0ea5e9"
C_INDIGO = "#6366f1"
C_ROSE = "#f43f5e"
C_SLATE = "#94a3b8"
C_BG = "#f1f5f9"
C_CARD = "#ffffff"
C_TEXT = "#0f172a"
C_MUTED = "#64748b"


def _fonts():
    windir = os.environ.get("WINDIR", r"C:\Windows")
    candidates = [
        (Path(windir) / "Fonts" / "segoeuib.ttf", Path(windir) / "Fonts" / "segoeui.ttf"),
        (Path(windir) / "Fonts" / "arialbd.ttf", Path(windir) / "Fonts" / "arial.ttf"),
    ]
    for bold, reg in candidates:
        if bold.is_file() and reg.is_file():
            return bold, reg
    return None, None


FONT_BOLD, FONT_REG = _fonts()


def _font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    path = FONT_BOLD if bold and FONT_BOLD else FONT_REG
    if path:
        try:
            return ImageFont.truetype(str(path), size)
        except OSError:
            pass
    return ImageFont.load_default()


def _rounded_rect(
    d: ImageDraw.ImageDraw,
    xy: tuple[int, int, int, int],
    r: int,
    fill: str | None = None,
    outline: str | None = None,
    width: int = 1,
) -> None:
    d.rounded_rectangle(xy, radius=r, fill=fill, outline=outline, width=width)


def gen_cover(w: int = 1920, h: int = 1080) -> Path:
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / (h - 1)
        r = int(15 + t * (30 - 15))
        g = int(23 + t * (58 - 23))
        b = int(42 + t * (138 - 42))
        d.line([(0, y), (w, y)], fill=(r, g, b))

    # Dekoratif halka
    d.ellipse([w - 420, -120, w + 80, 380], outline="#334155", width=3)
    d.ellipse([w - 380, -80, w + 40, 340], outline="#475569", width=2)

    ft = _font(72, True)
    fs = _font(34, False)
    fm = _font(26, False)
    d.text((80, h // 2 - 120), "Script Manager", fill="white", font=ft)
    d.text((80, h // 2 - 10), "Kurumsal T-SQL script yönetimi", fill="#cbd5e1", font=fs)
    d.text((80, h // 2 + 45), "Release · Batch · Script · Çakışma · .NET 9 · EF Core · MediatR", fill="#94a3b8", font=fm)

    # Alt bant — mini katman çizgisi
    band_h = 100
    d.rectangle([0, h - band_h, w, h], fill="#0f172a")
    bx = 80
    layers = [("MVC + API", C_SKY), ("BLL", C_INDIGO), ("DAL", "#10b981")]
    for label, col in layers:
        _rounded_rect(d, [bx, h - band_h + 22, bx + 220, h - 22], 12, fill=col)
        tfont = _font(22, True)
        d.text((bx + 18, h - band_h + 38), label, fill="white", font=tfont)
        bx += 260

    p = ASSETS / "slide_cover.png"
    img.save(p, "PNG")
    return p


def gen_dashboard_mock(w: int = 1400, h: int = 900) -> Path:
    """Gerçek Dashboard KPI kartlarına benzer örnek görsel."""
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)

    _rounded_rect(d, [0, 0, w, 110], 0, fill="#0f172a")
    d.text((40, 32), "Genel Bakış", fill="white", font=_font(40, True))
    d.text((40, 78), "Script ve sürüm durumlarının özeti (örnek — projedeki Dashboard’dan esinlenildi)", fill="#94a3b8", font=_font(22, False))

    cards = [
        ("Toplam Script", "24", f"{C_SKY} · taslak/hazır özeti", C_SKY),
        ("Toplam Sürüm", "5", "Son: 2.4.0", C_INDIGO),
        ("Hazır", "12", "Sürüme eklenebilir", "#22c55e"),
        ("Açık çakışma", "2", "Çözüm gerekiyor", C_ROSE),
    ]
    x0, y0, cw, ch, gap = 40, 140, 320, 200, 24
    for i, (title, val, sub, accent) in enumerate(cards):
        x = x0 + i * (cw + gap)
        _rounded_rect(d, [x, y0, x + cw, y0 + ch], 18, fill=C_CARD, outline="#e2e8f0", width=1)
        d.rectangle([x + 18, y0 + 24, x + 26, y0 + ch - 24], fill=accent)
        d.text((x + 44, y0 + 28), title, fill=C_MUTED, font=_font(22, False))
        d.text((x + 44, y0 + 68), val, fill=C_TEXT, font=_font(52, True))
        d.text((x + 44, y0 + 138), sub, fill=C_MUTED, font=_font(18, False))

    # Basit "pasta" alanı
    cx, cy, r = w - 280, y0 + ch + 140, 110
    d.text((w - 520, y0 + ch + 40), "Script dağılımı (örnek)", fill=C_TEXT, font=_font(28, True))
    for i, (c, span) in enumerate([(C_SKY, 110), (C_INDIGO, 95), (C_ROSE, 70), (C_SLATE, 85)]):
        d.pieslice([cx - r, cy - r, cx + r, cy + r], start=i * 90, end=(i + 1) * 90, fill=c)

    p = ASSETS / "slide_dashboard.png"
    img.save(p, "PNG")
    return p


def gen_goals(w: int = 1200, h: int = 900) -> Path:
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((40, 36), "Hedefler", fill=C_TEXT, font=_font(36, True))
    rows = [
        ("Tek doğru kaynak", "Release / batch altında merkezi script"),
        ("Kalite", "T-SQL doğrulama (ScriptDom + isteğe bağlı NOEXEC)"),
        ("Çakışma görünürlüğü", "Aynı kapsamda nesne & kayıt düzeyi"),
        ("İş akışı", "Geliştirici · Testçi rolleri"),
        ("Güvenlik", "Kilitli batch · rollback · soft delete"),
    ]
    y = 120
    for title, sub in rows:
        _rounded_rect(d, [40, y, w - 40, y + 120], 16, fill=C_CARD, outline="#e2e8f0")
        d.rectangle([40, y, 56, y + 120], fill=C_INDIGO)
        d.text((80, y + 22), title, fill=C_TEXT, font=_font(26, True))
        d.text((80, y + 62), sub, fill=C_MUTED, font=_font(22, False))
        # check
        d.line([(w - 90, y + 45), (w - 70, y + 70), (w - 45, y + 35)], fill="#22c55e", width=5)
        y += 136

    p = ASSETS / "slide_goals.png"
    img.save(p, "PNG")
    return p


def gen_tech(w: int = 1200, h: int = 900) -> Path:
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((40, 36), "Teknoloji yığını", fill=C_TEXT, font=_font(36, True))

    pills = [
        ".NET 9", "ASP.NET MVC", "Web API", "EF Core", "SQL Server",
        "MediatR", "ScriptDom", "SqlClient", "JWT", "Cookie Auth",
        "Swagger", "SHA256",
    ]
    x, y = 40, 120
    for label in pills:
        tw = len(label) * 14 + 48
        _rounded_rect(d, [x, y, x + tw, y + 56], 22, fill="#1e293b")
        d.text((x + 22, y + 14), label, fill="white", font=_font(22, False))
        x += tw + 16
        if x > w - 200:
            x = 40
            y += 72

    d.text((40, h - 120), "Seçim gerekçesi: güncel platform, tek iş kuralı (BLL), güvenilir T-SQL ayrıştırma, çift yüzey (insan + API)", fill=C_MUTED, font=_font(20, False))

    p = ASSETS / "slide_tech.png"
    img.save(p, "PNG")
    return p


def gen_workflow(w: int = 1200, h: int = 900) -> Path:
    """Release → Batch → Script ve çakışma (projedeki kavramlar)."""
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((40, 28), "İş süreçleri — özet", fill=C_TEXT, font=_font(34, True))
    d.text((40, 78), "Sürüm ve batch organizasyonu · script yaşam döngüsü · çakışma tespiti", fill=C_MUTED, font=_font(20, False))

    # Release kutusu
    _rounded_rect(d, [40, 130, 360, 280], 18, fill=C_CARD, outline=C_INDIGO, width=2)
    d.rectangle([40, 130, 52, 280], fill=C_INDIGO)
    d.text((68, 148), "Release (sürüm)", fill=C_TEXT, font=_font(26, True))
    d.text((68, 188), "Ad, versiyon, açıklama", fill=C_MUTED, font=_font(19, False))
    d.text((68, 218), "Kök batch / ağaç", fill=C_MUTED, font=_font(19, False))
    d.text((68, 248), "İptal · ağaç kilidi", fill=C_MUTED, font=_font(19, False))

    # Batch kutusu
    _rounded_rect(d, [400, 130, 720, 280], 18, fill=C_CARD, outline=C_SKY, width=2)
    d.rectangle([400, 130, 412, 280], fill=C_SKY)
    d.text((428, 148), "Batch (iş paketi)", fill=C_TEXT, font=_font(26, True))
    d.text((428, 188), "Hiyerarşi (alt klasörler)", fill=C_MUTED, font=_font(19, False))
    d.text((428, 218), "Release’e bağlı veya bağımsız", fill=C_MUTED, font=_font(19, False))
    d.text((428, 248), "Kilit → script eklenemez", fill=C_ROSE, font=_font(19, True))

    # Script kutusu
    _rounded_rect(d, [760, 130, w - 40, 280], 18, fill=C_CARD, outline="#10b981", width=2)
    d.rectangle([760, 130, 772, 280], fill="#10b981")
    d.text((788, 148), "Script", fill=C_TEXT, font=_font(26, True))
    d.text((788, 188), "SQL + rollback · doğrulama", fill=C_MUTED, font=_font(19, False))
    d.text((788, 218), "Taslak → inceleme → hazır", fill=C_MUTED, font=_font(19, False))
    d.text((788, 248), "Durum: Conflict olabilir", fill=C_ROSE, font=_font(19, True))

    # Oklar
    for x1, x2 in [(360, 400), (720, 760)]:
        d.line([(x1, 205), (x2, 205)], fill=C_SLATE, width=4)
        d.polygon([(x2 - 8, 205 - 10), (x2 - 8, 205 + 10), (x2 + 6, 205)], fill=C_SLATE)

    # Çakışma paneli
    _rounded_rect(d, [40, 320, w - 40, h - 36], 20, fill="#fff1f2", outline=C_ROSE, width=2)
    d.text((60, 340), "Script çakışması (conflict)", fill=C_ROSE, font=_font(28, True))
    lines = [
        "Script kaydedilince ScriptConflictSyncService aynı kapsamdaki diğer script’lerle karşılaştırır.",
        "SQL’den ConflictKey çıkarılır (tablo DDL, DML, nesne, kayıt düzeyi).",
        "Eşleşen çiftler Conflict kaydı; gerekirse script durumu güncellenir.",
        "Çözüm / görmezden gelme: SQL değişirse SHA256 fingerprint ile yeniden değerlendirme.",
    ]
    ly = 388
    for line in lines:
        d.text((60, ly), "• " + line, fill="#881337", font=_font(20, False))
        ly += 34

    p = ASSETS / "slide_workflow.png"
    img.save(p, "PNG")
    return p


def gen_architecture(w: int = 1200, h: int = 900) -> Path:
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((40, 36), "Katmanlı mimari", fill=C_TEXT, font=_font(36, True))

    blocks = [
        (80, 140, "Sunum katmanı", "ScriptManager (MVC)\nREST API", "#3b82f6", "Cookie · Razor\nJWT · Swagger"),
        (80, 340, "İş mantığı (BLL)", "MediatR · Commands / Queries\nValidators · Conflict sync", C_INDIGO, "Tek çekirdek\nAPI + MVC paylaşır"),
        (80, 540, "Veri (DAL)", "EF Core · Repositories\nEntities · Migrations", "#10b981", "SQL Server"),
    ]
    for x, y, title, mid, color, foot in blocks:
        _rounded_rect(d, [x, y, w - 80, y + 160], 20, fill=C_CARD, outline="#cbd5e1", width=2)
        d.rectangle([x, y, x + 12, y + 160], fill=color)
        d.text((x + 36, y + 18), title, fill=C_TEXT, font=_font(28, True))
        d.text((x + 36, y + 58), mid, fill="#334155", font=_font(22, False))
        d.text((x + 36, y + 118), foot, fill=C_MUTED, font=_font(18, False))
        if y < 540:
            d.line([(w // 2, y + 170), (w // 2, y + 200)], fill=C_SLATE, width=4)
            d.polygon([(w // 2 - 12, y + 198), (w // 2 + 12, y + 198), (w // 2, y + 218)], fill=C_SLATE)

    p = ASSETS / "slide_architecture.png"
    img.save(p, "PNG")
    return p


def gen_example_release(w: int = 1280, h: int = 780) -> Path:
    """Örnek 1: Yeni sürüm kartı / form hissi."""
    img = Image.new("RGB", (w, h), "#e8eef7")
    d = ImageDraw.Draw(img)
    d.text((36, 28), "Örnek senaryo: Release oluşturma", fill=C_TEXT, font=_font(32, True))
    d.text((36, 72), "Sürüm kaydı ve kök batch ile paketleme", fill=C_MUTED, font=_font(20, False))

    _rounded_rect(d, [48, 120, w - 48, h - 40], 24, fill=C_CARD, outline="#c7d2fe", width=2)
    d.rectangle([48, 120, 56, h - 40], fill=C_INDIGO)

    bx, by = 100, 160
    fields = [
        ("Sürüm adı", "2026-Q2 · Sipariş modülü"),
        ("Versiyon", "2.5.0"),
        ("Açıklama", "Ödeme tabloları ve indeks düzenlemeleri"),
        ("Durum", "Aktif · İptal değil"),
        ("Kök batch", "ROOT-2026Q2 (otomatik veya seçilen)"),
    ]
    for label, val in fields:
        d.text((bx, by), label.upper(), fill=C_MUTED, font=_font(16, False))
        _rounded_rect(d, [bx, by + 24, w - 100, by + 62], 10, fill="#f8fafc", outline="#e2e8f0")
        d.text((bx + 14, by + 32), val, fill=C_TEXT, font=_font(22, False))
        by += 92

    _rounded_rect(d, [w - 280, 140, w - 60, 200], 14, fill="#dcfce7", outline="#22c55e", width=2)
    d.text((w - 252, 158), "✓ Kayıt", fill="#166534", font=_font(22, True))

    p = ASSETS / "example_01_release.png"
    img.save(p, "PNG")
    return p


def gen_example_batch_tree(w: int = 1280, h: int = 780) -> Path:
    """Örnek 2: Batch hiyerarşisi."""
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((36, 24), "Örnek senaryo: Batch ağacı", fill=C_TEXT, font=_font(32, True))
    d.text((36, 68), "Release altında klasörler; bir dal kilitli", fill=C_MUTED, font=_font(20, False))

    def node(x, y, tw, th, label, sub, color, locked: bool = False):
        _rounded_rect(d, [x, y, x + tw, y + th], 14, fill=C_CARD, outline=color, width=2)
        d.text((x + 16, y + 12), label, fill=C_TEXT, font=_font(22, True))
        d.text((x + 16, y + 44), sub, fill=C_MUTED, font=_font(17, False))
        if locked:
            _rounded_rect(d, [x + tw - 100, y + 10, x + tw - 12, y + 42], 8, fill="#fee2e2", outline=C_ROSE)
            d.text((x + tw - 92, y + 16), "KİLİTLİ", fill=C_ROSE, font=_font(16, True))

    node(80, 130, 280, 88, "Release", "2026-Q2 v2.5.0", C_INDIGO)
    d.line([(220, 218), (220, 248)], fill=C_SLATE, width=3)

    node(80, 260, 260, 82, "Batch", "Özellikler", C_SKY)
    node(360, 260, 260, 82, "Batch", "DB şema", C_SKY)
    d.line([(200, 342), (200, 372)], fill=C_SLATE, width=3)
    d.line([(490, 342), (490, 372)], fill=C_SLATE, width=3)

    node(40, 385, 300, 88, "Alt batch", "Sipariş tabloları", "#10b981")
    node(360, 385, 300, 88, "Alt batch", "Deploy-Prod", "#10b981", locked=True)
    node(680, 385, 300, 88, "Alt batch", "Rollback paketi", C_SLATE)

    d.text((80, h - 56), "Kilitli batch’e yeni script eklenemez (iş kuralı).", fill=C_ROSE, font=_font(20, True))

    p = ASSETS / "example_02_batch_tree.png"
    img.save(p, "PNG")
    return p


def gen_example_script_flow(w: int = 1280, h: int = 780) -> Path:
    """Örnek 3: Script durum akışı."""
    img = Image.new("RGB", (w, h), "#f0f9ff")
    d = ImageDraw.Draw(img)
    d.text((36, 24), "Örnek senaryo: Script yaşam döngüsü", fill=C_TEXT, font=_font(32, True))
    d.text((36, 68), "Taslak → testçi → hazır; çakışmada ayrı dal", fill=C_MUTED, font=_font(20, False))

    steps = [
        ("Draft", "Taslak", "Geliştirici yazar", C_SKY),
        ("Review", "Testçi incelemesi", "PendingTesterReview", C_INDIGO),
        ("Ready", "Hazır", "Sürüme eklenebilir", "#22c55e"),
    ]
    x = 70
    for code, title, sub, col in steps:
        _rounded_rect(d, [x, 200, x + 300, 380], 18, fill=C_CARD, outline=col, width=3)
        d.rectangle([x, 200, x + 8, 380], fill=col)
        d.text((x + 24, 220), title, fill=C_TEXT, font=_font(26, True))
        d.text((x + 24, 268), sub, fill=C_MUTED, font=_font(18, False))
        d.text((x + 24, 310), code, fill=col, font=_font(17, True))
        if x < 70 + 600:
            d.line([(x + 300, 290), (x + 340, 290)], fill=C_SLATE, width=4)
            d.polygon([(x + 332, 282), (x + 332, 298), (x + 348, 290)], fill=C_SLATE)
        x += 360

    _rounded_rect(d, [780, 160, 1220, 420], 20, fill="#fff1f2", outline=C_ROSE, width=3)
    d.text((800, 180), "Conflict", fill=C_ROSE, font=_font(28, True))
    d.text((800, 230), "Aynı tabloya çakan\niki script tespit edildi", fill="#9f1239", font=_font(20, False))
    d.text((800, 310), "Çözüm veya düzeltme\ngerekir", fill="#9f1239", font=_font(18, False))

    p = ASSETS / "example_03_script_flow.png"
    img.save(p, "PNG")
    return p


def gen_example_conflict(w: int = 1280, h: int = 780) -> Path:
    """Örnek 4: İki script çakışması."""
    img = Image.new("RGB", (w, h), C_BG)
    d = ImageDraw.Draw(img)
    d.text((36, 24), "Örnek senaryo: Script çakışması", fill=C_TEXT, font=_font(32, True))
    d.text((36, 68), "Aynı nesneye dokunan iki script — Conflict kaydı", fill=C_MUTED, font=_font(20, False))

    def script_card(x, y, who, obj, sql_hint):
        _rounded_rect(d, [x, y, x + 520, y + 220], 16, fill=C_CARD, outline="#cbd5e1", width=2)
        d.text((x + 20, y + 16), who, fill=C_TEXT, font=_font(24, True))
        d.text((x + 20, y + 52), f"Hedef: {obj}", fill=C_INDIGO, font=_font(20, True))
        _rounded_rect(d, [x + 16, y + 92, x + 504, y + 196], 10, fill="#1e293b")
        d.text((x + 28, y + 108), sql_hint, fill="#e2e8f0", font=_font(17, False))

    script_card(70, 130, "Script #12 · Ayşe", "dbo.Orders", "ALTER TABLE dbo.Orders ADD …")
    script_card(70, 400, "Script #18 · Mehmet", "dbo.Orders", "CREATE INDEX IX_Orders_… ON dbo.Orders")

    cx, cy = 680, 260
    _rounded_rect(d, [cx, cy, cx + 220, cy + 200], 18, fill="#fef2f2", outline=C_ROSE, width=3)
    d.text((cx + 24, cy + 24), "⚡ Çakışma", fill=C_ROSE, font=_font(26, True))
    d.text((cx + 24, cy + 72), "Tablo DDL\n× DML", fill="#991b1b", font=_font(20, False))
    d.text((cx + 24, cy + 140), "ConflictKey\nesleşti", fill="#991b1b", font=_font(16, False))

    d.line([(590, 240), (cx, 290)], fill=C_ROSE, width=2)
    d.line([(590, 500), (cx, 430)], fill=C_ROSE, width=2)

    p = ASSETS / "example_04_conflict.png"
    img.save(p, "PNG")
    return p


def gen_example_validation(w: int = 1280, h: int = 780) -> Path:
    """Örnek 5: T-SQL doğrulama ve GO batch."""
    img = Image.new("RGB", (w, h), "#f8fafc")
    d = ImageDraw.Draw(img)
    d.text((36, 24), "Örnek senaryo: Doğrulama", fill=C_TEXT, font=_font(32, True))
    d.text((36, 68), "GO ile batch ayrımı · ScriptDom · isteğe bağlı NOEXEC", fill=C_MUTED, font=_font(20, False))

    _rounded_rect(d, [48, 120, 620, h - 48], 16, fill="#1e293b", outline="#334155", width=2)
    code_lines = [
        "CREATE TABLE dbo.Audit (Id INT);",
        "GO",
        "INSERT INTO dbo.Audit VALUES (1);",
        "/* yanlış lehçe */",
        "SELECT * FROM t LIMIT 10;",
    ]
    ly = 140
    for i, line in enumerate(code_lines, 1):
        col = "#f87171" if "LIMIT" in line else "#a5b4fc" if line == "GO" else "#e2e8f0"
        d.text((64, ly), f"{i:02d}  {line}", fill=col, font=_font(19, False))
        ly += 36

    _rounded_rect(d, [660, 120, w - 48, h - 48], 16, fill=C_CARD, outline="#86efac", width=2)
    d.text((688, 140), "Kontrol adımları", fill=C_TEXT, font=_font(26, True))
    checks = [
        "GO → iki ayrı batch parse",
        "ScriptDom: CREATE / INSERT sözdizimi",
        "LIMIT → MySQL ipucu uyarısı",
        "İsteğe bağlı: SqlClient SET NOEXEC",
    ]
    cy = 190
    for c in checks:
        d.text((688, cy), "✓  " + c, fill="#166534", font=_font(20, False))
        cy += 44

    p = ASSETS / "example_05_validation.png"
    img.save(p, "PNG")
    return p


def gen_closing(w: int = 1920, h: int = 1080) -> Path:
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / (h - 1)
        r = int(16 + t * (51 - 16))
        g = int(24 + t * (65 - 24))
        bl = int(40 + t * (85 - 40))
        d.line([(0, y), (w, y)], fill=(r, g, bl))

    d.text((80, h // 2 - 80), "Teşekkürler", fill="white", font=_font(80, True))
    d.text((80, h // 2 + 40), "Sorular?", fill="#94a3b8", font=_font(40, False))

    p = ASSETS / "slide_closing.png"
    img.save(p, "PNG")
    return p


def ensure_assets() -> dict[str, Path]:
    ASSETS.mkdir(parents=True, exist_ok=True)
    return {
        "cover": gen_cover(),
        "dashboard": gen_dashboard_mock(),
        "workflow": gen_workflow(),
        "goals": gen_goals(),
        "tech": gen_tech(),
        "architecture": gen_architecture(),
        "ex_release": gen_example_release(),
        "ex_batch": gen_example_batch_tree(),
        "ex_flow": gen_example_script_flow(),
        "ex_conflict": gen_example_conflict(),
        "ex_validate": gen_example_validation(),
        "closing": gen_closing(),
    }


def add_textbox(
    slide,
    left,
    top,
    width,
    height,
    title: str,
    bullets: list[str],
    *,
    body_pt: int = 15,
) -> None:
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = MSO_ANCHOR.TOP
    p0 = tf.paragraphs[0]
    p0.text = title
    p0.font.size = Pt(30)
    p0.font.bold = True
    p0.space_after = Pt(14)

    for line in bullets:
        p = tf.add_paragraph()
        p.text = line
        p.level = 0
        p.font.size = Pt(body_pt)
        p.space_after = Pt(6)
        p.line_spacing = 1.15


def main() -> None:
    assets = ensure_assets()
    prs = Presentation()
    blank = prs.slide_layouts[6]

    # --- Kapak ---
    s0 = prs.slides.add_slide(blank)
    s0.shapes.add_picture(str(assets["cover"]), 0, 0, width=prs.slide_width, height=prs.slide_height)

    content_slides: list[tuple[str, list[str], Path, int]] = [
        (
            "Proje nedir?",
            [
                "T-SQL script’lerini tek sistemde toplayan kurumsal uygulama",
                "Organizasyon: Release (sürüm) → Batch (iş paketi) → Script",
                "Çakışma tespiti ve rol bazlı inceleme (geliştirici / testçi)",
                "Web (MVC + cookie) ve API (REST + JWT) ile çift kanal",
            ],
            assets["dashboard"],
            15,
        ),
        (
            "Release, batch, script, çakışma",
            [
                "Release oluşturma: sürüm adı/versiyon; batch ağacının kökü; iptal ve ağaç kilidi.",
                "Batch oluşturma: hiyerarşik klasörler; release’e bağlama; kilitli batch’e yeni script eklenmez.",
                "Script: oluşturma/güncellemede T-SQL doğrulama; taslak → testçi incelemesi → hazır.",
                "Çakışma: aynı release kapsamındaki script’lerde tablo/nesne/kayıt çakışması; Conflict kaydı ve çözüm.",
                "SQL değişince fingerprint ile önceki ‘kabul’ kayıtlarının geçersizlenmesi.",
            ],
            assets["workflow"],
            13,
        ),
        (
            "Amaçlar",
            [
                "Merkezi kayıt ve izlenebilirlik",
                "Sözdizimi doğrulama ile hata riskini düşürme",
                "Çakışma tespiti ile koordinasyon",
                "Rol tabanlı güvenli operasyon",
            ],
            assets["goals"],
            15,
        ),
        (
            "Teknolojiler",
            [
                ".NET 9 · MVC · Web API · EF Core · SQL Server",
                "MediatR — ince controller, ortak BLL",
                "ScriptDom / SqlClient — T-SQL analizi",
                "JWT + Cookie · Swagger · SHA256 fingerprint",
            ],
            assets["tech"],
            15,
        ),
        (
            "Mimari",
            [
                "Sunum: farklı kimlik ve çıktı (HTML / JSON)",
                "BLL: iş kuralları ve MediatR handler’lar",
                "DAL: kalıcılık ve migration’lar",
                "İstek → Mediator.Send → Handler → DAL",
            ],
            assets["architecture"],
            15,
        ),
    ]

    left_m = Inches(0.45)
    top_m = Inches(0.4)
    text_w = Inches(5.85)
    img_left = Inches(6.45)
    img_top = Inches(1.05)
    img_w = Inches(6.5)
    img_h = Inches(5.9)

    for title, bullets, img_path, body_pt in content_slides:
        slide = prs.slides.add_slide(blank)
        add_textbox(
            slide, left_m, top_m, text_w, Inches(6.5), title, bullets, body_pt=body_pt
        )
        slide.shapes.add_picture(str(img_path), img_left, img_top, width=img_w, height=img_h)

    # --- 5 görsel senaryo örneği ---
    ex_left = Inches(0.45)
    ex_top = Inches(0.38)
    ex_text_w = Inches(3.15)
    ex_img_left = Inches(3.75)
    ex_img_top = Inches(0.95)
    ex_img_w = Inches(9.55)
    ex_img_h = Inches(6.75)

    example_slides: list[tuple[str, list[str], Path]] = [
        (
            "Örnek 1 / 5 — Release",
            [
                "Yeni sürüm: ad, versiyon, açıklama.",
                "Kök batch ile paket hiyerarşisi başlar.",
            ],
            assets["ex_release"],
        ),
        (
            "Örnek 2 / 5 — Batch ağacı",
            [
                "Release altında dallanan iş paketleri.",
                "Kilitli dal: yeni script eklenmez.",
            ],
            assets["ex_batch"],
        ),
        (
            "Örnek 3 / 5 — Script durumları",
            [
                "Taslak → testçi incelemesi → hazır.",
                "Çakışma: ayrı durum ve çözüm süreci.",
            ],
            assets["ex_flow"],
        ),
        (
            "Örnek 4 / 5 — Çakışan script’ler",
            [
                "İki geliştirici aynı tabloya dokununca.",
                "ConflictKey eşleşmesi ve kayıt.",
            ],
            assets["ex_conflict"],
        ),
        (
            "Örnek 5 / 5 — T-SQL doğrulama",
            [
                "GO ile batch’lere bölme ve parse.",
                "ScriptDom + uyarılar + NOEXEC.",
            ],
            assets["ex_validate"],
        ),
    ]

    for title, bullets, img_path in example_slides:
        slide = prs.slides.add_slide(blank)
        add_textbox(
            slide, ex_left, ex_top, ex_text_w, Inches(6.8), title, bullets, body_pt=14
        )
        slide.shapes.add_picture(
            str(img_path), ex_img_left, ex_img_top, width=ex_img_w, height=ex_img_h
        )

    # --- Kapanış ---
    s_end = prs.slides.add_slide(blank)
    s_end.shapes.add_picture(str(assets["closing"]), 0, 0, width=prs.slide_width, height=prs.slide_height)

    try:
        prs.save(str(OUT))
        print("Saved:", OUT)
    except PermissionError:
        alt = ROOT / "SUNUM_ScriptManager_yeni.pptx"
        prs.save(str(alt))
        print("Ana dosya acik veya kilitli; suraya kaydedildi:", alt)
    print("Assets:", ASSETS)


if __name__ == "__main__":
    main()
