using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

public static class ThemedWebGLHost
{
    [PostProcessBuild(100)]
    public static void AfterBuild(BuildTarget target, string outputPath)
    {
        if (target == BuildTarget.WebGL) Apply(outputPath);
    }
    public static void Apply(string outputPath)
    {
        string path = Path.Combine(outputPath, "index.html");
        string html = File.ReadAllText(path);
        File.Copy("Assets/TinyRiders/Gameplay/Editor/WebHost/TinyRiders.host.js.txt", Path.Combine(outputPath, "TinyRiders.host.js"), true);
        string hostVersion;
        using(var hash=System.Security.Cryptography.SHA256.Create())
            hostVersion=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Path.Combine(outputPath,"TinyRiders.host.js")))).Replace("-","").Substring(0,12).ToLowerInvariant();
        if (!html.Contains("TinyRiders.host.js"))
        {
            const string launch = "var script = document.createElement(\"script\");";
            const string ready = "}).then((unityInstance) => {";
            if (!html.Contains(launch) || !html.Contains(ready)) throw new InvalidOperationException("Unexpected Unity WebGL host template; no partial host edit applied.");
            html = html.Replace("</head>", "<script src=\"TinyRiders.host.js\"></script>\n</head>")
                .Replace(launch, "var tinyRidersReady = configureTinyRidersHost(canvas, config);\n      " + launch)
                .Replace(ready, ready + "\n                tinyRidersReady(unityInstance);");
            File.WriteAllText(path, html);
        }
        html=System.Text.RegularExpressions.Regex.Replace(html,@"TinyRiders\.host\.js(?:\?v=[a-f0-9]+)?","TinyRiders.host.js?v="+hostVersion);
        File.WriteAllText(path,html);
        string cssPath = Path.Combine(outputPath, "TemplateData/style.css");
        string css = File.ReadAllText(cssPath);
        if (!css.Contains("TinyRiders responsive host"))
            File.AppendAllText(cssPath, "\n/* TinyRiders responsive host */\nbody{overflow:hidden;background:#1F1F20}\n#unity-container{position:fixed!important;inset:0!important;transform:none!important;width:100vw!important;height:100vh!important}\n#unity-canvas{display:block;width:100vw!important;height:calc(100vh - 38px)!important}\n#unity-footer{height:38px;width:100vw;background:white;overflow:hidden}\n.unity-mobile #unity-canvas{height:100vh!important}\n");
    }
}
