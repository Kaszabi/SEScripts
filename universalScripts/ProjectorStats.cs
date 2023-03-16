public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateSource) {

    projectShip("SmallProjector", "ProjScreen1");

    projectShip("BigProjector", "ProjScreen2");

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

public void projectShip(String ProjectorName, String ScreenName) {
    
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName(ScreenName) as IMyTextPanel;
    IMyProjector projector = GridTerminalSystem.GetBlockWithName(ProjectorName) as IMyProjector;
    if (projector == null) {
        Echo($"Projector {ProjectorName} not found");
        return;
    }
    if (display == null) {
        Echo($"Display {ScreenName} not found");
        return;
    }
    String[] lcdLines = new String[20];
    bool finished = false;
    
    lcdLines[0] = $"------------------------------    {ProjectorName} STATISTICS     ------------------------------";
    if (projector.IsProjecting) {
        lcdLines[3] = $"Offset: {projector.ProjectionOffset}";
        lcdLines[4] = $"Rotation: {projector.ProjectionRotation}";
        lcdLines[5] = $"Welding Progress: [{projector.TotalBlocks - projector.RemainingBlocks} / {projector.TotalBlocks}] {getPercent(projector.TotalBlocks - projector.RemainingBlocks, projector.TotalBlocks)}%";

        var blocks = projector.RemainingBlocksPerType;
        char[] delimiters = new char[] { ',' };
        char[] remove = new char[] { '[', ']' };

        Echo(blocks);

        foreach (var item in blocks) {   // block count and type left to be welded
            string[] blockInfo = item.ToString().Trim(remove).Split(delimiters, StringSplitOptions.None);
            // blockInfo[0] is blueprint, blockInfo[1] is number of required item
            lcdLines[6] = $"{blockinfo[1]}x{blockinfo[0]}";

            // calculate amount of components required to finish the build
            switch (blockinfo[0]) {
                // https://steamcommunity.com/sharedfiles/filedetails/?id=2211055171
                default:
            }
        }

       



        // https://forum.keenswh.com/threads/programmable-block-moving-inventory-items-assembler-gt-storage.7252129/
        
    } else {
        lcdLines[2] = $"Finished Current Task, No Blueprints Currently Active";
    }

    PrintToLCD(lcdLines, display);
}