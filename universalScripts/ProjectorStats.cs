public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

bool isCrafting = false;
bool craftingFinished = false;

public void Main(string argument, UpdateType updateSource) {

    projectShip("SmallProjector", "ProjScreen1");

    // projectShip("BigProjector", "ProjScreen2");

    // projectShip("Ryujo", "[BPLCD] Wide LCD Panel 6");
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
    String[] lcdLines = new String[100];
    bool finished = false;

    
    lcdLines[0] = $"-------    {ProjectorName} STATISTICS     -------";
    if (projector.IsProjecting) {
        lcdLines[3] = $"Offset: {projector.ProjectionOffset}";
        lcdLines[4] = $"Rotation: {projector.ProjectionRotation}";
        lcdLines[5] = $"Welding Progress: [{projector.TotalBlocks - projector.RemainingBlocks} / {projector.TotalBlocks}] {getPercent(projector.TotalBlocks - projector.RemainingBlocks, projector.TotalBlocks)}%";


        var blocks = projector.RemainingBlocksPerType;
        char[] delimiters = new char[] { ',' };
        char[] remove = new char[] { '[', ']' };

        if (isCrafting) {
            lcdLines[6] = $"Crafting currently in progress";
        } else if (craftingFinished) {
            lcdLines[6] = $"All components required to finish build are in container";
        }
        int index = 0;
        int tempIndex = 0;
        String[] blockName = new string[90];
        String[] blockCount = new string[90];
        foreach (var block in blocks) {   // block count and type left to be welded
            String[] blockInfo = block.ToString().Trim(remove).Split(delimiters, StringSplitOptions.None);
            // blockInfo[0] is blueprint, blockInfo[1] is number of required item

            if (blockName.Contains(blockInfo[0].Split('_')[1].Split('/')[0])){
                tempIndex = Array.IndexOf(blockName, blockInfo[0].Split('_')[1].Split('/')[0]);
                blockCount[tempIndex] += blockInfo[1];
            } else {
                blockName[index] = blockInfo[0].Split('_')[1].Split('/')[0];
                blockCount[index] = blockInfo[1];
                index++;
            }


            
            

            // calculate amount of components required to finish the build
            // switch (blockinfo[0]) {
            //     // https://steamcommunity.com/sharedfiles/filedetails/?id=2211055171
            //     default:
            // }
        }
        index = 0;
        foreach (var name in blockName) {
            lcdLines[7+index] = $"{blockCount[index]}x {name}";
            index++;
        }

       
        // start assembling components
        if (!isCrafting) {
            // start crafting
            isCrafting = true;
            craftingFinished = false;
        }


        // https://forum.keenswh.com/threads/programmable-block-moving-inventory-items-assembler-gt-storage.7252129/
        
    } else {
        lcdLines[2] = $"Finished Current Task, No Blueprints Currently Active";
        isCrafting = false;
        craftingFinished = false;
    }

    PrintToLCD(lcdLines, display);
}