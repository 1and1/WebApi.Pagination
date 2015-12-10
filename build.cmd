@echo off
::Compiles the Visual Studio solution and creates NuGet packages.
cd /d "%~dp0"

rem Determine VS version
if defined VS140COMNTOOLS (
  ::Visual Studio 2015
  call "%VS140COMNTOOLS%vsvars32.bat"
  goto vs_ok
)
if defined VS120COMNTOOLS (
  ::Visual Studio 2013
  call "%VS120COMNTOOLS%vsvars32.bat"
  goto vs_ok
)
if defined VS110COMNTOOLS (
  ::Visual Studio 2012
  call "%VS110COMNTOOLS%vsvars32.bat"
  goto vs_ok
)
if defined VS100COMNTOOLS (
  ::Visual Studio 2010
  call "%VS100COMNTOOLS%vsvars32.bat"
  goto vs_ok
)
goto err_no_vs
:vs_ok



::Compile Visual Studio solution
nuget restore WebApi.Pagination.sln
msbuild WebApi.Pagination.sln /nologo /t:Rebuild /p:Configuration=Release
if errorlevel 1 pause

::Create NuGet packages
mkdir build\Packages
nuget pack WebApi.Pagination\WebApi.Pagination.csproj -Properties Configuration=Release -IncludeReferencedProjects -Symbols -OutputDirectory build\Packages
if errorlevel 1 pause



exit /b 0
rem Error messages

:err_no_vs
echo ERROR: No Visual Studio installation found. >&2
exit /b 1
