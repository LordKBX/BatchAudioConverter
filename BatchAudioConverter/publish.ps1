# load XML file into local variable and cast as XML type.
$doc = [xml](Get-Content "D:\\CODES\\VS\\BatchAudioConverter\\BatchAudioConverter\\BatchAudioConverter.csproj")

$v = [version]$doc.Project.PropertyGroup.Version
$v = [version]::New($v.Major,$v.Minor,$v.Build,$v.Revision+1)

$AppVersion = ""
$InstallerVersion = "1.0.0"
$ret = Read-Host "Version number(default:" $v.ToString() ")"

if($ret.ToString().Trim() -eq ""){
    $AppVersion = $v.ToString()
}
else{
    $AppVersion = $ret.ToString().Trim()
}
$doc.Project.PropertyGroup.Version = $AppVersion
$doc.Project.PropertyGroup.AssemblyVersion = $AppVersion
$doc.Project.PropertyGroup.FileVersion = $AppVersion
$doc.save("D:\\CODES\\VS\\BatchAudioConverter\\BatchAudioConverter\\BatchAudioConverter.csproj")

$confirmation = Read-Host "Installer version(default:" $InstallerVersion ", n=abort compiling)"
if ($confirmation.ToString().Trim() -eq 'n') {
	
}
else {
	if ($confirmation.ToString().Trim() -eq '') {
		}
	else{
		$InstallerVersion = $confirmation.ToString().Trim()
		}
    "Compiling"
    dotnet publish D:\CODES\VS\BatchAudioConverter -p:PublishProfile=FolderProfile
    cmd /C "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" D:\CODES\VS\BatchAudioConverter\BatchAudioConverter\Installer-x64.iss /DMyInstallerVersion=$InstallerVersion
}