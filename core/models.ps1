# VOIDTUNE - core/models.ps1
# Copyright (C) 2026 @OTZPT - GPL v3

try {
Add-Type -Language CSharp @"
using System.ComponentModel;
public class TI : INotifyPropertyChanged {
    private bool _s, _a;
    public string Id{get;set;} public string Name{get;set;} public string Desc{get;set;}
    public string Cmd{get;set;} public string RevertCmd{get;set;} public string Badge{get;set;} public string BC{get;set;} public string Cat{get;set;}
    public bool Sel{get{return _s;}set{_s=value;PC("Sel");}}
    public bool Applied{get{return _a;}set{_a=value;PC("Applied");PC("AppliedBrush");PC("AppliedLabel");}}
    public string AppliedBrush{get{return _a?"#22C55E":"#1e1e1e";}}
    public string AppliedLabel{get{return _a?"APPLIED":"";}}
    public event PropertyChangedEventHandler PropertyChanged;
    void PC(string n){if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs(n));}
}
public class AI : INotifyPropertyChanged {
    private bool _s;
    public string Id{get;set;} public string Name{get;set;} public string Cat{get;set;}
    public bool Sel{get{return _s;}set{_s=value;PC("Sel");}}
    public event PropertyChangedEventHandler PropertyChanged;
    void PC(string n){if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs(n));}
}
public class SI : INotifyPropertyChanged {
    private string _st="N/A",_sc="#3A3A3A";
    public string Name{get;set;} public string Desc{get;set;}
    public string Status{get{return _st;}set{_st=value;PC("Status");}}
    public string SC{get{return _sc;}set{_sc=value;PC("SC");}}
    public event PropertyChangedEventHandler PropertyChanged;
    void PC(string n){if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs(n));}
}
public class STI { public string Name{get;set;} public string Cmd{get;set;} public string Hive{get;set;} }
public class DC  { public string Lbl{get;set;}  public string Val{get;set;} public string Sub{get;set;}  }
public class BKI { public string Name{get;set;} public string Date{get;set;} public string Size{get;set;} public string FileName{get;set;} public string Detail{get;set;} public string Valid{get;set;} }
public class PI  { public string Name{get;set;} public int Pid{get;set;} public string Ram{get;set;} public string Cpu{get;set;} public string Category{get;set;} }
public class DRI { public string Name{get;set;} public string Version{get;set;} public string Date{get;set;} public string Mfg{get;set;} public string Cat{get;set;} }
public class LRI { public string Test{get;set;} public string Result{get;set;} public string Rating{get;set;} public string Detail{get;set;} }

"@
} catch {
    [System.Windows.Forms.MessageBox]::Show("Add-Type failed:`n$($_.Exception.Message)", "VOIDTUNE Error")
    exit
}
