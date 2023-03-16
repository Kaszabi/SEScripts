public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateSource) {
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName("Energy") as IMyTextPanel;
    
    var battery = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(battery);
    float cur = 0;
    float max = 0;
    int funcBat = 0;
    for(int i = 0; i < battery.Count; i++){
        cur += battery[i].CurrentStoredPower;
        max += battery[i].MaxStoredPower;
        if(battery[i].IsWorking) funcBat++;
    }

    var turbine = new List<IMyPowerProducer>();
    GridTerminalSystem.GetBlocksOfType(turbine, t => t.BlockDefinition.SubtypeId.Contains("WindTurbine"));
    float turbineGen = 0;
    int funcTurbine = 0;
    foreach(IMyPowerProducer t in turbine) {
        turbineGen += t.CurrentOutput;
        if(t.IsWorking) funcTurbine++;
    }
    
    var solar = new List<IMySolarPanel>();
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solar);
    float solarGen = 0;
    int funcSolar = 0;
    foreach(IMySolarPanel s in solar) {
        solarGen += s.CurrentOutput;
        if(s.IsWorking) funcSolar++;
    }

    var generator = new List<IMyPowerProducer>();
    GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(generator);
    float gen = 0;
    int funcGen = 0;
    float totalGen = turbineGen + solarGen;
    for(int i = 0; i < generator.Count; i++){
        gen += generator[i].CurrentOutput;
        if(generator[i].IsWorking) funcGen++;
    }
    float shipsCharging = gen - totalGen;

    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(blocks);
    int funcBlocks = 0;
    foreach(IMyTerminalBlock b in blocks) {
        if(b.IsWorking) funcBlocks++;
    }

    String[] lcdLines = new String[20];
    max = max*getPart(funcBat, battery.Count);
    lcdLines[0] = "Battery:    " + cur.ToString("0.00") + " / " + max.ToString("0.00") + " MWh " + getPercent(cur, max) + "%";
    lcdLines[1] = "Functional:     [" + funcBat + "/" + battery.Count + "] " + getPercent(funcBat, battery.Count) + "%";
    lcdLines[3] = "Production:    " + totalGen.ToString("0.00") + " MW ";
    lcdLines[4] = "Functional:     [" + funcGen + "/" + generator.Count + "] " + getPercent(funcGen, generator.Count) + "%";
    lcdLines[5] = "Turbines:    " + turbineGen.ToString("0.00") + " MW ";
    lcdLines[6] = "Functional:    [" + funcTurbine + "/" + turbine.Count + "] " + getPercent(funcTurbine, turbine.Count) + "%";
    lcdLines[7] = "Solars:    " + solarGen.ToString("0.00") + " MW ";
    lcdLines[8] = "Functional:    [" + funcSolar + "/" + solar.Count + "] " + getPercent(funcSolar, solar.Count) + "%";
    lcdLines[10] = "Ships Charging Rate:    " + shipsCharging.ToString("0.00") + " MW";
    lcdLines[12] = "Total Base Integrity:    [" + funcBlocks + "/" + blocks.Count + "] " + getPercent(funcBlocks, blocks.Count) + "%";
   

    
    string lcdText = " ";
    for(int i = 0; i < lcdLines.Length; i++){
        lcdText += lcdLines[i] + "\n";
    }
    display.WriteText(lcdText);
}

public float getPart(float a, float b) {
    float perc = a/b;
    return perc;
}

public string getPercent(float a, float b) {
    float percent = a/b*100;
    return percent.ToString("0.00");
}
