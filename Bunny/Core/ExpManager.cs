using System;
using System.Xml;

namespace Bunny.Core
{
    class ExpManager
    {
        private static readonly uint[] NeedExpTable = new uint[100];
        private static readonly uint[] GettingExpTable = new uint[100];
        private static readonly uint[] BountyExpTable = new uint[100];

        public static void Load()
        {
            LoadNeed();
            LoadGetting();
        }

        public static void LoadNeed()
        {
            var multTable = new float[100];
            using (var reader = new XmlTextReader("formula.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "LM":
                            var low = int.Parse(reader.GetAttribute("lower"));
                            var high = int.Parse(reader.GetAttribute("upper"));
                            var value = reader.ReadElementContentAsFloat();

                            for (; low <= high; low++)
                                multTable[low] = value;
                            break;

                        case "FORMULA_TABLE":
                            if (reader.GetAttribute("id") != "NeedExpLM")
                                reader.Close();
                            break;
                    }
                }
            }

            for (var i = 1; i < 100; ++i)
            {
                var exp = (UInt32)((double)(i * i) * multTable[i] * 100.0 + .5f);
                NeedExpTable[i] = Convert.ToUInt32(multTable[i - 1] + 2 * exp);
            }
        }
        public static void LoadGetting()
        {
            var multTable = new float[100];
            var getting = false;

            using (var reader = new XmlTextReader("formula.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "LM":
                            if (!getting)
                                break;

                            var low = int.Parse(reader.GetAttribute("lower"));
                            var high = int.Parse(reader.GetAttribute("upper"));
                            var value = reader.ReadElementContentAsFloat();

                            for (; low <= high; low++)
                                multTable[low] = value;
                            break;

                        case "FORMULA_TABLE":
                            if (reader.GetAttribute("id") == "GettingExpLM")
                                getting = true;
                            else if (reader.GetAttribute("id") == "GettingBountyLM")
                                reader.Close();
                            break;
                    }
                }
            }

            for (var i = 1; i < 100; ++i)
            {
                var exp = (UInt32)((i * multTable[i] * 20.0f + .5));
                GettingExpTable[i] = exp + Convert.ToUInt32((i - 1) * multTable[i] * 10.0 + 0.5f);
            }
        }
        public static void LoadBounty()
        {
            var multTable = new float[100];
            var getting = false;

            using (var reader = new XmlTextReader("formula.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "LM":
                            if (!getting)
                                break;

                            var low = int.Parse(reader.GetAttribute("lower"));
                            var high = int.Parse(reader.GetAttribute("upper"));
                            var value = reader.ReadElementContentAsFloat();

                            for (; low <= high; low++)
                                multTable[low] = value;
                            break;

                        case "FORMULA_TABLE":
                            if (reader.GetAttribute("id") == "GettingExpLM")
                                getting = true;
                            else if (reader.GetAttribute("id") == "GettingBountyLM")
                                reader.Close();
                            break;
                    }
                }
            }

            for (var i = 1; i < 100; ++i)
            {
                var exp = (UInt32)((i * multTable[i] * 20.0f + .5));
                GettingExpTable[i] = exp + Convert.ToUInt32((i - 1) * multTable[i] * 10.0 + 0.5f);
            }
        }


        public static bool Level(Int32 level, UInt32 exp)
        {
            return exp >= NeedExpTable[level];
        }

        public static UInt32 GetExpFromKill(Int32 killerLevel, Int32 victimLevel)
        {
            if (killerLevel > 99 || victimLevel > 99)
                return 0;

            var exp = 2 * GettingExpTable[killerLevel];

            if (exp >= GettingExpTable[victimLevel])
                exp = GettingExpTable[victimLevel];

            return exp;
        }

        public static UInt32 GetExp(int level)
        {
            return level <= 99 ? NeedExpTable[level] : 0;
        }

        public static int GetLevel(int exp)
        {
            for (var i = 0; i < 100; ++i)
                if (NeedExpTable[i] >= exp)
                    return i;

            return 100;
        }

        public static uint Exp(int level)
        {
            return NeedExpTable[level];
        }

        public static int PercentToNextLevel(int exp)
        {
            var level = GetLevel(exp);
            var nextExp = GetExp(level + 1);
            float final = ((float)exp / (float)nextExp) * 100;

            return (int)final;
        }
    }
}
