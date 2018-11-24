dim WshShell
Set WshShell = CreateObject("Wscript.Shell")

For i = 1 to 200
  WshShell.Run "TestCase01Client.exe" 
  WScript.Sleep 200
Next
