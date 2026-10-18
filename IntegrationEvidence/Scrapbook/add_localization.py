import csv, io
from pathlib import Path
p=Path('Localization.csv')
rows=list(csv.reader(io.StringIO(p.read_text(encoding='utf-8-sig'))))
new='''quest.scrapbook.title|MY COLLECTOR SCRAPBOOK|MI ÁLBUM DE COLECCIONISTA|MEU ÁLBUM DE COLECIONADOR|ALBUM KOLEKTORKU|KOLEKSİYON ALBÜMÜM|MÓJ ALBUM KOLEKCJONERA|SỔ SƯU TẦM CỦA TÔI|ألبوم مجموعتي
quest.scrapbook.board|COLLECTOR SCRAPBOOK|ÁLBUM DE COLECCIONISTA|ÁLBUM DO COLECIONADOR|ALBUM KOLEKTOR|KOLEKSİYON ALBÜMÜ|ALBUM KOLEKCJONERA|SỔ SƯU TẦM|ألبوم المجموعة
quest.scrapbook.collected|COLLECTED|RECOGIDO|RECEBIDO|DIKLAIM|ALINDI|ODEBRANO|ĐÃ NHẬN|تم الاستلام
quest.scrapbook.wallet|{0} coins|{0} monedas|{0} moedas|{0} koin|{0} jeton|{0} monet|{0} xu|{0} عملة
quest.scrapbook.notification|Reward ready at the board|Premio listo en el tablón|Recompensa pronta no quadro|Hadiah siap di papan|Ödül panoda hazır|Nagroda czeka przy tablicy|Phần thưởng đang chờ tại bảng|المكافأة جاهزة عند اللوحة
quest.scrapbook.rank_hint|Your collector card has a new badge!|¡Tu tarjeta tiene una nueva insignia!|Seu cartão ganhou um novo emblema!|Kartu kolektormu mendapat lencana baru!|Koleksiyon kartında yeni bir rozet var!|Nowa odznaka na karcie kolekcjonera!|Thẻ sưu tầm có huy hiệu mới!|شارة جديدة على بطاقة مجموعتك!
quest.scrapbook.batch|{0} new achievements!|¡{0} logros nuevos!|{0} novas conquistas!|{0} pencapaian baru!|{0} yeni başarı!|Nowe osiągnięcia: {0}!|{0} thành tích mới!|{0} إنجازات جديدة!'''
keys={r[0] for r in rows}
for line in new.splitlines():
 r=line.split('|')
 if r[0] not in keys: rows.append(r)
with p.open('w',newline='',encoding='utf-8-sig') as f: csv.writer(f,lineterminator='\n').writerows(rows)

