using System;
using CustomSlugcatUtils.Dev;
using CustomSlugcatUtils.Objects;
using DevInterface;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomSlugcatUtils.Hooks
{
    static class DevToolsHooks
    {
        public static readonly PlacedObject.Type CustomChatlogToken = new("CustomChatlogToken", true);

        public static void OnModsInit()
        {
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
            On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += ObjectsPage_DevObjectGetCategoryFromPlacedType;
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
            On.Room.Loaded += Room_Loaded;
            Plugin.Log("Dev Tool Hooks Loaded");
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);

            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                if (self.roomSettings.placedObjects[i].data is not CustomWhiteToken.CollectTokenData || (ModManager.Expedition && (self.game?.rainWorld?.ExpeditionMode ?? false)))
                    continue;
                var saveData = self.game?.GetStorySession?.saveState?.deathPersistentSaveData;
                if (saveData != null && !saveData.chatlogsRead.Contains(new ChatlogData.ChatlogID((self.roomSettings.placedObjects[i].data as CustomWhiteToken.CollectTokenData).chatlogId)))
                    self.AddObject(new CustomWhiteToken(self, self.roomSettings.placedObjects[i]));
                else
                    self.AddObject(new CustomWhiteStalk(self, self.roomSettings.placedObjects[i].pos,
                        self.roomSettings.placedObjects[i].pos + (self.roomSettings.placedObjects[i].data as CustomWhiteToken.CollectTokenData).handlePos, null)
                    { forceSatellite = true });
                

            }


        }
        private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            orig(self);
            if (self.type == CustomChatlogToken)
                self.data = new CustomWhiteToken.CollectTokenData(self);
        }

        private static ObjectsPage.DevObjectCategories ObjectsPage_DevObjectGetCategoryFromPlacedType(On.DevInterface.ObjectsPage.orig_DevObjectGetCategoryFromPlacedType orig, DevInterface.ObjectsPage self, PlacedObject.Type type)
        {
            if (type == CustomChatlogToken)
                return ObjectsPage.DevObjectCategories.Tutorial;
            return orig(self, type);
        }

        private static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            if (tp == CustomChatlogToken)
            {
                if (pObj == null)
                {
                    pObj = new PlacedObject(tp, null);
                    pObj.pos = self.owner.room.game.cameras[0].pos +
                               Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) +
                               Custom.DegToVec(Random.value * 360f) * 0.2f;
                    self.RoomSettings.placedObjects.Add(pObj);
                }

                var rep = new CustomWhiteTokenRepresentation(self.owner, tp + "_Rep", self, pObj);
                self.tempNodes.Add(rep);
                self.subNodes.Add(rep);
                return;
            }
            orig(self, tp, pObj);

        }
    }
}
