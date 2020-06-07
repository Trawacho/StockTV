#Installation

- Mit Hilfe von IoT-Dashboard eine SD-Karte mit Win-IoT flashen. 
Hier am Besten schon den Ger�tenamen festlegen. z.B.: BahnTV1
Ein Administratorkennwort vergen und dieses gut merken, am besten notieren

- Raspi mit geflashter SD-Karte booten
Die geflashte SD-Karte in den Raspi stecken und diesen dann booten. Beim booten sollte der Raspi bereits per LAN-Kabel
mit dem Netzwerk verbunden sein. Kurze Zeit nach dem Booten wird im IoT-Dashboard der Raspi unter "Meine Ger�te" aufgelistet.
�ber die drei Punkte in der Spalte "Aktionen" �ffnet sich das Context-Men�. Den Punkt "Im Ger�teportal �ffnen" anklicken.

- Einstellungen im Ger�teportal
Hier wird das zuvor vergebene Administratorkennwort ben�tigt, damit man Zugriff auf die Seite erh�lt.
Bei den "Device Settings" die TimeZone entsprechend einstellen. Im Punkt Windows Update "Check for Updates" anklicken und alle angebotenen Updates installieren. 
Dies kann sehr lange dauern. Auch nach dem erforderlichen Neustart vergeht nochmal eine Weile bis der Raspi wieder gewohnt bootet.

- StockTV installieren
Zur installation von StockTV am Raspi gibt es verschiedene M�glichkeiten
1. per VisualStudio
	Den SourceCode downloaden und das sln-File mit VisualStudio �ffnen. In den Debug-Properties RemoteMaschine w�hlen. Dort sollte der Raspi gefunden und ausgew�hlt werden.
	Danach kann mit F5 der Debug vorgang gestartet werden. Es werden alle notwendigen Files auf den Raspi kopiert.
2. per Ger�tePortal
    Im Ger�tePortal unter Apps - AppsManager kann die Software auch auf den Raspi geladen werden. Hierzu muss aber zuvor ein Zertifikat installiert werden. 
	W�hlen Sie im Ger�tePortal "Install Certificate". Das Zertifikat finden Sie im Verzeichnis \src\StockTV\AppPackages\StockTV_*\*.cer . Mit einem Klick auf Install wird das
	Zertifikat dann installiert. 
	Anschlie�end "Local Storage" w�hlen um die erforderlichen Pakete zu installieren. Dazu w�hlen sie die StockTV*.appxbundle Datei aus, aktiviern 
	die CheckBox f�r "Allow me to select framework packages" und klicken auf Next. Dann f�r jede Datei im Ordner "AppPackages\StockTV_*\Dependencies\arm\*.appx" "Datei ausw�hlen" klicken
	und die Dateien hinzuf�gen. Erst wenn alle Dateien in der Liste angezeigt werden auf "Install" klicken und die Pakte installieren. Nach kurzer Zeit wird in der unteren Liste 
	die App "StockTV" zus�tzlich angezeigt.
	
- StockTV als StartUp
In der Liste der Apps muss die App StockTV als StartUp aktiviert werden. Alle anderen Foreground Apps k�nnen gestoppt werden.
Die Installation ist damit abgeschlossen. Es empfiehlt sich, den Raspi neu zu starten.
StockTV sollte automatisch gestartet werden.
