﻿namespace pico8_interpreter.Pico8
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Defines the <see cref="PicoInterpreter{G}" />
    /// </summary>
    /// <typeparam name="G"></typeparam>
    public class PicoInterpreter<G>
    {
        /// <summary>
        /// Defines a random object to use in PICO-8 random functions.
        /// </summary>
        public Random random;

        /// <summary>
        /// Defines the time where the cartridge started running (at cartridge load time).
        /// </summary>
        private DateTime timeStart;

        /// <summary>
        /// Defines a list of functions that are used to know if a specific button was pressed or not.
        /// </summary>
        private List<Func<int, bool>> BtnPressedCallbacks;

        // First Players keys
        /// <summary>
        /// Defines the BtnLeft, BtnRight, BtnUp, BtnDown, BtnA, BtnB
        /// </summary>
        private int[] BtnLeft, BtnRight, BtnUp, BtnDown, BtnA, BtnB;

        /// <summary>
        /// Defines the BtnLeftLast, BtnRightLast, BtnUpLast, BtnDownLast, BtnALast, BtnBLast
        /// </summary>
        private bool[] BtnLeftLast, BtnRightLast, BtnUpLast, BtnDownLast, BtnALast, BtnBLast;

        /// <summary>
        /// Defines the BtnLeftCurrent, BtnRightCurrent, BtnUpCurrent, BtnDownCurrent, BtnACurrent, BtnBCurrent
        /// </summary>
        private bool[] BtnLeftCurrent, BtnRightCurrent, BtnUpCurrent, BtnDownCurrent, BtnACurrent, BtnBCurrent;

        /// <summary>
        /// Defines the memory unit
        /// </summary>
        public MemoryUnit memory;

        /// <summary>
        /// Defines the graphics unit
        /// </summary>
        public GraphicsUnit<G> graphics;

        /// <summary>
        /// Defines the <see cref="Game" />
        /// Struct to hold all information about a running pico8 game.
        /// </summary>
        public struct Game
        {
            /// <summary>
            /// Defines the cartridge
            /// </summary>
            public Cartridge cartridge;

            /// <summary>
            /// Defines the interpreter
            /// </summary>
            public ILuaInterpreter interpreter;

            /// <summary>
            /// Defines the path
            /// </summary>
            public string path;

            /// <summary>
            /// Defines the cartdata_id
            /// </summary>
            public string cartdata_id;

            /// <summary>
            /// Defines the cartdata
            /// </summary>
            public Int32[] cartdata;
        }

        /// <summary>
        /// Defines the currently loaded game.
        /// </summary>
        public Game loadedGame;

        /// <summary>
        /// Defines the cartdata folder path.
        /// </summary>
        public string cartdataPath = "cartdata/";

        /// <summary>
        /// Initializes a new instance of the <see cref="PicoInterpreter{G}"/> class.
        /// </summary>
        /// <param name="screenData">The screen color data reference.</param>
        /// <param name="rgbToColor">Function to convert RBG calues to an arbitrary color value.</param>
        public PicoInterpreter(ref G[] screenData, Func<int, int, int, G> rgbToColor)
        {
            random = new Random();
            memory = new MemoryUnit();
            graphics = new GraphicsUnit<G>(ref memory, ref screenData, rgbToColor);

            // Initialie controller variables
            BtnPressedCallbacks = new List<Func<int, bool>>();
            BtnLeft = new int[8];
            BtnRight = new int[8];
            BtnUp = new int[8];
            BtnDown = new int[8];
            BtnA = new int[8];
            BtnB = new int[8];

            BtnLeftLast = new bool[8];
            BtnRightLast = new bool[8];
            BtnUpLast = new bool[8];
            BtnDownLast = new bool[8];
            BtnALast = new bool[8];
            BtnBLast = new bool[8];

            BtnLeftCurrent = new bool[8];
            BtnRightCurrent = new bool[8];
            BtnUpCurrent = new bool[8];
            BtnDownCurrent = new bool[8];
            BtnACurrent = new bool[8];
            BtnBCurrent = new bool[8];

            for (int i = 0; i < 8; i++)
            {
                BtnLeft[i] = -1;
                BtnRight[i] = -1;
                BtnUp[i] = -1;
                BtnDown[i] = -1;
                BtnA[i] = -1;
                BtnB[i] = -1;
            }

            (new FileInfo("cartdata/")).Directory.Create();
        }

        /// <summary>
        /// Initializes the PICO-8 API.
        /// </summary>
        /// <param name="interpreter">The interpreter to use when adding the functions. <see cref="ILuaInterpreter"/></param>
        private void InitAPI(ref ILuaInterpreter interpreter)
        {
            // Graphics
            interpreter.AddFunction("line", (Func<int, int, int?, int?, byte?, object>)graphics.Line);
            interpreter.AddFunction("rect", (Func<int, int, int, int, byte?, object>)graphics.Rect);
            interpreter.AddFunction("rectfill", (Func<int, int, int, int, byte?, object>)graphics.Rectfill);
            interpreter.AddFunction("circ", (Func<int, int, double, byte?, object>)graphics.Circ);
            interpreter.AddFunction("circfill", (Func<int, int, double, byte?, object>)graphics.CircFill);
            interpreter.AddFunction("pset", (Func<int, int, byte?, object>)graphics.Pset);
            interpreter.AddFunction("pget", (Func<int, int, byte>)graphics.Pget);
            interpreter.AddFunction("sset", (Func<int, int, byte?, object>)graphics.Sset);
            interpreter.AddFunction("sget", (Func<int, int, byte>)graphics.Sget);
            interpreter.AddFunction("palt", (Func<int?, bool?, object>)graphics.Palt);
            interpreter.AddFunction("pal", (Func<int?, int?, int, object>)graphics.Pal);
            interpreter.AddFunction("clip", (Func<int?, int?, int?, int?, object>)graphics.Clip);
            interpreter.AddFunction("spr", (Func<int, int, int, int?, int?, bool?, bool?, object>)graphics.Spr);
            interpreter.AddFunction("sspr", (Func<int, int, int, int, int, int, int?, int?, bool?, bool?, object>)graphics.Sspr);
            interpreter.AddFunction("map", (Func<int, int, int, int, int, int, byte?, object>)graphics.Map);
            interpreter.AddFunction("mget", (Func<int, int, byte>)memory.Mget);
            interpreter.AddFunction("mset", (Func<int, int, byte, object>)memory.Mset);
            interpreter.AddFunction("fillp", (Func<double?, object>)memory.Fillp);

            // Memory related
            interpreter.AddFunction("cls", (Func<object>)memory.Cls);
            interpreter.AddFunction("peek", (Func<int, byte>)memory.Peek);
            interpreter.AddFunction("poke", (Func<int, byte, object>)memory.Poke);
            interpreter.AddFunction("peek2", (Func<int, int>)memory.Peek2);
            interpreter.AddFunction("poke2", (Func<int, int, object>)memory.Poke2);
            interpreter.AddFunction("peek4", (Func<int, double>)memory.Peek4);
            interpreter.AddFunction("poke4", (Func<int, double, object>)memory.Poke4);
            interpreter.AddFunction("fget", (Func<int, byte?, object>)memory.Fget);
            interpreter.AddFunction("fset", (Func<int, byte?, bool?, object>)memory.Fset);
            interpreter.AddFunction("camera", (Func<int?, int?, object>)memory.Camera);
            interpreter.AddFunction("memcpy", (Func<int, int, int, object>)memory.Memcpy);
            interpreter.AddFunction("memset", (Func<int, byte, int, object>)memory.Memset);
            interpreter.AddFunction("reload", (Func<int, int, int, string, object>)Reload);
            interpreter.AddFunction("cstore", (Func<int, int, int, string, object>)Cstore);
            interpreter.AddFunction("cartdata", (Action<string>)Cartdata);
            interpreter.AddFunction("dget", (Func<int, object>)Dget);
            interpreter.AddFunction("dset", (Action<int, double>)Dset);
            interpreter.AddFunction("color", (Func<byte, object>)memory.Color);

            // Math
            interpreter.AddFunction("max", (Func<double, double, double>)Math.Max);
            interpreter.AddFunction("min", (Func<double, double, double>)Math.Min);
            interpreter.AddFunction("mid", (Func<double, double, double, double>)((x, y, z) => Math.Max(Math.Min(Math.Max(x, y), z), Math.Min(x, y))));
            interpreter.AddFunction("flr", (Func<double, double>)Math.Floor);
            interpreter.AddFunction("ceil", (Func<double, double>)Math.Ceiling);
            interpreter.AddFunction("cos", (Func<double, double>)(x => Math.Cos(2 * x * Math.PI)));
            interpreter.AddFunction("sin", (Func<double, double>)(x => -Math.Sin(2 * x * Math.PI)));
            interpreter.AddFunction("atan2", (Func<double, double, double>)((dx, dy) => 1 - Math.Atan2(dy, dx) / (2 * Math.PI)));
            interpreter.AddFunction("sqrt", (Func<double, double>)Math.Sqrt);
            interpreter.AddFunction("abs", (Func<double, double>)Math.Abs);
            interpreter.AddFunction("rnd", (Func<double?, double>)Rnd);
            interpreter.AddFunction("srand", (Func<int, object>)Srand);
            interpreter.AddFunction("band", (Func<double, double, double>)((x, y) => util.FixedToFloat(util.FloatToFixed(x) & util.FloatToFixed(y))));
            interpreter.AddFunction("bor", (Func<double, double, double>)((x, y) => util.FixedToFloat(util.FloatToFixed(x) | util.FloatToFixed(y))));
            interpreter.AddFunction("bxor", (Func<double, double, double>)((x, y) => util.FixedToFloat(util.FloatToFixed(x) ^ util.FloatToFixed(y))));
            interpreter.AddFunction("bnot", (Func<double, double>)(x => util.FixedToFloat(~util.FloatToFixed(x))));
            interpreter.AddFunction("shl", (Func<double, int, double>)((x, n) => util.FixedToFloat(util.FloatToFixed(x) << n)));
            interpreter.AddFunction("shr", (Func<double, int, double>)((x, n) => util.FixedToFloat(util.FloatToFixed(x) >> n)));
            interpreter.AddFunction("lshr", (Func<double, int, double>)((x, n) => util.FixedToFloat((int)((uint)util.FloatToFixed(x)) >> n))); // Does Not Work I think

            // Controls
            interpreter.AddFunction("btn", (Func<int?, int?, object>)Btn);
            interpreter.AddFunction("btnp", (Func<int, int?, bool>)Btnp);

            interpreter.AddFunction("flip", (Action)graphics.Flip);

            // Music
            interpreter.AddFunction("music", (Func<int?, int?, int?, object>)Music);
            interpreter.AddFunction("sfx", (Func<int?, int?, int?, int?, object>)Sfx);

            // Misc
            interpreter.AddFunction("time", (Func<double>)Time);

            interpreter.AddFunction("print", (Action<object, int?, int?, byte?>)graphics.Print);
            interpreter.AddFunction("printh", (Action<object, int?, int?, byte?>)Print);

            interpreter.RunScript(@"
                function all(collection)
                   if (collection == nil) then return function() end end
                   local index = 0
                   local count = #collection
                   return function ()
                      index = index + 1
                      if index <= count
                      then
                         return collection[index]
                      end
                   end
                end

                function tostr(x)
                    if type(x) == ""number"" then return tostring(math.floor(x*10000)/10000) end
                    return tostring(x)
                end

                function tonum(x)
                    return tonumber(x)
                end

                function add(t,v)
                    if t == nil then return end
                    table.insert(t,v)
                end

                function del(t,v)
                    if t == nil then return end
                    for i = 0,#t do
                        if t[i] == v then
                            table.remove(t,i)
                            return
                        end
                    end
                end

                function foreach(t,f)
                    for e in all(t) do
                        f(e)
                    end
                end

                function string:split(sep)
                   local sep, fields = sep or "":"", {}
                   local pattern = string.format(""([^%s]+)"", sep)
                   self: gsub(pattern, function(c) fields[#fields+1] = c end)
                   return fields
                end

                function b(s)
                    local o = 0
                    local n = s:split('.')
                    for d in n[1]:gmatch('[01]') do
                        o = o*2 + tonumber(d)
                    end
                    if n[2] then
                        div = 2
                        for d in n[2]:gmatch('[01]') do
                            o = o + tonumber(d) / div
                            div = div * 2
                        end
                    end

                    return o
                end

                t = type
                function type(f)
                    if not f then
                        return 'nil'
                    end
                    return t(f)
                end

                cocreate = coroutine.create
                coresume = coroutine.resume
                costatus = coroutine.status
                yield = coroutine.yield
                sub = string.sub
                ");
        }

        /// <summary>
        /// Play music starting from pattern n (0..63)
		/// n -1 to stop music
        /// fade_len in ms(default: 0)
        ///
        /// channel_mask specifies which channels to reserve for music only
        ///
        ///    e.g.to play on channels 0..2: 1+2+4 = 7
        ///
        /// Reserved channels can still be used to play sound effects on, but only when that
        /// 
        /// channel index is explicitly requested by sfx().
        /// </summary>
        /// <param name="n">The pattern number</param>
        /// <param name="fade_len">The amount of time taken to fade music from 0 to full volume.</param>
        /// <param name="channel_mask">specifies which channels to reserve for music only</param>
        /// <returns>The </returns>
        public object Music(int? n = null, int? fade_len = null, int? channel_mask = null)
        {
            return null;
        }

        /// <summary>
        /// Play sfx n on channel (0..3) from note offset (0..31) for length notes
		/// n -1 to stop sound on that channel
        ///
        /// n -2 to release sound on that channel from looping
        ///
        /// Any music playing on the channel will be halted
        /// offset in number of notes(0..31)
        ///
        /// channel -1 (default) to automatically choose a channel that is not being used
        ///
        /// channel -2 to stop the sound from playing on any channel
        /// </summary>
        /// <param name="n">The n</param>
        /// <param name="channel">The channel</param>
        /// <param name="offset">The offset</param>
        /// <param name="length">The length</param>
        /// <returns>The </returns>
        public object Sfx(int? n = null, int? channel = null, int? offset = null, int? length = null)
        {
            return null;
        }

        /// <summary>
        /// Print a string
		/// If only str is supplied, and the cursor reaches the end of the screen,
		/// a carriage return and vertical scroll is automatically applied.
        /// </summary>
        /// <param name="s">The string to print</param>
        /// <param name="x">The x position</param>
        /// <param name="y">The y position</param>
        /// <param name="c">The c the color to draw the string.</param>
        public void Print(object s, int? x = null, int? y = null, byte? c = null)
        {
            if (c.HasValue)
            {
                memory.DrawColor = c.Value;
            }
            Console.WriteLine(String.Format("{0:####.####}", s));
        }

        /// <summary>
        /// cartdata() opens a permanent data storage slot indexed by id, that can be
        /// 	used to store and retrieve up to 256 bytes(64 numbers) worth of data using 
        /// 	DSET() and DGET().
        /// 	
        /// 	CARTDATA("zep_dark_forest") -- can only be set once per session
        /// 	-- later in the program..
        ///     DSET(0, score)
        /// 
        /// 
        /// id is a string up to 64 characters long, and should be unusual enough that
        /// 
        /// other cartridges do not accidentally use the same id.
        /// 
        /// e.g.cartdata("zep_jelpi")
        /// 
        /// 
        /// legal characters are a..z, 0..9 and underscore(_)
        /// 
        /// 
        /// returns true if data was loaded, otherwise false
        /// 
        /// cartdata can not be called more than once per cartridge execution.
        /// 
        /// Once a cartdata id has been set, the area of memory 0x5e00..0x5eff is mapped
        /// to permanent storage, and can either be accessed directly or via dget/dset.
        /// </summary>
        /// <param name="id">The id for the cartdata file</param>
        public void Cartdata(string id)
        {
            Trace.Assert(loadedGame.cartdata_id.Length == 0, "cartdata() can only be called once");
            Trace.Assert(id.Length <= 64, "cart data id too long");
            Trace.Assert(id.Length != 0, "empty cart data id");

            Trace.Assert(Regex.IsMatch(id, "^[a-zA-Z0-9 ]*$"), "cart data id: bad char");

            var fileName = cartdataPath + id;
            if (File.Exists(fileName))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
                {
                    for (int i = 0; i < loadedGame.cartdata.Length; i++)
                    {
                        loadedGame.cartdata[i] = reader.ReadInt32();
                    }
                }
            }
            else
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    for (int i = 0; i < loadedGame.cartdata.Length; i++)
                    {
                        writer.Write(0);
                        loadedGame.cartdata[i] = 0;
                    }
                }
            }

            loadedGame.cartdata_id = id;
        }

        /// <summary>
        /// Get the number stored at index (0..63)
		/// Use this only after you have called cartdata()
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The value</returns>
        public object Dget(int index)
        {
            Trace.Assert(index < loadedGame.cartdata.Length, "bad index");
            return util.FixedToFloat(loadedGame.cartdata[index]);
        }

        /// <summary>
        /// Set the number stored at index (0..63)
		/// Use this only after you have called cartdata()
        ///
        /// There is no need to flush written data -- it is automatically
        /// saved to permanent storage even if POKE()'ed directly.
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="value">The value</param>
        public void Dset(int index, double value)
        {
            Trace.Assert(index < loadedGame.cartdata.Length, "bad index");
            loadedGame.cartdata[index] = util.FloatToFixed(value);
            SaveCartdata(cartdataPath + loadedGame.cartdata_id);
        }

        /// <summary>
        /// Saves cartdata information to file.
        /// </summary>
        /// <param name="fileName">The fileName to write to.</param>
        private void SaveCartdata(string fileName)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                for (int i = 0; i < loadedGame.cartdata.Length; i++)
                {
                    writer.Write(loadedGame.cartdata[i]);
                }
            }
        }

        /// <summary>
        /// Returns a random number n, where 0 <= n < x
		/// If you want an integer, use flr(rnd(x))
        /// </summary>
        /// <param name="x">The x</param>
        /// <returns>The </returns>
        public double Rnd(double? x = null)
        {
            if (!x.HasValue) x = 1; return random.NextDouble() * x.Value;
        }

        /// <summary>
        /// Changes random seed.
        /// </summary>
        /// <param name="x">The new seed</param>
        /// <returns>Returns null everytime.</returns>
        public object Srand(int x)
        {
            random = new Random(x);
            return null;
        }

        /// <summary>
        /// Returns time passed since start.
        /// </summary>
        /// <returns>Returns time passed since start.</returns>
        public double Time()
        {
            return (DateTime.Now - timeStart).TotalSeconds;
        }

        /// <summary>
        /// Same as memcpy, but copies from cart rom
		/// The code section( >= 0x4300) is protected and can not be read.
        /// If filename specified, load data from a different cartridge.
        /// </summary>
        /// <param name="dest_addr">The destination address to write to.</param>
        /// <param name="source_addr">The source address to read from.</param>
        /// <param name="len">The length of data to read/write.</param>
        /// <param name="filename">The path to a different cartridge.</param>
        /// <returns>Returns null everytime.</returns>
        public object Reload(int dest_addr, int source_addr, int len, string filename = "")
        {
            Trace.Assert(dest_addr < 0x4300);
            Cartridge cart = filename.Length == 0 ? loadedGame.cartridge : new Cartridge(filename);

            memory.Memcpy(dest_addr, source_addr, len, cart.rom);
            return null;
        }

        /// <summary>
        /// Same as memcpy, but copies from base ram to cart rom
        /// cstore() is equivalent to cstore(0, 0, 0x4300)
        /// Can use for writing tools to construct carts or to visualize the state
        /// of the map / spritesheet using the map editor / gfx editor.
        /// The code section ( >= 0x4300) is protected and can not be written to.
        /// </summary>
        /// <param name="dest_addr">The destination address to write to.</param>
        /// <param name="source_addr">The source address to read from.</param>
        /// <param name="len">The length of data to read/write.</param>
        /// <param name="filename">The path to a different cartridge.</param>
        /// <returns>Returns null everytime.</returns>
        public object Cstore(int dest_addr, int source_addr, int len, string filename = null)
        {
            Trace.Assert(dest_addr < 0x4300);
            Cartridge cart = filename == null ? loadedGame.cartridge : new Cartridge(filename, true);

            Buffer.BlockCopy(memory.ram, source_addr, cart.rom, dest_addr, len);
            cart.SaveP8();
            return null;
        }

        /// <summary>
        /// Load a game from path and run it. 
        /// All paths are considered to be inside pico8/games folder
        /// </summary>
        /// <param name="path">The path to read cart from.</param>
        /// <param name="interpreter">The interpreter to use.<see cref="ILuaInterpreter"/></param>
        public void LoadGame(string path, ILuaInterpreter interpreter)
        {
            loadedGame = new Game();

            loadedGame.path = path;
            loadedGame.cartridge = new Cartridge(path);
            loadedGame.interpreter = interpreter;
            loadedGame.cartdata_id = "";
            loadedGame.cartdata = new Int32[64];

            InitAPI(ref loadedGame.interpreter);

            memory.LoadCartridgeData(loadedGame.cartridge.rom);
            loadedGame.interpreter.RunScript(loadedGame.cartridge.gameCode);

            timeStart = DateTime.Now;

            loadedGame.interpreter.CallIfDefined("_init");
        }

        /// <summary>
        /// Call scripts update method
        /// </summary>
        public void Update()
        {
            UpdateControllerState();
            if (!loadedGame.interpreter.CallIfDefined("_update"))
            {
                loadedGame.interpreter.CallIfDefined("_update60");
            }
        }

        /// <summary>
        /// Call scripts draw method
        /// </summary>
        public void Draw()
        {
            loadedGame.interpreter.CallIfDefined("_draw");
            graphics.Flip();
        }

        /// <summary>
        /// get button i state for player p (default 0) 
		/// i: 0..5: left right up down button_o button_x
        ///
        /// p: player index 0..7
        ///
        ///
        /// Instead of using a number for i, it is also possible to use a button glyph.
        ///
        /// (In the coded editor, use Shift-L R U D O X)
        ///
        /// If no parameters supplied, returns a bitfield of all 12 button states for player 0 & 1
        ///	 P0: bits 0..5  P1: bits 8..13
        ///
        /// Default keyboard mappings to player buttons:
        ///	player 0: cursors, Z,X / C,V / N,M
        ///    player 1: ESDF, LSHIFT,A / TAB,Q,E
        /// </summary>
        /// <param name="i">The index of the button</param>
        /// <param name="p">The player index.</param>
        /// <returns>The button state value.</returns>
        public object Btn(int? i = null, int? p = null)
        {
            if (!p.HasValue)
            {
                p = 0;
            }

            switch (i.Value)
            {
                case 0:
                    return BtnLeftCurrent[p.Value];
                case 1:
                    return BtnRightCurrent[p.Value];
                case 2:
                    return BtnUpCurrent[p.Value];
                case 3:
                    return BtnDownCurrent[p.Value];
                case 4:
                    return BtnACurrent[p.Value];
                case 5:
                    return BtnBCurrent[p.Value];
            }

            return false;
        }

        /// <summary>
        /// btnp is short for "Button Pressed"; Instead of being true when a button is held down, 
		/// btnp returns true when a button is down AND it was not down the last frame.It also
        /// repeats after 15 frames, returning true every 4 frames after that(at 30fps -- double
        /// that at 60fp). This can be used for things like menu navigation or grid-wise player
        /// movement.
        /// </summary>
        /// <param name="i">The index of the button</param>
        /// <param name="p">The player index.</param>
        /// <returns>The button state value.</returns>
        public bool Btnp(int i, int? p = null)
        {
            if (!p.HasValue)
            {
                p = 0;
            }

            int pi = p.Value;
            switch (i)
            {
                case 0:
                    return BtnLeftCurrent[pi] && !BtnLeftLast[pi];
                case 1:
                    return BtnRightCurrent[pi] && !BtnRightLast[pi];
                case 2:
                    return BtnUpCurrent[pi] && !BtnUpLast[pi];
                case 3:
                    return BtnDownCurrent[pi] && !BtnDownLast[pi];
                case 4:
                    return BtnACurrent[pi] && !BtnALast[pi];
                case 5:
                    return BtnBCurrent[pi] && !BtnBLast[pi];
            }

            return false;
        }

        /// <summary>
        /// Updates all controller state stuff.
        /// </summary>
        private void UpdateControllerState()
        {
            foreach (var f in BtnPressedCallbacks)
            {
                for (int i = 0; i < 8; i++)
                {
                    BtnLeftLast[i] = BtnLeftCurrent[i];
                    BtnRightLast[i] = BtnRightCurrent[i];
                    BtnUpLast[i] = BtnUpCurrent[i];
                    BtnDownLast[i] = BtnDownCurrent[i];
                    BtnALast[i] = BtnACurrent[i];
                    BtnBLast[i] = BtnBCurrent[i];


                    BtnLeftCurrent[i] = f(BtnLeft[i]);
                    BtnRightCurrent[i] = f(BtnRight[i]);
                    BtnUpCurrent[i] = f(BtnUp[i]);
                    BtnDownCurrent[i] = f(BtnDown[i]);
                    BtnACurrent[i] = f(BtnA[i]);
                    BtnBCurrent[i] = f(BtnB[i]);
                }
            }
        }

        public void SetBtnPressedCallback(Func<int, bool> callback)
        {
            BtnPressedCallbacks.Add(callback);
        }

        /// <summary>
        /// Sets controller key values. Used to call BtnPressedCallbacks so that it know the indexes of each button.
        /// </summary>
        /// <param name="index">The player index</param>
        /// <param name="Left">The Left button index</param>
        /// <param name="Right">The Right button index</param>
        /// <param name="Up">The Up button index</param>
        /// <param name="Down">The Down button index</param>
        /// <param name="A">The A button index</param>
        /// <param name="B">The B button index</param>
        public void SetControllerKeys(int index, int Left, int Right, int Up, int Down, int A, int B)
        {
            Trace.Assert(index >= 0 && index <= 7);

            BtnLeft[index] = Left;
            BtnRight[index] = Right;
            BtnUp[index] = Up;
            BtnDown[index] = Down;
            BtnA[index] = A;
            BtnB[index] = B;
        }
    }
}
