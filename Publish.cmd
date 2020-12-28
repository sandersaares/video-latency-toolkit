dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true Generator.Gui
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true Interpreter.Gui
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true Interpreter.Pipe

pause