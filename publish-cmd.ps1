param(
    [Parameter()]
    [ValidateSet("local","choco","octo")]
    $type="local",
    $publishdir = $null,
    [switch][bool] $build,
    [switch][bool] $newBuildNo = $true
)

ipmo require
req process
req pathutils
req nupkg

if ($newBuildNo) {
    update-buildversion "$psscriptroot\src\Holycode.Configuration"
}

if ($build) {
    & "$psscriptroot\build-cmd.ps1"
}
#if (!(test-path $publishdir)) { $null = mkdir $publishdir }
#cp "$psscriptroot\.build\hcfg" $publishdir -Recurse -Force


pushd
try {
    switch($type) {
        "local" {
            cd "$psscriptroot\.build\hcfg"
            $installPath = "C:\tools\hcfg"
            write-host "installing hcfg to $installpath"
            if (!(test-path $installpath)) { $null = mkdir $installpath }
            copy-item  * $installpath -force -Recurse

            ipmo require
            req pathutils

            $installpath | add-topath -persistent
        }

        "octo" {
            cd "$psscriptroot\.build\hcfg"

            if ($null -eq (where-is octo)) {
                choco install -y octopustools
            }

            $r = invoke octo pack -id "tools.hcfg" --outFolder="$psscriptroot\.nupkg\hcfg" -passthru
            $line = $r | ? { $_ -match "Saving ([^\s]*) to" }
            $nupkg = $matches[1]

            write-output "$psscriptroot\.nupkg\hcfg\$nupkg"
            
            cd "$psscriptroot\.nupkg\hcfg"
            $src = $env:NUGET_PUSH_SOURCE
            if ($src -ne $null) {
                invoke nuget push $nupkg -source $src -Verbose
            } else {
                Write-Warning "If you want to push to nuget, set `$env:NUGET_PUSH_SOURCE variable to target feed"
            }
        }
        "choco" {
            $chocoDir = "$psscriptroot\.choco"
            if (!(test-path "$chocoDir\bin")) { $null = mkdir "$chocoDir\bin" }
            cp "$psscriptroot\.build\hcfg\*" "$chocoDir\bin" -Recurse -Force
            cd $chocoDir

            $nuspec = get-content "hcfg.nuspec.xml"
            $version = invoke "bin\hcfg" "--version" -passthru
            if ($version -match "version\s*(.*)") { $version = $matches[1] }
            $nuspec = $nuspec | % { $_ -replace "{version}",$version }
            $p = "$($pwd.Path)\hcfg.nuspec"
            Write-host "updating nuspec '$p'"
            [System.IO.File]::WriteAllLines($p, $nuspec, [System.Text.Encoding]::UTF8)

            invoke cpack

            $nupkg = "hcfg.$version.nupkg"

            $src = $env:CHOCO_PUSH_SOURCE
            if ($src -ne $null) {
                invoke nuget push $nupkg -source $src -Verbose
            } else {
                Write-Warning "If you want to push to chocolatey, set `$env:CHOCO_PUSH_SOURCE variable to target feed"
            }
        }
    }
} finally {
    popd
}

