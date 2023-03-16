public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateSource) {

    projectShip("SmallProjector#BigProjector", "ProjScreen");

    projectShip("Ryujo", "[BPLCD] Wide LCD Panel 6");
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

public void projectShip(String ProjectorName, String ScreenName){
    
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName(ScreenName) as IMyTextPanel;
    String[] lcdLines = new String[20];
    String[] split = ProjectorName.Split('#');
    int shift = 0;
    bool finished = false;


    foreach (String s in split) {
        IMyProjector projector = GridTerminalSystem.GetBlockWithName(s) as IMyProjector;
        lcdLines[0+shift] = $"------------------------------    {s} STATISTICS     ------------------------------";
        if(projector.IsProjecting){
            lcdLines[3+shift] = $"Offset: {projector.ProjectionOffset}";
            lcdLines[4+shift] = $"Rotation: {projector.ProjectionRotation}";
            lcdLines[5+shift] = $"Welding Progress: [{projector.TotalBlocks - projector.RemainingBlocks} / {projector.TotalBlocks}] {getPercent(projector.TotalBlocks - projector.RemainingBlocks, projector.TotalBlocks)}%";
        }

        if(projector.IsProjecting){
            finished = false;
        }else{
            finished = true;
        }

        if(finished){
            lcdLines[2+shift] = $"Finished Current Task, No Blueprints Currently Active";
        }

        shift = 8;
    }

    PrintToLCD(lcdLines, display);
}