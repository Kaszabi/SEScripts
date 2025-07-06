public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateSource) {
    // ====================================================================
    // ===                      LCD Panel Name                          ===
    // ====================================================================
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName("Base") as IMyTextPanel;
    // ====================================================================


    // Get battery status
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);

    float charging = 0;
    float discharging = 0;
    
    foreach (IMyBatteryBlock batt in batteries)
    {
        charging += batt.CurrentInput;
        discharging += batt.CurrentOutput;
    }

    var battery = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(battery);
    
    float cur = 0;
    float max = 0;
    int funcBat = 0;
    
    for(int i = 0; i < battery.Count; i++){
        if(battery[i].IsWorking) {
            cur += battery[i].CurrentStoredPower;
            max += battery[i].MaxStoredPower;
            funcBat++;
        }
    }

    // Wind Turbines
    var turbine = new List<IMyPowerProducer>();
    GridTerminalSystem.GetBlocksOfType(turbine, t => t.BlockDefinition.SubtypeId.Contains("WindTurbine"));
    
    float turbineGen = 0;
    int funcTurbine = 0;
    
    foreach(IMyPowerProducer t in turbine) {
        turbineGen += t.CurrentOutput;
        if(t.IsWorking) funcTurbine++;
    }

    // Solar Panels
    var solar = new List<IMySolarPanel>();
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solar);
    
    float solarGen = 0;
    int funcSolar = 0;
    
    foreach(IMySolarPanel s in solar) {
        solarGen += s.CurrentOutput;
        if(s.IsWorking) funcSolar++;
    }

    // Reactors
    var reactor = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactor);
    
    float reactorGen = 0;
    int funcReactor = 0;
    
    foreach(IMyReactor r in reactor) {
        reactorGen += r.CurrentOutput;
        if(r.IsWorking) funcReactor++;
    }

    // Hydrogen ?
    // var solar = new List<IMySolarPanel>();
    // GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solar);
    
    // float solarGen = 0;
    // int funcSolar = 0;
    
    // foreach(IMySolarPanel s in solar) {
    //     solarGen += s.CurrentOutput;
    //     if(s.IsWorking) funcSolar++;
    // }

    // All Generators
    var generator = new List<IMyPowerProducer>();
    GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(generator);
    
    // float gen = 0;
    int funcGen = 0;
    float totalGen = 0;
    foreach(IMyPowerProducer g in generator) {
        totalGen += g.CurrentOutput;
        if(g.IsWorking) funcGen++;
    }
    // totalGen -= discharging;

    // Ship Charging Speed
    // for(int i = 0; i < generator.Count; i++){
    //     gen += generator[i].CurrentOutput;
    // }
    // float shipsCharging = gen - totalGen;
    
    // Base Integrity
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(blocks);
    
    int funcBlocks = 0;
    
    foreach(IMyTerminalBlock b in blocks) {
        if(b.IsWorking) funcBlocks++;
    }

    // Display
    String[] lcdLines = new String[20];
    // max = max*getPart(funcBat, battery.Count);
    
    lcdLines[0] = "Battery:    " + cur.ToString("0.00") + " / " + max.ToString("0.00") + " MWh " + getPercent(cur, max) + "%";
    lcdLines[1] = "+" + charging.ToString("0.00") + " MW | -" + discharging.ToString("0.00") + " MW [" + funcBat + "/" + battery.Count + "]";
    lcdLines[2] = getBatteryPrediction(cur, max, charging, discharging);
    
    lcdLines[4] = "Production:      " + totalGen.ToString("0.00") + " MW [" + funcGen + "/" + generator.Count + "]";
    lcdLines[5] = " => Turbines:   " + turbineGen.ToString("0.00") + " MW [" + funcTurbine + "/" + turbine.Count + "]";
    lcdLines[6] = " => Solars:       " + solarGen.ToString("0.00") + " MW [" + funcSolar + "/" + solar.Count + "]";
    lcdLines[7] = " => Reactors:   " + reactorGen.ToString("0.00") + " MW [" + funcReactor + "/" + reactor.Count + "]";
    lcdLines[8] = " => Batteries:   " + discharging.ToString("0.00") + " MW [" + funcBat + "/" + battery.Count + "]";

    lcdLines[10] = getTankStats("Hydrogen");
    lcdLines[11] = getTankStats("Oxygen");
    
    lcdLines[16] = "Total Base Integrity:    [" + funcBlocks + "/" + blocks.Count + "] " + getPercent(funcBlocks, blocks.Count) + "%";
    
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

public string getBatteryPrediction(float charge, float capacity, float charging, float discharging) {
    float powerTrend = charging - discharging;
    if (powerTrend > 0) {
        float fullTime = (capacity - charge) / powerTrend;
        return "Full in " + fullTime.ToString("0.00") + " hours";
    } else if (powerTrend < 0) {
        float emptyTime = charge / (powerTrend * -1);
        return "Empty in " + emptyTime.ToString("0.00") + " hours";
    }
    return "No charging/discharging or it is the same";
}

public string getTankStats(String type) {

    int count = 0;
    double filled = 0;
    switch (type) {
        case "Hydrogen":
            var tank = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(tank, t => t.BlockDefinition.SubtypeId.Contains($"{type}"));
            foreach (IMyGasTank t in tank) {
                count += 1;
                filled += t.FilledRatio;
                type = t.ToString();
            }
            type = "H2";
            break;
        case "Oxygen":
            var oxygentank = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(oxygentank, t => t.CustomName.Contains("BaseOxygen"));
            foreach (IMyGasTank t in oxygentank) {
                count += 1;
                filled += t.FilledRatio;
                type = t.ToString();
            }
            type = "O2";
            break;
    }
    
    float percent = (float)filled/count;
    int barWidth = 100;
    int filledBar = (int)(percent*barWidth);
    int emptyBar = barWidth - filledBar;
    String bar = "";
    for (int i = 0; i < filledBar; i++) {
        bar += "|";
    }
    for (int i = 0; i < emptyBar; i++) {
        bar += ".";
    }

    return type + ": " + bar + " " + (percent*100).ToString("0.00") + "%";
    // return "a";
}
