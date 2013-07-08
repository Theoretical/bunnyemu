using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Bunny.Core;

namespace Bunny.Stages
{
    class WorldItem
    {
        public Int32 Id;
        public string Type;
        public Int32 Time;
        public Int32 Amount;
        public string Model;
    }
     
    class WorldItemManager
    {
        private static List<WorldItem> _worldItems = new List<WorldItem>();
        private static object  _objectLock = new object();

        public static void Load()
        {
            var item = new WorldItem();

            using (var reader = new XmlTextReader("WorldItem.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "WORLDITEM":
                            item = new WorldItem();
                            var name = reader.GetAttribute("id");
                            if (name != null)
                                item.Id = Int32.Parse(name);
                            break;

                        case "TYPE":
                            item.Type = reader.ReadElementContentAsString();
                            break;
                        
                        case "TIME":
                            item.Time = Int32.Parse(reader.ReadElementContentAsString());
                            break;

                        case "AMOUNT":
                            item.Amount = Int32.Parse(reader.ReadElementContentAsString());
                            break;

                        case "MODELNAME":
                            item.Model = reader.ReadElementContentAsString();
                            _worldItems.Add(item);
                            break;

                        case "MeshInfo":
                            return;
                    }
                }
            }
        }
        public static Int32 GetTime(Int32 id)
        {
            lock (_objectLock)
                return _worldItems.Find(i => i.Id == id).Time;
        }
    }
}
