@rem Generate the C# code for .proto files

setlocal

@rem enter this directory
cd /d %~dp0

set TOOLS_PATH=..\..\packages\Grpc.Tools.1.18.0\tools\windows_x64

%TOOLS_PATH%\protoc.exe -I. --csharp_out . Protos\api.proto --grpc_out . --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

endlocal
