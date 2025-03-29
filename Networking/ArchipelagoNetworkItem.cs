using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    

    public class ArchipelagoNetworkItem
    {
        public const int OFFSET_BASE = 8000000;
        public const int OFFSET_KIND = 20000;
        public const int OFFSET_LEVEL = 1000;
        public const int OFFSET_SIDE = 100;

        // Type of item (i.e. completion, cassette, berry etc.)
        public CollectableType type;
        // Celeste Chapter
        public int area;
        // Celeste side
        public int mode;
        // offset for berries
        public int offset;
        // Corresponding strawberry entity for strawberry locations
        public EntityID? strawberry;

        // Maps from Chapter+Side+Offset <-> Strawberry in level
        private static Dictionary<int, EntityID> StrawberryMap;
        private static Dictionary<string, int> StrawberryReverseMap;

        // Maps from Chapter + Side + Offset -> Golden Strawbrry in level
        // Offset is only 1 for dashless
        private static Dictionary<int, EntityID> GoldenStrawberryMap; 

        public long ID
        {
            get
            {
                return OFFSET_BASE + (int)type * OFFSET_KIND + area * OFFSET_LEVEL + mode * OFFSET_SIDE + offset;
            }
        }

        public AreaKey areaKey
        {
            get
            {
                var areaKey = new AreaKey(0, (AreaMode)mode);
                areaKey.ID = area;
                return areaKey;
            }
        }

        public ArchipelagoNetworkItem(long networkID)
        {
            int temp = (int)(networkID % OFFSET_BASE);

            type = (CollectableType)(temp / OFFSET_KIND);
            temp %= OFFSET_KIND;

            area = temp / OFFSET_LEVEL;
            temp %= OFFSET_LEVEL;

            mode = temp / OFFSET_SIDE;
            temp %= OFFSET_SIDE;

            offset = temp;
            if (this.type == CollectableType.STRAWBERRY)
            {
                this.strawberry = GetStrawberryEntityID(area, mode, offset);
            } 
            else if (this.type == CollectableType.GOLDEN) 
            {   
                this.strawberry = GetGoldenEntityID(area, mode, offset);
            }
        }

        public ArchipelagoNetworkItem(CollectableType type, int area, int mode, EntityID? strawberry = null)
        {
            this.type = type;
            this.area = area;
            this.mode = mode;

            if (!strawberry.HasValue)
            {
                offset = 0;
                this.strawberry = null;
            }
            else if (this.type == CollectableType.GOLDEN) 
            {   
                bool isWinged = GetGoldenEntityID(area, mode, 1).Equals(strawberry);
                this.offset = isWinged ? 1 : 0;
                this.strawberry = GetGoldenEntityID(area, mode, offset);
            }
            else 
            {
                offset = (GetStrawberryOffset(strawberry.Value) ?? 99) % OFFSET_SIDE;
                if (offset == 99 && area == 10) {
                    // special case for moon berry bc idk where it's stored
                    offset = 0;
                }
                this.strawberry = GetStrawberryEntityID(area, mode, offset);
            }
        }

        public ArchipelagoNetworkItem(CollectableType type, AreaKey area, EntityID? strawberry = null) :
            this(type, area.ID, (int)area.Mode, strawberry) {}
       
        private static void BuildStrawberryMap()
        {
            StrawberryMap = new Dictionary<int, EntityID>();
            StrawberryReverseMap = new Dictionary<string, int>();
            GoldenStrawberryMap = new Dictionary<int, EntityID>();

            int offset, id;
            EntityID strawberry;
            
            foreach (AreaData area in AreaData.Areas)
            {
                for (int i = 0; i < area.Mode.Length; i++)
                {
                    ModeProperties modeProperties = area.Mode[i];
                    offset = 0;
                    var maxJ = modeProperties.Checkpoints == null ? 1 : modeProperties.Checkpoints.Length + 1;
                    for (int j = 0; j < maxJ; j++)
                    {
                        var maxK = j == 0 ? modeProperties.StartStrawberries : modeProperties.Checkpoints[j - 1].Strawberries;
                        for (int k = 0; k < maxK; k++)
                        {
                            EntityData entityData = modeProperties.StrawberriesByCheckpoint[j, k];
                            if (entityData == null || entityData.Name != "strawberry")
                            {
                                continue;
                            }
                            strawberry = new EntityID(entityData.Level.Name, entityData.ID);
                            id = area.ID * OFFSET_LEVEL + i * OFFSET_SIDE + offset;
                            StrawberryMap.Add(id, strawberry);
                            StrawberryReverseMap.Add(strawberry.Key, id);
                            offset++;
                        }
                    }
                    // Add golden berry
                    if (modeProperties.MapData.Goldenberries.Count > 0) {
                        EntityData entityData = modeProperties.MapData.Goldenberries[0];
                        strawberry = new EntityID(entityData.Level.Name, entityData.ID);
                        id = area.ID * OFFSET_LEVEL + i * OFFSET_SIDE;
                        GoldenStrawberryMap.Add(id, strawberry);
                    }
                    if (modeProperties.MapData.DashlessGoldenberries.Count > 0) {
                        EntityData entityData = modeProperties.MapData.DashlessGoldenberries[0];
                        strawberry = new EntityID(entityData.Level.Name, entityData.ID);
                        id = area.ID * OFFSET_LEVEL + i * OFFSET_SIDE + 1;
                        GoldenStrawberryMap.Add(id, strawberry);
                    }
                    
                }
            }
        }

        private static EntityID? GetStrawberryEntityID(int area, int mode, int offset)
        {
            if (StrawberryMap == null)
            {
                BuildStrawberryMap();
            }

            int index = area * OFFSET_LEVEL + mode * OFFSET_SIDE + offset;
            if (StrawberryMap.ContainsKey(index))
            {
                return StrawberryMap[index];
            }

            return null;
        }

        public static int? GetStrawberryOffset(EntityID strawberry)
        {
            if (StrawberryMap == null)
            {
                BuildStrawberryMap();
            }

            if (StrawberryReverseMap.ContainsKey(strawberry.Key))
            {
                return StrawberryReverseMap[strawberry.Key];
            }
            return null;
        }

        private static EntityID? GetGoldenEntityID(int area, int mode, int offset)
        {
            if (StrawberryMap == null)
            {
                BuildStrawberryMap();
            }

            int index = area * OFFSET_LEVEL + mode * OFFSET_SIDE + offset;
            if (GoldenStrawberryMap.ContainsKey(index))
            {
                return GoldenStrawberryMap[index];
            }

            return null;
        }
    }
}
