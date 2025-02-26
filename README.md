![StockTVBanner](/Bilder/StockTV.jpg)

# StockTV - Die Anzeige für den Stocksport
Der Zuschauer kann auf jeder Bahn den aktuellen Spielstand erkennen. Ein Mehrwert für Verein und Zuschauer. 
Ein Spiel genau beobachten und trotzdem alle Spiele im Blick haben. Auch für die Schützen wird es interessanter. Sie sehen nicht nur den eigenen Spielstand sondern auch die Spielstände auf den anderen Bahnen, was Turniere nochmal 
interessanter macht. Die Bedienung ist denkbar einfach und für jedes Alter geeignet (ältester bisher bekannter Schütze und Bediener des Systems ist 82 Jahre alt).

### ..für die typischen Wettbewerbe
Es sind momentan vier verschiedene Modis möglich. Neben dem Trainingsmodus kann man auch Turniere, BestOf-Begegnungen oder das Zielschiessen abbilden.
Die Anzeige ist auf jeden Modus individuell angepasst.

Training|Turnier|BestOf|Ziel
--------|-------|-------|-----
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Training.png" width="225" />|<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Turnier.png" width="225" />|<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/BestOf.png" width="225" />|<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Ziel.png" width="225" />|

### Was wird benötigt (pro Spielbahn):
 - 1x [Raspberry Pi 3] (alternativ ein Windows-PC)
 - 1x HDMI-Kabel für die Verbindung zwischen Raspi und TV
 - 2x [Kabelloser Ziffernblock]
 - 1x LCD-Display 55". (kleiner geht, aber nicht zu empfehlen!!)
 - Halterung für Ziffernblöcke (Eigenbau), Halterung für Display
 - 1x aktive USB-Verlängerung (z. B.: DeLock 83453), wird benötigt, um den Abstand zw. Funkempfänger und Tastatur auf der Raspi abgewandten Seite zu verringern. Alternativ kann auch LogiLink UA0021D - USB 2.0 Extender eingesetzt werden.
#### !!! Info bzgl Raspberry !!!
Für die Installation mit einem Raspberry ist das IoT-Dashobard von Microsoft notwendig. Das Produkt wurde eingestellt und ist nicht mehr offiziell verfügbar.  
Daher empfehle ich aktuell den Einsatz von Windows-PCs. Hier gibt es verschiedene Anbieter von Refurbished-Geräten, bei denen man Windows-PCs unter 100€ erwerben kann.  
Die StockTV-App wird dann im KIOSK-Modus unter Windows 10/11 eingesetzt.  
Wichtig wäre, dass sich der PC automatisch einschaltet, sobald er mit Strom versorgt wird!

### Was sollte vorhanden sein:
Du solltest im Verein jemanden haben, der von einem Computer keine Angst hat und mit einer SD-Karte umgehen kann. Neben der Montage braucht man für die Ersteinrichtung etwas Zeit. 
Wenn die erste Bahn erledigt ist, hat man es schon fast geschafft. Man hat die Möglichkeit das System zu Hause am Computermonitor oder TV zu testen. Für unter 100€ ist ein testen zu Hause möglich,
ohne ein Risiko einzugehen.

### Bilder
Ziffernblock mit Aufklebersatz zur besseren Lesbarkeit. (Aufklebersatz auf Anfrage)  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Keypad.jpg" width="150"/> 

#### Installationen
 - ESF Hankofen (4 Bahnen)<br>
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ESF1.jpg" width="300" /> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ESF2.jpg" width="300" /> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ESF4.jpg" width="300" />

 - TV Geiselhöring (3 Bahnen)<br>
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/TVG1.jpg" width="300" /> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/TVG2.jpg" width="300" /> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/TVG3.jpg" width="300" />

 - EC EBRA Aiterhofen (3 Bahnen)<br>
   <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/EC_EBRA_Aiterhofen.jpg" width="600"/>

 - EC Straßkirchen (8 Bahnen)<br>
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECS1.jpg" width="450"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECS2.jpg" width="450"/>

- SC Schönach (2 von 4 Bahnen)<br>
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/SCS2.jpeg" width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/SCS3.jpeg" width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/SCS1.jpeg" width="300"/>

- EC Griesbach (3 Bahnen)<br>
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECGB_1.png" width="450"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECGB_2.png" width="450"/> 
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECGB_3.png" width="450"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECGB_4.png" width="450"/>

- ETSV Hainsbach (5 Bahnen)  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ETSVH1.jpg"  Height="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ETSVH2.jpg"  Height="300"/> 

- EC Wegscheid (4 Bahnen)  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECWegscheid1.jpg" Width="450"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECWegscheid2.jpg" Width="450"/>  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECWegscheid4.jpg" Height="400"/>  <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECWegscheid5.jpg" Height="400"/>  

- EC Perkam (4 Bahnen im Freien)  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECP1.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECP2.jpeg" Width="300"/>

- FC Unteriglbach  
  <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/FCUnteriglbach.jpg" Height="450"/> 

- EC Sassbach   
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECSassbach1.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECSassbach2.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECSassbach3.jpg" Width="300"/>

- EC Pleinting  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECPleinting1.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECPleinting2.jpg" Width="300"/>

- EC Rettenbach (4 Bahnen / StockTV mit x86 PCs im Kiosk-Mode)  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECRettenbach1.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECRettenbach3.jpg" Width="300"/> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/ECRettenbach4.jpg" Width="300"/>

- SV Längenfeld (Tirol / Östereich)<br>
  <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/SV-Laengenfeld_Tirol_AT.jpg" Width="300"/>

- Sportunion Keferfeld-Oed (Österreich)<br>
  <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/SportunionKeferfeldOed.jpg" Width="300"/>

- Union Stocksport Putzleinsdorf ( Österreich )  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Putzleinsdorf01.JPG" Width="450"> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Putzleinsdorf04.JPG" Width="450">  
<img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Putzleinsdorf03.JPG" Width="300"> <img src="https://github.com/Trawacho/StockTV/blob/master/Bilder/Putzleinsdorf02.JPG" Width="300">


Viele weitere Installationen sind bereits umgesetzt bzw sind in Planung.

### Unterstützung
Die Software steht kostenlos zur Verfügung. Im Gegenzug wäre eine Unterstützung des Projekts wünschenswert. Sowohl im Coding als auch bei der Suche nach alternativer Hardware oder beim Testen der Software. Ideen oder Verbesserungsvorschläge sind willkommene Beiträge.  
In diesem Zug einen herzlichen Dank an Michael für seine aktive Unterstützung. Durch sein strukturiertes Testen, seine Ideen sowie seine konstruktiven Kritiken konnte das Projekt nochmal einen Schritt nach vorne machen. Danke Mich!  
Dank auch an [IGEL Design] für die Erstellung der Banner!  
Bei weiteren Fragen nutzen Sie bitte den Bereich [Discussions] oder [Issues]. Alternativ auch per Mail an *stocktv at gmx.de*

[Raspberry Pi 3]: <https://www.amazon.de/gp/product/B01CI5879A/ref=ppx_yo_dt_b_asin_title_o00_s01?ie=UTF8&th=1>
[Kabelloser Ziffernblock]: <https://www.amazon.de/gp/product/B00KYPJAMK/ref=ppx_yo_dt_b_asin_title_o09_s00?ie=UTF8&psc=1>
[IGEL Design]: <https://webdesign.igel-web.de/>
[Discussions]: <https://github.com/Trawacho/StockTV/discussions>
[Issues]: <https://github.com/Trawacho/StockTV/issues>
