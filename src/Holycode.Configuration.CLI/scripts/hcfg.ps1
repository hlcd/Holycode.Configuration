$o = hcfg.exe @args

try {
    $cfg = $o | out-string | ConvertFrom-Json 
    $list = $cfg.psobject.members | ? { $_.MemberType -eq "NoteProperty" } | % { 
        if ($_.Value -isnot [string]) { 
            new-object -type PSCustomObject -property @{ Name = $_.Name; Value = $_.Value.Value; Source = $_.Value.Source } 
        } else {
            new-object -type PSCustomObject -property @{ Name = $_.Name; Value = $_.Value }
        }
    } 
    $list 
    
} catch {
    return $o
}