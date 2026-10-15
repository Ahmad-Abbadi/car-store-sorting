using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class ReadCollectionBuildWarnings
{
 public static string Run()
 {
  var report=BuildReport.GetLatestReport();
  var warnings=new JArray(report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning).Select(m=>m.content));
  File.WriteAllText("IntegrationEvidence/complete_collection_build_warnings.json",warnings.ToString());
  return warnings.ToString();
 }
}
