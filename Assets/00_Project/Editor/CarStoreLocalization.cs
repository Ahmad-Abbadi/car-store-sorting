using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TinyRiders;
using UnityEditor;
using UnityEngine;

/// <summary>Merges store text into the template's eight-language database and CSV source.</summary>
public static class CarStoreLocalization
{
    private static readonly string[] Codes = { "en", "es", "pt-BR", "id", "tr", "pl", "vi", "ar" };

    public static void Merge(CarVariantDefinition[] variants)
    {
        LocalizationDatabase_SO database = AssetDatabase.LoadAssetAtPath<LocalizationDatabase_SO>(
            "Assets/00_Project/03_scriptableObjects/Resources/LocalizationDatabase.asset");
        if (database == null) throw new InvalidOperationException("LocalizationDatabase.asset is missing.");
        var tables = Codes.ToDictionary(code => code, code => database.BuildTable(code));
        foreach (var table in tables.Values)
        {
            table.Remove("store.model_directory");
            table.Remove("store.aisle_model");
            table.Remove("store.directory");
        }
        void Add(string key, params string[] values)
        {
            if (values.Length != Codes.Length) throw new ArgumentException("Expected eight translations for " + key);
            for (int i = 0; i < Codes.Length; i++) tables[Codes[i]][key] = values[i];
        }

        Add("store.progress", "Cars sorted: {0} / 525", "Coches ordenados: {0} / 525",
            "Carros organizados: {0} / 525", "Mobil tersortir: {0} / 525", "Sıralanan arabalar: {0} / 525",
            "Posortowane auta: {0} / 525", "Xe đã xếp: {0} / 525", "السيارات المرتبة: {0} / 525");
        Add("store.section_progress", "{0}: {1} / 35", "{0}: {1} / 35", "{0}: {1} / 35",
            "{0}: {1} / 35", "{0}: {1} / 35", "{0}: {1} / 35", "{0}: {1} / 35", "{0}: {1} / 35");
        Add("store.model_directory_title", "MODEL DIRECTORY", "DIRECTORIO DE MODELOS", "GUIA DE MODELOS",
            "DIREKTORI MODEL", "MODEL REHBERİ", "SPIS MODELI", "DANH MỤC MẪU XE", "دليل الطرازات");
        Add("store.title", "TOY CAR STORE", "TIENDA DE COCHES", "LOJA DE CARRINHOS",
            "TOKO MOBIL MAINAN", "OYUNCAK ARABA MAĞAZASI", "SKLEP Z AUTKAMI",
            "CỬA HÀNG XE ĐỒ CHƠI", "متجر سيارات الألعاب");
        Add("store.model_sign", "MODEL {0}  •  {1}", "MODELO {0}  •  {1}", "MODELO {0}  •  {1}",
            "MODEL {0}  •  {1}", "MODEL {0}  •  {1}", "MODEL {0}  •  {1}", "MẪU {0}  •  {1}", "الطراز {0}  •  {1}");
        Add("store.mixed_cars", "MIXED CARS  •  PICK UP HERE", "COCHES MEZCLADOS  •  RECOGE AQUÍ",
            "CARROS MISTURADOS  •  PEGUE AQUI", "MOBIL CAMPUR  •  AMBIL DI SINI",
            "KARIŞIK ARABALAR  •  BURADAN AL", "POMIESZANE AUTA  •  WEŹ STĄD",
            "XE CHƯA PHÂN LOẠI  •  LẤY TẠI ĐÂY", "سيارات مختلطة  •  خذ من هنا");
        Add("store.zone_legend", "W WEST  •  E EAST  •  B BACK  •  F FRONT",
            "W OESTE  •  E ESTE  •  B FONDO  •  F FRENTE",
            "W OESTE  •  E LESTE  •  B FUNDO  •  F FRENTE",
            "W BARAT  •  E TIMUR  •  B BELAKANG  •  F DEPAN",
            "W BATI  •  E DOĞU  •  B ARKA  •  F ÖN",
            "W ZACHÓD  •  E WSCHÓD  •  B TYŁ  •  F PRZÓD",
            "W TÂY  •  E ĐÔNG  •  B SAU  •  F TRƯỚC",
            "W غرب  •  E شرق  •  B خلف  •  F أمام");
        Model(1, "Cupé de turismo", "Cupê de turismo", "Kupe touring", "Touring Coupe", "Coupe turystyczne", "Coupe du lịch", "كوبيه سياحية");
        Model(2, "Cupé veloz", "Cupê de velocidade", "Kupe sprint", "Sprint Coupe", "Coupe sprinterskie", "Coupe tốc độ", "كوبيه سريعة");
        Model(3, "Escarabajo de rally", "Fusca de rali", "Kumbang reli", "Ralli Beetle", "Garbus rajdowy", "Bọ cánh cứng đua địa hình", "خنفساء رالي");
        Model(4, "Todoterreno de rally", "SUV de rali", "SUV reli", "Ralli SUV", "SUV rajdowy", "SUV đua địa hình", "دفع رباعي رالي");
        Model(5, "Hatchback de rally", "Hatch de rali", "Hatchback reli", "Ralli Hatchback", "Hatchback rajdowy", "Hatchback đua địa hình", "هاتشباك رالي");
        Model(6, "Monoplaza de fórmula", "Carro de fórmula", "Mobil formula", "Formula Yarışçısı", "Bolid formuły", "Xe đua công thức", "سيارة فورمولا");
        Model(7, "Furgoneta de reparto", "Van de entregas", "Van pengantar", "Teslimat Minibüsü", "Furgonetka dostawcza", "Xe giao hàng", "شاحنة توصيل صغيرة");
        Model(8, "Carreras descapotable", "Carro de corrida aberto", "Pembalap kokpit terbuka", "Açık Kokpit Yarışçısı", "Wyścigówka z otwartym kokpitem", "Xe đua buồng lái mở", "سيارة سباق مكشوفة");
        Model(9, "Prototipo de pista", "Protótipo de pista", "Prototipe lintasan", "Pist Prototipi", "Prototyp torowy", "Nguyên mẫu đường đua", "نموذج سباق الحلبة");
        Model(10, "Cupé de resistencia", "Cupê de resistência", "Kupe ketahanan", "Dayanıklılık Coupe", "Coupe wytrzymałościowe", "Coupe đường trường", "كوبيه تحمّل");
        Model(11, "Buggy todoterreno", "Buggy off-road", "Buggy offroad", "Arazi Arabası", "Buggy terenowe", "Xe buggy địa hình", "عربة طرق وعرة");
        Model(12, "Jeep de montaña", "Jipe de trilha", "Jeep lintasan", "Arazi Jeepi", "Jeep terenowy", "Jeep đường mòn", "جيب للمسارات");
        Model(13, "Camión de carga", "Caminhão de carga", "Truk kargo", "Yük Kamyonu", "Ciężarówka towarowa", "Xe tải chở hàng", "شاحنة بضائع");
        Model(14, "Buggy del desierto", "Buggy do deserto", "Buggy gurun", "Çöl Arabası", "Buggy pustynne", "Xe buggy sa mạc", "عربة صحراوية");
        Model(15, "Descapotable clásico", "Roadster clássico", "Roadster klasik", "Klasik Roadster", "Klasyczny roadster", "Xe mui trần cổ điển", "رودستر كلاسيكية");
        Add("store.tutorial",
            "Welcome to the toy car store! Pick up cars from the floor pile. Match each car to the shelf with its model and color. Each shelf needs seven copies. Wrong cars can be taken back. Buy upgrades with coins from correct placements. There is no timer.",
            "¡Bienvenido a la tienda de coches! Recoge coches del montón en el suelo y colócalos en el estante de su modelo y color. Cada estante necesita siete. Puedes retirar los incorrectos. Gana monedas para mejoras. No hay límite de tiempo.",
            "Bem-vindo à loja de carrinhos! Pegue carros da pilha no chão e coloque-os na prateleira do modelo e cor certos. Cada prateleira precisa de sete. Retire os incorretos. Ganhe moedas para melhorias. Não há tempo limite.",
            "Selamat datang di toko mobil mainan! Ambil mobil dari tumpukan di lantai dan cocokkan model serta warnanya. Setiap rak perlu tujuh mobil. Mobil yang salah bisa diambil kembali. Dapatkan koin untuk peningkatan. Tidak ada batas waktu.",
            "Oyuncak araba mağazasına hoş geldin! Arabaları yerdeki yığından al ve model ile rengine uygun rafa yerleştir. Her raf yedi araba ister. Yanlış arabaları geri alabilirsin. Doğru yerleştirerek geliştirme parası kazan. Süre sınırı yok.",
            "Witamy w sklepie z autkami! Bierz auta ze sterty na podłodze i dopasuj model oraz kolor do półki. Każda półka potrzebuje siedmiu sztuk. Błędne auta można zabrać. Za poprawne odkładanie dostajesz monety na ulepszenia. Nie ma limitu czasu.",
            "Chào mừng đến cửa hàng xe đồ chơi! Lấy xe từ đống trên sàn và đặt lên kệ đúng mẫu, đúng màu. Mỗi kệ cần bảy xe. Có thể lấy lại xe đặt sai. Đặt đúng để nhận xu nâng cấp. Không giới hạn thời gian.",
            "مرحباً بك في متجر سيارات الألعاب! خذ السيارات من الكومة على الأرض وضع كل سيارة على رف طرازها ولونها. يحتاج كل رف إلى سبع سيارات. يمكنك استرجاع السيارة الموضوعة خطأً. اكسب عملات للترقيات عند الوضع الصحيح. لا يوجد مؤقت.");
        Add("store.start", "Start sorting", "Empezar", "Começar", "Mulai", "Başla", "Zacznij", "Bắt đầu", "ابدأ الترتيب");
        Add("store.replay", "Replay store", "Reiniciar tienda", "Reiniciar loja", "Ulangi toko",
            "Mağazayı yeniden oyna", "Zagraj ponownie", "Chơi lại cửa hàng", "أعد المتجر");
        Add("store.complete", "Store complete!\n525 cars sorted across 75 shelves.\nTime: {0}  •  Coins left: {1}",
            "¡Tienda completa!\n525 coches en 75 estantes.\nTiempo: {0}  •  Monedas: {1}",
            "Loja completa!\n525 carros em 75 prateleiras.\nTempo: {0}  •  Moedas: {1}",
            "Toko selesai!\n525 mobil di 75 rak.\nWaktu: {0}  •  Sisa koin: {1}",
            "Mağaza tamamlandı!\n75 rafta 525 araba.\nSüre: {0}  •  Kalan para: {1}",
            "Sklep ukończony!\n525 aut na 75 półkach.\nCzas: {0}  •  Monety: {1}",
            "Hoàn thành cửa hàng!\n525 xe trên 75 kệ.\nThời gian: {0}  •  Xu còn: {1}",
            "اكتمل المتجر!\nتم ترتيب ٥٢٥ سيارة على ٧٥ رفاً.\nالوقت: {0}  •  العملات المتبقية: {1}");

        Add("store.build.STD", "Standard", "Estándar", "Padrão", "Standar", "Standart", "Standard", "Tiêu chuẩn", "قياسي");
        Add("store.build.CUS", "Custom", "Personalizado", "Personalizado", "Kustom", "Özel", "Niestandardowy", "Tùy chỉnh", "مخصص");
        Color("Scarlet", "Escarlata", "Escarlate", "Merah tua", "Kızıl", "Szkarłatny", "Đỏ tươi", "قرمزي");
        Color("Tangerine", "Mandarina", "Tangerina", "Jingga", "Mandalina", "Mandarynkowy", "Cam quýt", "برتقالي");
        Color("Amber", "Ámbar", "Âmbar", "Kuning amber", "Kehribar", "Bursztynowy", "Hổ phách", "كهرماني");
        Color("Lime", "Lima", "Lima", "Hijau limau", "Limon yeşili", "Limonkowy", "Xanh chanh", "ليموني");
        Color("Emerald", "Esmeralda", "Esmeralda", "Zamrud", "Zümrüt", "Szmaragdowy", "Lục bảo", "زمردي");
        Color("Teal", "Verde azulado", "Azul petróleo", "Hijau kebiruan", "Camgöbeği", "Morski", "Xanh cổ vịt", "أزرق مخضر");
        Color("Cyan", "Cian", "Ciano", "Sian", "Camgöbeği açık", "Cyjan", "Xanh lơ", "سماوي");
        Color("Cobalt", "Cobalto", "Cobalto", "Kobalt", "Kobalt", "Kobaltowy", "Xanh coban", "كوبالت");
        Color("Violet", "Violeta", "Violeta", "Ungu", "Menekşe", "Fioletowy", "Tím", "بنفسجي");
        Color("Fuchsia", "Fucsia", "Fúcsia", "Fusia", "Fuşya", "Fuksja", "Hồng vân anh", "فوشي");

        foreach (CarVariantDefinition variant in variants)
            for (int i = 0; i < Codes.Length; i++)
            {
                string code = Codes[i];
                string build = tables[code]["store.build." + variant.buildCode];
                string color = tables[code]["store.color." + variant.colorwayName];
                string modelName = tables[code]["category." + variant.baseCarId];
                tables[code]["item." + variant.variantId + ".name"] = modelName + " " + build + " " + color;
            }

        string[] keys = tables.Values.SelectMany(t => t.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var columns = new List<LocalizationDatabase_SO.Column>();
        foreach (string code in Codes)
            columns.Add(new LocalizationDatabase_SO.Column
            {
                Code = code,
                Values = keys.Select(key => tables[code].TryGetValue(key, out string value) ? value : "").ToArray()
            });
        database.SetTable(keys, columns);
        EditorUtility.SetDirty(database);
        Localizer.RefreshDatabase();
        WriteCsv(keys, columns);

        void Color(string english, params string[] other)
        {
            string[] values = new string[8];
            values[0] = english;
            Array.Copy(other, 0, values, 1, 7);
            Add("store.color." + english, values);
        }

        void Model(int number, params string[] other)
        {
            string[] values = new string[8];
            values[0] = CarModelCatalog.EnglishNames[number - 1];
            Array.Copy(other, 0, values, 1, 7);
            Add("category." + CarModelCatalog.Id(number - 1), values);
        }
    }

    private static void WriteCsv(string[] keys, List<LocalizationDatabase_SO.Column> columns)
    {
        var csv = new StringBuilder();
        csv.Append("key");
        foreach (string code in Codes) csv.Append(',').Append(code);
        csv.AppendLine();
        for (int i = 0; i < keys.Length; i++)
        {
            csv.Append(Escape(keys[i]));
            foreach (LocalizationDatabase_SO.Column column in columns) csv.Append(',').Append(Escape(column.Values[i]));
            csv.AppendLine();
        }
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Localization.csv"), csv.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string text) => "\"" + (text ?? "").Replace("\"", "\"\"") + "\"";
}
