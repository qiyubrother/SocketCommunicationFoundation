dim WshShell
Set WshShell = CreateObject("Wscript.Shell")

For i = 1 to 10
  WshShell.Run "TestCase07Client.exe" 
  WScript.Sleep 20
Next
