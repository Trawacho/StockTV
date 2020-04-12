# StockTV
Spielstand nach jeder Kehre im Stocksport Manschaftsspiel auf einem PublicTV live anzeigen


### Was wird benötigt:
 - 1x [Raspberry Pi 3]
 - 1x HDMI-Kabel
 - 2x [Kabelloser Ziffernblock]
 - 1x LCD-Display mind. 42"
 - Halterung für Ziffernblöcke (Eigenbau), Halterung für Display
### optional:
 - Kabel: https://www.amazon.de/gp/product/B0728GMRHH/ref=ppx_yo_dt_b_asin_title_o00_s00?ie=UTF8&psc=1
 - Verstärker: https://www.amazon.de/gp/product/B071WX56QF/ref=ppx_yo_dt_b_asin_title_o00_s01?ie=UTF8&psc=1

  
### Erste Schritte:
  Nach dem Zusammenbau des Raspberry PI muss auf die SD-Karte WindowsIoT installiert werden.<br>
  Tuturial: <a href="https://docs.microsoft.com/de-de/windows/iot-core/tutorials/rpi"> Link </a><br>
  Danach sollte der Pi mit WindowsIoT booten. Die Konfigutaion kann abgeschlossen werden. Am besten man nutzt dafür eine angeschlossene Tastatur und Maus.<br>
  Um die App auf dem Pi zu bekommen, braucht man VisualStudioCommunity. Hierzu die aktuelle kostenlose Version installieren [https://visualstudio.microsoft.com/de/vs/] <br>
  Das Repository downloaden und mit VisualStudio öffnen (/src/StockTV.sln)<br>
  Dieser Anleitung [https://docs.microsoft.com/de-de/windows/iot-core/develop-your-app/buildingappsforiotcore] folgen, um die App auf den PI zu installieren
  Nachdem die Software auf dem PI ist, muss die App noch als Standard eingestellt werden, damit sie bei einem neustart automatisch gestartet wird.
</p>


[Raspberry Pi 3]: <https://www.amazon.de/gp/product/B01CI5879A/ref=ppx_yo_dt_b_asin_title_o00_s01?ie=UTF8&th=1>
[Kabelloser Ziffernblock]: <https://www.amazon.de/gp/product/B00KYPJAMK/ref=ppx_yo_dt_b_asin_title_o09_s00?ie=UTF8&psc=1>
