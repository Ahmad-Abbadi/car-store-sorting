using UnityEditor;
using UnityEngine;
namespace TinyRiders.Editor
{
    public static partial class CollectorQuestBuilder
    {
        // Small reusable ink illustrations: car, magnifier, shelf, palette, family, wrench, medal.
        static Texture2D ScrapbookIcon(int kind)
        {
            string path=Folder+"/Scrapbook Icon "+kind+".asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool fresh=!texture;if(fresh){texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Scrapbook icon "+kind;}
            var pixels=new Color[64*64];Color ink=new Color(.18f,.37f,.33f);
            void Dot(float x,float y,float radius,Color color){for(int py=0;py<64;py++)for(int px=0;px<64;px++)if((px-x)*(px-x)+(py-y)*(py-y)<=radius*radius)pixels[py*64+px]=color;}
            void Line(float ax,float ay,float bx,float by,float width){int steps=Mathf.CeilToInt(Vector2.Distance(new Vector2(ax,ay),new Vector2(bx,by))*2);for(int i=0;i<=steps;i++){float t=steps==0 ? 0 : (float)i/steps;Dot(Mathf.Lerp(ax,bx,t),Mathf.Lerp(ay,by,t),width,ink);}}
            void Box(int x,int y,int w,int h){for(int py=y;py<y+h;py++)for(int px=x;px<x+w;px++)if(px>=0&&px<64&&py>=0&&py<64)pixels[py*64+px]=ink;}
            switch(kind){
                case 0:Box(8,21,48,15);Line(16,36,23,46,2);Line(23,46,42,46,2);Line(42,46,50,36,2);Dot(19,20,7,ink);Dot(46,20,7,ink);Dot(19,20,3,Paper);Dot(46,20,3,Paper);break;
                case 1:Dot(26,37,19,ink);Dot(26,37,13,Paper);Line(40,23,56,7,4);break;
                case 2:Box(8,9,5,46);Box(51,9,5,46);Box(8,13,48,4);Box(8,32,48,4);Box(8,51,48,4);Box(18,36,11,10);Box(34,36,12,10);Box(18,17,23,10);break;
                case 3:Dot(31,32,25,ink);Dot(43,17,11,Color.clear);Dot(19,40,5,Paper);Dot(34,46,5,Paper);Dot(47,37,5,Paper);Dot(18,24,5,Paper);break;
                case 4:Box(6,27,23,15);Box(35,27,23,15);Line(17,27,17,13,2);Line(46,27,46,13,2);Line(17,13,46,13,2);Dot(13,25,4,ink);Dot(23,25,4,ink);Dot(41,25,4,ink);Dot(51,25,4,ink);break;
                case 5:Line(15,12,46,43,5);Dot(45,44,14,ink);Line(49,51,58,59,6);Dot(16,13,8,ink);Dot(16,13,3,Paper);break;
                default:Line(23,31,17,5,6);Line(40,31,47,5,6);Dot(32,41,20,ink);Dot(32,41,15,new Color(.94f,.72f,.29f));Line(23,41,29,35,2);Line(29,35,42,48,2);break;
            }
            texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Bilinear;
            if(fresh)AssetDatabase.CreateAsset(texture,path);else EditorUtility.SetDirty(texture);return texture;
        }
    }
}
