/*
 * R e a d m e
 * -----------
 * 
 * LCD Inventory By Helfima
 * 1) Place Programmable block
 * 2) Place LCD Panel
 * 3) Edit LCD Custom Data and write prepare
 * 4) Add Script into Programmable block
 * 5) Return edit LCD Custom Data and set what you want
 */


const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;
KProperty MyProperty;

MyCommandLine commandLine = new MyCommandLine();
private IMyTextSurface drawingSurface;

private BlockSystem<IMyTextPanel> lcds = null;
private BlockSystem<IMyCockpit> cockpits = null;

private bool ForceUpdate = false;
private bool search = true;

private Dictionary<long, DisplayLcd> displayLcds = new Dictionary<long, DisplayLcd>();

public Program()
{
    MyProperty = new KProperty(this);
    MyProperty.Load();
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    drawingSurface = Me.GetSurface(0);
    drawingSurface.ContentType = ContentType.TEXT_AND_IMAGE;
    Search();
}

private void Init()
{
}

private void Search()
{
    BlockFilter<IMyTextPanel> block_filter = BlockFilter<IMyTextPanel>.Create(Me, MyProperty.lcd_filter);
    lcds = BlockSystem<IMyTextPanel>.SearchByFilter(this, block_filter);

    //BlockFilter<IMyCockpit> cockpit_filter = BlockFilter<IMyCockpit>.Create(Me, MyProperty.lcd_filter);
    //cockpits = BlockSystem<IMyCockpit>.SearchByFilter(this, cockpit_filter);

    search = false;
}

public void Save()
{
    MyProperty.Save();
}

public void Main(string argument, UpdateType updateType)
{
    if ((updateType & CommandUpdate) != 0)
    {
        RunCommand(argument);
    }
    if ((updateType & UpdateType.Update100) != 0)
    {
        RunContinuousLogic();
    }
}

private void RunCommand(string argument)
{
    MyProperty.Load();
    Init();
    if (argument != null)
    {
        commandLine.TryParse(argument);
        var command = commandLine.Argument(0);
        if (command != null) command = command.Trim().ToLower();
        switch (command)
        {
            case "default":
                Me.CustomData = "";
                MyProperty.Load();
                MyProperty.Save();
                break;
            case "forceupdate":
                ForceUpdate = true;
                break;
            case "test":
                IMyTextPanel lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(commandLine.Argument(1));
                lcd.ScriptBackgroundColor = Color.Black;
                Drawing drawing = new Drawing(lcd);
                drawing.Test();
                drawing.Dispose();
                break;
            case "getname":
                int index = 0;
                int.TryParse(commandLine.Argument(1), out index);
                var names = new List<string>();
                drawingSurface.GetSprites(names);
                Echo($"Sprite {index} name={names[index]}");
                IMyTextPanel lcdResult = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Result Name");
                lcdResult.ContentType = ContentType.TEXT_AND_IMAGE;
                lcdResult.WriteText($"Sprite {index}\n", false);
                lcdResult.WriteText($"name={names[index]}", true);
                break;
            case "gettype":
                int.TryParse(commandLine.Argument(1), out index);
                string name = commandLine.Argument(1);
                DiplayGetType(name);
                break;
            default:
                search = true;
                Search();
                break;
        }
    }
}
void RunContinuousLogic()
{
    if (search) Search();
    Display();
    RunLcd();
}
private void DiplayGetType(string name)
{
    IMyTerminalBlock block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(name);
    IMyTextPanel lcdResult2 = GridTerminalSystem.GetBlockWithName("Result Type") as IMyTextPanel;
    if(lcdResult2 != null)
    {
        lcdResult2.ContentType = ContentType.TEXT_AND_IMAGE;
        lcdResult2.WriteText($"Block {name}\n", false);
        lcdResult2.WriteText($"Type Name={block.GetType().Name}\n", true);
        lcdResult2.WriteText($"SubtypeName={block.BlockDefinition.SubtypeName}\n", true);
        lcdResult2.WriteText($"SubtypeId={block.BlockDefinition.SubtypeId}\n", true);
    }
    else
    {
        Echo($"Block {name}");
        Echo($"Type Name={block.GetType().Name}");
        Echo($"SubtypeName={block.BlockDefinition.SubtypeName}");
        Echo($"SubtypeId={block.BlockDefinition.SubtypeId}");
    }
}
private void Display()
{
    drawingSurface.WriteText($"LCD list size:{lcds.List.Count}\n", false);
    //drawingSurface.WriteText($"Cockpit list size:{cockpits.List.Count}\n", true);
}

private void RunLcd()
{
    lcds.List.ForEach(delegate (IMyTextPanel lcd) {
        if (lcd.CustomData != null && !lcd.CustomData.Equals(""))
        {
            MyIniParseResult result;
            MyIni MyIni = new MyIni();
            MyIni.TryParse(lcd.CustomData, out result);
            if (MyIni.ContainsSection("Inventory") || lcd.CustomData.Trim().Equals("prepare"))
            {
                DisplayLcd displayLcd;
                if (displayLcds.ContainsKey(lcd.EntityId))
                {
                    displayLcd = displayLcds[lcd.EntityId];
                }
                else
                {
                    displayLcd = new DisplayLcd(this, lcd);
                    displayLcds.Add(lcd.EntityId, displayLcd);
                }
                displayLcd.Load(MyIni);
                displayLcd.Draw();
            }
        }
    });
    ForceUpdate = false;
}

public class BlockSystem<T> where T: class
{
    protected Program program;
    public List<T> List = new List<T>();

    public BlockSystem(){
        List = new List<T>();
    }

    public static BlockSystem<T> SearchBlocks(Program program, Func<T, bool> collect = null, string info = null)
    {
        List<T> list = new List<T>();
        try
        {
            program.GridTerminalSystem.GetBlocksOfType<T>(list, collect);
        }
        catch { }
        if(info == null) program.Echo(String.Format("List <{0}> count: {1}", typeof(T).Name, list.Count));
        else program.Echo(String.Format("List <{0}> count: {1}", info, list.Count));

        return new BlockSystem<T>()
        {
            program = program,
            List = list
        };
        }
    public static BlockSystem<T> SearchByTag(Program program, string tag)
    {
        return BlockSystem<T>.SearchBlocks(program, block => ((IMyTerminalBlock)block).CustomName.Contains(tag), tag);
    }
    public static BlockSystem<T> SearchByName(Program program, string name)
    {
        return BlockSystem<T>.SearchBlocks(program, block => ((IMyTerminalBlock)block).CustomName.Equals(name), name);
    }
    public static List<IMyBlockGroup> SearchGroups(Program program, Func<IMyBlockGroup, bool> collect = null)
    {
        List<IMyBlockGroup> list = new List<IMyBlockGroup>();
        try
        {
            program.GridTerminalSystem.GetBlockGroups(list, collect);
        }
        catch { }
        program.Echo(String.Format("List <IMyBlockGroup> count: {0}", list.Count));

        return list;
    }
    public static BlockSystem<T> SearchByGroup(Program program, string name)
    {
        List<T> list = new List<T>();
        IMyBlockGroup group = null;
        try
        {
            group = program.GridTerminalSystem.GetBlockGroupWithName(name);
        }
        catch { }
        if (group != null) group.GetBlocksOfType<T>(list);
        program.Echo(String.Format("List <{0}> count: {1}", name, list.Count));

        return new BlockSystem<T>()
        {
            program = program,
            List = list
        };
    }
    public static BlockSystem<T> SearchByGrid(Program program, IMyCubeGrid cubeGrid)
    {
        return BlockSystem<T>.SearchBlocks(program, block => ((IMyTerminalBlock)block).CubeGrid == cubeGrid);
    }

    public static BlockSystem<T> SearchByFilter(Program program, BlockFilter<T> filter)
    {
        List<T> list = new List<T>();
        try
        {
            if (filter.ByGroup)
            {
                List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
                program.GridTerminalSystem.GetBlockGroups(groups, filter.GroupVisitor());
                List<T> group_list = new List<T>();
                groups.ForEach(delegate (IMyBlockGroup group)
                {
                    group_list.Clear();
                    group.GetBlocksOfType<T>(list, filter.BlockVisitor());
                    list.AddList(group_list);
                });
            }
            else
            {
                program.GridTerminalSystem.GetBlocksOfType<T>(list, filter.BlockVisitor());
            }
        }
        catch { }
        program.Echo(String.Format("List<{0}>:{1}", filter.Value, list.Count));
        return new BlockSystem<T>()
        {
            program = program,
            List = list
        };
    }

    public static List<IMyBlockGroup> SearchGroupFilter(Program program, BlockFilter<T> filter)
    {
        List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
        try
        {
            if (filter.ByGroup)
            {
                program.GridTerminalSystem.GetBlockGroups(groups, filter.GroupVisitor());
            }
        }
        catch { }
        program.Echo(String.Format("List <{0}> count: {1}", filter.Value, groups.Count));
        return groups;
    }

    public void ForEach(Action<T> action)
    {
        if (!IsEmpty)
        {
            List.ForEach(action);
        }
    }

    public bool IsPosition(float position, float epsilon = 0.1f)
    {
        bool isState = true;
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    float value = block.CurrentPosition - position;
                    if (Math.Abs(value) > epsilon) isState = false;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    float value = block.Angle - float.Parse(Util.DegToRad(position).ToString());
                    if (Math.Abs(value) > epsilon) isState = false;
                }
            }
        }
        return isState;
    }

    public bool IsMorePosition(float position)
    {
        bool isState = true;
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    if (block.CurrentPosition < position) isState = false;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    if (block.Angle < float.Parse(Util.DegToRad(position).ToString())) isState = false;
                }
            }
        }
        return isState;
    }

    public bool IsLessPosition(float position)
    {
        bool isState = true;
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    if (block.CurrentPosition > position) isState = false;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    if (block.Angle > float.Parse(Util.DegToRad(position).ToString())) isState = false;
                }
            }
        }
        return isState;
    }

    public bool IsPositionMax(float epsilon = 0.1f)
    {
        bool isState = true;
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    float value = block.CurrentPosition - block.MaxLimit;
                    if (Math.Abs(value) > epsilon) isState = false;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    float value = block.Angle - block.UpperLimitRad;
                    if (Math.Abs(value) > epsilon/100) isState = false;
                }
            }
        }
        return isState;
    }

    public bool IsPositionMin(float epsilon = 0.1f)
    {
        bool isState = true;
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    float value = block.CurrentPosition - block.MinLimit;
                    if (Math.Abs(value) > epsilon) isState = false;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    float value = block.Angle - block.LowerLimitRad;
                    if (Math.Abs(value) > epsilon / 100) isState = false;
                }
            }
        }
        return isState;
    }

    public void Velocity(float velocity)
    {
        if (!IsEmpty)
        {
            if (List is List<IMyPistonBase>)
            {
                foreach (IMyPistonBase block in List)
                {
                    block.Velocity = velocity;
                }
            }
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    block.TargetVelocityRPM = velocity;
                }
            }
        }
    }

    public void ApplyAction(string action)
    {
        if (!IsEmpty)
        {
            foreach (IMyTerminalBlock block in List)
            {
                block.ApplyAction(action);
            }
        }
    }
    public void On()
    {
        ApplyAction("OnOff_On");
    }
    public bool IsOn()
    {
        bool isState = true;
        if (!IsEmpty)
        {
            foreach (IMyTerminalBlock block in List)
            {
                if (!block.GetValueBool("OnOff")) isState = false;
            }
        }
        return isState;
    }
    public void Off()
    {
        ApplyAction("OnOff_Off");
    }

    public bool IsOff()
    {
        return !IsOn();
    }

    public void Lock()
    {
        if (!IsEmpty)
        {
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    block.RotorLock = true;
                }
            }
            else
            {
                ApplyAction("Lock");
            }
        }
    }
    public void Unlock()
    {
        if (!IsEmpty)
        {
            if (List is List<IMyMotorStator>)
            {
                foreach (IMyMotorStator block in List)
                {
                    block.RotorLock = false;
                }
            }
            else
            {
                ApplyAction("Unlock");
            }
        }
    }
    public void Merge(BlockSystem<T> blockSystem)
    {
        List.AddList(blockSystem.List);
    }

    public bool IsEmpty
    {
        get
        {
            if (List != null && List.Count > 0)
            {
                return false;
            }
            return true;
        }
    }

    public T First
    {
        get
        {
            if (!IsEmpty)
            {
                return List.First();
            }
            return null;
        }
    }
}

public class BlockFilter<T> where T : class
{
    public string Value;
    public string Filter;
    public IMyCubeGrid CubeGrid;
    public bool ByContains = false;
    public bool ByGroup = false;
    public bool MultiGrid = false;
    public bool HasInventory = false;

    public static BlockFilter<T> Create(IMyTerminalBlock parent, string filter)
    {
        BlockFilter<T> blockFilter = new BlockFilter<T>
        {
            Value = filter,
            CubeGrid = parent.CubeGrid
        };
        if (filter.Contains(":"))
        {
            string[] values = filter.Split(':');
            if (values[0].Contains("C")) blockFilter.ByContains = true;
            if (values[0].Contains("G")) blockFilter.ByGroup = true;
            if (values[0].Contains("M")) blockFilter.MultiGrid = true;
            if (!values[1].Equals("*")) blockFilter.Filter = values[1];
        }
        else
        {
            if(!filter.Equals("*")) blockFilter.Filter = filter;
        }
        return blockFilter;
    }
    public Func<T, bool> BlockVisitor()
    {
        return delegate(T block) {
            IMyTerminalBlock tBlock = (IMyTerminalBlock)block;
            bool state = true;
            if (Filter != null && !ByGroup)
            {
                if (ByContains) { if (!tBlock.CustomName.Contains(Filter)) state = false; }
                else { if (!tBlock.CustomName.Equals(Filter)) state = false; }
            }
            if (!MultiGrid) { if (tBlock.CubeGrid != CubeGrid) state = false; }
            if (HasInventory) { if (!tBlock.HasInventory) state = false; }
            return state;
        };
    }

    public Func<IMyBlockGroup, bool> GroupVisitor()
    {
        return delegate (IMyBlockGroup group) {
            bool state = true;
            if (Filter != null && ByGroup)
            {
                if (ByContains) { if (!group.Name.Contains(Filter)) state = false; }
                else { if (!group.Name.Equals(Filter)) state = false; }
            }
            return state;
        };
    }
}

public class DisplayDrill
{
    protected DisplayLcd DisplayLcd;

    private bool enable = false;

    public bool search = true;

    private string filter = "*";

    private string drills_orientation = "y";
    private bool drills_rotate = false;
    private bool drills_flip_x = false;
    private bool drills_flip_y = false;
    private bool drills_info = false;
    private float drills_size = 50f;
    private float drills_padding_x = 0f;
    private float drills_padding_y = 0f;

    private BlockSystem<IMyShipDrill> drill_inventories;
    public DisplayDrill(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }
    public void Load(MyIni MyIni)
    {
        enable = MyIni.Get("Drills", "on").ToBoolean(false);
        filter = MyIni.Get("Drills", "filter").ToString("GM:Drills");
        drills_orientation = MyIni.Get("Drills", "orientation").ToString("y");
        drills_rotate = MyIni.Get("Drills", "rotate").ToBoolean(false);
        drills_flip_x = MyIni.Get("Drills", "flip_x").ToBoolean(false);
        drills_flip_y = MyIni.Get("Drills", "flip_y").ToBoolean(false);
        drills_size = MyIni.Get("Drills", "size").ToSingle(50f);
        drills_info = MyIni.Get("Drills", "info").ToBoolean(false);
        drills_padding_x = MyIni.Get("Drills", "padding_x").ToSingle(0f);
        drills_padding_y = MyIni.Get("Drills", "padding_y").ToSingle(0f);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Drills", "on", enable);
        MyIni.Set("Drills", "filter", filter);
        MyIni.Set("Drills", "orientation", drills_orientation);
        MyIni.Set("Drills", "rotate", drills_rotate);
        MyIni.Set("Drills", "flip_x", drills_flip_x);
        MyIni.Set("Drills", "flip_y", drills_flip_y);
        MyIni.Set("Drills", "size", drills_size);
        MyIni.Set("Drills", "info", drills_info);
        MyIni.Set("Drills", "padding_x", drills_padding_x);
        MyIni.Set("Drills", "padding_y", drills_padding_y);
    }

    private void Search()
    {
        BlockFilter<IMyShipDrill> block_filter = BlockFilter<IMyShipDrill>.Create(DisplayLcd.lcd, filter);
        drill_inventories = BlockSystem<IMyShipDrill>.SearchByFilter(DisplayLcd.program, block_filter);

        search = false;
    }
    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        if (search) Search();

        float width = drills_size;
        float padding = 4f;
        float x_min = 0f;
        float x_max = 0f;
        float y_min = 0f;
        float y_max = 0f;
        bool first = true;
        Vector2 padding_screen = new Vector2(drills_padding_x, drills_padding_y);
        StyleGauge style = new StyleGauge()
        {
            Orientation = SpriteOrientation.Horizontal,
            Fullscreen = false,
            Width = width,
            Height = width,
            Padding = new StylePadding(0),
            Round = false,
            RotationOrScale = 0.5f,
            Percent= drills_size > 49 ? true : false
        };

        if (drills_info)
        {
            drawing.AddSprite(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = $"Drill Number:{drill_inventories.List.Count} ({filter})",
                Size = new Vector2(width, width),
                Color = Color.DimGray,
                Position = position + new Vector2(0, 0),
                RotationOrScale = 0.5f,
                FontId = drawing.Font,
                Alignment = TextAlignment.LEFT

            });
            position += new Vector2(0, 20);
        }
        drill_inventories.ForEach(delegate (IMyShipDrill drill)
        {
            switch (drills_orientation)
            {
                case "x":
                    if (first || drill.Position.Y < x_min) x_min = drill.Position.Y;
                    if (first || drill.Position.Y > x_max) x_max = drill.Position.Y;
                    if (first || drill.Position.Z < y_min) y_min = drill.Position.Z;
                    if (first || drill.Position.Z > y_max) y_max = drill.Position.Z;
                    break;
                case "y":
                    if (first || drill.Position.X < x_min) x_min = drill.Position.X;
                    if (first || drill.Position.X > x_max) x_max = drill.Position.X;
                    if (first || drill.Position.Z < y_min) y_min = drill.Position.Z;
                    if (first || drill.Position.Z > y_max) y_max = drill.Position.Z;
                    break;
                default:
                    if (first || drill.Position.X < x_min) x_min = drill.Position.X;
                    if (first || drill.Position.X > x_max) x_max = drill.Position.X;
                    if (first || drill.Position.Y < y_min) y_min = drill.Position.Y;
                    if (first || drill.Position.Y > y_max) y_max = drill.Position.Y;
                    break;
            }
            first = false;
        });
        //drawingSurface.WriteText($"X min:{x_min} Y min:{y_min}\n", false);
        drill_inventories.ForEach(delegate (IMyShipDrill drill)
        {
            IMyInventory block_inventory = drill.GetInventory(0);
            long volume = block_inventory.CurrentVolume.RawValue;
            long maxVolume = block_inventory.MaxVolume.RawValue;
            float x = 0;
            float y = 0;
            switch (drills_orientation)
            {
                case "x":
                    x = Math.Abs(drill.Position.Y - x_min);
                    y = Math.Abs(drill.Position.Z - y_min);
                    break;
                case "y":
                    x = Math.Abs(drill.Position.X - x_min);
                    y = Math.Abs(drill.Position.Z - y_min);
                    break;
                default:
                    x = Math.Abs(drill.Position.X - x_min);
                    y = Math.Abs(drill.Position.Y - y_min);
                    break;
            }
            //drawingSurface.WriteText($"X:{x} Y:{y}\n", true);
            if (drills_flip_x) x = Math.Abs(x_max - x_min) - x;
            if (drills_flip_y) y = Math.Abs(y_max - y_min) - y;
            //drawingSurface.WriteText($"Volume [{x},{y}]:{volume}/{maxVolume}\n", true);
            Vector2 position_relative = drills_rotate ? new Vector2(y * (width + padding), x * (width + padding)) : new Vector2(x * (width + padding), y * (width + padding));

            drawing.DrawGauge(position + position_relative + padding_screen, volume, maxVolume, style);
        });

        return position;
    }
}

public class DisplayInventory
{
    protected DisplayLcd DisplayLcd;

    private bool enable = false;

    public bool search = true;

    private string filter = "*";

    private bool gauge = true;
    private bool gaugeFullscreen = true;
    private bool gaugeHorizontal = true;
    private float gaugeWidth = 80f;
    private float gaugeHeight = 40f;

    private bool item = true;
    private float itemSize = 80f;
    private bool itemOre = true;
    private bool itemIngot = true;
    private bool itemComponent = true;
    private bool itemAmmo = true;

    private float topPadding = 10f;
    private float leftPadding = 10f;
    private float cellSpacing = 2f;

    private BlockSystem<IMyTerminalBlock> inventories = null;
    private Dictionary<string, Item> item_list = new Dictionary<string, Item>();
    private Dictionary<string, double> last_amount = new Dictionary<string, double>();
    public DisplayInventory(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }
    public void Load(MyIni MyIni)
    {
        filter = MyIni.Get("Inventory", "filter").ToString("*");
        enable = MyIni.Get("Inventory", "on").ToBoolean(true);

        gauge = MyIni.Get("Inventory", "gauge_on").ToBoolean(true);
        gaugeFullscreen = MyIni.Get("Inventory", "gauge_fullscreen").ToBoolean(true);
        gaugeHorizontal = MyIni.Get("Inventory", "gauge_horizontal").ToBoolean(true);
        gaugeWidth = MyIni.Get("Inventory", "gauge_width").ToSingle(80f);
        gaugeHeight = MyIni.Get("Inventory", "gauge_height").ToSingle(40f);

        item = MyIni.Get("Inventory", "item_on").ToBoolean(true);
        itemSize = MyIni.Get("Inventory", "item_size").ToSingle(80f);
        itemOre = MyIni.Get("Inventory", "item_ore").ToBoolean(true);
        itemIngot = MyIni.Get("Inventory", "item_ingot").ToBoolean(true);
        itemComponent = MyIni.Get("Inventory", "item_component").ToBoolean(true);
        itemAmmo = MyIni.Get("Inventory", "item_ammo").ToBoolean(true);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Inventory", "filter", filter);
        MyIni.Set("Inventory", "on", enable);

        MyIni.Set("Inventory", "gauge_on", gauge);
        MyIni.Set("Inventory", "gauge_fullscreen", gaugeFullscreen);
        MyIni.Set("Inventory", "gauge_horizontal", gaugeHorizontal);
        MyIni.Set("Inventory", "gauge_width", gaugeWidth);
        MyIni.Set("Inventory", "gauge_height", gaugeHeight);

        MyIni.Set("Inventory", "item_on", item);
        MyIni.Set("Inventory", "item_size", itemSize);
        MyIni.Set("Inventory", "item_ore", itemOre);
        MyIni.Set("Inventory", "item_ingot", itemIngot);
        MyIni.Set("Inventory", "item_component", itemComponent);
        MyIni.Set("Inventory", "item_ammo", itemAmmo);
    }

    private void Search()
    {
        BlockFilter<IMyTerminalBlock> block_filter = BlockFilter<IMyTerminalBlock>.Create(DisplayLcd.lcd, filter);
        block_filter.HasInventory = true;
        inventories = BlockSystem<IMyTerminalBlock>.SearchByFilter(DisplayLcd.program, block_filter);

        search = false;
    }
    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        if (search) Search();

        if (gauge)
        {
            position = DisplayGauge(drawing, position);
        }
        else { position += new Vector2(0, topPadding); }
        if (item)
        {
            List<string> types = new List<string>();
            if (itemOre) types.Add(Item.TYPE_ORE);
            if (itemIngot) types.Add(Item.TYPE_INGOT);
            if (itemComponent) types.Add(Item.TYPE_COMPONENT);
            if (itemAmmo) types.Add(Item.TYPE_AMMO);

            last_amount.Clear();
            foreach (KeyValuePair<string, Item> entry in item_list)
            {
                last_amount.Add(entry.Key, entry.Value.Amount);
            }

            InventoryCount();
            position = DisplayByType(drawing, position, types);
        }
        return position;
    }

    private Vector2 DisplayGauge(Drawing drawing, Vector2 position)
    {
        long volumes = 0;
        long maxVolumes = 1;
        inventories.ForEach(delegate (IMyTerminalBlock block)
        {
            for (int i = 0; i < block.InventoryCount; i++)
            {
                IMyInventory block_inventory = block.GetInventory(i);
                long volume = block_inventory.CurrentVolume.RawValue;
                volumes += volume;
                long maxVolume = block_inventory.MaxVolume.RawValue;
                maxVolumes += maxVolume;
                //drawingSurface.WriteText($"\nVolume:{volume}/{maxVolume}", true);
            }
        });
        //drawingSurface.WriteText($"\nVolume:{volumes}/{maxVolumes}", true);
        StyleGauge style = new StyleGauge()
        {
            Orientation = gaugeHorizontal ? SpriteOrientation.Horizontal : SpriteOrientation.Vertical,
            Fullscreen = gaugeFullscreen,
            Width = gaugeWidth,
            Height = gaugeHeight
        };
        drawing.DrawGauge(position, volumes, maxVolumes, style);
        if (gaugeHorizontal) position += new Vector2(0, gaugeHeight + topPadding);
        else position += new Vector2(gaugeWidth + leftPadding, 0);
        return position;
    }

    private int GetLimit(Drawing drawing)
    {
        int limit = 5;
        if (gauge && gaugeHorizontal) { limit = (int)Math.Floor((drawing.viewport.Height - gaugeHeight - topPadding) / (itemSize + cellSpacing)); }
        else { limit = (int)Math.Floor((drawing.viewport.Height - topPadding) / (itemSize + cellSpacing)); }
        return Math.Max(limit, 1);
    }
    private Vector2 DisplayByType(Drawing drawing, Vector2 position, List<string> types)
    {
        int count = 0;
        float height = itemSize;
        float width = 3 * itemSize;
        int limit = GetLimit(drawing);
        string colorDefault = DisplayLcd.program.MyProperty.Get("color", "default");
        int limitDefault = DisplayLcd.program.MyProperty.GetInt("Limit", "default");

        foreach (string type in types)
        {
            foreach (KeyValuePair<string, Item> entry in item_list.OrderByDescending(entry => entry.Value.Amount).Where(entry => entry.Value.Type == type))
            {
                Item item = entry.Value;
                Vector2 position2 = position + new Vector2(width * (count / limit), (cellSpacing + height) * (count - (count / limit) * limit));
                // Icon
                Color color = DisplayLcd.program.MyProperty.GetColor("color", item.Name, colorDefault);
                int limitBar = DisplayLcd.program.MyProperty.GetInt("Limit", item.Name, limitDefault);
                //DisplayIcon(drawing, item, position2, width);
                StyleIcon style = new StyleIcon()
                {
                    path = item.Icon,
                    Width = width,
                    Height = height,
                    Color = color
                };
                int variance = 2;
                //DisplayLcd.program.drawingSurface.WriteText($"variance:{entry.Key}?{last_amount.ContainsKey(entry.Key)}\n", true);
                if (last_amount.ContainsKey(entry.Key))
                {
                    if (last_amount[entry.Key] < item.Amount) variance = 1;
                    if (last_amount[entry.Key] > item.Amount) variance = 3;
                }
                else
                {
                    variance = 1;
                }
                drawing.DrawGaugeIcon(position2, item.Name, item.Amount, limitBar, style, variance);
                count++;
            }
        }
        if(item_list.Count > limit) return position + new Vector2(0, (cellSpacing + height) * limit);
        return position + new Vector2(0, (cellSpacing + height) * item_list.Count);
    }

    private void InventoryCount()
    {
        item_list.Clear();
        foreach (IMyTerminalBlock block in inventories.List)
        {

            for (int i = 0; i < block.InventoryCount; i++)
            {
                IMyInventory block_inventory = block.GetInventory(i);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block_inventory.GetItems(items);
                foreach (MyInventoryItem block_item in items)
                {

                    string name = Util.GetName(block_item);
                    string type = Util.GetType(block_item);
                    double amount = 0;
                    string key = String.Format("{0}_{1}", type, name);
                    //string icon = block_item.Type.
                    //DisplayLcd.program.drawingSurface.WriteText($"Type:{type} Name:{name}\n",true);
                    Double.TryParse(block_item.Amount.ToString(), out amount);
                    Item item = new Item()
                    {
                        Type = type,
                        Name = name,
                        Amount = amount
                    };

                    if (item_list.ContainsKey(key))
                    {
                        item_list[key].Amount += amount;
                    }
                    else
                    {
                        item_list.Add(key, item);
                    }
                }
            }
        }
    }
}

public class DisplayLcd
{
    public Program program;
    public IMyTextPanel lcd;

    private DisplayInventory DisplayInventory;
    private DisplayDrill DisplayDrill;
    private DisplayMachine DisplayMachine;
    private DisplayPower DisplayPower;
    private DisplayShip DisplayShip;
    private DisplayTank DisplayTank;
    private int cleanup;

    public DisplayLcd(Program program, IMyTextPanel lcd)
    {
        this.program = program;
        this.lcd = lcd;

        this.DisplayInventory = new DisplayInventory(this);
        this.DisplayDrill = new DisplayDrill(this);
        this.DisplayMachine = new DisplayMachine(this);
        this.DisplayPower = new DisplayPower(this);
        this.DisplayShip = new DisplayShip(this);
        this.DisplayTank = new DisplayTank(this);
        this.cleanup = 0;
    }

    public void Load(MyIni MyIni)
    {
        DisplayInventory.Load(MyIni);
        DisplayDrill.Load(MyIni);
        DisplayMachine.Load(MyIni);
        DisplayPower.Load(MyIni);
        DisplayShip.Load(MyIni);
        DisplayTank.Load(MyIni);
        if (lcd.CustomData.Trim().Equals("prepare") || program.ForceUpdate)
        {
            program.drawingSurface.WriteText($"Prepare:{lcd.CustomName}\n", true);
            DisplayInventory.Save(MyIni);
            DisplayDrill.Save(MyIni);
            DisplayMachine.Save(MyIni);
            DisplayPower.Save(MyIni);
            DisplayShip.Save(MyIni);
            DisplayTank.Save(MyIni);
            lcd.CustomData = MyIni.ToString();
        }
    }
    public void Draw()
    {
        cleanup++;
        Drawing drawing = new Drawing(lcd);
        lcd.ScriptBackgroundColor = Color.Black;
        Vector2 position = drawing.viewport.Position;

        if (cleanup < 100)
        {
            position = DisplayInventory.Draw(drawing, position);
            position = DisplayDrill.Draw(drawing, position);
            position = DisplayMachine.Draw(drawing, position);
            position = DisplayPower.Draw(drawing, position);
            position = DisplayShip.Draw(drawing, position);
            position = DisplayTank.Draw(drawing, position);
        }
        else
        {
            drawing.Clean();
            cleanup = 0;
        }

        drawing.Dispose();
    }
}

public class DisplayMachine
{
    protected DisplayLcd DisplayLcd;

    private bool enable = false;

    public bool search = true;

    private string filter = "*";

    private bool machine_refinery = false;
    private bool machine_assembler = false;

    private int max_loop = 3;
    private int string_len = 20;

    private BlockSystem<IMyProductionBlock> producers;
    private Dictionary<long, Dictionary<string, double>> last_machine_amount = new Dictionary<long, Dictionary<string, double>>();

    public DisplayMachine(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }

    public void Load(MyIni MyIni)
    {
        enable = MyIni.Get("Machine", "on").ToBoolean(false);
        filter = MyIni.Get("Machine", "filter").ToString("*");
        machine_refinery = MyIni.Get("Machine", "refinery").ToBoolean(true);
        machine_assembler = MyIni.Get("Machine", "assembler").ToBoolean(true);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Machine", "on", enable);
        MyIni.Set("Machine", "filter", filter);
        MyIni.Set("Machine", "refinery", machine_refinery);
        MyIni.Set("Machine", "assembler", machine_assembler);
    }

    private void Search()
    {
        BlockFilter<IMyProductionBlock> block_filter = BlockFilter<IMyProductionBlock>.Create(DisplayLcd.lcd, filter);
        producers = BlockSystem<IMyProductionBlock>.SearchByFilter(DisplayLcd.program, block_filter);

        search = false;
    }

    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        if (search) Search();
        List<string> types = new List<string>();
        int limit = 0;
        if (machine_refinery)
        {
            types.Add("Refinery");
            limit += 1;
        }
        if (machine_assembler)
        {
            types.Add("Assembler");
            limit += 1;
        }
        limit = 6 / limit;
        if (types.Count > 0)
        {
            Style style = new Style()
            {
                Width = 250,
                Height = 80,
                Padding = new StylePadding(0),
            };

            foreach (string type in types)
            {
                int count = 0;
                producers.List.Sort(new BlockComparer());
                producers.ForEach(delegate (IMyProductionBlock block)
                {
                    if (block.GetType().Name.Contains(type))
                    {
                        Vector2 position2 = position + new Vector2(style.Width * (count / limit), style.Height * (count - (count / limit) * limit));
                        List<Item> items = TraversalMachine(block);
                        DrawMachine(drawing, position2, block, items, style);
                        count += 1;
                    }
                });
                position += new Vector2(0, style.Height) * limit;
            }
        }

        return position;
    }

    public List<Item> TraversalMachine(IMyProductionBlock block)
    {
        int loop = 0;
        List<Item> items = new List<Item>();

        Dictionary<string, double> last_amount;
        if (last_machine_amount.ContainsKey(block.EntityId))
        {
            last_amount = last_machine_amount[block.EntityId];
        }
        else
        {
            last_amount = new Dictionary<string, double>();
            last_machine_amount.Add(block.EntityId, last_amount);
        }

        if (block is IMyAssembler)
        {
            List<MyProductionItem> productionItems = new List<MyProductionItem>();
            block.GetQueue(productionItems);
            if (productionItems.Count > 0)
            {
                loop = 0;
                foreach (MyProductionItem productionItem in productionItems)
                {
                    if (loop >= max_loop) break;
                    string iName = Util.GetName(productionItem);
                    string iType = Util.GetType(productionItem);
                    string key = String.Format("{0}_{1}", iType, iName);
                    MyDefinitionId itemDefinitionId = productionItem.BlueprintId;
                    double amount = 0;
                    Double.TryParse(productionItem.Amount.ToString(), out amount);

                    int variance = 2;
                    if (last_amount.ContainsKey(key))
                    {
                        if (last_amount[key] < amount) variance = 1;
                        if (last_amount[key] > amount) variance = 3;
                        last_amount[key] = amount;
                    }
                    else
                    {
                        variance = 1;
                        last_amount.Add(key, amount);
                    }

                    items.Add(new Item()
                    {
                        Name = iName,
                        Type = iType,
                        Amount = amount,
                        Variance = variance
                    });
                    loop++;
                }
            }
        }
        else
        {
            List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
            block.InputInventory.GetItems(inventoryItems);
            if (inventoryItems.Count > 0)
            {
                loop = 0;
                foreach (MyInventoryItem inventoryItem in inventoryItems)
                {
                    if (loop >= max_loop) break;
                    string iName = Util.GetName(inventoryItem);
                    string iType = Util.GetType(inventoryItem);
                    string key = String.Format("{0}_{1}", iType, iName);
                    double amount = 0;
                    Double.TryParse(inventoryItem.Amount.ToString(), out amount);

                    int variance = 2;
                    if (last_amount.ContainsKey(key))
                    {
                        if (last_amount[key] < amount) variance = 1;
                        if (last_amount[key] > amount) variance = 3;
                        last_amount[key] = amount;
                    }
                    else
                    {
                        variance = 1;
                        last_amount.Add(key, amount);
                    }

                    items.Add(new Item()
                    {
                        Name = iName,
                        Type = iType,
                        Amount = amount,
                        Variance = variance
                    });
                    loop++;
                }
            }
        }
        last_machine_amount[block.EntityId] = last_amount;
        return items;
    }
    public void DrawMachine(Drawing drawing, Vector2 position, IMyProductionBlock block, List<Item> items, Style style)
    {
        float size_icon = style.Height - 10;
        Color color_title = new Color(100, 100, 100, 128);
        Color color_text = new Color(100, 100, 100, 255);
        float RotationOrScale = 0.5f;
        float cell_spacing = 10f;

        float form_width = style.Width - 5;
        float form_height = style.Height - 5;

        string colorDefault = DisplayLcd.program.MyProperty.Get("color", "default");

        float x = 0f;

        drawing.AddForm(position + new Vector2(0, 0), SpriteForm.SquareSimple, form_width, form_height, new Color(5, 5, 5, 125));

        foreach (Item item in items)
        {

            // icon
            drawing.AddSprite(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = item.Icon,
                Size = new Vector2(size_icon, size_icon),
                Color = DisplayLcd.program.MyProperty.GetColor("color", item.Name, colorDefault),
                Position = position + new Vector2(x, size_icon / 2 + cell_spacing)

            });

            if (drawing.Symbol.Keys.Contains(item.Name))
            {
                // symbol
                Vector2 positionSymbol = position + new Vector2(x, 20);
                drawing.AddForm(positionSymbol, SpriteForm.SquareSimple, size_icon, 15f, new Color(10, 10, 10, 200));
                drawing.AddSprite(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = drawing.Symbol[item.Name],
                    Color = color_text,
                    Position = positionSymbol,
                    RotationOrScale = RotationOrScale,
                    FontId = drawing.Font,
                    Alignment = TextAlignment.LEFT
                });
            }

            // Quantity
            Vector2 positionQuantity = position + new Vector2(x, size_icon - 12);
            Color mask_color = new Color(0, 0, 20, 200);
            if (item.Variance == 2) mask_color = new Color(20, 0, 0, 200);
            if (item.Variance == 3) mask_color = new Color(0, 20, 0, 200);
            drawing.AddForm(positionQuantity, SpriteForm.SquareSimple, size_icon, 15f, mask_color);
            drawing.AddSprite(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = Util.GetKiloFormat(item.Amount),
                Color = color_text,
                Position = positionQuantity,
                RotationOrScale = RotationOrScale,
                FontId = drawing.Font,
                Alignment = TextAlignment.LEFT
            });
            x += style.Height;
        }

        // Element Name
        MySprite icon = new MySprite()
        {
            Type = SpriteType.TEXT,
            Data = Util.CutString(block.CustomName, string_len),
            Color = color_title,
            Position = position + new Vector2(style.Margin.X, 0),
            RotationOrScale = 0.6f,
            FontId = drawing.Font,
            Alignment = TextAlignment.LEFT

        };
        drawing.AddSprite(icon);
    }
}

public class DisplayPower
{
    protected DisplayLcd DisplayLcd;

    private MyDefinitionId PowerDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

    private bool enable = false;
    public DisplayPower(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }
    public void Load(MyIni MyIni)
    {
        enable = MyIni.Get("Power", "on").ToBoolean(false);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Power", "on", enable);
    }
    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        BlockSystem<IMyTerminalBlock> producers = BlockSystem<IMyTerminalBlock>.SearchBlocks(DisplayLcd.program, block => block.Components.Has<MyResourceSourceComponent>());
        BlockSystem<IMyTerminalBlock> consummers = BlockSystem<IMyTerminalBlock>.SearchBlocks(DisplayLcd.program, block => block.Components.Has<MyResourceSinkComponent>());
        Dictionary<string, Power> outputs = new Dictionary<string, Power>();
        Power batteries_store = new Power() { Type = "Batteries" };
        outputs.Add("all", new Power() { Type = "All" });
        float current_input = 0f;
        float max_input = 0f;
        float width = 30f;
        StyleGauge style = new StyleGauge()
        {
            Orientation = SpriteOrientation.Horizontal,
            Fullscreen = true,
            Width = width,
            Height = width,
            Padding = new StylePadding(0),
            Round = false,
            RotationOrScale = 0.5f
        };

        MySprite text = new MySprite()
        {
            Type = SpriteType.TEXT,
            Color = Color.DimGray,
            Position = position + new Vector2(0, 0),
            RotationOrScale = .6f,
            FontId = drawing.Font,
            Alignment = TextAlignment.LEFT

        };

        producers.ForEach(delegate (IMyTerminalBlock block)
        {
            string type = block.BlockDefinition.SubtypeName;
            if (block is IMyBatteryBlock)
            {
                IMyBatteryBlock battery = (IMyBatteryBlock)block;
                batteries_store.AddCurrent(battery.CurrentStoredPower);
                batteries_store.AddMax(battery.MaxStoredPower);
            }
            else
            {
                MyResourceSourceComponent resourceSource;
                block.Components.TryGet<MyResourceSourceComponent>(out resourceSource);
                if (resourceSource != null)
                {
                    ListReader<MyDefinitionId> myDefinitionIds = resourceSource.ResourceTypes;
                    if (myDefinitionIds.Contains(PowerDefinitionId))
                    {
                        Power global_output = outputs["all"];
                        global_output.AddCurrent(resourceSource.CurrentOutputByType(PowerDefinitionId));
                        global_output.AddMax(resourceSource.MaxOutputByType(PowerDefinitionId));

                        if (!outputs.ContainsKey(type)) outputs.Add(type, new Power() { Type = type });
                        Power current_output = outputs[type];
                        current_output.AddCurrent(resourceSource.CurrentOutputByType(PowerDefinitionId));
                        current_output.AddMax(resourceSource.MaxOutputByType(PowerDefinitionId));
                    }
                }
            }
        });

        drawing.DrawGauge(position, outputs["all"].Current, outputs["all"].Max, style);

        foreach (KeyValuePair<string, Power> kvp in outputs)
        {
            string title = kvp.Key;

            position += new Vector2(0, 40);
            if (kvp.Key.Equals("all"))
            {
                text.Data = $"Global Generator\n Out: {Math.Round(kvp.Value.Current, 2)}MW / {Math.Round(kvp.Value.Max, 2)}MW";
            }
            else
            {
                text.Data = $"{title} (n={kvp.Value.Count})\n";
                text.Data += $" Out: {Math.Round(kvp.Value.Current, 2)}MW / {Math.Round(kvp.Value.Max, 2)}MW";
                text.Data += $" (Moy={kvp.Value.Moyen}MW)";
            }
            text.Position = position;
            drawing.AddSprite(text);
        }

        position += new Vector2(0, 40);
        drawing.DrawGauge(position, batteries_store.Current, batteries_store.Max, style, true);
        position += new Vector2(0, 40);
        text.Data = $"Battery Store (n={batteries_store.Count})\n Store: {Math.Round(batteries_store.Current, 2)}MW / {Math.Round(batteries_store.Max, 2)}MW";
        text.Position = position;
        drawing.AddSprite(text);

        consummers.ForEach(delegate (IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
            {
            }
            else
            {
                MyResourceSinkComponent resourceSink;
                block.Components.TryGet<MyResourceSinkComponent>(out resourceSink);
                if (resourceSink != null)
                {
                    ListReader<MyDefinitionId> myDefinitionIds = resourceSink.AcceptedResources;
                    if (myDefinitionIds.Contains(PowerDefinitionId))
                    {
                        max_input += resourceSink.RequiredInputByType(PowerDefinitionId);
                        current_input += resourceSink.CurrentInputByType(PowerDefinitionId);
                    }
                }
            }
        });
        position += new Vector2(0, 40);
        drawing.DrawGauge(position, current_input, max_input, style, true);
        position += new Vector2(0, 40);
        text.Data = $"Power In: {Math.Round(current_input, 2)}MW / {Math.Round(max_input, 2)}MW";
        text.Position = position;
        drawing.AddSprite(text);

        return position + new Vector2(0, 60);
    }
}

public class Power
{
    public string Type;
    public float Current = 0f;
    public float Max = 0f;
    public int Count = 0;

    public void AddCurrent(float value)
    {
        Current += value;
        Count += 1;
    }
    public void AddMax(float value)
    {
        Max += value;
    }
    public double Moyen
    {
        get
        {
            return Math.Round(Current / Count, 2);
        }
    }
}

public class DisplayShip
{
    protected DisplayLcd DisplayLcd;

    private bool enable = false;

    public bool search = true;

    private BlockSystem<IMyThrust> thrusts_up = null;
    private BlockSystem<IMyThrust> thrusts_down = null;
    private BlockSystem<IMyThrust> thrusts_left = null;
    private BlockSystem<IMyThrust> thrusts_right = null;
    private BlockSystem<IMyThrust> thrusts_forward = null;
    private BlockSystem<IMyThrust> thrusts_backward = null;
    private BlockSystem<IMyCockpit> cockpit = null;

    public DisplayShip(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }

    public void Load(MyIni MyIni)
    {
        enable = MyIni.Get("Ship", "on").ToBoolean(false);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Ship", "on", enable);
    }
    private void Search()
    {
        cockpit = BlockSystem<IMyCockpit>.SearchBlocks(DisplayLcd.program);
        thrusts_up = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Up");
        thrusts_down = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Down");
        thrusts_left = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Left");
        thrusts_right = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Right");
        thrusts_forward = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Forward");
        thrusts_backward = BlockSystem<IMyThrust>.SearchByGroup(DisplayLcd.program, "Thrusters Backward");

        search = false;
    }
    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        if (search) Search();

        float force = 0f;
        float mass = 0f;
        if (!cockpit.IsEmpty)
        {
            MyShipMass shipMass = cockpit.First.CalculateShipMass();
            mass = shipMass.TotalMass;
        }
        string direction = "none";

        Dictionary<string, float> forces = new Dictionary<string, float>();
        thrusts_up.ForEach(delegate (IMyThrust block)
        {
            direction = "Up";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        thrusts_down.ForEach(delegate (IMyThrust block)
        {
            direction = "Down";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        thrusts_left.ForEach(delegate (IMyThrust block)
        {
            direction = "Left";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        thrusts_right.ForEach(delegate (IMyThrust block)
        {
            direction = "Right";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        thrusts_forward.ForEach(delegate (IMyThrust block)
        {
            direction = "Forward";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        thrusts_backward.ForEach(delegate (IMyThrust block)
        {
            direction = "Backward";
            if (forces.ContainsKey(direction)) forces[direction] += block.MaxThrust;
            else forces.Add(direction, block.MaxThrust);
        });
        MySprite text = new MySprite()
        {
            Type = SpriteType.TEXT,
            Color = Color.DimGray,
            Position = position + new Vector2(0, 0),
            RotationOrScale = 1f,
            FontId = drawing.Font,
            Alignment = TextAlignment.LEFT

        };
        // Up
        force = 0f;
        forces.TryGetValue("Up", out force);
        text.Data = $"Up: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        drawing.AddSprite(text);
        // Down
        position += new Vector2(0, 40);
        force = 0f;
        forces.TryGetValue("Down", out force);
        text.Data = $"Down: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        text.Position = position;
        drawing.AddSprite(text);
        // Forward
        position += new Vector2(0, 40);
        force = 0f;
        forces.TryGetValue("Forward", out force);
        text.Data = $"Forward: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        text.Position = position;
        drawing.AddSprite(text);
        // Backward
        position += new Vector2(0, 40);
        force = 0f;
        forces.TryGetValue("Backward", out force);
        text.Data = $"Backward: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        text.Position = position;
        drawing.AddSprite(text);
        // Right
        position += new Vector2(0, 40);
        force = 0f;
        forces.TryGetValue("Right", out force);
        text.Data = $"Right: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        text.Position = position;
        drawing.AddSprite(text);
        // Left
        position += new Vector2(0, 40);
        force = 0f;
        forces.TryGetValue("Left", out force);
        text.Data = $"Left: {force / 1000}kN / {Math.Round(force / mass, 1)}m/s²";
        text.Position = position;
        drawing.AddSprite(text);

        position += new Vector2(0, 40);

        return position;
    }
}

public class DisplayTank
{
    protected DisplayLcd DisplayLcd;

    private bool enable = false;

    private bool tank_h2 = false;
    private bool tank_o2 = false;
    public DisplayTank(DisplayLcd DisplayLcd)
    {
        this.DisplayLcd = DisplayLcd;
    }

    public void Load(MyIni MyIni)
    {
        enable = MyIni.Get("Tank", "on").ToBoolean(false);
        tank_h2 = MyIni.Get("Tank", "H2").ToBoolean(false);
        tank_o2 = MyIni.Get("Tank", "O2").ToBoolean(false);
    }

    public void Save(MyIni MyIni)
    {
        MyIni.Set("Tank", "on", enable);
        MyIni.Set("Tank", "H2", tank_h2);
        MyIni.Set("Tank", "O2", tank_o2);
    }
    public Vector2 Draw(Drawing drawing, Vector2 position)
    {
        if (!enable) return position;
        List<string> types = new List<string>();
        if (tank_h2) types.Add("Hydrogen");
        if (tank_o2) types.Add("Oxygen");
        if (types.Count > 0)
        {
            foreach (string type in types)
            {
                BlockSystem<IMyGasTank> tanks = BlockSystem<IMyGasTank>.SearchBlocks(DisplayLcd.program, block => block.BlockDefinition.SubtypeName.Contains(type));
                float volumes = 0f;
                float capacity = 0f;
                float width = 50f;
                StyleGauge style = new StyleGauge()
                {
                    Orientation = SpriteOrientation.Horizontal,
                    Fullscreen = true,
                    Width = width,
                    Height = width,
                    Padding = new StylePadding(0),
                    Round = false,
                    RotationOrScale = 0.5f
                };

                MySprite text = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Color = Color.DimGray,
                    Position = position + new Vector2(0, 0),
                    RotationOrScale = 1f,
                    FontId = drawing.Font,
                    Alignment = TextAlignment.LEFT

                };

                tanks.ForEach(delegate (IMyGasTank block)
                {
                    volumes += (float)block.FilledRatio * block.Capacity;
                    capacity += block.Capacity;
                });

                drawing.DrawGauge(position, volumes, capacity, style);
                position += new Vector2(0, 60);
                switch (type)
                {
                    case "Hydrogen":
                        text.Data = $"H2: {Math.Round(volumes, 2)}L / {Math.Round(capacity, 2)}L";
                        break;
                    case "Oxygen":
                        text.Data = $"O2: {Math.Round(volumes, 2)}L / {Math.Round(capacity, 2)}L";
                        break;
                }
                text.Position = position;
                drawing.AddSprite(text);
                position += new Vector2(0, 60);
            }
        }
        return position;
    }
}


public class Drawing
{
    public float Padding_x = 10f;
    public float Padding_y = 10f;
    public string Font = "Monospace";

    private IMyTextPanel surfaceProvider;
    private MySpriteDrawFrame frame;
    public RectangleF viewport;

    private MySprite icon;

    public Dictionary<string, string> Symbol = new Dictionary<string, string>();

    public Drawing(IMyTextPanel lcd)
    {
        surfaceProvider = lcd;
        Initialize();
    }

    private void Initialize()
    {
        // Set the sprite display mode
        surfaceProvider.ContentType = ContentType.SCRIPT;
        // Make sure no built-in script has been selected
        surfaceProvider.Script = "";
        // Calculate the viewport by centering the surface size onto the texture size
        this.viewport = new RectangleF((surfaceProvider.TextureSize - surfaceProvider.SurfaceSize) / 2f, surfaceProvider.SurfaceSize);
        // Retrieve the Large Display, which is the first surface
        this.frame = surfaceProvider.DrawFrame();
        Symbol.Add("Cobalt", "Co");
        Symbol.Add("Nickel", "Ni");
        Symbol.Add("Magnesium", "Mg");
        Symbol.Add("Platinum", "Pt");
        Symbol.Add("Iron", "Fe");
        Symbol.Add("Gold", "Au");
        Symbol.Add("Silicon", "Si");
        Symbol.Add("Silver", "Ag");
        Symbol.Add("Stone", "Stone");
        Symbol.Add("Uranium", "U");
        Symbol.Add("Ice", "Ice");
    }

    public void Dispose()
    {
        // We are done with the frame, send all the sprites to the text panel
        this.frame.Dispose();
    }
    public void Clean()
    {
        AddForm(new Vector2(), SpriteForm.SquareSimple, viewport.Width, viewport.Height, Color.Black);
    }

    public MySprite AddSprite(MySprite sprite)
    {
        frame.Add(sprite);
        return sprite;
    }

    public MySprite AddForm(Vector2 position, SpriteForm form, float width, float height, Color color)
    {
        return AddSprite(new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = form.ToString(),
            Size = new Vector2(width, height),
            Color = color,
            Position = position + new Vector2(0, height/2)

        });
    }

    public MySprite AddSprite(SpriteType type = SpriteType.TEXTURE, string data = null, Vector2? position = null, Vector2? size = null, Color? color = null, string fontId = null, TextAlignment alignment = TextAlignment.LEFT, float rotation = 0)
    {
        MySprite sprite = new MySprite(type, data, position, size, color, fontId, alignment, rotation);
        // Add the sprite to the frame
        frame.Add(sprite);
        return sprite;
    }

    public void DrawGaugeIcon(Vector2 position, string name, double amount, int limit, StyleIcon style_icon, int variance = 0)
    {
        Vector2 position2 = position + new Vector2(style_icon.Padding.X, style_icon.Padding.Y);
        // cadre info
        //AddForm(position2, SpriteForm.SquareSimple, style_icon.Width, style_icon.Height, new Color(40, 40, 40, 128));

        float width = (style_icon.Width - 3 * style_icon.Margin.X) / 3;
        float fontTitle = Math.Max(0.3f, (float)Math.Round(0.5f * (style_icon.Height / 80f), 1));
        float fontQuantity = Math.Max(0.5f, (float)Math.Round(1f * (style_icon.Height / 80f), 1));
        // Icon
        AddSprite(new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = style_icon.path,
            Size = new Vector2(width, width),
            Color = style_icon.Color,
            Position = position2 + new Vector2(0, width / 2)

        });

        StyleGauge style = new StyleGauge()
        {
            Orientation = SpriteOrientation.Horizontal,
            Fullscreen = false,
            Width = width * 2,
            Height = width / 3,
            Padding = new StylePadding(0),
            RotationOrScale = Math.Max(0.3f, (float)Math.Round(0.6f * (style_icon.Height / 80f), 1))
        };
        DrawGauge(position2 + new Vector2(width + style_icon.Margin.X, style_icon.Height / 2), (float)amount, limit, style);

        // Element Name
        icon = new MySprite()
        {
            Type = SpriteType.TEXT,
            Data = name,
            Size = new Vector2(width, width),
            Color = Color.DimGray,
            Position = position2 + new Vector2(style_icon.Margin.X, -8),
            RotationOrScale = fontTitle,
            FontId = Font,
            Alignment = TextAlignment.LEFT

        };
        AddSprite(icon);
        // Quantity
        icon = new MySprite()
        {
            Type = SpriteType.TEXT,
            Data = Util.GetKiloFormat(amount),
            Size = new Vector2(width, width),
            Color = Color.LightGray,
            Position = position2 + new Vector2(width + style_icon.Margin.X, style_icon.Margin.Y),
            RotationOrScale = fontQuantity,
            FontId = Font

        };
        AddSprite(icon);

        float symbolSize = 20f;
        if (variance == 1)
        {
            AddSprite(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = SpriteForm.Triangle.ToString(),
                Size = new Vector2(symbolSize, symbolSize),
                Color = new Color(0,100,0,255),
                Position = position2 + new Vector2(3 * width - 25, symbolSize - style_icon.Margin.Y),
                RotationOrScale = 0
            });
        }
        if (variance == 3)
        {
            AddSprite(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = SpriteForm.Triangle.ToString(),
                Size = new Vector2(symbolSize, symbolSize),
                Color = new Color(100, 0, 0, 255),
                Position = position2 + new Vector2(3 * width - 25, symbolSize + style_icon.Margin.Y),
                RotationOrScale = (float)Math.PI
            });
        }
    }

    public void DrawGauge(Vector2 position, float amount, float limit, StyleGauge style, bool invert = false)
    {
        float width = style.Width;
        float height = style.Height;

        if (style.Fullscreen && style.Orientation.Equals(SpriteOrientation.Horizontal)) width = viewport.Width;
        if (style.Fullscreen && style.Orientation.Equals(SpriteOrientation.Vertical)) height = viewport.Height;

        width += - 2 * style.Padding.X;
        height += - 2 * style.Padding.X;
        Vector2 position2 = position + new Vector2(style.Padding.X, style.Padding.Y);
        // Gauge
        AddForm(position2, SpriteForm.SquareSimple, width, height, style.Color);
        // Gauge Interrior
        AddForm(position2 + new Vector2(style.Margin.X, style.Margin.Y), SpriteForm.SquareSimple, width - 2 * style.Margin.X, height - 2 * style.Margin.Y, new Color(20, 20, 20, 255));

        // Gauge quantity
        float percent = Math.Min(1f, amount / limit);
        Color color = Color.Green;
        if (percent > 0.5 && !invert) color = new Color(180, 130, 0, 128);
        if (percent > 0.75 && !invert) color = new Color(180, 0, 0, 128);

        if (percent < 0.5 && invert) color = new Color(180, 130, 0, 128);
        if (percent < 0.25 && invert) color = new Color(180, 0, 0, 128);

        if (style.Orientation.Equals(SpriteOrientation.Horizontal))
        {
            float width2 = width - 2 * style.Margin.X;
            float height2 = height - 2 * style.Margin.Y;
            float length = width2 * percent;
            AddForm(position2 + new Vector2(style.Margin.X, style.Margin.Y), SpriteForm.SquareSimple, length, height2, color);
        }
        else
        {
            float width2 = width - 2 * style.Margin.X;
            float height2 = height - 2 * style.Margin.Y;
            float length = height2 * percent;
            AddForm(position2 + new Vector2(style.Margin.X, height2 - length + style.Margin.Y), SpriteForm.SquareSimple, width2, length, color);
        }
        if (style.Percent)
        {
            string data = $"{percent:P0}";
            if (percent < 0.999 && style.Round) data = $"{percent:P1}";
            // Tag
            icon = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = data,
                Size = new Vector2(width, width),
                Color = Color.Black,
                Position = position2 + new Vector2(2 * style.Margin.X, style.Margin.Y),
                RotationOrScale = style.RotationOrScale,
                FontId = Font,
                Alignment = TextAlignment.LEFT

            };
            AddSprite(icon);
        }
    }
    public void Test()
    {
        MySprite icon;
        //Sandbox.ModAPI.Ingame.IMyTextSurface#GetSprites
        //Gets a list of available sprites
        var names = new List<string>();
        this.surfaceProvider.GetSprites(names);
        int count = -1;
        float width = 40;
        bool auto = false;
        if (auto)
        {
            float delta = 100 - 4 * (viewport.Width - 100) * viewport.Height / names.Count;
            width = (-10 + (float)Math.Sqrt(Math.Abs(delta))) / 2f;
        }
        float height = width + 10f;
        int limit = (int)Math.Floor(viewport.Height/height);
        Vector2 position = new Vector2(0, 0);

        foreach (string name in names)
        {
            //logger.Debug(String.Format("Sprite {0}", name));
            count++;
            Vector2 position2 = position + new Vector2(width * (count / limit), height * (count - (count / limit) * limit));
            icon = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = name,
                Size = new Vector2(width, width),
                Color = Color.White,
                Position = position2 + new Vector2(0, height/2+10/2),

            };
            this.frame.Add(icon);
            icon = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = count.ToString(),
                Size = new Vector2(width, width),
                RotationOrScale = 0.3f,
                Color = Color.Gray,
                Position = position2 + new Vector2(0, 0),
                FontId = Font
            };
            this.frame.Add(icon);
        }
    }
}

public enum SpriteForm
{
    SquareSimple,
    SquareHollow,
    Circle,
    Triangle
}

public enum SpriteOrientation
{
    Horizontal,
    Vertical
}

public class StylePadding
{
    public StylePadding(int x = 2, int y = 2)
    {
        X = x;
        Y = y;
    }
    public StylePadding(int value)
    {
        X = value;
        Y = value;
    }

    public int X = 2;
    public int Y = 2;
}

public class StyleMargin : StylePadding
{
    public StyleMargin(int x = 2, int y = 2)
    {
        X = x;
        Y = y;
    }
    public StyleMargin(int value)
    {
        X = value;
        Y = value;
    }
}
public class Style
{
    public StylePadding Padding = new StylePadding(2);
    public StyleMargin Margin = new StyleMargin(2);
    public float Width = 50f;
    public float Height = 50f;
    public Color Color = new Color(100, 100, 100, 128);
}
public class StyleIcon : Style
{
    public string path;
}
public class StyleGauge : Style
{
    public SpriteOrientation Orientation = SpriteOrientation.Horizontal;
    public bool Fullscreen = false;
    public bool Percent = true;
    public bool Round = true;
    public float RotationOrScale = 0.6f;
}

public class Item : IComparable<Item>
{
    public const string TYPE_ORE = "MyObjectBuilder_Ore";
    public const string TYPE_INGOT = "MyObjectBuilder_Ingot";
    public const string TYPE_COMPONENT = "MyObjectBuilder_Component";
    public const string TYPE_AMMO = "MyObjectBuilder_AmmoMagazine";

    public string Name;
    public string Type;
    public Double Amount;
    public int Variance;

    public string Icon
    {
        get
        {
            return String.Format("{0}/{1}", Type, Name);
        }
    }

    public int CompareTo(Item other)
    {
        return Amount.CompareTo(other.Amount);
    }
}

class BlockComparer : IComparer<IMyTerminalBlock>
{
    public int Compare(IMyTerminalBlock block1, IMyTerminalBlock block2)
    {
        return block1.CustomName.CompareTo(block2.CustomName);
    }
}

public class KProperty
{
    protected MyIni MyIni = new MyIni();
    protected Program program;

    public string limit_default;
    public string color_default;

    public string lcd_filter = "*";

    public KProperty(Program program)
    {
        this.program = program;
    }

    public void Load()
    {
        MyIniParseResult result;
        if (!MyIni.TryParse(program.Me.CustomData, out result))
            throw new Exception(result.ToString());
        limit_default = MyIni.Get("Limit", "default").ToString("10000");
        color_default = MyIni.Get("Color", "default").ToString("128,128,128,255");

        lcd_filter = MyIni.Get("LCD", "filter").ToString("*");

        if (program.Me.CustomData.Trim().Equals(""))
        {
            Save(true);
        }
    }

    public string Get(string section, string key, string default_value = "")
    {
        return MyIni.Get(section, key).ToString(default_value);
    }

    public int GetInt(string section, string key, int default_value = 0)
    {
        return MyIni.Get(section, key).ToInt32(default_value);
    }

    public Color GetColor(string section, string key, string default_value = null)
    {
        if (default_value == null) default_value = color_default;
        string colorValue = MyIni.Get(section, key).ToString(default_value);
        Color color = Color.Gray;
        // Find matches.
        //program.drawingSurface.WriteText($"{section}/{key}={colorValue}", true);
        if (!colorValue.Equals(""))
        {
            string[] colorSplit = colorValue.Split(',');
            color = new Color(int.Parse(colorSplit[0]), int.Parse(colorSplit[1]), int.Parse(colorSplit[2]), int.Parse(colorSplit[3]));
        }
        return color;
    }

    public void Save(bool prepare = false)
    {
        MyIniParseResult result;
        if (!MyIni.TryParse(program.Me.CustomData, out result))
            throw new Exception(result.ToString());
        MyIni.Set("LCD", "filter", lcd_filter);

        MyIni.Set("Limit", "default", limit_default);
        if (prepare)
        {
            MyIni.Set("Limit", "Cobalt", "1000");
            MyIni.Set("Limit", "Iron", "100000");
            MyIni.Set("Limit", "Gold", "1000");
            MyIni.Set("Limit", "Platinum", "1000");
            MyIni.Set("Limit", "Silver", "1000");
        }
        MyIni.Set("Color", "default", color_default);
        if (prepare)
        {
            MyIni.Set("Color", "Cobalt", "000,080,080,255");
            MyIni.Set("Color", "Gold", "255,153,000,255");
            MyIni.Set("Color", "Ice", "040,130,130,255");
            MyIni.Set("Color", "Iron", "040,040,040,255");
            MyIni.Set("Color", "Nickel", "110,080,080,255");
            MyIni.Set("Color", "Platinum", "120,150,120,255");
            MyIni.Set("Color", "Silicon", "150,150,150,255");
            MyIni.Set("Color", "Silver", "120,120,150,255");
            MyIni.Set("Color", "Stone", "120,040,000,200");
            MyIni.Set("Color", "Uranium", "040,130,000,200");
        }
        program.Me.CustomData = MyIni.ToString();
    }
}

public class Logger
{
    Program myProgram;

    private IMyTextPanel panel;
    private List<string> messages = new List<string>();
    public int level = 0;
    public bool console = false;

    public Logger(Program program, string lcd_name)
    {
        myProgram = program;
        Search(lcd_name);
    }

    private void Search(string name)
    {
        if (myProgram.GridTerminalSystem == null) return;
        panel = (IMyTextPanel)myProgram.GridTerminalSystem.GetBlockWithName(name);
        if (panel != null)
        {
            panel.FontSize = 0.75f;
            panel.Font = "Monospace";
            panel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
        }
    }

    public void Console(string message)
    {
        if (console) myProgram.Echo(message);
    }

    public void Info(string message)
    {
        if (level >= 1) Print("INFO", message);
    }

    public void Debug(string message)
    {
        if (level >= 2) Print("DEBUG", message);
    }

    public void Trace(string message)
    {
        if (level >= 3) Print("TRACE", message);
    }

    private void Print(string mode, string message)
    {
        if (panel != null)
        {
            messages.Add(String.Join("", String.Format("{0:0.00}", myProgram.Runtime.LastRunTimeMs), " [", mode, "] ", message, "\n"));
            if (messages.Count > 20) messages.RemoveAt(0);
            panel.WriteText("", false);
            foreach (string content in messages)
            {
                panel.WriteText(content, true);
            }
        }


    }
}

public class ParserInfo
{
    protected Program program;

    private BlockSystem<IMyTextPanel> lcds = null;

    readonly private string lcd_filter = "C:Parser";
    public ParserInfo(Program program)
    {
        this.program = program;
        Search();
    }

    private void Search()
    {
        BlockFilter<IMyTextPanel> block_filter = BlockFilter<IMyTextPanel>.Create(program.Me, lcd_filter);
        lcds = BlockSystem<IMyTextPanel>.SearchByFilter(program, block_filter);
    }

    public void WriteText(string message, bool append)
    {
        message += "\n";
        if (lcds != null) lcds.ForEach(delegate (IMyTextPanel block)
        {
            block.WriteText(message, append);
        });
    }
    public void ParserTitle(IMyTerminalBlock block)
    {
        WriteText($"{block.BlockDefinition.SubtypeId}:{block.CustomName}", false);
    }

    public void ParserCockpit(IMyCockpit block)
    {
        WriteText($"=== Cockpit Info ===", true);
        WriteText($"OxygenCapacity={block.OxygenCapacity}", true);
        WriteText($"OxygenFilledRatio={block.OxygenFilledRatio}", true);
    }
    public void ParserMotorSuspension(IMyMotorSuspension block)
    {
        WriteText($"=== Motor Suspension Info ===", true);
        WriteText($"Steering={block.Steering}", true);
        WriteText($"Propulsion={block.Propulsion}", true);
        WriteText($"InvertSteer={block.InvertSteer}", true);
        WriteText($"InvertPropulsion={block.InvertPropulsion}", true);
        WriteText($"Strength={block.Strength}", true);
        WriteText($"Friction={block.Friction}", true);
        WriteText($"Power={block.Power}", true);
        WriteText($"Height={block.Height}", true);
        WriteText($"SteerAngle={block.SteerAngle}", true);
        WriteText($"MaxSteerAngle={block.MaxSteerAngle}", true);
        WriteText($"Brake={block.Brake}", true);
        WriteText($"AirShockEnabled={block.AirShockEnabled}", true);
    }
    public void ParserShipController(IMyShipController block)
    {
        WriteText($"HasWheels={block.HasWheels}", true);
        WriteText($"RollIndicator={block.RollIndicator}", true);
        WriteText($"RotationIndicator={block.RotationIndicator}", true);
        WriteText($"MoveIndicator={block.MoveIndicator}", true);
        WriteText($"ShowHorizonIndicator={block.ShowHorizonIndicator}", true);
        WriteText($"DampenersOverride={block.DampenersOverride}", true);
        WriteText($"HandBrake={block.HandBrake}", true);
        WriteText($"ControlThrusters={block.ControlThrusters}", true);
        WriteText($"ControlWheels={block.ControlWheels}", true);
        WriteText($"CenterOfMass={block.CenterOfMass}", true);
        WriteText($"IsMainCockpit={block.IsMainCockpit}", true);
        WriteText($"CanControlShip={block.CanControlShip}", true);
        WriteText($"IsUnderControl={block.IsUnderControl}", true);

        WriteText($"CalculateShipMass={block.CalculateShipMass()}", true);
        WriteText($"GetArtificialGravity={block.GetArtificialGravity()}", true);
        WriteText($"GetNaturalGravity={block.GetNaturalGravity()}", true);
        WriteText($"GetShipSpeed={block.GetShipSpeed()}", true);
        WriteText($"GetShipVelocities={block.GetShipVelocities()}", true);
        WriteText($"GetTotalGravity={block.GetTotalGravity()}", true);
    }
    public void ParserThrust(IMyThrust block)
    {
        WriteText($"=== Thrust Info ===", true);
        WriteText($"ThrustOverride={block.ThrustOverride}", true);
        WriteText($"ThrustOverridePercentage={block.ThrustOverridePercentage}", true);
        WriteText($"MaxThrust={block.MaxThrust}", true);
        WriteText($"MaxEffectiveThrust={block.MaxEffectiveThrust}", true);
        WriteText($"CurrentThrust={block.CurrentThrust}", true);
        WriteText($"GridThrustDirection={block.GridThrustDirection}", true);
    }

    public void ParserProperty(IMyTerminalBlock block)
    {
        WriteText($"=== Properties Info ===", true);
        List<ITerminalProperty> propertyList = new List<ITerminalProperty>();
        block.GetProperties(propertyList, null);
        propertyList.ForEach(delegate (ITerminalProperty property) {
            if (property.Is<float>()) WriteText($"{property.Id}={block.GetValueFloat(property.Id)}", true);
            if (property.Is<bool>()) WriteText($"{property.Id}={block.GetValueBool(property.Id)}", true);
        });
    }

    public void ParserTerminalBlock(IMyTerminalBlock block)
    {
        WriteText($"=== TerminalBlock Info ===", true);
        WriteText($"ShowInInventory={block.ShowInInventory}", true);
        WriteText($"ShowInTerminal={block.ShowInTerminal}", true);
        WriteText($"ShowOnHUD={block.ShowOnHUD}", true);
        WriteText($"CustomData={block.CustomData}", true);
        WriteText($"CustomInfo={block.CustomInfo}", true);
        WriteText($"ShowInToolbarConfig={block.ShowInToolbarConfig}", true);
        WriteText($"CustomName={block.CustomName}", true);
        WriteText($"CustomNameWithFaction={block.CustomNameWithFaction}", true);
        WriteText($"DetailedInfo={block.DetailedInfo}", true);
    }
    public void ParserEntity(IMyEntity block)
    {
        WriteText($"=== Entity Info ===", true);
        WriteText($"EntityId={block.EntityId}", true);
        WriteText($"Name={block.Name}", true);
        WriteText($"DisplayName={block.DisplayName}", true);
        WriteText($"HasInventory={block.HasInventory}", true);
        WriteText($"InventoryCount={block.InventoryCount}", true);
        WriteText($"WorldAABB={block.WorldAABB}", true);
        WriteText($"WorldAABBHr={block.WorldAABBHr}", true);
        WriteText($"WorldMatrix={block.WorldMatrix}", true);
        WriteText($"WorldVolume={block.WorldVolume}", true);
        WriteText($"WorldVolumeHr={block.WorldVolumeHr}", true);
    }

    public void ParserCubeBlock(IMyCubeBlock block)
    {
        WriteText($"=== CubeBlock Info ===", true);
        WriteText($"DisplayNameText={block.DisplayNameText}", true);
        WriteText($"Orientation={block.Orientation}", true);
        WriteText($"NumberInGrid={block.NumberInGrid}", true);
        WriteText($"Min={block.Min}", true);
        WriteText($"Mass={block.Mass}", true);
        WriteText($"Max={block.Max}", true);
        WriteText($"IsWorking={block.IsWorking}", true);
        WriteText($"IsFunctional={block.IsFunctional}", true);
        WriteText($"IsBeingHacked={block.IsBeingHacked}", true);
        WriteText($"OwnerId={block.OwnerId}", true);
        WriteText($"Position={block.Position}", true);
        WriteText($"DefinitionDisplayNameText={block.DefinitionDisplayNameText}", true);
        WriteText($"CubeGrid={block.CubeGrid}", true);
        WriteText($"BlockDefinition={block.BlockDefinition}", true);
        WriteText($"DisassembleRatio={block.DisassembleRatio}", true);
    }
}

public class ThrusterSystem
{
    protected Program program;

    private BlockSystem<IMyThrust> thrusters;

    private string thrusters_filter;
    public ThrusterSystem(Program program)
    {
        this.program = program;
        Search();
    }

    private void Search()
    {
        BlockFilter<IMyThrust> block_thrusters_filter = BlockFilter<IMyThrust>.Create(program.Me, thrusters_filter);
        thrusters = BlockSystem<IMyThrust>.SearchByFilter(program, block_thrusters_filter);
    }
}

public class Util
{
    static public string GetKiloFormat(double value)
    {
        double pow = 1.0;
        string suffix = "";
        if (value > 1000.0)
        {
            int y = int.Parse(Math.Floor(Math.Log10(value) / 3).ToString());
            suffix = "KMGTPEZY".Substring(y - 1, 1);
            pow = Math.Pow(10, y * 3);
        }
        return String.Format("{0:0.0}{1}", (value / pow), suffix);

    }

    static public double RadToDeg(float angle)
    {
        return angle * 180 / Math.PI;
    }
    static public double DegToRad(float angle)
    {
        return angle * Math.PI / 180;
    }
    static public string GetType(MyInventoryItem inventory_item)
    {
        return inventory_item.Type.TypeId;
    }

    static public string GetName(MyInventoryItem inventory_item)
    {
        return inventory_item.Type.SubtypeId;
    }
    static public string GetType(MyProductionItem production_item)
    {
        MyDefinitionId itemDefinitionId;
        string subtypeName = production_item.BlueprintId.SubtypeName;
        string typeName = Util.GetName(production_item);
        if ((subtypeName.EndsWith("Rifle") || subtypeName.StartsWith("Welder") || subtypeName.StartsWith("HandDrill") || subtypeName.StartsWith("AngleGrinder"))
            && MyDefinitionId.TryParse("MyObjectBuilder_PhysicalGunObject", typeName, out itemDefinitionId)) return itemDefinitionId.TypeId.ToString();
        if (subtypeName.StartsWith("Hydrogen") && MyDefinitionId.TryParse("MyObjectBuilder_GasContainerObject", typeName, out itemDefinitionId)) return itemDefinitionId.TypeId.ToString();
        if (subtypeName.StartsWith("Oxygen") && MyDefinitionId.TryParse("MyObjectBuilder_OxygenContainerObject", typeName, out itemDefinitionId)) return itemDefinitionId.TypeId.ToString();
        if ((subtypeName.Contains("Missile") || subtypeName.EndsWith("Magazine")) && MyDefinitionId.TryParse("MyObjectBuilder_AmmoMagazine", typeName, out itemDefinitionId)) return itemDefinitionId.TypeId.ToString();
        if (MyDefinitionId.TryParse("MyObjectBuilder_Component", typeName, out itemDefinitionId)) return itemDefinitionId.TypeId.ToString();
        return production_item.BlueprintId.TypeId.ToString();
    }

    static public string GetName(MyProductionItem production_item)
    {
        string subtypeName = production_item.BlueprintId.SubtypeName;
        if (subtypeName.EndsWith("Component")) subtypeName = subtypeName.Replace("Component", "");
        if (subtypeName.EndsWith("Rifle") || subtypeName.StartsWith("Welder") || subtypeName.StartsWith("HandDrill") || subtypeName.StartsWith("AngleGrinder")) subtypeName = subtypeName + "Item";
        if (subtypeName.EndsWith("Magazine")) subtypeName = subtypeName.Replace("Magazine", "");
        return subtypeName;
    }

    static public string CutString(string value, int limit)
    {
        if(value.Length > limit)
        {
            int len = (limit - 3) / 2;
            return value.Substring(0, len) + "..." + value.Substring(value.Length - len, len);
        }
        return value;
    }
}