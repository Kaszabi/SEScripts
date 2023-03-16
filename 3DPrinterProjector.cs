public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

bool error = false;
bool finished = false;
IMyProjector projector;

public void Main(string argument, UpdateType updateSource) {
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName("Printer - ProjScreen") as IMyTextPanel;
    String[] lcdLines = new String[20];

    IMyProjector smalProj = GridTerminalSystem.GetBlockWithName("Printer - SmallProjector") as IMyProjector;
    IMyProjector bigProj = GridTerminalSystem.GetBlockWithName("Printer - BigProjector") as IMyProjector;


    if(smalProj.IsProjecting && !bigProj.IsProjecting){
        projector = GridTerminalSystem.GetBlockWithName("Printer - SmallProjector") as IMyProjector;
        error = false;
        finished = false;
    }else if(!smalProj.IsProjecting && bigProj.IsProjecting){
        projector = GridTerminalSystem.GetBlockWithName("Printer - BigProjector") as IMyProjector;
        error = false;
        finished = false;
    }else if(smalProj.IsProjecting && bigProj.IsProjecting){
        lcdLines[3] = $"ERROR: Multiple Projections Active";
        error = true;
        finished = false;
    }else{
        finished = true;
        error = false;
    }

    if(finished){
        lcdLines[2] = $"Finished Current Task, No Blueprints Currently Active";
    }else if(error){
        lcdLines[2] = $"ERROR Occured";
    }


    if(smalProj.IsProjecting && !bigProj.IsProjecting){

        lcdLines[2] = $"Small Projection Is Currently Active";

    }
    
    
    if(smalProj.IsProjecting || bigProj.IsProjecting){
        lcdLines[3] = $"Offset: {projector.ProjectionOffset}";
        lcdLines[4] = $"Rotation: {projector.ProjectionRotation}";
        lcdLines[5] = $"Welding Progress: [{projector.TotalBlocks - projector.RemainingBlocks} / {projector.TotalBlocks}] {getPercent(projector.TotalBlocks - projector.RemainingBlocks, projector.TotalBlocks)}%";
    }


    lcdLines[0] = $"------------------------------     Blueprint STATISTICS     ------------------------------";
    

    PrintToLCD(lcdLines, display);
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