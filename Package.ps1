$ErrorActionPreference = "Stop"

$publishPath = Join-Path $PSScriptRoot "publish"

[IO.Directory]::CreateDirectory($publishPath) | Out-Null

Compress-Archive (Join-Path $PSScriptRoot "Generator.Gui\bin\Release\net5.0-windows\win-x64\publish\*") (Join-Path $publishPath "Generator.Gui.zip") -Force
Compress-Archive (Join-Path $PSScriptRoot "Interpreter.Gui\bin\Release\net5.0-windows\win-x64\publish\*") (Join-Path $publishPath "Interpreter.Gui.zip") -Force
Compress-Archive (Join-Path $PSScriptRoot "Interpreter.Pipe\bin\Release\net5.0\win-x64\publish\*") (Join-Path $publishPath "Interpreter.Pipe.zip") -Force