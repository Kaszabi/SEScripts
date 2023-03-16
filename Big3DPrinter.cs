public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

float maxX = 10;
float maxY = 10;
float maxZ = 10;
float prevY = 0;
float prevZ = 0;
bool XIsMoving = false;
bool YIsMoving = false;
bool ZIsMoving = false;
int step = 0;
float welderSpeed = 0.5f;

public void Main(string argument, UpdateType updateSource) {
    IMyTextPanel display = GridTerminalSystem.GetBlockWithName("Printer - PrintScreen") as IMyTextPanel;
    String[] lcdLines = new String[20];
    IMyPistonBase pistonX = GridTerminalSystem.GetBlockWithName("Printer - X axis") as IMyPistonBase;
    IMyPistonBase pistonY = GridTerminalSystem.GetBlockWithName("Printer - Y axis") as IMyPistonBase;
    IMyPistonBase pistonZ = GridTerminalSystem.GetBlockWithName("Printer - Z axis") as IMyPistonBase;
    IMyShipWelder welder = GridTerminalSystem.GetBlockWithName("Printer - Welder") as IMyShipWelder;

    IMyInteriorLight btnOrigo = GridTerminalSystem.GetBlockWithName("Printer - L1") as IMyInteriorLight;
    IMyInteriorLight btnStart = GridTerminalSystem.GetBlockWithName("Printer - L2") as IMyInteriorLight;
    IMyInteriorLight btnStop = GridTerminalSystem.GetBlockWithName("Printer - L3") as IMyInteriorLight;
    IMyInteriorLight btnWelder = GridTerminalSystem.GetBlockWithName("Printer - L4") as IMyInteriorLight;

    float curX = pistonX.CurrentPosition;
    float curY = pistonY.CurrentPosition;
    float curZ = pistonZ.CurrentPosition;

    if(btnStart.IsWorking){
        step = startPrinting(pistonX, pistonY, pistonZ, curX, curY, curZ, welder);
    }

    if(btnStop.IsWorking){
        pistonX.Velocity = 0f;
        pistonY.Velocity = 0f;
        pistonZ.Velocity = 0f;
        XIsMoving = false;
        YIsMoving = false;
        ZIsMoving = false;
        welder.Enabled = false;
    }

    if(btnOrigo.IsWorking){
        if(curZ != maxZ && (curX != 0 || curY != 0)){
            pistonZ.Extend();
            pistonZ.Velocity = 5f;
        }else{
            pistonZ.Velocity = 0f;
            pistonX.Retract();
            pistonX.Velocity = -5f;
            pistonY.Retract();
            pistonY.Velocity = -5f;
            if(curX == 0 && curY == 0){
                pistonX.Velocity = 0f;
                pistonY.Velocity = 0f;
                pistonZ.Retract();
                pistonZ.Velocity = -5f;
            }
        }
        if(curX == 0 && curY == 0 && curZ == 0){
            pistonZ.Velocity = 0f;
        }
        prevY = 0;
        prevZ = 0;
        XIsMoving = false;
        YIsMoving = false;
        ZIsMoving = false;
        welder.Enabled = false;
    }
    

    lcdLines[0] = $"------------------------------     PRINTING STATISTICS     ------------------------------";
    lcdLines[2] = $"Printer Coords (X, Y, Z): {curX.ToString("0.00")} {curY.ToString("0.00")} {curZ.ToString("0.00")}";
    lcdLines[3] = $"Current step: {step}";
    lcdLines[4] = $"Welding Status: {welder.Enabled}";
    PrintToLCD(lcdLines, display);
}

public void PrintToLCD(String[] lcdLines, IMyTextPanel display){
    string lcdText = " ";
    for(int i = 0; i < lcdLines.Length; i++){
        lcdText += lcdLines[i] + "\n";
    }
    display.WriteText(lcdText);
}

public void extendPiston(IMyPistonBase piston, float velocity){
    piston.Extend();
    piston.Velocity = velocity;
}

public void retractPiston(IMyPistonBase piston, float velocity){
    piston.Retract();
    piston.Velocity = -velocity;
}

public void stopPiston(IMyPistonBase piston){
    piston.Velocity = 0f;
}

public int startPrinting(IMyPistonBase pistonX, IMyPistonBase pistonY, IMyPistonBase pistonZ, float curX, float curY, float curZ, IMyShipWelder welder){

    if(!XIsMoving && curX == 0 && !YIsMoving && !ZIsMoving){ // Start move
        extendPiston(pistonX, welderSpeed);
        XIsMoving = true;
        welder.Enabled = true;
        return 1;

    }else if(XIsMoving && curX == maxX && !YIsMoving && !ZIsMoving){ // End of X line, step Y
        stopPiston(pistonX);
        XIsMoving = false;
        extendPiston(pistonY, welderSpeed);
        YIsMoving = true;
        return 2;

    }else if(YIsMoving && curY >= prevY + 2.5f && !XIsMoving && !ZIsMoving && curX == maxX){ // Y stepped, move back X
        stopPiston(pistonY);
        YIsMoving = false;
        prevY += 2.5f;
        retractPiston(pistonX, welderSpeed);
        XIsMoving = true;
        return 3;

    }else if(XIsMoving && curX == 0 && !YIsMoving && !ZIsMoving){ // X back, step Y
        stopPiston(pistonX);
        XIsMoving = false;
        extendPiston(pistonY, welderSpeed);
        YIsMoving = true;
        return 4;

    }else if(YIsMoving && curY >= prevY + 2.5f && !XIsMoving && !ZIsMoving && curX == 0){ // Y stepped, move X
        stopPiston(pistonY);
        YIsMoving = false;
        prevY += 2.5f;
        extendPiston(pistonX, welderSpeed);
        XIsMoving = true;
        return 5;

    }else if(curX == maxX && curY == maxY){ // Layer finished, stepping up and going to (0,0) turn off welder
        retractPiston(pistonX, 5);
        retractPiston(pistonY, 5);
        extendPiston(pistonZ, welderSpeed);
        XIsMoving = true;
        YIsMoving = true;
        ZIsMoving = true;
        welder.Enabled = false;
        return 6;

    }else if(ZIsMoving && curZ >= prevZ + 2.5f){ // Z stepped
        stopPiston(pistonZ);
        ZIsMoving = false;
        prevZ += 2.5f;
        return 7;

    }else if(!ZIsMoving && curX == 0 && curY == 0){ // Waiting for X,Y to reach (0,0)
        stopPiston(pistonX);
        stopPiston(pistonY);
        XIsMoving = false;
        YIsMoving = false;
        prevY = 0;
        welder.Enabled = true;
        return 8;
    }else{
        return step;
    }
}