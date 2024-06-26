# Anleitung

## Standards 
Zur Bedienung wird grundsätzlich nur der Nummernblock benötigt. Dieser ist per Funk (Bluetooth) verbunden oder USB-Kabel direkt angeschlossen. 
Zur leichteren Bedienung wird empfohlen die 4 wichtigen Systemtasten farbig zu markieren
- **+** --> gelb
- **-** --> blau
- **/** --> rot
- **\*** --> grün
- **Bckspc** --> rot (anstatt /)

Es wird immer erst der entsprechende Wert eingegeben, dann die Farbtaste für die Mannschaft (rot oder grün) gedrückt. 
Mit der blauen Taste wird immer der letzte Wert gelöscht.
Die gelbe Taste schließt ein Spiel ab

## Einstellungen
Damit man zu den Settings gelangt, muss in der Eingabe eine "0" stehen. Dann 5x die Enter-Taste drücken. Es erscheint die Settings-Seite.
Die Navigation erfolgt mit den Tasten 8 und 2. Die Wert-Änderung mit 4 und 6. Verlassen werden die Settings mit der *gelben* Taste.

- *Farb-Schema*: Hintergrund kann dunkel oder hell eingestellt werden
- *nächste Bahn*: Berachtet man Bahn 1 in Spielrichtung so befindet sich die nächste Bahn links oder rechts. Dies hat eine Auswirkung auf die Farben grün und rot sowie dem Netzwerkmodus
- *Spielmodus*: Training, BestOf, Turnier, Ziel
	- **Training**: Es gibt nur ein Spiel begrenzt durch die Anzahl der Kehren. Wenn das Trainingsspiel beendet ist, kann mit der gelben Taste das Spiel zurückgesetzt werden
	- **BestOf**: Es werden mehrere Spiele aufeinanderfolgend gegen die gleiche Mannschaft gespielt. Nach Eingabe der letzten Kehre wird mit der gelben Taste das Spiel beendet und das nächste Spiel wird gestartet. Der Zwischenstand wird zusätzlich angezeigt. 2 Punkte für Sieg. 1 Punkt für Unentschieden
	- **Turnier**: Es werden mehrere Spiele aufeinanderfolgend gegen Unterschiedliche Mannschaften gespielt. Nach Eingabe der letzten Kehre wird mit der gelben Taste das Spiel beendet und das nächste Spiel gestartet. 
	- **Ziel**: Es können die Werte pro Versuch eingegeben werden. Pro 6 Versuche wird eine Zwischensumme in der unteren Zeile angezeigt. Rechts wird das GesamtErgebnis dargestellt. Mit der gelben Taste werden alle Versuche gelöscht.
- *Max Punkte pro Kehre*: Maximal pro Kehre einzugebender Wert. Im Trainingsmodus 30, im Turniermodus 15 als Default. Kann aber angepasst werden.
- *Max Kehren pro Spiel*: Im Trainingsmodus 30, Im Turniermodus 6 Kehren als Standard. Kann aber auch angepasst werden.
- *Bahnnummer*: Auf welcher Bahn wird das System Eingesetzt. Wichtig bei der Netzwerkübertragung
- *Gruppe*: Bei mehreren Gruppen auf der Spielfläche kann jedes System einer Gruppe zugeordnet werden.
- *Networking*: Im Turniermodus und Zielmodus werden bei ON die Daten im Netzwerk zur Verfügung gestellt. Mit der Software [StockApp](https://github.com/Trawacho/StockAppV2/releases) können die Daten empfangen und live ausgewertet werden.

### Blaue Taste
Mit der blauen Taste kann die Eingabe oder die letzte Kehre bzw. der letzte Versuch gelöscht werden. Sollten viele Eingaben gelöscht werden (zum Beispiel nach einem Turnier), kann man die Eingabeverzögerung umgehen, in dem man eine "0" eingibt und dann die Blaue Taste nutzt. 
Die 0 in der Eingabe kann mit der blauen Taste nicht gelöscht werden. Hierzu die 0 mit einer anderen beliebigen Zahl überschreiben.

### Eingabeverzögerung
Um die Eingabe bedienerfreundlich zu gestalten, ist eine Eingabeverzögerung von ca. 1 Sekunde eingestellt. Das bedeutet dass die gleiche Taste nur einmal pro Sekunde vom System akzeptiert wird.

### Eingabewert
In den Settings kann ein maximaler Wert pro Kehre festgelegt werden. Wenn dieser mit der Eingabe überschritten wird, wird die Eingabe automatisch überschrieben. 
Im Modus "Ziel" können nur die üblichen Werte eingegeben werden. Bei den Mass-Versuchen die Werte 0,2,4,6,8,10. Bei den Schussversuchen die Werte 0,2,5,10.

### Marketing
Seit der Version *v1.3.0.0* ist es möglich, Stock***TV*** auch für Marketingzwecke zu nutzen. Dazu muss in der Eingabe der Wert "10" stehen. Danach 5x die Enter-Taste drücken.
Es erscheint das hinterlegt Bild. Sobald die **blaue** oder die **gelbe** Taste gedrückt wird, erreicht man das vorherige Menü wieder. Das Bild kann mit der Software *StockApp* angepasst werden.

