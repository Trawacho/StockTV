# Installation

## Mit Hilfe von IoT-Dashboard eine SD-Karte mit Win-IoT flashen. 
Hier am Besten schon den Gerätenamen festlegen. z.B.: BahnTV1. 
Ein Administratorkennwort vergeben und dieses gut merken oder notieren.

## Raspi mit geflashter SD-Karte booten
Die geflashte SD-Karte in den Raspi stecken und diesen dann booten. Beim Booten sollte der Raspi bereits per LAN-Kabel
mit dem Netzwerk verbunden sein. Kurze Zeit nach dem Booten wird im IoT-Dashboard der Raspi unter "Meine Geräte" aufgelistet.
Über die drei Punkte in der Spalte "Aktionen" öffnet sich das Context-Menü. Den Punkt "Im Geräteportal öffnen" anklicken.

## Einstellungen im Geräteportal
Hier wird das zuvor vergebene Administratorkennwort benötigt, damit man Zugriff auf die Seite erhält. Username lautet "Administrator". Bei den "Device Settings" die TimeZone entsprechend einstellen. Im Punkt Windows Update "Check for Updates" anklicken und alle angebotenen Updates installieren. 
Dies kann sehr lange dauern. Auch nach dem erforderlichen Neustart vergeht nochmal eine Weile bis der Raspi wieder wie gewohnt bootet.

## StockTV über das Geräteportal installieren
  - Im GerätePortal unter Apps - AppsManager kann die Software auf den Raspi geladen werden. Hierzu muss aber zuvor einmalig ein Zertifikat installiert werden (oder wenn das Zertifikat abgelaufen ist). Dazu wählen Sie im GerätePortal "Install Certificate". Das Zertifikat finden Sie im Verzeichnis \src\StockTV\AppPackages\StockTV_..\StockTV_*_arm.cer . Mit einem Klick auf Install wird das Zertifikat dann installiert. 
  - Anschließend "Local Storage" wählen um die erforderlichen Pakete zu installieren. Dazu wählen sie die StockTV*.appxbundle Datei aus, aktiviern die CheckBox für "Allow me to select framework packages" und klicken auf Next. Dann für jede Datei im Ordner "AppPackages\StockTV_...\Dependencies\arm\*.appx" "Datei auswählen" klicken und die Dateien hinzufügen. 
  - Erst wenn alle Dateien in der Liste angezeigt werden auf "Install" klicken und die Pakte installieren. Nach kurzer Zeit wird in der unteren Liste die App "StockTV" zusätzlich angezeigt.  
	
## StockTV als StartUp
In der Liste der Apps muss die App StockTV als StartUp aktiviert werden. Alle anderen Foreground Apps können gestoppt werden.
Die Installation ist damit abgeschlossen. Es empfiehlt sich, den Raspi neu zu starten.
StockTV sollte automatisch gestartet werden.

## Netzwerkeinstellungen
Um den vollen Funktionsumfang nutzen zu können, sollten alle StockTV-Systeme vernetzt werden. Da die WiFi-Qualität der Raspi´s nicht gut ist, sollten alle Geräte per Netzwerkkabel miteinander verbunden werden. WLAN ist nicht zu empfehlen. Verwenden sie am Besten statische IP-Adressen für die Raspi´s. Dazu im Geräteportal auf der linken Seite den Menüpunkt *Connectivity* und dann *Network* wählen. Auf der rechten Seite sind alle Netzwerkadapter aufgeführt. Bei dem Adapter mit Type Ethernet die *IPv4 Configuration* vornehmen. Jeder Raspi benötigt eine eigene IP-Adresse, die sich im selben Subnetz befindet.  
Beispiel für 3 Bahnen:  
| Raspi	| IP-Adresse | Subnet-Mask | Gateway |
|------|------|------|------|
| Bahn1: | 192.168.22.11 | 255.255.255.0 | 192.168.22.1 |
| Bahn2: | 192.168.22.12 | 255.255.255.0 | 192.168.22.1 |
| Bahn3: | 192.168.22.13 | 255.255.255.0 | 192.168.22.1 |
| BahnX: | 192.168.22.1x | 255.255.255.0 | 192.168.22.1 |
| PC mit StockApp: | 192.168.22.50 | 255.255.255.0 | 192.168.22.1 |
| Router/FritzBox | 192.168.22.1 | 255.255.255.0 |  |

*Bei verwendung der Subnet-Mask von 255.255.255.0 kann man bei den IP-Adressen jede Adresse von 192.168.22.1 bis 192.168.22.254 nutzen. Jede IP-Adresse darf nur 1x vorkommen. Die ersten drei Teile müssen identisch sein (192.168.22.xxx), der letzte Teil ist variabel. Das Gateway ist die IP-Adresse vom Router (oder Fritzbox). Sollte kein Router vorhanden sein, trotzdem ein Gateway eingeben!*

Auf keinen Fall den Raspi mit WLAN und Kabel gleichzeitig betreiben!  
Zur Verbindung aller Geräte empfiehlt sich ein Netzwerkswitch. Dieser wird am besten in der Nähe der Raspi´s installiert. Mit einem weiteren langen Netzwerkkabel kann der PC/Laptop mit dem Switch verbunden werden.

