using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
namespace CustomSlugcatUtils.Objects
{
    internal class CustomWhiteStalk : UpdatableAndDeletable, IDrawable
    {
     

        public int BaseSprite => 0;

        public int Arm1Sprite => 1;

        public int Arm2Sprite => 2;

        public int Arm3Sprite => 3;

        public int Arm4Sprite => 4;

        public int Arm5Sprite => 5;

        public int ArmJointSprite => 6;

        public int SocketSprite => 7;


        public int HeadSprite => 8;

        public int LampSprite => 9;

        public int SataFlasher => 10;

        public int CoordSprite(int s)
        {
            return (ModManager.MSC ? 11 : 10) + s;
        }


        public int TotalSprites => (ModManager.MSC ? 11 : 10) + coord.GetLength(0);


        public float alive
        {
            get
            {
                if (token == null)
                {
                    return 0f;
                }
                return 0.25f + 0.75f * token.power;
            }
        }

        internal CustomWhiteStalk(Room room, Vector2 hoverPos, Vector2 basePos, CustomWhiteToken token)
        {
            this.token = token;
            this.hoverPos = hoverPos;
            this.basePos = basePos;
            if (token != null)
            {
                lampPower = 1f;
                lastLampPower = 1f;
            }

            //TODO : lampColor
            lampColor = Color.Lerp(RainWorld.GoldRGB, new Color(1f, 1f, 1f), 0.5f);

            Random.State state = Random.state;
            Random.InitState((int)(hoverPos.x * 10f) + (int)(hoverPos.y * 10f));
            curveLerps = new float[2, 5];
            for (int i = 0; i < curveLerps.GetLength(0); i++)
            {
                curveLerps[i, 0] = 1f;
                curveLerps[i, 1] = 1f;
            }
            curveLerps[0, 3] = Random.value * 360f;
            curveLerps[1, 3] = Mathf.Lerp(10f, 20f, Random.value);
            flip = ((Random.value < 0.5f) ? -1f : 1f);
            mainDir = Custom.DirVec(basePos, hoverPos);
            coordLength = Vector2.Distance(basePos, hoverPos) * 0.6f;
            coord = new Vector2[(int)(coordLength / coordSeg), 3];
            armLength = Vector2.Distance(basePos, hoverPos) / 2f;
            armPos = basePos + mainDir * armLength;
            lastArmPos = armPos;
            armGetToPos = armPos;
            for (int j = 0; j < coord.GetLength(0); j++)
            {
                coord[j, 0] = armPos;
                coord[j, 1] = armPos;
            }
            head = hoverPos - mainDir * headDist;
            lastHead = head;
            Random.state = state;
        }

        public override void Update(bool eu)
        {
            lastArmPos = armPos;
            armPos += armVel;
            armPos = Custom.MoveTowards(armPos, armGetToPos, (0.8f + armLength / 150f) / 2f);
            armVel *= 0.8f;
            armVel += Vector2.ClampMagnitude(armGetToPos - armPos, 4f) / 11f;
            lastHead = head;

            //TODO : statFlasherLight
            sataFlasherLight += 2;


            head += headVel;
            headVel *= 0.8f;
            if (token != null && token.slatedForDeletetion)
            {
                token = null;
            }
            lastLampPower = lampPower;
            lastSinCounter = sinCounter;
            sinCounter += Random.value * lampPower;

            if (token != null)
                lampPower = Custom.LerpAndTick(lampPower, 1f, 0.02f, 0.016666668f);
            else
                lampPower = Mathf.Max(0f, lampPower - 0.008333334f);

            if (!Custom.DistLess(head, armPos, coordLength))
            {
                headVel -= Custom.DirVec(armPos, head) * (Vector2.Distance(armPos, head) - coordLength) * 0.8f;
                head -= Custom.DirVec(armPos, head) * (Vector2.Distance(armPos, head) - coordLength) * 0.8f;
            }

            var v = Vector3.Slerp(Custom.DegToVec(GetCurveLerp(0, 0.5f, 1f)), new Vector2(0f, 1f), 0.4f) * 0.4f;
            headVel += new Vector2(v.x,v.y);
            lastHeadDir = headDir;
            Vector2 vector = hoverPos;
            if (token is { expand: 0f, contract: false })
            {
                vector = Vector2.Lerp(hoverPos, token.pos, alive);
            }
            headVel -= Custom.DirVec(vector, head) * (Vector2.Distance(vector, head) - headDist) * 0.8f;
            head -= Custom.DirVec(vector, head) * (Vector2.Distance(vector, head) - headDist) * 0.8f;
            headDir = Custom.DirVec(head, vector);
            if (Random.value < 1f / Mathf.Lerp(300f, 60f, alive))
            {
                Vector2 b = basePos + mainDir * armLength * 0.7f + Custom.RNV() * Random.value * armLength * Mathf.Lerp(0.1f, 0.3f, alive);
                if (SharedPhysics.RayTraceTilesForTerrain(room, armGetToPos, b))
                {
                    armGetToPos = b;
                }
                NewCurveLerp(0, curveLerps[0, 3] + Mathf.Lerp(-180f, 180f, Random.value), Mathf.Lerp(1f, 2f, alive));
                NewCurveLerp(1, Mathf.Lerp(10f, 20f, Mathf.Pow(Random.value, 0.75f)), Mathf.Lerp(0.4f, 0.8f, alive));
            }
            headDist = GetCurveLerp(1, 0.5f, 1f);
            if (token != null)
            {
                keepDistance = Custom.LerpAndTick(keepDistance, Mathf.Sin(Mathf.Clamp01(token.glitch) * 3.1415927f) * alive, 0.006f, alive / ((keepDistance < token.glitch) ? 40f : 80f));
            }
            headDist = Mathf.Lerp(headDist, 50f, Mathf.Pow(keepDistance, 0.5f));
            Vector2 a = Custom.DirVec(Custom.InverseKinematic(basePos, armPos, armLength * 0.65f, armLength * 0.35f, flip), armPos);
            for (int i = 0; i < coord.GetLength(0); i++)
            {
                float num = Mathf.InverseLerp(-1f, (float)coord.GetLength(0), (float)i);
                Vector2 a2 = Custom.Bezier(armPos, armPos + a * coordLength * 0.5f, head, head - headDir * coordLength * 0.5f, num);
                coord[i, 1] = coord[i, 0];
                coord[i, 0] += coord[i, 2];
                coord[i, 2] *= 0.8f;
                coord[i, 2] += (a2 - coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                coord[i, 0] += (a2 - coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                if (i > 2)
                {
                    coord[i, 2] += Custom.DirVec(coord[i - 2, 0], coord[i, 0]);
                    coord[i - 2, 2] -= Custom.DirVec(coord[i - 2, 0], coord[i, 0]);
                }
                if (i > 3)
                {
                    coord[i, 2] += Custom.DirVec(coord[i - 3, 0], coord[i, 0]) * 0.5f;
                    coord[i - 3, 2] -= Custom.DirVec(coord[i - 3, 0], coord[i, 0]) * 0.5f;
                }
                if (num < 0.5f)
                {
                    coord[i, 2] += a * Mathf.InverseLerp(0.5f, 0f, num) * Mathf.InverseLerp(5f, 0f, (float)i);
                }
                else
                {
                    coord[i, 2] -= headDir * Mathf.InverseLerp(0.5f, 1f, num);
                }
            }
            ConnectCoord();
            ConnectCoord();
            for (int j = 0; j < coord.GetLength(0); j++)
            {
                SharedPhysics.TerrainCollisionData terrainCollisionData = scratchTerrainCollisionData.Set(coord[j, 0], coord[j, 1], coord[j, 2], 2f, new IntVector2(0, 0), true);
                terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                coord[j, 0] = terrainCollisionData.pos;
                coord[j, 2] = terrainCollisionData.vel;
            }
            for (int k = 0; k < curveLerps.GetLength(0); k++)
            {
                curveLerps[k, 1] = curveLerps[k, 0];
                curveLerps[k, 0] = Mathf.Min(1f, curveLerps[k, 0] + curveLerps[k, 4]);
            }
            base.Update(eu);
        }

        private void NewCurveLerp(int curveLerp, float to, float speed)
        {
            if (curveLerps[curveLerp, 0] < 1f || curveLerps[curveLerp, 1] < 1f)
            {
                return;
            }
            curveLerps[curveLerp, 2] = curveLerps[curveLerp, 3];
            curveLerps[curveLerp, 3] = to;
            curveLerps[curveLerp, 4] = speed / Mathf.Abs(curveLerps[curveLerp, 2] - curveLerps[curveLerp, 3]);
            curveLerps[curveLerp, 0] = 0f;
            curveLerps[curveLerp, 1] = 0f;
        }

        private float GetCurveLerp(int curveLerp, float sCurveK, float timeStacker)
        {
            return Mathf.Lerp(curveLerps[curveLerp, 2], curveLerps[curveLerp, 3], Custom.SCurve(Mathf.Lerp(curveLerps[curveLerp, 1], curveLerps[curveLerp, 0], timeStacker), sCurveK));
        }

        private void ConnectCoord()
        {
            coord[0, 2] -= Custom.DirVec(armPos, coord[0, 0]) * (Vector2.Distance(armPos, coord[0, 0]) - coordSeg);
            coord[0, 0] -= Custom.DirVec(armPos, coord[0, 0]) * (Vector2.Distance(armPos, coord[0, 0]) - coordSeg);
            for (int i = 1; i < coord.GetLength(0); i++)
            {
                if (!Custom.DistLess(coord[i - 1, 0], coord[i, 0], coordSeg))
                {
                    Vector2 a = Custom.DirVec(coord[i, 0], coord[i - 1, 0]) * (Vector2.Distance(coord[i - 1, 0], coord[i, 0]) - coordSeg);
                    coord[i, 2] += a * 0.5f;
                    coord[i, 0] += a * 0.5f;
                    coord[i - 1, 2] -= a * 0.5f;
                    coord[i - 1, 0] -= a * 0.5f;
                }
            }
            coord[coord.GetLength(0) - 1, 2] -= Custom.DirVec(head, coord[coord.GetLength(0) - 1, 0]) * (Vector2.Distance(head, coord[coord.GetLength(0) - 1, 0]) - coordSeg);
            coord[coord.GetLength(0) - 1, 0] -= Custom.DirVec(head, coord[coord.GetLength(0) - 1, 0]) * (Vector2.Distance(head, coord[coord.GetLength(0) - 1, 0]) - coordSeg);
        }

        public Vector2 EyePos(float timeStacker)
        {
            return Vector2.Lerp(lastHead, head, timeStacker) + Vector3.Slerp(lastHeadDir, headDir, timeStacker).ToVector2InPoints() * 3f;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            sLeaser.sprites[BaseSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[BaseSprite].scaleX = 0.5f;
            sLeaser.sprites[BaseSprite].scaleY = 0.7f;
            sLeaser.sprites[BaseSprite].rotation = Custom.VecToDeg(mainDir);
            sLeaser.sprites[Arm1Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm1Sprite].scaleX = 4f;
            sLeaser.sprites[Arm1Sprite].anchorY = 0f;
            sLeaser.sprites[Arm2Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm2Sprite].scaleX = 3f;
            sLeaser.sprites[Arm2Sprite].anchorY = 0f;
            sLeaser.sprites[Arm3Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm3Sprite].scaleX = 1.5f;
            sLeaser.sprites[Arm3Sprite].scaleY = armLength * 0.6f;
            sLeaser.sprites[Arm3Sprite].anchorY = 0f;
            sLeaser.sprites[Arm4Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm4Sprite].scaleX = 3f;
            sLeaser.sprites[Arm4Sprite].scaleY = 8f;
            sLeaser.sprites[Arm5Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm5Sprite].scaleX = 6f;
            sLeaser.sprites[Arm5Sprite].scaleY = 8f;
            sLeaser.sprites[ArmJointSprite] = new FSprite("JetFishEyeA", true);
            sLeaser.sprites[LampSprite] = new FSprite("tinyStar", true);
            sLeaser.sprites[SocketSprite] = new FSprite("pixel", true);
            sLeaser.sprites[SocketSprite].scaleX = 5f;
            sLeaser.sprites[SocketSprite].scaleY = 9f;

            sLeaser.sprites[HeadSprite] = new FSprite("MiniSatellite", true);
            sLeaser.sprites[SataFlasher] = new FSprite("Futile_White", true);
            sLeaser.sprites[SataFlasher].shader = rCam.room.game.rainWorld.Shaders["FlatLight"];
            sLeaser.sprites[SataFlasher].scale = 0.8f;
            sLeaser.sprites[SataFlasher].isVisible = false;


            for (int i = 0; i < coord.GetLength(0); i++)
            {
                sLeaser.sprites[CoordSprite(i)] = new FSprite("pixel", true);
                sLeaser.sprites[CoordSprite(i)].scaleX = ((i % 2 == 0) ? 2f : 3f);
                sLeaser.sprites[CoordSprite(i)].scaleY = 5f;
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[BaseSprite].x = basePos.x - camPos.x;
            sLeaser.sprites[BaseSprite].y = basePos.y - camPos.y;
            Vector2 vector = Vector2.Lerp(lastHead, head, timeStacker);
            Vector2 vector2 = Vector3.Slerp(lastHeadDir, headDir, timeStacker);
            Vector2 vector3 = Vector2.Lerp(lastArmPos, armPos, timeStacker);
            Vector2 vector4 = Custom.InverseKinematic(basePos, vector3, armLength * 0.65f, armLength * 0.35f, flip);
            sLeaser.sprites[Arm1Sprite].x = basePos.x - camPos.x;
            sLeaser.sprites[Arm1Sprite].y = basePos.y - camPos.y;
            sLeaser.sprites[Arm1Sprite].scaleY = Vector2.Distance(basePos, vector4);
            sLeaser.sprites[Arm1Sprite].rotation = Custom.AimFromOneVectorToAnother(basePos, vector4);
            sLeaser.sprites[Arm2Sprite].x = vector4.x - camPos.x;
            sLeaser.sprites[Arm2Sprite].y = vector4.y - camPos.y;
            sLeaser.sprites[Arm2Sprite].scaleY = Vector2.Distance(vector4, vector3);
            sLeaser.sprites[Arm2Sprite].rotation = Custom.AimFromOneVectorToAnother(vector4, vector3);
            sLeaser.sprites[SocketSprite].x = vector3.x - camPos.x;
            sLeaser.sprites[SocketSprite].y = vector3.y - camPos.y;
            sLeaser.sprites[SocketSprite].rotation = Custom.VecToDeg(Vector3.Slerp(Custom.DirVec(vector4, vector3), Custom.DirVec(vector3, Vector2.Lerp(coord[0, 1], coord[0, 0], timeStacker)), 0.4f));
            Vector2 vector5 = Vector2.Lerp(basePos, vector4, 0.3f);
            Vector2 vector6 = Vector2.Lerp(vector4, vector3, 0.4f);
            sLeaser.sprites[Arm3Sprite].x = vector5.x - camPos.x;
            sLeaser.sprites[Arm3Sprite].y = vector5.y - camPos.y;
            sLeaser.sprites[Arm3Sprite].rotation = Custom.AimFromOneVectorToAnother(vector5, vector6);
            sLeaser.sprites[Arm4Sprite].x = vector6.x - camPos.x;
            sLeaser.sprites[Arm4Sprite].y = vector6.y - camPos.y;
            sLeaser.sprites[Arm4Sprite].rotation = Custom.AimFromOneVectorToAnother(vector5, vector6);
            vector5 += Custom.DirVec(basePos, vector4) * (armLength * 0.1f + 2f);
            sLeaser.sprites[Arm5Sprite].x = vector5.x - camPos.x;
            sLeaser.sprites[Arm5Sprite].y = vector5.y - camPos.y;
            sLeaser.sprites[Arm5Sprite].rotation = Custom.AimFromOneVectorToAnother(basePos, vector4);
            sLeaser.sprites[LampSprite].x = vector5.x - camPos.x;
            sLeaser.sprites[LampSprite].y = vector5.y - camPos.y;
            sLeaser.sprites[LampSprite].color = Color.Lerp(lampOffCol, lampColor, Mathf.Lerp(lastLampPower, lampPower, timeStacker) * Mathf.Pow(Random.value, 0.5f) * (0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(lastSinCounter, sinCounter, timeStacker) / 6f)));
            sLeaser.sprites[ArmJointSprite].x = vector4.x - camPos.x;
            sLeaser.sprites[ArmJointSprite].y = vector4.y - camPos.y;
            sLeaser.sprites[HeadSprite].x = vector.x - camPos.x;
            sLeaser.sprites[HeadSprite].y = vector.y - camPos.y;
            if (ModManager.MSC && forceSatellite && sLeaser.sprites[HeadSprite].element.name != "MiniSatellite")
            {
                sLeaser.sprites[HeadSprite].SetElementByName("MiniSatellite");
                sLeaser.sprites[HeadSprite].scaleX = 1f;
                sLeaser.sprites[HeadSprite].scaleY = 1f;
            }

            sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(vector2) - 90f;
            if (sataFlasherLight >= 99)
            {
                sLeaser.sprites[SataFlasher].isVisible = !sLeaser.sprites[SataFlasher].isVisible;
                sataFlasherLight = 0;
            }
            sLeaser.sprites[SataFlasher].color = Color.Lerp(TokenColor, lampOffCol, Random.value * 0.1f);
            sLeaser.sprites[SataFlasher].alpha = 0.9f + Random.value * 0.09f;
            sLeaser.sprites[SataFlasher].x = vector.x + vector2.x * 5f - camPos.x;
            sLeaser.sprites[SataFlasher].y = vector.y + vector2.y * 5f - camPos.y;

            //else
            //{
            //    sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(vector2);
            //    if (ModManager.MSC)
            //    {
            //        sLeaser.sprites[SataFlasher].isVisible = false;
            //    }
            //}
            Vector2 p = vector3;
            for (int i = 0; i < coord.GetLength(0); i++)
            {
                Vector2 vector7 = Vector2.Lerp(coord[i, 1], coord[i, 0], timeStacker);
                sLeaser.sprites[CoordSprite(i)].x = vector7.x - camPos.x;
                sLeaser.sprites[CoordSprite(i)].y = vector7.y - camPos.y;
                sLeaser.sprites[CoordSprite(i)].rotation = Custom.AimFromOneVectorToAnother(p, vector7);
                p = vector7;
            }
            if (base.slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        public Color TokenColor => new(171f / 255f, 220f / 255f, 255f / 255f);

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            foreach (var sprite in sLeaser.sprites)
                sprite.color = palette.blackColor;
            
            lampOffCol = Color.Lerp(palette.blackColor, TokenColor, 0.15f);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer(layerName: "Midground");

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                if (ModManager.MSC && i == SataFlasher)
                    rCam.ReturnFContainer(layerName: "ForegroundLights").AddChild(sLeaser.sprites[SataFlasher]);
                
                else
                    newContatiner.AddChild(sLeaser.sprites[i]);
                
            }
        }

        public Vector2 hoverPos;

        private CustomWhiteToken token;
        

        public Vector2 basePos;

        public Vector2 mainDir;

        public float flip;

        public Vector2 armPos;

        public Vector2 lastArmPos;

        public Vector2 armVel;

        public Vector2 armGetToPos;

        public Vector2 head;

        public Vector2 lastHead;

        public Vector2 headVel;

        public Vector2 headDir;

        public Vector2 lastHeadDir;

        private float headDist = 15f;

        public float armLength;

        private Vector2[,] coord;

        private float coordLength;

        private float coordSeg = 3f;

        private float[,] curveLerps;

        private float keepDistance;

        private float sinCounter;

        private float lastSinCounter;

        private float lampPower;

        private float lastLampPower;

        private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData = new SharedPhysics.TerrainCollisionData();

        public Color lampColor;

        public bool forceSatellite;

        public int sataFlasherLight;

        private Color lampOffCol;
    }
}
