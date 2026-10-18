"""Merge quest translations into the existing CSV master without rewriting other rows."""
import csv
import io
from pathlib import Path

# key | en | es | pt-BR | id | tr | pl | vi | ar
translations = """
quest.board.title|COLLECTOR\\nQUESTS|MISIONES DEL\\nCOLECCIONISTA|MISSÕES DO\\nCOLECIONADOR|MISI\\nKOLEKTOR|KOLEKSİYONCU\\nGÖREVLERİ|ZADANIA\\nKOLEKCJONERA|NHIỆM VỤ\\nSƯU TẦM|مهام\\nالجامع
quest.board.open|Collector Quests|Misiones de colección|Missões de coleção|Misi Kolektor|Koleksiyoncu Görevleri|Zadania kolekcjonera|Nhiệm vụ sưu tầm|مهام الجامع
quest.board.subtitle|SORT • DISCOVER • GROW|ORDENA • DESCUBRE • CRECE|ORGANIZE • DESCUBRA • CRESÇA|SORTIR • TEMUKAN • BERKEMBANG|DÜZENLE • KEŞFET • GELİŞ|SORTUJ • ODKRYWAJ • ROZWIJAJ SIĘ|XẾP • KHÁM PHÁ • PHÁT TRIỂN|رتب • اكتشف • تقدم
quest.board.invite|Your next goal starts here|Tu próximo objetivo empieza aquí|Seu próximo objetivo começa aqui|Tujuan berikutnya dimulai di sini|Sıradaki hedefin burada başlıyor|Twój kolejny cel zaczyna się tutaj|Mục tiêu tiếp theo bắt đầu tại đây|هدفك القادم يبدأ هنا
quest.board.ready|{0} rewards ready!|¡{0} recompensas listas!|{0} recompensas prontas!|{0} hadiah siap!|{0} ödül hazır!|Gotowe nagrody: {0}!|{0} phần thưởng đã sẵn sàng!|{0} مكافآت جاهزة!
quest.ui.title|COLLECTOR QUESTS|MISIONES DE COLECCIÓN|MISSÕES DE COLEÇÃO|MISI KOLEKTOR|KOLEKSİYONCU GÖREVLERİ|ZADANIA KOLEKCJONERA|NHIỆM VỤ SƯU TẦM|مهام الجامع
quest.ui.subtitle|Small goals. A spectacular collection.|Pequeñas metas. Una colección espectacular.|Pequenos objetivos. Uma coleção espetacular.|Tujuan kecil. Koleksi luar biasa.|Küçük hedefler. Muhteşem bir koleksiyon.|Małe cele. Wspaniała kolekcja.|Mục tiêu nhỏ. Bộ sưu tập tuyệt vời.|أهداف صغيرة. مجموعة مذهلة.
quest.ui.footer|No rush. No timers. Make the showroom yours.|Sin prisas ni cronómetros. Haz tuyo el salón.|Sem pressa ou cronômetros. O showroom é seu.|Tanpa terburu-buru atau batas waktu. Jadikan ruang pamer milikmu.|Acele yok. Süre yok. Sergi salonunu sahiplen.|Bez pośpiechu i limitów czasu. Stwórz własny salon.|Không vội. Không giới hạn giờ. Tạo phòng trưng bày của bạn.|بلا عجلة أو مؤقتات. اجعل المعرض يعبر عنك.
quest.tab.journey|Journey|Trayectoria|Jornada|Perjalanan|Yolculuk|Podróż|Hành trình|الرحلة
quest.tab.challenges|Challenges|Desafíos|Desafios|Tantangan|Meydan Okumalar|Wyzwania|Thử thách|التحديات
quest.tab.completed|Completed|Completadas|Concluídas|Selesai|Tamamlananlar|Ukończone|Đã hoàn thành|المكتملة
quest.close|Close|Cerrar|Fechar|Tutup|Kapat|Zamknij|Đóng|إغلاق
quest.pin|Pin|Fijar|Fixar|Sematkan|Sabitle|Przypnij|Ghim|تثبيت
quest.unpin|Unpin|Desfijar|Desafixar|Lepas|Sabitlemeyi kaldır|Odepnij|Bỏ ghim|إلغاء التثبيت
quest.claim|Claim|Recoger|Resgatar|Klaim|Al|Odbierz|Nhận|استلام
quest.replace|Replace|Cambiar|Trocar|Ganti|Değiştir|Zamień|Đổi|استبدال
quest.claimed|CLAIMED|RECOGIDA|RESGATADA|DIKLAIM|ALINDI|ODEBRANO|ĐÃ NHẬN|تم الاستلام
quest.ready|READY TO CLAIM|LISTA PARA RECOGER|PRONTA PARA RESGATAR|SIAP DIKLAIM|ALMAYA HAZIR|GOTOWE DO ODBIORU|SẴN SÀNG NHẬN|جاهزة للاستلام
quest.in_progress|IN PROGRESS|EN CURSO|EM ANDAMENTO|BERLANGSUNG|DEVAM EDİYOR|W TOKU|ĐANG THỰC HIỆN|قيد التقدم
quest.rewards|{0} coins  +  {1} XP|{0} monedas  +  {1} EXP|{0} moedas  +  {1} EXP|{0} koin  +  {1} XP|{0} jeton  +  {1} XP|{0} monet  +  {1} PD|{0} xu  +  {1} KN|{0} عملات  +  {1} خبرة
quest.tracker.hint|Claim rewards at the quest board|Recoge premios en el tablón de misiones|Resgate recompensas no quadro de missões|Klaim hadiah di papan misi|Ödülleri görev panosundan al|Odbierz nagrody przy tablicy zadań|Nhận thưởng tại bảng nhiệm vụ|استلم المكافآت من لوحة المهام
quest.empty.challenges|All challenges complete for this store|Todos los desafíos de esta tienda completados|Todos os desafios desta loja concluídos|Semua tantangan toko ini selesai|Bu mağazadaki tüm meydan okumalar tamamlandı|Wszystkie wyzwania tego sklepu ukończone|Đã hoàn thành mọi thử thách của cửa hàng|اكتملت جميع تحديات هذا المتجر
quest.empty.completed|Your claimed quests will appear here|Tus misiones recogidas aparecerán aquí|Suas missões resgatadas aparecerão aqui|Misi yang diklaim akan muncul di sini|Ödülünü aldığın görevler burada görünür|Tutaj pojawią się odebrane zadania|Nhiệm vụ đã nhận thưởng sẽ hiện ở đây|ستظهر هنا المهام التي استلمت مكافآتها
quest.empty.journey|Collector journey complete!|¡Trayectoria de colección completada!|Jornada de coleção concluída!|Perjalanan kolektor selesai!|Koleksiyoncu yolculuğu tamamlandı!|Podróż kolekcjonera ukończona!|Đã hoàn thành hành trình sưu tầm!|اكتملت رحلة الجامع!
quest.toast.complete|QUEST COMPLETE|MISIÓN COMPLETADA|MISSÃO CONCLUÍDA|MISI SELESAI|GÖREV TAMAMLANDI|ZADANIE UKOŃCZONE|HOÀN THÀNH NHIỆM VỤ|اكتملت المهمة
quest.toast.rank|COLLECTOR RANK UP|NUEVO RANGO DE COLECCIÓN|NOVO NÍVEL DE COLEÇÃO|PERINGKAT KOLEKTOR NAIK|KOLEKSİYONCU RÜTBESİ ARTTI|AWANS KOLEKCJONERA|TĂNG HẠNG SƯU TẦM|ارتفعت رتبة الجامع
quest.rank.0|Rookie|Principiante|Iniciante|Pemula|Çaylak|Nowicjusz|Tân binh|مبتدئ
quest.rank.1|Organizer|Organizador|Organizador|Penata|Düzenleyici|Organizator|Người sắp xếp|منظم
quest.rank.2|Collector|Coleccionista|Colecionador|Kolektor|Koleksiyoncu|Kolekcjoner|Nhà sưu tầm|جامع
quest.rank.3|Specialist|Especialista|Especialista|Spesialis|Uzman|Specjalista|Chuyên gia|متخصص
quest.rank.4|Master Collector|Maestro coleccionista|Mestre colecionador|Master Kolektor|Usta Koleksiyoncu|Mistrz kolekcjoner|Bậc thầy sưu tầm|جامع محترف
quest.first_display.title|First Display|Primera exposición|Primeira exposição|Pajangan Pertama|İlk Sergi|Pierwsza ekspozycja|Trưng bày đầu tiên|العرض الأول
quest.getting_organized.title|Getting Organized|Todo en orden|Tudo em ordem|Mulai Tertata|Düzen Kuruluyor|Czas na porządek|Bắt đầu ngăn nắp|بداية التنظيم
quest.full_house.title|Full House|Estante completo|Prateleira cheia|Rak Penuh|Dolu Raf|Pełna półka|Kệ đầy ắp|رف مكتمل
quest.better_tools.title|Better Tools|Mejores herramientas|Ferramentas melhores|Alat Lebih Baik|Daha İyi Araçlar|Lepsze narzędzia|Công cụ tốt hơn|أدوات أفضل
quest.growing_collection.title|Growing Collection|Colección creciente|Coleção crescente|Koleksi Berkembang|Büyüyen Koleksiyon|Rosnąca kolekcja|Bộ sưu tập lớn dần|مجموعة متنامية
quest.theme_collector.title|Theme Collector|Coleccionista temático|Colecionador temático|Kolektor Tema|Tema Koleksiyoncusu|Kolekcjoner motywów|Nhà sưu tầm chủ đề|جامع السمات
quest.showroom_specialist.title|Showroom Specialist|Especialista del salón|Especialista do showroom|Spesialis Ruang Pamer|Sergi Salonu Uzmanı|Specjalista salonu|Chuyên gia trưng bày|متخصص المعرض
quest.store_master.title|Store Master|Maestro de la tienda|Mestre da loja|Master Toko|Mağaza Ustası|Mistrz sklepu|Bậc thầy cửa hàng|محترف المتجر
quest.challenge.sort.title|Fresh Displays|Nuevas exposiciones|Novas exposições|Pajangan Baru|Yeni Sergiler|Nowe ekspozycje|Trưng bày mới|عروض جديدة
quest.challenge.discover.title|New Discoveries|Nuevos descubrimientos|Novas descobertas|Penemuan Baru|Yeni Keşifler|Nowe odkrycia|Khám phá mới|اكتشافات جديدة
quest.challenge.theme.title|Theme Spotlight|Tema destacado|Tema em destaque|Sorotan Tema|Tema Sahnesi|Motyw w centrum uwagi|Tiêu điểm chủ đề|سمة تحت الأضواء
quest.first_display.desc|Correctly shelve {0} cars|Coloca bien {0} coches|Coloque {0} carros corretamente|Susun {0} mobil dengan benar|{0} arabayı doğru rafa yerleştir|Ustaw poprawnie {0} aut|Xếp đúng {0} xe lên kệ|رتب {0} سيارات بشكل صحيح
quest.getting_organized.desc|Correctly shelve {0} cars|Coloca bien {0} coches|Coloque {0} carros corretamente|Susun {0} mobil dengan benar|{0} arabayı doğru rafa yerleştir|Ustaw poprawnie {0} aut|Xếp đúng {0} xe lên kệ|رتب {0} سيارات بشكل صحيح
quest.growing_collection.desc|Correctly shelve {0} cars|Coloca bien {0} coches|Coloque {0} carros corretamente|Susun {0} mobil dengan benar|{0} arabayı doğru rafa yerleştir|Ustaw poprawnie {0} aut|Xếp đúng {0} xe lên kệ|رتب {0} سيارات بشكل صحيح
quest.full_house.desc|Fill {0} shelf correctly|Completa bien {0} estante|Complete {0} prateleira corretamente|Isi {0} rak dengan benar|{0} rafı doğru şekilde doldur|Wypełnij poprawnie {0} półkę|Lấp đầy đúng {0} kệ|أكمل {0} رف بشكل صحيح
quest.better_tools.desc|Own any upgrade|Consigue cualquier mejora|Tenha qualquer melhoria|Miliki peningkatan apa saja|Herhangi bir yükseltmeye sahip ol|Zdobądź dowolne ulepszenie|Sở hữu bất kỳ nâng cấp nào|امتلك أي ترقية
quest.theme_collector.desc|Complete {0} themes|Completa {0} temas|Complete {0} temas|Selesaikan {0} tema|{0} temayı tamamla|Ukończ motywy: {0}|Hoàn thành {0} chủ đề|أكمل {0} سمات
quest.showroom_specialist.desc|Complete {0} themes|Completa {0} temas|Complete {0} temas|Selesaikan {0} tema|{0} temayı tamamla|Ukończ motywy: {0}|Hoàn thành {0} chủ đề|أكمل {0} سمات
quest.store_master.desc|Correctly shelve the entire collection|Coloca bien toda la colección|Organize toda a coleção corretamente|Susun seluruh koleksi dengan benar|Tüm koleksiyonu doğru raflara yerleştir|Ustaw poprawnie całą kolekcję|Xếp đúng toàn bộ bộ sưu tập lên kệ|رتب المجموعة كاملة بشكل صحيح
quest.challenge.sort.desc|Correctly shelve {0} additional cars|Coloca bien {0} coches más|Coloque mais {0} carros corretamente|Susun {0} mobil tambahan dengan benar|{0} araba daha doğru rafa yerleştir|Ustaw poprawnie kolejne {0} aut|Xếp đúng thêm {0} xe lên kệ|رتب {0} سيارات إضافية بشكل صحيح
quest.challenge.discover.desc|Discover {0} additional car designs|Descubre {0} diseños más|Descubra mais {0} modelos de carros|Temukan {0} desain mobil tambahan|{0} yeni araba tasarımı keşfet|Odkryj kolejne {0} wzorów aut|Khám phá thêm {0} mẫu xe|اكتشف {0} تصاميم سيارات إضافية
quest.challenge.theme.desc|Complete the {1} theme|Completa el tema {1}|Complete o tema {1}|Selesaikan tema {1}|{1} temasını tamamla|Ukończ motyw {1}|Hoàn thành chủ đề {1}|أكمل سمة {1}
"""

path = Path("Localization.csv")
existing = path.read_text(encoding="utf-8-sig")
keys = {row[0] for row in csv.reader(io.StringIO(existing)) if row}
out = io.StringIO(newline="")
writer = csv.writer(out, lineterminator="\n")
added = 0
for line in translations.strip().splitlines():
    fields = line.split("|")
    assert len(fields) == 9, fields[0]
    if fields[0] not in keys:
        writer.writerow(fields)
        added += 1
with path.open("a", encoding="utf-8", newline="") as stream:
    if existing and not existing.endswith("\n"):
        stream.write("\n")
    stream.write(out.getvalue())
print(f"Added {added} quest localization keys in all eight languages.")
