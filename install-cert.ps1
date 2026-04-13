$password = ConvertTo-SecureString -String "quicklink" -AsPlainText -Force
$cert = Import-PfxCertificate -FilePath "QuickLink.pfx" -CertStoreLocation "Cert:\LocalMachine\Root" -Password $password
Write-Output "Certificate installed to Trusted Root Certification Authorities!"
Write-Output "Thumbprint: $($cert.Thumbprint)"

# Also install to Trusted Publishers
Import-PfxCertificate -FilePath "QuickLink.pfx" -CertStoreLocation "Cert:\LocalMachine\TrustedPublisher" -Password $password
Write-Output "Certificate installed to Trusted Publishers!"
