![StockTVBanner](/Bilder/StockTV.jpg)

# Neue Version

Seit der Version v1.7.0 ist die Basis des Programms von UWP auf BLAZOR geändert worden. Grund hierfür ist, dass Microsoft das Windows IoT Core nicht mehr unterstützt. Somit musste das Programm auf eine neue Umgebung umgestellt werden. Mit der neuen Version, sind nun Installation auf Windows und Linux Plattformen möglich. Das Programm basiert nun auf einem Webdienst und einer browserbasierten Steuerung. Die Anzeige und Bedienung bleibt aber identisch und wurde nicht geändert.

# StockTV - Die Anzeige für den Stocksport

Der Zuschauer kann auf jeder Bahn den aktuellen Spielstand erkennen. Ein Mehrwert für Verein und Zuschauer. 
Ein Spiel genau beobachten und trotzdem alle Spiele im Blick haben. Auch für die Schützen wird es interessanter. Sie sehen nicht nur den eigenen Spielstand sondern auch die Spielstände auf den anderen Bahnen, was Turniere nochmal interessanter macht. Die Bedienung ist denkbar einfach und für jedes Alter geeignet (ältester bisher bekannter Schütze und Bediener des Systems ist über 82 Jahre alt).

## ...für die typischen Wettbewerbe

Es sind momentan vier verschiedene Modis möglich. Neben dem Trainingsmodus kann man auch Turniere, BestOf-Begegnungen oder das Zielschiessen abbilden.
Die Anzeige ist auf jeden Modus individuell angepasst.

Training|Turnier|BestOf|Ziel
--------|-------|-------|-----
<img src="Bilder/Training.png" width="225" />|<img src="Bilder/Turnier.png" width="225" />|<img src="Bilder/BestOf.png" width="225" />|<img src="Bilder/Ziel.png" width="225" />|

### Was wird benötigt (pro Spielbahn):

- 1x Raspberry 3 oder neuer (alternativ ein Windows oder Linux-PC)
- 1x HDMI-Kabel für die Verbindung zwischen Raspi und TV
- 2x [Kabelloser Ziffernblock]
- 1x LCD-Display 55". (kleiner geht, aber nicht zu empfehlen!!)
- Halterung für Ziffernblöcke (Eigenbau), Halterung für Display
- 1x aktive USB-Verlängerung (z. B.: DeLock 83453), wird benötigt, um den Abstand zw. Funkempfänger und Tastatur auf der Raspi abgewandten Seite zu verringern. Alternativ kann auch LogiLink UA0021D - USB 2.0 Extender eingesetzt werden.
- als Alternative für die Ziffernblöcke ist die Eingabe auch über Tablets möglich

### Was sollte vorhanden sein:

Du solltest im Verein jemanden haben, der von einem Computer keine Angst hat und mit einer SD-Karte umgehen kann. Neben der Montage braucht man für die Ersteinrichtung etwas Zeit. 
Wenn die erste Bahn erledigt ist, hat man es schon fast geschafft. Man hat die Möglichkeit das System zu Hause am Computermonitor oder TV zu testen. Für unter 100€ ist ein Testen zu Hause möglich,
ohne ein Risiko einzugehen.

### Installation

Die Installation auf dem Raspberry Pi ist entweder über ein fertiges, sofort einsatzbereites Image oder per Install-Script auf einem bestehenden Raspberry Pi OS möglich. Alternativ kann auch ein "normaler" PC mit Windows oder Linux genutzt werden. Eine Schritt-für-Schritt-Anleitung findest du in der [INSTALL.md].

### Konzept

Man benötigt für jede Bahn einen PC, auf dem StockTV installiert wird. Die Bedienung erfolgt über 2 Ziffernblöcke. Pro Zielfeld einer Bahn ein Ziffernblock. Somit kann nach jeder Kehre sofort das Ergebnis eingegeben werden. Die Ziffernblöcke sind kabellos per Dongle mit dem Rechner verbunden. Um den Abstand zu verkürzen, sollte man eine USB-Verlängerung nutzen, um den Abstand von USB-Dongle und Ziffernblock zu verkürzen (max 10m). Am Rechner wird das große Display (TV) angeschlossen. Wenn man StockTV in Verbindung mit StockAPP nutzen möchte, müssen alle Systeme vernetzt werden. Hierzu wird dringend von WLAN abgeraten da die Rechner, vor allem Raspberry, ein schlechtes WLAN haben; es empfiehlt sich alles zu verkabeln. Ab der Version v1.7.0 ist es auch möglich, die Eingabe der Spielstände per Tablet zu realisieren. Dazu empfiehlt es sich, eine KIOSK-App zu nutzen.  

*Exemplarischer Aufbau:*
<img src="Bilder/StockTV_Aufbau_1.png">

### Bilder

Ziffernblock mit Aufklebersatz zur besseren Lesbarkeit. (Aufklebersatz auf Anfrage)  
<img src="Bilder/Keypad.jpg" width="150"/> 

#### Installationen
 - ESF Hankofen (4 Bahnen)<br>
<img src="Bilder/ESF1.jpg" width="300" /> <img src="Bilder/ESF2.jpg" width="300" /> <img src="Bilder/ESF4.jpg" width="300" />

 - TV Geiselhöring (3 Bahnen)<br>
<img src="Bilder/TVG1.jpg" width="300" /> <img src="Bilder/TVG2.jpg" width="300" /> <img src="Bilder/TVG3.jpg" width="300" />

 - EC EBRA Aiterhofen (3 Bahnen)<br>
   <img src="Bilder/EC_EBRA_Aiterhofen.jpg" width="600"/>

 - EC Straßkirchen (8 Bahnen)<br>
<img src="Bilder/ECS1.jpg" width="450"/> <img src="Bilder/ECS2.jpg" width="450"/>

- SC Schönach (2 von 4 Bahnen)<br>
<img src="Bilder/SCS2.jpeg" width="300"/> <img src="Bilder/SCS3.jpeg" width="300"/> <img src="Bilder/SCS1.jpeg" width="300"/>

- EC Griesbach (3 Bahnen)<br>
<img src="Bilder/ECGB_1.png" width="450"/> <img src="Bilder/ECGB_2.png" width="450"/> 
<img src="Bilder/ECGB_3.png" width="450"/> <img src="Bilder/ECGB_4.png" width="450"/>

- ETSV Hainsbach (5 Bahnen)  
<img src="Bilder/ETSVH1.jpg"  Height="300"/> <img src="Bilder/ETSVH2.jpg"  Height="300"/> 

- EC Wegscheid (4 Bahnen)  
<img src="Bilder/ECWegscheid1.jpg" Width="450"/> <img src="Bilder/ECWegscheid2.jpg" Width="450"/>  
<img src="Bilder/ECWegscheid4.jpg" Height="400"/>  <img src="Bilder/ECWegscheid5.jpg" Height="400"/>  

- EC Perkam (4 Bahnen im Freien)  
<img src="Bilder/ECP1.jpg" Width="300"/> <img src="Bilder/ECP2.jpeg" Width="300"/>

- FC Unteriglbach  
  <img src="Bilder/FCUnteriglbach.jpg" Height="450"/> 

- EC Sassbach   
<img src="Bilder/ECSassbach1.jpg" Width="300"/> <img src="Bilder/ECSassbach2.jpg" Width="300"/> <img src="Bilder/ECSassbach3.jpg" Width="300"/>

- EC Pleinting  
<img src="Bilder/ECPleinting1.jpg" Width="300"/> <img src="Bilder/ECPleinting2.jpg" Width="300"/>

- EC Rettenbach (4 Bahnen / StockTV mit x86 PCs im Kiosk-Mode)  
<img src="Bilder/ECRettenbach1.jpg" Width="300"/> <img src="Bilder/ECRettenbach3.jpg" Width="300"/> <img src="Bilder/ECRettenbach4.jpg" Width="300"/>

- EC Eintracht Bodenmais  
  <img src="Bilder/EintrachtBodenmais.jpg" Width="300"/>  

- SV Längenfeld (Tirol / Östereich)<br>
  <img src="Bilder/SV-Laengenfeld_Tirol_AT.jpg" Width="300"/>

- Sportunion Keferfeld-Oed (Österreich)<br>
  <img src="Bilder/SportunionKeferfeldOed.jpg" Width="300"/>

- Union Stocksport Putzleinsdorf ( Österreich )  
<img src="Bilder/Putzleinsdorf01.JPG" Width="450"> <img src="Bilder/Putzleinsdorf04.JPG" Width="450">  
<img src="Bilder/Putzleinsdorf03.JPG" Width="300"> <img src="Bilder/Putzleinsdorf02.JPG" Width="300">


Viele weitere Installationen sind bereits umgesetzt bzw sind in Planung.

### Unterstützung
Die Software steht kostenlos zur Verfügung. Im Gegenzug wäre eine Unterstützung des Projekts wünschenswert. Sowohl im Coding als auch bei der Suche nach alternativer Hardware oder beim Testen der Software. Ideen oder Verbesserungsvorschläge sind willkommene Beiträge.  
In diesem Zug einen herzlichen Dank an Michael für seine aktive Unterstützung. Durch sein strukturiertes Testen, seine Ideen sowie seine konstruktiven Kritiken konnte das Projekt nochmal einen Schritt nach vorne machen. Danke Mich!  
Dank auch an [IGEL Design] für die Erstellung der Banner!  
Bei weiteren Fragen nutzen Sie bitte den Bereich [Discussions] oder [Issues]. Alternativ auch per Mail an *stocktv at gmx.de*

[INSTALL.md]: <INSTALL.md>
[Kabelloser Ziffernblock]: <https://www.amazon.de/gp/product/B00KYPJAMK/ref=ppx_yo_dt_b_asin_title_o09_s00?ie=UTF8&psc=1>
[IGEL Design]: <https://webdesign.igel-web.de/>
[Discussions]: <https://github.com/Trawacho/StockTV/discussions>
[Issues]: <https://github.com/Trawacho/StockTV/issues>
