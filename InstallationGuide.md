#Installation

- Mit Hilfe von IoT-Dashboard eine SD-Karte mit Win-IoT flashen. 
Hier am Besten schon den Gerätenamen festlegen. z.B.: BahnTV1
Ein Administratorkennwort vergen und dieses gut merken, am besten notieren

- Raspi mit geflashter SD-Karte booten
Die geflashte SD-Karte in den Raspi stecken und diesen dann booten. Beim booten sollte der Raspi bereits per LAN-Kabel
mit dem Netzwerk verbunden sein. Kurze Zeit nach dem Booten wird im IoT-Dashboard der Raspi unter "Meine Geräte" aufgelistet.
Über die drei Punkte in der Spalte "Aktionen" öffnet sich das Context-Menü. Den Punkt "Im Geräteportal öffnen" anklicken.

- Einstellungen im Geräteportal
Hier wird das zuvor vergebene Administratorkennwort benötigt, damit man Zugriff auf die Seite erhält.
Bei den "Device Settings" die TimeZone entsprechend einstellen. Im Punkt Windows Update "Check for Updates" anklicken und alle angebotenen Updates installieren. 
Dies kann sehr lange dauern. Auch nach dem erforderlichen Neustart vergeht nochmal eine Weile bis der Raspi wieder gewohnt bootet.

- StockTV installieren
Zur installation von StockTV am Raspi gibt es verschiedene Möglichkeiten
1. per VisualStudio
	Den SourceCode downloaden und das sln-File mit VisualStudio öffnen. In den Debug-Properties RemoteMaschine wählen. Dort sollte der Raspi gefunden und ausgewählt werden.
	Danach kann mit F5 der Debug vorgang gestartet werden. Es werden alle notwendigen Files auf den Raspi kopiert.
2. per GerätePortal
    Im GerätePortal unter Apps - AppsManager kann die Software auch auf den Raspi geladen werden. Hierzu muss aber zuvor ein Zertifikat installiert werden. 
	Wählen Sie im GerätePortal "Install Certificate". Das Zertifikat finden Sie im Verzeichnis \src\StockTV\AppPackages\StockTV_*\*.cer . Mit einem Klick auf Install wird das
	Zertifikat dann installiert. 
	Anschließend "Local Storage" wählen um die erforderlichen Pakete zu installieren. Dazu wählen sie die StockTV*.appxbundle Datei aus, aktiviern 
	die CheckBox für "Allow me to select framework packages" und klicken auf Next. Dann für jede Datei im Ordner "AppPackages\StockTV_*\Dependencies\arm\*.appx" "Datei auswählen" klicken
	und die Dateien hinzufügen. Erst wenn alle Dateien in der Liste angezeigt werden auf "Install" klicken und die Pakte installieren. Nach kurzer Zeit wird in der unteren Liste 
	die App "StockTV" zusätzlich angezeigt.
	
- StockTV als StartUp
In der Liste der Apps muss die App StockTV als StartUp aktiviert werden. Alle anderen Foreground Apps können gestoppt werden.
Die Installation ist damit abgeschlossen. Es empfiehlt sich, den Raspi neu zu starten.
StockTV sollte automatisch gestartet werden.
