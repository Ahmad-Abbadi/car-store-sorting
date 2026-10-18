"""Append parameterized expansion templates to the existing eight-language master."""
import csv
import io
from pathlib import Path

rows = """
quest.tab.all|All Quests|Todas las misiones|Todas as missões|Semua Misi|Tüm Görevler|Wszystkie zadania|Tất cả nhiệm vụ|جميع المهام
quest.category.-1|All|Todo|Todas|Semua|Tümü|Wszystkie|Tất cả|الكل
quest.category.0|Sorting|Clasificación|Organização|Penyortiran|Sıralama|Sortowanie|Sắp xếp|الترتيب
quest.category.1|Discoveries|Descubrimientos|Descobertas|Penemuan|Keşifler|Odkrycia|Khám phá|الاكتشافات
quest.category.2|Shelves|Estantes|Prateleiras|Rak|Raflar|Półki|Kệ|الرفوف
quest.category.3|Themes|Temas|Temas|Tema|Temalar|Motywy|Chủ đề|السمات
quest.category.4|Car Families|Familias de coches|Famílias de carros|Keluarga Mobil|Araba Aileleri|Rodziny aut|Dòng xe|عائلات السيارات
quest.category.5|Upgrades|Mejoras|Melhorias|Peningkatan|Yükseltmeler|Ulepszenia|Nâng cấp|الترقيات
quest.page.previous|Previous|Anterior|Anterior|Sebelumnya|Önceki|Poprzednia|Trước|السابق
quest.page.next|Next|Siguiente|Próxima|Berikutnya|Sonraki|Następna|Tiếp|التالي
quest.page.status|Page {0} / {1}|Página {0} / {1}|Página {0} / {1}|Halaman {0} / {1}|Sayfa {0} / {1}|Strona {0} / {1}|Trang {0} / {1}|صفحة {0} / {1}
quest.claimed_count|{0} / {1} quests claimed|{0} / {1} misiones cobradas|{0} / {1} missões resgatadas|{0} / {1} misi diklaim|{0} / {1} görev ödülü alındı|Odebrane zadania: {0} / {1}|Đã nhận thưởng {0} / {1} nhiệm vụ|تم استلام {0} / {1} من المهام
quest.empty.category|No quests in this category yet|Aún no hay misiones en esta categoría|Ainda não há missões nesta categoria|Belum ada misi dalam kategori ini|Bu kategoride henüz görev yok|Brak zadań w tej kategorii|Chưa có nhiệm vụ trong mục này|لا توجد مهام في هذه الفئة بعد
quest.toast.batch|{0} quests completed! Visit the board for rewards.|¡{0} misiones completadas! Recoge tus premios en el tablón.|{0} missões concluídas! Visite o quadro para resgatar.|{0} misi selesai! Kunjungi papan untuk hadiah.|{0} görev tamamlandı! Ödüller için panoya uğra.|Ukończone zadania: {0}! Odbierz nagrody przy tablicy.|Đã xong {0} nhiệm vụ! Đến bảng để nhận thưởng.|اكتملت {0} مهام! زر اللوحة لاستلام المكافآت.
quest.rank.5|Curator|Conservador|Curador|Kurator|Küratör|Kustosz|Giám tuyển|أمين المجموعة
quest.rank.6|Collector Legend|Leyenda de colección|Lenda da coleção|Legenda Kolektor|Koleksiyon Efsanesi|Legenda kolekcjonerów|Huyền thoại sưu tầm|أسطورة الجمع
quest.rank.7|Hall of Fame|Salón de la fama|Hall da fama|Hall of Fame|Şöhretler Salonu|Galeria sław|Đại sảnh danh vọng|قاعة المشاهير
quest.expansion.theme.1.title|{1} Starter|Inicios de {1}|Início de {1}|Pemula {1}|{1} Başlangıcı|Początki: {1}|Khởi đầu {1}|بداية {1}
quest.expansion.theme.2.title|{1} Specialist|Especialista en {1}|Especialista em {1}|Spesialis {1}|{1} Uzmanı|Specjalista: {1}|Chuyên gia {1}|متخصص {1}
quest.expansion.theme.3.title|{1} Master|Maestro de {1}|Mestre de {1}|Master {1}|{1} Ustası|Mistrz: {1}|Bậc thầy {1}|محترف {1}
quest.expansion.family.1.title|{1} Enthusiast|Afición por {1}|Entusiasta de {1}|Penggemar {1}|{1} Meraklısı|Miłośnik: {1}|Người mê {1}|هاوي {1}
quest.expansion.family.2.title|{1} Expert|Experto en {1}|Especialista em {1}|Ahli {1}|{1} Eksperi|Ekspert: {1}|Chuyên gia {1}|خبير {1}
quest.expansion.family.3.title|{1} Champion|Campeón de {1}|Campeão de {1}|Juara {1}|{1} Şampiyonu|Czempion: {1}|Nhà vô địch {1}|بطل {1}
quest.expansion.sort.title|{0} Cars on Display|{0} coches expuestos|{0} carros em exposição|{0} Mobil Dipajang|Sergide {0} Araba|{0} aut na wystawie|Trưng bày {0} xe|عرض {0} سيارات
quest.expansion.discover.title|{0} Designs Discovered|{0} diseños descubiertos|{0} modelos descobertos|{0} Desain Ditemukan|{0} Tasarım Keşfedildi|Odkryte wzory: {0}|Khám phá {0} mẫu xe|اكتشاف {0} تصاميم
quest.expansion.shelf.title|{0} Shelves of Pride|{0} estantes de orgullo|{0} prateleiras de orgulho|{0} Rak Kebanggaan|Gurur Veren {0} Raf|{0} półek dumy|{0} kệ đáng tự hào|{0} رفوف للفخر
quest.expansion.themes.title|{0} Complete Worlds|{0} mundos completos|{0} mundos completos|{0} Dunia Lengkap|Tamamlanan {0} Dünya|{0} pełnych światów|{0} thế giới hoàn chỉnh|{0} عوالم مكتملة
quest.expansion.upgrade_own.title|Unlock {1}|Desbloquea {1}|Desbloqueie {1}|Buka {1}|{1} Kilidini Aç|Odblokuj: {1}|Mở khóa {1}|افتح {1}
quest.expansion.upgrade_max.title|Master {1}|Domina {1}|Domine {1}|Kuasai {1}|{1} Ustası Ol|Opanuj: {1}|Làm chủ {1}|أتقن {1}
quest.expansion.upgrade_levels.title|{0} Levels of Improvement|{0} niveles de mejora|{0} níveis de melhoria|{0} Level Peningkatan|{0} Yükseltme Seviyesi|{0} poziomów ulepszeń|{0} cấp nâng cấp|{0} مستويات من التطوير
quest.expansion.theme.desc|Correctly shelve {0} cars from the {1} theme|Coloca bien {0} coches del tema {1}|Coloque corretamente {0} carros do tema {1}|Susun {0} mobil tema {1} dengan benar|{1} temasından {0} arabayı doğru rafa yerleştir|Ustaw poprawnie {0} aut z motywu {1}|Xếp đúng {0} xe chủ đề {1} lên kệ|رتب {0} سيارات من سمة {1} بشكل صحيح
quest.expansion.family.desc|Correctly shelve {0} {1} cars across themes|Coloca bien {0} coches {1} de varios temas|Coloque corretamente {0} carros {1} de vários temas|Susun {0} mobil {1} dari berbagai tema dengan benar|Farklı temalardan {0} {1} arabasını doğru rafa yerleştir|Ustaw poprawnie {0} aut {1} z różnych motywów|Xếp đúng {0} xe {1} thuộc các chủ đề|رتب {0} سيارات من عائلة {1} عبر السمات بشكل صحيح
quest.expansion.sort.desc|Correctly shelve {0} cars|Coloca bien {0} coches|Coloque {0} carros corretamente|Susun {0} mobil dengan benar|{0} arabayı doğru rafa yerleştir|Ustaw poprawnie {0} aut|Xếp đúng {0} xe lên kệ|رتب {0} سيارات بشكل صحيح
quest.expansion.discover.desc|Discover {0} different car designs|Descubre {0} diseños de coches distintos|Descubra {0} modelos de carros diferentes|Temukan {0} desain mobil berbeda|{0} farklı araba tasarımı keşfet|Odkryj {0} różnych wzorów aut|Khám phá {0} mẫu xe khác nhau|اكتشف {0} تصاميم سيارات مختلفة
quest.expansion.shelf.desc|Fill {0} shelves correctly|Completa bien {0} estantes|Complete {0} prateleiras corretamente|Isi {0} rak dengan benar|{0} rafı doğru şekilde doldur|Wypełnij poprawnie {0} półek|Lấp đầy đúng {0} kệ|أكمل {0} رفوف بشكل صحيح
quest.expansion.themes.desc|Complete {0} themes|Completa {0} temas|Complete {0} temas|Selesaikan {0} tema|{0} temayı tamamla|Ukończ motywy: {0}|Hoàn thành {0} chủ đề|أكمل {0} سمات
quest.expansion.upgrade_own.desc|Reach level {0} of {1}|Alcanza el nivel {0} de {1}|Alcance o nível {0} de {1}|Capai level {0} untuk {1}|{1} için {0}. seviyeye ulaş|Osiągnij poziom {0}: {1}|Đạt cấp {0} của {1}|بلغ المستوى {0} من {1}
quest.expansion.upgrade_max.desc|Reach level {0} of {1}|Alcanza el nivel {0} de {1}|Alcance o nível {0} de {1}|Capai level {0} untuk {1}|{1} için {0}. seviyeye ulaş|Osiągnij poziom {0}: {1}|Đạt cấp {0} của {1}|بلغ المستوى {0} من {1}
quest.expansion.upgrade_levels.desc|Own {0} total upgrade levels|Consigue {0} niveles de mejora en total|Tenha {0} níveis de melhoria no total|Miliki total {0} level peningkatan|Toplam {0} yükseltme seviyesine sahip ol|Zdobądź łącznie {0} poziomów ulepszeń|Sở hữu tổng cộng {0} cấp nâng cấp|امتلك {0} مستويات ترقية إجمالا
"""

path=Path("Localization.csv")
existing=path.read_text(encoding="utf-8-sig")
keys={r[0] for r in csv.reader(io.StringIO(existing)) if r}
output=io.StringIO(newline="")
writer=csv.writer(output,lineterminator="\n")
count=0
for line in rows.strip().splitlines():
    fields=line.split("|")
    assert len(fields)==9, fields[0]
    if fields[0] not in keys:
        writer.writerow(fields)
        count+=1
with path.open("a",encoding="utf-8",newline="") as stream:
    if existing and not existing.endswith("\n"):
        stream.write("\n")
    stream.write(output.getvalue())
print(f"Added {count} expansion templates in all eight languages.")
