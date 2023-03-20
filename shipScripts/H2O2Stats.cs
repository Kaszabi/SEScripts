public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}



double[] prevFilled = {0};

public void Main(string argument, UpdateType updateSource) {
    getTankStats("HydrogenScreen", "Hydrogen");
    //getTankStats("OxygenScreen", "Oxygen");
}

public void PrintToLCD(String[] lcdLines, IMyTextPanel display){
    string lcdText = " ";
    for(int i = 0; i < lcdLines.Length; i++){
        lcdText += lcdLines[i] + "\n";
    }
    display.WriteText(lcdText);
}

public float getPercent(float a, float b) {
    float percent = a/b*100;
    return percent;
}

public void getTankStats(String displayName, String type) {
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName(displayName) as IMyTextPanel;
    var tank = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(tank, t => t.BlockDefinition.SubtypeId.Contains($"{type}Tank"));
    String[] lcdLines = new String[20];
    float capacity = 0;
    double filled = 0;
    int index = 0;

    switch (type) {
        case "Hydrogen":
            index = 0;
            break;
        case "Oxygen":
            index = 1;
            break;
    }

    lcdLines[0] = $"------------------------------     {type} Levels     ------------------------------";

    lcdLines[2] = $"{type} tanks: {tank.Count}";

    foreach (IMyGasTank t in tank) {
        capacity += t.Capacity;
        filled += t.FilledRatio*15000000;
    }

    lcdLines[4] = $"Total {type}: {filled.ToString("0")} / {capacity.ToString("0")}";
    float percent = getPercent((float)filled, capacity);
    int barWidth = 100;
    int filledBar = (int)(percent/100*barWidth);
    int emptyBar = barWidth - filledBar;
    String bar = "";
    for (int i = 0; i < filledBar; i++) {
        bar += "|";
    }
    for (int i = 0; i < emptyBar; i++) {
        bar += ".";
    }
    lcdLines[5] = $"[{bar}] {getPercent((float)filled, capacity).ToString("0.00")}%"; // % bar

    int untilFull = (int)((capacity - filled) / ((filled - prevFilled[index]) * 0.6));
    int untilEmpty = (int)(filled / ((prevFilled[index] - filled) * 0.6));

    if (prevFilled[index] > filled) {
        lcdLines[6] = $"{type} levels decreasing by: {((int)prevFilled[index] - (int)filled)*0.6} / s";
        if (untilEmpty < 60) {
            lcdLines[7] = $"{type} tanks will be empty in: {untilEmpty} s";
        } else if (untilEmpty < 3600) {
            lcdLines[7] = $"{type} tanks will be empty in: {untilEmpty/60} m {untilEmpty%60} s";
        } else {
            lcdLines[7] = $"{type} tanks will be empty in: {untilEmpty/3600} h {untilEmpty%3600/60} m {untilEmpty%60} s";
        }
    } else if (prevFilled[index] < filled) {
        lcdLines[6] = $"{type} levels increasing by: {((int)filled - (int)prevFilled[index])*0.6} / s";
        if (untilFull < 60) {
            lcdLines[7] = $"{type} tanks will be full in: {untilFull} s";
        } else if (untilFull < 3600) {
            lcdLines[7] = $"{type} tanks will be full in: {untilFull/60} m {untilFull%60} s";
        } else {
            lcdLines[7] = $"{type} tanks will be full in: {untilFull/3600} h {untilFull%3600/60} m {untilFull%60} s";
        }
        
    } else {
        lcdLines[6] = $"{type} levels unchanged in the last 1s";
    }

    prevFilled[index] = filled;

    PrintToLCD(lcdLines, display);

}