### Create a certificate for package signing
Anleitung: https://docs.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing#

Es muss ein neues Zertifikat erstellt werden. Dazu PowerShell mit Adminrechte starten

To use a certificate to sign your app package, the "Subject" in the certificate must match the "Publisher" section in your app's manifest.
For example, the "Identity" section in your app's AppxManifest.xml file should look something like this:
<Identity Name="Contoso.AssetTracker" 
    Version="1.0.0.0" 
    Publisher="CN=Contoso Software, O=Contoso Corporation, C=US"/>

The "Publisher", in this case, is "CN=Contoso Software, O=Contoso Corporation, C=US" which needs to be used for creating your certificate.

Man braucht den Publisher, so wie er in der Package.appxmanifest hinterlegt ist. Dazu die Datei als PlainText anzeigen und
in der Node Identity den Wert auslesen. (Beispiel: CN=Daniel Sturm)


1. Neues Zertifikat erstellen
    -> New-SelfSignedCertificate -Type Custom -Subject "CN=Daniel Sturm" -NotAfter (Get-Date).AddYears(20) -KeyUsage DigitalSignature -FriendlyName "Daniel Sturm" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

2. Export Zertifikat
    -> $password = ConvertTo-SecureString -String <Your Password> -Force -AsPlainText 
    -> Export-PfxCertificate -cert "Cert:\CurrentUser\My\<Certificate Thumbprint>" -FilePath <FilePath>.pfx -Password $password

In VisualStudio muss das neue Zertifikat hinterlegt werden. Dazu die Datei Package.appxmanifest öffnen und im Bereich "Packaging" das neue Zertifikat
hinterlegen.

Zum erstellen des Project dann in VisualStudio "Project" - "Publish" - "Create App packages..." wählen
    - SideLoading
    - Zertifikat überprüfen
    - Server für Zeitstempel: http://timestamp.globalsign.com/scripts/timstamp.dll
    - Bei Architectur nur "ARM" und "Release" auswählen, evtl. den Ausgabeodner anpassen.
