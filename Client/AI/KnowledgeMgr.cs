using System;
using System.Collections.Generic;
using WotlkClient.Shared;
using WotlkClient.Constants;
using WotlkClient.Terrain;

namespace WotlkClient.AI
{
    public struct TrainerLocation
    {
        public int NpcId;
        public string Name;
        public int MapId; // 0=Azeroth, 1=Kalimdor, 530=Outland, 571=Northrend
        public Coordinate Position;
        public string ZoneName;

        public TrainerLocation(int id, string name, int map, float x, float y, float z, string zone)
        {
            NpcId = id;
            Name = name;
            MapId = map;
            Position = new Coordinate(x, y, z);
            ZoneName = zone;
        }
    }

    public struct TalentStep
    {
        public int Level;
        public int TabIndex; // 0=Balance, 1=Feral, 2=Restoration (Druid)
        public int TalentId; // This needs to be the specific SpellID of the talent rank
        public int Rank;
        public string Description;
    }

    public class KnowledgeMgr
    {
        // Druid Trainers (Horde - Thunder Bluff)
        public static List<TrainerLocation> DruidTrainersHorde = new List<TrainerLocation>()
        {
            // Turak Runetotem - Thunder Bluff (Elder Rise)
            new TrainerLocation(3033, "Turak Runetotem", 1, -1054f, 203f, 114f, "Thunder Bluff"),

            // Sheal Runetotem - Thunder Bluff (Elder Rise)
            new TrainerLocation(3034, "Sheal Runetotem", 1, -1056f, 200f, 114f, "Thunder Bluff"),

            // Gart Mistrunner - Mulgore (Camp Narache start area)
            new TrainerLocation(3060, "Gart Mistrunner", 1, -2917f, -260f, 56f, "Camp Narache"), // Starter
            
             // Bloodhoof Village Trainer
            new TrainerLocation(3064, "Gennia Runetotem", 1, -2313f, -440f, -6f, "Bloodhoof Village")
        };

        public static List<TalentStep> FeralBuild = new List<TalentStep>()
        {
            // Level 10-14: Ferocity (Feral Tree - Row 1)
            new TalentStep { Level = 10, TabIndex=1, TalentId=16934, Rank=1, Description="Ferocity 1/5" },
            new TalentStep { Level = 11, TabIndex=1, TalentId=16935, Rank=2, Description="Ferocity 2/5" },
            new TalentStep { Level = 12, TabIndex=1, TalentId=16936, Rank=3, Description="Ferocity 3/5" },
            new TalentStep { Level = 13, TabIndex=1, TalentId=16937, Rank=4, Description="Ferocity 4/5" },
            new TalentStep { Level = 14, TabIndex=1, TalentId=16938, Rank=5, Description="Ferocity 5/5" },
             // ...
        };

        public static TrainerLocation? GetClosestTrainer(Coordinate myPos, int myMapId)
        {
            TrainerLocation? best = null;
            float bestDist = float.MaxValue;

            foreach(var t in DruidTrainersHorde)
            {
                if(t.MapId != myMapId) continue; 

                float d = TerrainMgr.CalculateDistance(myPos, t.Position);
                if(d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
            return best;
        }

        public static string GetZoneName(Coordinate myPos, int mapId)
        {
            float minDist = float.MaxValue;
            string zoneName = "Inconnue";

            // Check against Trainers
            foreach(var t in DruidTrainersHorde)
            {
                if(t.MapId != mapId) continue;
                float d = TerrainMgr.CalculateDistance(myPos, t.Position);
                if(d < 500.0f) // Close to a trainer/town
                {
                    if (d < minDist)
                    {
                        minDist = d;
                        zoneName = $"Proche de {t.ZoneName}";
                    }
                }
            }

            // General zone centers
            if (mapId == 1 && TerrainMgr.CalculateDistance(myPos, new Coordinate(-2917f, -260f, 56f)) < 1000f) return "Camp Narache (Mulgore)";
            if (mapId == 1 && TerrainMgr.CalculateDistance(myPos, new Coordinate(-1280f, 126f, 131f)) < 1000f) return "Les Pitons du Tonnerre (Mulgore)";

            if (minDist < float.MaxValue) return zoneName;
            
            return "Terres sauvages";
        }
    }
}
