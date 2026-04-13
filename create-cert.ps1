$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=QuickLink, O=skibi" -CertStoreLocation "Cert:\CurrentUser\My" -FriendlyName "QuickLink Signing Cert" -NotAfter (Get-Date).AddYears(10)
$password = ConvertTo-SecureString -String "quicklink" -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath "QuickLink.pfx" -Password $password
Write-Output "Certificate created!"
Write-Output "Thumbprint: $($cert.Thumbprint)"
