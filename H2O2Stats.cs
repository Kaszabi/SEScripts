public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

float capacity = 0;
double filled = 0;
double prevFilled = 0;
int untilFull = 0;
int untilEmpty = 0;

float capacityO = 0;
double filledO = 0;

public void Main(string argument, UpdateType updateSource) {
    IMyTextPanel displayH = GridTerminalSystem.GetBlockWithName("Hydrogen Stats") as IMyTextPanel;
    String[] lcdLinesH = new String[20];
    IMyTextPanel displayO = GridTerminalSystem.GetBlockWithName("Oxygen Stats") as IMyTextPanel;
    String[] lcdLinesO = new String[20];

    var hydrogen = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(hydrogen, t => t.BlockDefinition.SubtypeId.Contains("HydrogenTank"));
    


    lcdLinesH[0] = $"------------------------------     Hydrogen Levels     ------------------------------";

    lcdLinesH[2] = $"Hydrogen tanks: {hydrogen.Count}";

    for (int i = 0; i < hydrogen.Count; i++) {
        capacity += hydrogen[i].Capacity;
        filled += hydrogen[i].FilledRatio*15000000;
    }

    lcdLinesH[4] = $"Total hydrogen: {filled.ToString("0")} / {capacity.ToString("0")}";
    lcdLinesH[5] = $"Total filled: {getPercent((float)filled, capacity)}%";

    untilFull = (int)((capacity - filled) / ((filled - prevFilled) * 0.6));
    untilEmpty = (int)(filled / ((prevFilled - filled) * 0.6));

    if(prevFilled > filled){
        lcdLinesH[6] = $"Hydrogen levels decreasing by: {((int)prevFilled - (int)filled)*0.6} / s";
        if(untilEmpty < 60){
            lcdLinesH[7] = $"Hydrogen tanks will be empty in: {untilEmpty} s";
        }else if(untilEmpty < 3600){
            lcdLinesH[7] = $"Hydrogen tanks will be empty in: {untilEmpty/60} m {untilEmpty%60} s";
        }else{
            lcdLinesH[7] = $"Hydrogen tanks will be empty in: {untilEmpty/3600} h {untilEmpty%3600/60} m {untilEmpty%60} s";
        }
    } else if(prevFilled < filled){
        lcdLinesH[6] = $"Hydrogen levels increasing by: {((int)filled - (int)prevFilled)*0.6} / s";
        if(untilFull < 60){
            lcdLinesH[7] = $"Hydrogen tanks will be full in: {untilFull} s";
        }else if(untilFull < 3600){
            lcdLinesH[7] = $"Hydrogen tanks will be full in: {untilFull/60} m {untilFull%60} s";
        }else{
            lcdLinesH[7] = $"Hydrogen tanks will be full in: {untilFull/3600} h {untilFull%3600/60} m {untilFull%60} s";
        }
        
    } else {
        lcdLinesH[6] = $"Hydrogen levels unchanged in the last 1s";
    }

    prevFilled = filled;

    PrintToLCD(lcdLinesH, displayH);




    var oxygen = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(oxygen, t => t.BlockDefinition.SubtypeId.Contains("OxygenTank"));

    lcdLinesO[0] = $"------------------------------     Oxygen Levels     ------------------------------";

    lcdLinesO[2] = $"Oxygen tanks: {oxygen.Count}";
    capacity = 0;
    filled = 0;

    for (int i = 0; i < oxygen.Count; i++) {
        capacityO += oxygen[i].Capacity;
        filledO += oxygen[i].FilledRatio;
    }

    lcdLinesO[4] = $"Total oxygen: {filledO.ToString("0.00")} / {capacityO.ToString("0.00")}";
    lcdLinesO[5] = $"Total filled: {getPercent((float)filledO, capacityO)} ";

    PrintToLCD(lcdLinesO, displayO);
}

public void PrintToLCD(String[] lcdLines, IMyTextPanel display){
    string lcdText = " ";
    for(int i = 0; i < lcdLines.Length; i++){
        lcdText += lcdLines[i] + "\n";
    }
    display.WriteText(lcdText);
}

public string getPercent(float a, float b) {
    float percent = a/b*100;
    return percent.ToString("0.00");
}