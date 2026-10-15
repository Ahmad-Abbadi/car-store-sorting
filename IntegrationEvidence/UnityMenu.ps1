param([string]$MenuPath, [switch]$Invoke)
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class UnityMenuBridge {
 [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr h);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr h,uint id,StringBuilder text,int max,uint flags);
 [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr h,int pos);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr h,int pos);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
}
'@
$unityHandle=(Get-Process Unity | Where-Object MainWindowTitle -like '*Car Store Sorting*' | Select-Object -First 1).MainWindowHandle
if (-not $unityHandle) { throw 'The project Unity window is unavailable' }
function Read-UnityMenu([IntPtr]$Handle,[string]$Prefix) {
 for($index=0;$index -lt [UnityMenuBridge]::GetMenuItemCount($Handle);$index++) {
  $label=[Text.StringBuilder]::new(512)
  [void][UnityMenuBridge]::GetMenuString($Handle,$index,$label,512,1024)
  $name=($label.ToString() -split "`t")[0].Replace('&','')
  $path=if($Prefix){$Prefix+'/'+$name}else{$name}
  $child=[UnityMenuBridge]::GetSubMenu($Handle,$index)
  if($child -ne [IntPtr]::Zero) { Read-UnityMenu $child $path }
  elseif($name) { [pscustomobject]@{Path=$path;Id=[UnityMenuBridge]::GetMenuItemID($Handle,$index)} }
 }
}
$matches=@(Read-UnityMenu ([UnityMenuBridge]::GetMenu($unityHandle)) '' | Where-Object { $_.Path -like $MenuPath })
if($Invoke) {
 if($matches.Count -ne 1) { throw "Expected exactly one matching menu command, got $($matches.Count): $MenuPath" }
 [void][UnityMenuBridge]::PostMessage($unityHandle,273,[IntPtr]::new($matches[0].Id),[IntPtr]::Zero)
}
$matches
