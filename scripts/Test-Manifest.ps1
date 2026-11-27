param([string]$Path=(Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets/app.manifest'))
$ErrorActionPreference='Stop'
$file=Get-Item -LiteralPath $Path
if($file.Length -gt 65536){throw 'Manifest exceeds the allowed size.'}
$settings=[Xml.XmlReaderSettings]::new()
$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit
$settings.XmlResolver=$null;$settings.MaxCharactersInDocument=65536
$reader=[Xml.XmlReader]::Create($file.FullName,$settings)
try {$doc=[Xml.XmlDocument]::new();$doc.XmlResolver=$null;$doc.Load($reader)}finally{$reader.Dispose()}
$ns=[Xml.XmlNamespaceManager]::new($doc.NameTable)
$ns.AddNamespace('a','urn:schemas-microsoft-com:asm.v1')
$ns.AddNamespace('t','urn:schemas-microsoft-com:asm.v3')
$ns.AddNamespace('c','urn:schemas-microsoft-com:compatibility.v1')
$ns.AddNamespace('w','http://schemas.microsoft.com/SMI/2005/WindowsSettings')
$identity=$doc.SelectNodes('/a:assembly/a:assemblyIdentity',$ns)
if($identity.Count -ne 1 -or $identity[0].GetAttribute('name') -ne 'Flux.Desktop' -or $identity[0].GetAttribute('version') -ne '2.1.0.0'){throw 'Unexpected manifest identity/version.'}
$levels=$doc.SelectNodes('//t:requestedExecutionLevel',$ns)
if($levels.Count -ne 1 -or $levels[0].GetAttribute('level') -ne 'asInvoker' -or $levels[0].GetAttribute('uiAccess') -ne 'false'){throw 'Elevated or ambiguous manifest privileges rejected.'}
if($doc.SelectNodes('/a:assembly/t:trustInfo/t:security/t:requestedPrivileges/t:requestedExecutionLevel',$ns).Count -ne 1){throw 'Invalid privilege structure.'}
if($doc.SelectNodes('/a:assembly/c:compatibility/c:application/c:supportedOS[@Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"]',$ns).Count -ne 1){throw 'Windows compatibility entry missing.'}
$dpi=$doc.SelectNodes('/a:assembly/t:application/t:windowsSettings/w:dpiAware',$ns)
if($dpi.Count -ne 1 -or $dpi[0].InnerText -ne 'true'){throw 'Unexpected DPI manifest.'}
Write-Output 'Application manifest verification passed.'
