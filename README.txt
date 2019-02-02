Antwoord op vraag 2:
We hebben gebruik gemaakt van de aanpak die in de fractal framework werd 
gebruikt, waarbij de cellen per 32 in een uint worden opgeslagen en gelezen door middel van 
bit shifting. Dit heeft als voordeel dat het zoals gezegd een stuk geheugen efficienter is dan 
bijvoorbeeld 1 byte per cel, want dan heb je acht keer zoveel bytes nodig dan het aantal cellen en 
aangezien er enorm veel cellen kunnen zijn loopt dat snel uit de hand. Een aanpak met 1 byte per 
cel heeft wel als voordeel dat er voor elke write die een kernel doet een aparte byte is, en er 
dus niet meerdere kernels naar dezelfde uint willen schrijven. Dit probleem lossen we op door 
atomic_or te gebruiken bij het schrijven.
Deze methode is behoorlijk memory efficient, maar niet perfect. Een unsigned integer heeft immers
altijd 32 bits. Dit betekent dat we ongebruikte bits overhouden aan de rechterkant van het geheel 
als onze Game of Life-savefile een breedte heeft die niet deelbaar is door 32. Om precies te zijn
hebben we te maken met (b % 32) * h ongebruikte bits, waarbij b de breedte en h de hoogte van het
bestand is.

We hebben punten 1, 2, 3 en 5 uit de opdracht geimplementeerd. In en uit zoomen gaat met de omhoog
en omlaag pijltjestoetsen.