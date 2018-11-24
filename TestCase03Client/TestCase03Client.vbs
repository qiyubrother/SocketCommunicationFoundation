dim WshShell
Set WshShell = CreateObject("Wscript.Shell")

For i = 1 to 20
  WshShell.Run "TestCase03Client.exe" 
  WScript.Sleep 200
Next
