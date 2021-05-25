using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Utility;

namespace Bunny.Stages
{
    class MapManager
    {
        private List<Map> maps = new List<Map>();
        private Map LoadMap(string mapname)
        {
            Map map = new Map();
            XmlReader reader; 
            if (Type.GetType("Mono.Runtime") == null)
                reader = new XmlTextReader($"Maps\\{mapname}\\{mapname}.RS.xml");
            else
                reader = new XmlTextReader($"Maps/{mapname}/{mapname}.RS.xml");

            SpawnType lastType = SpawnType.Solo;
            Position p = new Position();
            Direction d = new Direction();
            string[] cords;
            while (reader.Read())
            {
                switch (reader.Name)
                {
                    case "DUMMY":
                        p = new Position();
                        d = new Direction();
                        string name = reader.GetAttribute("name");

                        if (name != null && name.StartsWith("spawn_solo"))
                        {
                            lastType = SpawnType.Solo;
                        }
                        else if (name != null && name.StartsWith("spawn_team1"))
                        {
                            lastType = SpawnType.Red;
                        }
                        else if (name != null && name.StartsWith("spawn_team2"))
                        {
                            lastType = SpawnType.Blue;
                        }
                        else if (name != null && name.StartsWith("wait_"))
                        {
                            lastType = SpawnType.Wait;
                        }
                        break;

                    case "POSITION":
                        cords = reader.ReadElementContentAsString().Split(' ');

                        p.X = float.Parse(cords[0]);
                        p.Y = float.Parse(cords[1]);
                        p.Z = float.Parse(cords[2]);
                        break;
                    case "DIRECTION":
                        cords = reader.ReadElementContentAsString().Split(' ');

                        d.X = float.Parse(cords[0]);
                        d.Y = float.Parse(cords[1]);
                        d.Z = float.Parse(cords[2]);

                        if (lastType == SpawnType.Solo)
                        {
                            map.Deathmatch.Add(new Pair<Position, Direction>(p, d));
                        }
                        else if (lastType == SpawnType.Red)
                        {
                            map.TeamRed.Add(new Pair<Position, Direction>(p, d));
                        }
                        else if (lastType == SpawnType.Blue)
                        {
                            map.TeamBlue.Add(new Pair<Position, Direction>(p, d));
                        }
                        break;
                }
            }
            return map;
        }
        public static void LoadSpawns(string mapname, ref Map map)
        {
            XmlTextReader reader;
            if (Type.GetType("Mono.Runtime") == null)
                reader = new XmlTextReader("Maps\\" + mapname + "\\spawn.xml");
            else
                reader = new XmlTextReader("Maps/" + mapname + "/spawn.xml");

            var lastType = SpawnType.Solo;
            var p = new Position();
            var item = 0;
            double time = 0;

            string[] cords;
            while (reader.Read())
            {
                switch (reader.Name)
                {
                    case "GAMETYPE":
                        string name = reader.GetAttribute("id");

                        if (name != null && name.StartsWith("solo"))
                        {
                            lastType = SpawnType.Solo;
                        }
                        else if (name != null && name.StartsWith("team"))
                        {
                            lastType = SpawnType.Red;
                        }
                        break;

                    case "SPAWN":
                        p = new Position();
                        string curitem = reader.GetAttribute("item");
                        if (curitem == null)
                            continue;
                        time = int.Parse(reader.GetAttribute("timesec"));

                        if (curitem.Equals("bullet02"))
                        {
                            item = 8;
                        }
                        else if (curitem.StartsWith("hp02"))
                        {
                            item = 2;
                        }
                        else if (curitem.StartsWith("ap02"))
                        {
                            item = 5;
                        }
                        else if (curitem.StartsWith("hp03"))
                        {
                            item = 3;
                        }
                        else if (curitem.StartsWith("ap03"))
                        {
                            item = 6;
                        }
                        break;
                    case "POSITION":
                        cords = reader.ReadElementContentAsString().Split(' ');

                        p.X = float.Parse(cords[0]);
                        p.Y = float.Parse(cords[1]);
                        p.Z = float.Parse(cords[2]);
                        if (lastType == SpawnType.Solo)
                        {
                            var i = new ItemSpawn();
                            i.Position = p;
                            i.ItemId = item;
                            i.SpawnTime = (int)(time / 1000);
                            i.NextSpawn = DateTime.Now;
                            map.DeathMatchItems.Add(i);
                        }
                        else if (lastType == SpawnType.Red)
                        {
                            var i = new ItemSpawn();
                            i.Position = p;
                            i.ItemId = item;
                            i.SpawnTime = (int)(time / 1000);
                            i.NextSpawn = DateTime.Now;
                            map.TeamItems.Add(i);
                        }
                        break;
                }
            }
        }
        public void LoadMaps()
        {
            IEnumerable<String> mapList;
            if (Type.GetType("Mono.Runtime") == null)
            {
                mapList = from directory in Directory.GetDirectories(Directory.GetCurrentDirectory() + "\\Maps")
                              let mapName = directory.Split('\\').Last()
                              select mapName;
            }
            else
            {
                mapList = from directory in Directory.GetDirectories(Directory.GetCurrentDirectory() + "/Maps")
                          let mapName = directory.Split('/').Last()
                          select mapName;
            }

                foreach (var map in mapList)
                {
                    try
                    {
                        var currentMap = LoadMap(map);
                        currentMap.MapName = map;

                        LoadSpawns(map, ref currentMap);
                        Log.Write("Loaded: Map[{0}] spawns: {1}", map, currentMap.Deathmatch.Count);
                        maps.Add(currentMap);
                    }
                    catch(Exception ex)
                    {
                        Log.Write("[ERROR] UNABLE TO LOAD MAP: {0} - {1}", map, ex);
                        continue;
                    }
                }
        }

        public Map GetMap(string mapname)
        {
            lock (maps)
            {
                return maps.Find(m => m.MapName == mapname).Clone();
            }
        }
    }
}

