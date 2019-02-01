Antwoord op vraag 2: We hebben gebruik gemaakt van de aanpak die in de fractal framework werd 
gebruikt, waarbij de cellen per 32 in een uint worden opgeslagen en gelezen door middel van 
bit shifting. Dit heeft als voordeel dat het zoals gezegd een stuk geheugen efficienter is dan 
bvb 1 byte per cel, want dan heb je acht keer zoveel bytes nodig dan het aantal cellen en aangezien 
er enorm veel cellen kunnen zijn loopt dat snel uit de hand. Een aanpak met 1 byte per cel heeft 
wel als voordeel dat er voor elke write die een kernel doet een aparte byte is, en er dus niet 
meerdere kernels naar dezelfde uint willen schrijven. Dit probleem lossen we op door atomic_or 
te gebruiken bij het schrijven. 