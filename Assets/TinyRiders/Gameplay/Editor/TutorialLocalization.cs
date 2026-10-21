using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TutorialLocalization
{
    // Same column order as the project's master CSV.
    public static readonly string[][] Rows = {
        new[]{"tutorial.store.pickup","Pick up a car","Recoge un coche","Pegue um carro","Ambil sebuah mobil","Bir araba al","Podnieś samochód","Nhặt một chiếc xe","التقط سيارة"},
        new[]{"tutorial.store.match","Find its matching shelf","Busca su estante correspondiente","Encontre a prateleira correspondente","Cari rak yang cocok","Eşleşen rafı bul","Znajdź pasujący regał","Tìm kệ phù hợp","ابحث عن الرف المطابق"},
        new[]{"tutorial.store.place","Place the car here","Coloca el coche aquí","Coloque o carro aqui","Letakkan mobil di sini","Arabayı buraya yerleştir","Postaw samochód tutaj","Đặt xe ở đây","ضع السيارة هنا"},
        new[]{"tutorial.hint.mouse.pickup","Left-click to pick up","Clic izquierdo para recoger","Clique esquerdo para pegar","Klik kiri untuk mengambil","Almak için sol tıkla","Lewy przycisk: podnieś","Nhấp trái để nhặt","انقر بزر الفأرة الأيسر للالتقاط"},
        new[]{"tutorial.hint.mouse.place","Right-click to place","Clic derecho para colocar","Clique direito para colocar","Klik kanan untuk menaruh","Yerleştirmek için sağ tıkla","Prawy przycisk: odłóż","Nhấp phải để đặt","انقر بزر الفأرة الأيمن للوضع"},
        new[]{"tutorial.hint.mouse.take_back","Left-click to take back","Clic izquierdo para recuperar","Clique esquerdo para pegar de volta","Klik kiri untuk mengambil kembali","Geri almak için sol tıkla","Lewy przycisk: zabierz","Nhấp trái để lấy lại","انقر بزر الفأرة الأيسر للاستعادة"},
        new[]{"tutorial.hint.touch.pickup","Tap the hand button to pick up","Toca la mano para recoger","Toque na mão para pegar","Ketuk tombol tangan untuk mengambil","Almak için el düğmesine dokun","Dotknij dłoni, aby podnieść","Chạm nút bàn tay để nhặt","اضغط زر اليد للالتقاط"},
        new[]{"tutorial.hint.touch.place","Tap the place button to place","Toca colocar para dejar el coche","Toque no botão de colocar","Ketuk tombol taruh untuk menaruh","Yerleştirme düğmesine dokun","Dotknij przycisku odkładania","Chạm nút đặt để xếp xe","اضغط زر الوضع لوضع السيارة"},
        new[]{"tutorial.hint.touch.take_back","Tap the hand button to take back","Toca la mano para recuperar","Toque na mão para pegar de volta","Ketuk tombol tangan untuk mengambil kembali","Geri almak için el düğmesine dokun","Dotknij dłoni, aby zabrać auto","Chạm nút bàn tay để lấy lại","اضغط زر اليد لاستعادة السيارة"},
        new[]{"tutorial.desktop_move","WASD  •  Mouse to look","WASD  •  Ratón para mirar","WASD  •  Mouse para olhar","WASD  •  Mouse untuk melihat","WASD  •  Fareyle etrafa bak","WASD  •  Rozglądaj się myszą","WASD  •  Chuột để nhìn","WASD  •  حرك الفأرة للنظر"},
        new[]{"tutorial.touch_move","Drag left to move • right to look","Arrastra: izquierda para moverte • derecha para mirar","Arraste: esquerda para mover • direita para olhar","Geser kiri untuk bergerak • kanan untuk melihat","Sol tarafı sürükle: yürü • sağ taraf: bak","Przeciągnij: lewa strona ruch • prawa rozglądanie","Kéo bên trái để đi • bên phải để nhìn","اسحب يسار الشاشة للتحرك ويمينها للنظر"},
        new[]{"tutorial.title","Your First Shelf","Tu primera estantería","Sua primeira prateleira","Rak Pertamamu","İlk Rafın","Twoja pierwsza półka","Kệ đầu tiên của bạn","رفك الأول"},
        new[]{"tutorial.approach","Walk to the car","Acércate al coche","Vá até o carro","Dekati mobil","Arabaya doğru yürü","Podejdź do auta","Đi đến chiếc xe","اقترب من السيارة"},
        new[]{"tutorial.pickup","Pick up the car","Recoge el coche","Pegue o carro","Ambil mobil","Arabayı al","Podnieś auto","Nhặt chiếc xe","التقط السيارة"},
        new[]{"tutorial.match","Match the car to its shelf","Pon el coche en su estantería","Coloque o carro na prateleira certa","Taruh mobil di rak yang cocok","Arabayı doğru rafa koy","Odłóż auto na właściwą półkę","Đặt xe vào đúng kệ","ضع السيارة على رفها المطابق"},
        new[]{"tutorial.practice","Sort these two!","¡Ordena estos dos!","Organize estes dois!","Rapikan dua mobil ini!","Bu ikisini yerleştir!","Posortuj te dwa auta!","Xếp hai chiếc này nhé!","رتب هاتين السيارتين!"},
        new[]{"tutorial.complete","Ready for your store!","¡Todo listo para tu tienda!","Tudo pronto para sua loja!","Siap untuk tokomu!","Mağazan için hazırsın!","Czas na twój sklep!","Sẵn sàng cho cửa hàng của bạn!","أنت جاهز لمتجرك!"},
        new[]{"tutorial.correct","Take it back and match the picture","Recógelo y busca la imagen correcta","Pegue de volta e combine com a imagem","Ambil kembali dan cocokkan gambarnya","Geri al ve resimle eşleştir","Zabierz auto i dopasuj obrazek","Lấy lại xe và ghép đúng hình","استعد السيارة وطابق الصورة"},
        new[]{"tutorial.progress","{0} / 3 sorted","{0} / 3 ordenados","{0} / 3 organizados","{0} / 3 rapi","{0} / 3 yerleştirildi","{0} / 3 na miejscu","Đã xếp {0} / 3","تم ترتيب {0} / 3"},
        new[]{"tutorial.exit","YOUR STORE","TU TIENDA","SUA LOJA","TOKOMU","MAĞAZAN","TWÓJ SKLEP","CỬA HÀNG CỦA BẠN","متجرك"},
        new[]{"tutorial.skip","Skip Tutorial","Saltar tutorial","Pular tutorial","Lewati tutorial","Eğitimi atla","Pomiń samouczek","Bỏ qua hướng dẫn","تخطي التدريب"},
        new[]{"tutorial.replay","Learn to Play","Cómo jugar","Aprenda a jogar","Belajar bermain","Oynamayı öğren","Naucz się grać","Học cách chơi","تعلم اللعب"}
    };
    public static void Build()
    {
        string[] codes={"en","es","pt-BR","id","tr","pl","vi","ar"};
        var data=Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
        foreach(var table in data.tables)
        {
            int col=System.Array.IndexOf(codes,table.LocaleIdentifier.Code)+1;
            if(col<=0) continue;
            foreach(var row in Rows) table.AddEntry(row[0],row[col]);
            EditorUtility.SetDirty(table); EditorUtility.SetDirty(table.SharedData);
        }
        string csv=File.ReadAllText("Localization.csv");
        csv=System.Text.RegularExpressions.Regex.Replace(csv,@"(?m)^""?tutorial\.[^\r\n]*(\r?\n|$)","");
        if(!csv.EndsWith("\n")) csv+="\r\n";
        csv+=string.Join("\r\n",Rows.Select(row=>string.Join(",",row.Select(value=>"\""+value.Replace("\"","\"\"")+"\""))))+"\r\n";
        File.WriteAllText("Localization.csv",csv,new System.Text.UTF8Encoding(true));
        AssetDatabase.SaveAssets();
    }
}
