param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class TileWindowInspection {
    public delegate bool Callback(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(Callback cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Callback cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint command);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetStyle(IntPtr h, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder text, int length);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder text, int length);
    public sealed class Entry {
        public long Handle {get;set;} public uint Pid {get;set;} public string Title {get;set;}
        public string Class {get;set;} public string ParentClass {get;set;} public long Parent {get;set;}
        public long Owner {get;set;} public string Style {get;set;} public string ExStyle {get;set;}
        public bool Child {get;set;} public bool ToolWindow {get;set;} public bool AppWindow {get;set;}
        public bool TransparentInput {get;set;} public bool Visible {get;set;}
    }
    static string Class(IntPtr h) { var s=new StringBuilder(256); GetClassName(h,s,s.Capacity); return s.ToString(); }
    public static List<Entry> Read(uint[] ids) {
        var list=new List<Entry>(); var seen=new HashSet<IntPtr>(); var pids=new HashSet<uint>(ids);
        Callback collect=(h,p)=> {
            uint pid; GetWindowThreadProcessId(h,out pid);
            if(pids.Contains(pid) && seen.Add(h)) {
                var text=new StringBuilder(512); GetWindowText(h,text,text.Capacity);
                long s=GetStyle(h,-16).ToInt64(), x=GetStyle(h,-20).ToInt64(); var parent=GetParent(h);
                list.Add(new Entry { Handle=h.ToInt64(), Pid=pid, Title=text.ToString(), Class=Class(h), ParentClass=Class(parent),
                    Parent=parent.ToInt64(), Owner=GetWindow(h,4).ToInt64(), Style=s.ToString("X"), ExStyle=x.ToString("X"),
                    Child=(s&0x40000000)!=0, ToolWindow=(x&0x80)!=0, AppWindow=(x&0x40000)!=0,
                    TransparentInput=(x&0x20)!=0, Visible=IsWindowVisible(h) });
            }
            return true;
        };
        EnumWindows((h,p)=> {collect(h,p); EnumChildWindows(h,collect,IntPtr.Zero); return true;},IntPtr.Zero);
        return list;
    }
}
'@
$tileRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..')) + [IO.Path]::DirectorySeparatorChar
$tileProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Tile.exe'" | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($tileRoot, [StringComparison]::OrdinalIgnoreCase) })
$tileIds = [uint[]]@($tileProcesses | ForEach-Object { $_.ProcessId })
$report = [ordered]@{ processes = @($tileProcesses | Select-Object ProcessId,ExecutablePath); windows = @([TileWindowInspection]::Read($tileIds)) }
$json = $report | ConvertTo-Json -Depth 5
if ($OutputPath) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $json) }
$json
