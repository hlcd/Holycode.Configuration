param($framework="netcoreapp2.0")

import-module pathutils
(get-item "$psscriptroot/.tools/dotnet").FullName | add-topath

$outDir = "$psscriptroot\.build\hcfg"
$runtime = "win7-x64"
$bindir = $runtime
$config = "Release"

pushd 
try {
    cd $psscriptroot\src\Holycode.Configuration.CLI\
    dotnet build --configuration $config --framework $framework --runtime $runtime
    if (!(test-path $outDir)) { $null = mkdir $outDir }
    cp bin\$config\$framework\$bindir\* $outdir -Recurse -Force

if ($lastexitcode -ne 0){ exit $lastexitcode }
} finally {
    popd
}